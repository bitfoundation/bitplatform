using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Bit.Butil.Build;

namespace ButilTests.Manual;

/// <summary>
/// The publish-time bundler itself, under test: the code a consumer's publish runs to work out which
/// Bit.Butil JavaScript modules a trimmed app still calls and to assemble a bundle from them.
/// </summary>
/// <remarks>
/// <see cref="ScriptTrimming"/> runs that code once, over this harness's own assembly, and asks whether the
/// answer is the expected one - which is the feature working end to end, but only ever exercises the one
/// input this repository happens to produce. The checks here go after the parts of it a real consumer can
/// reach and this repository cannot: an identifier shaped unlike Butil's own, a manifest that does not
/// parse, a chunk that is missing halfway through writing a bundle, a file that is not a managed assembly.
/// Every one of those happens inside someone's build, and the difference between "reported" and "crashed
/// the publish" - or worse, "wrote a bundle missing a module" - is decided by code no other test covers.
/// <br/>
/// It then checks the artifacts the bundler works from and produces, because the whole feature rests on
/// three claims that are nowhere stated in code: the chunks concatenate into exactly the
/// <c>bit-butil.js</c> the package ships, each lazy module file is exactly its own dependency closure, and
/// a bundle assembled from a subset actually runs. The last one is checked by running it - see
/// <c>verify-bundle.mjs</c>.
/// </remarks>
internal static class ScriptBundling
{
    /// <summary>The script that evaluates an assembled bundle, copied next to the executable by the csproj.</summary>
    internal const string VerifierFileName = "verify-bundle.mjs";

    /// <summary>
    /// An interop identifier naming a module Bit.Butil does not ship, present in this assembly for the
    /// heap-scanning checks below to find. It is a plain literal in code that runs, so the trimmer keeps it
    /// in the trimmed harness too - which is the point: the bundler's "the app calls a module that does not
    /// exist" path (the BUTIL001 warning a consumer would see) needs a real assembly to trigger it, and
    /// nothing in Bit.Butil itself can ever be one.
    /// </summary>
    private const string UnknownModuleIdentifier = "BitButil.nosuchmodule.call";

    private const string UnknownModule = "nosuchmodule";

    public static (int Passed, int Failed) Run(string? butilAssemblyPath, List<string> failures)
    {
        var checks = new Checks(failures);
        var workspace = Path.Combine(AppContext.BaseDirectory, "script-bundling");

        // A fresh workspace every run: a bundle or chunk left over from a previous one would let a check
        // that no longer writes anything keep passing.
        if (Directory.Exists(workspace)) Directory.Delete(workspace, recursive: true);
        Directory.CreateDirectory(workspace);

        CheckIdentifierParsing(checks);
        CheckManifestReading(checks, workspace);
        CheckDependencyResolution(checks);
        CheckBundleAssembly(checks, workspace);
        CheckAssemblyStrings(checks, workspace, butilAssemblyPath);

        var butilRoot = ScriptTrimming.LocateButilProject();
        if (butilRoot is null)
        {
            checks.That(false, "the shipped JavaScript was not checked", "the Bit.Butil project folder could not be located from the executable's location - run this harness from inside the repository");
            return (checks.Passed, checks.Failed);
        }

        CheckShippedArtifacts(checks, butilRoot, workspace, butilAssemblyPath);
        CheckAssembledBundlesRun(checks, butilRoot, workspace, butilAssemblyPath);
        CheckPackageLayout(checks, butilRoot);

        return (checks.Passed, checks.Failed);
    }

    /// <summary>
    /// <see cref="ButilScriptBundler.TryGetModule"/>: which string literals count as a call into a Butil
    /// module, and which module they name.
    /// </summary>
    /// <remarks>
    /// This is the entire input to the trimming decision, and it runs over every literal in the assembly -
    /// Butil's and the app's alike - so both ways of getting it wrong are consequential: a literal it fails
    /// to recognise is a module dropped from a bundle the app needs, and a literal it recognises when it
    /// should not is a phantom module name in a consumer's build warning.
    /// </remarks>
    private static void CheckIdentifierParsing(Checks checks)
    {
        (string? Identifier, string? Module, string Because)[] cases =
        [
            ("BitButil.clipboard.readText", "clipboard", "an interop identifier names the module between the prefix and the first dot"),
            ("BitButil.storage.setItem.extra", "storage", "only the first segment after the prefix is the module"),
            ("BitButil.cookie_store.getAll", "cookie_store", "an underscore is part of a module name"),
            ("BitButil.utils2.x", "utils2", "a digit is part of a module name"),
            ("BitButil.clipboard", null, "an identifier with no member after the module is not a call into one"),
            ("BitButil.", null, "the prefix on its own is not a call into a module"),
            ("BitButil..readText", null, "an empty module name is not a module"),
            ("BitButil.clip-board.readText", null, "a module name cannot hold punctuation"),
            ("BitButil.clip board.readText", null, "a module name cannot hold a space"),
            ("bitbutil.clipboard.readText", null, "the prefix is matched case-sensitively"),
            ("MyApp.BitButil.clipboard.readText", null, "the prefix has to start the identifier"),
            ("import", null, "an unrelated identifier names no module"),
            ("", null, "an empty identifier names no module"),
            (null, null, "a null identifier names no module"),
        ];

        foreach (var (identifier, expected, because) in cases)
        {
            var recognized = ButilScriptBundler.TryGetModule(identifier!, out var module);
            var passed = expected is null
                ? recognized is false && module.Length == 0
                : recognized && module == expected;

            checks.That(passed, because,
                $"'{identifier ?? "<null>"}' was read as {(recognized ? $"module '{module}'" : "not an interop identifier")}");
        }
    }

