//-:cnd:noEmit
namespace Boilerplate.Client.Core.Services;

public partial class ClientSideAuthTokenProvider : IAuthTokenProvider
{
    [AutoInject] private IStorageService storageService = default!;

    public bool InPrerenderSession => false;

    public async Task<string?> GetAccessTokenAsync()
    {
        return await storageService.GetItem("access_token");
    }
}
