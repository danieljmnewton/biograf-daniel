using Biograf.Api.Models;
using Biograf.Api.Services;
using Biograf.Shared.Auth;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Biograf.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(UserManager<Anvandare> anvandarHanterare, TokenTjanst tokenTjanst) : ControllerBase
{
    /// <summary>Skapar ett nytt kundkonto och loggar in direkt.</summary>
    [HttpPost("registrera")]
    public async Task<ActionResult<InloggningSvar>> Registrera(RegistreraDto dto)
    {
        var anvandare = new Anvandare
        {
            UserName = dto.Email,
            Email = dto.Email,
            Fornamn = dto.Fornamn,
            Efternamn = dto.Efternamn
        };

        var resultat = await anvandarHanterare.CreateAsync(anvandare, dto.Losenord);
        if (!resultat.Succeeded)
        {
            foreach (var fel in resultat.Errors.Where(f => f.Description.Length > 0))
                ModelState.AddModelError(fel.Code, fel.Description);
            return ValidationProblem(ModelState);
        }

        return await SkapaSvarAsync(anvandare);
    }

    /// <summary>Loggar in med e-post och lösenord och returnerar en JWT.</summary>
    [HttpPost("logga-in")]
    public async Task<ActionResult<InloggningSvar>> LoggaIn(LoggaInDto dto)
    {
        var anvandare = await anvandarHanterare.FindByEmailAsync(dto.Email);

        // Samma felmeddelande oavsett om e-posten eller lösenordet är fel,
        // så att man inte kan ta reda på vilka konton som finns.
        if (anvandare is null || !await anvandarHanterare.CheckPasswordAsync(anvandare, dto.Losenord))
            return Unauthorized(new ProblemDetails { Title = "Fel e-post eller lösenord." });

        return await SkapaSvarAsync(anvandare);
    }

    private async Task<InloggningSvar> SkapaSvarAsync(Anvandare anvandare)
    {
        var roller = await anvandarHanterare.GetRolesAsync(anvandare);
        var (token, giltigTill) = tokenTjanst.SkapaToken(anvandare, roller);
        return new InloggningSvar(token, giltigTill, anvandare.Email!, anvandare.FulltNamn, roller.ToList());
    }
}
