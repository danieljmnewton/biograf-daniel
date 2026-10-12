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

    /// <summary>
    /// Skapar salongens säten rad för rad. De bakersta raderna blir VIP-platser.
    /// </summary>
    public void GenereraSaten(int rader, int platserPerRad, int vipRader)
    {
        if (rader < 1 || platserPerRad < 1)
            throw new ArgumentException("En salong måste ha minst en rad och en plats per rad.");
        if (vipRader < 0 || vipRader > rader)
            throw new ArgumentException("Antal VIP-rader måste vara mellan 0 och antal rader.");

        Saten.Clear();
        for (var rad = 1; rad <= rader; rad++)
        {
            var typ = rad > rader - vipRader ? SateTyp.VIP : SateTyp.Standard;
            for (var nummer = 1; nummer <= platserPerRad; nummer++)
            {
                Saten.Add(new Sate { Rad = rad, Nummer = nummer, Typ = typ });
            }
        }
    }
}
