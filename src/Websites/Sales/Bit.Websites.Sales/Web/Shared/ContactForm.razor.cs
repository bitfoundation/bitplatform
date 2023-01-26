using Bit.Websites.Sales.Shared.Dtos.ContactUs;

namespace Bit.Websites.Sales.Web.Shared;

public partial class ContactForm
{
    private bool _isLoading;
    private ContactUsDto _contactUs = new();

    [AutoInject] private HttpClient _httpClient = default!;

    private async Task DoSubmit()
    {
        if (_isLoading) return;

        _isLoading = true;

        try
        {
            await _httpClient.PostAsJsonAsync("ContactUs/Create", _contactUs, AppJsonContext.Default.ContactUsDto);
            //ToastService.ShowInfo("Your request submitted successfully");
        }
        catch (KnownException e)
        {
            //ToastService.ShowError(ErrorStrings.ResourceManager.Translate(e.Message));
        }
        finally
        {
            _isLoading = false;
        }
    }
}
