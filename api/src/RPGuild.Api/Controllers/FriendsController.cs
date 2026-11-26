using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RPGuild.Api.Http;
using RPGuild.Application.Taberna;

namespace RPGuild.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/friends")]
public sealed class FriendsController(AmizadeService amizades) : ControllerBase
{
    [HttpGet("requests")]
    [ProducesResponseType<PedidosResponse>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Pedidos(CancellationToken cancellationToken) =>
        Ok(await amizades.ListarPedidosAsync(User.Id(), cancellationToken));

    [HttpPost("requests")]
    [ProducesResponseType<PedidoEnviadoResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Enviar(EnviarPedidoRequest request, CancellationToken cancellationToken)
    {
        var resultado = await amizades.EnviarAsync(User.Id(), request, cancellationToken);
        return this.ParaResposta(resultado, pedido => StatusCode(StatusCodes.Status201Created, pedido));
    }
}
