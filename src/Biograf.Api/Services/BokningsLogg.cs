using System.Collections.Concurrent;
using Biograf.Shared.Bokningar;

namespace Biograf.Api.Services;

/// <summary>
/// Håller de senaste bokningshändelserna i minnet så att admin kan se aktiviteten.
/// Fylls på via eventen BokningSkapad och BokningAvbokad i BokningTjanst.
/// </summary>
public class BokningsLogg(ILogger<BokningsLogg> logger)
{
    private const int MaxAntal = 50;
    private readonly ConcurrentQueue<BokningsHandelseDto> handelser = new();

    /// <summary>Kopplar loggen till en BokningTjanst-instans via dess events.</summary>
    public void Prenumerera(BokningTjanst tjanst)
    {
        tjanst.BokningSkapad += (_, e) => LaggTill("Skapad", e.Bokning.Bokningsnummer,
            $"{e.Bokning.Platser.Count} plats(er), {e.Bokning.Totalpris} kr");

        tjanst.BokningAvbokad += (_, e) => LaggTill("Avbokad", e.Bokning.Bokningsnummer,
            $"{e.Bokning.Platser.Count} plats(er) frigjorda");
    }

    public IReadOnlyList<BokningsHandelseDto> HamtaSenaste() => handelser.Reverse().ToList();

    private void LaggTill(string typ, string bokningsnummer, string beskrivning)
    {
        handelser.Enqueue(new BokningsHandelseDto(DateTime.Now, typ, bokningsnummer, beskrivning));
        while (handelser.Count > MaxAntal)
            handelser.TryDequeue(out _);

        logger.LogInformation("Bokning {Bokningsnummer} {Typ}: {Beskrivning}", bokningsnummer, typ.ToLower(), beskrivning);
    }
}
