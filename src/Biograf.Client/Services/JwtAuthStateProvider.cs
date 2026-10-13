using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Biograf.Shared.Auth;
using Microsoft.AspNetCore.Components.Authorization;

namespace Biograf.Client.Services;

/// <summary>
/// Talar om för Blazor vem som är inloggad genom att läsa claims ur den sparade JWT:n.
/// Signaturen kontrolleras inte här, det gör API:t vid varje anrop.
/// </summary>
public class JwtAuthStateProvider(TokenLagring tokenLagring) : AuthenticationStateProvider
{
    private static readonly AuthenticationState Anonym = new(new ClaimsPrincipal(new ClaimsIdentity()));

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var token = await tokenLagring.HamtaAsync();
        if (string.IsNullOrEmpty(token))
            return Anonym;

        var claims = LasClaims(token);
        var utgang = claims.FirstOrDefault(c => c.Type == "exp")?.Value;
        if (utgang is null || DateTimeOffset.FromUnixTimeSeconds(long.Parse(utgang)) < DateTimeOffset.UtcNow)
        {
            await tokenLagring.TaBortAsync();
            return Anonym;
        }

        var identitet = new ClaimsIdentity(claims, "jwt", ClaimTypes.Name, ClaimTypes.Role);
        return new AuthenticationState(new ClaimsPrincipal(identitet));
    }

    public async Task LoggaInAsync(InloggningSvar svar)
    {
        await tokenLagring.SparaAsync(svar.Token);
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    }

    public async Task LoggaUtAsync()
    {
        await tokenLagring.TaBortAsync();
        NotifyAuthenticationStateChanged(Task.FromResult(Anonym));
    }

    /// <summary>Avkodar JWT:ns mittdel (payload) och gör om varje fält till en claim.</summary>
    private static List<Claim> LasClaims(string token)
    {
        var delar = token.Split('.');
        if (delar.Length != 3)
            return [];

        var payload = delar[1].Replace('-', '+').Replace('_', '/');
        payload = payload.PadRight(payload.Length + (4 - payload.Length % 4) % 4, '=');

        using var json = JsonDocument.Parse(Encoding.UTF8.GetString(Convert.FromBase64String(payload)));
        var claims = new List<Claim>();
        foreach (var falt in json.RootElement.EnumerateObject())
        {
            // Har användaren flera roller kommer de som en array.
            if (falt.Value.ValueKind == JsonValueKind.Array)
                claims.AddRange(falt.Value.EnumerateArray().Select(v => new Claim(falt.Name, v.ToString())));
            else
                claims.Add(new Claim(falt.Name, falt.Value.ToString()));
        }
        return claims;
    }
}
