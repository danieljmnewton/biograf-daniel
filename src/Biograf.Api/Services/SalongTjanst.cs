using Biograf.Api.Common;
using Biograf.Api.Data;
using Biograf.Api.Models;
using Biograf.Shared.Salonger;
using Microsoft.EntityFrameworkCore;

namespace Biograf.Api.Services;

public class SalongTjanst(BiografDbContext db)
{
    public async Task<List<SalongDto>> HamtaAllaAsync()
    {
        var salonger = await db.Salonger.AsNoTracking()
            .Include(s => s.Saten)
            .Include(s => s.Visningar)
            .OrderBy(s => s.Namn)
            .ToListAsync();

        return salonger.Select(TillDto).ToList();
    }

    public async Task<Resultat<SalongDto>> HamtaAsync(int id)
    {
        var salong = await db.Salonger.AsNoTracking()
            .Include(s => s.Saten)
            .Include(s => s.Visningar)
            .FirstOrDefaultAsync(s => s.Id == id);

        return salong is null
            ? Resultat<SalongDto>.HittadesInte($"Salongen med id {id} finns inte.")
            : Resultat<SalongDto>.Ok(TillDto(salong));
    }

    public async Task<Resultat<SalongDto>> SkapaAsync(SparaSalongDto dto)
    {
        if (dto.VipRader > dto.Rader)
            return Resultat<SalongDto>.Ogiltig("Det kan inte finnas fler VIP-rader än rader.");

        if (await NamnetUpptagetAsync(dto.Namn))
            return Resultat<SalongDto>.Konflikt($"Det finns redan en salong som heter {dto.Namn}.");

        var salong = new Salong { Namn = dto.Namn.Trim() };
        salong.GenereraSaten(dto.Rader, dto.PlatserPerRad, dto.VipRader);
        db.Salonger.Add(salong);
        await db.SaveChangesAsync();

        return await HamtaAsync(salong.Id);
    }

    /// <summary>
    /// Ändrar namn och/eller platslayout. Layouten får bara ändras om salongen
    /// aldrig har haft några visningar, eftersom bokningar pekar på enskilda säten.
    /// </summary>
    public async Task<Resultat<SalongDto>> UppdateraAsync(int id, SparaSalongDto dto)
    {
        var salong = await db.Salonger
            .Include(s => s.Saten)
            .Include(s => s.Visningar)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (salong is null)
            return Resultat<SalongDto>.HittadesInte($"Salongen med id {id} finns inte.");

        if (dto.VipRader > dto.Rader)
            return Resultat<SalongDto>.Ogiltig("Det kan inte finnas fler VIP-rader än rader.");

        if (await NamnetUpptagetAsync(dto.Namn, id))
            return Resultat<SalongDto>.Konflikt($"Det finns redan en salong som heter {dto.Namn}.");

        var nu = TillDto(salong);
        var layoutAndrad = nu.Rader != dto.Rader || nu.PlatserPerRad != dto.PlatserPerRad || nu.VipRader != dto.VipRader;

        if (layoutAndrad)
        {
            if (salong.Visningar.Count > 0)
                return Resultat<SalongDto>.Konflikt("Platslayouten kan inte ändras när salongen har visningar. Bara namnet kan ändras.");

            db.Saten.RemoveRange(salong.Saten);
            salong.GenereraSaten(dto.Rader, dto.PlatserPerRad, dto.VipRader);
        }

        salong.Namn = dto.Namn.Trim();
        await db.SaveChangesAsync();
        return await HamtaAsync(id);
    }

    public async Task<Resultat<Inget>> TaBortAsync(int id)
    {
        var salong = await db.Salonger.Include(s => s.Visningar).FirstOrDefaultAsync(s => s.Id == id);
        if (salong is null)
            return Resultat<Inget>.HittadesInte($"Salongen med id {id} finns inte.");

        if (salong.Visningar.Count > 0)
            return Resultat<Inget>.Konflikt("Salongen har visningar och kan inte tas bort.");

        db.Salonger.Remove(salong);
        await db.SaveChangesAsync();
        return Resultat<Inget>.Ok(new Inget());
    }

    private Task<bool> NamnetUpptagetAsync(string namn, int? utomId = null) =>
        db.Salonger.AnyAsync(s => s.Namn == namn.Trim() && s.Id != utomId);

    /// <summary>Räknar fram layouten (rader, platser, VIP-rader) från salongens säten.</summary>
    private static SalongDto TillDto(Salong s) => new(
        s.Id,
        s.Namn,
        s.Kapacitet,
        Rader: s.Saten.Select(x => x.Rad).DefaultIfEmpty().Max(),
        PlatserPerRad: s.Saten.Select(x => x.Nummer).DefaultIfEmpty().Max(),
        VipRader: s.Saten.Where(x => x.Typ == SateTyp.VIP).Select(x => x.Rad).Distinct().Count(),
        AntalKommandeVisningar: s.Visningar.Count(v => v.Starttid > DateTime.Now));
}
