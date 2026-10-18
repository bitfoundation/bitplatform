using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Bit.BlazorUI.Build;

public sealed class UpdateBitBlazorUIServiceWorker : Task
{
    private static readonly Regex FingerprintToken = new(@"#\[(?<prefix>[^\]{]*)\{(?<name>[^}]+)\}\](?<optional>\?)?", RegexOptions.CultureInvariant);

    [Required]
    public ITaskItem[] StaticWebAssets { get; set; } = [];

    public ITaskItem[] RemovedAssets { get; set; } = [];

    public ITaskItem[] TrimmedAssets { get; set; } = [];

    [Required]
    public string OutputDirectory { get; set; } = "";

    [Output]
    public ITaskItem[] CandidateAssets { get; set; } = [];

    [Output]
    public ITaskItem[] StaleAssets { get; set; } = [];

    public override bool Execute()
    {
        var serviceWorkerFiles = StaticWebAssets
            .Where(a => string.Equals(a.GetMetadata("AssetTraitName"), "BlazorServiceWorker", StringComparison.Ordinal)
                && string.Equals(a.GetMetadata("AssetRole"), "Primary", StringComparison.OrdinalIgnoreCase)
                && string.Equals(a.GetMetadata("AssetKind"), "Build", StringComparison.OrdinalIgnoreCase) is false)
            .ToList();

        var manifests = serviceWorkerFiles.Where(a => string.Equals(a.GetMetadata("AssetTraitValue"), "ServiceWorkerManifest", StringComparison.Ordinal)).ToList();
        if (manifests.Count == 0) return true;

        var removed = new HashSet<string>(RemovedAssets.Select(UrlOf), StringComparer.OrdinalIgnoreCase);
        var trimmedHashes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var asset in TrimmedAssets.Where(a => string.IsNullOrEmpty(a.GetMetadata("RelatedAsset")) && string.IsNullOrEmpty(a.GetMetadata("Integrity")) is false))
        {
            trimmedHashes[UrlOf(asset)] = "sha256-" + asset.GetMetadata("Integrity");
        }

        var candidates = new List<ITaskItem>();
        var stale = new List<ITaskItem>();

        foreach (var manifest in manifests)
        {
            var path = manifest.GetMetadata("FullPath");
            var text = File.ReadAllText(path);
            var start = text.IndexOf('{');
            var end = text.LastIndexOf('}');
            if (start < 0 || end < start)
            {
                Log.LogWarning("Bit.BlazorUI: {0} is not a service worker assets manifest it can read, so it still describes the untrimmed scripts and stylesheets.", path);
                continue;
            }

            var root = Json.AsObject(Json.Parse(text.Substring(start, end - start + 1)), path);
            if (root.TryGetValue("assets", out var assetsValue) is false || assetsValue is not List<object?> entries) continue;

            var kept = new List<object?>();
            var dropped = 0;
            var rehashed = 0;
            foreach (var entry in entries)
            {
                if (entry is not Dictionary<string, object?> asset || Json.GetOptionalString(asset, "url") is not string url)
                {
                    kept.Add(entry);
                    continue;
                }

                var key = url.TrimStart('/');
                if (removed.Contains(key))
                {
                    dropped++;
                    continue;
                }

                if (trimmedHashes.TryGetValue(key, out var hash) && string.Equals(Json.GetOptionalString(asset, "hash"), hash, StringComparison.Ordinal) is false)
                {
                    asset["hash"] = hash;
                    rehashed++;
                }

                kept.Add(asset);
            }

            if (dropped == 0 && rehashed == 0) continue;

            var previousVersion = Json.GetOptionalString(root, "version");
            var version = VersionOf(kept);
            root["assets"] = kept;
            root["version"] = version;

            candidates.Add(Rewrite(manifest, text.Substring(0, start) + Serialize(root, 0) + text.Substring(end + 1), stale));
            Log.LogMessage(MessageImportance.Normal, "Bit.BlazorUI: {0} lists {1} fewer assets and {2} trimmed bundles' hashes, version {3}.", manifest.GetMetadata("RelativePath"), dropped, rehashed, version);

            foreach (var script in serviceWorkerFiles.Where(a => string.Equals(a.GetMetadata("AssetTraitValue"), "ServiceWorkerJs", StringComparison.Ordinal)
                && string.Equals(a.GetMetadata("SourceId"), manifest.GetMetadata("SourceId"), StringComparison.OrdinalIgnoreCase)))
            {
                var scriptText = File.ReadAllText(script.GetMetadata("FullPath"));
                var marker = "Manifest version: " + previousVersion;
                if (previousVersion is null || scriptText.Contains(marker) is false)
                {
                    Log.LogMessage(MessageImportance.Normal, "Bit.BlazorUI: {0} does not carry the manifest's version, so it is left as it is.", script.GetMetadata("RelativePath"));
                    continue;
                }

                candidates.Add(Rewrite(script, scriptText.Replace(marker, "Manifest version: " + version), stale));
            }
        }

