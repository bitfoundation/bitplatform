using System.IO.Compression;
using System.Text.RegularExpressions;
using Bit.Butil.Build;

namespace ButilTests.Manual;

/// <summary>
/// The JavaScript half of the trimming report: which Bit.Butil script modules the (trimmed or untrimmed)
/// assembly still calls, whether the module set the publish-time bundler would produce is the expected one,
/// and how much smaller the bundle gets.
/// </summary>
/// <remarks>
/// This runs the very same computation the consumer-side MSBuild task runs at publish
/// (<see cref="ButilScriptBundler"/> from Bit.Butil.Build), against the very same trimmed assembly this
/// harness already measures for the C# side - so the numbers here are what a real trimmed consumer ships.
/// <br/>
/// The inputs it needs besides the assembly are Bit.Butil's own JavaScript build outputs, found relative to
/// the source tree (this is a repo-only harness): the chunks and dependency manifest under
/// <c>Bit.Butil/obj/butil-js/</c> and the lazy-loadable modules under <c>Bit.Butil/wwwroot/modules/</c>.
/// </remarks>
internal static class ScriptTrimming
{
    /// <summary>
    /// The modules a trimmed publish of this harness must end up calling - the JavaScript behind the services
    /// <see cref="ConsumerComponent"/> uses (plus <c>events</c>, reached through <c>Window.SubscribeEvent</c>'s
    /// internal <c>DomEventsInterop</c>). Dependencies (<c>butil</c>, <c>utils</c>) are added by the manifest,
    /// not listed here, so this stays a statement about what the C# side calls.
    /// </summary>
    internal static readonly string[] MustSurviveModules = ["canvas", "clipboard", "cookie", "dom", "events", "geolocation", "storage", "streams", "webRtc", "window"];

    /// <summary>
    /// The same question asked of the <em>untrimmed</em> signals - the class-to-module map and the scan
    /// built on it - which reach two modules more.
    /// </summary>
    /// <remarks>
    /// The map follows type references, while ILLink follows call sites, so a type a class merely mentions
    /// pulls its module in here and not there: <c>Streams</c> hands out a <c>FetchRequest</c> carrying an
    /// abort signal, and <c>WebRtc</c> names the media-stream types, without this project calling either.
    /// That direction is the safe one - an untrimmed publish shipping a module the app never calls costs
    /// bytes, while missing one breaks it in the browser - so the two lists are compared exactly and kept
    /// separately, rather than the check being loosened to a subset test that would stop noticing either.
    /// </remarks>
    internal static readonly string[] ScanReachableModules = [.. MustSurviveModules, "abortController", "mediaDevices"];

    /// <summary>
    /// Modules no C# code calls directly and that are legitimately only ever pulled in as a dependency of
    /// another module. Anything else in the manifest that nothing calls is an orphan: JavaScript shipped
    /// for an API that no longer exists on the C# side.
    /// </summary>
    private static readonly string[] DependencyOnlyModules = ["butil", "utils"];

    public sealed record Report(
        string[] Referenced,
        string[] Included,
        int TotalModules,
        long FullBundleBytes,
        long TrimmedBundleBytes,
        long TrimmedBundleGzipBytes,
        long TrimmedBundleBrotliBytes,
        long LazyModulesBytes);

