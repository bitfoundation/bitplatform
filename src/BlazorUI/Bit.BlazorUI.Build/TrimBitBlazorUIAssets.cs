using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Bit.BlazorUI.Build;

/// <summary>
/// Puts the bit BlazorUI packages' script and style bundles back together from only the pieces an app can use, and
/// hands MSBuild the static web assets to swap the packages' own bundles for.
/// </summary>
/// <remarks>
/// <para>
/// What the app can use comes from one of two signals. Where ILLink trimmed the publish, the library assemblies it left
/// say it exactly: the types still in them are the types the app can reach, and the string literals still in them are
/// the interop identifiers its code can still call. Where it did not, the app's own assemblies are read for the library
/// types they reference, and the untrimmed library assemblies for everything those types reach. A csproj can add names
/// to either, for what neither can see: a component rendered by reflection, a script called from the app's own
/// JavaScript, an icon named in data.
/// </para>
/// <para>
/// With no signal at all the task stands down and the full bundles are published: nothing to go on is never a reason
/// to trim to nothing.
/// </para>
/// </remarks>
public sealed class TrimBitBlazorUIAssets : Task
{
    /// <summary>The folders the packages keep their pieces and manifests in (scripts.json, styles.json).</summary>
    [Required]
    public ITaskItem[] AssetFolders { get; set; } = [];

    /// <summary>Where ILLink wrote the trimmed assemblies; empty when the publish is not trimmed.</summary>
    public string? TrimmedAssembliesDirectory { get; set; }

    /// <summary>The untrimmed library assemblies (Bit.BlazorUI.dll, ...), for the scan and for the names a csproj gives.</summary>
    public ITaskItem[] LibraryAssemblies { get; set; } = [];

    /// <summary>
    /// How the app's own assemblies are read for what it uses: None, TypeReferences (their TypeRef rows, which name the
    /// namespace too - the precise mode), or TypeNames (the names their string heaps hold, which over-includes a type of
    /// the app's own named like one of the packages', and never misses).
    /// </summary>
    public string ScanMode { get; set; } = "None";

    /// <summary>Whether the csproj asked for the scan itself, which makes a scan that cannot run an error rather than a no-op.</summary>
    public bool ScanRequested { get; set; }

    /// <summary>The app's own assemblies, which the scan reads.</summary>
    public ITaskItem[] ScanAssemblies { get; set; } = [];

    /// <summary>
    /// The app's own static markup (an index.html), read as text: what it says is what the app says, whatever the signal -
    /// an &lt;html bit-theme-system&gt; there needs the theme's script, a bit-icon--Add class there the glyph.
    /// </summary>
    public ITaskItem[] ScanFiles { get; set; } = [];

    /// <summary>Types the csproj keeps whatever the signals say, with everything they reach (BitBlazorUIComponent).</summary>
    public ITaskItem[] Components { get; set; } = [];

    /// <summary>Scripts and component stylesheets the csproj keeps by name (BitBlazorUIAsset).</summary>
    public ITaskItem[] Assets { get; set; } = [];

    /// <summary>Icons the csproj keeps by name (BitBlazorUIIcon).</summary>
    public ITaskItem[] Icons { get; set; } = [];

    /// <summary>Puts the script bundles back together from the scripts the app keeps.</summary>
    public bool TrimScripts { get; set; }

    /// <summary>
    /// Leaves the modules of the lazy scripts the app keeps none of out of the publish, and writes the boot modules again
    /// with the scripts the app starts with that it keeps.
    /// </summary>
    public bool TrimScriptModules { get; set; }

    public bool TrimStyles { get; set; }

    public bool TrimIcons { get; set; }

    /// <summary>
    /// The folders of the packages' static web assets that a component loads at run time (a vendored library, a set of
    /// images): each one a folder under the package's wwwroot, with the package as its Package metadata.
    /// </summary>
    public ITaskItem[] VendorFolders { get; set; } = [];

    public bool TrimVendorAssets { get; set; }

    /// <summary>
    /// The static web assets nothing the app can run asks for - the files of the vendor folders, the modules of the lazy
    /// scripts it keeps none of - and what was derived from them.
    /// </summary>
    [Output]
    public ITaskItem[] RemovedAssets { get; set; } = [];

