using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Bit.BlazorUI.Build;

/// <summary>
/// How the tasks hand the targets a static web asset to swap: a candidate asset publishing a file of their own where an
/// existing asset was published, and the assets that leave with the one it replaces. One copy for
/// <see cref="TrimBitBlazorUIAssets"/> (the trimmed bundles, the files it leaves out) and
/// <see cref="UpdateBitBlazorUIServiceWorker"/> (the rewritten service worker files), so the two swaps cannot drift apart.
/// </summary>
internal static class StaticWebAssetSwap
{
    // What places an asset - its source, URL and role - and nothing computed from its content (fingerprint, integrity),
    // which DefineStaticWebAssets works out again from the new file.
    private static readonly string[] CopiedMetadata =
    [
        "SourceId", "SourceType", "BasePath", "RelativePath", "AssetMode", "AssetRole", "AssetMergeSource",
        "AssetMergeBehavior", "RelatedAsset", "AssetTraitName", "AssetTraitValue",
    ];

    /// <summary>
    /// A candidate asset publishing <paramref name="path"/> at the URL <paramref name="original"/> was published at, ready to
    /// be defined.
    /// </summary>
    /// <param name="original">The asset replaced.</param>
    /// <param name="path">The file replacing it.</param>
    /// <param name="contentRoot">The folder the file is in, as <see cref="ContentRoot"/> gives it.</param>
    public static ITaskItem Candidate(ITaskItem original, string path, string contentRoot)
    {
        var fullPath = Path.GetFullPath(path);
        var candidate = new TaskItem(fullPath);
        foreach (var name in CopiedMetadata) candidate.SetMetadata(name, original.GetMetadata(name));
        candidate.SetMetadata("ContentRoot", contentRoot);
        candidate.SetMetadata("AssetKind", "Publish");
        candidate.SetMetadata("CopyToOutputDirectory", "Never");
        candidate.SetMetadata("CopyToPublishDirectory", "PreserveNewest");
        candidate.SetMetadata("OriginalItemSpec", fullPath);
        return candidate;
    }

    /// <summary>A folder of <paramref name="outputDirectory"/>, full and with its trailing separator, as a content root is written.</summary>
    public static string ContentRoot(string outputDirectory, string folder) =>
        Path.GetFullPath(Path.Combine(outputDirectory, folder)).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;

    /// <summary>
    /// <paramref name="originals"/> and every asset derived from one of them - the compressed copies, whose
    /// <c>RelatedAsset</c> names it - each carrying an <c>AssetFile</c> to match its endpoints on: endpoints are keyed by the
    /// file they serve, which is what an item removing them matches on.
    /// </summary>
    public static List<ITaskItem> WithDerived(IReadOnlyCollection<ITaskItem> originals, IEnumerable<ITaskItem> staticWebAssets)
    {
        var paths = new HashSet<string>(originals.Select(o => FullPath(o.GetMetadata("FullPath"))), StringComparer.OrdinalIgnoreCase);
        var derived = staticWebAssets.Where(a => string.IsNullOrEmpty(a.GetMetadata("RelatedAsset")) is false && paths.Contains(FullPath(a.GetMetadata("RelatedAsset"))));

        return originals.Concat(derived).Select(WithAssetFile).ToList();
    }

    private static ITaskItem WithAssetFile(ITaskItem asset)
    {
        var item = new TaskItem(asset);
        item.SetMetadata("AssetFile", asset.GetMetadata("FullPath"));
        return item;
    }

    private static string FullPath(string path) => Path.GetFullPath(path).Replace('\\', '/');
}
