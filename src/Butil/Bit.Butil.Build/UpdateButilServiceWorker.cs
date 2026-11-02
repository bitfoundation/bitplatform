using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Bit.Butil.Build;

/// <summary>
/// Rewrites the service worker assets manifests a publish carries - and the service worker scripts that name
/// their version - after the script trimming swapped in a bundle or dropped module files. Runs in a consumer's
/// publish; wired up by <c>buildTransitive/Bit.Butil.targets</c>.
/// </summary>
/// <remarks>
/// <see cref="ButilServiceWorkerManifest"/> says why a manifest needs it and does the rewriting. This is the
/// MSBuild half: it finds the manifests among the static web assets, writes each changed file to a folder of its
/// own, and hands back candidate assets for the new files along with the stale assets they replace - the
/// originals and the compressed variants derived from them - so the targets swap them the way the trimmed bundle
/// is swapped, and the SDK computes integrity, endpoints and fresh <c>.br</c>/<c>.gz</c> copies from the new
/// content.
/// <br/>
/// A manifest that cannot be read, or is not in the shape the SDK writes, is reported with BUTIL003 and published
/// as it is, since the alternative - failing a publish over a file the app may not even register - is worse than
/// a stale entry. A service worker script that cannot be given the new version is reported with BUTIL004: the
/// manifest is still corrected, but nothing tells an installed app to fetch it. Both are warnings with codes so a
/// consumer can silence them, or keep them out of a warnings-as-errors policy, the way BUTIL001 and BUTIL002 are.
/// </remarks>
public sealed class UpdateButilServiceWorker : Task
{
    private const string ManifestWarning = "BUTIL003";
    private const string ScriptWarning = "BUTIL004";

    /// <summary>The static web assets as they stand once the trimming is done.</summary>
    [Required]
    public ITaskItem[] StaticWebAssets { get; set; } = [];

    /// <summary>The assets the trimming removed: the per-module files the app can no longer import.</summary>
    public ITaskItem[] RemovedAssets { get; set; } = [];

    /// <summary>The assets the trimming defined in place of the package's: the trimmed bundle.</summary>
    public ITaskItem[] TrimmedAssets { get; set; } = [];

    /// <summary>Where the rewritten files go, one folder per project that contributed a service worker.</summary>
    [Required]
    public string OutputDirectory { get; set; } = "";

    /// <summary>The rewritten files, carrying the metadata of the assets they replace, ready to be defined.</summary>
    [Output]
    public ITaskItem[] CandidateAssets { get; set; } = [];

    /// <summary>The assets the candidates replace, each with an <c>AssetFile</c> to match its endpoints on.</summary>
    [Output]
    public ITaskItem[] StaleAssets { get; set; } = [];

    private static readonly string[] CopiedMetadata =
    [
        "SourceId", "SourceType", "BasePath", "RelativePath", "AssetMode", "AssetRole", "AssetMergeSource",
        "AssetMergeBehavior", "RelatedAsset", "AssetTraitName", "AssetTraitValue", "AssetKind",
    ];

    public override bool Execute()
    {
        // The publish copies only: a build carries its own pair, written from the untrimmed files a build keeps.
        var serviceWorkerFiles = StaticWebAssets
            .Where(a => string.Equals(a.GetMetadata("AssetTraitName"), "BlazorServiceWorker", StringComparison.Ordinal)
                && string.Equals(a.GetMetadata("AssetRole"), "Primary", StringComparison.OrdinalIgnoreCase)
                && string.Equals(a.GetMetadata("AssetKind"), "Build", StringComparison.OrdinalIgnoreCase) is false)
            .ToList();

        // None in a standalone WebAssembly app, whose own manifest is written after this runs, from the trimmed
        // files - which is why it never needs rewriting.
        var manifests = serviceWorkerFiles.Where(a => string.Equals(a.GetMetadata("AssetTraitValue"), "ServiceWorkerManifest", StringComparison.Ordinal)).ToList();
        if (manifests.Count == 0) return true;

        var removed = RemovedAssets.Select(UrlOf).ToList();
        var hashes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var asset in TrimmedAssets.Where(a => string.IsNullOrEmpty(a.GetMetadata("RelatedAsset"))))
        {
            // An asset defined without an integrity has its file hashed here instead.
            var integrity = asset.GetMetadata("Integrity");
            if (string.IsNullOrEmpty(integrity))
            {
                using var sha = SHA256.Create();
                using var stream = File.OpenRead(asset.GetMetadata("FullPath"));
                integrity = Convert.ToBase64String(sha.ComputeHash(stream));
            }

            hashes[UrlOf(asset)] = "sha256-" + integrity;
        }

