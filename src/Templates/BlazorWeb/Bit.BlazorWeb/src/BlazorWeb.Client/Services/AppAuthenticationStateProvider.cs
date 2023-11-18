namespace BlazorWeb.Client.Services;

public partial class AppAuthenticationStateProvider : AuthenticationStateProvider
{
    [AutoInject] private IAuthTokenProvider _tokenProvider = default!;
    [AutoInject] private HttpClient _httpClient = default!;

    public async Task RaiseAuthenticationStateHasChanged()
    {
        NotifyAuthenticationStateChanged(Task.FromResult(await GetAuthenticationStateAsync()));
    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var access_token = await _tokenProvider.GetAccessTokenAsync();

        if (string.IsNullOrWhiteSpace(access_token)) return NotSignedIn();

        try
        {
            var userDto = await _httpClient.GetFromJsonAsync("User/GetCurrentUser", AppJsonContext.Default.UserDto);

            var claimPrincipal = new ClaimsPrincipal(new ClaimsIdentity(claims: new[]
            {
                new Claim(ClaimTypes.NameIdentifier, userDto!.Id.ToString()),
                new Claim(ClaimTypes.Name, userDto.UserName!)
            }, authenticationType: "Bearer", nameType: "name", roleType: "role"));

            return new AuthenticationState(claimPrincipal);
        }
        catch (UnauthorizedException)
        {
            return NotSignedIn();
        }
    }

    private static AuthenticationState NotSignedIn()
    {
        return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
    }
}
