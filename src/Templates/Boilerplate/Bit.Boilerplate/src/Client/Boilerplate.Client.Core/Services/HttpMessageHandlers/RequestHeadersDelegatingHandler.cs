using System.Net;
using System.Net.Http.Headers;
using Boilerplate.Shared.Dtos.Identity;
using Boilerplate.Shared.Controllers.Identity;
using Microsoft.AspNetCore.Components.WebAssembly.Http;

namespace Boilerplate.Client.Core.Services.HttpMessageHandlers;

public partial class RequestHeadersDelegatingHandler(ITelemetryContext telemetryContext, HttpMessageHandler handler)
    : DelegatingHandler(handler)
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        // Having `credentials` set to `Include` will double send access tokens in the request headers (Cookeies and Authorization header).
        // But in order to make server's Set-Cookie work properly, we need to set `credentials` to `Include`.
        var responseType = request.Options.GetValueOrDefault(RequestOptionNames.ResponseType, null) as Type;
        var actionName = request.Options.GetValueOrDefault(RequestOptionNames.ActionName, null) as string;
        var actionHasSetCookieInServerSide = responseType == typeof(SignInResponseDto) || responseType == typeof(TokenResponseDto) || actionName is nameof(IUserController.SignOut);
        request.SetBrowserRequestCredentials(actionHasSetCookieInServerSide ? BrowserRequestCredentials.Include : BrowserRequestCredentials.Omit);

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
