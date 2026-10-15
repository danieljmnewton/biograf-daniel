using Biograf.Shared;

namespace Biograf.Api.Models;

/// <summary>
/// Ett säte som ingår i en bokning. VisningId lagras även här så att databasen
/// kan garantera att ett säte bara är bokat en gång per visning.
/// </summary>
public class BokadPlats
{
    public int Id { get; set; }

    public int BokningId { get; set; }
    public Bokning Bokning { get; set; } = null!;

    public int VisningId { get; set; }

    public int SateId { get; set; }
    public Sate Sate { get; set; } = null!;

    public Biljettyp Biljettyp { get; set; } = Biljettyp.Vuxen;

    /// <summary>Priset vid bokningstillfället, så att historiken inte ändras om priserna ändras.</summary>
    public int Pris { get; set; }

    /// <summary>Sätts till false vid avbokning så att sätet blir ledigt igen.</summary>
    public bool Aktiv { get; set; } = true;
}
