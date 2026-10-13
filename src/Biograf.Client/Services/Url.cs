namespace Biograf.Client.Services;

public static class Url
{
    /// <summary>
    /// Bygger en adress med frågesträng och hoppar över tomma värden, t.ex.
    /// Url.Med("api/filmer", ("sok", "dune"), ("genre", null)) blir "api/filmer?sok=dune".
    /// </summary>
    public static string Med(string sokvag, params (string Namn, object? Varde)[] parametrar)
    {
        var delar = parametrar
            .Where(p => p.Varde is not null && p.Varde.ToString() != string.Empty)
            .Select(p => $"{p.Namn}={Uri.EscapeDataString(Formatera(p.Varde!))}")
            .ToList();

        return delar.Count == 0 ? sokvag : $"{sokvag}?{string.Join("&", delar)}";
    }

    private static string Formatera(object varde) => varde switch
    {
        DateTime d => d.ToString("yyyy-MM-dd"),
        bool b => b ? "true" : "false",
        _ => varde.ToString()!
    };
}
