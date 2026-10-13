using Microsoft.AspNetCore.Identity;

namespace Biograf.Api.Models;

/// <summary>
/// En inloggningsbar användare. Bygger på ASP.NET Core Identity, som sköter
/// lösenordshashning, e-post och roller.
/// </summary>
public class Anvandare : IdentityUser
{
    public string Fornamn { get; set; } = string.Empty;
    public string Efternamn { get; set; } = string.Empty;
    public DateTime Skapad { get; set; } = DateTime.UtcNow;

    public ICollection<Bokning> Bokningar { get; set; } = new List<Bokning>();

    public string FulltNamn => $"{Fornamn} {Efternamn}".Trim();
}
