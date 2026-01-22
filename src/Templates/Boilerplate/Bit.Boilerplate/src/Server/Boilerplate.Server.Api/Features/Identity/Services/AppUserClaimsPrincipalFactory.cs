using System.IdentityModel.Tokens.Jwt;
using Boilerplate.Server.Api.Infrastructure.Services;
using Boilerplate.Server.Api.Features.Identity.Models;

namespace Boilerplate.Server.Api.Features.Identity.Services;

public partial class AppUserClaimsPrincipalFactory(UserClaimsService userClaimsService, UserManager<User> userManager, RoleManager<Role> roleManager,
        IOptions<IdentityOptions> optionsAccessor, IConfiguration configuration, IServiceProvider serviceProvider,
        DistributedLockFactory distributedLockProvider,
        IHttpContextAccessor httpContextAccessor)
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

        if (result.Claims.Any(c => c.Type == AppClaimTypes.METHOD) is false)
        {
            string? authenticationMethod = httpContextAccessor.HttpContext!.Items[AppClaimTypes.METHOD]?.ToString() ?? "Password";
            result.AddClaim(new Claim(AppClaimTypes.METHOD, authenticationMethod));
        }

        await RetrieveKeycloakClaims(user, result);

        return result;
    }

    /// <summary>
    /// Retrieves additional claims from Keycloak and adds them to the user's claims, if the user has been externally authenticated via Keycloak.
    /// It also prevents disabled/deleted keycloak users from accessing the application by throwing an UnauthorizedException.
    /// </summary>
    private async Task RetrieveKeycloakClaims(User user, ClaimsIdentity aspnetCoreIdentityClaims)
    {
        if (aspnetCoreIdentityClaims.HasClaim(AppClaimTypes.METHOD, "External") is false)
            return; // User was not authenticated via Keycloak

        var keycloakBaseUrl = configuration["KEYCLOAK_HTTP"] ?? configuration["Authentication:Keycloak:KeycloakUrl"];
        if (string.IsNullOrEmpty(keycloakBaseUrl) is false)
        {
            var keycloakRefreshToken = await UserManager.GetAuthenticationTokenAsync(user, "Keycloak", "refresh_token");
            if (string.IsNullOrEmpty(keycloakRefreshToken) is false)
            {
                var realm = configuration["Authentication:Keycloak:Realm"] ?? throw new InvalidOperationException("Authentication:Keycloak:Realm configuration is required");
                string? keycloakAccessToken = await GetKeycloakAccessToken(user, keycloakRefreshToken, realm);
                var handler = new JwtSecurityTokenHandler();
                var parsedKeycloakAccessToken = handler.ReadJwtToken(keycloakAccessToken);
                var keycloakClaims = parsedKeycloakAccessToken.Claims
                    .Select(claim => new Claim(
                        JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.TryGetValue(claim.Type, out var mapped)
                            ? mapped
                            : claim.Type,
                        claim.Value))
                    .ToList();

                foreach (var claim in keycloakClaims.Where(c => aspnetCoreIdentityClaims.HasClaim(c.Type, c.Value) is false))
                    aspnetCoreIdentityClaims.AddClaim(claim);
            }
        }
    }

    private async Task<string> GetKeycloakAccessToken(User user, string? keycloakRefreshToken, string realm)
    {
        var keycloakTokenExpiryDate = DateTimeOffset.Parse(await UserManager.GetAuthenticationTokenAsync(user, "Keycloak", "expires_at") ?? throw new InvalidOperationException("expires_at token is missing"));

        if (DateTimeOffset.UtcNow < keycloakTokenExpiryDate)
            return (await UserManager.GetAuthenticationTokenAsync(user, "Keycloak", "access_token"))!;

        await using var distributedLock = await distributedLockProvider($"Boilerplate-Locks-KeycloakTokenRefresh-{user.Id}").AcquireAsync(TimeSpan.FromSeconds(10));

        keycloakTokenExpiryDate = DateTimeOffset.Parse(await UserManager.GetAuthenticationTokenAsync(user, "Keycloak", "expires_at") ?? throw new InvalidOperationException("expires_at token is missing"));

        if (DateTimeOffset.UtcNow < keycloakTokenExpiryDate) // Token was refreshed while waiting for the lock by another request
            return (await UserManager.GetAuthenticationTokenAsync(user, "Keycloak", "access_token"))!;

        var httpClient = serviceProvider.GetRequiredService<IHttpClientFactory>().CreateClient("Keycloak");
        var refreshRequestPayload = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "grant_type", "refresh_token" },
            { "client_id", configuration["Authentication:Keycloak:ClientId"]! },
            { "client_secret", configuration["Authentication:Keycloak:ClientSecret"]! },
            { "refresh_token", keycloakRefreshToken! }
        });
        using var response = await httpClient.PostAsync($"realms/{realm}/protocol/openid-connect/token", refreshRequestPayload);
        if (response.IsSuccessStatusCode is false)
        {
            var errorDetails = await response.Content.ReadFromJsonAsync<Dictionary<string, object?>>();
            if (errorDetails?.Any(i => i.Key == "error" && i.Value?.ToString() is "invalid_grant") is true)
                throw new UnauthorizedException(errorDetails["error_description"]!.ToString()!).WithData(errorDetails);
            throw new InvalidOperationException("Failed to refresh Keycloak access token").WithData(errorDetails ?? []);
        }
        var responseBody = await response.Content.ReadFromJsonAsync<JsonElement>();
        var keycloakAccessToken = responseBody!.GetProperty("access_token").GetString()!;
        keycloakRefreshToken = responseBody!.GetProperty("refresh_token").GetString();
        var expiresIn = responseBody!.GetProperty("expires_in").GetInt32();
        var newExpiryDate = DateTimeOffset.UtcNow.AddSeconds(expiresIn);
        await UserManager.SetAuthenticationTokenAsync(user, "Keycloak", "access_token", keycloakAccessToken!);
        await UserManager.SetAuthenticationTokenAsync(user, "Keycloak", "refresh_token", keycloakRefreshToken!);
        await UserManager.SetAuthenticationTokenAsync(user, "Keycloak", "expires_at", newExpiryDate.ToString("O"));

        return keycloakAccessToken;
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
