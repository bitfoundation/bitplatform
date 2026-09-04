using System.Diagnostics;
using Bit.Butil.Build;

namespace ButilTests.Manual;

/// <summary>
/// The publish itself: a real consumer app published with each combination of the switches, and the
/// JavaScript that came out of it read back off disk.
/// </summary>
/// <remarks>
/// Everything else in this harness checks the computation - which modules a set of inputs works out to - by
/// calling into Bit.Butil.Build directly. That is one step of the feature. The rest is MSBuild: whether the
/// trimming runs at all, which of the three signals it is allowed to use, that a csproj module list is added
/// to what the others found rather than replacing it, that the assets removed from the build list are also
/// removed from the publish list, and that a name that means nothing fails the build instead of being
/// ignored. None of that is reachable from a method call, and all of it is what a consumer actually gets.
/// <br/>
/// So these publish <c>Bit.Butil.Tests.PublishFixture</c> - a two-class web app next door - and assert on the
/// bundle and the module files in its publish output. One <c>dotnet publish</c> each, which is why the list
/// of scenarios is short and every one of them is a distinct claim rather than a variation.
/// <br/>
/// Untrimmed runs only. The fixture is published by this process, so the trimmed copy of this harness would
/// publish the very same app to the very same answers - twice the cost for nothing.
/// </remarks>
internal static class ScriptPublishing
{
    private const string FixtureProject = "Bit.Butil.Tests.PublishFixture.csproj";

    /// <summary>Where the fixture's JavaScript lands, under the publish directory.</summary>
    private const string ContentPath = "wwwroot/_content/Bit.Butil";

    /// <summary>
    /// A publish is slow enough that a hung one has to be cut off rather than waited on: a build server that
    /// stops making progress would otherwise take the whole harness down with it.
    /// </summary>
    private static readonly TimeSpan PublishTimeout = TimeSpan.FromMinutes(5);

    /// <summary>What a scenario asks of a publish, and what its output has to look like afterwards.</summary>
    /// <param name="Name">Named after the claim, since that is what a failure reports.</param>
    /// <param name="Properties">The MSBuild properties a consumer would put in their csproj.</param>
    /// <param name="Bundle">The modules bit-butil.js must hold. Null, with <paramref name="FullBundle"/> off, means there must be no bundle at all.</param>
    /// <param name="FullBundle">The bundle must hold every module the library ships - the feature standing down.</param>
    /// <param name="BundleIsMinimum">The bundle must hold at least <paramref name="Bundle"/>; more is allowed.</param>
    /// <param name="Modules">The per-module files that must be published, or null for "none of them".</param>
    /// <param name="Error">A fragment of the build error the publish must fail with, when it must fail.</param>
    private sealed record Scenario(
        string Name,
        string[] Properties,
        string[]? Bundle = null,
        bool FullBundle = false,
        bool BundleIsMinimum = false,
        string[]? Modules = null,
        string? Error = null);

    /// <summary>
    /// The modules the fixture's own two classes need, and the two that every bundle carries: <c>butil</c> is
    /// the prelude every module depends on and <c>utils</c> is pulled in behind these two by the manifest.
    /// Spelled out rather than computed, so a change in what the fixture calls has to be stated here too.
    /// </summary>
    private static readonly string[] Scanned = ["butil", "utils", "clipboard", "geolocation"];