    /// <summary>
    /// <see cref="ButilScriptBundler.ReadManifest"/>: the dependency manifest Bit.Butil's build emits, and
    /// what it does with one that is broken.
    /// </summary>
    /// <remarks>
    /// The manifest is the only thing that closes a bundle over dependencies, so a line silently skipped or
    /// half-read produces a bundle missing JavaScript the app calls - which fails in the browser, not in the
    /// build. Every malformed shape has to throw rather than be tolerated; the MSBuild task turns those into
    /// a build error naming the file.
    /// </remarks>
    private static void CheckManifestReading(Checks checks, string workspace)
    {
        var manifest = ButilScriptBundler.ReadManifest(WriteText(workspace, "manifest-valid.txt",
            "# the prelude first, the way build.mjs writes it\r\n" +
            "butil=\r\n" +
            "\r\n" +
            "   utils=butil   \r\n" +
            "cookie=butil,,\r\n" +
            "window=butil,utils\r\n"));

        checks.That(manifest.Order.SequenceEqual(["butil", "utils", "cookie", "window"], StringComparer.Ordinal),
            "the manifest is read in the order it lists modules, past comments and blank lines",
            $"read [{string.Join(", ", manifest.Order)}]");
        checks.That(manifest.Dependencies["butil"].Length == 0,
            "a module with no dependencies reads as having none",
            $"read [{string.Join(", ", manifest.Dependencies["butil"])}]");
        checks.That(manifest.Dependencies["window"].SequenceEqual(["butil", "utils"], StringComparer.Ordinal),
            "a module's dependencies are read from its line",
            $"read [{string.Join(", ", manifest.Dependencies["window"])}]");
        checks.That(manifest.Dependencies["cookie"].SequenceEqual(["butil"], StringComparer.Ordinal),
            "a trailing separator does not read as an empty dependency",
            $"read [{string.Join(", ", manifest.Dependencies["cookie"])}]");

        checks.Throws<InvalidDataException>(
            () => ButilScriptBundler.ReadManifest(WriteText(workspace, "manifest-duplicate.txt", "butil=\nutils=butil\nutils=butil\n")),
            "a manifest listing a module twice is rejected");
        checks.Throws<InvalidDataException>(
            () => ButilScriptBundler.ReadManifest(WriteText(workspace, "manifest-ghost-dependency.txt", "butil=\nwindow=butil,ghost\n")),
            "a manifest whose dependency is not a module is rejected");
        checks.Throws<InvalidDataException>(
            () => ButilScriptBundler.ReadManifest(WriteText(workspace, "manifest-no-separator.txt", "butil=\nwindow\n")),
            "a manifest line without a separator is rejected");
        checks.Throws<InvalidDataException>(
            () => ButilScriptBundler.ReadManifest(WriteText(workspace, "manifest-no-name.txt", "=butil\n")),
            "a manifest line with no module name is rejected");
        checks.Throws<IOException>(
            () => ButilScriptBundler.ReadManifest(Path.Combine(workspace, "manifest-that-is-not-there.txt")),
            "a manifest that is not there is reported as an IO failure the publish can explain");
    }

