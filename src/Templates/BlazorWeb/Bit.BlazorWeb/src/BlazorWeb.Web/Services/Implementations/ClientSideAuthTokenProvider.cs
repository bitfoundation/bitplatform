//-:cnd:noEmit
namespace BlazorWeb.Web.Services.Implementations;

public partial class ClientSideAuthTokenProvider : IAuthTokenProvider
{
    [AutoInject] private IJSRuntime _jsRuntime = default!;

    public async Task<string?> GetAcccessTokenAsync()
    {
        return await _jsRuntime.InvokeAsync<string>("App.getCookie", "access_token");
    }
}
