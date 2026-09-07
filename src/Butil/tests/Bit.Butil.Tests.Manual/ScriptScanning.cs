using Bit.Butil.Build;

namespace ButilTests.Manual;

/// <summary>
/// The signals an <em>untrimmed</em> publish trims its JavaScript on: the map from Bit.Butil class to
/// JavaScript module, the scan of a consumer's own assemblies that uses it, and the module list a consumer
/// writes into their csproj.
/// </summary>
/// <remarks>
/// <see cref="ScriptTrimming"/> checks the trimmed publish's signal - the interop identifiers ILLink leaves
/// behind - against <see cref="ScriptTrimming.MustSurviveModules"/>, the modules this project's own code
/// calls. This file's ground truth is <see cref="ScriptTrimming.InjectedReferenceModules"/> and
/// <see cref="ScriptTrimming.ScanReachableModules"/>: the same sets plus the two the reference closure reaches
/// without the code calling them. That is the whole point - the map and the scan are meant to reach the same
/// answer about the same code without ILLink having run, erring only towards <em>more</em>. If they ever
/// diverge downwards, an untrimmed consumer publishes a bundle missing JavaScript their app calls, and finds
/// out in a browser.
/// <br/>
/// Only run untrimmed. Everything here starts from the <em>untrimmed</em> Bit.Butil.dll - the map says which
/// module each class needs, which is a question about the library as shipped, not about what survived one
/// app's trimming - and a trimmed self-contained publish has no such assembly to read.
/// </remarks>
internal static class ScriptScanning
{
    public static (int Passed, int Failed) Run(string? butilAssemblyPath, bool trimmed, List<string> failures)
    {
        var checks = new ScriptBundling.Checks(failures, "script scanning");

        if (trimmed)
        {
            // Not a failure, and not silence either: the count printed by the report would otherwise look
            // like the checks passed.
            return (0, 0);
        }

        if (string.IsNullOrEmpty(butilAssemblyPath) || File.Exists(butilAssemblyPath) is false)
        {
            checks.That(false, "the class-to-module map was not checked", "there is no Bit.Butil assembly file to read (a single-file build)");
            return (checks.Passed, checks.Failed);
        }

        var workspace = Path.Combine(AppContext.BaseDirectory, "script-scanning");
        if (Directory.Exists(workspace)) Directory.Delete(workspace, recursive: true);
        Directory.CreateDirectory(workspace);

        var map = ButilTypeModules.Build(butilAssemblyPath);
        if (checks.That(map.IsEmpty is false, "the class-to-module map is read out of Bit.Butil.dll",
                $"{butilAssemblyPath} produced no mapped types at all") is false)
        {
            return (checks.Passed, checks.Failed);
        }

        CheckMapCoverage(checks, butilAssemblyPath, map);
        CheckMapAgreesWithTrimming(checks, map);
        CheckScanFindsTheSameModules(checks, map);
        CheckExplicitNames(checks, butilAssemblyPath, map);
        CheckUnreadableInputs(checks, workspace, butilAssemblyPath, map);
        CheckScannedBundleRuns(checks, workspace, map);

        return (checks.Passed, checks.Failed);
    }

    /// <summary>
    /// Every module the library calls is behind some class. A module the map cannot attribute is one no
    /// scan can ever conclude, so a consumer relying on the scan would silently never get it.
    /// </summary>
    private static void CheckMapCoverage(ScriptBundling.Checks checks, string butilAssemblyPath, ButilTypeModules map)
    {
        var named = ButilScriptBundler.ReadReferencedModules(butilAssemblyPath);
        var attributed = new SortedSet<string>(map.FullTypeNames.SelectMany(map.ForFullName), StringComparer.Ordinal);
        var unattributed = named.Except(attributed, StringComparer.Ordinal).ToArray();

        checks.That(unattributed.Length == 0,
            "every module the library calls is reachable from some Bit.Butil class",
            $"[{string.Join(", ", unattributed)}] {(unattributed.Length == 1 ? "is named" : "are named")} by an interop identifier but no class leads to {(unattributed.Length == 1 ? "it" : "them")}, so a scan could never include {(unattributed.Length == 1 ? "it" : "them")}");
    }

