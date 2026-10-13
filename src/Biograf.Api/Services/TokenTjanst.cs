using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Biograf.Api.Models;
using Microsoft.IdentityModel.Tokens;

namespace Biograf.Api.Services;

/// <summary>Inställningar för JWT, läses från sektionen "Jwt" i appsettings.json.</summary>
public class JwtInstallningar
{
    public string Utfardare { get; set; } = "Biograf.Api";
    public string Mottagare { get; set; } = "Biograf.Client";
    public string Nyckel { get; set; } = string.Empty;
    public int GiltigTimmar { get; set; } = 8;
}

/// <summary>Skapar signerade JWT-token med användarens id, namn och roller som claims.</summary>
public class TokenTjanst(JwtInstallningar installningar)
{
    public (string Token, DateTime GiltigTill) SkapaToken(Anvandare anvandare, IEnumerable<string> roller)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, anvandare.Id),
            new(JwtRegisteredClaimNames.Email, anvandare.Email ?? string.Empty),
            new(ClaimTypes.NameIdentifier, anvandare.Id),
            new(ClaimTypes.Name, anvandare.FulltNamn),
        };
        claims.AddRange(roller.Select(roll => new Claim(ClaimTypes.Role, roll)));

        var nyckel = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(installningar.Nyckel));
        var giltigTill = DateTime.UtcNow.AddHours(installningar.GiltigTimmar);

        var token = new JwtSecurityToken(
            issuer: installningar.Utfardare,
            audience: installningar.Mottagare,
            claims: claims,
            expires: giltigTill,
            signingCredentials: new SigningCredentials(nyckel, SecurityAlgorithms.HmacSha256));

        return (new JwtSecurityTokenHandler().WriteToken(token), giltigTill);
    }
}
