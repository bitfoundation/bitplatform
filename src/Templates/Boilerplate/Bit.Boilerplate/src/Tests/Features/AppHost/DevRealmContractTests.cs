//+:cnd:noEmit
using System.Text.Json;
using System.Globalization;
using Boilerplate.Shared.Infrastructure.Services;

namespace Boilerplate.Tests.Features.AppHost;

/// <summary>
/// <c>dev-realm.json</c> restates, as plain strings, three things the application already declares in C#: which groups
/// map onto <see cref="AppRoles"/>, which <see cref="AppFeatures"/> values the demo group grants, and which languages
/// the app supports. Nothing joins the two sides. A feature constant renamed, a role added, a culture added - none of
/// it touches the realm, nothing fails to compile, and the only symptom is a Keycloak-backed user quietly getting a
/// different set of permissions from a locally-registered one.
/// <para>
/// These are pure string comparisons against the shipped realm file, so they cost nothing and they are the only thing
/// in the suite that reads it: <c>ExternalIdentityTests</c> signs in as alice and asserts the persona text, which
/// would pass with every claim in this file wrong.
/// </para>
/// </summary>
[TestClass, TestCategory("UnitTest")]
public class DevRealmContractTests
{
    /// <summary>
    /// The realm's groups become ASP.NET Core roles verbatim (the group-membership mapper is configured with
    /// <c>"full.path": "false"</c>, so <c>/g-admin</c> arrives as <c>g-admin</c>). A group whose name is not a role the
    /// app knows grants nothing at all, silently - and renaming a role in <see cref="AppRoles"/> is exactly how that
    /// happens.
    /// </summary>
    [TestMethod]
    public void EveryRealmGroup_Should_NameARoleTheAppKnows()
    {
        using var realm = LoadDevRealm();

        var groupNames = realm.RootElement.GetProperty("groups")
            .EnumerateArray()
            .Select(group => group.GetProperty("name").GetString()!)
            .ToArray();

        Assert.IsNotEmpty(groupNames, "The realm declares no groups, so this test checked nothing.");

        foreach (var groupName in groupNames)
        {
            //#if (multitenant != true)
            // One development realm is shared by every configuration, so it always carries the tenant-admin group.
            // Without multi-tenancy AppRoles declares no TenantAdmin and that group simply grants nothing, which is
            // harmless - not worth maintaining a second realm file for.
            if (groupName is "t-admin") continue;
            //#endif

            Assert.IsTrue(AppRoles.IsBuiltInRole(groupName),
                $"The realm seeds users into the group '{groupName}', which is not a role this application declares "
                + $"(see AppRoles). Those users would sign in with no permissions and no error.");
        }

        Assert.Contains(AppRoles.GlobalAdmin, groupNames, "The realm no longer offers a global-admin account to sign in as.");
        //#if (multitenant == true)
        Assert.Contains(AppRoles.TenantAdmin, groupNames, "The realm no longer offers a tenant-admin account to sign in as.");
        //#endif
    }

    /// <summary>
    /// The demo group's <c>features</c> attribute is a hand-copied snapshot of <see cref="AppFeatures.GetDemoFeatures"/>,
    /// which is what <c>RoleClaimConfiguration</c> seeds the local <c>demo</c> role from. Adding a feature to any
    /// <see cref="AppFeatures"/> group other than <c>System</c>/<c>Management</c> grows the local role and leaves the
    /// realm behind, so the same user gets different permissions depending on how they signed in.
    /// </summary>
    [TestMethod]
    public void TheDemoGroupsFeatures_Should_MatchWhatTheAppGrantsItsOwnDemoRole()
    {
        using var realm = LoadDevRealm();

        var realmFeatures = realm.RootElement.GetProperty("groups")
            .EnumerateArray()
            .Single(group => group.GetProperty("name").GetString() == AppRoles.Demo)
            .GetProperty("attributes").GetProperty("features")
            .EnumerateArray()
            .Select(feature => feature.GetString()!)
            .Order()
            .ToArray();

        var appFeatures = AppFeatures.GetDemoFeatures().Select(feature => feature.Value).Order().ToArray();

        Assert.AreEqual(string.Join(',', appFeatures), string.Join(',', realmFeatures),
            "The demo group in dev-realm.json no longer grants what AppFeatures.GetDemoFeatures() does, so a Keycloak "
            + "demo user and a locally registered one have different permissions.");
    }

