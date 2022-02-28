namespace TodoTemplate.App.Services.Implementations;

public class ClientSideAuthTokenProvider : IAuthTokenProvider
{
    private readonly IJSRuntime _jsRuntime;

    public ClientSideAuthTokenProvider(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    public async Task<string?> GetAcccessToken()
    {
        return await _jsRuntime.InvokeAsync<string>("todoTemplate.getCookie", "access_token");
    }
}