    private static Scenario[] Scenarios =>
    [
        // Nothing to trim against. The one that has to keep working: a consumer who never asked for any of
        // this must publish the whole bundle, not an empty one. Reached here by turning the scan off, since
        // the switch this fixture leaves on brings the scan with it - which is the next scenario.
        new("with no signal at all the full bundle is published",
            ["BitButilScriptScan=None"],
            FullBundle: true),

        // The switch on its own, which is what an app actually writes: BitButilTrimScripts=true (set by this
        // fixture's csproj, and the default in a WebAssembly project) has to be enough, with no scan mode
        // named beside it - the same answer as the explicit TypeReferences below.
        new("the trimming switch alone scans the app, with no scan mode set",
            [],
            Bundle: Scanned),

        // Option 2/3: the app's own assemblies, for a publish ILLink never touched.
        new("a scan of the app's own assemblies trims the bundle to what its classes need",
            ["BitButilScriptScan=TypeReferences"],
            Bundle: Scanned),

        // The coarser mode. A minimum rather than an exact set: matching bare type names is meant to
        // over-include - the library has classes called Console, Document and Location - and pinning the
        // exact list would be asserting which of those names the framework happens to use this month.
        new("the name-matching scan finds at least what the reference-matching one does",
            ["BitButilScriptScan=TypeNames"],
            Bundle: Scanned,
            BundleIsMinimum: true),

        // Option 1 alone: no ILLink, no scan, just a csproj. The whole point of it being a signal in its own
        // right rather than an addition to one - so the scan the switch defaults to is turned off to isolate
        // it, and what comes out is the csproj's module and nothing else.
        new("a csproj module list is a signal on its own",
            ["BitButilScriptScan=None", "FixtureScriptModules=Cookie"],
            Bundle: ["butil", "cookie"]),

        // Option 1 on top of option 3 - the thing that has to be additive rather than either/or.
        new("a csproj module list is added to what the scan found, not used instead of it",
            ["BitButilScriptScan=TypeReferences", "FixtureScriptModules=Cookie|battery"],
            Bundle: [.. Scanned, "cookie", "battery"]),

        // The other shape of the same JavaScript: no bundle at all, one file per module the app can reach.
        new("lazy scripts publish only the module files the scan can reach, and no bundle",
            ["BitButilLazyScripts=true", "BitButilScriptScan=TypeReferences"],
            Modules: Scanned),

        // Turning the feature off has to survive being given something to trim against.
        new("BitButilTrimScripts=false publishes the full bundle whatever else is set",
            ["BitButilTrimScripts=false", "BitButilScriptScan=TypeReferences", "FixtureScriptModules=Cookie"],
            FullBundle: true),

        // The properties in a shared Directory.Build.props reach every project in the solution, and the ones
        // that do not publish the app's static web assets - a Razor class library, a MAUI/Blazor Hybrid head -
        // must leave the JavaScript alone: their reference closure is not the app's, so trimming against it
        // would hand the head a bundle short of the modules only the head names.
        //
        // The gate reads both halves of what the SDK says about a project, so both are forced here: Root is
        // the .NET 10 Web SDK's StaticWebAssetProjectMode (the .NET 8 and 9 Web SDKs leave a head at
        // 'Default', which is why the marker below is read as well), and UsingMicrosoftNETSdkWeb is what
        // says a Web SDK was loaded at all. A class library sets neither, and this fixture - a Web SDK app -
        // stands in for one by unsetting both.
        new("a project that does not publish the app's static web assets does not trim",
            ["StaticWebAssetProjectMode=Default", "UsingMicrosoftNETSdkWeb=false", "BitButilScriptScan=TypeReferences", "FixtureScriptModules=Cookie"],
            FullBundle: true),

        // MSBuild accepts a misspelled item without a word, so the build is the only thing that can say so.
        new("a module name that names nothing fails the publish",
            ["FixtureScriptModules=Clippboard"],
            Error: "'Clippboard'"),

        new("a scan mode that is not one of the three fails the publish",
            ["BitButilScriptScan=TypeRefs"],
            Error: "not a value of <BitButilScriptScan>"),
    ];

    public static (int Passed, int Failed) Run(bool trimmed, List<string> failures)
    {
        var checks = new ScriptBundling.Checks(failures, "script publishing");
        if (trimmed) return (0, 0);

        var butilRoot = ScriptTrimming.LocateButilProject();
        var fixtureProject = butilRoot is null ? null : Path.Combine(butilRoot, "..", "tests", "Bit.Butil.Tests.PublishFixture", FixtureProject);
        var manifestPath = butilRoot is null ? null : Path.Combine(butilRoot, "obj", "butil-js", "chunks", "manifest.txt");

        if (fixtureProject is null || File.Exists(fixtureProject) is false || manifestPath is null || File.Exists(manifestPath) is false)
        {
            checks.That(false, "no publish was run", $"{FixtureProject} or Bit.Butil's module manifest could not be found from {AppContext.BaseDirectory}");
            return (checks.Passed, checks.Failed);
        }

        var manifest = ButilScriptBundler.ReadManifest(manifestPath);
        var workspace = Path.Combine(AppContext.BaseDirectory, "script-publishing");
        if (Directory.Exists(workspace)) Directory.Delete(workspace, recursive: true);

        var scenario = 0;
        foreach (var candidate in Scenarios)
        {
            var output = Path.Combine(workspace, $"publish-{++scenario}");
            if (Publish(checks, fixtureProject, output, candidate, out var log) is false) continue;

            if (candidate.Error is not null)
            {
                checks.That(log.Contains(candidate.Error, StringComparison.Ordinal), candidate.Name,
                    $"the publish did fail, but over something else: {FirstError(log)}");
                continue;
            }

            var expectedBundle = candidate.FullBundle ? manifest.Order : candidate.Bundle;

            CheckBundle(checks, candidate, Path.Combine(output, ContentPath, "bit-butil.js"), expectedBundle, manifest);
            CheckModules(checks, candidate, Path.Combine(output, ContentPath, "modules"), manifest);
        }

        CheckReferencedPublishStage(checks, fixtureProject);

        return (checks.Passed, checks.Failed);
    }

