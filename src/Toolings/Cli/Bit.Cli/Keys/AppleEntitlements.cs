using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using Bit.Cli.Projects;

namespace Bit.Cli.Keys;

/// <summary>
/// A capability of an App Store Connect bundle id, as the API names it (https://developer.apple.com/documentation/appstoreconnectapi/capabilitytype),
/// with the setting App Store Connect takes for it, if it takes one.
/// </summary>
public sealed record AppleCapability(string Type, string? SettingKey = null, string? SettingOption = null);

/// <summary>
/// The entitlements an iOS app signs with. A provisioning profile grants only the entitlements whose capability its bundle id has on,
/// and App Store Connect refuses an app signed with one its profile doesn't grant.
/// </summary>
public sealed partial class AppleEntitlements
{
    /// <summary>
    /// Each entitlement as Apple's reference names it (https://developer.apple.com/documentation/bundleresources/entitlements),
    /// whose page names the capability it needs, and that capability's type in the App Store Connect API.
    /// </summary>
    private static readonly Dictionary<string, string> capabilityTypes = new(StringComparer.Ordinal)
    {
        ["aps-environment"] = "PUSH_NOTIFICATIONS",
        ["com.apple.developer.associated-domains"] = "ASSOCIATED_DOMAINS",
        ["com.apple.developer.applesignin"] = "APPLE_ID_AUTH",
        ["com.apple.security.application-groups"] = "APP_GROUPS",
        ["com.apple.developer.icloud-container-identifiers"] = "ICLOUD",
        ["com.apple.developer.icloud-container-development-container-identifiers"] = "ICLOUD",
        ["com.apple.developer.icloud-container-environment"] = "ICLOUD",
        ["com.apple.developer.icloud-services"] = "ICLOUD",
        ["com.apple.developer.ubiquity-kvstore-identifier"] = "ICLOUD",
        ["com.apple.developer.game-center"] = "GAME_CENTER",
        ["com.apple.developer.pass-type-identifiers"] = "WALLET",
        ["com.apple.developer.healthkit"] = "HEALTHKIT",
        ["com.apple.developer.healthkit.access"] = "HEALTHKIT",
        ["com.apple.developer.healthkit.background-delivery"] = "HEALTHKIT",
        ["com.apple.developer.homekit"] = "HOMEKIT",
        ["com.apple.developer.in-app-payments"] = "APPLE_PAY",
        ["com.apple.developer.default-data-protection"] = "DATA_PROTECTION",
        ["com.apple.developer.siri"] = "SIRIKIT",
        ["com.apple.developer.networking.networkextension"] = "NETWORK_EXTENSIONS",
        ["com.apple.developer.networking.vpn.api"] = "PERSONAL_VPN",
        ["com.apple.developer.networking.multipath"] = "MULTIPATH",
        ["com.apple.developer.networking.HotspotConfiguration"] = "HOT_SPOT",
        ["com.apple.developer.nfc.readersession.formats"] = "NFC_TAG_READING",
        ["com.apple.developer.ClassKit-environment"] = "CLASSKIT",
        ["com.apple.developer.authentication-services.autofill-credential-provider"] = "AUTOFILL_CREDENTIAL_PROVIDER",
        ["com.apple.developer.networking.wifi-info"] = "ACCESS_WIFI_INFORMATION",
        ["com.apple.external-accessory.wireless-configuration"] = "WIRELESS_ACCESSORY_CONFIGURATION",
        ["inter-app-audio"] = "INTER_APP_AUDIO",
        ["com.apple.developer.maps"] = "MAPS"
    };

    /// <summary>
    /// What every App Store profile grants, whatever capabilities its bundle id has.
    /// </summary>
    private static readonly HashSet<string> everyProfile = new(StringComparer.Ordinal) { "application-identifier", "com.apple.developer.team-identifier", "keychain-access-groups", "get-task-allow", "beta-reports-active" };

    private static readonly Dictionary<string, string> dataProtectionLevels = new(StringComparer.Ordinal)
    {
        ["NSFileProtectionComplete"] = "COMPLETE_PROTECTION",
        ["NSFileProtectionCompleteUnlessOpen"] = "PROTECTED_UNLESS_OPEN",
        ["NSFileProtectionCompleteUntilFirstUserAuthentication"] = "PROTECTED_UNTIL_FIRST_USER_AUTH"
    };

    private readonly IReadOnlyList<(string Key, string? Value)> entries;

    private AppleEntitlements(string filePath, IReadOnlyList<(string Key, string? Value)> entries)
    {
        FilePath = filePath;
        this.entries = entries;
    }

    public string FilePath { get; }

    /// <summary>
    /// The capabilities the bundle id needs on for its profile to grant these entitlements.
    /// </summary>
    public IReadOnlyList<AppleCapability> Capabilities => [.. entries.Where(e => capabilityTypes.ContainsKey(e.Key)).Select(e => Capability(capabilityTypes[e.Key], e.Value)).DistinctBy(c => c.Type)];

