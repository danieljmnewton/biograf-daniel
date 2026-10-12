namespace Biograf.Api.Models;

/// <summary>En film i biografens filmregister.</summary>
public class Film
{
    public int Id { get; set; }
    public string Titel { get; set; } = string.Empty;
    public string Genre { get; set; } = string.Empty;

    /// <summary>Svensk åldersgräns: 0 (barntillåten), 7, 11, 15 eller 18.</summary>
    public int Aldersgrans { get; set; }

    public int LangdMinuter { get; set; }
    public string Beskrivning { get; set; } = string.Empty;
    public string? PosterUrl { get; set; }

    public ICollection<Visning> Visningar { get; set; } = new List<Visning>();
}
