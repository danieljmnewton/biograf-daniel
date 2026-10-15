using Biograf.Api.Common;
using Biograf.Api.Models;
using Biograf.Api.Services;
using Biograf.Shared;
using Biograf.Shared.Bokningar;
using Biograf.Tests.Hjalpare;
using Microsoft.EntityFrameworkCore;

namespace Biograf.Tests;

/// <summary>Scenarier för ändringsbegäran #32, biljettyper. Se docs/testscenarier.md (T1–T5).</summary>
public class BiljettypTester : IDisposable
{
    private readonly TestDatabas testDb = new();

    private BokningTjanst NyTjanst() => new(testDb.SkapaKontext());

    private static PlatsVal Plats(int sateId, Biljettyp typ) => new() { SateId = sateId, Biljettyp = typ };

    [Theory(DisplayName = "T1: Priset räknas från grundpris, biljettyp och VIP")]
    [InlineData(120, false, Biljettyp.Vuxen, 120)]
    [InlineData(120, false, Biljettyp.Barn, 80)]
    [InlineData(120, false, Biljettyp.Pensionar, 95)]
    [InlineData(120, true, Biljettyp.Vuxen, 170)]
    [InlineData(120, true, Biljettyp.Barn, 130)]
    [InlineData(30, false, Biljettyp.Barn, 0)]   // rabatten kan inte ge negativt pris
    public void Prisberakning(int grundpris, bool vip, Biljettyp typ, int forvantat)
    {
        Assert.Equal(forvantat, Priser.Berakna(grundpris, vip, typ));
    }

    [Fact(DisplayName = "T2: En bokning med blandade biljettyper får rätt pris per plats och totalt")]
    public async Task Blandade_biljettyper_i_samma_bokning()
    {
        var dto = new SkapaBokningDto
        {
            VisningId = testDb.Visning.Id,
            Platser =
            [
                Plats(testDb.Sate(testDb.Salong, 1, 1), Biljettyp.Vuxen),     // 120
                Plats(testDb.Sate(testDb.Salong, 1, 2), Biljettyp.Barn),      // 80
                Plats(testDb.Sate(testDb.Salong, 3, 1), Biljettyp.Pensionar)  // 95 + 50 VIP = 145
            ]
        };

        var resultat = await NyTjanst().SkapaAsync(testDb.Kund.Id, dto);

        Assert.True(resultat.Lyckades);
        Assert.Equal(120 + 80 + 145, resultat.Varde!.Totalpris);
        Assert.Equal(
            [(Biljettyp.Vuxen, 120), (Biljettyp.Barn, 80), (Biljettyp.Pensionar, 145)],
            resultat.Varde.Platser.Select(p => (p.Biljettyp, p.Pris)));
    }

    [Fact(DisplayName = "T3: Barnbiljett nekas till en film från 15 år, men vuxenbiljett går bra")]
    public async Task Barnbiljett_nekas_till_film_fran_15_ar()
    {
        var visning = await SkapaVisningMedAldersgransAsync(15);

        var barn = await NyTjanst().SkapaAsync(testDb.Kund.Id,
            new SkapaBokningDto { VisningId = visning.Id, Platser = [Plats(testDb.Sate(testDb.Salong, 1, 1), Biljettyp.Barn)] });
        var vuxen = await NyTjanst().SkapaAsync(testDb.Kund.Id,
            new SkapaBokningDto { VisningId = visning.Id, Platser = [Plats(testDb.Sate(testDb.Salong, 1, 1), Biljettyp.Vuxen)] });

        Assert.Equal(FelTyp.Ogiltig, barn.Fel);
        Assert.Contains("Barnbiljetter", barn.Meddelande);
        Assert.True(vuxen.Lyckades);
    }

    [Fact(DisplayName = "T4: Samma plats kan inte väljas två gånger i samma bokning")]
    public async Task Samma_plats_tva_ganger_nekas()
    {
        var sate = testDb.Sate(testDb.Salong, 2, 1);
        var dto = new SkapaBokningDto
        {
            VisningId = testDb.Visning.Id,
            Platser = [Plats(sate, Biljettyp.Vuxen), Plats(sate, Biljettyp.Barn)]
        };

        var resultat = await NyTjanst().SkapaAsync(testDb.Kund.Id, dto);

        Assert.Equal(FelTyp.Ogiltig, resultat.Fel);
    }

    [Fact(DisplayName = "T5: Biljettypen sparas i databasen och visas i Mina bokningar")]
    public async Task Biljettyp_sparas_och_visas()
    {
        var dto = new SkapaBokningDto
        {
            VisningId = testDb.Visning.Id,
            Platser = [Plats(testDb.Sate(testDb.Salong, 2, 4), Biljettyp.Pensionar)]
        };
        await NyTjanst().SkapaAsync(testDb.Kund.Id, dto);

        await using var db = testDb.SkapaKontext();
        var sparad = await db.BokadePlatser.SingleAsync();
        var mina = await NyTjanst().HamtaMinaAsync(testDb.Kund.Id);

        Assert.Equal(Biljettyp.Pensionar, sparad.Biljettyp);
        Assert.Equal(Biljettyp.Pensionar, mina.Single().Platser.Single().Biljettyp);
    }

    private async Task<Visning> SkapaVisningMedAldersgransAsync(int aldersgrans)
    {
        await using var db = testDb.SkapaKontext();
        var film = new Film { Titel = $"Film från {aldersgrans} år", Genre = "Skräck", Aldersgrans = aldersgrans, LangdMinuter = 90 };
        var visning = new Visning { Film = film, SalongId = testDb.Salong.Id, Starttid = TestDatabas.Imorgon18.AddDays(1), Grundpris = 120 };
        db.Visningar.Add(visning);
        await db.SaveChangesAsync();
        return visning;
    }

    public void Dispose() => testDb.Dispose();
}
