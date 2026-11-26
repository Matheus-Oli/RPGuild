using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RPGuild.Application.Campanhas;

namespace RPGuild.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/systems")]
public sealed class SystemsController(SistemasService sistemas) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<SistemaDisponivelResponse>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Listar(CancellationToken cancellationToken) => Ok(await sistemas.ListarAsync(cancellationToken));
}
