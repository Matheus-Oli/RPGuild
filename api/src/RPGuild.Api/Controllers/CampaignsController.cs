using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RPGuild.Api.Http;
using RPGuild.Application.Campanhas;

namespace RPGuild.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/campaigns")]
public sealed class CampaignsController(CampanhaService campanhas, GradeDeCampanhas grade) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<CampanhaCardResponse>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Listar(CancellationToken cancellationToken) =>
        Ok(await campanhas.ListarAsync(User.Id(), cancellationToken));

    [HttpPost]
    [ProducesResponseType<CampanhaDetalheResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Criar(CriarCampanhaRequest request, CancellationToken cancellationToken)
    {
        var resultado = await campanhas.CriarAsync(User.Id(), request, cancellationToken);
        return this.ParaResposta(resultado, detalhe => Created($"/api/campaigns/{detalhe.Id}", detalhe));
    }

    [HttpPut("order")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Ordenar(OrdenarCampanhasRequest request, CancellationToken cancellationToken)
    {
        var resultado = await grade.ReordenarAsync(User.Id(), request.CampaignIds, cancellationToken);
        return this.ParaResposta(resultado, _ => NoContent());
    }
}
