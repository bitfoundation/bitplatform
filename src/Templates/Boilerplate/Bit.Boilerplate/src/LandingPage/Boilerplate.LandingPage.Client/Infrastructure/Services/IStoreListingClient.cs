using System.Net.Http.Json;

namespace Boilerplate.LandingPage.Client.Infrastructure.Services;

public interface IStoreListingClient
{
    Task<StoreListing?> GetAppStoreListingAsync(string appleAppId, CancellationToken cancellationToken);
}

internal sealed partial class HttpStoreListingClient : IStoreListingClient
{
    [AutoInject] private HttpClient httpClient = default!;

    public async Task<StoreListing?> GetAppStoreListingAsync(string appleAppId, CancellationToken cancellationToken)
    {
        try
        {
            return await httpClient.GetFromJsonAsync($"api/store-listing/{Uri.EscapeDataString(appleAppId)}", LandingJsonContext.Default.StoreListing, cancellationToken);
        }
        catch (HttpRequestException)
        {
            return null;
        }
    }
}
