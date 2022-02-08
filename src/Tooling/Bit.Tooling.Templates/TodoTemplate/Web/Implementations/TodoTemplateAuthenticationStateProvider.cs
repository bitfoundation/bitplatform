using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;

namespace TodoTemplate.App.Implementations
{
    public class TodoTemplateAuthenticationStateProvider : AuthenticationStateProvider
    {
        private readonly IJSRuntime _jsRuntime;

        public TodoTemplateAuthenticationStateProvider(IJSRuntime jsRuntime)
        {
            _jsRuntime = jsRuntime;
        }

        public override async Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            var access_token = await _jsRuntime.InvokeAsync<string>("window.localStorage.getItem", "access_token");

            if (string.IsNullOrWhiteSpace(access_token))
            {
                return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
            }

            return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity(ParseTokenClaims(access_token), "Bearer")));
        }

        private IEnumerable<Claim> ParseTokenClaims(string access_token)
        {
            return Jose.JWT.Payload<Dictionary<string, object>>(access_token)
                .Select(keyValue => new Claim(keyValue.Key, keyValue.Value.ToString() ?? string.Empty))
                .ToArray();
        }
    }
}
