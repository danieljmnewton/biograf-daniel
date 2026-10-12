namespace Biograf.Api.Models;

/// <summary>En visning av en film i en salong vid en viss tid.</summary>
public class Visning
{
    public int Id { get; set; }

    public int FilmId { get; set; }
    public Film Film { get; set; } = null!;

    public int SalongId { get; set; }
    public Salong Salong { get; set; } = null!;

    public DateTime Starttid { get; set; }

    /// <summary>Pris i hela kronor för ett standardsäte. VIP-säten får ett tillägg.</summary>
    public int Grundpris { get; set; }

    public ICollection<Bokning> Bokningar { get; set; } = new List<Bokning>();
}
