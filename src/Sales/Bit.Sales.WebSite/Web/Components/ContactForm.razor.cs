using Bit.Sales.WebSite.Shared.Dtos.ContactUs;
using Blazored.Toast.Services;

namespace Bit.Sales.WebSite.App.Components;

public partial class ContactForm
{
    [AutoInject] public HttpClient HttpClient = default!;
    [AutoInject] public IToastService ToastService = default!;

    public ContactUsDto ContactUs { get; set; } = new();

    public bool IsLoading { get; set; }

    private bool IsSubmitButtonEnabled =>
        string.IsNullOrWhiteSpace(ContactUs.Name) is false &&
        string.IsNullOrWhiteSpace(ContactUs.Email) is false &&
        IsLoading is false;

    private async Task DoSubmit()
    {
        if (IsLoading)
        {
            return;
        }

        IsLoading = true;

        try
        {
            await HttpClient.PostAsJsonAsync("ContactUs", ContactUs, AppJsonContext.Default.ContactUsDto);
            ToastService.ShowInfo("Your request submitted successfully");
        }
        catch (KnownException e)
        {
            ToastService.ShowError(ErrorStrings.ResourceManager.Translate(e.Message));
        }
        finally
        {
            IsLoading = false;
        }
    }
}
