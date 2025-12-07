using System.IdentityModel.Tokens.Jwt;
using Boilerplate.Server.Api.Models.Identity;

namespace Boilerplate.Server.Api.Services.Identity;

public partial class AppUserClaimsPrincipalFactory(UserClaimsService userClaimsService, UserManager<User> userManager, RoleManager<Role> roleManager,
        IOptions<IdentityOptions> optionsAccessor, IConfiguration configuration, IServiceProvider serviceProvider)
    : UserClaimsPrincipalFactory<User, Role>(userManager, roleManager, optionsAccessor)
{
    /// <summary>
    /// These claims will be included in both the access and refresh tokens only if the successful sign-in happens during the current HTTP request lifecycle.
    /// </summary>
    public List<Claim> SessionClaims { get; set; } = [];

    protected override async Task<ClaimsIdentity> GenerateClaimsAsync(User user)
    {
        var result = await GenerateClaims(user);

        foreach (var sessionClaim in SessionClaims)
        {
            if (result.HasClaim(sessionClaim.Type, sessionClaim.Value) is false)
                result.AddClaim(sessionClaim);
        }

        await RetrieveKeycloakClaims(user, result);

        return result;
    }

    /// <summary>
    /// Retrieves additional claims from Keycloak and adds them to the user's claims.
    /// </summary>
    private async Task RetrieveKeycloakClaims(User user, ClaimsIdentity result)
    {
        var keycloakBaseUrl = configuration["KEYCLOAK_HTTP"];
        if (string.IsNullOrEmpty(keycloakBaseUrl) is false)
        {
            var keycloakRefreshToken = await UserManager.GetAuthenticationTokenAsync(user, "EnterpriseSso", "refresh_token");
            if (string.IsNullOrEmpty(keycloakRefreshToken) is false)
            {
                var keycloakTokenExpiryDate = DateTimeOffset.Parse(await UserManager.GetAuthenticationTokenAsync(user, "EnterpriseSso", "expires_at") ?? throw new InvalidOperationException("expires_at token is missing"));
                var keycloakAccessToken = await UserManager.GetAuthenticationTokenAsync(user, "EnterpriseSso", "access_token") ?? throw new InvalidOperationException("access_token token is missing");
                if (DateTimeOffset.UtcNow >= keycloakTokenExpiryDate)
                {
                    var httpClient = serviceProvider.GetRequiredService<IHttpClientFactory>().CreateClient("Keycloak");
                    var refreshRequestPayload = new FormUrlEncodedContent(new Dictionary<string, string>
                    {
                        { "grant_type", "refresh_token" },
                        { "client_id", configuration["Authentication:EnterpriseSso:ClientId"]! },
                        { "client_secret", configuration["Authentication:EnterpriseSso:ClientSecret"]! },
                        { "refresh_token", keycloakRefreshToken }
                    });
                    using var response = await httpClient.PostAsync($"realms/demo/protocol/openid-connect/token", refreshRequestPayload);
                    if (response.IsSuccessStatusCode is false)
                    {
                        var errorDetails = await response.Content.ReadFromJsonAsync<Dictionary<string, object?>>();
                        if (errorDetails?.Any(i => i.Key == "error" && i.Value?.ToString() is "invalid_grant") is true)
                            throw new UnauthorizedException(errorDetails["error_description"]!.ToString()!).WithData(errorDetails);
                        throw new InvalidOperationException("Failed to refresh Keycloak access token").WithData(errorDetails ?? []);
                    }
                    var responseBody = await response.Content.ReadFromJsonAsync<JsonElement>();
                    keycloakAccessToken = responseBody!.GetProperty("access_token").GetString();
                }
                var handler = new JwtSecurityTokenHandler();
                var parsedKeycloakAccessToken = handler.ReadJwtToken(keycloakAccessToken);
                var keycloakClaims = parsedKeycloakAccessToken.Claims
                    .Select(claim => new Claim(
                        JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.TryGetValue(claim.Type, out var mapped)
                            ? mapped
                            : claim.Type,
                        claim.Value))
                    .ToList();

                foreach (var claim in keycloakClaims)
                {
                    result.AddClaim(claim);
                }
            }
        }
    }

    /// <summary>
    /// aspnetcore identity's code to retrieve claims is not performant enough,
    /// because it doesn't have access to navigation properties and has to query the database for user claims, user roles and role claims separately,
    /// while we use <see cref="UserClaimsService.GetClaims(Guid,CancellationToken)"/> to retrieve all claims in a single query.
    /// The original code borrowed from https://github.com/dotnet/aspnetcore/blob/main/src/Identity/Extensions.Core/src/UserClaimsPrincipalFactory.cs#L71
    /// </summary>
    private async Task<ClaimsIdentity> GenerateClaims(User user)
    {
        var userId = user.Id.ToString();
        var userName = user.UserName;
        var id = new ClaimsIdentity("Identity.Application", // REVIEW: Used to match Application scheme
            Options.ClaimsIdentity.UserNameClaimType,
            Options.ClaimsIdentity.RoleClaimType);
        id.AddClaim(new Claim(Options.ClaimsIdentity.UserIdClaimType, userId));
        id.AddClaim(new Claim(Options.ClaimsIdentity.UserNameClaimType, userName!));
        var email = user.Email;
        if (string.IsNullOrEmpty(email) is false)
        {
            id.AddClaim(new Claim(Options.ClaimsIdentity.EmailClaimType, email));
        }
        id.AddClaim(new Claim(Options.ClaimsIdentity.SecurityStampClaimType, user.SecurityStamp!));

        foreach (var claim in await userClaimsService.GetClaims(user.Id, default))
        {
            if (id.HasClaim(claim.Type, claim.Value) is false)
                id.AddClaim(new(claim.Type, claim.Value));
        }

        return id;
    }
}
