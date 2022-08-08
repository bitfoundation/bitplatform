#if BlazorServer
using System.Reflection;
#endif

namespace TodoTemplate.App.Services.Implementations;

#if BlazorServer
public partial class ServerSideAuthTokenProvider : IAuthTokenProvider
{
    [AutoInject] readonly IJSRuntime jsRuntime = default!;
    [AutoInject] readonly IHttpContextAccessor httpContextAccessor = default!;

    private static readonly PropertyInfo IsInitializedProp = Assembly.Load("Microsoft.AspNetCore.Components.Server")
        .GetType("Microsoft.AspNetCore.Components.Server.Circuits.RemoteJSRuntime")
        .GetProperty("IsInitialized");

    public async Task<string?> GetAcccessToken()
    {
        var isInitialized = (bool)IsInitializedProp.GetValue(jsRuntime);

        if (isInitialized)
        {
            return await jsRuntime.InvokeAsync<string>("App.getCookie", "access_token");
        }

        return httpContextAccessor.HttpContext?.Request.Cookies["access_token"];
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

    public async Task<string?> GetAcccessToken()
    {
        return _httpContextAccessor.HttpContext?.Request.Cookies["access_token"];
    }
}
#endif
