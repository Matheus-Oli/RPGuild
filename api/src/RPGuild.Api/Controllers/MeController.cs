using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RPGuild.Api.Http;
using RPGuild.Application.Contas;

namespace RPGuild.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/me")]
public sealed class MeController(IPerfilService perfil) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<PerfilResponse>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        var resultado = await perfil.ObterAsync(User.Id(), cancellationToken);
        return this.ParaResposta(resultado, Ok);
    }

    [HttpPut]
    [ProducesResponseType<PerfilResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Put(AtualizarPerfilRequest request, CancellationToken cancellationToken)
    {
        var resultado = await perfil.AtualizarAsync(User.Id(), request, cancellationToken);
        return this.ParaResposta(resultado, Ok);
    }

    [HttpPut("email")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> PutEmail(TrocarEmailRequest request, CancellationToken cancellationToken)
    {
        var resultado = await perfil.PedirTrocaDeEmailAsync(User.Id(), request, cancellationToken);
        return this.ParaResposta(resultado, _ => Accepted());
    }

    [HttpPut("password")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> PutPassword(TrocarSenhaRequest request, CancellationToken cancellationToken)
    {
        var resultado = await perfil.TrocarSenhaAsync(User.Id(), request, User.SessaoId(), cancellationToken);
        return this.ParaResposta(resultado, _ => NoContent());
    }

    // Anônimo: o link chega por e-mail e pode ser aberto sem estar logado. Se vier Bearer, a sessão dele continua.
    [AllowAnonymous]
    [HttpPost("email/confirm")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ConfirmEmail(ConfirmarTrocaDeEmailRequest request, CancellationToken cancellationToken)
    {
        var resultado = await perfil.ConfirmarTrocaDeEmailAsync(request, User.SessaoId(), cancellationToken);
        return this.ParaResposta(resultado, _ => NoContent());
    }
}
