using Biograf.Api.Common;
using Biograf.Api.Data;
using Biograf.Api.Models;
using Biograf.Shared.Filmer;
using Microsoft.EntityFrameworkCore;

namespace Biograf.Api.Services;

public class FilmTjanst(BiografDbContext db)
{
    /// <summary>Hämtar filmer som matchar filtret, sorterade på titel.</summary>
    public async Task<List<FilmDto>> HamtaAllaAsync(FilmFilter filter)
    {
        var fraga = db.Filmer.AsNoTracking().AsQueryable();

        // Varje filter läggs bara på om det är ifyllt, så frågan byggs upp steg för steg.
        if (!string.IsNullOrWhiteSpace(filter.Sok))
            fraga = fraga.Where(f => EF.Functions.Like(f.Titel, $"%{filter.Sok.Trim()}%"));

        if (!string.IsNullOrWhiteSpace(filter.Genre))
            fraga = fraga.Where(f => f.Genre == filter.Genre);

        if (filter.MaxAldersgrans is int max)
            fraga = fraga.Where(f => f.Aldersgrans <= max);

        return await TillDto(fraga.OrderBy(f => f.Titel)).ToListAsync();
    }

    public async Task<Resultat<FilmDto>> HamtaAsync(int id)
    {
        var film = await TillDto(db.Filmer.AsNoTracking().Where(f => f.Id == id)).FirstOrDefaultAsync();
        return film is null ? Resultat<FilmDto>.HittadesInte($"Filmen med id {id} finns inte.") : Resultat<FilmDto>.Ok(film);
    }

    /// <summary>Alla genrer som förekommer i filmregistret, för filtreringslistor.</summary>
    public async Task<List<string>> HamtaGenrerAsync() =>
        await db.Filmer.Select(f => f.Genre).Distinct().OrderBy(g => g).ToListAsync();

    public async Task<Resultat<FilmDto>> SkapaAsync(SparaFilmDto dto)
    {
        var film = new Film();
        KopieraTill(dto, film);
        db.Filmer.Add(film);
        await db.SaveChangesAsync();
        return await HamtaAsync(film.Id);
    }

    public async Task<Resultat<FilmDto>> UppdateraAsync(int id, SparaFilmDto dto)
    {
        var film = await db.Filmer.FindAsync(id);
        if (film is null)
            return Resultat<FilmDto>.HittadesInte($"Filmen med id {id} finns inte.");

        KopieraTill(dto, film);
        await db.SaveChangesAsync();
        return await HamtaAsync(id);
    }

    public async Task<Resultat<Inget>> TaBortAsync(int id)
    {
        var film = await db.Filmer.Include(f => f.Visningar).FirstOrDefaultAsync(f => f.Id == id);
        if (film is null)
            return Resultat<Inget>.HittadesInte($"Filmen med id {id} finns inte.");

        if (film.Visningar.Any(v => v.Starttid > DateTime.Now))
            return Resultat<Inget>.Konflikt("Filmen har kommande visningar och kan inte tas bort.");

        if (film.Visningar.Count > 0)
            return Resultat<Inget>.Konflikt("Filmen har visats tidigare och finns kvar i bokningshistoriken.");

        db.Filmer.Remove(film);
        await db.SaveChangesAsync();
        return Resultat<Inget>.Ok(new Inget());
    }

    private IQueryable<FilmDto> TillDto(IQueryable<Film> filmer)
    {
        var nu = DateTime.Now;
        return filmer.Select(f => new FilmDto(
            f.Id, f.Titel, f.Genre, f.Aldersgrans, f.LangdMinuter, f.Beskrivning, f.PosterUrl,
            f.Visningar.Count(v => v.Starttid > nu)));
    }

    private static void KopieraTill(SparaFilmDto dto, Film film)
    {
        film.Titel = dto.Titel.Trim();
        film.Genre = dto.Genre.Trim();
        film.Aldersgrans = dto.Aldersgrans;
        film.LangdMinuter = dto.LangdMinuter;
        film.Beskrivning = dto.Beskrivning.Trim();
        film.PosterUrl = string.IsNullOrWhiteSpace(dto.PosterUrl) ? null : dto.PosterUrl.Trim();
    }
}
