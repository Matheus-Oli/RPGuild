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
}
