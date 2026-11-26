using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RPGuild.Api.Http;
using RPGuild.Application.Contas;

namespace RPGuild.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/me")]
public sealed class MeController(IAuthService auth) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<PerfilResponse>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        var resultado = await auth.ObterPerfilAsync(User.Id(), cancellationToken);
        return this.ParaResposta(resultado, Ok);
    }
}
