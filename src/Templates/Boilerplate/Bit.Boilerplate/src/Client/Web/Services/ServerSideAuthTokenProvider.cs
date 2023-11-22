//-:cnd:noEmit
#if BlazorServer
using System.Reflection;
#endif

namespace Boilerplate.Client.Web.Services;

/// <summary>
/// The <see cref="ClientSideAuthTokenProvider"/> reads the token from the cookie,
/// but during prerendering, there is no access to localStorage or the stored cookies.
/// However, the cookies are sent automatically in http request and The <see cref="ServerSideAuthTokenProvider"/> provides that token to the application.
/// </summary>
#if BlazorServer
public partial class ServerSideAuthTokenProvider : IAuthTokenProvider
{
    [AutoInject] private IHttpContextAccessor _httpContextAccessor = default!;
    [AutoInject] private IJSRuntime _jsRuntime = default!;

    private static readonly PropertyInfo IsInitializedProp = Assembly.Load("Microsoft.AspNetCore.Components.Server")!
                                                                .GetType("Microsoft.AspNetCore.Components.Server.Circuits.RemoteJSRuntime")!
                                                                .GetProperty("IsInitialized")!;

    public bool IsInitialized => (bool)IsInitializedProp.GetValue(_jsRuntime)!;

    public async Task<string?> GetAccessTokenAsync()
    {
        if (IsInitialized)
        {
            return await _jsRuntime.GetCookie("access_token");
        }

        return _httpContextAccessor.HttpContext?.Request.Cookies["access_token"];
    }
}
#else
public class ServerSideAuthTokenProvider : IAuthTokenProvider
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public ServerSideAuthTokenProvider(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public bool IsInitialized => false;

    public async Task<string?> GetAccessTokenAsync()
    {
        return _httpContextAccessor.HttpContext?.Request.Cookies["access_token"];
    }
}
#endif
