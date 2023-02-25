using Bit.Websites.Sales.Shared.Dtos.ContactUs;

namespace Bit.Websites.Sales.Web.Shared;

public partial class ContactForm
{
    private bool _isLoading;
    private string? _successMessage;
    private string? _errorMessage;
    private ContactUsDto _contactUs = new();

    [AutoInject] private HttpClient _httpClient = default!;

    private async Task DoSubmit()
    {
        if (_isLoading) return;

        _isLoading = true;

        _errorMessage = string.Empty;
        _successMessage = string.Empty;

        try
        {
            await _httpClient.PostAsJsonAsync("ContactUs/SendMessage", _contactUs, AppJsonContext.Default.ContactUsDto);

            _contactUs.Name = string.Empty;
            _contactUs.Email = string.Empty;
            _contactUs.Information = string.Empty;

            _successMessage = "Your request submitted successfully";
        }
        catch (KnownException e)
        {
            _errorMessage = ErrorStrings.ResourceManager.Translate(e.Message);
        }
        finally
        {
            _isLoading = false;
        }
    }
}
