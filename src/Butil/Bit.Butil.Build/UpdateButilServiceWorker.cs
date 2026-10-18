using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Bit.Butil.Build;

public sealed class UpdateButilServiceWorker : Task
{
    private static readonly Regex AssetEntry = new(@",?\s*\{[^{}]*\}", RegexOptions.CultureInvariant);
    private static readonly Regex UrlProperty = new(@"""url""\s*:\s*""(?<value>(?:[^""\\]|\\.)*)""", RegexOptions.CultureInvariant);
    private static readonly Regex HashProperty = new(@"""hash""\s*:\s*""(?<value>(?:[^""\\]|\\.)*)""", RegexOptions.CultureInvariant);
    private static readonly Regex VersionProperty = new(@"""version""\s*:\s*""(?<value>(?:[^""\\]|\\.)*)""", RegexOptions.CultureInvariant);
    private static readonly Regex AssetsArray = new(@"""assets""\s*:\s*\[(?<entries>.*)\]", RegexOptions.CultureInvariant | RegexOptions.Singleline);
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

    private static readonly string[] CopiedMetadata =
    [
        "SourceId", "SourceType", "BasePath", "RelativePath", "AssetMode", "AssetRole", "AssetMergeSource",
        "AssetMergeBehavior", "RelatedAsset", "AssetTraitName", "AssetTraitValue", "AssetKind",
    ];

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
            var text = File.ReadAllText(manifest.GetMetadata("FullPath"));
            var assets = AssetsArray.Match(text);
            var previousVersion = VersionProperty.Match(text);
            if (assets.Success is false || previousVersion.Success is false)
            {
                Log.LogWarning("Bit.Butil: {0} is not a service worker assets manifest it can read, so it still describes the untrimmed bit-butil.js.", manifest.GetMetadata("FullPath"));
                continue;
            }

            var dropped = 0;
            var rehashed = 0;
            var kept = new List<(string Url, string Hash, string Text)>();
            foreach (Match entry in AssetEntry.Matches(assets.Groups["entries"].Value))
            {
                var body = entry.Value.TrimStart(',', ' ', '\t', '\r', '\n');
                var url = Unescape(UrlProperty.Match(body).Groups["value"].Value).TrimStart('/');
                var hash = Unescape(HashProperty.Match(body).Groups["value"].Value);

                if (removed.Contains(url))
                {
                    dropped++;
                    continue;
                }

                if (trimmedHashes.TryGetValue(url, out var trimmedHash) && string.Equals(hash, trimmedHash, StringComparison.Ordinal) is false)
                {
                    body = HashProperty.Replace(body, $"\"hash\": \"{trimmedHash}\"", 1);
                    hash = trimmedHash;
                    rehashed++;
                }

                kept.Add((url, hash, body));
            }

            if (dropped == 0 && rehashed == 0) continue;

            var version = VersionOf(kept.Select(e => e.Url + ":" + e.Hash));
            var newline = text.Contains("\r\n") ? "\r\n" : "\n";
            var entries = newline + string.Join("," + newline, kept.Select(e => "    " + e.Text.Trim())) + newline + "  ";
            var rewritten = text.Substring(0, assets.Groups["entries"].Index) + entries + text.Substring(assets.Groups["entries"].Index + assets.Groups["entries"].Length);
            rewritten = VersionProperty.Replace(rewritten, $"\"version\": \"{version}\"", 1);

            candidates.Add(Rewrite(manifest, rewritten, stale));
            Log.LogMessage(MessageImportance.Normal, "Bit.Butil: {0} lists {1} fewer assets and {2} trimmed hashes, version {3}.", manifest.GetMetadata("RelativePath"), dropped, rehashed, version);

            var marker = "Manifest version: " + previousVersion.Groups["value"].Value;
            foreach (var script in serviceWorkerFiles.Where(a => string.Equals(a.GetMetadata("AssetTraitValue"), "ServiceWorkerJs", StringComparison.Ordinal)
                && string.Equals(a.GetMetadata("SourceId"), manifest.GetMetadata("SourceId"), StringComparison.OrdinalIgnoreCase)))
            {
                var scriptText = File.ReadAllText(script.GetMetadata("FullPath"));
                if (scriptText.Contains(marker) is false)
                {
                    Log.LogMessage(MessageImportance.Normal, "Bit.Butil: {0} does not carry the manifest's version, so it is left as it is.", script.GetMetadata("RelativePath"));
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
        var originalPath = Path.GetFullPath(original.GetMetadata("FullPath"));
        var contentRoot = Path.GetFullPath(Path.Combine(OutputDirectory, original.GetMetadata("SourceId"))).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
        var path = Path.Combine(contentRoot, Path.GetFileName(originalPath));
        Directory.CreateDirectory(contentRoot);

        var bytes = new UTF8Encoding(HasBom(originalPath)).GetPreamble().Concat(new UTF8Encoding(false).GetBytes(content)).ToArray();
        if (File.Exists(path) is false || File.ReadAllBytes(path).SequenceEqual(bytes) is false)
        {
            File.WriteAllBytes(path, bytes);
        }

        var candidate = new TaskItem(path);
        foreach (var name in CopiedMetadata) candidate.SetMetadata(name, original.GetMetadata(name));
        candidate.SetMetadata("ContentRoot", contentRoot);
        candidate.SetMetadata("CopyToOutputDirectory", "Never");
        candidate.SetMetadata("CopyToPublishDirectory", "PreserveNewest");
        candidate.SetMetadata("OriginalItemSpec", path);

        stale.Add(WithAssetFile(original));
        foreach (var derived in StaticWebAssets.Where(a => string.IsNullOrEmpty(a.GetMetadata("RelatedAsset")) is false
            && string.Equals(Path.GetFullPath(a.GetMetadata("RelatedAsset")), originalPath, StringComparison.OrdinalIgnoreCase)))
        {
            stale.Add(WithAssetFile(derived));
        }

        return candidate;
    }

    private static ITaskItem WithAssetFile(ITaskItem asset)
    {
        var item = new TaskItem(asset);
        item.SetMetadata("AssetFile", asset.GetMetadata("FullPath"));
        return item;
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

    private static string Unescape(string value) => Regex.Unescape(value);

    private static string VersionOf(IEnumerable<string> entries)
    {
        using var sha = SHA256.Create();
        return Convert.ToBase64String(sha.ComputeHash(Encoding.UTF8.GetBytes(string.Join("\n", entries)))).Substring(0, 8);
    }
}
