namespace TodoTemplate.App.Services.Implementations;

public class ClientSideTokenProvider : IAuthTokenProvider
{
    private readonly IJSRuntime _jsRuntime;

    public ClientSideTokenProvider(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    public async Task<string?> GetAcccessToken()
    {
        return await _jsRuntime.InvokeAsync<string>("todoTemplate.getCookie", "access_token");
    }
}
