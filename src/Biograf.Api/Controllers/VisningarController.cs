using Biograf.Api.Common;
using Biograf.Api.Services;
using Biograf.Shared.Auth;
using Biograf.Shared.Visningar;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Biograf.Api.Controllers;

[ApiController]
[Route("api/visningar")]
public class VisningarController(VisningTjanst visningTjanst) : ControllerBase
{
    /// <summary>Hämtar visningar. Filtrera med t.ex. ?datum=2026-10-15&amp;salongId=1&amp;genre=Action</summary>
    [HttpGet]
    public Task<List<VisningDto>> HamtaAlla([FromQuery] VisningFilter filter) => visningTjanst.HamtaAllaAsync(filter);

    [HttpGet("{id:int}")]
    public async Task<ActionResult<VisningDto>> Hamta(int id) => this.TillSvar(await visningTjanst.HamtaAsync(id));

    [HttpPost]
    [Authorize(Roles = Roller.PersonalEllerAdmin)]
    public async Task<ActionResult<VisningDto>> Skapa(SparaVisningDto dto)
    {
        var resultat = await visningTjanst.SkapaAsync(dto);
        return resultat.Lyckades
            ? CreatedAtAction(nameof(Hamta), new { id = resultat.Varde!.Id }, resultat.Varde)
            : this.TillSvar(resultat);
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = Roller.PersonalEllerAdmin)]
    public async Task<ActionResult<VisningDto>> Uppdatera(int id, SparaVisningDto dto) =>
        this.TillSvar(await visningTjanst.UppdateraAsync(id, dto));

    [HttpDelete("{id:int}")]
    [Authorize(Roles = Roller.PersonalEllerAdmin)]
    public async Task<ActionResult<Inget>> TaBort(int id) => this.TillSvar(await visningTjanst.TaBortAsync(id));
}
