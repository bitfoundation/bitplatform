namespace TodoTemplate.App.Services.Implementations;

public partial class ClientSideAuthTokenProvider : IAuthTokenProvider
{
    [AutoInject] readonly IJSRuntime jsRuntime = default!;

    public async Task<string?> GetAcccessToken()
    {
#if BlazorHybrid
        return Preferences.Get("access_token", null);
#else
        return await jsRuntime.InvokeAsync<string>("App.getCookie", "access_token");
#endif
    }
}
