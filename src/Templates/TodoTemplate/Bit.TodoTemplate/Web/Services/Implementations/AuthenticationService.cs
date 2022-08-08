using TodoTemplate.Shared.Dtos.Account;

namespace TodoTemplate.App.Services.Implementations;

public partial class AuthenticationService : IAuthenticationService
{
    [AutoInject] private HttpClient httpClient = default!;

    [AutoInject] private IJSRuntime jsRuntime = default!;

    [AutoInject] private AppAuthenticationStateProvider authenticationStateProvider = default!;

    public async Task SignIn(SignInRequestDto dto)
    {
        var response = await httpClient.PostAsJsonAsync("Auth/SignIn", dto, AppJsonContext.Default.SignInRequestDto);

        var result = await response.Content.ReadFromJsonAsync(AppJsonContext.Default.SignInResponseDto);

#if BlazorHybrid
        Preferences.Set("access_token", result!.AccessToken);
#else
        await jsRuntime.InvokeVoidAsync("App.setCookie", "access_token", result!.AccessToken, result.ExpiresIn);
#endif

        await authenticationStateProvider.RaiseAuthenticationStateHasChanged();
    }

    public async Task SignOut()
    {
#if BlazorHybrid
        Preferences.Remove("access_token");
#else
        await jsRuntime.InvokeVoidAsync("App.removeCookie", "access_token");
#endif

        await authenticationStateProvider.RaiseAuthenticationStateHasChanged();
    }
}
