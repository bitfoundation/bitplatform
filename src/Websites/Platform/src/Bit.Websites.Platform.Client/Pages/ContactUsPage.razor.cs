using Bit.Websites.Platform.Shared.Dtos.ContactUs;

namespace Bit.Websites.Platform.Client.Pages;

public partial class ContactUsPage
{
    private bool isSent;
    private bool isSending;
    private ContactUsDto contactUsModel = new();

    private void ResetForm()
    {
        isSent = false;
        contactUsModel = new();
    }

    private async Task SendMessage()
    {
        if (isSending) return;

        isSending = true;
        isSent = false;

        try
        {
            // ExceptionDelegatingHandler in the HttpClient pipeline throws a typed exception on any
            // non-success response; WrapHandled on the form's OnValidSubmit surfaces its message.
            await HttpClient.PostAsJsonAsync("api/ContactUs/SendMessage", contactUsModel, AppJsonContext.Default.ContactUsDto);
            contactUsModel = new();
            isSent = true;
        }
        finally
        {
            isSending = false;
        }
    }
}
