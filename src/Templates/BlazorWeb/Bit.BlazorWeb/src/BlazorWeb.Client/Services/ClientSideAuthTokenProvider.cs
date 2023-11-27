//-:cnd:noEmit
namespace BlazorWeb.Client.Services;

public partial class ClientSideAuthTokenProvider : IAuthTokenProvider
{
    [AutoInject] private IStorageService storageService = default!;

    public bool IsInitialized => true;

    public async Task<string?> GetAccessTokenAsync()
    {
        return await storageService.GetItem("access_token");
    }
}