    [Required]
    public string OutputDirectory { get; set; } = string.Empty;

    /// <summary>The app's static web assets, the packages' own bundles among them.</summary>
    public ITaskItem[] StaticWebAssets { get; set; } = [];

    /// <summary>The trimmed bundles, as static web asset candidates carrying the metadata of the assets they replace.</summary>
    [Output]
    public ITaskItem[] CandidateAssets { get; set; } = [];

    /// <summary>The assets they replace, and what was derived from those (compressed copies), whose endpoints go too.</summary>
    [Output]
    public ITaskItem[] StaleAssets { get; set; } = [];

    /// <summary>True when there was nothing to go on and the full bundles are kept.</summary>
    [Output]
    public bool Skipped { get; set; }

    public override bool Execute()
    {
        try
        {
            return Run();
        }
        catch (Exception e) when (e is IOException or BadImageFormatException or FormatException or InvalidOperationException or UnauthorizedAccessException)
        {
            Log.LogError("Bit.BlazorUI: {0}", e.Message);
            return false;
        }
    }

    // The codes of the warnings, so a project can silence one through NoWarn rather than turn the trimming off.
    private const string UnresolvedExternalCode = "BITBLAZORUI001";
    private const string NothingScannedCode = "BITBLAZORUI002";
    private const string IconsWithoutPackageCode = "BITBLAZORUI003";

    private static readonly string[] ScanModes = ["None", "TypeReferences", "TypeNames"];