    /// <summary>
    /// <see cref="ButilScriptBundler.Resolve"/>: closing the directly called modules over their dependencies,
    /// which is where a bundle's contents are actually decided.
    /// </summary>
    /// <remarks>
    /// Checked against a hand-written manifest rather than Bit.Butil's own, because the shapes that matter -
    /// two roots sharing a dependency, a root nothing else references, an identifier naming no module at all -
    /// are not all present in it at any given moment, and the ones that are would stop being the day someone
    /// edits a TypeScript file.
    /// </remarks>
    private static void CheckDependencyResolution(Checks checks)
    {
        // butil -> utils/events -> element/window, plus a clipboard that depends on nothing else: enough to
        // tell "pulled in as a dependency" apart from "pulled in as a root".
        var manifest = new ButilScriptManifest(
            ["butil", "utils", "events", "element", "window", "clipboard"],
            new Dictionary<string, string[]>(StringComparer.Ordinal)
            {
                ["butil"] = [],
                ["utils"] = ["butil"],
                ["events"] = ["butil"],
                ["element"] = ["butil", "utils", "events"],
                ["window"] = ["butil", "utils", "events"],
                ["clipboard"] = ["butil"],
            });

        var included = ButilScriptBundler.Resolve(manifest, ["window"], out var unknown);
        checks.That(included.SequenceEqual(["butil", "utils", "events", "window"], StringComparer.Ordinal) && unknown.Count == 0,
            "a module's dependencies come with it, in the manifest's order",
            $"resolved to [{string.Join(", ", included)}]");

        included = ButilScriptBundler.Resolve(manifest, ["clipboard"], out _);
        checks.That(included.SequenceEqual(["butil", "clipboard"], StringComparer.Ordinal),
            "the modules nothing calls are left out - the whole point of the exercise",
            $"resolved to [{string.Join(", ", included)}]");

        included = ButilScriptBundler.Resolve(manifest, ["window", "element"], out _);
        checks.That(included.SequenceEqual(["butil", "utils", "events", "element", "window"], StringComparer.Ordinal),
            "a dependency two modules share is included once",
            $"resolved to [{string.Join(", ", included)}]");

        included = ButilScriptBundler.Resolve(manifest, ["window", "clipboard"], out _);
        checks.That(included.SequenceEqual(["butil", "utils", "events", "window", "clipboard"], StringComparer.Ordinal),
            "the result follows the manifest's dependency-first order, not the order the modules were called in",
            $"resolved to [{string.Join(", ", included)}]");

        included = ButilScriptBundler.Resolve(manifest, ["window", "window", "butil"], out _);
        checks.That(included.SequenceEqual(["butil", "utils", "events", "window"], StringComparer.Ordinal),
            "a module named twice, or named as well as being a dependency, is included once",
            $"resolved to [{string.Join(", ", included)}]");

        included = ButilScriptBundler.Resolve(manifest, manifest.Order, out _);
        checks.That(included.SequenceEqual(manifest.Order, StringComparer.Ordinal),
            "calling every module resolves to the manifest as it stands - the full bundle",
            $"resolved to [{string.Join(", ", included)}]");

        included = ButilScriptBundler.Resolve(manifest, [], out unknown);
        checks.That(included.Count == 0 && unknown.Count == 0,
            "an assembly that calls no module at all resolves to an empty bundle",
            $"resolved to [{string.Join(", ", included)}]");

        included = ButilScriptBundler.Resolve(manifest, ["ghost", "clipboard", "ghost", "alsoGhost"], out unknown);
        checks.That(included.SequenceEqual(["butil", "clipboard"], StringComparer.Ordinal)
                && unknown.SequenceEqual(["alsoGhost", "ghost"], StringComparer.Ordinal),
            "a called module the manifest does not have is reported once and skipped, and the rest still resolve",
            $"resolved to [{string.Join(", ", included)}], unknown [{string.Join(", ", unknown)}]");

        // build.mjs refuses to emit a manifest with a cycle in it, so this can only come from one that was
        // tampered with - but resolution walks the manifest before anything validates it, and a publish that
        // hangs is a far worse way to find that out than a bundle with a module too many.
        var circular = new ButilScriptManifest(
            ["x", "y"],
            new Dictionary<string, string[]>(StringComparer.Ordinal) { ["x"] = ["y"], ["y"] = ["x"] });
        included = ButilScriptBundler.Resolve(circular, ["x"], out _);
        checks.That(included.SequenceEqual(["x", "y"], StringComparer.Ordinal),
            "a manifest with a dependency cycle resolves rather than looping forever",
            $"resolved to [{string.Join(", ", included)}]");
    }

    /// <summary>
    /// <see cref="ButilScriptBundler.WriteBundle"/>: the concatenation, and what it leaves behind when a
    /// chunk it was promised is not there.
    /// </summary>
    /// <remarks>
    /// The bundle is written into a consumer's intermediate folder, which an incremental publish reads back
    /// on the next run - so a half-written file is not a transient failure but a wrong bundle that keeps
    /// being served until someone cleans the project. That is what the write-then-move is for, and what the
    /// missing-chunk checks here are about.
    /// </remarks>
    private static void CheckBundleAssembly(Checks checks, string workspace)
    {
        var chunks = Path.Combine(workspace, "chunks");
        Directory.CreateDirectory(chunks);
        WriteText(chunks, "a.js", "/*a*/\n");
        WriteText(chunks, "b.js", "/*b*/\n");
        WriteText(chunks, "unicode.js", "/*café*/\n");

        var bundle = Path.Combine(workspace, "bundles", "bundle.js");
        var bundleDirectory = Path.GetDirectoryName(bundle)!;

        ButilScriptBundler.WriteBundle(chunks, ["b", "a"], bundle);
        checks.That(File.ReadAllText(bundle) == "/*b*/\n/*a*/\n",
            "a bundle is the chunks concatenated in the order they were given, and nothing else",
            $"wrote {Show(File.ReadAllText(bundle))}");
        checks.That(Directory.Exists(bundleDirectory),
            "a bundle can be written into a folder that does not exist yet");

        ButilScriptBundler.WriteBundle(chunks, ["a"], bundle);
        checks.That(File.ReadAllText(bundle) == "/*a*/\n",
            "writing a bundle replaces the previous one rather than appending to it",
            $"wrote {Show(File.ReadAllText(bundle))}");

        var unicodeBundle = Path.Combine(bundleDirectory, "unicode.js");
        ButilScriptBundler.WriteBundle(chunks, ["unicode"], unicodeBundle);
        var bytes = File.ReadAllBytes(unicodeBundle);
        checks.That(bytes.Length < 3 || bytes[0] != 0xEF || bytes[1] != 0xBB || bytes[2] != 0xBF,
            "a bundle is written without a byte-order mark, which a <script> tag would serve as content",
            "it starts with a UTF-8 BOM");
        checks.That(File.ReadAllText(unicodeBundle) == "/*café*/\n",
            "a chunk holding non-ASCII text survives the round trip");

        var empty = Path.Combine(bundleDirectory, "empty.js");
        ButilScriptBundler.WriteBundle(chunks, [], empty);
        checks.That(File.Exists(empty) && new FileInfo(empty).Length == 0,
            "an empty module set writes an empty bundle rather than nothing at all",
            File.Exists(empty) ? $"it is {new FileInfo(empty).Length} bytes" : "no file was written");

        // The interesting half: the chunk that is missing is the second one, so a bundler that streamed
        // straight to the destination would already have written the first.
        checks.Throws<FileNotFoundException>(
            () => ButilScriptBundler.WriteBundle(chunks, ["a", "ghost"], bundle),
            "a bundle naming a chunk that does not exist is refused");
        checks.That(File.ReadAllText(bundle) == "/*a*/\n",
            "a refused bundle leaves the previous one intact instead of a half-written file an incremental publish would reuse",
            $"the file now holds {Show(File.ReadAllText(bundle))}");
        checks.That(Directory.GetFiles(bundleDirectory, "*.tmp").Length == 0,
            "a refused bundle leaves no temporary file behind",
            $"left [{string.Join(", ", Directory.GetFiles(bundleDirectory, "*.tmp").Select(Path.GetFileName))}]");
    }

