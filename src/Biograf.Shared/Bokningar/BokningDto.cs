using System.ComponentModel.DataAnnotations;

namespace Biograf.Shared.Bokningar;

public class SkapaBokningDto
{
    [Range(1, int.MaxValue)]
    public int VisningId { get; set; }

    [MinLength(1, ErrorMessage = "Välj minst en plats")]
    [MaxLength(10, ErrorMessage = "Du kan boka högst 10 platser åt gången")]
    public List<int> SateIds { get; set; } = [];
}

public record BokadPlatsDto(int Rad, int Nummer, string Typ, int Pris);

public record BokningDto(
    int Id,
    string Bokningsnummer,
    int VisningId,
    string FilmTitel,
    string SalongNamn,
    DateTime Starttid,
    string Status,
    int Totalpris,
    DateTime Skapad,
    bool KanAvbokas,
    string KundNamn,
    string KundEmail,
    IReadOnlyList<BokadPlatsDto> Platser);

/// <summary>Ett säte i säteskartan för en viss visning.</summary>
public record SateStatusDto(int Id, int Rad, int Nummer, string Typ, bool Upptagen);

public record SateskartaDto(
    int VisningId,
    string FilmTitel,
    string SalongNamn,
    DateTime Starttid,
    int Grundpris,
    int VipPris,
    IReadOnlyList<SateStatusDto> Saten);

/// <summary>En händelse i bokningsloggen, t.ex. "Bokning NB-7K2Q9 skapad".</summary>
public record BokningsHandelseDto(DateTime Tid, string Typ, string Bokningsnummer, string Beskrivning);

public static class BokningsStatus
{
    public const string Aktiv = "Aktiv";
    public const string Avbokad = "Avbokad";
    public const string Genomford = "Genomförd";

    /// <summary>Hur långt före visningen man senast kan avboka.</summary>
    public const int AvbokningsgransTimmar = 2;
}
