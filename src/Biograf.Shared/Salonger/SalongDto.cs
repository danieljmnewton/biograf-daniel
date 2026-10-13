using System.ComponentModel.DataAnnotations;

namespace Biograf.Shared.Salonger;

public record SalongDto(
    int Id,
    string Namn,
    int Kapacitet,
    int Rader,
    int PlatserPerRad,
    int VipRader,
    int AntalKommandeVisningar);

/// <summary>
/// Formulärdata för en salong. Sätena genereras utifrån rader och platser per rad,
/// där de bakersta raderna blir VIP.
/// </summary>
public class SparaSalongDto
{
    [Required(ErrorMessage = "Namn krävs")]
    [StringLength(100)]
    public string Namn { get; set; } = string.Empty;

    [Range(1, 30, ErrorMessage = "Antal rader måste vara 1–30")]
    public int Rader { get; set; } = 8;

    [Range(1, 40, ErrorMessage = "Platser per rad måste vara 1–40")]
    public int PlatserPerRad { get; set; } = 12;

    [Range(0, 30, ErrorMessage = "Antal VIP-rader måste vara 0–30")]
    public int VipRader { get; set; } = 1;
}