    /// <summary>
    /// <see cref="UserStringHeap"/> and <see cref="ButilScriptBundler.ReadReferencedModules"/>: reading the
    /// string literals out of an assembly, which is the signal this whole feature is built on.
    /// </summary>
    /// <remarks>
    /// The reader is hand-rolled (see <see cref="UserStringHeap"/> for why it cannot take a dependency on
    /// System.Reflection.Metadata) and runs inside MSBuild against whatever file the consumer's build points
    /// it at. So the file not being a managed assembly is a case that happens - and it has to come out as
    /// the <see cref="BadImageFormatException"/> the task catches and reports, not as an index that ran off
    /// the end of an array.
    /// </remarks>
    private static void CheckAssemblyStrings(Checks checks, string workspace, string? butilAssemblyPath)
    {
        var self = typeof(ScriptBundling).Assembly.Location;
        if (string.IsNullOrEmpty(self) || string.IsNullOrEmpty(butilAssemblyPath))
        {
            checks.That(false, "the assembly string reader was not checked", "there is no assembly file to read (a single-file build)");
            return;
        }

        var literals = UserStringHeap.Read(butilAssemblyPath).ToArray();
        var derived = new SortedSet<string>(StringComparer.Ordinal);
        foreach (var literal in literals)
        {
            if (ButilScriptBundler.TryGetModule(literal, out var module)) derived.Add(module);
        }

        var referenced = ButilScriptBundler.ReadReferencedModules(butilAssemblyPath);
        checks.That(literals.Length > 0 && derived.Count > 0,
            "Bit.Butil's own interop identifiers are read back out of its assembly",
            $"read {literals.Length} literals holding {derived.Count} module names");
        checks.That(derived.SequenceEqual(referenced, StringComparer.Ordinal),
            "the modules a bundle is built from are exactly the ones the assembly's literals name",
            $"[{string.Join(", ", derived)}] against [{string.Join(", ", referenced)}]");

        // This assembly is not Bit.Butil and calls no interop at all; the identifier is simply a literal in
        // it - passed here whole rather than interpolated into a message, which would fold it into the
        // surrounding text and leave no literal of its own in the heap to find. The reader has to answer for
        // any assembly it is handed, which is what makes the "calls a module that does not exist" path
        // reachable at all.
        ButilScriptBundler.TryGetModule(UnknownModuleIdentifier, out var sentinel);
        checks.That(sentinel == UnknownModule && ButilScriptBundler.ReadReferencedModules(self).Contains(UnknownModule),
            "an interop identifier is found in any assembly's literals, not only in Bit.Butil's",
            $"the identifier for '{UnknownModule}' is a literal in this assembly but was not read back out of {Path.GetFileName(self)}");

        var text = WriteText(workspace, "not-an-assembly.dll", "A text file that a consumer's build pointed the task at.");
        checks.Throws<BadImageFormatException>(
            () => UserStringHeap.Read(text).ToArray(),
            "a file that is not a PE image is reported as a bad image rather than crashing the build");

        var truncated = Path.Combine(workspace, "truncated.dll");
        File.WriteAllBytes(truncated, [.. File.ReadAllBytes(butilAssemblyPath).Take(512)]);
        checks.Throws<BadImageFormatException>(
            () => UserStringHeap.Read(truncated).ToArray(),
            "an assembly cut short is reported as a bad image rather than read past its end");

        checks.Throws<IOException>(
            () => UserStringHeap.Read(Path.Combine(workspace, "assembly-that-is-not-there.dll")).ToArray(),
            "an assembly that is not there is reported as an IO failure the publish can explain");
    }