        var candidates = new List<ITaskItem>();
        var stale = new List<ITaskItem>();

        const string StaleManifest = "so it is published as it is, still listing the Bit.Butil scripts as they were before the trimming";
        const string StaleScript = "so it is published unchanged, and a browser that already installed the app sees no new service worker to update to - it keeps the Bit.Butil scripts it cached";

        foreach (var manifest in manifests)
        {
            if (ReadText(manifest, ManifestWarning, StaleManifest) is not { } text) continue;

            var update = ButilServiceWorkerManifest.Update(text, removed, hashes);
            if (update is null)
            {
                Warn(ManifestWarning, $"{manifest.GetMetadata("FullPath")} is not a service worker assets manifest it can read, {StaleManifest}.");
                continue;
            }

            if (update.Changed is false) continue;

            candidates.Add(Rewrite(manifest, update.Text, stale));
            Log.LogMessage(MessageImportance.Normal, "Bit.Butil: {0} lists {1} fewer assets and {2} trimmed hashes, version {3}.", manifest.GetMetadata("RelativePath"), update.Dropped, update.Rehashed, update.Version);

            foreach (var script in serviceWorkerFiles.Where(a => string.Equals(a.GetMetadata("AssetTraitValue"), "ServiceWorkerJs", StringComparison.Ordinal)
                && string.Equals(a.GetMetadata("SourceId"), manifest.GetMetadata("SourceId"), StringComparison.OrdinalIgnoreCase)))
            {
                if (ReadText(script, ScriptWarning, StaleScript) is not { } source) continue;

                var updated = ButilServiceWorkerManifest.UpdateVersion(source, update.PreviousVersion, update.Version);
                if (updated is null)
                {
                    // The SDK writes the version line into every published service worker, so a script without
                    // it is a hand-written one or an SDK that changed - either way invisible at the default
                    // verbosity unless it is a warning, and what it costs is an installed app never updating.
                    Warn(ScriptWarning, $"{script.GetMetadata("FullPath")} does not carry the version of the manifest it precaches from ('/* {ButilServiceWorkerManifest.VersionMarker}{update.PreviousVersion} */'), {StaleScript}.");
                    continue;
                }

                candidates.Add(Rewrite(script, updated, stale));
            }
        }

        CandidateAssets = [.. candidates];
        StaleAssets = [.. stale];
        return Log.HasLoggedErrors is false;
    }

    /// <summary>
    /// The text of a service worker file, or null - with a warning - when it cannot be read. A listed asset whose
    /// file is not on disk (an SDK that defines the asset ahead of writing it, a stale entry left by an interrupted
    /// client publish) would otherwise fail the whole publish over a file the app may not even register.
    /// </summary>
    private string? ReadText(ITaskItem asset, string code, string consequence)
    {
        var path = asset.GetMetadata("FullPath");
        try
        {
            return File.ReadAllText(path);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            Warn(code, $"{path} could not be read ({exception.Message}), {consequence}.");
            return null;
        }
    }

    private void Warn(string code, string message) => Log.LogWarning(null, code, null, null, 0, 0, 0, 0, "Bit.Butil: " + message);

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

    /// <summary>The URL an asset is served at, as a manifest spells it - see <see cref="ButilServiceWorkerManifest.AssetUrl"/>.</summary>
    private static string UrlOf(ITaskItem asset) => ButilServiceWorkerManifest.AssetUrl(asset.GetMetadata("BasePath"), asset.GetMetadata("RelativePath"), asset.GetMetadata);
}
