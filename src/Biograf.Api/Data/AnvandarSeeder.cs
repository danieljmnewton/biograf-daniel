using Biograf.Api.Models;
using Biograf.Shared.Auth;
using Microsoft.AspNetCore.Identity;

namespace Biograf.Api.Data;

/// <summary>
/// Skapar rollerna och ett testkonto per roll vid uppstart.
/// Konton som redan finns lämnas orörda.
/// </summary>
public static class AnvandarSeeder
{
    public static async Task SeedAsync(RoleManager<IdentityRole> rollHanterare, UserManager<Anvandare> anvandarHanterare)
    {
        foreach (var roll in Roller.Alla)
        {
            if (!await rollHanterare.RoleExistsAsync(roll))
                await rollHanterare.CreateAsync(new IdentityRole(roll));
        }

        await SkapaOmSaknasAsync(anvandarHanterare, "admin@biograf.se", "Admin123", "Ada", "Admin", Roller.Admin);
        await SkapaOmSaknasAsync(anvandarHanterare, "personal@biograf.se", "Personal123", "Per", "Personal", Roller.Personal);
        await SkapaOmSaknasAsync(anvandarHanterare, "kund@biograf.se", "Kund123", "Klara", "Kund", Roller.Kund);
    }

    private static async Task SkapaOmSaknasAsync(
        UserManager<Anvandare> anvandarHanterare,
        string email, string losenord, string fornamn, string efternamn, string roll)
    {
        if (await anvandarHanterare.FindByEmailAsync(email) is not null)
            return;

        var anvandare = new Anvandare { UserName = email, Email = email, Fornamn = fornamn, Efternamn = efternamn };
        var resultat = await anvandarHanterare.CreateAsync(anvandare, losenord);
        if (!resultat.Succeeded)
            throw new InvalidOperationException(
                $"Kunde inte skapa {email}: {string.Join(", ", resultat.Errors.Select(e => e.Description))}");

        await anvandarHanterare.AddToRoleAsync(anvandare, roll);
    }
}
