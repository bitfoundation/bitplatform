namespace Bit.Websites.Careers.Web.Services.Implementations;

public partial class ClientSideAuthTokenProvider : IAuthTokenProvider
{
    [AutoInject] private IJSRuntime _jsRuntime = default!;

    public async Task<string?> GetAccessTokenAsync()
    {
        return await _jsRuntime.InvokeAsync<string>("App.getCookie", "access_token");
    }
}
