using Bit.Websites.Platform.Server.Services;
using Bit.Websites.Platform.Shared.Dtos.ContactUs;

namespace Bit.Websites.Platform.Server.Controllers;

[Route("api/[controller]/[action]")]
[ApiController]
public partial class ContactUsController : AppControllerBase
{
    [AutoInject] private TelegramBotService telegramBotService = default!;

    [HttpPost]
    public async Task<IActionResult> SendMessage(ContactUsDto contactUsDto, CancellationToken cancellationToken)
    {
        await telegramBotService.SendContactUsMessage(contactUsDto.Email, contactUsDto.Message, cancellationToken);
        return Ok();
    }
}
