using System.Text;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.BearerToken;

namespace Boilerplate.Server.Api.Services.Identity;

public class AppBearerTokenOptionsSetup(IConfiguration configuration,
    IServiceProvider serviceProvider) : IPostConfigureOptions<BearerTokenOptions>
{
    public void PostConfigure(string? name, BearerTokenOptions options)
    {
        options.BearerTokenProtector = ActivatorUtilities.CreateInstance<AppJwtSecureDataFormat>(serviceProvider, "AccessToken");
        options.RefreshTokenProtector = ActivatorUtilities.CreateInstance<AppJwtSecureDataFormat>(serviceProvider, "RefreshToken");

        options.Events = new()
        {
            OnMessageReceived = async context =>
            {
                // The server accepts the accessToken from either the authorization header, the cookie, or the request URL query string
                context.Token ??= context.Request.Query.ContainsKey("access_token") ? context.Request.Query["access_token"] : context.Request.Cookies["access_token"];
            }
        };

        configuration.GetRequiredSection("Identity").Bind(options);
    }
}


/// <summary>
/// Stores bearer token in jwt format
/// </summary>
public partial class AppJwtSecureDataFormat
    : ISecureDataFormat<AuthenticationTicket>
{
    private readonly string tokenType;
    private readonly IHostEnvironment env;
    private readonly ServerApiSettings appSettings;
    private readonly ILogger<AppJwtSecureDataFormat> logger;
    private readonly IHttpContextAccessor httpContextAccessor;
    private readonly TokenValidationParameters validationParameters;

    public AppJwtSecureDataFormat(ServerApiSettings appSettings,
        IHostEnvironment env,
        ILogger<AppJwtSecureDataFormat> logger,
        IHttpContextAccessor httpContextAccessor,
        string tokenType)
    {
        this.env = env;
        this.logger = logger;
        this.tokenType = tokenType;
        this.appSettings = appSettings;
        this.httpContextAccessor = httpContextAccessor;

        validationParameters = new()
        {
            ClockSkew = TimeSpan.Zero,
            RequireSignedTokens = true,

            ValidateIssuerSigningKey = env.IsDevelopment() is false,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(appSettings.Identity.JwtIssuerSigningKeySecret)),

            RequireExpirationTime = true,
            ValidateLifetime = tokenType is "AccessToken", /* IdentityController.Refresh will validate expiry itself while refreshing the token */

            ValidateAudience = true,
            ValidAudience = appSettings.Identity.Audience,

            ValidateIssuer = true,
            ValidIssuer = appSettings.Identity.Issuer,

            AuthenticationType = IdentityConstants.BearerScheme
        };
    }

    public AuthenticationTicket? Unprotect(string? protectedText) => Unprotect(protectedText, null);

    public AuthenticationTicket? Unprotect(string? protectedText, string? purpose)
    {
        try
        {
            if (string.IsNullOrEmpty(protectedText))
            {
                return Anonymous();
            }

            var handler = new JwtSecurityTokenHandler();
            var principal = handler.ValidateToken(protectedText, validationParameters, out var validToken);

            var validJwt = (JwtSecurityToken)validToken;
            var properties = new AuthenticationProperties() { ExpiresUtc = validJwt.ValidTo };

            var identity = new ClaimsIdentity(principal.Identity, principal.Claims, IdentityConstants.BearerScheme, ClaimTypes.NameIdentifier, ClaimTypes.Role);

            if (principal.IsInRole(AppRoles.SuperAdmin))
            {
                foreach (var feat in AppFeatures.GetSuperAdminFeatures())
                {
                    identity.AddClaim(new Claim(AppClaimTypes.FEATURES, feat.Value));
                }
            }

            var result = new ClaimsPrincipal(identity);

            var data = new AuthenticationTicket(result, properties: properties, IdentityConstants.BearerScheme);

            return data;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to unprotect the {TokenType}.", tokenType);

            return Anonymous();
        }
    }

    private static AuthenticationTicket Anonymous()
    {
        return new AuthenticationTicket(new ClaimsPrincipal(new ClaimsIdentity()), string.Empty);
    }

    public string Protect(AuthenticationTicket data) => Protect(data, null);

    public string Protect(AuthenticationTicket data, string? purpose)
    {
        var jwtSecurityTokenHandler = new JwtSecurityTokenHandler();

        var securityToken = jwtSecurityTokenHandler
            .CreateJwtSecurityToken(new SecurityTokenDescriptor
            {
                Issuer = appSettings.Identity.Issuer,
                Audience = appSettings.Identity.Audience,
                IssuedAt = DateTimeOffset.UtcNow.DateTime,
                Expires = data.Properties.ExpiresUtc!.Value.UtcDateTime,
                SigningCredentials = new SigningCredentials(validationParameters.IssuerSigningKey, SecurityAlgorithms.HmacSha512),
                Subject = new ClaimsIdentity(data.Principal.Claims),
            });

        var encodedJwt = jwtSecurityTokenHandler.WriteToken(securityToken);

        if (tokenType is "AccessToken")
        {
            var context = httpContextAccessor?.HttpContext ?? throw new InvalidOperationException("HttpContext is not available.");

            // Set access token cookie for pre-rendering.
            context.Response.Cookies.Append(
                "access_token",
                encodedJwt,
                new()
                {
                    HttpOnly = true,
                    Secure = env.IsDevelopment() is false,
                    SameSite = SameSiteMode.Strict,
                    IsEssential = true,
                    Path = "/",
                    Domain = context.Request.GetBaseUrl().Host,
                    Expires = data.Properties.ExpiresUtc!.Value.UtcDateTime
                });
        }

        return encodedJwt;
    }
}
