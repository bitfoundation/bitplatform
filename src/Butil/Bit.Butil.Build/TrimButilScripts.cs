using System;
using System.IO;
using System.Linq;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Bit.Butil.Build;

/// <summary>
/// Works out which JavaScript modules a trimmed <c>Bit.Butil.dll</c> still calls, and assembles a
/// <c>bit-butil.js</c> holding only those. Runs in a consumer's publish; wired up by
/// <c>buildTransitive/Bit.Butil.targets</c>, which also uses <see cref="IncludedModules"/> to decide which
/// per-module files an app publishing that shape of the scripts still needs.
/// </summary>
/// <remarks>
/// The task is deliberately forgiving: when there is no trimmed assembly to read (the publish is not
/// trimmed, or trimming did not touch Bit.Butil) it sets <see cref="Skipped"/> and writes nothing, and the
/// consumer keeps the full set the package ships. Only a genuinely broken input - a manifest that does not
/// parse, a chunk that is missing - fails the build, because a silently wrong bundle would surface as
/// "BitButil.x is undefined" in a browser long after publishing.
/// </remarks>
public sealed class TrimButilScripts : Task
{
    /// <summary>The trimmed <c>Bit.Butil.dll</c>, normally <c>$(IntermediateLinkDir)Bit.Butil.dll</c>.</summary>
    [Required]
    public string TrimmedAssembly { get; set; } = string.Empty;

    /// <summary>The folder holding one <c>&lt;module&gt;.js</c> chunk per module (shipped in the package).</summary>
    [Required]
    public string ChunksDirectory { get; set; } = string.Empty;

    /// <summary>The <c>manifest.txt</c> emitted next to the chunks by Bit.Butil's build.</summary>
    [Required]
    public string ManifestPath { get; set; } = string.Empty;

    /// <summary>
    /// Where to write the trimmed bundle. Optional: left empty, no bundle is written and the task only
    /// works out the module set - which is all a lazy-scripts app needs, having no bundle to trim.
    /// </summary>
    public string OutputPath { get; set; } = string.Empty;

    /// <summary>The modules the bundle ends up holding, in bundle order.</summary>
    [Output]
    public ITaskItem[] IncludedModules { get; set; } = [];

    /// <summary>The modules the trimmed assembly calls directly (before closing over dependencies).</summary>
    [Output]
    public ITaskItem[] ReferencedModules { get; set; } = [];

    /// <summary>True when no bundle was written because there was no trimmed assembly to read.</summary>
    [Output]
    public bool Skipped { get; set; }

    public override bool Execute()
    {
        if (File.Exists(TrimmedAssembly) is false)
        {
            Log.LogMessage(MessageImportance.Normal, $"Bit.Butil: no trimmed assembly at '{TrimmedAssembly}', keeping the full bit-butil.js.");
            Skipped = true;
            return true;
        }

        try
        {
            var manifest = ButilScriptBundler.ReadManifest(ManifestPath);
            var referenced = ButilScriptBundler.ReadReferencedModules(TrimmedAssembly);
            var included = ButilScriptBundler.Resolve(manifest, referenced, out var unknown);

            foreach (var module in unknown)
            {
                // Not an error: the JavaScript this package ships simply has no such module, so the C# call
                // would fail regardless of trimming. Worth a warning because it means the two halves drifted.
                // Carries a code so a consumer can silence it the usual way ($(NoWarn), or a warning-as-error
                // policy) without having to turn the whole trimming off.
                Log.LogWarning(null, "BUTIL001", null, null, 0, 0, 0, 0,
                    $"Bit.Butil: the trimmed app calls 'BitButil.{module}.*' but no such JavaScript module exists in this version of Bit.Butil.");
            }

            if (string.IsNullOrEmpty(OutputPath) is false) ButilScriptBundler.WriteBundle(ChunksDirectory, included, OutputPath);

            ReferencedModules = referenced.Select(module => (ITaskItem)new TaskItem(module)).ToArray();
            IncludedModules = included.Select(module => (ITaskItem)new TaskItem(module)).ToArray();

            var what = string.IsNullOrEmpty(OutputPath)
                ? $"the trimmed app can reach {included.Count} of {manifest.Order.Count} modules"
                : $"bit-butil.js trimmed to {included.Count} of {manifest.Order.Count} modules ({new FileInfo(OutputPath).Length:N0} bytes)";

            Log.LogMessage(MessageImportance.High, $"Bit.Butil: {what}: {string.Join(", ", included)}");
            return true;
        }
        catch (Exception exception) when (exception is IOException or InvalidDataException or UnauthorizedAccessException or BadImageFormatException or ArgumentException)
        {
            Log.LogError($"Bit.Butil: could not assemble the trimmed bit-butil.js - {exception.Message} Set <BitButilTrimScripts>false</BitButilTrimScripts> to publish the full bundle instead.");
            return false;
        }
    }
}
