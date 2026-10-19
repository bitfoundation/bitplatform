using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Bit.BlazorUI.Build;

/// <summary>
/// Rewrites the service worker assets manifests a publish carries from a referenced project - and the service worker
/// scripts that name their version - after the trimming swapped in trimmed bundles or left files out. Runs in a
/// consumer's publish; wired up by <c>buildTransitive/Bit.BlazorUI.targets</c>.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="ServiceWorkerManifest"/> says why a manifest needs it and does the rewriting. This is the MSBuild half: it
/// finds the manifests among the static web assets, writes each changed file to a folder of its own, and hands back
/// candidate assets for the new files along with the stale assets they replace - the originals and the compressed copies
/// derived from them - so the targets swap them the way the trimmed bundles are swapped (<see cref="StaticWebAssetSwap"/>),
/// and the SDK computes integrity, endpoints and fresh <c>.br</c>/<c>.gz</c> copies from the new content.
/// </para>
/// <para>
/// Only a manifest another project wrote is read: the client of a Blazor Web App, which wrote it during its own publish,
/// before this project trimmed. The project's own manifest - a standalone WebAssembly app's - is the SDK's to write after
/// this runs, from the trimmed files, and is left alone even where the SDK already lists it: the .NET 8 SDK defines it as
/// an asset from the build on, so at this point its file is not there yet on a first publish, and is the previous
/// publish's on a later one.
/// </para>
/// <para>
/// A manifest that cannot be read, or is not in the shape the SDK writes, is reported with BITBLAZORUI004 and published as
/// it is, since the alternative - failing a publish over a file the app may not even register - is worse than a stale
/// entry. A service worker script that cannot be given the new version is reported with BITBLAZORUI005: the manifest is
/// still corrected, but nothing tells an installed app to fetch it. Both are warnings with codes so a project can silence
/// them, or keep them out of a warnings-as-errors policy, the way the trimming's own are.
/// </para>
/// </remarks>
public sealed class UpdateBitBlazorUIServiceWorker : Task
{
    private const string ManifestWarningCode = "BITBLAZORUI004";
    private const string ScriptWarningCode = "BITBLAZORUI005";

    /// <summary>The static web assets as they stand once the trimming is done.</summary>
    [Required]
    public ITaskItem[] StaticWebAssets { get; set; } = [];

    /// <summary>The <c>SourceId</c> of the project's own assets (its <c>PackageId</c>), whose manifest is never read.</summary>
    [Required]
    public string ProjectSourceId { get; set; } = string.Empty;

    /// <summary>
    /// The assets the trimming removed - the files of the vendor folders, the modules of the lazy scripts the app can no
    /// longer import - and what was derived from them.
    /// </summary>
    public ITaskItem[] RemovedAssets { get; set; } = [];

    /// <summary>The assets the trimming defined in place of the packages': the trimmed bundles and boot modules.</summary>
    public ITaskItem[] TrimmedAssets { get; set; } = [];

    /// <summary>Where the rewritten files go, one folder per project that contributed a service worker.</summary>
    [Required]
    public string OutputDirectory { get; set; } = string.Empty;

    /// <summary>The rewritten files, carrying the metadata of the assets they replace, ready to be defined.</summary>
    [Output]
    public ITaskItem[] CandidateAssets { get; set; } = [];

    /// <summary>The assets the candidates replace, each with an <c>AssetFile</c> to match its endpoints on.</summary>
    [Output]
    public ITaskItem[] StaleAssets { get; set; } = [];

    private const string StaleManifest = "so it is published as it is, still listing the bit BlazorUI scripts and stylesheets as they were before the trimming";
    private const string StaleScript = "so it is published unchanged, and a browser that already installed the app sees no new service worker to update to - it keeps the bit BlazorUI scripts and stylesheets it cached";

    public override bool Execute()
    {
        try
        {
            return Run();
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            Log.LogError("Bit.BlazorUI: {0}", e.Message);
            return false;
        }
    }

