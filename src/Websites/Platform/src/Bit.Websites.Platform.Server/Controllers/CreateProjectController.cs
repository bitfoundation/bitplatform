using Bit.Websites.Platform.Server.Services;
using Bit.Websites.Platform.Shared.Dtos.CreateProject;
using Microsoft.AspNetCore.RateLimiting;

namespace Bit.Websites.Platform.Server.Controllers;

[Route("api/[controller]/[action]")]
[ApiController]
[EnableRateLimiting("MessageSubmit")]
public partial class CreateProjectController : AppControllerBase
{
    [AutoInject] private TelegramBotService telegramBotService = default!;

    [HttpPost]
    public async Task<IActionResult> Created(ProjectCreatedDto dto, CancellationToken cancellationToken)
    {
        await telegramBotService.SendProjectCreatedMessage(dto.Command!, cancellationToken);
        return Ok();
    }
}