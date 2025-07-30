using System.Net;
using System.Net.Http.Headers;
using Boilerplate.Shared.Dtos.Identity;
using Microsoft.AspNetCore.Components.WebAssembly.Http;

namespace Boilerplate.Client.Core.Services.HttpMessageHandlers;

public partial class RequestHeadersDelegatingHandler(ITelemetryContext telemetryContext, HttpMessageHandler handler)
    : DelegatingHandler(handler)
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        // In order to have access_token as http only cookie to support pre-rendering scenarios,
        // we need to set the request credentials to include to api's that return access token in the response.
        // In other actions, include only result into double sending the access token in the request headers unnecessarily. (Authorization and Cookie headers).
        var responseType = request.Options.GetValueOrDefault(RequestOptionNames.ResponseType, null) as Type;
        var includeCredentials = responseType == typeof(SignInResponseDto) || responseType == typeof(TokenResponseDto);
        request.SetBrowserRequestCredentials(includeCredentials ? BrowserRequestCredentials.Include : BrowserRequestCredentials.Omit);

        request.SetBrowserResponseStreamingEnabled(true);

        request.Version = HttpVersion.Version20;
        request.VersionPolicy = HttpVersionPolicy.RequestVersionOrLower;

        if (request.Headers.UserAgent.Any() is false)
        {
            request.Headers.UserAgent.TryParseAdd(telemetryContext.Platform);
        }

        if (CultureInfoManager.InvariantGlobalization is false && string.IsNullOrEmpty(CultureInfo.CurrentUICulture.Name) is false)
        {
            request.Headers.AcceptLanguage.Add(new StringWithQualityHeaderValue(CultureInfo.CurrentUICulture.Name));
        }

        request.Headers.Add("X-App-Version", telemetryContext.AppVersion);
        request.Headers.Add("X-App-Platform", AppPlatform.Type.ToString());

        return await base.SendAsync(request, cancellationToken);
    }
}
