using Microsoft.AspNetCore.Mvc;
using RPGuild.Application.Contas;

namespace RPGuild.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(IAuthService auth) : ControllerBase
{
    [HttpPost("register")]
    [ProducesResponseType<UsuarioResponse>(StatusCodes.Status201Created)]
    public async Task<IActionResult> Register(RegistrarRequest request, CancellationToken cancellationToken)
    {
        var resultado = await auth.RegistrarAsync(request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, resultado.Valor);
    }
}
