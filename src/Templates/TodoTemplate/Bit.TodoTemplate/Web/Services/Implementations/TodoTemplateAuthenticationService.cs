using TodoTemplate.Shared.Dtos.Account;

namespace TodoTemplate.App.Services.Implementations;

public class TodoTemplateAuthenticationService : ITodoTemplateAuthenticationService
{
    private readonly HttpClient _httpClient;

    private readonly IJSRuntime _jsRuntime;

    private readonly TodoTemplateAuthenticationStateProvider _authenticationStateProvider;

    public TodoTemplateAuthenticationService(HttpClient httpClient, IJSRuntime jsRuntime,
        TodoTemplateAuthenticationStateProvider authenticationStateProvider)
    {
        _httpClient = httpClient;
        _jsRuntime = jsRuntime;
        _authenticationStateProvider = authenticationStateProvider;
    }

    public async Task SignIn(SignInRequestDto dto)
    {
        var response = await _httpClient.PostAsJsonAsync("Auth/SignIn", dto, TodoTemplateJsonContext.Default.SignInRequestDto);

        var result = await response.Content.ReadFromJsonAsync(TodoTemplateJsonContext.Default.SignInResponseDto);

        await _jsRuntime.InvokeVoidAsync("todoTemplate.setCookie", "access_token", result!.AccessToken, result.ExpiresIn);

        await _authenticationStateProvider.RaiseAuthenticationStateHasChanged();
    }

    public async Task SignOut()
    {
        await _jsRuntime.InvokeVoidAsync("todoTemplate.removeCookie", "access_token");

        await _authenticationStateProvider.RaiseAuthenticationStateHasChanged();
    }
}
