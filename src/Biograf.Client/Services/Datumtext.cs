namespace Biograf.Client.Services;

/// <summary>
/// Svensk datumformatering. Skriven för hand så att klienten inte behöver ladda
/// hela globaliseringsdatan för sv-SE.
/// </summary>
public static class Datumtext
{
    private static readonly string[] Veckodagar = ["sön", "mån", "tis", "ons", "tor", "fre", "lör"];
    private static readonly string[] Manader = ["jan", "feb", "mar", "apr", "maj", "jun", "jul", "aug", "sep", "okt", "nov", "dec"];

    /// <summary>"Idag", "Imorgon" eller t.ex. "Ons 14 okt".</summary>
    public static string Dag(DateTime datum)
    {
        var skillnad = (datum.Date - DateTime.Today).Days;
        return skillnad switch
        {
            0 => "Idag",
            1 => "Imorgon",
            _ => $"{Stor(Veckodagar[(int)datum.DayOfWeek])} {datum.Day} {Manader[datum.Month - 1]}"
        };
    }

    /// <summary>T.ex. "Ons 14 okt 19:30".</summary>
    public static string DagOchTid(DateTime datum) => $"{Dag(datum)} {datum:HH:mm}";

    /// <summary>T.ex. "2 h 46 min".</summary>
    public static string Langd(int minuter) =>
        minuter >= 60 ? $"{minuter / 60} h {minuter % 60:00} min" : $"{minuter} min";

    public static string Aldersgrans(int alder) => alder == 0 ? "Btl" : $"{alder} år";

    private static string Stor(string text) => char.ToUpper(text[0]) + text[1..];
}