        CandidateAssets = [.. candidates];
        StaleAssets = [.. stale];
        return Log.HasLoggedErrors is false;
    }

    private ITaskItem Rewrite(ITaskItem original, string content, List<ITaskItem> stale)
    {
        var contentRoot = Path.GetFullPath(Path.Combine(OutputDirectory, original.GetMetadata("SourceId"))).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
        var path = Path.Combine(contentRoot, Path.GetFileName(original.GetMetadata("FullPath")));
        Directory.CreateDirectory(contentRoot);

        var originalPath = original.GetMetadata("FullPath");
        var bytes = new UTF8Encoding(HasBom(originalPath)).GetPreamble().Concat(new UTF8Encoding(false).GetBytes(content)).ToArray();
        if (File.Exists(path) is false || File.ReadAllBytes(path).SequenceEqual(bytes) is false)
        {
            File.WriteAllBytes(path, bytes);
        }

        var candidate = new TaskItem(path);
        foreach (var name in TrimBitBlazorUIAssets.CopiedMetadata) candidate.SetMetadata(name, original.GetMetadata(name));
        candidate.SetMetadata("ContentRoot", contentRoot);
        candidate.SetMetadata("AssetKind", original.GetMetadata("AssetKind"));
        candidate.SetMetadata("CopyToOutputDirectory", "Never");
        candidate.SetMetadata("CopyToPublishDirectory", "PreserveNewest");
        candidate.SetMetadata("OriginalItemSpec", path);

        var normalized = TrimBitBlazorUIAssets.Normalize(Path.GetFullPath(originalPath));
        stale.Add(TrimBitBlazorUIAssets.WithAssetFile(original));
        foreach (var derived in StaticWebAssets.Where(a => string.IsNullOrEmpty(a.GetMetadata("RelatedAsset")) is false
            && string.Equals(TrimBitBlazorUIAssets.Normalize(Path.GetFullPath(a.GetMetadata("RelatedAsset"))), normalized, StringComparison.OrdinalIgnoreCase)))
        {
            stale.Add(TrimBitBlazorUIAssets.WithAssetFile(derived));
        }

        return candidate;
    }

    private static bool HasBom(string path)
    {
        using var stream = File.OpenRead(path);
        var head = new byte[3];
        return stream.Read(head, 0, 3) == 3 && head[0] == 0xEF && head[1] == 0xBB && head[2] == 0xBF;
    }

    private static string UrlOf(ITaskItem asset)
    {
        var relativePath = FingerprintToken.Replace(asset.GetMetadata("RelativePath").Replace('\\', '/'),
            m => m.Groups["optional"].Success ? "" : m.Groups["prefix"].Value + asset.GetMetadata(m.Groups["name"].Value.Substring(0, 1).ToUpperInvariant() + m.Groups["name"].Value.Substring(1)));
        var basePath = asset.GetMetadata("BasePath").Replace('\\', '/').Trim('/');
        relativePath = relativePath.TrimStart('/');
        return basePath.Length == 0 ? relativePath : basePath + "/" + relativePath;
    }

    private static string VersionOf(List<object?> entries)
    {
        var combined = string.Join("\n", entries.OfType<Dictionary<string, object?>>()
            .Select(e => Json.GetOptionalString(e, "url") + ":" + Json.GetOptionalString(e, "hash")));
        using var sha = SHA256.Create();
        return Convert.ToBase64String(sha.ComputeHash(Encoding.UTF8.GetBytes(combined))).Substring(0, 8);
    }

    private static string Serialize(object? value, int depth)
    {
        var indent = new string(' ', (depth + 1) * 2);
        var closing = new string(' ', depth * 2);
        switch (value)
        {
            case null:
                return "null";
            case string s:
                return Quote(s);
            case bool b:
                return b ? "true" : "false";
            case double d:
                return d.ToString("R", CultureInfo.InvariantCulture);
            case Dictionary<string, object?> obj:
                return obj.Count == 0 ? "{}" : "{\n" + string.Join(",\n", obj.Select(p => indent + Quote(p.Key) + ": " + Serialize(p.Value, depth + 1))) + "\n" + closing + "}";
            case List<object?> list:
                return list.Count == 0 ? "[]" : "[\n" + string.Join(",\n", list.Select(i => indent + Serialize(i, depth + 1))) + "\n" + closing + "]";
            default:
                return Quote(Convert.ToString(value, CultureInfo.InvariantCulture) ?? "");
        }
    }

    private static string Quote(string value)
    {
        var builder = new StringBuilder("\"");
        foreach (var c in value)
        {
            switch (c)
            {
                case '"': builder.Append("\\\""); break;
                case '\\': builder.Append("\\\\"); break;
                case '\n': builder.Append("\\n"); break;
                case '\r': builder.Append("\\r"); break;
                case '\t': builder.Append("\\t"); break;
                default:
                    if (c < 0x20) builder.Append("\\u").Append(((int)c).ToString("x4", CultureInfo.InvariantCulture));
                    else builder.Append(c);
                    break;
            }
        }
        return builder.Append('"').ToString();
    }
}
