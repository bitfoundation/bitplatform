//-:cnd:noEmit
using Microsoft.JSInterop;
using Boilerplate.Client.Core.Services;
using Boilerplate.Client.Core.Services.Contracts;

namespace Boilerplate.Server.Web.Services;

/// <summary>
/// The <see cref="ClientSideAuthTokenProvider"/> reads the token from the local storage,
/// but during prerendering, there is no access to localStorage or the stored cookies.
/// However, the cookies are sent automatically in http request and The <see cref="ServerSideAuthTokenProvider"/> provides that token to the application.
/// </summary>
public partial class ServerSideAuthTokenProvider : IAuthTokenProvider
{
    [AutoInject] private IJSRuntime jsRuntime = default!;
    [AutoInject] private IStorageService storageService = default!;
    [AutoInject] private IHttpContextAccessor httpContextAccessor = default!;

    public async Task<string?> GetAccessToken()
    {
        if (jsRuntime.IsInitialized())
        {
            return await storageService.GetItem("access_token");
        }

        return httpContextAccessor.HttpContext?.Request.Cookies["access_token"];
    }
}
