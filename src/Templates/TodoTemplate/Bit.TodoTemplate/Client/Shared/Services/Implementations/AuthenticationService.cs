using TodoTemplate.Shared.Dtos.Account;

namespace TodoTemplate.Client.Shared.Services.Implementations;

public partial class AuthenticationService : IAuthenticationService
{
    [AutoInject] private HttpClient _httpClient = default!;

    [AutoInject] private IJSRuntime _jsRuntime = default!;

    [AutoInject] private AppAuthenticationStateProvider _authenticationStateProvider = default!;

    public async Task SignIn(SignInRequestDto dto)
    {
        var response = await _httpClient.PostAsJsonAsync("Auth/SignIn", dto, AppJsonContext.Default.SignInRequestDto);

        var result = await response.Content.ReadFromJsonAsync(AppJsonContext.Default.SignInResponseDto);

#if BlazorHybrid
        Preferences.Set("access_token", result!.AccessToken);
#else
        await _jsRuntime.InvokeVoidAsync("App.setCookie", "access_token", result!.AccessToken, result.ExpiresIn);
#endif

        await _authenticationStateProvider.RaiseAuthenticationStateHasChanged();
    }

    public async Task SignOut()
    {
#if BlazorHybrid
        Preferences.Remove("access_token");
#else
        await _jsRuntime.InvokeVoidAsync("App.removeCookie", "access_token");
#endif

        await _authenticationStateProvider.RaiseAuthenticationStateHasChanged();
    }
}
