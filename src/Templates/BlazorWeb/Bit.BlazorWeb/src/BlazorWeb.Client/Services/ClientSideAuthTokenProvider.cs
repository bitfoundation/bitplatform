//-:cnd:noEmit
namespace BlazorWeb.Client.Services;

public partial class ClientSideAuthTokenProvider : IAuthTokenProvider
{
    [AutoInject] private IJSRuntime _jsRuntime = default!;

    public async Task<string?> GetAccessTokenAsync()
    {
        return await _jsRuntime.GetCookie("access_token");
    }

    public async Task<string?> GetRefreshTokenAsync()
    {
        return await _jsRuntime.GetCookie("refresh_token");
    }
}
