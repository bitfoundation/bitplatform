using Bit.Websites.Platform.Api.Services;
using Bit.Websites.Platform.Shared.Dtos.ContactUs;
using Bit.Websites.Platform.Shared.Dtos.SupportPackage;
using Microsoft.AspNetCore.Mvc;

namespace Bit.Websites.Platform.Api.Controllers;

[Route("api/[controller]/[action]")]
[ApiController]
public partial class SupportPackageController : ControllerBase
{
    [AutoInject] protected TelegramBotService TelegramBotService = default!;

    [HttpPost]
    public async Task<IActionResult> BuyPackage(BuyPackageDto buyPackageDto, CancellationToken cancellationToken)
    {
        if (ModelState.IsValid)
        {
            await TelegramBotService.SendBuyPackageMessage(buyPackageDto.SalePackageTitle, buyPackageDto.Email, buyPackageDto.Message, cancellationToken);
            return Ok();
        }
        else
        {
            return BadRequest(ModelState);
        }
    }
}
