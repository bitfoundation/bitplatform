﻿#if BlazorServer
using System.Reflection;
#endif

namespace AdminPanel.App.Services.Implementations;

#if BlazorServer
public class ServerSideAuthTokenProvider : IAuthTokenProvider
{
    private readonly IJSRuntime _jsRuntime;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public ServerSideAuthTokenProvider(IJSRuntime jsRuntime, IHttpContextAccessor httpContextAccessor)
    {
        _jsRuntime = jsRuntime;
        _httpContextAccessor = httpContextAccessor;
    }

    private static readonly PropertyInfo IsInitializedProp = Assembly.Load("Microsoft.AspNetCore.Components.Server")
        .GetType("Microsoft.AspNetCore.Components.Server.Circuits.RemoteJSRuntime")
        .GetProperty("IsInitialized");

    public async Task<string?> GetAcccessToken()
    {
        var isInitialized = (bool)IsInitializedProp.GetValue(_jsRuntime);

        if (isInitialized)
        {
            return await _jsRuntime.InvokeAsync<string>("App.getCookie", "access_token");
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

    public async Task<string?> GetAcccessToken()
    {
        return _httpContextAccessor.HttpContext?.Request.Cookies["access_token"];
    }
}
#endif
