using System.Text.Json.Serialization;

namespace Biograf.Shared;

/// <summary>Biljettyp per plats. Skickas som text ("Barn") i JSON så att API:t blir läsbart.</summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum Biljettyp
{
    Vuxen,
    Barn,
    Pensionar
}

/// <summary>
/// Prisregler som används av både API och klient. Priset räknas bara här, så att
/// sammanfattningen i webbappen alltid visar samma pris som API:t sedan tar betalt.
/// </summary>
public static class Priser
{
    /// <summary>Tillägg i kronor för en VIP-plats.</summary>
    public const int VipTillagg = 50;

    /// <summary>Minuter mellan två visningar i samma salong för städning och insläpp.</summary>
    public const int StadtidMinuter = 20;

    public const int BarnRabatt = 40;
    public const int PensionarRabatt = 25;

    /// <summary>Barnbiljetter säljs inte till filmer med den här åldersgränsen eller högre.</summary>
    public const int BarnbiljettSparrFranAlder = 15;

    /// <summary>Pris för en plats: grundpris minus eventuell rabatt (lägst 0 kr), plus VIP-tillägg.</summary>
    public static int Berakna(int grundpris, bool arVip, Biljettyp biljettyp) =>
        Math.Max(0, grundpris - Rabatt(biljettyp)) + (arVip ? VipTillagg : 0);

    public static int Rabatt(Biljettyp biljettyp) => biljettyp switch
    {
        Biljettyp.Barn => BarnRabatt,
        Biljettyp.Pensionar => PensionarRabatt,
        _ => 0
    };

    public static bool BarnbiljettTillaten(int aldersgrans) => aldersgrans < BarnbiljettSparrFranAlder;

    public static string Namn(Biljettyp biljettyp) => biljettyp switch
    {
        Biljettyp.Barn => "Barn",
        Biljettyp.Pensionar => "Pensionär",
        _ => "Vuxen"
    };
}
