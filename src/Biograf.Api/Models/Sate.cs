namespace Biograf.Api.Models;

public enum SateTyp
{
    Standard,
    VIP
}

/// <summary>En fysisk plats i en salong, identifierad av rad och nummer.</summary>
public class Sate
{
    public int Id { get; set; }
    public int SalongId { get; set; }
    public Salong Salong { get; set; } = null!;

    public int Rad { get; set; }
    public int Nummer { get; set; }
    public SateTyp Typ { get; set; } = SateTyp.Standard;
}
