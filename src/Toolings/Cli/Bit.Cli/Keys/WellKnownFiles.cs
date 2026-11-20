using System.Text.Json;
using System.Text.Json.Nodes;
using Bit.Cli.Infrastructure;
using Bit.Cli.Projects;

namespace Bit.Cli.Keys;

public static class WellKnownFiles
{
    private static readonly JsonSerializerOptions indented = new() { WriteIndented = true, IndentSize = 4 };

    public const string AssetLinks = "assetlinks.json";

    public const string AppleAppSiteAssociation = "apple-app-site-association.json";

    public const string Option = "--well-known";

    public const string DefaultPlace = "a wwwroot/.well-known folder under src/Client";

    public static string NoneIn(ProjectContext project, string links) => project.Parts.WellKnown.Count > 0
        ? $"bit found none in what {Option} names, so {links} won't verify."
        : $"bit found none in {DefaultPlace}, so {links} won't verify. {Option} names its folder.";

    public static (IReadOnlyList<string> Changed, IReadOnlyList<string> Unreadable) Change(IEnumerable<string> files, Func<string, bool> change)
    {
        var changed = new List<string>();
        var unreadable = new List<string>();

        foreach (var file in files)
        {
            try
            {
                if (change(file))
                {
                    changed.Add(file);
                }
            }
            catch (Exception exp) when (exp is JsonException or IOException or UnauthorizedAccessException or InvalidOperationException)
            {
                unreadable.Add(file);
            }
        }

        return (changed, unreadable);
    }

    public static IReadOnlyList<string> Folders(ProjectContext project) => project.Parts.WellKnown.Count > 0
        ? project.Parts.WellKnown
        : [.. ProjectContext.FilesUnder(Path.Combine(project.Directory, "src", "Client"), "*.json")
            .Where(file => Path.GetFileName(file) is AssetLinks or AppleAppSiteAssociation)
            .Select(file => Path.GetDirectoryName(file)!)
            .Where(folder => Path.GetFileName(folder) is ".well-known" && Path.GetFileName(Path.GetDirectoryName(folder)) is "wwwroot")
            .Distinct(StringComparer.Ordinal)];

    public static IReadOnlyList<string> Files(ProjectContext project, string fileName) => [.. Folders(project).Select(folder => Path.Combine(folder, fileName)).Where(File.Exists)];

    public static bool AddAndroidFingerprint(string path, string packageName, string fingerprint)
    {
        if (File.Exists(path) is false)
            return false;

        var statements = JsonNode.Parse(File.ReadAllText(path)) as JsonArray ?? [];
        var target = statements.OfType<JsonObject>().Select(s => s["target"] as JsonObject).FirstOrDefault(t => (string?)t?["package_name"] == packageName);

        if (target is null)
        {
            target = new JsonObject { ["namespace"] = "android_app", ["package_name"] = packageName, ["sha256_cert_fingerprints"] = new JsonArray() };
            statements.Add(new JsonObject { ["relation"] = new JsonArray("delegate_permission/common.handle_all_urls"), ["target"] = target });
        }

        if (target["sha256_cert_fingerprints"] is not JsonArray fingerprints)
        {
            target["sha256_cert_fingerprints"] = fingerprints = [];
        }

        if (fingerprints.Any(f => string.Equals((string?)f, fingerprint, StringComparison.OrdinalIgnoreCase)))
            return false;

        fingerprints.Add(fingerprint);
        AtomicFile.WriteAllText(path, statements.ToJsonString(indented));
        return true;
    }

    public static bool AddAppleApp(string path, string teamId, string bundleId)
    {
        if (File.Exists(path) is false || JsonNode.Parse(File.ReadAllText(path)) is not JsonObject association)
            return false;

        var appId = $"{teamId}.{bundleId}";
        var changed = false;

        if (association["activitycontinuation"]?["apps"] is JsonArray continuation && continuation.All(a => (string?)a != appId))
        {
            continuation.Add(appId);
            changed = true;
        }

        if (association["webcredentials"]?["apps"] is JsonArray credentials && credentials.All(a => (string?)a != appId))
        {
            credentials.Add(appId);
            changed = true;
        }

        if (association["applinks"]?["details"] is JsonArray details && details.OfType<JsonObject>().All(d => (string?)d["appID"] != appId))
        {
            var paths = details.OfType<JsonObject>().Select(d => d["paths"]).FirstOrDefault(p => p is not null)?.DeepClone() ?? new JsonArray("*");
            details.Add(new JsonObject { ["appID"] = appId, ["paths"] = paths });
            changed = true;
        }

        if (changed)
        {
            AtomicFile.WriteAllText(path, association.ToJsonString(indented));
        }

        return changed;
    }

    /// <summary>
    /// The App ID prefix of the app's entry, <c>&lt;prefix&gt;.&lt;bundle id&gt;</c>, or null. <see cref="AddAppleApp"/> writes the
    /// team id there, which an old team's prefix may not be.
    /// </summary>
    public static string? AppIdPrefixOf(string path, string bundleId)
    {
        try
        {
            if (File.Exists(path) is false || JsonNode.Parse(File.ReadAllText(path)) is not JsonObject association)
                return null;

            var appIds = new[] { association["activitycontinuation"]?["apps"], association["webcredentials"]?["apps"] }.OfType<JsonArray>().SelectMany(apps => apps)
                .Concat((association["applinks"]?["details"] as JsonArray)?.OfType<JsonObject>().Select(d => d["appID"]) ?? []);

            return appIds.OfType<JsonValue>().Select(id => id.TryGetValue<string>(out var text) ? text : null)
                .FirstOrDefault(id => id is not null && id.Length == bundleId.Length + 11 && id.EndsWith($".{bundleId}", StringComparison.Ordinal))?[..10];
        }
        catch (Exception exp) when (exp is JsonException or IOException)
        {
            return null;
        }
    }
}
