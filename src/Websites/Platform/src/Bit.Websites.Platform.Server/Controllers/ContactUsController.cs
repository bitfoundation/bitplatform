using Bit.Websites.Platform.Server.Services;
using Bit.Websites.Platform.Shared.Dtos.ContactUs;

namespace Bit.Websites.Platform.Server.Controllers;

[Route("api/[controller]/[action]")]
[ApiController]
public partial class ContactUsController : AppControllerBase
{
    [AutoInject] protected TelegramBotService TelegramBotService = default!;

    [HttpPost]
    public async Task<IActionResult> SendMessage(ContactUsDto contactUsDto, CancellationToken cancellationToken)
    {
        await TelegramBotService.SendContactUsMessage(contactUsDto.Email, contactUsDto.Message, cancellationToken);
        return Ok();
    }
}
