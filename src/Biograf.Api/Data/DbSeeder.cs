using Biograf.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Biograf.Api.Data;

/// <summary>
/// Fyller en tom databas med testdata så att appen går att demonstrera direkt.
/// Körs bara om det inte redan finns några filmer.
/// </summary>
public static class DbSeeder
{
    public static async Task SeedAsync(BiografDbContext db)
    {
        if (await db.Filmer.AnyAsync())
            return;

        var filmer = SkapaFilmer();
        var salonger = SkapaSalonger();

        db.Filmer.AddRange(filmer);
        db.Salonger.AddRange(salonger);
        await db.SaveChangesAsync();
    }

    private static List<Film> SkapaFilmer() =>
    [
        new() { Titel = "Dune: Part Two", Genre = "Sci-fi", Aldersgrans = 11, LangdMinuter = 166,
            Beskrivning = "Paul Atreides förenar sig med Chani och fremen på sin väg mot hämnd mot dem som förstörde hans familj." },
        new() { Titel = "Oppenheimer", Genre = "Drama", Aldersgrans = 15, LangdMinuter = 180,
            Beskrivning = "Berättelsen om J. Robert Oppenheimer och utvecklingen av atombomben under andra världskriget." },
        new() { Titel = "Inside Out 2", Genre = "Animerat", Aldersgrans = 0, LangdMinuter = 96,
            Beskrivning = "Riley är tonåring och nya känslor flyttar in i huvudkontoret – bland dem Ångest." },
        new() { Titel = "The Batman", Genre = "Action", Aldersgrans = 15, LangdMinuter = 176,
            Beskrivning = "Batman jagar en seriemördare som riktar in sig på Gotham Citys elit." },
        new() { Titel = "Knives Out", Genre = "Thriller", Aldersgrans = 11, LangdMinuter = 130,
            Beskrivning = "Detektiven Benoit Blanc utreder ett mystiskt dödsfall i en rik och dysfunktionell familj." },
        new() { Titel = "Spider-Man: Across the Spider-Verse", Genre = "Animerat", Aldersgrans = 7, LangdMinuter = 140,
            Beskrivning = "Miles Morales kastas ut i multiversum och möter ett helt sällskap av Spider-personer." },
        new() { Titel = "Barbie", Genre = "Komedi", Aldersgrans = 7, LangdMinuter = 114,
            Beskrivning = "Barbie och Ken lämnar Barbieland och upptäcker hur det är att leva i den riktiga världen." },
        new() { Titel = "A Quiet Place", Genre = "Skräck", Aldersgrans = 15, LangdMinuter = 90,
            Beskrivning = "En familj måste leva i total tystnad för att undkomma varelser som jagar med hjälp av ljud." },
        new() { Titel = "Top Gun: Maverick", Genre = "Action", Aldersgrans = 11, LangdMinuter = 131,
            Beskrivning = "Efter trettio år i tjänst får Maverick i uppdrag att utbilda en ny generation stridspiloter." },
        new() { Titel = "Paddington 2", Genre = "Familj", Aldersgrans = 0, LangdMinuter = 103,
            Beskrivning = "Paddington tar ströjobb för att köpa en present till faster Lucy, men boken han vill ha blir stulen." },
    ];

    private static List<Salong> SkapaSalonger()
    {
        var stora = new Salong { Namn = "Salong 1 – Stora" };
        stora.GenereraSaten(rader: 10, platserPerRad: 14, vipRader: 2);

        var mellan = new Salong { Namn = "Salong 2" };
        mellan.GenereraSaten(rader: 8, platserPerRad: 12, vipRader: 1);

        var lilla = new Salong { Namn = "Salong 3 – Lilla" };
        lilla.GenereraSaten(rader: 6, platserPerRad: 10, vipRader: 1);

        return [stora, mellan, lilla];
    }
}
