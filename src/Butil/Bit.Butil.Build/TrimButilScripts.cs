using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Bit.Butil.Build;

/// <summary>
/// Works out which JavaScript modules a consumer's app can still call, and assembles a
/// <c>bit-butil.js</c> holding only those. Runs in a consumer's publish; wired up by
/// <c>buildTransitive/Bit.Butil.targets</c>, which also uses <see cref="IncludedModules"/> to decide which
/// per-module files an app publishing that shape of the scripts still needs.
/// </summary>
/// <remarks>
/// Three signals feed the answer, and the targets decide which are in play:
/// <list type="bullet">
/// <item><see cref="TrimmedAssembly"/> - the interop identifiers ILLink left in a trimmed
/// <c>Bit.Butil.dll</c>. The most precise of the three, and the only one on offer when the publish is
/// trimmed.</item>
/// <item><see cref="ScanMode"/> over <see cref="ScanAssemblies"/> - the Bit.Butil types the app's own
/// assemblies reference, for a publish ILLink never touched.</item>
/// <item><see cref="ExplicitModules"/> - what the consumer wrote in their csproj, always added to whatever
/// the other two found.</item>
/// </list>
/// The task is deliberately forgiving: when the signal it was told to use is not there (a trimmed publish
/// whose ILLink never reached Bit.Butil, a scan that found no assembly referencing the library) it sets
/// <see cref="Skipped"/> and writes nothing, and the consumer keeps the full set the package ships. Only a
/// genuinely broken input - a manifest that does not parse, a chunk that is missing, a module name that
/// names nothing - fails the build, because a silently wrong bundle would surface as
/// "BitButil.x is undefined" in a browser long after publishing.
/// </remarks>
public sealed class TrimButilScripts : Task
{
    /// <summary>
    /// The trimmed <c>Bit.Butil.dll</c>, normally <c>$(IntermediateLinkDir)Bit.Butil.dll</c>. Empty when the
    /// publish is not trimmed, which is what puts the scan below in play instead.
    /// </summary>
    public string TrimmedAssembly { get; set; } = string.Empty;

    /// <summary>
    /// The untrimmed <c>Bit.Butil.dll</c> the app references. Read to work out which module each Bit.Butil
    /// class needs, which is what a scan - and a consumer naming classes rather than modules - resolves
    /// against. Not needed when ILLink supplies the answer and the csproj names modules by their own names.
    /// </summary>
    public string ButilAssembly { get; set; } = string.Empty;

    /// <summary>The folder holding one <c>&lt;module&gt;.js</c> chunk per module (shipped in the package).</summary>
    [Required]
    public string ChunksDirectory { get; set; } = string.Empty;

    /// <summary>The <c>manifest.txt</c> emitted next to the chunks by Bit.Butil's build.</summary>
    [Required]
    public string ManifestPath { get; set; } = string.Empty;

    /// <summary>
    /// How to read the consumer's own assemblies: <c>None</c> (the default), <c>TypeNames</c> or
    /// <c>TypeReferences</c>. See <see cref="ButilScanMode"/>.
    /// </summary>
    public string ScanMode { get; set; } = nameof(ButilScanMode.None);

    /// <summary>The assemblies to scan. Anything that is not a managed assembly is passed over.</summary>
    public ITaskItem[] ScanAssemblies { get; set; } = [];

    /// <summary>
    /// Modules or Bit.Butil class names the consumer named in their csproj. Added to whatever the other
    /// signals found, never instead of them.
    /// </summary>
    public ITaskItem[] ExplicitModules { get; set; } = [];

    /// <summary>
    /// Where to write the trimmed bundle. Optional: left empty, no bundle is written and the task only
    /// works out the module set - which is all a lazy-scripts app needs, having no bundle to trim.
    /// </summary>
    public string OutputPath { get; set; } = string.Empty;

    /// <summary>The modules the bundle ends up holding, in bundle order.</summary>
    [Output]
    public ITaskItem[] IncludedModules { get; set; } = [];

    /// <summary>The modules the app calls directly (before closing over dependencies).</summary>
    [Output]
    public ITaskItem[] ReferencedModules { get; set; } = [];

    /// <summary>True when no bundle was written because there was no signal to work from.</summary>
    [Output]
    public bool Skipped { get; set; }

