using System.ComponentModel.DataAnnotations;

namespace Biograf.Shared.Filmer;

public record FilmDto(
    int Id,
    string Titel,
    string Genre,
    int Aldersgrans,
    int LangdMinuter,
    string Beskrivning,
    string? PosterUrl,
    int AntalKommandeVisningar);

/// <summary>Formulärdata när personal skapar eller ändrar en film.</summary>
public class SparaFilmDto
{
    [Required(ErrorMessage = "Titel krävs")]
    [StringLength(200)]
    public string Titel { get; set; } = string.Empty;

    [Required(ErrorMessage = "Genre krävs")]
    [StringLength(50)]
    public string Genre { get; set; } = string.Empty;

    [TillatnaVarden(0, 7, 11, 15, 18, ErrorMessage = "Åldersgränsen måste vara 0, 7, 11, 15 eller 18")]
    public int Aldersgrans { get; set; }

    [Range(1, 600, ErrorMessage = "Längden måste vara mellan 1 och 600 minuter")]
    public int LangdMinuter { get; set; } = 120;

    [StringLength(2000)]
    public string Beskrivning { get; set; } = string.Empty;

    [Url(ErrorMessage = "Ogiltig länk")]
    public string? PosterUrl { get; set; }
}

/// <summary>Sökkriterier för filmlistan. Alla fält är valfria.</summary>
public class FilmFilter
{
    public string? Sok { get; set; }
    public string? Genre { get; set; }
    public int? MaxAldersgrans { get; set; }
}