    /// <summary>
    /// The entitlements bit knows no capability of.
    /// </summary>
    public IReadOnlyList<string> Unknown => [.. entries.Select(e => e.Key).Where(key => capabilityTypes.ContainsKey(key) is false && everyProfile.Contains(key) is false)];

    public string? UnknownWarning => Unknown is { Count: > 0 } unknown
        ? $"bit knows no App Store Connect capability of {string.Join(", ", unknown)}, which {Path.GetFileName(FilePath)} asks for. If the profile has to grant {(unknown.Count == 1 ? "it" : "them")}, turn the capability on for the bundle id at {AppleSigning.IdentifiersPage} and make the profile again."
        : null;

    /// <summary>
    /// Finds and reads the entitlements the iOS app of a project's Client.Maui signs with outside Development. Null when it names none.
    /// </summary>
    public static AppleEntitlements? ForProject(string directory, string projectName) => Find(directory, projectName) is { } path ? Read(path) : null;

    /// <summary>
    /// The CodesignEntitlements the csproj of a project's Client.Maui sets for iOS outside Development, like bit Boilerplate's
    /// Platforms/iOS/Entitlements.Production.plist, or else Platforms/iOS/Entitlements.plist, which .NET MAUI signs with when it exists.
    /// </summary>
    public static string? Find(string directory, string projectName)
    {
        var project = Path.Combine(directory, ProjectContext.FindMauiProject(directory, projectName));
        var folder = Path.GetDirectoryName(project)!;
        string? named;

        try
        {
            if (File.Exists(project) is false)
                return null;

            // MSBuild keeps the last value a property is given, so the last one that applies wins.
            named = XDocument.Load(project).Descendants()
                .Where(e => e.Name.LocalName == "CodesignEntitlements" && e.Value.Trim() is { Length: > 0 } value && value.Contains("$(", StringComparison.Ordinal) is false && AppliesToIosRelease(e))
                .Select(e => e.Value.Trim())
                .LastOrDefault();
        }
        catch (Exception exp) when (exp is IOException or UnauthorizedAccessException or XmlException)
        {
            return null;
        }

        var path = named ?? (File.Exists(Path.Combine(folder, "Platforms", "iOS", "Entitlements.plist")) ? "Platforms/iOS/Entitlements.plist" : null);
        return path is null ? null : Path.GetFullPath(path.Replace('\\', '/'), folder);
    }

    public static AppleEntitlements Read(string path)
    {
        if (File.Exists(path) is false)
            throw new AppleSigningException($"There's no entitlements file at {path}.");

        try
        {
            using var reader = XmlReader.Create(path, new XmlReaderSettings { DtdProcessing = DtdProcessing.Ignore });
            var entries = new List<(string Key, string? Value)>();

            foreach (var key in XDocument.Load(reader).Root?.Element("dict")?.Elements("key") ?? [])
            {
                var value = key.ElementsAfterSelf().FirstOrDefault();

                if (value?.Name.LocalName is "false")
                    continue;

                entries.Add((key.Value.Trim(), value?.Name.LocalName is "string" ? value.Value.Trim() : null));
            }

            return new AppleEntitlements(path, entries);
        }
        catch (Exception exp) when (exp is IOException or UnauthorizedAccessException or XmlException)
        {
            throw new AppleSigningException($"Couldn't read the entitlements in {path}: {exp.Message}");
        }
    }

    private static AppleCapability Capability(string type, string? value) => type switch
    {
        "APPLE_ID_AUTH" => new(type, "APPLE_ID_AUTH_APP_CONSENT", "PRIMARY_APP_CONSENT"),
        "ICLOUD" => new(type, "ICLOUD_VERSION", "XCODE_6"),
        "DATA_PROTECTION" when value is not null && dataProtectionLevels.TryGetValue(value, out var level) => new(type, "DATA_PROTECTION_PERMISSION_LEVEL", level),
        _ => new(type)
    };

    private static bool AppliesToIosRelease(XElement element)
    {
        var condition = $"{(string?)element.Parent?.Attribute("Condition")} {(string?)element.Attribute("Condition")}";
        var platforms = PlatformRegex().Matches(condition).Select(m => m.Value).ToList();

        return (platforms.Count == 0 || platforms.Contains("ios", StringComparer.OrdinalIgnoreCase)) && DevelopmentRegex().IsMatch(condition) is false;
    }

    [GeneratedRegex(@"(?<![A-Za-z0-9])(ios|maccatalyst|macos|tvos|android|windows|tizen)(?![A-Za-z0-9])", RegexOptions.IgnoreCase)]
    private static partial Regex PlatformRegex();

    /// <summary>
    /// A comparison that holds only in Development or Debug, on either side of the ==, like '$(Environment)' == 'Development'
    /// or '$(Configuration)|$(Platform)' == 'Debug|AnyCPU'.
    /// </summary>
    [GeneratedRegex(@"==\s*'(Development|Debug)(\|[^']*)?'|'(Development|Debug)(\|[^']*)?'\s*==", RegexOptions.IgnoreCase)]
    private static partial Regex DevelopmentRegex();
}