    private bool Run()
    {
        if (TrimScripts is false && TrimScriptModules is false && TrimStyles is false && TrimIcons is false && TrimVendorAssets is false) return StandDown(null);

        var packages = AssetFolders.Select(f => PackageAssets.Load(f.GetMetadata("FullPath"))).ToList();
        var duplicate = packages.GroupBy(p => p.PackageId, StringComparer.Ordinal).FirstOrDefault(g => g.Count() > 1);
        if (duplicate is not null)
        {
            Log.LogError("Bit.BlazorUI: the assets of {0} were handed in twice: {1}.", duplicate.Key, string.Join(", ", duplicate.Select(p => p.Directory)));
            return false;
        }
        if (packages.Count == 0) return StandDown("no bit BlazorUI package assets to trim");

        var libraryNames = new HashSet<string>(packages.Select(p => p.PackageId), StringComparer.OrdinalIgnoreCase);
        var trimmed = string.IsNullOrEmpty(TrimmedAssembliesDirectory) is false;

        // A mode misspelled is the trimming asked for and none given, with nothing said: an error rather than a None.
        var mode = ScanModes.FirstOrDefault(m => string.Equals(m, ScanMode?.Trim(), StringComparison.OrdinalIgnoreCase));
        if (mode is null)
        {
            Log.LogError("Bit.BlazorUI: '{0}' is not a BitBlazorUIScan mode; use {1}.", ScanMode, string.Join(", ", Enumerable.Reverse(ScanModes)));
            return false;
        }
        var scan = trimmed is false && mode != "None";

        // The components a csproj names are the whole of what it renders where nothing else says so. The scripts,
        // stylesheets and icons it names only add to what something else said: a list of glyphs is no list of components.
        if (trimmed is false && scan is false && Components.Length == 0) return StandDown("nothing says what this app uses (no trimmed publish, no scan, no <BitBlazorUIComponent> in the csproj)");

        var presentNames = new HashSet<string>(StringComparer.Ordinal);
        // Sets, not lists: the library's literals repeat once per type that loads them, and every text is searched for
        // each marker, vendor folder and icon name below.
        var strings = new HashSet<string>(StringComparer.Ordinal);
        var appStrings = new HashSet<string>(StringComparer.Ordinal);

        LibraryGraph? graph = null;
        try
        {
            if (trimmed || scan is false)
            {
                // Only the names a csproj gives need the graph here; a trimmed publish answers the rest itself.
                if (Components.Length > 0) graph = LoadGraph(libraryNames, required: true, out _);
                if (Components.Length > 0 && graph is null) return false;
            }

            if (trimmed)
            {
                if (ReadTrimmed(libraryNames, presentNames, strings, appStrings) is false) return StandDown($"no trimmed bit BlazorUI assembly in '{TrimmedAssembliesDirectory}' - the publish was set up to be trimmed and the packages were not");
            }
            else if (scan)
            {
                graph = LoadGraph(libraryNames, required: ScanRequested, out var missing);
                if (graph is null)
                {
                    if (Log.HasLoggedErrors) return false;
                    return StandDown(missing!, MessageImportance.High);
                }

                var result = AppScan.Scan(ScanAssemblies.Select(a => a.GetMetadata("FullPath")), libraryNames, graph, byTypeNames: mode == "TypeNames");

                // Every app that renders a component references the packages, so none doing so means the assemblies read
                // were not the app's: trimming on that would leave out every script and stylesheet it uses.
                if (result.Referencing == 0)
                {
                    Log.LogWarning(null, NothingScannedCode, null, null, 0, 0, 0, 0,
                        "Bit.BlazorUI: none of the {0} assemblies read references the bit BlazorUI packages, so there is nothing to tell what this app uses from; publishing the full scripts and stylesheets. Add a <BitBlazorUIScanAssembly> item for each of the app's own assemblies, or set <BitBlazorUIScan>None</BitBlazorUIScan> to silence this.",
                        ScanAssemblies.Length);
                    Skipped = true;
                    return true;
                }

                foreach (var s in result.Strings) appStrings.Add(s);

                var reached = graph.Closure(result.Roots);
                foreach (var type in reached) presentNames.Add(type.Name);
                strings.UnionWith(LibraryGraph.StringsOf(reached));

                Log.LogMessage(MessageImportance.Low, "Bit.BlazorUI: read {0} for what they use of the packages; the library types they reference: {1}.",
                    string.Join(", ", result.Scanned.Select(Path.GetFileName)),
                    string.Join(", ", result.Roots.Where(r => r.FullName.IndexOf('/') < 0).Select(r => r.Name).Distinct().OrderBy(n => n, StringComparer.Ordinal)));
            }

            foreach (var item in Components)
            {
                var name = item.ItemSpec.Trim();
                var types = graph!.FindByName(name).ToList();
                if (types.Count == 0)
                {
                    Log.LogError("Bit.BlazorUI: <BitBlazorUIComponent Include=\"{0}\" /> names no type of the bit BlazorUI packages this app references.", name);
                    continue;
                }

                var reached = graph.Closure(types);
                foreach (var type in reached) presentNames.Add(type.Name);
                strings.UnionWith(LibraryGraph.StringsOf(reached));
            }
            if (Log.HasLoggedErrors) return false;
        }
        finally
        {
            graph?.Dispose();
        }

        // The app's own markup and scripts: their text for what it says (the bit-theme attributes, an icon's class), and
        // every BitBlazorUI.x.y in them as an identifier of its own - a script of the app calling into the library
        // (BitBlazorUI.Theme.onChange) keeps the script it calls, as an interop call of its code does.
        foreach (var file in ScanFiles)
        {
            var path = file.GetMetadata("FullPath");
            if (File.Exists(path) is false) continue;
            // The packages' own files (a ProjectReference's static web assets are of the Project source type, like the
            // app's) are what is being trimmed, not what the app says.
            if (libraryNames.Contains(file.GetMetadata("SourceId"))) continue;
            // Nor are the framework's: the .NET 10 SDK hands blazor.web.js and its siblings in as assets the app
            // discovered, under the _framework folder no app writes its own files to - and their text names a word of
            // every kind (Delete, Copy, Search), each one a glyph kept for nothing.
            if (file.GetMetadata("RelativePath").Replace('\\', '/').TrimStart('/').StartsWith("_framework/", StringComparison.OrdinalIgnoreCase)) continue;

            appStrings.Add(File.ReadAllText(path));
        }

        // The same for the app's own string literals, where markup of its own lands: an inline <script> of a .razor page.
        foreach (var text in appStrings.ToList())
        {
            if (text.IndexOf("BitBlazorUI", StringComparison.Ordinal) < 0) continue;
            foreach (var identifier in AssetSelection.IdentifiersIn(text)) appStrings.Add(identifier);
        }

        var texts = new HashSet<string>(strings, StringComparer.Ordinal);
        texts.UnionWith(appStrings);

        var selection = new AssetSelection(packages);
        selection.Select(presentNames, texts);

        foreach (var item in Assets)
        {
            if (selection.KeepByName(item.ItemSpec.Trim()) is false)
            {
                Log.LogError("Bit.BlazorUI: <BitBlazorUIAsset Include=\"{0}\" /> names no script or component stylesheet of the bit BlazorUI packages this app references.", item.ItemSpec.Trim());
            }
        }

        // Each icon set's glyphs, read once for the selection and the bundle alike - and only when they are trimmed.
        var iconSets = TrimIcons ? packages.SelectMany(p => p.IconSets.Select(s => (Package: p, Set: s, Glyphs: AssetComposer.ReadGlyphs(p, s)))).ToList() : [];
        if (iconSets.Count > 0)
        {
            var known = iconSets.SelectMany(s => s.Glyphs.Rules.Select(g => g.Key)).ToList();
            selection.SelectIcons(texts, known, iconSets.Select(s => s.Set.ClassPrefix));

            var knownSet = new HashSet<string>(known, StringComparer.Ordinal);
            foreach (var item in Icons)
            {
                var name = item.ItemSpec.Trim();
                if (knownSet.Contains(name)) selection.Icons.Add(name);
                else Log.LogError("Bit.BlazorUI: <BitBlazorUIIcon Include=\"{0}\" /> names no icon of Bit.BlazorUI.Icons.", name);
            }
        }
        else if (Icons.Length > 0 && packages.All(p => p.IconSets.Count == 0))
        {
            Log.LogWarning(null, IconsWithoutPackageCode, null, null, 0, 0, 0, 0, "Bit.BlazorUI: <BitBlazorUIIcon> items are given, but this app does not reference Bit.BlazorUI.Icons.");
        }
        else if (Icons.Length > 0 && TrimIcons is false)
        {
            // Not a warning: a csproj may well list the icons it reads from data once, and turn the icon trimming on for some
            // publishes only. But said where a publish says what it did, since the items do nothing here.
            Log.LogMessage(MessageImportance.High, "Bit.BlazorUI: the <BitBlazorUIIcon> items keep nothing, since BitBlazorUITrimIcons is off and the icon stylesheet is published whole.");
        }
        if (Log.HasLoggedErrors) return false;

        foreach (var unresolved in selection.UnresolvedIdentifiers)
        {
            Log.LogMessage(MessageImportance.Low, "Bit.BlazorUI: '{0}' looks like a call into the packages' scripts but no script exports it.", unresolved);
        }

        // What a script of one package uses of another's that no package of the app exports: the packages are of different
        // versions, and the script fails where it calls it - trimmed or not.
        foreach (var (path, user) in selection.UnresolvedExternals)
        {
            Log.LogWarning(null, UnresolvedExternalCode, null, null, 0, 0, 0, 0,
                "Bit.BlazorUI: {0} uses BitBlazorUI.{1}, which no bit BlazorUI package of this app exports. Reference the same version of every bit BlazorUI package.", user, path);
        }

        var outputs = new List<AssetComposer.Output>();
        foreach (var package in packages)
        {
            if (TrimScripts) outputs.AddRange(AssetComposer.ComposeScripts(package, selection, OutputDirectory));
            if (TrimScriptModules) outputs.AddRange(AssetComposer.ComposeBoot(package, selection, OutputDirectory));
            if (TrimStyles) outputs.AddRange(AssetComposer.ComposeStyles(package, selection, OutputDirectory));
        }
        foreach (var (package, set, glyphs) in iconSets) outputs.Add(AssetComposer.ComposeIcons(package, set, glyphs, selection, OutputDirectory));

        Report(packages, selection, outputs);

        var removed = new List<ITaskItem>();
        if (TrimVendorAssets) removed.AddRange(RemoveVendorAssets(texts));
        if (TrimScriptModules) removed.AddRange(RemoveScriptModules(packages, selection));
        RemovedAssets = [.. removed];

        return Swap(outputs);
    }

