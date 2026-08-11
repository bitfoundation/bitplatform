using Bit.Websites.Platform.Server.Services;
using Bit.Websites.Platform.Shared.Dtos.SupportPackage;
using Microsoft.AspNetCore.RateLimiting;

namespace Bit.Websites.Platform.Server.Controllers;

[Route("api/[controller]/[action]")]
[ApiController]
[EnableRateLimiting("MessageSubmit")]
public partial class SupportPackageController : AppControllerBase
{
    [AutoInject] private TelegramBotService telegramBotService = default!;

    [HttpPost]
    public async Task<IActionResult> BuyPackage(BuyPackageDto buyPackageDto, CancellationToken cancellationToken)
    {
        await telegramBotService.SendBuyPackageMessage(string.Empty, buyPackageDto.Email, buyPackageDto.Message, cancellationToken);
        return Ok();
    }
}
