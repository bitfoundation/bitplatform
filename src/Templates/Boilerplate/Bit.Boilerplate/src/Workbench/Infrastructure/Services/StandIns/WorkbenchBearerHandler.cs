using System.Net.Http.Headers;

namespace Boilerplate.Workbench.Infrastructure.Services.StandIns;

/// <summary>Puts the view's access token on what the app's code sends with an HttpClient, as the app's own client does.</summary>
public sealed class WorkbenchBearerHandler(IAuthTokenProvider tokenProvider) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (request.Headers.Authorization is null && await tokenProvider.GetAccessToken() is { } accessToken)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        }

        return await base.SendAsync(request, cancellationToken);
    }
}