    // A module of the lazy scripts is imported for an identifier naming it, or for a script that uses it - so one the
    // selection does not keep is a file no code the app can run will ever import, and the publish leaves it out. It is the
    // very selection the bundles are put back together from, so a module is published exactly where its piece is bundled.
    private List<ITaskItem> RemoveScriptModules(List<PackageAssets> packages, AssetSelection selection)
    {
        var removed = new List<ITaskItem>();

        foreach (var package in packages)
        {
            var modules = package.ScriptModules.Where(m => m.ModulePath is not null).ToList();
            var dropped = new HashSet<string>(modules.Where(m => selection.Scripts.Contains(m) is false).Select(m => "/" + m.ModulePath!), StringComparer.OrdinalIgnoreCase);
            if (dropped.Count == 0) continue;

            var primaries = StaticWebAssets
                .Where(a => string.Equals(a.GetMetadata("SourceId"), package.PackageId, StringComparison.OrdinalIgnoreCase)
                    && string.IsNullOrEmpty(a.GetMetadata("RelatedAsset"))
                    && dropped.Any(suffix => Normalize(a.GetMetadata("FullPath")).EndsWith(suffix, StringComparison.OrdinalIgnoreCase)))
                .ToList();
            if (primaries.Count == 0) continue;

            var paths = new HashSet<string>(primaries.Select(p => Normalize(p.GetMetadata("FullPath"))), StringComparer.OrdinalIgnoreCase);
            removed.AddRange(primaries.Select(WithAssetFile));
            removed.AddRange(StaticWebAssets
                .Where(a => string.IsNullOrEmpty(a.GetMetadata("RelatedAsset")) is false && paths.Contains(Normalize(Path.GetFullPath(a.GetMetadata("RelatedAsset")))))
                .Select(WithAssetFile));

            Log.LogMessage(MessageImportance.High, "Bit.BlazorUI: {0} lazy script modules -> {1} of {2} ({3} left out: nothing this app can run imports them).",
                package.PackageId, modules.Count - primaries.Count, modules.Count, primaries.Count);
        }

        return removed;
    }

