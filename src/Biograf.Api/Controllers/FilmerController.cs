using Biograf.Api.Common;
using Biograf.Api.Services;
using Biograf.Shared.Auth;
using Biograf.Shared.Filmer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Biograf.Api.Controllers;

/// <summary>Filmregistret. Alla kan läsa, personal och admin kan ändra.</summary>
[ApiController]
[Route("api/filmer")]
public class FilmerController(FilmTjanst filmTjanst) : ControllerBase
{
    /// <summary>Hämtar filmer. Filtrera med t.ex. ?sok=dune&amp;genre=Sci-fi&amp;maxAldersgrans=11</summary>
    [HttpGet]
    public Task<List<FilmDto>> HamtaAlla([FromQuery] FilmFilter filter) => filmTjanst.HamtaAllaAsync(filter);

    [HttpGet("{id:int}")]
    public async Task<ActionResult<FilmDto>> Hamta(int id) => this.TillSvar(await filmTjanst.HamtaAsync(id));

    [HttpGet("genrer")]
    public Task<List<string>> HamtaGenrer() => filmTjanst.HamtaGenrerAsync();

    [HttpPost]
    [Authorize(Roles = Roller.PersonalEllerAdmin)]
    public async Task<ActionResult<FilmDto>> Skapa(SparaFilmDto dto)
    {
        var resultat = await filmTjanst.SkapaAsync(dto);
        return resultat.Lyckades
            ? CreatedAtAction(nameof(Hamta), new { id = resultat.Varde!.Id }, resultat.Varde)
            : this.TillSvar(resultat);
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = Roller.PersonalEllerAdmin)]
    public async Task<ActionResult<FilmDto>> Uppdatera(int id, SparaFilmDto dto) =>
        this.TillSvar(await filmTjanst.UppdateraAsync(id, dto));

    [HttpDelete("{id:int}")]
    [Authorize(Roles = Roller.PersonalEllerAdmin)]
    public async Task<ActionResult<Inget>> TaBort(int id) => this.TillSvar(await filmTjanst.TaBortAsync(id));
}
