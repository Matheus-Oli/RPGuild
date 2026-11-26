using Microsoft.AspNetCore.Mvc;
using RPGuild.Api.Http;
using RPGuild.Application.Contas;

namespace RPGuild.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(IAuthService auth) : ControllerBase
{
    [HttpPost("register")]
    [ProducesResponseType<UsuarioResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register(RegistrarRequest request, CancellationToken cancellationToken)
    {
        var resultado = await auth.RegistrarAsync(request, cancellationToken);
        return this.ParaResposta(resultado, usuario => StatusCode(StatusCodes.Status201Created, usuario));
    }
}
