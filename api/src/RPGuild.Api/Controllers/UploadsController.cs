using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RPGuild.Api.Http;
using RPGuild.Application.Uploads;

namespace RPGuild.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/uploads")]
public sealed class UploadsController(UploadService uploads) : ControllerBase
{
    [HttpPost("sign")]
    [ProducesResponseType<UrlDeUploadResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Sign(AssinarUploadRequest request, CancellationToken cancellationToken) =>
        Ok(await uploads.AssinarAsync(User.Id(), request, cancellationToken));
}
