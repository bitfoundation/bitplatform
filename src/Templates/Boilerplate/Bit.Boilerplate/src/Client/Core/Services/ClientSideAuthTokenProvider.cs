//-:cnd:noEmit
namespace Boilerplate.Client.Core.Services;

public partial class ClientSideAuthTokenProvider : IAuthTokenProvider
{
    [AutoInject] private IJSRuntime jsRuntime = default!;

    public bool IsInitialized => true;

    public async Task<string?> GetAccessTokenAsync()
    {
        return await jsRuntime.GetCookie("access_token");
    }
}
