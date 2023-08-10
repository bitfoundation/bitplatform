using Bit.Websites.Sales.Api.Services;
using Bit.Websites.Sales.Shared.Dtos.ContactUs;

namespace Bit.Websites.Sales.Api.Controllers;

[Route("api/[controller]/[action]")]
[ApiController]
public partial class ContactUsController : AppControllerBase
{
    [AutoInject] protected TelegramBotService TelegramBotService = default!;

    [HttpPost]
    public async Task<IActionResult> SendMessage(ContactUsDto contactUsDto, CancellationToken cancellationToken)
    {
        if (ModelState.IsValid)
        {
            await TelegramBotService.SendContactUsMessage(contactUsDto.Email, contactUsDto.Name, contactUsDto.Information, cancellationToken);

            return Ok();
        }
        else
        {
            return BadRequest(ModelState);
        }
    }
}
