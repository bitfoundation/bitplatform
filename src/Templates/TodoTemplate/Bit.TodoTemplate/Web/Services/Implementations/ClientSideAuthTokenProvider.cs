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
#if BlazorHybrid
        return Preferences.Get("access_token", null);
#else
        return await _jsRuntime.InvokeAsync<string>("App.getCookie", "access_token");
#endif
    }
}
