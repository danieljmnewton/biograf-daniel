using Biograf.Api.Common;
using Biograf.Api.Services;
using Biograf.Shared.Auth;
using Biograf.Shared.Salonger;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Biograf.Api.Controllers;

[ApiController]
[Route("api/salonger")]
public class SalongerController(SalongTjanst salongTjanst) : ControllerBase
{
    [HttpGet]
    public Task<List<SalongDto>> HamtaAlla() => salongTjanst.HamtaAllaAsync();

    [HttpGet("{id:int}")]
    public async Task<ActionResult<SalongDto>> Hamta(int id) => this.TillSvar(await salongTjanst.HamtaAsync(id));

    [HttpPost]
    [Authorize(Roles = Roller.PersonalEllerAdmin)]
    public async Task<ActionResult<SalongDto>> Skapa(SparaSalongDto dto)
    {
        var resultat = await salongTjanst.SkapaAsync(dto);
        return resultat.Lyckades
            ? CreatedAtAction(nameof(Hamta), new { id = resultat.Varde!.Id }, resultat.Varde)
            : this.TillSvar(resultat);
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = Roller.PersonalEllerAdmin)]
    public async Task<ActionResult<SalongDto>> Uppdatera(int id, SparaSalongDto dto) =>
        this.TillSvar(await salongTjanst.UppdateraAsync(id, dto));

    [HttpDelete("{id:int}")]
    [Authorize(Roles = Roller.PersonalEllerAdmin)]
    public async Task<ActionResult<Inget>> TaBort(int id) => this.TillSvar(await salongTjanst.TaBortAsync(id));
}
