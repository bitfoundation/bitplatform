﻿//-:cnd:noEmit
namespace Boilerplate.Client.Core.Services;

public partial class ClientSideAuthTokenProvider : IAuthTokenProvider
{
    [AutoInject] private IJSRuntime _jsRuntime = default!;

    public bool IsInitialized => true;

    public async Task<string?> GetAccessTokenAsync()
    {
#if BlazorHybrid
        return Preferences.Get("access_token", null);
#else
        return await _jsRuntime.GetCookie("access_token");
#endif
    }
}
