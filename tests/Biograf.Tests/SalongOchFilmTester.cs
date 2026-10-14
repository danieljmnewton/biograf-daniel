using Biograf.Api.Common;
using Biograf.Api.Models;
using Biograf.Api.Services;
using Biograf.Shared.Filmer;
using Biograf.Shared.Salonger;
using Biograf.Tests.Hjalpare;

namespace Biograf.Tests;

/// <summary>Scenarier för salonger och filmregistret. Se docs/testscenarier.md (S1–S3, F1–F3).</summary>
public class SalongOchFilmTester : IDisposable
{
    private readonly TestDatabas testDb = new();

    [Fact(DisplayName = "S1: Sätena genereras rad för rad med VIP-raderna längst bak")]
    public void Saten_genereras_med_VIP_bakerst()
    {
        var salong = new Salong();
        salong.GenereraSaten(rader: 5, platserPerRad: 8, vipRader: 2);

        Assert.Equal(40, salong.Kapacitet);
        Assert.All(salong.Saten.Where(s => s.Rad >= 4), s => Assert.Equal(SateTyp.VIP, s.Typ));
        Assert.All(salong.Saten.Where(s => s.Rad <= 3), s => Assert.Equal(SateTyp.Standard, s.Typ));
    }

    [Fact(DisplayName = "S2: Fler VIP-rader än rader är inte tillåtet")]
    public void For_manga_VIP_rader_ger_fel()
    {
        Assert.Throws<ArgumentException>(() => new Salong().GenereraSaten(rader: 2, platserPerRad: 5, vipRader: 3));
    }

    [Fact(DisplayName = "S3: Platslayouten kan inte ändras när salongen har visningar, men namnet kan")]
    public async Task Layout_last_nar_salongen_har_visningar()
    {
        var tjanst = new SalongTjanst(testDb.SkapaKontext());

        var nyLayout = await tjanst.UppdateraAsync(testDb.Salong.Id,
            new SparaSalongDto { Namn = "Salong A", Rader = 5, PlatserPerRad = 4, VipRader = 1 });
        var nyttNamn = await tjanst.UppdateraAsync(testDb.Salong.Id,
            new SparaSalongDto { Namn = "Salong A – Guld", Rader = 3, PlatserPerRad = 4, VipRader = 1 });

        Assert.Equal(FelTyp.Konflikt, nyLayout.Fel);
        Assert.True(nyttNamn.Lyckades);
        Assert.Equal("Salong A – Guld", nyttNamn.Varde!.Namn);
    }

    [Fact(DisplayName = "F1: En film med kommande visningar kan inte tas bort")]
    public async Task Film_med_visningar_kan_inte_tas_bort()
    {
        var resultat = await new FilmTjanst(testDb.SkapaKontext()).TaBortAsync(testDb.Film.Id);

        Assert.Equal(FelTyp.Konflikt, resultat.Fel);
    }

    [Fact(DisplayName = "F2: Sökning och filter kombineras")]
    public async Task Sok_och_filter_kombineras()
    {
        var tjanst = new FilmTjanst(testDb.SkapaKontext());
        await tjanst.SkapaAsync(new SparaFilmDto { Titel = "Testfilmen 2", Genre = "Skräck", Aldersgrans = 15, LangdMinuter = 90 });
        await tjanst.SkapaAsync(new SparaFilmDto { Titel = "Barnfilmen", Genre = "Drama", Aldersgrans = 0, LangdMinuter = 80 });

        var testfilmer = await tjanst.HamtaAllaAsync(new FilmFilter { Sok = "testfilm" });
        var dramaForAlla = await tjanst.HamtaAllaAsync(new FilmFilter { Genre = "Drama", MaxAldersgrans = 7 });

        Assert.Equal(["Testfilmen", "Testfilmen 2"], testfilmer.Select(f => f.Titel));
        Assert.Equal("Barnfilmen", Assert.Single(dramaForAlla).Titel);
    }

    [Fact(DisplayName = "F3: En ny film utan visningar kan tas bort")]
    public async Task Ny_film_kan_tas_bort()
    {
        var tjanst = new FilmTjanst(testDb.SkapaKontext());
        var film = await tjanst.SkapaAsync(new SparaFilmDto { Titel = "Felregistrerad", Genre = "Drama", LangdMinuter = 90 });

        var resultat = await tjanst.TaBortAsync(film.Varde!.Id);
        var efter = await tjanst.HamtaAsync(film.Varde.Id);

        Assert.True(resultat.Lyckades);
        Assert.Equal(FelTyp.HittadesInte, efter.Fel);
    }

    public void Dispose() => testDb.Dispose();
}
