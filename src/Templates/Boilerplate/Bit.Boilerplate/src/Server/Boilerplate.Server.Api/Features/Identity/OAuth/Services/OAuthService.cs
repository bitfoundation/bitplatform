//+:cnd:noEmit
using System.Security.Cryptography;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.WebUtilities;
using Boilerplate.Shared.Features.Identity.OAuth;
using Boilerplate.Shared.Features.Identity.OAuth.Dtos;
using Boilerplate.Server.Api.Features.Identity.OAuth.Models;

namespace Boilerplate.Server.Api.Features.Identity.OAuth.Services;

/// <summary>
/// The authorization server's own logic, shared by the anonymous protocol endpoints and the consent page's controller
/// so that a rule cannot hold in one and not the other.
/// </summary>
public partial class OAuthService(AppDbContext dbContext,
    OAuthClientResolver clientResolver,
    ServerApiSettings appSettings,
    IHttpContextAccessor httpContextAccessor,
    TimeProvider timeProvider,
    ILogger<OAuthService> logger)
{
    private HttpRequest Request => httpContextAccessor.HttpContext!.Request;

    public string Issuer => Request.GetIssuer();

    /// <summary>
    /// Everything checkable before a user is involved. The two failure kinds are split because until the client and its
    /// redirect uri are known good there is nowhere safe to send the browser.
    /// </summary>
    public async Task<OAuthValidationResult> Validate(OAuthAuthorizeRequestDto request, CancellationToken cancellationToken)
    {
        var client = await clientResolver.Resolve(request.ClientId, cancellationToken);

        if (client is null)
            return OAuthValidationResult.Unredirectable("invalid_client");

        if (client.IsRegisteredRedirectUri(request.RedirectUri) is false)
            return OAuthValidationResult.Unredirectable("invalid_redirect_uri");

        // Past this line the redirect uri is trustworthy, so errors can and should go back to the client.

        if (string.Equals(request.ResponseType, "code", StringComparison.Ordinal) is false)
            return OAuthValidationResult.Redirectable(client, "unsupported_response_type");

        // OAuth 2.1 makes PKCE mandatory; `plain` sends the verifier through the browser, which is what it exists to avoid.
        // The shape is checked here rather than at the insert: RFC 7636 §4.2 bounds it at 43-128 base64url characters,
        // and a longer one would otherwise pass consent and fail on OAuthAuthorizationCode.CodeChallenge as a 500.
        if (string.Equals(request.CodeChallengeMethod, "S256", StringComparison.Ordinal) is false
            || IsAcceptableCodeChallenge(request.CodeChallenge) is false)
        {
            return OAuthValidationResult.Redirectable(client, "invalid_request");
        }

        if (OAuthResources.TryNormalize(Issuer, request.Resource, out var resource) is false)
        {
            // Usually one flow spread over two origins rather than a bad request, and invalid_target alone shows neither.
            logger.LogError("Refusing an authorization request for resource {Resource}: this request arrived at {Issuer}, which serves {KnownResources}.",
                            request.Resource, Issuer, string.Join(", ", OAuthResources.All(Issuer)));

            return OAuthValidationResult.Redirectable(client, "invalid_target");
        }

        // RFC 6749 §3.3: an omitted scope means "whatever this resource is for", not "nothing"; one that names nothing
        // we recognise is refused.
        var scopes = string.IsNullOrWhiteSpace(request.Scope)
            ? OAuthResources.ScopesFor(resource)
            : OAuthScopes.Parse(request.Scope);

        if (scopes is { Length: 0 })
            return OAuthValidationResult.Redirectable(client, "invalid_scope");

        //#if (multitenant == true)
        // Told apart from an absent one: a client that names a workspace and gets a tenantless grant is worse off than
        // one that is told its request was wrong.
        if (string.IsNullOrWhiteSpace(request.TenantId) is false && Guid.TryParse(request.TenantId, out _) is false)
            return OAuthValidationResult.Redirectable(client, "invalid_request");
        //#endif

        return OAuthValidationResult.Valid(client, resource, scopes);
    }

    /// <summary>Narrows scopes to what this user can grant: nobody delegates a feature they do not hold.</summary>
    public static (string[] Granted, string[] Denied) NarrowScopes(string[] requestedScopes, ClaimsPrincipal user)
    {
        var userFeatures = user.FindAll(AppClaimTypes.FEATURES).Select(claim => claim.Value).ToHashSet(StringComparer.Ordinal);

        var granted = requestedScopes
            .Where(scope => OAuthScopes.FeaturesFor([scope]).All(userFeatures.Contains))
            .ToArray();

        return (granted, [.. requestedScopes.Except(granted, StringComparer.Ordinal)]);
    }

    /// <summary>Issues the code and returns where the browser should go. It is returned once and stored only hashed.</summary>
    public async Task<string> IssueCode(OAuthValidationResult validated,
        OAuthAuthorizeRequestDto request,
        string[] grantedScopes,
        ClaimsPrincipal user,
        Guid? tenantId,
        CancellationToken cancellationToken)
    {
        var code = GenerateCode();
        var now = timeProvider.GetUtcNow();

        // Out here because NonAsyncEFCoreMethodsUsageAnalyzer reads DateTimeOffset.Add inside a DbSet call as DbSet.Add.
        var expiresOn = now.Add(appSettings.OAuth.AuthorizationCodeLifetime).ToUnixTimeSeconds();

        await dbContext.OAuthAuthorizationCodes.AddAsync(new OAuthAuthorizationCode
        {
            Id = Guid.CreateSequentialGuid(),
            CodeHash = Hash(code),
            ClientId = validated.Client!.ClientId,
            RedirectUri = request.RedirectUri!,
            Resource = validated.Resource!,
            Scope = string.Join(' ', grantedScopes),
            CodeChallenge = request.CodeChallenge!,
            UserId = user.GetUserId(),
            Amr = string.Join(' ', user.FindAll(AppClaimTypes.AMR).Select(claim => claim.Value)),
            //#if (multitenant == true)
            TenantId = tenantId,
            //#endif
            ExpiresOn = expiresOn
        }, cancellationToken);

        // Expired codes are OAuthRetentionJobRunner's: sweeping here is a table scan per approval, and a consumed code
        // has to outlive its expiry anyway so a replay has a session to revoke.
        await dbContext.SaveChangesAsync(cancellationToken);

        return BuildRedirectUrl(request.RedirectUri!, new()
        {
            ["code"] = code,
            ["state"] = request.State,
            // RFC 9207: clients compare this against the issuer they discovered, which stops one server's response
            // being replayed at another.
            ["iss"] = Issuer
        });
    }

    public string BuildErrorRedirectUrl(string redirectUri, string error, string? state)
    {
        return BuildRedirectUrl(redirectUri, new()
        {
            ["error"] = error,
            ["state"] = state,
            ["iss"] = Issuer
        });
    }

    private static string BuildRedirectUrl(string redirectUri, Dictionary<string, string?> parameters)
    {
        foreach (var parameter in parameters.Where(p => string.IsNullOrEmpty(p.Value)).ToArray())
        {
            parameters.Remove(parameter.Key);
        }

        return QueryHelpers.AddQueryString(redirectUri, parameters!);
    }

    /// <summary>
    /// Looks the code up, proves the presenting client is the one it was issued to, and consumes it. A code travels
    /// through the browser once, so a second attempt is theft: OAuth 2.1 §4.1.3 destroys the session it minted.
    /// </summary>
    public async Task<OAuthAuthorizationCode> ConsumeCode(string? code,
        string? clientId,
        string? redirectUri,
        string? codeVerifier,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(codeVerifier))
            throw new BadRequestException().WithData("Reason", "invalid_request");

        var stored = await dbContext.OAuthAuthorizationCodes
            .FirstOrDefaultAsync(existing => existing.CodeHash == Hash(code), cancellationToken)
            ?? throw new BadRequestException().WithData("Reason", "invalid_grant");

        if (stored.ConsumedOn is not null)
        {
            logger.LogWarning("An already consumed authorization code was presented for client {ClientId}. Revoking the session it minted.", stored.ClientId);

            if (stored.UserSessionId is not null)
            {
                await dbContext.UserSessions.Where(session => session.Id == stored.UserSessionId).ExecuteDeleteAsync(cancellationToken);
            }

            throw new BadRequestException().WithData("Reason", "invalid_grant");
        }

        if (stored.ExpiresOn < timeProvider.GetUtcNow().ToUnixTimeSeconds())
            throw new BadRequestException().WithData("Reason", "invalid_grant");

        // Bound to the client and redirect uri it was issued for, so a code intercepted from one client's callback is
        // worthless anywhere else.
        if (string.Equals(stored.ClientId, clientId, StringComparison.Ordinal) is false
            || string.Equals(stored.RedirectUri, redirectUri, StringComparison.Ordinal) is false)
        {
            throw new BadRequestException().WithData("Reason", "invalid_grant");
        }

        if (IsWellFormedCodeVerifier(codeVerifier) is false)
            throw new BadRequestException().WithData("Reason", "invalid_grant");

        if (string.Equals(stored.CodeChallenge, ComputeCodeChallenge(codeVerifier), StringComparison.Ordinal) is false)
            throw new BadRequestException().WithData("Reason", "invalid_grant");

        stored.ConsumedOn = timeProvider.GetUtcNow().ToUnixTimeSeconds();

        return stored;
    }

    /// <summary>RFC 7636 S256: base64url of the SHA-256 of the ASCII verifier, unpadded.</summary>
    public static string ComputeCodeChallenge(string codeVerifier)
    {
        return WebEncoders.Base64UrlEncode(SHA256.HashData(Encoding.ASCII.GetBytes(codeVerifier)));
    }

    /// <summary>
    /// RFC 7636 §4.1: 43-128 characters of the unreserved set. Guards against a client picking a verifier short enough
    /// to brute force back from the challenge in the authorization request.
    /// </summary>
    public static bool IsWellFormedCodeVerifier(string codeVerifier)
    {
        if (codeVerifier.Length is < 43 or > 128)
            return false;

        return codeVerifier.All(character => char.IsAsciiLetterOrDigit(character) || character is '-' or '.' or '_' or '~');
    }

    /// <summary>
    /// Anything absolute except plaintext http to another machine, which puts the code on the wire in the clear, and
    /// the schemes a browser runs rather than leaves for - <c>javascript:</c> would execute in this app's own origin,
    /// code and all. Private-use schemes and loopback listeners are legitimate for native apps (RFC 8252). Here rather
    /// than on the registration service because a metadata document's uris need the same rule.
    /// </summary>
    public static bool IsAcceptableRedirectUri(string redirectUri)
    {
        // Registrations are stored space delimited, so a uri containing a space comes back as two.
        if (redirectUri.Length > 2048 || redirectUri.Any(char.IsWhiteSpace))
            return false;

        if (Uri.TryCreate(redirectUri, UriKind.Absolute, out var uri) is false)
            return false;

        if (string.IsNullOrEmpty(uri.Fragment) is false)
            return false; // RFC 6749 §3.1.2: a redirect uri must not carry a fragment.

        if (uri.Scheme is "javascript" or "data" or "vbscript" or "file" or "blob" or "about")
            return false;

        if (uri.Scheme is "http")
            return uri.IsLoopback;

        return true;
    }

    /// <summary>RFC 7636 §4.1: 43-128 characters of base64url. An S256 challenge is always exactly 43.</summary>
    private static bool IsAcceptableCodeChallenge(string? codeChallenge)
    {
        if (codeChallenge is not { Length: >= 43 and <= 128 })
            return false;

        return codeChallenge.All(c => char.IsAsciiLetterOrDigit(c) || c is '-' or '_' or '.' or '~');
    }

    private static string GenerateCode()
    {
        return WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(32));
    }

    private static string Hash(string code)
    {
        return WebEncoders.Base64UrlEncode(SHA256.HashData(Encoding.UTF8.GetBytes(code)));
    }
}

/// <summary>The outcome of validating an authorization request, and whether an error can be sent to a redirect uri.</summary>
public partial class OAuthValidationResult
{
    public OAuthClient? Client { get; private init; }

    public string? Resource { get; private init; }

    public string[] Scopes { get; private init; } = [];

    /// <summary>An OAuth error code, or null when the request is valid.</summary>
    public string? Error { get; private init; }

    /// <summary>False when the client's identity or redirect uri is the problem - redirecting then is an open redirect.</summary>
    [MemberNotNullWhen(true, nameof(Client))]
    public bool CanRedirect => Client is not null;

    [MemberNotNullWhen(false, nameof(Error))]
    [MemberNotNullWhen(true, nameof(Client), nameof(Resource))]
    public bool IsValid => Error is null;

    public static OAuthValidationResult Valid(OAuthClient client, string resource, string[] scopes) =>
        new() { Client = client, Resource = resource, Scopes = scopes };

    public static OAuthValidationResult Redirectable(OAuthClient client, string error) =>
        new() { Client = client, Error = error };

    public static OAuthValidationResult Unredirectable(string error) =>
        new() { Error = error };
}