    /// <summary>
    /// The trimming stage reached from a project instance where only its own target chain has run, rather
    /// than the way a publish of this project reaches it.
    /// </summary>
    /// <remarks>
    /// ResolvePublishStaticWebAssets is not the publish-only stage its name suggests. The SDK also runs it on
    /// referenced projects, through ComputeReferencedStaticWebAssetsPublishManifest, and a head that packages
    /// its wwwroot at build time reaches it during a plain build. Either way it can land in a project instance
    /// where only that target chain has run - a MSBuild call carrying global properties of its own forks a
    /// fresh instance - and ResolveReferences is not part of that chain. @(ReferenceCopyLocalPaths) is then
    /// empty, and the trimming, which reads the untrimmed Bit.Butil.dll out of it to know which module each
    /// class needs, used to fail the build with "'' could not be read" and no way out but switching the
    /// feature off. _BitButilAssembleTrimmedBundle naming ResolveReferences in its DependsOnTargets is the
    /// fix, and this is what holds it there.
    /// <br/>
    /// The two Butil targets are driven by name, in order, which is what puts them in that position - a
    /// `dotnet publish` cannot, because by then ResolveReferences has long since run. In order because
    /// MSBuild evaluates a target's own Condition before building its DependsOnTargets, so the gate
    /// _BitButilResolveScriptTrimming sets has to be set by a previous entry in the target list rather than
    /// by a dependency. Driving the SDK's own ComputeReferencedStaticWebAssetsPublishManifest instead would
    /// walk Bit.Butil's asset pipeline down a path no consumer takes and die there, before any of this runs.
    /// <br/>
    /// Lazy scripts, because in an instance this bare there is no @(StaticWebAsset) to find a bundle among,
    /// and the per-module half of the trimming is the half that still has work to do without one. The claim
    /// is asserted positively - the scan's own line, which it only ever writes having read that assembly -
    /// so that a stage that stood down, or never ran, fails rather than passing by saying nothing.
    /// </remarks>
    private static void CheckReferencedPublishStage(ScriptBundling.Checks checks, string fixtureProject)
    {
        const string Claim = "the trimming stage resolves Bit.Butil.dll when only its own targets have run";

        string[] arguments =
        [
            "msbuild", fixtureProject, "-t:_BitButilResolveScriptTrimming", "-t:_BitButilAssembleTrimmedBundle",
            "--nologo", "-v:normal", "-tl:off", "-p:BitButilScriptScan=TypeReferences", "-p:BitButilLazyScripts=true",
        ];

        if (Run(arguments, out var log, out var exitCode) is false)
        {
            checks.That(false, $"{Claim} was not checked", log);
            return;
        }

        checks.That(exitCode == 0 && log.Contains("referencing Bit.Butil and found", StringComparison.Ordinal), Claim,
            exitCode == 0
                ? "the stage ran but the scan never reported reading the app's assemblies, so the untrimmed Bit.Butil.dll did not resolve"
                : $"the stage failed: {FirstError(log)}");
    }

    /// <summary>
    /// The bundle holds a module when it holds the chunk that registers it, which is what the browser would
    /// find too - the file is the chunks concatenated, so its content is the assertion, not its size.
    /// </summary>
    private static void CheckBundle(ScriptBundling.Checks checks, Scenario scenario, string bundlePath, IReadOnlyList<string>? expected, ButilScriptManifest manifest)
    {
        if (expected is null)
        {
            checks.That(File.Exists(bundlePath) is false, scenario.Name,
                $"bit-butil.js was published anyway ({(File.Exists(bundlePath) ? new FileInfo(bundlePath).Length.ToString("N0") : "0")} bytes) - a lazy-scripts app never loads it");
            return;
        }

        if (File.Exists(bundlePath) is false)
        {
            checks.That(false, scenario.Name, "no bit-butil.js was published at all");
            return;
        }

        var content = File.ReadAllText(bundlePath);
        var present = manifest.Order.Where(module => content.Contains(Guard(module), StringComparison.Ordinal)).ToArray();

        if (scenario.BundleIsMinimum)
        {
            var missing = expected.Except(present, StringComparer.Ordinal).OrderBy(name => name, StringComparer.Ordinal).ToArray();
            checks.That(missing.Length == 0, scenario.Name,
                $"the published bundle is missing [{string.Join(", ", missing)}] - those APIs would be dead in the browser");
            return;
        }

        Compare(checks, scenario.Name, expected, present,
            missing => $"the published bundle is missing [{missing}] - those APIs would be dead in the browser",
            extra => $"the published bundle carries [{extra}], which nothing in the app can reach");
    }

