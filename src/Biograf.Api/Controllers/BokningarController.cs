using System.Security.Claims;
using Biograf.Api.Common;
using Biograf.Api.Services;
using Biograf.Shared.Auth;
using Biograf.Shared.Bokningar;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Biograf.Api.Controllers;

[ApiController]
[Route("api/bokningar")]
[Authorize]
public class BokningarController(BokningTjanst bokningTjanst, BokningsLogg bokningsLogg) : ControllerBase
{
    private string AnvandarId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;
    private bool ArPersonal => User.IsInRole(Roller.Personal) || User.IsInRole(Roller.Admin);

    /// <summary>Säteskartan för en visning. Öppen för alla så att man kan se lediga platser innan inloggning.</summary>
    [AllowAnonymous]
    [HttpGet("~/api/visningar/{visningId:int}/saten")]
    public async Task<ActionResult<SateskartaDto>> HamtaSateskarta(int visningId) =>
        this.TillSvar(await bokningTjanst.HamtaSateskartaAsync(visningId));

    [HttpPost]
    public async Task<ActionResult<BokningDto>> Skapa(SkapaBokningDto dto)
    {
        var resultat = await bokningTjanst.SkapaAsync(AnvandarId, dto);
        return resultat.Lyckades
            ? CreatedAtAction(nameof(Hamta), new { id = resultat.Varde!.Id }, resultat.Varde)
            : this.TillSvar(resultat);
    }

    [HttpGet("mina")]
    public Task<List<BokningDto>> HamtaMina() => bokningTjanst.HamtaMinaAsync(AnvandarId);

    [HttpGet("{id:int}")]
    public async Task<ActionResult<BokningDto>> Hamta(int id) =>
        this.TillSvar(await bokningTjanst.HamtaAsync(id, AnvandarId, ArPersonal));

    [HttpPost("{id:int}/avboka")]
    public async Task<ActionResult<BokningDto>> Avboka(int id) =>
        this.TillSvar(await bokningTjanst.AvbokaAsync(id, AnvandarId, ArPersonal));

    /// <summary>Alla bokningar för en visning (personal).</summary>
    [HttpGet]
    [Authorize(Roles = Roller.PersonalEllerAdmin)]
    public Task<List<BokningDto>> HamtaForVisning([FromQuery] int visningId) =>
        bokningTjanst.HamtaForVisningAsync(visningId);

    /// <summary>De senaste bokningshändelserna (skapade och avbokade).</summary>
    [HttpGet("logg")]
    [Authorize(Roles = Roller.PersonalEllerAdmin)]
    public IReadOnlyList<BokningsHandelseDto> HamtaLogg() => bokningsLogg.HamtaSenaste();
}
