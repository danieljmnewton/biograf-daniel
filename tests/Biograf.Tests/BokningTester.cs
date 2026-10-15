using Biograf.Api.Common;
using Biograf.Api.Models;
using Biograf.Api.Services;
using Biograf.Shared.Bokningar;
using Biograf.Tests.Hjalpare;
using Microsoft.EntityFrameworkCore;

namespace Biograf.Tests;

/// <summary>Scenarier för bokning och avbokning. Se docs/testscenarier.md (B1–B9).</summary>
public class BokningTester : IDisposable
{
    private readonly TestDatabas testDb = new();

    private BokningTjanst NyTjanst() => new(testDb.SkapaKontext());

    private SkapaBokningDto Bokning(params int[] sateIds) =>
        new() { VisningId = testDb.Visning.Id, Platser = sateIds.Select(id => new PlatsVal { SateId = id }).ToList() };

    [Fact(DisplayName = "B1: Kund bokar en standard- och en VIP-plats och får rätt totalpris")]
    public async Task Bokning_med_VIP_plats_far_pristillagg()
    {
        var resultat = await NyTjanst().SkapaAsync(testDb.Kund.Id,
            Bokning(testDb.Sate(testDb.Salong, 1, 1), testDb.Sate(testDb.Salong, 3, 1)));

        Assert.True(resultat.Lyckades);
        Assert.Equal(120 + 170, resultat.Varde!.Totalpris);
        Assert.Equal(Bokningsregler.Aktiv, resultat.Varde.Status);
        Assert.Matches("^NB-[A-Z2-9]{5}$", resultat.Varde.Bokningsnummer);
    }

    [Fact(DisplayName = "B2: En plats som redan är bokad kan inte bokas igen")]
    public async Task Dubbelbokning_nekas()
    {
        var plats = testDb.Sate(testDb.Salong, 2, 2);
        await NyTjanst().SkapaAsync(testDb.Kund.Id, Bokning(plats));

        var resultat = await NyTjanst().SkapaAsync(testDb.AnnanKund.Id, Bokning(plats, testDb.Sate(testDb.Salong, 2, 3)));

        Assert.False(resultat.Lyckades);
        Assert.Equal(FelTyp.Konflikt, resultat.Fel);
        Assert.Contains("rad 2 plats 2", resultat.Meddelande);
    }

    [Fact(DisplayName = "B3: Databasens unika index stoppar dubbelbokning även om tjänstens kontroll kringgås")]
    public async Task Unikt_index_stoppar_dubbelbokning()
    {
        // Simulerar två samtidiga bokningar som båda passerat kontrollen: två aktiva
        // platser för samma säte och visning sparas direkt i databasen.
        var sateId = testDb.Sate(testDb.Salong, 1, 4);
        await using var db = testDb.SkapaKontext();
        db.Bokningar.AddRange(
            NyBokning("NB-AAAAA", testDb.Kund.Id, sateId),
            NyBokning("NB-BBBBB", testDb.AnnanKund.Id, sateId));

        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
    }

    [Fact(DisplayName = "B4: En plats från en annan salong nekas")]
    public async Task Plats_i_fel_salong_nekas()
    {
        var resultat = await NyTjanst().SkapaAsync(testDb.Kund.Id, Bokning(testDb.Sate(testDb.AnnanSalong, 1, 1)));

        Assert.Equal(FelTyp.Ogiltig, resultat.Fel);
    }

    [Fact(DisplayName = "B5: En avbokad plats blir ledig och kan bokas av någon annan")]
    public async Task Avbokad_plats_kan_bokas_igen()
    {
        var plats = testDb.Sate(testDb.Salong, 1, 2);
        var forsta = await NyTjanst().SkapaAsync(testDb.Kund.Id, Bokning(plats));

        var avbokning = await NyTjanst().AvbokaAsync(forsta.Varde!.Id, testDb.Kund.Id, arPersonal: false);
        var karta = await NyTjanst().HamtaSateskartaAsync(testDb.Visning.Id);
        var andra = await NyTjanst().SkapaAsync(testDb.AnnanKund.Id, Bokning(plats));

        Assert.Equal(Bokningsregler.Avbokad, avbokning.Varde!.Status);
        Assert.False(karta.Varde!.Saten.Single(s => s.Id == plats).Upptagen);
        Assert.True(andra.Lyckades);
    }

