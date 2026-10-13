using Biograf.Api.Common;
using Biograf.Api.Data;
using Biograf.Api.Models;
using Biograf.Shared;
using Biograf.Shared.Visningar;
using Microsoft.EntityFrameworkCore;

namespace Biograf.Api.Services;

public class VisningTjanst(BiografDbContext db)
{
    /// <summary>Hämtar visningar som matchar filtret, sorterade på starttid.</summary>
    public async Task<List<VisningDto>> HamtaAllaAsync(VisningFilter filter)
    {
        var fraga = db.Visningar.AsNoTracking().AsQueryable();

        if (filter.BaraKommande)
            fraga = fraga.Where(v => v.Starttid > DateTime.Now);

        if (filter.Datum is DateTime datum)
            fraga = fraga.Where(v => v.Starttid >= datum.Date && v.Starttid < datum.Date.AddDays(1));

        if (filter.SalongId is int salongId)
            fraga = fraga.Where(v => v.SalongId == salongId);

        if (filter.FilmId is int filmId)
            fraga = fraga.Where(v => v.FilmId == filmId);

        if (!string.IsNullOrWhiteSpace(filter.Genre))
            fraga = fraga.Where(v => v.Film.Genre == filter.Genre);

        return await TillDto(fraga.OrderBy(v => v.Starttid)).ToListAsync();
    }

    public async Task<Resultat<VisningDto>> HamtaAsync(int id)
    {
        var visning = await TillDto(db.Visningar.AsNoTracking().Where(v => v.Id == id)).FirstOrDefaultAsync();
        return visning is null
            ? Resultat<VisningDto>.HittadesInte($"Visningen med id {id} finns inte.")
            : Resultat<VisningDto>.Ok(visning);
    }

    public async Task<Resultat<VisningDto>> SkapaAsync(SparaVisningDto dto)
    {
        var kontroll = await KontrolleraAsync(dto, utomVisningId: null);
        if (!kontroll.Lyckades)
            return kontroll.FelSom<VisningDto>();

        var visning = new Visning
        {
            FilmId = dto.FilmId,
            SalongId = dto.SalongId,
            Starttid = dto.Starttid,
            Grundpris = dto.Grundpris
        };
        db.Visningar.Add(visning);
        await db.SaveChangesAsync();
        return await HamtaAsync(visning.Id);
    }

    public async Task<Resultat<VisningDto>> UppdateraAsync(int id, SparaVisningDto dto)
    {
        var visning = await db.Visningar.FindAsync(id);
        if (visning is null)
            return Resultat<VisningDto>.HittadesInte($"Visningen med id {id} finns inte.");

        // Bokningarna gäller specifika säten i salongen, så salongen kan inte bytas efteråt.
        if (visning.SalongId != dto.SalongId && await HarAktivaBokningarAsync(id))
            return Resultat<VisningDto>.Konflikt("Visningen har bokningar och kan inte flyttas till en annan salong.");

        var kontroll = await KontrolleraAsync(dto, utomVisningId: id);
        if (!kontroll.Lyckades)
            return kontroll.FelSom<VisningDto>();

        visning.FilmId = dto.FilmId;
        visning.SalongId = dto.SalongId;
        visning.Starttid = dto.Starttid;
        visning.Grundpris = dto.Grundpris;
        await db.SaveChangesAsync();
        return await HamtaAsync(id);
    }

    public async Task<Resultat<Inget>> TaBortAsync(int id)
    {
        var visning = await db.Visningar.FindAsync(id);
        if (visning is null)
            return Resultat<Inget>.HittadesInte($"Visningen med id {id} finns inte.");

        if (await db.Bokningar.AnyAsync(b => b.VisningId == id))
            return Resultat<Inget>.Konflikt("Visningen har bokningar och kan inte tas bort.");

        db.Visningar.Remove(visning);
        await db.SaveChangesAsync();
        return Resultat<Inget>.Ok(new Inget());
    }

    /// <summary>
    /// Kontrollerar att film och salong finns, att tiden inte har passerat och att
    /// visningen inte krockar med en annan visning i samma salong.
    /// </summary>
    private async Task<Resultat<Inget>> KontrolleraAsync(SparaVisningDto dto, int? utomVisningId)
    {
        var film = await db.Filmer.FindAsync(dto.FilmId);
        if (film is null)
            return Resultat<Inget>.Ogiltig("Filmen finns inte.");

        if (!await db.Salonger.AnyAsync(s => s.Id == dto.SalongId))
            return Resultat<Inget>.Ogiltig("Salongen finns inte.");

        if (dto.Starttid <= DateTime.Now)
            return Resultat<Inget>.Ogiltig("Starttiden har redan passerat.");

        var nyStart = dto.Starttid;
        var nySlut = nyStart.AddMinutes(film.LangdMinuter + Priser.StadtidMinuter);

        // Hämtar visningar i samma salong som börjar inom ett rimligt tidsfönster
        // och kontrollerar sedan överlappet i minnet med filmernas faktiska längd.
        var narliggande = await db.Visningar
            .Include(v => v.Film)
            .Where(v => v.SalongId == dto.SalongId && v.Id != utomVisningId)
            .Where(v => v.Starttid < nySlut && v.Starttid > nyStart.AddHours(-12))
            .ToListAsync();

        Func<Visning, bool> overlappar = v =>
            v.Starttid < nySlut &&
            v.Starttid.AddMinutes(v.Film.LangdMinuter + Priser.StadtidMinuter) > nyStart;

        var krock = narliggande.FirstOrDefault(overlappar);
        return krock is null
            ? Resultat<Inget>.Ok(new Inget())
            : Resultat<Inget>.Konflikt(
                $"Salongen är upptagen: {krock.Film.Titel} visas {krock.Starttid:HH:mm} " +
                $"till {krock.Starttid.AddMinutes(krock.Film.LangdMinuter):HH:mm} (+{Priser.StadtidMinuter} min städning).");
    }

    private Task<bool> HarAktivaBokningarAsync(int visningId) =>
        db.BokadePlatser.AnyAsync(p => p.VisningId == visningId && p.Aktiv);

    private IQueryable<VisningDto> TillDto(IQueryable<Visning> visningar) =>
        visningar.Select(v => new VisningDto(
            v.Id,
            v.FilmId,
            v.Film.Titel,
            v.Film.Genre,
            v.Film.Aldersgrans,
            v.Film.LangdMinuter,
            v.SalongId,
            v.Salong.Namn,
            v.Starttid,
            v.Starttid.AddMinutes(v.Film.LangdMinuter),
            v.Grundpris,
            v.Grundpris + Priser.VipTillagg,
            v.Salong.Saten.Count,
            v.Salong.Saten.Count - db.BokadePlatser.Count(p => p.VisningId == v.Id && p.Aktiv)));
}
