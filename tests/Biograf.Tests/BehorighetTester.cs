using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Biograf.Shared.Auth;
using Biograf.Shared.Filmer;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Biograf.Tests;

/// <summary>
/// Startar hela API:t i minnet (med en egen temporär databasfil och de seedade testkontona)
/// och testar inloggning och rollskydd via riktiga HTTP-anrop. Se docs/testscenarier.md (R1–R4).
/// </summary>
public class BehorighetTester : IClassFixture<BehorighetTester.ApiFabrik>
{
    public class ApiFabrik : WebApplicationFactory<Program>
    {
        private readonly string databas = Path.Combine(Path.GetTempPath(), $"biograf-test-{Guid.NewGuid():N}.db");

        protected override void ConfigureWebHost(Microsoft.AspNetCore.Hosting.IWebHostBuilder builder) =>
            builder.UseSetting("ConnectionStrings:Biograf", $"Data Source={databas}");

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
            File.Delete(databas);
        }
    }

    private readonly ApiFabrik fabrik;

    public BehorighetTester(ApiFabrik fabrik) => this.fabrik = fabrik;

    private async Task<HttpClient> InloggadKlientAsync(string email, string losenord)
    {
        var klient = fabrik.CreateClient();
        var svar = await klient.PostAsJsonAsync("api/auth/logga-in", new LoggaInDto { Email = email, Losenord = losenord });
        svar.EnsureSuccessStatusCode();

        var inloggning = await svar.Content.ReadFromJsonAsync<InloggningSvar>();
        klient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", inloggning!.Token);
        return klient;
    }

    private static SparaFilmDto NyFilm() => new() { Titel = "Behörighetstest", Genre = "Drama", LangdMinuter = 90 };

    [Fact(DisplayName = "R1: Fel lösenord ger 401 och samma meddelande som okänd e-post")]
    public async Task Fel_losenord_ger_401()
    {
        var klient = fabrik.CreateClient();

        var felLosen = await klient.PostAsJsonAsync("api/auth/logga-in", new LoggaInDto { Email = "kund@biograf.se", Losenord = "fel123" });
        var okandEpost = await klient.PostAsJsonAsync("api/auth/logga-in", new LoggaInDto { Email = "finns@inte.se", Losenord = "fel123" });

        Assert.Equal(HttpStatusCode.Unauthorized, felLosen.StatusCode);
        Assert.Equal(await felLosen.Content.ReadAsStringAsync(), await okandEpost.Content.ReadAsStringAsync());
    }

    [Fact(DisplayName = "R2: Alla kan läsa filmer, men utan inloggning går det inte att skapa")]
    public async Task Utloggad_kan_lasa_men_inte_skapa()
    {
        var klient = fabrik.CreateClient();

        Assert.Equal(HttpStatusCode.OK, (await klient.GetAsync("api/filmer")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await klient.PostAsJsonAsync("api/filmer", NyFilm())).StatusCode);
    }

    [Theory(DisplayName = "R3: Bara personal och admin får ändra i filmregistret")]
    [InlineData("kund@biograf.se", "Kund123", HttpStatusCode.Forbidden)]
    [InlineData("personal@biograf.se", "Personal123", HttpStatusCode.Created)]
    [InlineData("admin@biograf.se", "Admin123", HttpStatusCode.Created)]
    public async Task Rollskydd_pa_filmregistret(string email, string losenord, HttpStatusCode forvantat)
    {
        var klient = await InloggadKlientAsync(email, losenord);

        var svar = await klient.PostAsJsonAsync("api/filmer", NyFilm());

        Assert.Equal(forvantat, svar.StatusCode);
    }

    [Theory(DisplayName = "R4: Bara admin kommer åt användarhanteringen")]
    [InlineData("kund@biograf.se", "Kund123", HttpStatusCode.Forbidden)]
    [InlineData("personal@biograf.se", "Personal123", HttpStatusCode.Forbidden)]
    [InlineData("admin@biograf.se", "Admin123", HttpStatusCode.OK)]
    public async Task Bara_admin_ser_anvandare(string email, string losenord, HttpStatusCode forvantat)
    {
        var klient = await InloggadKlientAsync(email, losenord);

        var svar = await klient.GetAsync("api/admin/anvandare");

        Assert.Equal(forvantat, svar.StatusCode);
    }
}
