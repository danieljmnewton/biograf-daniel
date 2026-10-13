using System.ComponentModel.DataAnnotations;

namespace Biograf.Shared.Visningar;

public record VisningDto(
    int Id,
    int FilmId,
    string FilmTitel,
    string Genre,
    int Aldersgrans,
    int LangdMinuter,
    int SalongId,
    string SalongNamn,
    DateTime Starttid,
    DateTime Sluttid,
    int Grundpris,
    int VipPris,
    int Kapacitet,
    int LedigaPlatser);

public class SparaVisningDto
{
    [Range(1, int.MaxValue, ErrorMessage = "Välj en film")]
    public int FilmId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Välj en salong")]
    public int SalongId { get; set; }

    [Required(ErrorMessage = "Starttid krävs")]
    public DateTime Starttid { get; set; } = DateTime.Today.AddDays(1).AddHours(19);

    [Range(0, 1000, ErrorMessage = "Priset måste vara 0–1000 kr")]
    public int Grundpris { get; set; } = 130;
}

/// <summary>Sökkriterier för visningar. Alla fält är valfria.</summary>
public class VisningFilter
{
    public DateTime? Datum { get; set; }
    public int? SalongId { get; set; }
    public int? FilmId { get; set; }
    public string? Genre { get; set; }

    /// <summary>Visar bara visningar som inte har börjat ännu (standard).</summary>
    public bool BaraKommande { get; set; } = true;
}