    public override bool Execute()
    {
        if (TryParseScanMode(out var mode) is false) return false;

        try
        {
            var manifest = ButilScriptBundler.ReadManifest(ManifestPath);
            var referenced = new SortedSet<string>(StringComparer.Ordinal);
            var explicitNames = ExplicitModules.Select(item => item.ItemSpec).Where(name => string.IsNullOrWhiteSpace(name) is false).ToArray();

            // The type map is what turns a class name into modules. Built once, and only when something
            // actually asks for it: a trimmed publish whose csproj names modules by their own names needs no
            // map at all, and reading Bit.Butil.dll for nothing would be a cost on every such publish.
            ButilTypeModules? types = null;
            var typesRead = false;

            ButilTypeModules? Types()
            {
                if (typesRead) return types;

                typesRead = true;
                if (string.IsNullOrEmpty(ButilAssembly) || File.Exists(ButilAssembly) is false)
                {
                    Log.LogMessage(MessageImportance.Normal, $"Bit.Butil: no untrimmed assembly at '{ButilAssembly}' to read the class-to-module map from.");
                    return null;
                }

                types = ButilTypeModules.Build(ButilAssembly);
                return types.IsEmpty ? null : types;
            }

            // Signal one: what ILLink left behind. In play whenever the targets passed a path at all, and a
            // path that is not there means the publish was set up to be trimmed but Bit.Butil was not - the
            // full bundle is then the only safe answer, whatever else the csproj said.
            if (string.IsNullOrEmpty(TrimmedAssembly) is false)
            {
                if (File.Exists(TrimmedAssembly) is false)
                {
                    Log.LogMessage(MessageImportance.Normal, $"Bit.Butil: no trimmed assembly at '{TrimmedAssembly}', keeping the full bit-butil.js.");
                    Skipped = true;
                    return true;
                }

                referenced.UnionWith(ButilScriptBundler.ReadReferencedModules(TrimmedAssembly));
            }
            // Signal two: which Bit.Butil types the app's own assemblies name. Only ever reached for an
            // untrimmed publish - the targets do not set ScanMode when ILLink is in play, because the
            // identifiers ILLink leaves are the better answer to the same question.
            else if (mode != ButilScanMode.None)
            {
                var map = Types();
                if (map is null)
                {
                    Log.LogError($"Bit.Butil: <BitButilScriptScan>{ScanMode}</BitButilScriptScan> needs to read the untrimmed Bit.Butil.dll to know which module each class uses, and '{ButilAssembly}' could not be read. Set <BitButilUntrimmedAssembly> to that assembly, or <BitButilScriptScan>None</BitButilScriptScan> to publish the full bundle instead.");
                    return false;
                }

                var scan = ButilConsumerScan.Scan(ScanAssemblies.Select(item => item.ItemSpec), map, mode);
                if (scan.Scanned.Count == 0)
                {
                    // Every app that can call into Bit.Butil references it, so finding nothing means the list
                    // of assemblies was not the app's. Trimming on that would drop every module.
                    Log.LogWarning(null, "BUTIL002", null, null, 0, 0, 0, 0,
                        $"Bit.Butil: none of the {ScanAssemblies.Length} assemblies scanned references Bit.Butil, so there is nothing to work out which JavaScript this app uses from; keeping the full bit-butil.js. Add a <BitButilScriptScanAssembly> item for each of the app's own assemblies, or set <BitButilScriptScan>None</BitButilScriptScan> to silence this.");
                    Skipped = true;
                    return true;
                }

                referenced.UnionWith(scan.Modules);
                Log.LogMessage(MessageImportance.Normal,
                    $"Bit.Butil: scanned {scan.Scanned.Count} assembly(s) referencing Bit.Butil and found {scan.Types.Count} Bit.Butil type(s): {string.Join(", ", scan.Types)}.");
            }

            // Signal three: the csproj, always added on top. It is how an app keeps a module it reaches in a
            // way none of the above can see - through reflection, or from JavaScript of its own.
            if (explicitNames.Length > 0)
            {
                var chosen = ButilScriptBundler.ResolveNames(explicitNames, manifest, Types(), out var unresolved);
                if (unresolved.Count > 0)
                {
                    Log.LogError($"Bit.Butil: <BitButilScriptModule> names {string.Join(", ", unresolved.Select(name => $"'{name}'"))}, which is neither a JavaScript module nor a Bit.Butil class. The modules are: {string.Join(", ", manifest.Order)}.");
                    return false;
                }

                referenced.UnionWith(chosen);
            }

            // Nothing to go on: no ILLink, no scan, no csproj. Publishing the full set is the only answer
            // that cannot be wrong.
            if (string.IsNullOrEmpty(TrimmedAssembly) && mode == ButilScanMode.None && explicitNames.Length == 0)
            {
                Log.LogMessage(MessageImportance.Normal, "Bit.Butil: nothing says which JavaScript this app uses, keeping the full bit-butil.js.");
                Skipped = true;
                return true;
            }

            var included = ButilScriptBundler.Resolve(manifest, referenced, out var unknown);

            foreach (var module in unknown)
            {
                // Not an error: the JavaScript this package ships simply has no such module, so the C# call
                // would fail regardless of trimming. Worth a warning because it means the two halves drifted.
                // Carries a code so a consumer can silence it the usual way ($(NoWarn), or a warning-as-error
                // policy) without having to turn the whole trimming off.
                Log.LogWarning(null, "BUTIL001", null, null, 0, 0, 0, 0,
                    $"Bit.Butil: the app calls 'BitButil.{module}.*' but no such JavaScript module exists in this version of Bit.Butil.");
            }

            if (string.IsNullOrEmpty(OutputPath) is false) ButilScriptBundler.WriteBundle(ChunksDirectory, included, OutputPath);

            ReferencedModules = referenced.Select(module => (ITaskItem)new TaskItem(module)).ToArray();
            IncludedModules = included.Select(module => (ITaskItem)new TaskItem(module)).ToArray();

            var what = string.IsNullOrEmpty(OutputPath)
                ? $"the app can reach {included.Count} of {manifest.Order.Count} modules"
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

    /// <summary>
    /// The scan mode as the csproj spelled it. An unknown one is an error rather than a silent <c>None</c>:
    /// a consumer who misspelled the mode asked for trimming and would otherwise get none, with nothing said.
    /// </summary>
    private bool TryParseScanMode(out ButilScanMode mode)
    {
        mode = ButilScanMode.None;
        if (string.IsNullOrWhiteSpace(ScanMode)) return true;

        if (Enum.TryParse(ScanMode.Trim(), ignoreCase: true, out mode) && Enum.IsDefined(typeof(ButilScanMode), mode)) return true;

        Log.LogError($"Bit.Butil: '{ScanMode}' is not a value of <BitButilScriptScan>; it is one of {string.Join(", ", Enum.GetNames(typeof(ButilScanMode)))}.");
        return false;
    }
}
