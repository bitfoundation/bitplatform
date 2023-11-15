using Bit.Websites.Platform.Shared.Dtos.ContactUs;

namespace Bit.Websites.Platform.Client.Pages;

public partial class ContactUsPage
{
    private ContactUsDto _contactUsModel { get; set; } = new();

    private bool _isSending { get; set; }

    private async Task SendMessage()
    {
        if (_isSending) return;

        _isSending = true;

        try
        {
            await HttpClient.PostAsJsonAsync("ContactUs/SendMessage", _contactUsModel, AppJsonContext.Default.ContactUsDto);
            _contactUsModel.Email = "";
            _contactUsModel.Message = "";
        }
        finally
        {
            _isSending = false;
        }
    }
}
