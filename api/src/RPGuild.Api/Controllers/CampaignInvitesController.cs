using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RPGuild.Api.Http;
using RPGuild.Application.Campanhas;

namespace RPGuild.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/campaign-invites")]
public sealed class CampaignInvitesController(ConviteService convites) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<MeuConviteResponse>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Meus(CancellationToken cancellationToken) =>
        Ok(await convites.ListarMeusAsync(User.Id(), cancellationToken));
}
