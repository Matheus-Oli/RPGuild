using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.JsonWebTokens;
using RPGuild.Api.Http;
using RPGuild.Application.Contas;

namespace RPGuild.Api.Controllers;

[ApiController]
[Route("api/me")]
public sealed class MeController(IAuthService auth) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<PerfilResponse>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        var userId = Guid.Parse(User.FindFirst(JwtRegisteredClaimNames.Sub)!.Value);
        var resultado = await auth.ObterPerfilAsync(userId, cancellationToken);
        return this.ParaResposta(resultado, Ok);
    }
}
