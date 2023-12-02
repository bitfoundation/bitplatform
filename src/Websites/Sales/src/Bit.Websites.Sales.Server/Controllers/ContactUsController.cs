using Bit.Websites.Sales.Server.Services;
using Bit.Websites.Sales.Shared.Dtos.ContactUs;

namespace Bit.Websites.Sales.Server.Controllers;

[Route("api/[controller]/[action]")]
[ApiController]
public partial class ContactUsController : AppControllerBase
{
    [AutoInject] private TelegramBotService telegramBotService = default!;

    [HttpPost]
    public async Task<IActionResult> SendMessage(ContactUsDto contactUsDto, CancellationToken cancellationToken)
    {
        await telegramBotService.SendContactUsMessage(contactUsDto.Email, contactUsDto.Name, contactUsDto.Information, cancellationToken);
        return Ok();
    }
}