    /// <summary>
    /// The realm claims no <c>mx-p-s</c> (max privileged sessions). It must not: the server computes that value per
    /// session from the user's own claims and roles and stamps it into the token itself, and both readers take
    /// <c>Max()</c> over every matching claim - so a federated second value is merged with the server's rather than
    /// replacing it, and the Sessions page ends up promising a device limit the server does not enforce.
    /// <c>AppUserClaimsPrincipalFactory.serverManagedClaimTypes</c> now drops it on the way in; this keeps the realm
    /// from growing an attribute that would silently do nothing.
    /// </summary>
    [TestMethod]
    public void TheRealm_Should_NotClaimAnythingTheServerComputesPerSession()
    {
        var realmText = File.ReadAllText(FindDevRealmPath());

        Assert.DoesNotContain(AppClaimTypes.MAX_PRIVILEGED_SESSIONS, realmText, StringComparison.Ordinal,
            $"dev-realm.json mentions '{AppClaimTypes.MAX_PRIVILEGED_SESSIONS}'. That claim is owned by the server "
            + "(see AppUserClaimsPrincipalFactory.serverManagedClaimTypes), so anything the realm says about it is "
            + "dropped on the way in and only misleads whoever reads the realm.");
    }

    /// <summary>
    /// Keycloak renders its own hosted login page, so the languages it offers are declared here rather than in the app.
    /// Two ways this silently does nothing, both of which had happened: the whole block is ignored unless
    /// <c>internationalizationEnabled</c> is true, and a locale Keycloak ships no message bundle for falls back to
    /// English without an error.
    /// </summary>
    [TestMethod]
    public void TheRealmsLocales_Should_BeUsableAndCoverEveryCultureTheAppSupports()
    {
        using var realm = LoadDevRealm();

        Assert.IsTrue(realm.RootElement.TryGetProperty("internationalizationEnabled", out var enabled) && enabled.GetBoolean(),
            "supportedLocales below it is inert without `internationalizationEnabled: true` - Keycloak defaults it to "
            + "false, and then renders the login page in English with no language picker.");

        var realmLocales = realm.RootElement.GetProperty("supportedLocales")
            .EnumerateArray()
            .Select(locale => locale.GetString()!)
            .ToArray();

        foreach (var locale in realmLocales)
        {
            Assert.Contains(locale, keycloakLoginThemeLocales,
                $"Keycloak ships no login-page message bundle for '{locale}', so selecting it falls back to English "
                + "with no error. Keycloak uses specific codes for Chinese (zh-CN / zh-TW), not the neutral one.");
        }

        foreach (var culture in CultureInfoManager.SupportedCultures.Select(supported => supported.Culture))
        {
            var covered = realmLocales.Any(locale =>
                string.Equals(locale, culture.Name, StringComparison.OrdinalIgnoreCase)
                || string.Equals(locale, culture.TwoLetterISOLanguageName, StringComparison.OrdinalIgnoreCase));

            // Keycloak simply has no bundle for some of the languages this app supports - Hindi is the current one.
            // Listing such a locale in the realm would be config that reads as working and does nothing, so the honest
            // state is to leave it out, and this exemption is where that decision is written down.
            var keycloakHasNoBundleForIt = keycloakLoginThemeLocales.Contains(culture.Name) is false
                && keycloakLoginThemeLocales.Contains(culture.TwoLetterISOLanguageName) is false;

            Assert.IsTrue(covered || keycloakHasNoBundleForIt,
                $"The app supports {culture.Name} and Keycloak can render its login page in that language, but the "
                + "realm does not offer it - so Enterprise SSO drops the user onto a login page in another language.");
        }
    }

    /// <summary>
    /// The locales Keycloak's own login themes carry a message bundle for. Anything outside this list is accepted by
    /// the import and then falls back to the realm default. Kept as a literal because the alternative is asking a
    /// running Keycloak, which is far more than this assertion is worth.
    /// </summary>
    private static readonly string[] keycloakLoginThemeLocales =
    [
        "ar", "az", "ca", "cs", "da", "de", "el", "en", "es", "eu", "fa", "fi", "fr", "hr", "hu", "hy", "id", "it",
        "ja", "kk", "ko", "ky", "lt", "lv", "nl", "no", "pl", "pt", "pt-BR", "ro", "ru", "sk", "sl", "sv", "th", "tr",
        "uk", "vi", "zh-CN", "zh-TW"
    ];

    private static JsonDocument LoadDevRealm() => JsonDocument.Parse(File.ReadAllText(FindDevRealmPath()));

    /// <summary>
    /// Walks up from the test binaries to the solution root and finds the realm the app host imports. The app host
    /// project is renamed per generated project, so the folder is matched by suffix rather than by name.
    /// </summary>
    private static string FindDevRealmPath()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null && Directory.Exists(Path.Combine(directory.FullName, "src", "Server")) is false)
        {
            directory = directory.Parent;
        }

        Assert.IsNotNull(directory, "Could not find the solution's src/Server folder above the test binaries.");

        var realm = Directory.EnumerateDirectories(Path.Combine(directory.FullName, "src", "Server"), "*.Server.AppHost")
            .Select(appHost => Path.Combine(appHost, "Infrastructure", "Realms", "dev-realm.json"))
            .FirstOrDefault(File.Exists);

        Assert.IsNotNull(realm, "Could not find dev-realm.json under any *.Server.AppHost folder.");

        return realm;
    }
}