    // A vendor folder is loaded by the code that names it - "_content/Bit.BlazorUI.Legacy/pdf.js/pdfjs.js" is a literal
    // of BitPdfReaderLegacy - or by the app's own markup, which is read along with it. Nothing naming the folder, nothing
    // can request a file of it, and the publish leaves the whole folder out.
    private List<ITaskItem> RemoveVendorAssets(IReadOnlyCollection<string> texts)
    {
        var removed = new List<ITaskItem>();

        foreach (var vendor in VendorFolders)
        {
            var package = vendor.GetMetadata("Package");
            var folder = vendor.ItemSpec.Replace('\\', '/').Trim('/');
            if (string.IsNullOrEmpty(package) || folder.Length == 0) continue;

            var url = $"_content/{package}/{folder}/";
            if (texts.Any(t => t.IndexOf(url, StringComparison.OrdinalIgnoreCase) >= 0)) continue;

            var primaries = StaticWebAssets
                .Where(a => string.Equals(a.GetMetadata("SourceId"), package, StringComparison.OrdinalIgnoreCase)
                    && string.IsNullOrEmpty(a.GetMetadata("RelatedAsset"))
                    && a.GetMetadata("RelativePath").Replace('\\', '/').TrimStart('/').StartsWith(folder + "/", StringComparison.OrdinalIgnoreCase))
                .ToList();
            if (primaries.Count == 0) continue;

            var paths = new HashSet<string>(primaries.Select(p => Normalize(p.GetMetadata("FullPath"))), StringComparer.OrdinalIgnoreCase);
            removed.AddRange(primaries.Select(WithAssetFile));
            removed.AddRange(StaticWebAssets
                .Where(a => string.IsNullOrEmpty(a.GetMetadata("RelatedAsset")) is false && paths.Contains(Normalize(Path.GetFullPath(a.GetMetadata("RelatedAsset")))))
                .Select(WithAssetFile));

            Log.LogMessage(MessageImportance.High, "Bit.BlazorUI: {0} -> left out ({1} files): nothing this app can run loads it.", url, primaries.Count);
        }

        return removed;
    }

    private bool StandDown(string? why, MessageImportance importance = MessageImportance.Normal)
    {
        if (why is not null) Log.LogMessage(importance, "Bit.BlazorUI: {0}; publishing the full scripts and stylesheets.", why);
        Skipped = true;
        return true;
    }