    public static Report? Run(string assemblyPath, bool trimmed, List<string> failures)
    {
        var butilRoot = LocateButilProject();
        if (butilRoot is null)
        {
            failures.Add("the Bit.Butil project folder could not be located from the executable's location, so the JavaScript side was not checked at all - run this harness from inside the repository.");
            return null;
        }

        var chunksDirectory = Path.Combine(butilRoot, "obj", "butil-js", "chunks");
        var manifestPath = Path.Combine(butilRoot, "obj", "butil-js", "chunks", "manifest.txt");
        var modulesDirectory = Path.Combine(butilRoot, "wwwroot", "modules");
        var fullBundlePath = Path.Combine(butilRoot, "wwwroot", "bit-butil.js");

        if (File.Exists(manifestPath) is false || File.Exists(fullBundlePath) is false)
        {
            failures.Add($"Bit.Butil's JavaScript build outputs are missing ({manifestPath}); build Bit.Butil first.");
            return null;
        }

        var manifest = ButilScriptBundler.ReadManifest(manifestPath);
        var referenced = ButilScriptBundler.ReadReferencedModules(assemblyPath);
        var included = ButilScriptBundler.Resolve(manifest, referenced, out var unknown);

        // A "BitButil.<x>.*" identifier the C# side still carries but no JavaScript module answers to.
        foreach (var module in unknown)
        {
            failures.Add($"the C# side invokes 'BitButil.{module}.*' but Bit.Butil ships no JavaScript module called '{module}' - the call would fail in the browser.");
        }

        // The build outputs have to agree with each other and with the sources; the publish-time bundler
        // trusts them without checking.
        foreach (var module in manifest.Order)
        {
            if (File.Exists(Path.Combine(chunksDirectory, module + ".js")) is false) failures.Add($"the manifest lists module '{module}' but there is no chunk for it under {chunksDirectory}.");
            if (File.Exists(Path.Combine(modulesDirectory, module + ".js")) is false) failures.Add($"the manifest lists module '{module}' but there is no lazy-loadable file for it under {modulesDirectory}.");
        }

        VerifyManifestAgainstSources(butilRoot, manifest, failures);

        if (trimmed)
        {
            var expected = MustSurviveModules.OrderBy(name => name, StringComparer.Ordinal).ToArray();
            var actual = referenced.OrderBy(name => name, StringComparer.Ordinal).ToArray();

            foreach (var missing in expected.Except(actual, StringComparer.Ordinal))
            {
                failures.Add($"module '{missing}' is behind an API ConsumerComponent uses but its identifiers did not survive trimming - the trimmed bundle would lack it.");
            }

            var unexpected = actual.Except(expected, StringComparer.Ordinal).ToArray();
            if (unexpected.Length > 0)
            {
                failures.Add($"JavaScript modules nothing in this project calls survived trimming: {string.Join(", ", unexpected)} - either a static reference to unrelated Butil APIs was reintroduced, or ConsumerComponent gained a call without MustSurviveModules being updated.");
            }
        }
        else
        {
            // Untrimmed, every C# call site is present, so every module the library ships must be answering to
            // one of them (or be a pure dependency): an unreferenced module is JavaScript for a C# API that
            // no longer exists.
            var orphans = manifest.Order
                .Where(module => referenced.Contains(module) is false && DependencyOnlyModules.Contains(module, StringComparer.Ordinal) is false)
                .ToArray();
            foreach (var orphan in orphans)
            {
                failures.Add($"JavaScript module '{orphan}' is not called by any 'BitButil.{orphan}.*' identifier in the C# side - dead script, or a module that should be listed as dependency-only.");
            }

            foreach (var name in MustSurviveModules.Concat(ScanReachableModules).Distinct(StringComparer.Ordinal).Where(name => manifest.Dependencies.ContainsKey(name) is false))
            {
                failures.Add($"'{name}' is an expected module name but no such module exists - the name here is stale.");
            }
        }

        // The size story: what a consumer publishing this exact assembly would ship.
        var trimmedBundlePath = Path.Combine(AppContext.BaseDirectory, "trimmed-bit-butil.js");
        ButilScriptBundler.WriteBundle(chunksDirectory, included, trimmedBundlePath);
        var trimmedBytes = File.ReadAllBytes(trimmedBundlePath);

        // A download total, not a code total: each lazy module file is self-contained, so a dependency shared
        // by several of them is counted once per file that inlines it.
        var lazyBytes = referenced
            .Select(module => Path.Combine(modulesDirectory, module + ".js"))
            .Where(File.Exists)
            .Sum(path => new FileInfo(path).Length);

        return new Report(
            [.. referenced],
            [.. included],
            manifest.Order.Count,
            new FileInfo(fullBundlePath).Length,
            trimmedBytes.Length,
            Compressed(trimmedBytes, stream => new GZipStream(stream, CompressionLevel.SmallestSize)),
            Compressed(trimmedBytes, stream => new BrotliStream(stream, CompressionLevel.SmallestSize)),
            lazyBytes);
    }

    /// <summary>
    /// Re-derives each module's dependencies from the TypeScript sources with the same rule build.mjs uses
    /// (any <c>butil.&lt;other&gt;</c> / <c>BitButil.&lt;other&gt;</c> reference) and compares with the
    /// manifest, so a stale manifest - or a change to the rule on one side only - is caught here rather
    /// than by a consumer whose trimmed bundle is missing a module.
    /// </summary>
    private static void VerifyManifestAgainstSources(string butilRoot, ButilScriptManifest manifest, List<string> failures)
    {
        var scripts = Path.Combine(butilRoot, "Scripts");
        var sources = Directory.GetFiles(scripts, "*.ts")
            .Where(path => path.EndsWith(".d.ts", StringComparison.OrdinalIgnoreCase) is false)
            .Select(path => Path.GetFileNameWithoutExtension(path))
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        var names = sources.ToHashSet(StringComparer.Ordinal);
        foreach (var name in sources.Where(name => manifest.Dependencies.ContainsKey(name) is false))
        {
            failures.Add($"Scripts/{name}.ts exists but the manifest has no module '{name}' - Bit.Butil's JavaScript build is stale.");
        }
        foreach (var name in manifest.Order.Where(name => names.Contains(name) is false))
        {
            failures.Add($"the manifest lists module '{name}' but there is no Scripts/{name}.ts - Bit.Butil's JavaScript build is stale.");
        }

        var reference = new Regex(@"\b(?:butil|BitButil)\.([A-Za-z0-9_]+)\b", RegexOptions.Compiled);
        foreach (var name in sources.Where(manifest.Dependencies.ContainsKey))
        {
            var source = File.ReadAllText(Path.Combine(scripts, name + ".ts"));
            var expected = reference.Matches(source)
                .Select(match => match.Groups[1].Value)
                .Where(target => target != name && names.Contains(target))
                .Append(name == "butil" ? null : "butil")
                .Where(target => target is not null)
                .Distinct(StringComparer.Ordinal)
                .OrderBy(target => target, StringComparer.Ordinal)
                .ToArray();
            var actual = manifest.Dependencies[name].OrderBy(target => target, StringComparer.Ordinal).ToArray();

            if (expected.SequenceEqual(actual, StringComparer.Ordinal) is false)
            {
                failures.Add($"the manifest says module '{name}' depends on [{string.Join(", ", actual)}] but its source references [{string.Join(", ", expected!)}] - Bit.Butil's JavaScript build is stale.");
            }
        }
    }

    private static long Compressed(byte[] content, Func<Stream, Stream> wrap)
    {
        // Disposing the compressor flushes its trailer and closes the underlying stream, so measure through
        // ToArray (still readable after close) rather than Length.
        var output = new MemoryStream();
        using (var compressor = wrap(output))
        {
            compressor.Write(content);
        }

        return output.ToArray().LongLength;
    }

    /// <summary>Walks up from the executable to the folder that holds Bit.Butil/Scripts.</summary>
    internal static string? LocateButilProject()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            var candidate = Path.Combine(directory.FullName, "Bit.Butil", "Scripts");
            if (Directory.Exists(candidate)) return Path.Combine(directory.FullName, "Bit.Butil");
        }

        return null;
    }
}
