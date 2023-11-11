//-:cnd:noEmit

using System.Reflection;
using Microsoft.JSInterop;

namespace BlazorWeb.Web.Services;

/// <summary>
/// The <see cref="ClientSideAuthTokenProvider"/> reads the token from the cookie,
/// but during prerendering, there is no access to localStorage or the stored cookies.
/// However, the cookies are sent automatically in http request and The <see cref="ServerSideAuthTokenProvider"/> provides that token to the application.
/// </summary>
public partial class ServerSideAuthTokenProvider : IAuthTokenProvider
{
    [AutoInject] private IJSRuntime _jsRuntime = default!;
    [AutoInject] private IHttpContextAccessor _httpContextAccessor = default!;

    private static readonly PropertyInfo IsInitializedProp = Assembly.Load("Microsoft.AspNetCore.Components.Server")!
                                                                .GetType("Microsoft.AspNetCore.Components.Server.Circuits.RemoteJSRuntime")!
                                                                .GetProperty("IsInitialized")!;

    public async Task<string?> GetAccessTokenAsync()
    {
        var isInitialized = (bool)IsInitializedProp.GetValue(_jsRuntime)!;

        if (isInitialized)
        {
            return await _jsRuntime.InvokeAsync<string>("App.getCookie", "access_token");
        }

        return _httpContextAccessor.HttpContext?.Request.Cookies["access_token"];
    }
}
