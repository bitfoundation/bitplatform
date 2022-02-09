using TodoTemplate.Shared.Dtos.Account;

namespace TodoTemplate.App.Implementations
{
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

        public async Task SignIn(RequestTokenDto dto)
        {
            var response = await _httpClient.PostAsync("User/Token", JsonContent.Create(dto));

            var result = await response.Content.ReadFromJsonAsync<ResponseTokenDto>();

            await _jsRuntime.InvokeVoidAsync("window.localStorage.setItem", "access_token", result?.Token);

            await _authenticationStateProvider.GetAuthenticationStateAsync();
        }

        public async Task SignOut()
        {
            await _jsRuntime.InvokeVoidAsync("window.localStorage.removeItem", "access_token");

            await _authenticationStateProvider.GetAuthenticationStateAsync();
        }
    }
}