    /// <summary>
    /// The check this file exists for: the map, asked about exactly the classes
    /// <see cref="ConsumerComponent"/> injects, must answer what a reference closure over exactly that code
    /// reaches - which is what ILLink concludes for those classes plus the two modules named in
    /// <see cref="ScriptTrimming.InjectedReferenceModules"/>.
    /// </summary>
    /// <remarks>
    /// It is a real test of the closure and not a restatement of it. <c>LocalStorage</c> carries no interop
    /// identifiers of its own - they are on the <c>ButilStorage</c> base class - and <c>Window</c> reaches
    /// the <c>events</c> module only through an internal interop class, called from inside a compiler-
    /// generated async state machine. A map built from anything less than the reference closure gets both
    /// of those wrong, and the difference shows up here as a missing module.
    /// </remarks>
    private static void CheckMapAgreesWithTrimming(ScriptBundling.Checks checks, ButilTypeModules map)
    {
        var actual = new SortedSet<string>(StringComparer.Ordinal);
        foreach (var type in ConsumerComponent.InjectedTypes)
        {
            var modules = map.ForFullName(type.FullName ?? type.Name);
            checks.That(modules.Count > 0, $"the map knows which JavaScript {type.Name} needs", "it maps to no module at all");
            foreach (var module in modules) actual.Add(module);
        }

        Compare(checks, ScriptTrimming.InjectedReferenceModules, actual,
            "the classes ConsumerComponent injects map to exactly the modules a reference closure over them reaches",
            missing => $"the map does not lead from any injected class to [{missing}], so an untrimmed publish would drop JavaScript the app calls",
            extra => $"the map leads to [{extra}], which nothing in this project reaches even by reference - the closure is following something it should not");
    }

    /// <summary>
    /// The scan, over this harness's own assembly, has to reach every module this project's code needs: the
    /// injected classes named the way a consumer's assembly names them, plus the three services
    /// <see cref="CancellationContract"/> constructs directly.
    /// </summary>
    private static void CheckScanFindsTheSameModules(ScriptBundling.Checks checks, ButilTypeModules map)
    {
        var self = typeof(ScriptScanning).Assembly.Location;
        if (checks.That(string.IsNullOrEmpty(self) is false, "the scan was checked against a real assembly", "this harness has no assembly file") is false) return;

        var references = ButilConsumerScan.Scan([self], map, ButilScanMode.TypeReferences);
        checks.That(references.Scanned.Count == 1, "an assembly that references Bit.Butil is recognised as one to read", $"{references.Scanned.Count} of 1 assemblies were read");

        Compare(checks, ScriptTrimming.ScanReachableModules, references.Modules,
            "TypeReferences over this assembly finds exactly the modules its Butil classes reach",
            missing => $"the scan missed [{missing}] - an untrimmed publish of this app would ship a bundle without it",
            extra => $"the scan added [{extra}], which nothing here reaches even by reference");

        // TypeNames matches on the bare name, so it cannot miss what TypeReferences found and may well find
        // more - that is the trade the mode exists to make, and the direction of it is what is asserted.
        var names = ButilConsumerScan.Scan([self], map, ButilScanMode.TypeNames);
        var missed = references.Modules.Except(names.Modules, StringComparer.Ordinal).ToArray();
        checks.That(missed.Length == 0,
            "TypeNames never finds less than TypeReferences",
            $"it missed [{string.Join(", ", missed)}], so the coarser mode is the less safe one in the wrong direction");

        // Nothing to find is reported as nothing read, not as "this app calls no JavaScript" - which, acted
        // on, would trim every module away.
        var framework = typeof(object).Assembly.Location;
        var unrelated = ButilConsumerScan.Scan([framework], map, ButilScanMode.TypeReferences);
        checks.That(unrelated.Scanned.Count == 0 && unrelated.Modules.Count == 0,
            "an assembly that does not reference Bit.Butil counts for nothing",
            $"{Path.GetFileName(framework)} was read as referencing Bit.Butil and contributed {unrelated.Modules.Count} module(s)");

        // The library's own assembly names every one of its types, so reading it would conclude that every
        // app uses every module.
        var itself = ButilConsumerScan.Scan([typeof(Bit.Butil.BitButil).Assembly.Location], map, ButilScanMode.TypeReferences);
        checks.That(itself.Scanned.Count == 0,
            "Bit.Butil's own assembly is left out of the scan",
            $"it was read, and contributed {itself.Modules.Count} module(s) - which would defeat the whole scan");
    }