    // The trimmed copies of the library assemblies name exactly the types and identifiers the app can still reach; the
    // app's own assemblies beside them add the string literals it passes in (icon names, an identifier it calls itself).
    private bool ReadTrimmed(ISet<string> libraryNames, ISet<string> presentNames, ISet<string> strings, ISet<string> appStrings)
    {
        if (Directory.Exists(TrimmedAssembliesDirectory) is false) return false;

        var found = false;
        foreach (var path in Directory.EnumerateFiles(TrimmedAssembliesDirectory!, "*.dll"))
        {
            if (libraryNames.Contains(Path.GetFileNameWithoutExtension(path)) is false) continue;

            using var pe = new PEReader(File.OpenRead(path));
            var reader = pe.GetMetadataReader();
            found = true;

            foreach (var handle in reader.TypeDefinitions)
            {
                var name = reader.GetString(reader.GetTypeDefinition(handle).Name);
                var tick = name.IndexOf('`');
                presentNames.Add(tick < 0 ? name : name.Substring(0, tick));
            }

            strings.UnionWith(AppScan.ReadUserStrings(reader));
        }

        if (found)
        {
            var app = AppScan.Scan(Directory.EnumerateFiles(TrimmedAssembliesDirectory!, "*.dll"), libraryNames, graph: null);
            foreach (var s in app.Strings) appStrings.Add(s);
        }

        return found;
    }

    /// <summary>
    /// The untrimmed library assemblies as one graph; null, with an error logged when they are required and the reason
    /// handed back when they are not, where they cannot be told.
    /// </summary>
    private LibraryGraph? LoadGraph(ISet<string> libraryNames, bool required, out string? missing)
    {
        missing = null;

        var byName = LibraryAssemblies
            .Select(a => a.GetMetadata("FullPath"))
            .Where(p => libraryNames.Contains(Path.GetFileNameWithoutExtension(p)))
            .GroupBy(p => Path.GetFileNameWithoutExtension(p), StringComparer.OrdinalIgnoreCase)
            .ToList();

        var ambiguous = byName.FirstOrDefault(g => g.Distinct(StringComparer.OrdinalIgnoreCase).Count() > 1);
        if (ambiguous is not null)
        {
            var why = string.Format(CultureInfo.InvariantCulture, "more than one {0}.dll is referenced ({1}), so there is no telling which one this app compiled against. Set BitBlazorUILibraryAssembly to the one it did", ambiguous.Key, string.Join(", ", ambiguous.Distinct()));
            if (required) Log.LogError("Bit.BlazorUI: {0}.", why);
            else missing = why;
            return null;
        }

        var paths = byName.Select(g => g.First()).Where(File.Exists).ToList();
        if (paths.Count == 0)
        {
            const string why = "the untrimmed bit BlazorUI assemblies are needed to work out what the app uses, and none was found among its references. Set BitBlazorUILibraryAssembly to them";
            if (required) Log.LogError("Bit.BlazorUI: {0}.", why);
            else missing = why;
            return null;
        }

        return LibraryGraph.Load(paths);
    }

    private void Report(List<PackageAssets> packages, AssetSelection selection, List<AssetComposer.Output> outputs)
    {
        // The sizes are formatted here, in the invariant culture: the logger would format them in the culture of the machine
        // running the build, so one line would read differently on every build agent.
        foreach (var output in outputs)
        {
            Log.LogMessage(MessageImportance.High, "Bit.BlazorUI: {0} -> {1} of {2} pieces, {3} of {4} bytes ({5}).",
                $"{output.PackageId}/{output.Bundle}", output.KeptPieces, output.AllPieces,
                output.TrimmedLength.ToString("N0", CultureInfo.InvariantCulture), output.FullLength.ToString("N0", CultureInfo.InvariantCulture),
                output.FullLength == 0 ? "-" : $"-{Math.Round(100.0 * (output.FullLength - output.TrimmedLength) / output.FullLength, MidpointRounding.AwayFromZero).ToString(CultureInfo.InvariantCulture)}%");
        }

        foreach (var package in packages)
        {
            if ((TrimScripts || TrimScriptModules) && package.ScriptModules.Count > 0)
            {
                Log.LogMessage(MessageImportance.Low, "Bit.BlazorUI: {0} scripts kept: {1}.", package.PackageId, string.Join(", ", package.ScriptModules.Where(selection.Scripts.Contains).Select(m => m.Name)));
            }
            if (TrimStyles && package.StyleChunks.Count > 0)
            {
                Log.LogMessage(MessageImportance.Low, "Bit.BlazorUI: {0} component stylesheets kept: {1}.", package.PackageId, string.Join(", ", package.StyleChunks.Where(selection.Styles.Contains).Select(c => c.Name)));
            }
        }

        if (TrimIcons && selection.Icons.Count > 0)
        {
            Log.LogMessage(MessageImportance.Low, "Bit.BlazorUI: icons kept: {0}.", string.Join(", ", selection.Icons.OrderBy(i => i, StringComparer.Ordinal)));
        }
    }

