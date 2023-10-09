//-:cnd:noEmit
namespace TodoTemplate.Client.Core.Services;

public partial class ClientSideAuthTokenProvider : IAuthTokenProvider
{
    [AutoInject] private IJSRuntime _jsRuntime = default!;

    public async Task<string?> GetAccessTokenAsync()
    {
#if BlazorHybrid
        return Preferences.Get("access_token", null);
#else
        return await _jsRuntime.InvokeAsync<string>("App.getCookie", "access_token");
#endif
    }
}