    /// <summary>
    /// <see cref="ButilScriptBundler.ResolveNames"/>: what a consumer may write in
    /// <c>&lt;BitButilScriptModule&gt;</c>, and what happens to what they may not.
    /// </summary>
    private static void CheckExplicitNames(ScriptBundling.Checks checks, string butilAssemblyPath, ButilTypeModules map)
    {
        var butilRoot = ScriptTrimming.LocateButilProject();
        var manifestPath = butilRoot is null ? null : Path.Combine(butilRoot, "obj", "butil-js", "chunks", "manifest.txt");
        if (manifestPath is null || File.Exists(manifestPath) is false)
        {
            checks.That(false, "the csproj module list was not checked", "Bit.Butil's JavaScript build outputs are missing");
            return;
        }

        var manifest = ButilScriptBundler.ReadManifest(manifestPath);

        (string[] Names, string[] Expected, string Because)[] cases =
        [
            (["clipboard"], ["clipboard"], "a module name is a module"),
            (["Clipboard"], ["clipboard"], "a Bit.Butil class name resolves to the module behind it"),
            (["Bit.Butil.Clipboard"], ["clipboard"], "a class can be named in full"),
            (["LocalStorage"], ["storage"], "a class whose module is named nothing like it still resolves - the map, not the spelling, decides"),
            (["Window"], ["events", "window"], "a class needing more than one module contributes all of them"),
            (["CLIPBOARD"], ["clipboard"], "a module named in the wrong case is understood rather than rejected"),
            (["clipboard", "Clipboard"], ["clipboard"], "the same module reached two ways is one module"),
            ([" clipboard ", ""], ["clipboard"], "surrounding space is trimmed and an empty entry is ignored"),
        ];

        foreach (var (names, expected, because) in cases)
        {
            var modules = ButilScriptBundler.ResolveNames(names, manifest, map, out var unresolved);
            checks.That(unresolved.Count == 0 && modules.SequenceEqual(expected, StringComparer.Ordinal),
                because,
                $"[{string.Join(", ", names)}] resolved to [{string.Join(", ", modules)}]{(unresolved.Count == 0 ? string.Empty : $" with [{string.Join(", ", unresolved)}] unresolved")}");
        }

        // The one that must NOT quietly resolve: MSBuild accepts a misspelled item without a word, so the
        // build has to be the thing that says something.
        ButilScriptBundler.ResolveNames(["Clippboard", "clipboard"], manifest, map, out var missing);
        checks.That(missing.Count == 1 && missing[0] == "Clippboard",
            "a name that is neither a module nor a class is reported rather than ignored",
            $"unresolved: [{string.Join(", ", missing)}]");

        // Without the map only module names can resolve, which is what a publish that never had reason to
        // read Bit.Butil.dll gets. A class named like its module (Clipboard/clipboard) still resolves on the
        // case-insensitive pass; one named unlike it cannot, and is reported rather than dropped.
        var withoutMap = ButilScriptBundler.ResolveNames(["clipboard", "Clipboard", "LocalStorage"], manifest, null, out var withoutMapMissing);
        checks.That(withoutMap.SequenceEqual(["clipboard"], StringComparer.Ordinal)
                && withoutMapMissing.SequenceEqual(["LocalStorage"], StringComparer.Ordinal),
            "without the class map a class named unlike its module is reported rather than dropped",
            $"resolved [{string.Join(", ", withoutMap)}], unresolved [{string.Join(", ", withoutMapMissing)}]");

        checks.That(File.Exists(butilAssemblyPath), "the map was built from a file that is really there");
    }

    /// <summary>
    /// What the metadata reader does with a file that is not what it was told it is. These run inside a
    /// consumer's publish over whatever their references resolved to, so "reported" versus "crashed the
    /// build" is decided here.
    /// </summary>
    private static void CheckUnreadableInputs(ScriptBundling.Checks checks, string workspace, string butilAssemblyPath, ButilTypeModules map)
    {
        var text = Path.Combine(workspace, "not-an-assembly.dll");
        File.WriteAllText(text, "A text file that a consumer's build pointed the task at.");

        var truncated = Path.Combine(workspace, "truncated.dll");
        File.WriteAllBytes(truncated, [.. File.ReadAllBytes(butilAssemblyPath).Take(512)]);

        checks.Throws<BadImageFormatException>(() => ButilTypeModules.Build(text),
            "a file that is not a PE image is reported as a bad image rather than crashing the build");
        checks.Throws<BadImageFormatException>(() => ButilTypeModules.Build(truncated),
            "an assembly cut short is reported as a bad image rather than read past its end");
        checks.Throws<IOException>(() => ButilTypeModules.Build(Path.Combine(workspace, "not-there.dll")),
            "an assembly that is not there is reported as an IO failure the publish can explain");

        // The scan, by contrast, must not fail a publish over any of them: the list it is handed is whatever
        // a consumer's references resolved to, native libraries and stale paths included.
        var scan = ButilConsumerScan.Scan([text, truncated, Path.Combine(workspace, "not-there.dll"), string.Empty], map, ButilScanMode.TypeReferences);
        checks.That(scan.Skipped.Count == 3 && scan.Scanned.Count == 0 && scan.Modules.Count == 0,
            "a scan passes over what it cannot read instead of failing the publish",
            $"{scan.Skipped.Count} skipped, {scan.Scanned.Count} read, {scan.Modules.Count} module(s)");
    }