    // Each trimmed bundle becomes a candidate asset carrying the metadata of the package asset it replaces - the same
    // source, base path and relative path, so it is served at the same URL - minus what is computed from the content
    // (fingerprint, integrity), which DefineStaticWebAssets works out again from the trimmed file.
    private bool Swap(List<AssetComposer.Output> outputs)
    {
        var candidates = new List<ITaskItem>();
        var stale = new List<ITaskItem>();

        foreach (var output in outputs)
        {
            var suffix = "/" + output.Bundle.Replace('\\', '/');
            var matches = StaticWebAssets
                .Where(a => string.Equals(a.GetMetadata("SourceId"), output.PackageId, StringComparison.OrdinalIgnoreCase)
                    && string.Equals(a.GetMetadata("AssetRole"), "Primary", StringComparison.OrdinalIgnoreCase)
                    && Normalize(a.GetMetadata("FullPath")).EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (matches.Count == 0)
            {
                Log.LogMessage(MessageImportance.Normal, "Bit.BlazorUI: no {0} static web asset of {1} to replace; leaving it out.", output.Bundle, output.PackageId);
                continue;
            }

            if (matches.Count > 1)
            {
                Log.LogError("Bit.BlazorUI: expected a single {0} static web asset of {1} but found {2}: {3}. Set BitBlazorUITrimAssets to false to publish the full bundles instead.",
                    output.Bundle, output.PackageId, matches.Count, string.Join(", ", matches.Select(m => m.ItemSpec)));
                return false;
            }

            var original = matches[0];
            var originalPath = Normalize(original.GetMetadata("FullPath"));

            var contentRoot = Path.Combine(OutputDirectory, output.PackageId);
            contentRoot = Path.GetFullPath(contentRoot).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;

            var candidate = new TaskItem(Path.GetFullPath(output.Path));
            foreach (var name in CopiedMetadata) candidate.SetMetadata(name, original.GetMetadata(name));
            candidate.SetMetadata("ContentRoot", contentRoot);
            candidate.SetMetadata("AssetKind", "Publish");
            candidate.SetMetadata("CopyToOutputDirectory", "Never");
            candidate.SetMetadata("CopyToPublishDirectory", "PreserveNewest");
            candidate.SetMetadata("OriginalItemSpec", Path.GetFullPath(output.Path));
            candidates.Add(candidate);

            stale.Add(WithAssetFile(original));
            foreach (var derived in StaticWebAssets.Where(a => string.IsNullOrEmpty(a.GetMetadata("RelatedAsset")) is false
                && string.Equals(Normalize(Path.GetFullPath(a.GetMetadata("RelatedAsset"))), originalPath, StringComparison.OrdinalIgnoreCase)))
            {
                stale.Add(WithAssetFile(derived));
            }
        }

        CandidateAssets = [.. candidates];
        StaleAssets = [.. stale];
        return Log.HasLoggedErrors is false;
    }

    private static readonly string[] CopiedMetadata =
    [
        "SourceId", "SourceType", "BasePath", "RelativePath", "AssetMode", "AssetRole", "AssetMergeSource",
        "AssetMergeBehavior", "RelatedAsset", "AssetTraitName", "AssetTraitValue",
    ];

    // Endpoints are keyed by the file they serve (their AssetFile), which is what an item removing them matches on.
    private static ITaskItem WithAssetFile(ITaskItem asset)
    {
        var item = new TaskItem(asset);
        item.SetMetadata("AssetFile", asset.GetMetadata("FullPath"));
        return item;
    }

    private static string Normalize(string path) => path.Replace('\\', '/');
}
