using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RPGuild.Api.Http;
using RPGuild.Application.Campanhas;

namespace RPGuild.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/campaign-invites")]
public sealed class CampaignInvitesController(ConviteService convites) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<MeuConviteResponse>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Meus(CancellationToken cancellationToken) =>
        Ok(await convites.ListarMeusAsync(User.Id(), cancellationToken));

    /// <summary>
    /// O card do convite. O convidado e o mestre veem tudo; qualquer outro usuário logado, só {id, podeResponder, para}.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType<CardDoConviteResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<CardDoConviteParaOutroResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Card(Guid id, CancellationToken cancellationToken)
    {
        var resultado = await convites.CardAsync(User.Id(), id, cancellationToken);

        // Pelo tipo concreto: cada perfil vê só os campos do seu card.
        return this.ParaResposta(resultado, card => Ok((object)card));
    }

    [HttpPost("{id:guid}/accept")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Aceitar(Guid id, CancellationToken cancellationToken)
    {
        var resultado = await convites.AceitarAsync(User.Id(), id, cancellationToken);
        return this.ParaResposta(resultado, _ => NoContent());
    }

    [HttpPost("{id:guid}/decline")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Recusar(Guid id, CancellationToken cancellationToken)
    {
        var resultado = await convites.RecusarAsync(User.Id(), id, cancellationToken);
        return this.ParaResposta(resultado, _ => NoContent());
    }

    /// <summary>
    /// O mestre cancela um convite pendente.
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Cancelar(Guid id, CancellationToken cancellationToken)
    {
        var resultado = await convites.CancelarAsync(User.Id(), id, cancellationToken);
        return this.ParaResposta(resultado, _ => NoContent());
    }
}
