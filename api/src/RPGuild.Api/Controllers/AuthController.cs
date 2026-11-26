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

    [HttpPost("login")]
    [ProducesResponseType<AccessTokenResponse>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Login(LoginRequest request, CancellationToken cancellationToken)
    {
        var resultado = await auth.LoginAsync(request, cancellationToken);
        return this.ParaResposta(resultado, Sessao);
    }

    [HttpPost("refresh")]
    [ProducesResponseType<AccessTokenResponse>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Refresh(CancellationToken cancellationToken)
    {
        var resultado = await auth.RenovarAsync(RefreshCookie.Ler(Request), cancellationToken);
        return this.ParaResposta(resultado, Sessao);
    }

    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        await auth.LogoutAsync(RefreshCookie.Ler(Request), cancellationToken);
        RefreshCookie.Apagar(Response);
        return NoContent();
    }

    private OkObjectResult Sessao(SessaoAutenticada sessao)
    {
        RefreshCookie.Definir(Response, sessao.Refresh);
        return Ok(new AccessTokenResponse(sessao.Access.Token, sessao.Access.ExpiresAt));
    }
}