    /// <summary>
    /// The claims Bit.Butil's own build outputs have to keep for any of this to work: the chunks are the
    /// shipped bundle, each lazy module file is its own dependency closure, and every chunk carries the
    /// guard that makes it safe to load twice.
    /// </summary>
    /// <remarks>
    /// Nothing enforces them. <c>build.mjs</c> writes the bundle, the lazy files and the chunks in one pass
    /// today, so they agree by construction - and would stop agreeing the moment the bundle gains a header,
    /// the lazy files gain an export, or the chunks are minified differently from the bundle. A consumer
    /// would find out as a bundle that behaves unlike the one every other app is running.
    /// </remarks>
    private static void CheckShippedArtifacts(Checks checks, string butilRoot, string workspace, string? butilAssemblyPath)
    {
        var chunks = Path.Combine(butilRoot, "obj", "butil-js", "chunks");
        var manifestPath = Path.Combine(chunks, "manifest.txt");
        var modulesDirectory = Path.Combine(butilRoot, "wwwroot", "modules");
        var fullBundlePath = Path.Combine(butilRoot, "wwwroot", "bit-butil.js");

        if (File.Exists(manifestPath) is false || File.Exists(fullBundlePath) is false)
        {
            checks.That(false, "the shipped JavaScript was not checked", $"Bit.Butil's build outputs are missing ({manifestPath}); build Bit.Butil first");
            return;
        }

        var manifest = ButilScriptBundler.ReadManifest(manifestPath);
        var everything = ButilScriptBundler.Resolve(manifest, manifest.Order, out _);
        var assembled = Path.Combine(workspace, "full-bundle.js");
        ButilScriptBundler.WriteBundle(chunks, everything, assembled);

        checks.That(File.ReadAllBytes(assembled).SequenceEqual(File.ReadAllBytes(fullBundlePath)),
            "a bundle holding every module is byte-for-byte the bit-butil.js the package ships",
            $"{new FileInfo(assembled).Length:N0} bytes against {new FileInfo(fullBundlePath).Length:N0}");

        // The same computation with one root at a time: what the publish-time bundler produces for an app
        // that calls only this module has to be what a lazy-scripts app imports for it.
        var divergent = new List<string>();
        foreach (var module in manifest.Order)
        {
            var lazyPath = Path.Combine(modulesDirectory, module + ".js");
            if (File.Exists(lazyPath) is false) continue;   // A missing lazy file is ScriptTrimming's to report.

            var closure = Path.Combine(workspace, "closure.js");
            ButilScriptBundler.WriteBundle(chunks, ButilScriptBundler.Resolve(manifest, [module], out _), closure);
            if (File.ReadAllBytes(closure).SequenceEqual(File.ReadAllBytes(lazyPath)) is false) divergent.Add(module);
        }

        checks.That(divergent.Count == 0,
            "every lazy module file is exactly the bundle its own dependency closure assembles to",
            $"they differ for [{string.Join(", ", divergent)}] - the two shapes of the JavaScript have drifted apart");

        // The guard is what makes a chunk safe to evaluate twice, which is what lets the lazy files overlap
        // and what keeps a module's private state across a second load. Matched loosely on purpose: a Release
        // build minifies the chunks, which rewrites the condition but not what it tests.
        var bundleText = File.ReadAllText(fullBundlePath);
        var unguarded = new List<string>();
        var duplicated = new List<string>();
        foreach (var module in manifest.Order)
        {
            var chunk = File.ReadAllText(Path.Combine(chunks, module + ".js"));
            var key = module == "butil" ? "version" : module;   // build.mjs registers the prelude as BitButil.version.
            var trimmed = chunk.TrimEnd();

            if (trimmed.StartsWith("(function(){if(", StringComparison.Ordinal) is false
                || trimmed.EndsWith("})();", StringComparison.Ordinal) is false
                || chunk.Contains("window.BitButil." + key, StringComparison.Ordinal) is false)
            {
                unguarded.Add(module);
            }

            // Forward only: the first occurrence, then a second search starting just past it. Same answer as
            // comparing against LastIndexOf, without scanning the whole bundle backwards for every chunk.
            var first = bundleText.IndexOf(chunk, StringComparison.Ordinal);
            if (first < 0 || bundleText.IndexOf(chunk, first + 1, StringComparison.Ordinal) >= 0) duplicated.Add(module);
        }

        checks.That(unguarded.Count == 0,
            "every chunk is wrapped in the guard that makes evaluating it a second time a no-op",
            $"[{string.Join(", ", unguarded)}] are not");
        checks.That(duplicated.Count == 0,
            "every chunk appears in the shipped bundle exactly once",
            $"[{string.Join(", ", duplicated)}] appear twice, or not at all");

        // Dependency-first order is what makes concatenation a valid load order.
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var outOfOrder = new List<string>();
        foreach (var module in manifest.Order)
        {
            if (manifest.Dependencies[module].All(seen.Contains) is false) outOfOrder.Add(module);
            seen.Add(module);
        }

        checks.That(outOfOrder.Count == 0,
            "the manifest lists every module after the modules it depends on",
            $"[{string.Join(", ", outOfOrder)}] come before something they depend on");

        // And the feature itself, against the real manifest: an app calling one API ships that API's
        // JavaScript and its dependencies, and none of the rest.
        var single = ButilScriptBundler.Resolve(manifest, ["clipboard"], out _);
        checks.That(single.Contains("clipboard") && single.Contains("butil") && single.Contains("window") is false && single.Count < manifest.Order.Count,
            "an app that calls one module ships that module and its dependencies, not the library",
            $"resolved to [{string.Join(", ", single)}] of {manifest.Order.Count} modules");

        if (string.IsNullOrEmpty(butilAssemblyPath) is false)
        {
            var referenced = ButilScriptBundler.ReadReferencedModules(butilAssemblyPath).Append(UnknownModule);
            ButilScriptBundler.Resolve(manifest, referenced, out var unknown);
            checks.That(unknown.SequenceEqual([UnknownModule], StringComparer.Ordinal),
                "an identifier naming a module the package does not ship is reported (the BUTIL001 warning) rather than dropped",
                $"reported [{string.Join(", ", unknown)}]");
        }
    }

