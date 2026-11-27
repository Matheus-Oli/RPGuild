using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RPGuild.Api.Http;
using RPGuild.Application.Campanhas;

namespace RPGuild.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/campaigns")]
public sealed class CampaignsController(CampanhaService campanhas, ConviteService convites, GradeDeCampanhas grade) : ControllerBase
{
    [HttpPost("{id:guid}/invites")]
    [ProducesResponseType<IReadOnlyList<ConviteDaCampanhaResponse>>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Convidar(Guid id, ConvidarRequest request, CancellationToken cancellationToken)
    {
        var resultado = await convites.ConvidarAsync(User.Id(), id, request, cancellationToken);
        return this.ParaResposta(resultado, criados => StatusCode(StatusCodes.Status201Created, criados));
    }

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

    [HttpGet("{id:guid}")]
    [ProducesResponseType<CampanhaDetalheResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Detalhe(Guid id, CancellationToken cancellationToken)
    {
        var resultado = await campanhas.DetalheAsync(User.Id(), id, cancellationToken);
        return this.ParaResposta(resultado, detalhe => Ok(detalhe));
    }

    [HttpGet("{id:guid}/members")]
    [ProducesResponseType<IReadOnlyList<MembroDaCampanhaResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Membros(Guid id, CancellationToken cancellationToken)
    {
        var resultado = await campanhas.MembrosAsync(User.Id(), id, cancellationToken);
        return this.ParaResposta(resultado, membros => Ok(membros));
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType<CampanhaDetalheResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Editar(Guid id, EditarCampanhaRequest request, CancellationToken cancellationToken)
    {
        var resultado = await campanhas.EditarAsync(User.Id(), id, request, cancellationToken);
        return this.ParaResposta(resultado, detalhe => Ok(detalhe));
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Excluir(Guid id, ExcluirCampanhaRequest request, CancellationToken cancellationToken)
    {
        var resultado = await campanhas.ExcluirAsync(User.Id(), id, request, cancellationToken);
        return this.ParaResposta(resultado, _ => NoContent());
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
