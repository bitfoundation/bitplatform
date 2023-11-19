//-:cnd:noEmit

using System.Reflection;
using Microsoft.JSInterop;

namespace BlazorWeb.Client.Services;

/// <summary>
/// The <see cref="ClientSideAuthTokenProvider"/> reads the token from the cookie,
/// but during prerendering, there is no access to localStorage or the stored cookies.
/// However, the cookies are sent automatically in http request and The <see cref="ServerSideAuthTokenProvider"/> provides that token to the application.
/// </summary>
public partial class ServerSideAuthTokenProvider : IAuthTokenProvider
{
    [AutoInject] private IHttpContextAccessor _httpContextAccessor = default!;

    private static readonly PropertyInfo IsInitializedProp = Assembly.Load("Microsoft.AspNetCore.Components.Server")!
                                                                .GetType("Microsoft.AspNetCore.Components.Server.Circuits.RemoteJSRuntime")!
                                                                .GetProperty("IsInitialized")!;

    public bool IsInitialized => (bool)IsInitializedProp.GetValue(_httpContextAccessor.HttpContext!.RequestServices.GetRequiredService<IJSRuntime>())!;

    public async Task<string?> GetAccessTokenAsync()
    {
        var jsRuntime = _httpContextAccessor.HttpContext!.RequestServices.GetRequiredService<IJSRuntime>();

        if (IsInitialized)
        {
            return await jsRuntime.GetCookie("access_token");
        }

        return _httpContextAccessor.HttpContext?.Request.Cookies["access_token"];
    }
}
