using Bit.Websites.Platform.Shared.Dtos.ContactUs;

namespace Bit.Websites.Platform.Client.Pages;

public partial class ContactUsPage
{
    private ContactUsDto contactUsModel { get; set; } = new();

    private bool isSending { get; set; }

    private async Task SendMessage()
    {
        if (isSending) return;

        isSending = true;

        try
        {
            await HttpClient.PostAsJsonAsync("ContactUs/SendMessage", contactUsModel, AppJsonContext.Default.ContactUsDto);
            contactUsModel.Email = "";
            contactUsModel.Message = "";
        }
        finally
        {
            isSending = false;
        }
    }
}