    /// <summary>
    /// The end of it: assemble the bundle a scan-trimmed publish of this harness would serve, and run it.
    /// </summary>
    /// <remarks>
    /// Every check above compares one list of module names with another, and a module set that is right on
    /// paper still ships broken JavaScript if the chunks behind it do not stand alone - a module whose
    /// dependency the manifest does not record evaluates to a <c>BitButil.x is undefined</c> the moment
    /// something calls it. <c>verify-bundle.mjs</c> evaluates the assembled file and asks which
    /// <c>BitButil</c> namespaces it registered, which is the only check here that the browser would agree
    /// with. <see cref="ScriptBundling"/> does the same for the bundle ILLink's own answer produces; this is
    /// the same artifact for the publish that never ran ILLink.
    /// </remarks>
    private static void CheckScannedBundleRuns(ScriptBundling.Checks checks, string workspace, ButilTypeModules map)
    {
        var verifier = Path.Combine(AppContext.BaseDirectory, ScriptBundling.VerifierFileName);
        var butilRoot = ScriptTrimming.LocateButilProject();
        var self = typeof(ScriptScanning).Assembly.Location;
        if (butilRoot is null || string.IsNullOrEmpty(self)) return;   // Already reported above.

        var chunks = Path.Combine(butilRoot, "obj", "butil-js", "chunks");
        var manifestPath = Path.Combine(chunks, "manifest.txt");
        if (File.Exists(verifier) is false || File.Exists(manifestPath) is false)
        {
            checks.That(false, "the scanned bundle was not run", $"{ScriptBundling.VerifierFileName} or the module manifest is missing");
            return;
        }

        var manifest = ButilScriptBundler.ReadManifest(manifestPath);

        // Exactly what the MSBuild task assembles for an untrimmed publish: the scan's modules, plus the ones
        // named in a csproj, closed over the manifest's dependencies.
        var scan = ButilConsumerScan.Scan([self], map, ButilScanMode.TypeReferences);
        var referenced = new SortedSet<string>(scan.Modules, StringComparer.Ordinal);
        referenced.UnionWith(ButilScriptBundler.ResolveNames(["Battery"], manifest, map, out _));

        var included = ButilScriptBundler.Resolve(manifest, referenced, out var unknown);
        checks.That(unknown.Count == 0, "everything a scan concludes is a module the library ships", $"[{string.Join(", ", unknown)}] is not");
        checks.That(included.Contains("battery"),
            "a module named in the csproj survives into the bundle beside the ones the scan found",
            $"the bundle holds [{string.Join(", ", included)}]");

        var bundle = Path.Combine(workspace, "scanned-bundle.js");
        ButilScriptBundler.WriteBundle(chunks, included, bundle);
        ScriptBundling.RunVerifier(checks, verifier, ScriptBundling.Keys(included), bundle);
    }

    private static void Compare(ScriptBundling.Checks checks, IEnumerable<string> expected, IEnumerable<string> actual,
        string what, Func<string, string> onMissing, Func<string, string> onExtra)
    {
        var missing = expected.Except(actual, StringComparer.Ordinal).OrderBy(name => name, StringComparer.Ordinal).ToArray();
        var extra = actual.Except(expected, StringComparer.Ordinal).OrderBy(name => name, StringComparer.Ordinal).ToArray();

        if (missing.Length == 0 && extra.Length == 0)
        {
            checks.That(true, what);
            return;
        }

        var detail = missing.Length > 0 ? onMissing(string.Join(", ", missing)) : onExtra(string.Join(", ", extra));
        if (missing.Length > 0 && extra.Length > 0) detail += $"; it also {onExtra(string.Join(", ", extra))}";

        checks.That(false, what, detail);
    }
}
