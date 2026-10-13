using System.Security.Claims;
using Biograf.Api.Data;
using Biograf.Api.Models;
using Biograf.Shared.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Biograf.Api.Controllers;

/// <summary>Användarhantering. Endast för admin.</summary>
[ApiController]
[Route("api/admin/anvandare")]
[Authorize(Roles = Roller.Admin)]
public class AdminController(BiografDbContext db, UserManager<Anvandare> anvandarHanterare) : ControllerBase
{
    /// <summary>Listar alla användare med deras roller, sorterat på roll och namn.</summary>
    [HttpGet]
    public async Task<IEnumerable<AnvandarDto>> HamtaAlla()
    {
        // Rollerna hämtas med en join mot Identity-tabellerna i samma fråga.
        var anvandare = await db.Users
            .Select(a => new
            {
                a.Id,
                a.Email,
                a.Fornamn,
                a.Efternamn,
                Roller = db.UserRoles
                    .Where(ar => ar.UserId == a.Id)
                    .Join(db.Roles, ar => ar.RoleId, r => r.Id, (ar, r) => r.Name!)
                    .ToList()
            })
            .ToListAsync();

        // Admin först, sedan personal och sist kunder.
        var rollordning = Roller.Alla.ToList();
        return anvandare
            .OrderBy(a => rollordning.IndexOf(a.Roller.FirstOrDefault() ?? Roller.Kund))
            .ThenBy(a => a.Fornamn)
            .Select(a => new AnvandarDto(a.Id, a.Email!, a.Fornamn, a.Efternamn, a.Roller));
    }

    /// <summary>Skapar ett personal- eller adminkonto.</summary>
    [HttpPost]
    public async Task<ActionResult<AnvandarDto>> Skapa(SkapaAnvandareDto dto)
    {
        if (!Roller.Alla.Contains(dto.Roll))
            return BadRequest(new ProblemDetails { Title = $"Okänd roll: {dto.Roll}" });

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

        await anvandarHanterare.AddToRoleAsync(anvandare, dto.Roll);

        return CreatedAtAction(nameof(HamtaAlla), null,
            new AnvandarDto(anvandare.Id, anvandare.Email, anvandare.Fornamn, anvandare.Efternamn, [dto.Roll]));
    }

    /// <summary>Byter roll på en användare. En användare har alltid exakt en roll.</summary>
    [HttpPut("{id}/roll")]
    public async Task<IActionResult> BytRoll(string id, BytRollDto dto)
    {
        if (!Roller.Alla.Contains(dto.Roll))
            return BadRequest(new ProblemDetails { Title = $"Okänd roll: {dto.Roll}" });

        var anvandare = await anvandarHanterare.FindByIdAsync(id);
        if (anvandare is null)
            return NotFound();

        // Skydd mot att admin råkar låsa ut sig själv.
        var egetId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (anvandare.Id == egetId && dto.Roll != Roller.Admin)
            return BadRequest(new ProblemDetails { Title = "Du kan inte ta bort din egen admin-roll." });

        var nuvarande = await anvandarHanterare.GetRolesAsync(anvandare);
        await anvandarHanterare.RemoveFromRolesAsync(anvandare, nuvarande);
        await anvandarHanterare.AddToRoleAsync(anvandare, dto.Roll);

        return NoContent();
    }
}
