using System.Security.Cryptography;
using Biograf.Api.Common;
using Biograf.Api.Data;
using Biograf.Api.Models;
using Biograf.Shared;
using Biograf.Shared.Bokningar;
using Microsoft.EntityFrameworkCore;

namespace Biograf.Api.Services;

/// <summary>Information som skickas med när en bokning skapas eller avbokas.</summary>
public class BokningEventArgs(Bokning bokning) : EventArgs
{
    public Bokning Bokning { get; } = bokning;
}

public class BokningTjanst(BiografDbContext db)
{
    /// <summary>Utlöses efter att en bokning har sparats.</summary>
    public event EventHandler<BokningEventArgs>? BokningSkapad;

    /// <summary>Utlöses efter att en bokning har avbokats.</summary>
    public event EventHandler<BokningEventArgs>? BokningAvbokad;

    /// <summary>Hämtar alla säten i visningens salong och markerar vilka som är upptagna.</summary>
    public async Task<Resultat<SateskartaDto>> HamtaSateskartaAsync(int visningId)
    {
        var visning = await db.Visningar.AsNoTracking()
            .Include(v => v.Film)
            .Include(v => v.Salong).ThenInclude(s => s.Saten)
            .FirstOrDefaultAsync(v => v.Id == visningId);

        if (visning is null)
            return Resultat<SateskartaDto>.HittadesInte($"Visningen med id {visningId} finns inte.");

        var upptagna = (await db.BokadePlatser
            .Where(p => p.VisningId == visningId && p.Aktiv)
            .Select(p => p.SateId)
            .ToListAsync()).ToHashSet();

        var saten = visning.Salong.Saten
            .OrderBy(s => s.Rad).ThenBy(s => s.Nummer)
            .Select(s => new SateStatusDto(s.Id, s.Rad, s.Nummer, s.Typ.ToString(), upptagna.Contains(s.Id)))
            .ToList();

        return Resultat<SateskartaDto>.Ok(new SateskartaDto(
            visning.Id, visning.Film.Titel, visning.Salong.Namn, visning.Starttid,
            visning.Grundpris, visning.Grundpris + Priser.VipTillagg, saten));
    }

    /// <summary>
    /// Bokar de valda sätena. Allt sker i en transaktion, och databasens unika index
    /// stoppar två samtidiga bokningar av samma säte.
    /// </summary>
    public async Task<Resultat<BokningDto>> SkapaAsync(string kundId, SkapaBokningDto dto)
    {
        var sateIds = dto.SateIds.Distinct().ToList();
        if (sateIds.Count == 0)
            return Resultat<BokningDto>.Ogiltig("Välj minst en plats.");

        var visning = await db.Visningar.FindAsync(dto.VisningId);
        if (visning is null)
            return Resultat<BokningDto>.HittadesInte("Visningen finns inte.");

        if (visning.Starttid <= DateTime.Now)
            return Resultat<BokningDto>.Ogiltig("Visningen har redan börjat.");

        var saten = await db.Saten
            .Where(s => sateIds.Contains(s.Id) && s.SalongId == visning.SalongId)
            .ToListAsync();

        if (saten.Count != sateIds.Count)
            return Resultat<BokningDto>.Ogiltig("En eller flera platser finns inte i visningens salong.");

        await using var transaktion = await db.Database.BeginTransactionAsync();

        var redanBokade = await db.BokadePlatser
            .Where(p => p.VisningId == visning.Id && p.Aktiv && sateIds.Contains(p.SateId))
            .Select(p => p.Sate)
            .ToListAsync();

        if (redanBokade.Count > 0)
            return Resultat<BokningDto>.Konflikt(
                "Platserna är redan bokade: " + string.Join(", ", redanBokade.Select(s => $"rad {s.Rad} plats {s.Nummer}")));

        var bokning = new Bokning
        {
            Bokningsnummer = await SkapaUniktBokningsnummerAsync(),
            KundId = kundId,
            VisningId = visning.Id,
            Skapad = DateTime.Now,
            Platser = saten.Select(s => new BokadPlats
            {
                VisningId = visning.Id,
                SateId = s.Id,
                Pris = PrisFor(s, visning)
            }).ToList()
        };
        bokning.Totalpris = bokning.Platser.Sum(p => p.Pris);

        db.Bokningar.Add(bokning);
        try
        {
            await db.SaveChangesAsync();
            await transaktion.CommitAsync();
        }
        catch (DbUpdateException)
        {
            // Någon annan hann boka samma säte mellan kontrollen och sparningen.
            return Resultat<BokningDto>.Konflikt("Någon annan hann boka en av platserna. Välj andra platser och försök igen.");
        }

        BokningSkapad?.Invoke(this, new BokningEventArgs(bokning));
        return await HamtaAsync(bokning.Id, kundId, arPersonal: false);
    }

    /// <summary>Hämtar en bokning. Kunder får bara se sina egna bokningar.</summary>
    public async Task<Resultat<BokningDto>> HamtaAsync(int id, string anvandarId, bool arPersonal)
    {
        var bokning = await FragaMedDetaljer().FirstOrDefaultAsync(b => b.Id == id);
        if (bokning is null || (!arPersonal && bokning.KundId != anvandarId))
            return Resultat<BokningDto>.HittadesInte("Bokningen finns inte.");

        return Resultat<BokningDto>.Ok(TillDto(bokning));
    }

