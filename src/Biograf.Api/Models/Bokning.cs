namespace Biograf.Api.Models;

public enum BokningsStatus
{
    Aktiv,
    Avbokad
}

/// <summary>En kunds bokning av en eller flera platser på en visning.</summary>
public class Bokning
{
    public int Id { get; set; }

    /// <summary>Kort kod som visas för kunden, t.ex. "NB-7K2Q9".</summary>
    public string Bokningsnummer { get; set; } = string.Empty;

    public string KundId { get; set; } = string.Empty;
    public Anvandare Kund { get; set; } = null!;

    public int VisningId { get; set; }
    public Visning Visning { get; set; } = null!;

    public DateTime Skapad { get; set; } = DateTime.UtcNow;
    public BokningsStatus Status { get; set; } = BokningsStatus.Aktiv;
    public int Totalpris { get; set; }

    public ICollection<BokadPlats> Platser { get; set; } = new List<BokadPlats>();
}