    /// <summary>
    /// Runs the assembled JavaScript: the bundle a publish of this harness would ship, the full one, and two
    /// overlapping lazy module files loaded one after the other.
    /// </summary>
    /// <remarks>
    /// Everything above compares bytes, and bytes cannot say whether the result is a script that loads. A
    /// bundle assembled in the wrong order, closed over the wrong set, or cut short by a chunk that lost its
    /// trailing newline is a file whose only symptom is a browser console - so the checks that matter most
    /// are the ones that evaluate it and look at what it registered. See <c>verify-bundle.mjs</c>, which
    /// runs under Node, already a build dependency of Bit.Butil (it compiles the TypeScript).
    /// </remarks>
    private static void CheckAssembledBundlesRun(Checks checks, string butilRoot, string workspace, string? butilAssemblyPath)
    {
        var verifier = Path.Combine(AppContext.BaseDirectory, VerifierFileName);
        if (File.Exists(verifier) is false)
        {
            checks.That(false, "the assembled bundles were not run", $"{VerifierFileName} is missing from {AppContext.BaseDirectory}");
            return;
        }

        var chunks = Path.Combine(butilRoot, "obj", "butil-js", "chunks");
        var manifestPath = Path.Combine(chunks, "manifest.txt");
        var modulesDirectory = Path.Combine(butilRoot, "wwwroot", "modules");
        var fullBundlePath = Path.Combine(butilRoot, "wwwroot", "bit-butil.js");
        if (File.Exists(manifestPath) is false) return;   // Already reported by CheckShippedArtifacts.

        var manifest = ButilScriptBundler.ReadManifest(manifestPath);

        // The bundle a consumer publishing this very assembly would serve. Untrimmed that is the whole
        // library; trimmed it is the handful of modules ConsumerComponent's APIs still call - the artifact
        // this entire feature exists to produce, and the one nothing else ever evaluates.
        if (string.IsNullOrEmpty(butilAssemblyPath) is false)
        {
            var included = ButilScriptBundler.Resolve(manifest, ButilScriptBundler.ReadReferencedModules(butilAssemblyPath), out _);
            var bundle = Path.Combine(workspace, "publish-bundle.js");
            ButilScriptBundler.WriteBundle(chunks, included, bundle);
            RunVerifier(checks, verifier, Keys(included), bundle);
        }

        RunVerifier(checks, verifier, Keys(manifest.Order), fullBundlePath);

        // Two lazy module files that overlap - element and window both carry butil, utils and events - loaded
        // one after the other, the way a lazy-scripts app loads them as the user reaches each API.
        string[] pair = ["element", "window"];
        if (pair.All(module => manifest.Dependencies.ContainsKey(module) && File.Exists(Path.Combine(modulesDirectory, module + ".js"))))
        {
            RunVerifier(checks, verifier, Keys(ButilScriptBundler.Resolve(manifest, pair, out _)),
                [.. pair.Select(module => Path.Combine(modulesDirectory, module + ".js"))]);
        }
    }

    /// <summary>The <c>BitButil.&lt;key&gt;</c> namespaces a set of modules registers between them.</summary>
    internal static string Keys(IEnumerable<string> modules)
        => string.Join(",", modules.Select(module => module == "butil" ? "version" : module));

    internal static void RunVerifier(Checks checks, string verifier, string expectedKeys, params string[] scripts)
    {
        var what = string.Join(" + ", scripts.Select(Path.GetFileName));
        var process = new Process
        {
            StartInfo = new ProcessStartInfo("node")
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
            }
        };

        process.StartInfo.ArgumentList.Add(verifier);
        process.StartInfo.ArgumentList.Add(expectedKeys);
        foreach (var script in scripts) process.StartInfo.ArgumentList.Add(script);

        try
        {
            process.Start();
        }
        catch (Exception exception)
        {
            // Node builds Bit.Butil's JavaScript in the first place, so a checkout that can produce the
            // artifacts under test can always run them. Anything else is a machine that cannot run this part
            // of the harness, and saying so is better than reporting a pass it did not earn.
            checks.That(false, $"{what} was not run", $"node could not be started ({exception.Message}) - it is what evaluates the assembled JavaScript");
            return;
        }