    /// <summary>Den inloggade kundens bokningar, kommande först och sedan tidigare.</summary>
    public async Task<List<BokningDto>> HamtaMinaAsync(string kundId)
    {
        var bokningar = await FragaMedDetaljer().Where(b => b.KundId == kundId).ToListAsync();
        var nu = DateTime.Now;

        return bokningar
            .OrderBy(b => b.Visning.Starttid < nu)
            .ThenBy(b => b.Visning.Starttid < nu ? -b.Visning.Starttid.Ticks : b.Visning.Starttid.Ticks)
            .Select(TillDto)
            .ToList();
    }

    /// <summary>Alla bokningar för en visning, för personalen.</summary>
    public async Task<List<BokningDto>> HamtaForVisningAsync(int visningId) =>
        (await FragaMedDetaljer().Where(b => b.VisningId == visningId).ToListAsync())
            .OrderBy(b => b.Status)
            .ThenBy(b => b.Skapad)
            .Select(TillDto)
            .ToList();

    /// <summary>
    /// Avbokar en bokning. Kunden kan avboka sina egna fram till två timmar före visningen.
    /// Personal kan avboka vilken bokning som helst innan visningen har börjat.
    /// </summary>
    public async Task<Resultat<BokningDto>> AvbokaAsync(int id, string anvandarId, bool arPersonal)
    {
        var bokning = await db.Bokningar
            .Include(b => b.Visning)
            .Include(b => b.Platser)
            .FirstOrDefaultAsync(b => b.Id == id);

        if (bokning is null || (!arPersonal && bokning.KundId != anvandarId))
            return Resultat<BokningDto>.HittadesInte("Bokningen finns inte.");

        if (bokning.Status == BokningsStatus.Avbokad)
            return Resultat<BokningDto>.Konflikt("Bokningen är redan avbokad.");

        var grans = arPersonal ? bokning.Visning.Starttid : SenastAvbokning(bokning.Visning.Starttid);
        if (DateTime.Now > grans)
            return Resultat<BokningDto>.Ogiltig(arPersonal
                ? "Visningen har redan börjat."
                : $"Det går bara att avboka fram till {Bokningsregler.AvbokningsgransTimmar} timmar före visningen.");

        bokning.Status = BokningsStatus.Avbokad;
        foreach (var plats in bokning.Platser)
            plats.Aktiv = false;

        await db.SaveChangesAsync();

        BokningAvbokad?.Invoke(this, new BokningEventArgs(bokning));
        return await HamtaAsync(id, anvandarId, arPersonal);
    }

    /// <summary>VIP-platser kostar grundpriset plus ett fast tillägg.</summary>
    public static int PrisFor(Sate sate, Visning visning) =>
        sate.Typ == SateTyp.VIP ? visning.Grundpris + Priser.VipTillagg : visning.Grundpris;

    public static DateTime SenastAvbokning(DateTime starttid) =>
        starttid.AddHours(-Bokningsregler.AvbokningsgransTimmar);

    private IQueryable<Bokning> FragaMedDetaljer() =>
        db.Bokningar.AsNoTracking()
            .Include(b => b.Kund)
            .Include(b => b.Visning).ThenInclude(v => v.Film)
            .Include(b => b.Visning).ThenInclude(v => v.Salong)
            .Include(b => b.Platser).ThenInclude(p => p.Sate);

    private static BokningDto TillDto(Bokning b)
    {
        var nu = DateTime.Now;
        var status = b.Status == BokningsStatus.Avbokad ? Bokningsregler.Avbokad
            : b.Visning.Starttid < nu ? Bokningsregler.Genomford
            : Bokningsregler.Aktiv;

        return new BokningDto(
            b.Id,
            b.Bokningsnummer,
            b.VisningId,
            b.Visning.Film.Titel,
            b.Visning.Salong.Namn,
            b.Visning.Starttid,
            status,
            b.Totalpris,
            b.Skapad,
            KanAvbokas: status == Bokningsregler.Aktiv && nu <= SenastAvbokning(b.Visning.Starttid),
            b.Kund.FulltNamn,
            b.Kund.Email ?? string.Empty,
            b.Platser
                .OrderBy(p => p.Sate.Rad).ThenBy(p => p.Sate.Nummer)
                .Select(p => new BokadPlatsDto(p.Sate.Rad, p.Sate.Nummer, p.Sate.Typ.ToString(), p.Pris))
                .ToList());
    }

    /// <summary>Skapar en kort kod som "NB-7K2Q9" utan tecken som är lätta att förväxla (0/O, 1/I).</summary>
    private async Task<string> SkapaUniktBokningsnummerAsync()
    {
        const string tecken = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        while (true)
        {
            var kod = "NB-" + new string(Enumerable.Range(0, 5)
                .Select(_ => tecken[RandomNumberGenerator.GetInt32(tecken.Length)])
                .ToArray());

            if (!await db.Bokningar.AnyAsync(b => b.Bokningsnummer == kod))
                return kod;
        }
    }
}
