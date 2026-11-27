using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RPGuild.Api.Http;
using RPGuild.Application.Campanhas;

namespace RPGuild.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/campaigns")]
public sealed class CampaignsController(CampanhaService campanhas) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType<CampanhaDetalheResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Criar(CriarCampanhaRequest request, CancellationToken cancellationToken)
    {
        var resultado = await campanhas.CriarAsync(User.Id(), request, cancellationToken);
        return this.ParaResposta(resultado, detalhe => Created($"/api/campaigns/{detalhe.Id}", detalhe));
    }
}
