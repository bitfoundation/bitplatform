//+:cnd:noEmit
// [mirror] the claims a signed-in user carries - keep in sync with:
// - src/Server/Boilerplate.Server.Api/Features/Identity/Services/AppUserClaimsPrincipalFactory.cs
using System.Security.Claims;
using System.Text;
using System.Text.Json;

namespace Boilerplate.Workbench.Infrastructure.Services.StandIns;

/// <summary>
/// The claims the app's server issues the view's demo user, with the role's features and the session's own claims (the
/// server's IdentityController adds those, and RoleClaimConfiguration says who has unlimited privileged sessions), and the
/// access token that carries them. The ids are fixed, so they come out the same on every run.
/// </summary>
public static class WorkbenchClaims
{
    /// <summary>The session of the device the view stands for: the settings page's current session, which can't be revoked.</summary>
    public static readonly Guid CurrentSessionId = new(1, 0, 0, new byte[8]);

    //#if (multitenant == true)
    /// <summary>The tenant every demo user is signed into.</summary>
    public static readonly Guid TenantId = new(1, 0, 0, new byte[8]);
    //#endif

    /// <summary>The id of the demo user at this index.</summary>
    public static Guid UserIdOf(int user) => new(user + 1, 0, 0, new byte[8]);

    /// <summary>The demo user an id belongs to, or null.</summary>
    public static WorkbenchDemoUser? UserOf(Guid id) => Enumerable.Range(0, WorkbenchDemoUsers.All.Count).Where(index => UserIdOf(index) == id).Select(WorkbenchDemoUsers.Find).FirstOrDefault();

    /// <summary>The scenario's user's claims; none when the view is signed out.</summary>
    public static IReadOnlyList<Claim> Of(WorkbenchScenario scenario)
    {
        if (scenario.Auth is WorkbenchAuthState.SignedOut)
            return [];

        var user = WorkbenchDemoUsers.Find(scenario.User);
        var features = user.Role is AppRoles.Demo ? AppFeatures.GetDemoFeatures() : AppFeatures.GetRoleImpliedFeatures(role => role == user.Role);

        // The seeded admins have unlimited privileged sessions (RoleClaimConfiguration); anyone else gets the server's MaxPrivilegedSessionsCount.
        //#if (multitenant == true)
        var maxPrivilegedSessions = user.Role is AppRoles.GlobalAdmin or AppRoles.TenantAdmin ? AppClaimTypes.UNLIMITED_PRIVILEGED_SESSIONS : 3;
        //#else
        //#if (IsInsideProjectTemplate == true)
        /*
        //#endif
        var maxPrivilegedSessions = user.Role is AppRoles.GlobalAdmin ? AppClaimTypes.UNLIMITED_PRIVILEGED_SESSIONS : 3;
        //#if (IsInsideProjectTemplate == true)
        */
        //#endif
        //#endif

        return
        [
            new(ClaimTypes.NameIdentifier, UserIdOf(scenario.User).ToString()),
            new(ClaimTypes.Name, user.Email),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Role, user.Role),
            new(AppClaimTypes.SESSION_ID, CurrentSessionId.ToString()),
            new(AppClaimTypes.METHOD, "Password"),
            new(AppClaimTypes.PRIVILEGED_SESSION, "true"),
            new(AppClaimTypes.MAX_PRIVILEGED_SESSIONS, maxPrivilegedSessions.ToString(CultureInfo.InvariantCulture)),
            //#if (multitenant == true)
            new(AppClaimTypes.TENANT_ID, TenantId.ToString()),
            //#endif
            .. features.Select(feature => new Claim(AppClaimTypes.FEATURES, feature.Value))
        ];
    }

    /// <summary>
    /// An access token of these claims, the way the app's client reads one (IAuthTokenProvider.ParseAccessToken): good for an
    /// hour, elevated until <paramref name="elevatedUntil"/> when given, and unsigned, as nothing in the Workbench checks a
    /// signature.
    /// </summary>
    public static string AccessToken(IEnumerable<Claim> claims, DateTimeOffset now, DateTimeOffset? elevatedUntil)
    {
        var payload = claims.GroupBy(claim => claim.Type)
                            .ToDictionary(group => group.Key, group => group.Count() is 1 ? (object)group.First().Value : group.Select(claim => claim.Value).ToArray());

        payload["exp"] = now.AddHours(1).ToUnixTimeSeconds();

        if (elevatedUntil is { } until)
        {
            payload[AppClaimTypes.ELEVATED_SESSION] = until.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture);
        }

        return $"{Base64Url("""{"alg":"none","typ":"JWT"}""")}.{Base64Url(JsonSerializer.Serialize(payload))}.";
    }

    private static string Base64Url(string json) => Convert.ToBase64String(Encoding.UTF8.GetBytes(json)).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}
