//-:cnd:noEmit
using BlazorWeb.Shared.Dtos.Identity;

namespace BlazorWeb.Client.Services;

public partial class AuthenticationService : IAuthenticationService
{
    [AutoInject] private HttpClient _httpClient = default!;

    [AutoInject] private IJSRuntime _jsRuntime = default!;

    [AutoInject] private AppAuthenticationStateProvider _authenticationStateProvider = default!;

    public async Task SignIn(SignInRequestDto dto)
    {
        var result = await (await _httpClient.PostAsJsonAsync("Identity/SignIn", dto, AppJsonContext.Default.SignInRequestDto))
            .Content.ReadFromJsonAsync(AppJsonContext.Default.TokenResponseDto);

        await StoreAuthToken(result!, dto.RememberMe);

        await _authenticationStateProvider.RaiseAuthenticationStateHasChanged();
    }

    public async Task StoreAuthToken(TokenResponseDto result, bool rememberMe)
    {
        await _jsRuntime.InvokeVoidAsync("App.setCookie", "access_token", result.AccessToken, result.ExpiresIn, rememberMe);
        await _jsRuntime.InvokeVoidAsync("App.setCookie", "refresh_token", result.RefreshToken, TokenResponseDto.RefreshTokenExpiresIn, rememberMe);
    }

    public async Task SignOut()
    {
        await _jsRuntime.InvokeVoidAsync("App.removeCookie", "access_token");
        await _jsRuntime.InvokeVoidAsync("App.removeCookie", "refresh_token");

        await _authenticationStateProvider.RaiseAuthenticationStateHasChanged();
    }
}