        using (process)
        {
            // Drain both pipes at once, before waiting: a bundle's worth of report can fill either one, and
            // a full pipe blocks the process this is waiting for - which draining them one after the other
            // would do too, whenever the one still unread is the one that filled up.
            var outputTask = process.StandardOutput.ReadToEndAsync();
            var errorTask = process.StandardError.ReadToEndAsync();

            // One 60-second deadline shared by both waits, so the failure message below is the truth: two
            // independent 60-second timeouts would let this sit here for two minutes and still say sixty.
            var elapsed = Stopwatch.StartNew();
            int RemainingMilliseconds() => (int)Math.Max(0, 60_000 - elapsed.ElapsedMilliseconds);

            if (Task.WaitAll([outputTask, errorTask], RemainingMilliseconds()) is false || process.WaitForExit(RemainingMilliseconds()) is false)
            {
                process.Kill(entireProcessTree: true);
                checks.That(false, $"{what} was not run", "node did not finish within 60 seconds");
                return;
            }

            var output = outputTask.Result;
            var error = errorTask.Result;

            var reported = 0;
            foreach (var line in output.Split('\n').Select(line => line.Trim()))
            {
                if (line.StartsWith("PASS ", StringComparison.Ordinal)) { checks.That(true, line[5..]); reported++; }
                else if (line.StartsWith("FAIL ", StringComparison.Ordinal)) { checks.That(false, line[5..]); reported++; }
            }

            // A verifier that reported nothing checked nothing, whatever its exit code says.
            if (reported == 0)
            {
                checks.That(false, $"{what} was not run", $"the verifier reported nothing (exit code {process.ExitCode}): {error.Trim()}");
            }
        }
    }

    /// <summary>
    /// That the pieces the publish-time bundler needs are packed into the folders the consumer-side targets
    /// look for them in.
    /// </summary>
    /// <remarks>
    /// The chunks, the manifest and the task assembly reach a consumer only through the NuGet package: the
    /// paths they are packed to live in Bit.Butil.csproj and the paths they are read from live in
    /// buildTransitive/Bit.Butil.targets, with nothing connecting the two. A consumer finds out that they
    /// disagree when their publish cannot find a task or a manifest, and this repository never would - every
    /// project in it references Bit.Butil as a project and overrides both paths.
    /// </remarks>
    private static void CheckPackageLayout(Checks checks, string butilRoot)
    {
        var projectPath = Path.Combine(butilRoot, "Bit.Butil.csproj");
        var targetsPath = Path.Combine(butilRoot, "buildTransitive", "Bit.Butil.targets");
        if (File.Exists(projectPath) is false || File.Exists(targetsPath) is false)
        {
            checks.That(false, "the package layout was not checked", $"{projectPath} or {targetsPath} is missing");
            return;
        }

        var targets = File.ReadAllText(targetsPath);

        CheckScriptAssetSelection(checks, targets);

        var packed = XDocument.Load(projectPath).Descendants()
            .Where(element => element.Name.LocalName is "TfmSpecificPackageFile" or "None")
            .Select(element => (Include: element.Attribute("Include")?.Value ?? string.Empty, PackagePath: element.Attribute("PackagePath")?.Value))
            .Where(entry => entry.PackagePath is not null)
            .ToArray();

        var chunksDirectory = Normalize(TargetsDefault(targets, "BitButilChunksDirectory"));
        var tasksAssembly = Normalize(TargetsDefault(targets, "BitButilBuildTasksAssembly"));

        checks.That(packed.Any(entry => entry.Include.Contains("butil-js", StringComparison.OrdinalIgnoreCase)
                && entry.Include.Contains("manifest.txt", StringComparison.OrdinalIgnoreCase)
                && PackageFolders(entry.PackagePath).Contains(chunksDirectory)),
            "the chunks and their manifest are packed into the folder the consumer-side targets read them from",
            $"the targets read '{chunksDirectory}' and the package ships them to [{string.Join(", ", packed.SelectMany(entry => PackageFolders(entry.PackagePath)))}]");

        // The task is packed through the item its own project's GetTargetPath hands back, so the pack entry
        // names no file - only the folder it lands in can be compared with the path the targets load from.
        checks.That(packed.Any(entry => entry.Include.Contains("ButilBuildTaskAssembly", StringComparison.OrdinalIgnoreCase)
                && PackageFolders(entry.PackagePath).Any(folder => tasksAssembly == folder + "/" + Path.GetFileName(tasksAssembly))),
            "the MSBuild task is packed into the folder the consumer-side targets load it from",
            $"the targets load '{tasksAssembly}'");

        var packedTargets = packed.Where(entry => entry.Include.Contains(".targets", StringComparison.OrdinalIgnoreCase)).ToArray();
        checks.That(packedTargets.Any(entry => entry.Include.Contains("Bit.Butil.targets", StringComparison.OrdinalIgnoreCase)
                && entry.Include.Contains("Bit.Butil.Endpoints.targets", StringComparison.OrdinalIgnoreCase)),
            "both consumer-side targets files are packed - the .NET 9+ half is imported by the other, and a package without it fails every consumer's build",
            $"packed [{string.Join(", ", packedTargets.Select(entry => entry.Include))}]");

        checks.That(packedTargets.Any(entry => PackageFolders(entry.PackagePath).Contains("buildtransitive")
                && PackageFolders(entry.PackagePath).Contains("build")),
            "the targets are packed under both buildTransitive/ (what NuGet imports today) and build/ (older tooling)",
            $"packed to [{string.Join(", ", packedTargets.Select(entry => entry.PackagePath))}]");
    }

    /// <summary>
    /// The one piece of the consumer-side targets that exists twice: dropping the shape of the JavaScript an
    /// app does not use, once for the build asset list and once for the publish one.
    /// </summary>
    /// <remarks>
    /// The duplication is forced - a target runs at most once per project build, so a single target hooked
    /// into both stages runs at the first and is skipped at the second - and it is the kind that rots: a fix
    /// made to one body and not the other leaves a publish shipping 72 module files an app never requests,
    /// or a bundle a lazy-scripts app never loads, and neither shows up in a build. So the two bodies are
    /// compared here, and each is checked to be hooked into the stage it exists for.
    /// </remarks>
    private static void CheckScriptAssetSelection(Checks checks, string targets)
    {
        const string build = "BitButilSelectScriptAssets";
        const string publish = "BitButilSelectPublishScriptAssets";

        var buildBody = TargetBody(targets, build);
        var publishBody = TargetBody(targets, publish);

        if (checks.That(buildBody.Length > 0 && publishBody.Length > 0,
                "the JavaScript shape an app does not use is dropped at both the build and the publish stage",
                $"{(buildBody.Length == 0 ? build : publish)} is not a target in the consumer-side targets file") is false)
        {
            return;
        }

        checks.That(string.Equals(buildBody, publishBody, StringComparison.Ordinal),
            "the build-stage and publish-stage selections still do the same thing",
            $"{build} and {publish} have drifted apart - the publish output no longer matches the build's idea of which scripts the app uses");

        // Hooked into the right stage, and in the publish list ahead of the trimming, which only narrows what
        // the selection leaves behind.
        var publishHook = HookList(targets, "ResolvePublishStaticWebAssetsDependsOn");
        var buildHook = HookList(targets, "ResolveCoreStaticWebAssetsDependsOn");

        checks.That(buildHook.Contains(build, StringComparison.Ordinal), $"{build} runs at the build stage", $"it is not in ResolveCoreStaticWebAssetsDependsOn: '{buildHook}'");
        checks.That(publishHook.Contains(publish, StringComparison.Ordinal), $"{publish} runs at the publish stage", $"it is not in ResolvePublishStaticWebAssetsDependsOn: '{publishHook}'");
        checks.That(publishHook.Contains(build, StringComparison.Ordinal) is false,
            $"{build} is not also hooked into the publish stage",
            "a target runs once per build, so hooking the same one into both stages leaves the second with nothing to do");

        var selection = publishHook.IndexOf(publish, StringComparison.Ordinal);
        var trimming = publishHook.IndexOf("BitButilTrimScript", StringComparison.Ordinal);
        checks.That(selection >= 0 && trimming > selection,
            "the publish-stage selection runs before the trimming it feeds",
            $"the publish hook list orders them '{publishHook}'");
    }

    /// <summary>A target's body, comments and layout removed, so two of them can be compared for what they do.</summary>
    private static string TargetBody(string targets, string name)
    {
        var match = Regex.Match(targets, $"<Target Name=\"{Regex.Escape(name)}\"[^>]*>(?<body>.*?)</Target>", RegexOptions.Singleline);
        if (match.Success is false) return string.Empty;

        var body = Regex.Replace(match.Groups["body"].Value, "<!--.*?-->", " ", RegexOptions.Singleline);
        return Regex.Replace(body, @"\s+", " ").Trim();
    }

    /// <summary>The value this targets file appends to one of the SDK's DependsOn properties.</summary>
    private static string HookList(string targets, string property)
        => Regex.Match(targets, $"<{property}>(?<value>[^<]*)</{property}>").Groups["value"].Value;

    /// <summary>The folders one pack item's PackagePath names - it may name several, separated by semicolons.</summary>
    private static string[] PackageFolders(string? packagePath)
        => [.. (packagePath ?? string.Empty).Split(';', StringSplitOptions.RemoveEmptyEntries).Select(Normalize)];

    /// <summary>The default a targets file gives a property, as written in its conditioned assignment.</summary>
    private static string TargetsDefault(string targets, string property)
        => Regex.Match(targets, $"<{property}[^>]*>([^<]*)</{property}>").Groups[1].Value;

    /// <summary>
    /// A package path as the two files spell it, made comparable: MSBuild's separators and casing differ
    /// between a pack item and a property, and neither difference is a defect.
    /// </summary>
    private static string Normalize(string? path)
        => (path ?? string.Empty)
            .Replace("$(MSBuildThisFileDirectory)", string.Empty, StringComparison.Ordinal)
            .Replace("..\\", string.Empty, StringComparison.Ordinal)
            .Replace("../", string.Empty, StringComparison.Ordinal)
            .Replace('\\', '/')
            .Trim()
            .ToLowerInvariant()
            .TrimEnd('/');

    private static string WriteText(string directory, string name, string content)
    {
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, name);
        File.WriteAllText(path, content);
        return path;
    }

    /// <summary>Bundle content in a failure message: short, and with its line breaks visible.</summary>
    private static string Show(string content)
    {
        var single = content.Replace("\r", "\\r", StringComparison.Ordinal).Replace("\n", "\\n", StringComparison.Ordinal);
        return $"'{(single.Length > 120 ? single[..120] + "..." : single)}'";
    }

    /// <summary>
    /// Counts what held and records what did not, in the harness's own terms - every failure ends up in the
    /// list the report prints and the exit code is read from.
    /// </summary>
    internal sealed class Checks(List<string> failures, string subject = "script bundling")
    {
        public int Passed { get; private set; }

        public int Failed { get; private set; }

        public bool That(bool passed, string what, string? detail = null)
        {
            if (passed)
            {
                Passed++;
                return true;
            }

            Failed++;
            failures.Add($"{subject}: {what}{(detail is null ? string.Empty : $" - {detail}")}.");
            return false;
        }

        /// <summary>
        /// An input the bundler has to refuse. Refusing it some other way counts as a failure too: the
        /// MSBuild task catches a named set of exceptions and turns those into a build error naming the
        /// file, and anything outside that set comes out as an unhandled exception mid-publish.
        /// </summary>
        public void Throws<TException>(Action action, string what) where TException : Exception
        {
            try
            {
                action();
                That(false, what, "it was accepted instead");
            }
            catch (TException)
            {
                That(true, what);
            }
            catch (Exception exception)
            {
                That(false, what, $"it threw {exception.GetType().Name} instead: {exception.Message}");
            }
        }
    }
}
