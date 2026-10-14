using Biograf.Api.Common;
using Biograf.Api.Services;
using Biograf.Shared;
using Biograf.Shared.Visningar;
using Biograf.Tests.Hjalpare;

namespace Biograf.Tests;

/// <summary>Scenarier för schemaläggning av visningar. Se docs/testscenarier.md (V1–V5).</summary>
public class VisningTester : IDisposable
{
    private readonly TestDatabas testDb = new();

    // Testfilmen är 100 minuter och visas imorgon 18:00, så salongen är upptagen
    // till 19:40 + 20 minuters städning = 20:00.
    private static readonly DateTime Start = TestDatabas.Imorgon18;

    private VisningTjanst NyTjanst() => new(testDb.SkapaKontext());

    private SparaVisningDto Visning(DateTime starttid, int? salongId = null) => new()
    {
        FilmId = testDb.Film.Id,
        SalongId = salongId ?? testDb.Salong.Id,
        Starttid = starttid,
        Grundpris = 130
    };

    [Theory(DisplayName = "V1: En visning som överlappar en annan i samma salong nekas")]
    [InlineData(0)]     // exakt samma tid
    [InlineData(60)]    // under pågående film
    [InlineData(110)]   // under städtiden
    [InlineData(-90)]   // börjar före och slutar under den befintliga
    public async Task Overlappande_visning_nekas(int minuterFranStart)
    {
        var resultat = await NyTjanst().SkapaAsync(Visning(Start.AddMinutes(minuterFranStart)));

        Assert.Equal(FelTyp.Konflikt, resultat.Fel);
        Assert.Contains("Salongen är upptagen", resultat.Meddelande);
    }

    [Fact(DisplayName = "V2: En visning direkt efter filmens längd plus städtid godkänns")]
    public async Task Visning_efter_stadtid_godkanns()
    {
        var direktEfter = Start.AddMinutes(testDb.Film.LangdMinuter + Priser.StadtidMinuter);

        var resultat = await NyTjanst().SkapaAsync(Visning(direktEfter));

        Assert.True(resultat.Lyckades);
        Assert.Equal(direktEfter, resultat.Varde!.Starttid);
    }

    [Fact(DisplayName = "V3: Samma tid i en annan salong godkänns")]
    public async Task Samma_tid_i_annan_salong_godkanns()
    {
        var resultat = await NyTjanst().SkapaAsync(Visning(Start, testDb.AnnanSalong.Id));

        Assert.True(resultat.Lyckades);
        Assert.Equal(testDb.AnnanSalong.Kapacitet, resultat.Varde!.LedigaPlatser);
    }

    [Fact(DisplayName = "V4: En starttid som redan har passerat nekas")]
    public async Task Starttid_i_dataiden_nekas()
    {
        var resultat = await NyTjanst().SkapaAsync(Visning(DateTime.Now.AddHours(-1)));

        Assert.Equal(FelTyp.Ogiltig, resultat.Fel);
    }

    [Fact(DisplayName = "V5: En visning kan flyttas utan att krocka med sig själv")]
    public async Task Visning_kan_flyttas_en_kvart()
    {
        var resultat = await NyTjanst().UppdateraAsync(testDb.Visning.Id, Visning(Start.AddMinutes(15)));

        Assert.True(resultat.Lyckades);
        Assert.Equal(Start.AddMinutes(15), resultat.Varde!.Starttid);
    }

    public void Dispose() => testDb.Dispose();
}