    private static void CheckModules(ScriptBundling.Checks checks, Scenario scenario, string modulesDirectory, ButilScriptManifest manifest)
    {
        var present = Directory.Exists(modulesDirectory)
            ? Directory.GetFiles(modulesDirectory, "*.js").Select(Path.GetFileNameWithoutExtension).OfType<string>().ToArray()
            : [];

        if (scenario.Modules is null)
        {
            checks.That(present.Length == 0, $"{scenario.Name} (and publishes no per-module files)",
                $"{present.Length} of them were published anyway: [{string.Join(", ", present.Take(8))}{(present.Length > 8 ? ", ..." : string.Empty)}]");
            return;
        }

        Compare(checks, $"{scenario.Name} (module files)", scenario.Modules, present,
            missing => $"[{missing}] was not published, so the import() for it would 404",
            extra => $"[{extra}] was published for nothing");

        // A module file the manifest does not know is a name this check would silently pass over.
        var unknown = present.Where(module => manifest.Dependencies.ContainsKey(module) is false).ToArray();
        checks.That(unknown.Length == 0, $"{scenario.Name} (every published module file is a real module)", $"[{string.Join(", ", unknown)}] is not in the manifest");
    }

    /// <summary>
    /// The guard build.mjs wraps every chunk in, which names the namespace that chunk registers. Matching on
    /// it rather than on any occurrence of the module's name keeps a module that merely <em>mentions</em>
    /// another from being read as that other one being present.
    /// </summary>
    private static string Guard(string module) => $"window.BitButil.{(module == "butil" ? "version" : module)}";

    /// <summary>
    /// Runs <c>dotnet</c> with the arguments given. False when it could not be started or did not finish, and
    /// <paramref name="log"/> then says which rather than carrying output; true with the combined output and
    /// the exit code, whatever that was - a run that failed is an outcome for the caller to read, not an
    /// error here.
    /// </summary>
    private static bool Run(string[] arguments, out string log, out int exitCode)
    {
        exitCode = 0;

        var process = new Process
        {
            StartInfo = new ProcessStartInfo("dotnet")
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
            }
        };

        foreach (var argument in arguments) process.StartInfo.ArgumentList.Add(argument);

        try
        {
            process.Start();
        }
        catch (Exception exception)
        {
            log = $"dotnet could not be started ({exception.Message})";
            return false;
        }

        // Read both streams before waiting: a publish writes more than a pipe buffer holds, and waiting first
        // would deadlock against a process blocked on a full stdout.
        var standardOutput = process.StandardOutput.ReadToEndAsync();
        var standardError = process.StandardError.ReadToEndAsync();

        if (process.WaitForExit((int)PublishTimeout.TotalMilliseconds) is false)
        {
            try { process.Kill(entireProcessTree: true); } catch { /* it is already gone, which is the outcome wanted */ }
            log = $"it did not finish within {PublishTimeout.TotalMinutes:N0} minutes";
            return false;
        }

        log = standardOutput.Result + standardError.Result;
        exitCode = process.ExitCode;
        return true;
    }

    private static bool Publish(ScriptBundling.Checks checks, string project, string output, Scenario scenario, out string log)
    {
        string[] arguments =
        [
            "publish", project, "-c", "Debug", "--nologo", "-v", "quiet", "-tl:off", "-o", output,
            .. scenario.Properties.Select(property => $"-p:{property}"),
        ];

        if (Run(arguments, out log, out var exitCode) is false)
        {
            checks.That(false, $"{scenario.Name} was not checked", log);
            log = string.Empty;
            return false;
        }

        var failed = exitCode != 0;

        if (scenario.Error is not null)
        {
            // The scenario is the failure, so a publish that succeeded is the defect: MSBuild took a name
            // that means nothing and published something anyway.
            return checks.That(failed, scenario.Name, "the publish succeeded, so the bad input was accepted in silence");
        }

        return checks.That(failed is false, $"{scenario.Name} was not checked", $"the publish failed: {FirstError(log)}");
    }

    /// <summary>The first error line of a build log, which is the one that says what went wrong.</summary>
    private static string FirstError(string log)
    {
        var line = log.Split('\n').FirstOrDefault(candidate => candidate.Contains("error", StringComparison.OrdinalIgnoreCase))?.Trim();

        return string.IsNullOrEmpty(line) ? "no error line in the build log" : (line.Length > 300 ? line[..300] + "..." : line);
    }

    private static void Compare(ScriptBundling.Checks checks, string what, IEnumerable<string> expected, IEnumerable<string> actual,
        Func<string, string> onMissing, Func<string, string> onExtra)
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
