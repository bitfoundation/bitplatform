using TodoTemplate.Shared.Dtos.Account;

namespace TodoTemplate.App.Services.Implementations;

public class AuthenticationService : IAuthenticationService
{
    private readonly HttpClient _httpClient;

    private readonly IJSRuntime _jsRuntime;

    private readonly AppAuthenticationStateProvider _authenticationStateProvider;

    public AuthenticationService(HttpClient httpClient, IJSRuntime jsRuntime,
        AppAuthenticationStateProvider authenticationStateProvider)
    {
        _httpClient = httpClient;
        _jsRuntime = jsRuntime;
        _authenticationStateProvider = authenticationStateProvider;
    }

    public async Task SignIn(SignInRequestDto dto)
    {
        var response = await _httpClient.PostAsJsonAsync("Auth/SignIn", dto, AppJsonContext.Default.SignInRequestDto);

        var result = await response.Content.ReadFromJsonAsync(AppJsonContext.Default.SignInResponseDto);

        await _jsRuntime.InvokeVoidAsync("App.setCookie", "access_token", result!.AccessToken, result.ExpiresIn);

        await _authenticationStateProvider.RaiseAuthenticationStateHasChanged();
    }

    public async Task SignOut()
    {
        await _jsRuntime.InvokeVoidAsync("App.removeCookie", "access_token");

        await _authenticationStateProvider.RaiseAuthenticationStateHasChanged();
    }
}
