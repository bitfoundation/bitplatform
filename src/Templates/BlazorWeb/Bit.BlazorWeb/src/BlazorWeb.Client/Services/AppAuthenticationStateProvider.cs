using System.Text;
using System.Text.Json;
using BlazorWeb.Shared.Dtos.Identity;

namespace BlazorWeb.Client.Services;

public partial class AppAuthenticationStateProvider : AuthenticationStateProvider
{
    [AutoInject] private IAuthTokenProvider _tokenProvider = default!;
    [AutoInject] private HttpClient _httpClient = default!;
    [AutoInject] private IJSRuntime _jsRuntime = default!;

    public async Task RaiseAuthenticationStateHasChanged()
    {
        NotifyAuthenticationStateChanged(Task.FromResult(await GetAuthenticationStateAsync()));
    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        try
        {
            var access_token = await _tokenProvider.GetAccessTokenAsync();

            if (string.IsNullOrWhiteSpace(access_token))
            {
                var refresh_token = await _tokenProvider.GetRefreshTokenAsync();

                if (refresh_token is not null)
                {
                    var refreshTokenResponse = await (await _httpClient.PostAsJsonAsync("Identity/Refresh", new RefreshRequestDto { RefreshToken = refresh_token }, AppJsonContext.Default.RefreshRequestDto))
                        .Content.ReadFromJsonAsync(AppJsonContext.Default.TokenResponseDto);

                    access_token = refreshTokenResponse!.AccessToken;

                    try
                    {
                        await _jsRuntime.InvokeVoidAsync("App.setCookie", "access_token", refreshTokenResponse.AccessToken, refreshTokenResponse.ExpiresIn, true);
                        await _jsRuntime.InvokeVoidAsync("App.setCookie", "refresh_token", refreshTokenResponse.RefreshToken, TokenResponseDto.RefreshTokenExpiresIn, true);
                    }
                    catch (InvalidOperationException) { /* Ignore js runtime exception during pre rendering */ }
                }
            }

            var identity = new ClaimsIdentity(claims: ParseTokenClaims(access_token ?? throw new UnauthorizedException()), authenticationType: "Bearer", nameType: "name", roleType: "role");

            return new AuthenticationState(new ClaimsPrincipal(identity));
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

    private static IEnumerable<Claim> ParseTokenClaims(string access_token)
    {
        return ParseJwt(access_token)
            .Select(keyValue => new Claim(keyValue.Key, keyValue.Value.ToString() ?? string.Empty))
            .ToArray();
    }

    private static Dictionary<string, object> ParseJwt(string access_token)
    {
        // Split the token to get the payload
        string base64UrlPayload = access_token.Split('.')[1];

        // Convert the payload from Base64Url format to Base64
        string base64Payload = ConvertBase64UrlToBase64(base64UrlPayload);

        // Decode the Base64 string to get a JSON string
        string jsonPayload = Encoding.UTF8.GetString(Convert.FromBase64String(base64Payload));

        // Deserialize the JSON string to a dictionary
        var claims = JsonSerializer.Deserialize(jsonPayload, AppJsonContext.Default.DictionaryStringObject)!;

        return claims;
    }

    private static string ConvertBase64UrlToBase64(string base64Url)
    {
        base64Url = base64Url.Replace('-', '+').Replace('_', '/');

        // Adjust base64Url string length for padding
        switch (base64Url.Length % 4)
        {
            case 2:
                base64Url += "==";
                break;
            case 3:
                base64Url += "=";
                break;
        }

        return base64Url;
    }
}
