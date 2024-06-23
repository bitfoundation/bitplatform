using Bit.Websites.Sales.Shared.Dtos.ContactUs;

namespace Bit.Websites.Sales.Client.Shared;

public partial class ContactForm
{
    private bool isLoading;
    private string? successMessage;
    private string? errorMessage;
    private ContactUsDto contactUs = new();

    [AutoInject] private HttpClient httpClient = default!;

    private async Task DoSubmit()
    {
        if (isLoading) return;

        isLoading = true;

        errorMessage = string.Empty;
        successMessage = string.Empty;

        try
        {
            await httpClient.PostAsJsonAsync("ContactUs/SendMessage", contactUs, AppJsonContext.Default.ContactUsDto);

            contactUs.Name = string.Empty;
            contactUs.Email = string.Empty;
            contactUs.Information = string.Empty;

            successMessage = "Your request submitted successfully";
        }
        catch (KnownException e)
        {
            errorMessage = e.Message;
        }
        finally
        {
            isLoading = false;
        }
    }
}