    private bool Run()
    {
        // The publish copies only: a build carries its own pair, written from the untrimmed files a build keeps.
        var serviceWorkerFiles = StaticWebAssets
            .Where(a => string.Equals(a.GetMetadata("AssetTraitName"), "BlazorServiceWorker", StringComparison.Ordinal)
                && string.Equals(a.GetMetadata("AssetRole"), "Primary", StringComparison.OrdinalIgnoreCase)
                && string.Equals(a.GetMetadata("AssetKind"), "Build", StringComparison.OrdinalIgnoreCase) is false)
            .ToList();

        // Another project's only (see the remarks): none in a standalone WebAssembly app.
        var manifests = serviceWorkerFiles.Where(a => string.Equals(a.GetMetadata("AssetTraitValue"), "ServiceWorkerManifest", StringComparison.Ordinal)
                && string.Equals(a.GetMetadata("SourceId"), ProjectSourceId, StringComparison.OrdinalIgnoreCase) is false)
            .ToList();
        if (manifests.Count == 0) return true;

        var removed = RemovedAssets.Select(UrlOf).ToList();
        var trimmed = new Dictionary<string, ITaskItem>(StringComparer.OrdinalIgnoreCase);
        foreach (var asset in TrimmedAssets.Where(a => string.IsNullOrEmpty(a.GetMetadata("RelatedAsset")))) trimmed[UrlOf(asset)] = asset;

        // Asked for by the rewrite only for a URL a manifest lists, so no file is hashed that no manifest needs.
        var hashes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        string? HashOf(string url)
        {
            if (hashes.TryGetValue(url, out var hash)) return hash;
            return trimmed.TryGetValue(url, out var asset) ? hashes[url] = "sha256-" + IntegrityOf(asset) : null;
        }

        var candidates = new List<ITaskItem>();
        var stale = new List<ITaskItem>();

        foreach (var manifest in manifests)
        {
            if (ReadText(manifest, ManifestWarningCode, StaleManifest) is not { } text) continue;

            // Its URLs are written under the base path of the project that wrote it, the one its own asset is served under.
            var update = ServiceWorkerManifest.Update(text, removed, HashOf, manifest.GetMetadata("BasePath"));
            if (update is null)
            {
                Warn(ManifestWarningCode, $"{manifest.GetMetadata("FullPath")} is not a service worker assets manifest it can read, {StaleManifest}.");
                continue;
            }

            if (update.Changed is false) continue;

            candidates.Add(Rewrite(manifest, update.Text, stale));
            // One line per rewritten manifest, at the importance of the trimming's own lines: it is a file of the publish
            // the trimming changed.
            Log.LogMessage(MessageImportance.High, "Bit.BlazorUI: {0} lists {1} fewer assets and {2} trimmed hashes, version {3}.",
                manifest.GetMetadata("RelativePath"), update.Dropped, update.Rehashed, update.Version);

            foreach (var script in serviceWorkerFiles.Where(a => string.Equals(a.GetMetadata("AssetTraitValue"), "ServiceWorkerJs", StringComparison.Ordinal)
                && string.Equals(a.GetMetadata("SourceId"), manifest.GetMetadata("SourceId"), StringComparison.OrdinalIgnoreCase)))
            {
                if (ReadText(script, ScriptWarningCode, StaleScript) is not { } source) continue;

                var updated = ServiceWorkerManifest.UpdateVersion(source, update.PreviousVersion, update.Version);
                if (updated is null)
                {
                    // The SDK writes the version line into every published service worker, so a script without it is a
                    // hand-written one or an SDK that changed - either way invisible at the default verbosity unless it
                    // is a warning, and what it costs is an installed app never updating.
                    Warn(ScriptWarningCode, $"{script.GetMetadata("FullPath")} does not carry the version of the manifest it precaches from ('/* {ServiceWorkerManifest.VersionMarker}{update.PreviousVersion} */'), {StaleScript}.");
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
    /// The text of a service worker file, or null - with a warning - when it cannot be read. A listed asset whose file is
    /// not on disk (an SDK that defines the asset ahead of writing it, a stale entry left by an interrupted client publish)
    /// would otherwise fail the whole publish over a file the app may not even register.
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

    private void Warn(string code, string message) => Log.LogWarning(null, code, null, null, 0, 0, 0, 0, "Bit.BlazorUI: " + message);

    private ITaskItem Rewrite(ITaskItem original, string content, List<ITaskItem> stale)
    {
        var originalPath = original.GetMetadata("FullPath");
        var contentRoot = StaticWebAssetSwap.ContentRoot(OutputDirectory, original.GetMetadata("SourceId"));
        var path = Path.Combine(contentRoot, Path.GetFileName(originalPath));
        Directory.CreateDirectory(contentRoot);

        // Written only when it changed, so an incremental publish finds the file as old as the content it holds.
        var bytes = new UTF8Encoding(HasBom(originalPath)).GetPreamble().Concat(new UTF8Encoding(false).GetBytes(content)).ToArray();
        if (File.Exists(path) is false || File.ReadAllBytes(path).SequenceEqual(bytes) is false)
        {
            File.WriteAllBytes(path, bytes);
        }

        stale.AddRange(StaticWebAssetSwap.WithDerived([original], StaticWebAssets));
        return StaticWebAssetSwap.Candidate(original, path, contentRoot);
    }

    // The .NET 8 SDK defines an asset without an integrity, so the file is hashed here instead.
    private static string IntegrityOf(ITaskItem asset)
    {
        var integrity = asset.GetMetadata("Integrity");
        if (string.IsNullOrEmpty(integrity) is false) return integrity;

        using var sha = SHA256.Create();
        using var stream = File.OpenRead(asset.GetMetadata("FullPath"));
        return Convert.ToBase64String(sha.ComputeHash(stream));
    }

    private static bool HasBom(string path)
    {
        using var stream = File.OpenRead(path);
        var head = new byte[3];
        return stream.Read(head, 0, 3) == 3 && head[0] == 0xEF && head[1] == 0xBB && head[2] == 0xBF;
    }

    /// <summary>The URL an asset is served at, as a manifest spells it - see <see cref="ServiceWorkerManifest.AssetUrl"/>.</summary>
    private static string UrlOf(ITaskItem asset) => ServiceWorkerManifest.AssetUrl(asset.GetMetadata("BasePath"), asset.GetMetadata("RelativePath"), asset.GetMetadata);
}
