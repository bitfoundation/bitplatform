using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Bit.Websites.Platform.Shared.Dtos;
using Bit.Websites.Platform.Shared.Dtos.ContactUs;
using Microsoft.Extensions.DependencyInjection;

namespace Bit.Websites.Platform.Web.Pages;

public partial class ContactUsPage
{
    [AutoInject] protected HttpClient HttpClient = default!;

    public ContactUsDto ContactUsModel { get; set; } = new();

    public bool IsLoading { get; set; }

    public bool IsSubmitButtonEnabled => IsLoading is false;

    private async Task SendMessage()
    {
        if (IsLoading)
        {
            return;
        }

        IsLoading = true;

        try
        {
            await HttpClient.PostAsJsonAsync("ContactUs/SendMessage", ContactUsModel, AppJsonContext.Default.ContactUsDto);
        }
        finally
        {
            IsLoading = false;
        }
    }
}
