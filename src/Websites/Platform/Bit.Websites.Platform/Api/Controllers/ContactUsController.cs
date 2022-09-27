using Bit.Websites.Platform.Api.Services;
using Bit.Websites.Platform.Shared.Dtos.ContactUs;
using Microsoft.AspNetCore.Mvc;

namespace Bit.Websites.Platform.Api.Controllers;

[Route("api/[controller]/[action]")]
[ApiController]
public partial class ContactUsController : ControllerBase
{
    [AutoInject] protected TelegramBotService TelegramBotService = default!;

    [HttpPost]
    public async Task<IActionResult> SendMessage(ContactUsDto contactUsDto, CancellationToken cancellationToken)
    {
        if (ModelState.IsValid)
        {
            await TelegramBotService.SendContactUsMessage(contactUsDto.Email, contactUsDto.Message, cancellationToken);
            return Ok();
        }
        else
        {
            return BadRequest(ModelState);
        }
    }
}
