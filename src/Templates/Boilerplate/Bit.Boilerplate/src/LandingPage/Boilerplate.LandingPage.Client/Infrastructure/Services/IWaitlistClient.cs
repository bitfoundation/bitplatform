using System.Net;
using System.Net.Http.Json;

namespace Boilerplate.LandingPage.Client.Infrastructure.Services;

public interface IWaitlistClient
{
    Task<WaitlistJoinResult> JoinAsync(WaitlistJoinRequest request, CancellationToken cancellationToken);

    Task<WaitlistConfirmResult> ConfirmAsync(string token, CancellationToken cancellationToken);

    Task UnsubscribeAsync(string token, CancellationToken cancellationToken);
}

public sealed class WaitlistException(string message) : Exception(message);

internal sealed partial class HttpWaitlistClient : IWaitlistClient
{
    [AutoInject] private HttpClient httpClient = default!;
    [AutoInject] private IStringLocalizer<LandingStrings> localizer = default!;

    public async Task<WaitlistJoinResult> JoinAsync(WaitlistJoinRequest request, CancellationToken cancellationToken)
    {
        using var response = await PostAsync("api/waitlist", JsonContent.Create(request, LandingJsonContext.Default.WaitlistJoinRequest), cancellationToken);
        return (await response.Content.ReadFromJsonAsync(LandingJsonContext.Default.WaitlistJoinResult, cancellationToken))!;
    }

    public async Task<WaitlistConfirmResult> ConfirmAsync(string token, CancellationToken cancellationToken)
    {
        using var response = await PostAsync("api/waitlist/confirm", JsonContent.Create(new WaitlistTokenRequest(token), LandingJsonContext.Default.WaitlistTokenRequest), cancellationToken);
        return (await response.Content.ReadFromJsonAsync(LandingJsonContext.Default.WaitlistConfirmResult, cancellationToken))!;
    }

    public async Task UnsubscribeAsync(string token, CancellationToken cancellationToken)
    {
        using var response = await PostAsync("api/waitlist/unsubscribe", JsonContent.Create(new WaitlistTokenRequest(token), LandingJsonContext.Default.WaitlistTokenRequest), cancellationToken);
    }

    private async Task<HttpResponseMessage> PostAsync(string url, HttpContent content, CancellationToken cancellationToken)
    {
        HttpResponseMessage response;

        try
        {
            response = await httpClient.PostAsync(url, content, cancellationToken);
        }
        catch (HttpRequestException)
        {
            throw new WaitlistException(localizer[nameof(LandingStrings.WaitlistOffline)]);
        }

        if (response.IsSuccessStatusCode)
            return response;

        var message = response.StatusCode switch
        {
            HttpStatusCode.TooManyRequests => localizer[nameof(LandingStrings.WaitlistTooManyRequests)],
            HttpStatusCode.BadRequest => localizer[nameof(LandingStrings.WaitlistInvalidEmail)],
            HttpStatusCode.NotFound => localizer[nameof(LandingStrings.WaitlistLinkExpired)],
            _ => localizer[nameof(LandingStrings.WaitlistFailed)]
        };

        response.Dispose();

        throw new WaitlistException(message);
    }
}
