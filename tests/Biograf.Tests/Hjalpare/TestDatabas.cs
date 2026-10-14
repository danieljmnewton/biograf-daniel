using Biograf.Api.Data;
using Biograf.Api.Models;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Biograf.Tests.Hjalpare;

/// <summary>
/// En SQLite-databas i minnet med samma schema som den riktiga. Till skillnad från
/// EF:s InMemory-provider gäller unika index och transaktioner, så testerna visar
/// hur systemet beter sig mot en riktig databas.
/// </summary>
public sealed class TestDatabas : IDisposable
{
    private readonly SqliteConnection anslutning;

    public Film Film { get; }
    public Salong Salong { get; }
    public Salong AnnanSalong { get; }
    public Visning Visning { get; }
    public Anvandare Kund { get; }
    public Anvandare AnnanKund { get; }

    /// <summary>Visningens starttid: imorgon kl. 18.</summary>
    public static DateTime Imorgon18 => DateTime.Today.AddDays(1).AddHours(18);

    public TestDatabas()
    {
        anslutning = new SqliteConnection("DataSource=:memory:");
        anslutning.Open();

        using var db = SkapaKontext();
        db.Database.EnsureCreated();

        Film = new Film { Titel = "Testfilmen", Genre = "Drama", Aldersgrans = 11, LangdMinuter = 100 };

        // 3 rader × 4 platser där rad 3 är VIP.
        Salong = new Salong { Namn = "Salong A" };
        Salong.GenereraSaten(rader: 3, platserPerRad: 4, vipRader: 1);

        AnnanSalong = new Salong { Namn = "Salong B" };
        AnnanSalong.GenereraSaten(rader: 2, platserPerRad: 2, vipRader: 0);

        Visning = new Visning { Film = Film, Salong = Salong, Starttid = Imorgon18, Grundpris = 120 };

        Kund = new Anvandare { UserName = "kund@test.se", Email = "kund@test.se", Fornamn = "Klara", Efternamn = "Kund" };
        AnnanKund = new Anvandare { UserName = "annan@test.se", Email = "annan@test.se", Fornamn = "Anna", Efternamn = "Annan" };

        db.AddRange(Film, Salong, AnnanSalong, Visning, Kund, AnnanKund);
        db.SaveChanges();
    }

    /// <summary>
    /// Skapar en ny DbContext mot samma databas. Varje test använder nya kontexter
    /// så att inget ligger kvar i EF:s minne mellan stegen.
    /// </summary>
    public BiografDbContext SkapaKontext() =>
        new(new DbContextOptionsBuilder<BiografDbContext>().UseSqlite(anslutning).Options);

    /// <summary>Id för ett säte i salongen, t.ex. Sate(Salong, 1, 2) = rad 1 plats 2.</summary>
    public int Sate(Salong salong, int rad, int nummer) =>
        salong.Saten.Single(s => s.Rad == rad && s.Nummer == nummer).Id;

    public void Dispose() => anslutning.Dispose();
}
