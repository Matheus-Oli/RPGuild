using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RPGuild.Api.Http;
using RPGuild.Application.Notificacoes;

namespace RPGuild.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/notifications")]
public sealed class NotificationsController(NotificacoesService notificacoes) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<PaginaDeNotificacoesResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Listar([FromQuery] ListarNotificacoesRequest request, CancellationToken cancellationToken) =>
        Ok(await notificacoes.ListarAsync(User.Id(), request, cancellationToken));

    [HttpGet("unread-count")]
    [ProducesResponseType<ContagemDeNaoLidasResponse>(StatusCodes.Status200OK)]
    public async Task<IActionResult> NaoLidas(CancellationToken cancellationToken) =>
        Ok(await notificacoes.ContarNaoLidasAsync(User.Id(), cancellationToken));

    [HttpPost("{id:guid}/read")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Ler(Guid id, CancellationToken cancellationToken)
    {
        var resultado = await notificacoes.MarcarComoLidaAsync(User.Id(), id, cancellationToken);
        return this.ParaResposta(resultado, _ => NoContent());
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Apagar(Guid id, CancellationToken cancellationToken)
    {
        var resultado = await notificacoes.ApagarAsync(User.Id(), id, cancellationToken);
        return this.ParaResposta(resultado, _ => NoContent());
    }

    [HttpPost("read-all")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> LerTodas(CancellationToken cancellationToken)
    {
        await notificacoes.MarcarTodasComoLidasAsync(User.Id(), cancellationToken);
        return NoContent();
    }
}
