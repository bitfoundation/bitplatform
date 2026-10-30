using System.Text.Json;
using System.Text.Json.Nodes;
using Bit.Cli.Infrastructure;

namespace Bit.Cli.Keys;

public static class WellKnownFiles
{
    private static readonly JsonSerializerOptions indented = new() { WriteIndented = true, IndentSize = 4 };

    public static string AssetLinksPath(string projectDirectory, string name) => Path.Combine(projectDirectory, "src", "Client", $"{name}.Client.Web", "wwwroot", ".well-known", "assetlinks.json");

    public static string AppleAppSiteAssociationPath(string projectDirectory, string name) => Path.Combine(projectDirectory, "src", "Client", $"{name}.Client.Web", "wwwroot", ".well-known", "apple-app-site-association.json");

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
}
