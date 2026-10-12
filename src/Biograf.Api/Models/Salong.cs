using System.ComponentModel.DataAnnotations.Schema;

namespace Biograf.Api.Models;

/// <summary>En biografsalong. Kapaciteten räknas fram från salongens säten.</summary>
public class Salong
{
    public int Id { get; set; }
    public string Namn { get; set; } = string.Empty;

    public ICollection<Sate> Saten { get; set; } = new List<Sate>();
    public ICollection<Visning> Visningar { get; set; } = new List<Visning>();

    [NotMapped]
    public int Kapacitet => Saten.Count;
}
