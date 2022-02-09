﻿using Microsoft.AspNetCore.Components.Authorization;

namespace TodoTemplate.App.Services.Implementations
{
    public class TodoTemplateAuthenticationStateProvider : AuthenticationStateProvider
    {
        private readonly IJSRuntime _jsRuntime;

        public TodoTemplateAuthenticationStateProvider(IJSRuntime jsRuntime)
        {
            _jsRuntime = jsRuntime;
        }

        public void RaiseAuthenticationStateHasChanged()
        {
            NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
        }

        public override async Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            var access_token = await _jsRuntime.InvokeAsync<string>("todoTemplate.getCookie", "access_token");

            if (string.IsNullOrWhiteSpace(access_token))
            {
                return NotSignedIn();
            }

            var identity = new ClaimsIdentity(claims: ParseTokenClaims(access_token), authenticationType: "Bearer", nameType: "name", roleType: "role");

            return new AuthenticationState(new ClaimsPrincipal(identity));
        }

        AuthenticationState NotSignedIn()
        {
            return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
        }

        private IEnumerable<Claim> ParseTokenClaims(string access_token)
        {
            return Jose.JWT.Payload<Dictionary<string, object>>(access_token)
                .Select(keyValue => new Claim(keyValue.Key, keyValue.Value.ToString() ?? string.Empty))
                .ToArray();
        }
    }
}