    [Fact(DisplayName = "B6: Kunden kan inte avboka mindre än två timmar före visningen, men personalen kan")]
    public async Task Sen_avbokning_bara_for_personal()
    {
        var snart = await SkapaVisningAsync(DateTime.Now.AddHours(1));
        var bokning = await NyTjanst().SkapaAsync(testDb.Kund.Id,
            new SkapaBokningDto { VisningId = snart.Id, Platser = [new PlatsVal { SateId = testDb.Sate(testDb.Salong, 1, 1) }] });

        Assert.False(bokning.Varde!.KanAvbokas);

        var kundensForsok = await NyTjanst().AvbokaAsync(bokning.Varde.Id, testDb.Kund.Id, arPersonal: false);
        var personalensForsok = await NyTjanst().AvbokaAsync(bokning.Varde.Id, "personal-id", arPersonal: true);

        Assert.Equal(FelTyp.Ogiltig, kundensForsok.Fel);
        Assert.True(personalensForsok.Lyckades);
    }

    [Fact(DisplayName = "B7: En kund kan inte se eller avboka någon annans bokning")]
    public async Task Kund_ser_bara_egna_bokningar()
    {
        var bokning = await NyTjanst().SkapaAsync(testDb.Kund.Id, Bokning(testDb.Sate(testDb.Salong, 1, 3)));

        var visa = await NyTjanst().HamtaAsync(bokning.Varde!.Id, testDb.AnnanKund.Id, arPersonal: false);
        var avboka = await NyTjanst().AvbokaAsync(bokning.Varde.Id, testDb.AnnanKund.Id, arPersonal: false);
        var mina = await NyTjanst().HamtaMinaAsync(testDb.AnnanKund.Id);

        Assert.Equal(FelTyp.HittadesInte, visa.Fel);
        Assert.Equal(FelTyp.HittadesInte, avboka.Fel);
        Assert.Empty(mina);
    }

    [Fact(DisplayName = "B8: Eventen BokningSkapad och BokningAvbokad utlöses")]
    public async Task Events_utloses_vid_bokning_och_avbokning()
    {
        var tjanst = NyTjanst();
        var handelser = new List<string>();
        tjanst.BokningSkapad += (_, e) => handelser.Add($"skapad {e.Bokning.Bokningsnummer}");
        tjanst.BokningAvbokad += (_, e) => handelser.Add($"avbokad {e.Bokning.Bokningsnummer}");

        var bokning = await tjanst.SkapaAsync(testDb.Kund.Id, Bokning(testDb.Sate(testDb.Salong, 2, 1)));
        await tjanst.AvbokaAsync(bokning.Varde!.Id, testDb.Kund.Id, arPersonal: false);

        var nummer = bokning.Varde.Bokningsnummer;
        Assert.Equal([$"skapad {nummer}", $"avbokad {nummer}"], handelser);
    }

    [Fact(DisplayName = "B9: En visning som redan har börjat går inte att boka")]
    public async Task Paborjad_visning_kan_inte_bokas()
    {
        var paborjad = await SkapaVisningAsync(DateTime.Now.AddMinutes(-10));

        var resultat = await NyTjanst().SkapaAsync(testDb.Kund.Id,
            new SkapaBokningDto { VisningId = paborjad.Id, Platser = [new PlatsVal { SateId = testDb.Sate(testDb.Salong, 1, 1) }] });

        Assert.Equal(FelTyp.Ogiltig, resultat.Fel);
    }

    private Bokning NyBokning(string nummer, string kundId, int sateId) => new()
    {
        Bokningsnummer = nummer,
        KundId = kundId,
        VisningId = testDb.Visning.Id,
        Platser = [new BokadPlats { VisningId = testDb.Visning.Id, SateId = sateId, Pris = 120 }]
    };

    /// <summary>Lägger in en visning direkt i databasen, förbi VisningTjanst, så att även dåtid går att testa.</summary>
    private async Task<Visning> SkapaVisningAsync(DateTime starttid)
    {
        await using var db = testDb.SkapaKontext();
        var visning = new Visning { FilmId = testDb.Film.Id, SalongId = testDb.Salong.Id, Starttid = starttid, Grundpris = 100 };
        db.Visningar.Add(visning);
        await db.SaveChangesAsync();
        return visning;
    }

    public void Dispose() => testDb.Dispose();
}
