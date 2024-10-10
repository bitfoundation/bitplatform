using Boilerplate.Client.Core.Services.Contracts;

namespace Boilerplate.Tests.Services;

public partial class TestTokenProvider : IAuthTokenProvider
{
    [AutoInject] private IStorageService storageService = default!;

    public async Task<string?> GetAccessToken()
    {
        return await storageService.GetItem("access_token");
    }
}
