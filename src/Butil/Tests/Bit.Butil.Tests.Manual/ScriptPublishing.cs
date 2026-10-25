using System.Diagnostics;
using System.Globalization;
using System.IO.Compression;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;
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
/// A few publish <c>Bit.Butil.Tests.PublishFixture.Hosted</c> instead, a Blazor Web App whose WebAssembly
/// client has a service worker, or that client on its own. The trimming changes files a service worker
/// precaches, in a project other than the one that listed them, and those scenarios check that what the
/// service worker is told to install is what the publish serves.
/// <br/>
/// Every publish, of either fixture, also has its endpoints and compressed copies checked against the files
/// on disk: the trimming swaps and removes static web assets, and an asset swapped without everything derived
/// from it is a server answering with another file's headers, or another file altogether.
/// <br/>
/// Untrimmed runs only. The fixture is published by this process, so the trimmed copy of this harness would
/// publish the very same app to the very same answers - twice the cost for nothing.
/// </remarks>
internal static class ScriptPublishing
{
    private const string Fixture = "Bit.Butil.Tests.PublishFixture";

    private const string FixtureProject = Fixture + ".csproj";

    /// <summary>The Blazor Web App's server project, which publishes its client along with it.</summary>
    private const string HostedFixture = "Bit.Butil.Tests.PublishFixture.Hosted";

    /// <summary>The same client published on its own, as a standalone PWA.</summary>
    private const string HostedClientFixture = "Bit.Butil.Tests.PublishFixture.Hosted.Client";

    /// <summary>Where the fixture's JavaScript lands, under the publish directory.</summary>
    private const string ContentPath = "wwwroot/_content/Bit.Butil";

    /// <summary>The bundle's URL, as a service worker manifest lists it.</summary>
    private const string BundleUrl = "_content/Bit.Butil/bit-butil.js";

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
    /// <param name="Project">The fixture published: a folder under Tests/ holding the csproj of the same name.</param>
    /// <param name="ServiceWorker">For a fixture with a service worker, what the publish had to do to the manifest its client wrote; null for one without.</param>
    private sealed record Scenario(
        string Name,
        string[] Properties,
        string[]? Bundle = null,
        bool FullBundle = false,
        bool BundleIsMinimum = false,
        string[]? Modules = null,
        string? Error = null,
        string Project = Fixture,
        ServiceWorkerRewrite? ServiceWorker = null);

    /// <summary>What a publish has to do to a service worker manifest written before the trimming ran.</summary>
    private enum ServiceWorkerRewrite
    {
        /// <summary>Nothing: everything it lists is served as it lists it.</summary>
        None,

        /// <summary>Give the bundle's entry the hash of the bundle the trimming served in its place.</summary>
        Rehash,

        /// <summary>Drop the entries of the module files the trimming stopped publishing.</summary>
        Drop,
    }

    /// <summary>
    /// The modules the fixture's own two classes need, and the two that every bundle carries: <c>butil</c> is
    /// the prelude every module depends on and <c>utils</c> is pulled in behind these two by the manifest.
    /// Spelled out rather than computed, so a change in what the fixture calls has to be stated here too.
    /// </summary>
    private static readonly string[] Scanned = ["butil", "utils", "clipboard", "geolocation"];

    /// <summary>
    /// What the hosted fixture's client reaches (Clipboard), and what its server does: the client's assembly and
    /// its own Geolocation. One module apart on purpose - the server's bundle is then never the one the client's
    /// manifest describes, and in lazy mode the server publishes a module the client never listed.
    /// </summary>
    private static readonly string[] HostedClientModules = ["butil", "utils", "clipboard"];

    private static readonly string[] HostedServerModules = [.. HostedClientModules, "geolocation"];

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

        // One module out of a split family, kept by name through a real publish. The finer control the
        // split is for: an app reaching Butil's JavaScript from its own scripts can keep the key-management
        // module without the four other crypto modules coming with it - and cryptoKeyMaterial does come,
        // because the manifest says cryptoKeys cannot run without it.
        new("a csproj list naming one module of a split family publishes that module and its dependencies alone",
            ["BitButilScriptScan=None", "FixtureScriptModules=cryptoKeys"],
            Bundle: ["butil", "utils", "cryptoKeyMaterial", "cryptoKeys"]),

        // And the whole family when the class is named instead, since that is what a consumer keeping
        // "Crypto" means by it.
        new("a csproj list naming the class of a split family publishes the whole family",
            ["BitButilScriptScan=None", "FixtureScriptModules=Crypto"],
            Bundle: ["butil", "utils", "crypto", "cryptoCipher", "cryptoDerive", "cryptoKeyMaterial", "cryptoKeys", "cryptoSign"]),

        // The lazy shape of the same thing: one file per module, split family included.
        new("lazy scripts publish one file per module of a kept split family",
            ["BitButilLazyScripts=true", "BitButilScriptScan=None", "FixtureScriptModules=userAgentParser"],
            Modules: ["butil", "userAgentParser"]),

        // The other shape of the same JavaScript: no bundle at all, one file per module the app can reach.
        new("lazy scripts publish only the module files the scan can reach, and no bundle",
            ["BitButilLazyScripts=true", "BitButilScriptScan=TypeReferences"],
            Modules: Scanned),

        // Kept straight after the lazy ones, and bundle-shaped: every scenario publishes from the same obj
        // folder, so this is also lazy scripts turned off through a -p: on an incremental publish - the case
        // that used to leave the lazy-scripts initializer compiled into a bundle app (see CheckLazyScriptsSwitch).
        // Turning the feature off has to survive being given something to trim against.
        new("BitButilTrimScripts=false publishes the full bundle whatever else is set",
            ["BitButilTrimScripts=false", "BitButilScriptScan=TypeReferences", "FixtureScriptModules=Cookie"],
            FullBundle: true),

        // The properties in a shared Directory.Build.props reach every project in the solution, and the ones
        // that do not publish the app's static web assets - a Razor class library, a MAUI/Blazor Hybrid head -
        // must leave the JavaScript alone: their reference closure is not the app's, so trimming against it
        // would hand the head a bundle short of the modules only the head names.
        //
        // What the gate reads is the SDK a project loaded: UsingMicrosoftNETSdkWeb and the two WebAssembly
        // markers, which are set identically on 8, 9 and 10. A class library sets none of them, and this
        // fixture - a Web SDK app - stands in for one by unsetting the one it has. StaticWebAssetProjectMode
        // is set to the 'Default' a library carries as well, so the shape being stood in for is the whole
        // shape, even though the gate no longer reads that half.
        new("a project that does not publish the app's static web assets does not trim",
            ["StaticWebAssetProjectMode=Default", "UsingMicrosoftNETSdkWeb=false", "BitButilScriptScan=TypeReferences", "FixtureScriptModules=Cookie"],
            FullBundle: true),

        // Root is not the signal, and this is the shape that says so: a MAUI/Blazor Hybrid head IS the root
        // of its asset graph - the WebView package makes it one so that it can package its wwwroot - and it
        // reaches the publish asset stage from ConvertStaticWebAssetsToMauiAssets, before ResolveReferences
        // rather than after. Trimming there reads references that are not resolved yet, and the dependency
        // that would resolve them closes a cycle in that head's target graph (MSB4006), which is a failed
        // build rather than a wrong bundle. Reading StaticWebAssetProjectMode as "this is the app's head" is
        // therefore the tempting mistake, and this is what catches it: the previous scenario would still pass
        // if Root were let back into the gate, and this one - Root forced on with the Web SDK marker off -
        // would not.
        new("the asset root of a hybrid head does not trim",
            ["StaticWebAssetProjectMode=Root", "UsingMicrosoftNETSdkWeb=false", "BitButilScriptScan=TypeReferences", "FixtureScriptModules=Cookie"],
            FullBundle: true),

        // MSBuild accepts a misspelled item without a word, so the build is the only thing that can say so.
        new("a module name that names nothing fails the publish",
            ["FixtureScriptModules=Clippboard"],
            Error: "'Clippboard'"),

        new("a scan mode that is not one of the three fails the publish",
            ["BitButilScriptScan=TypeRefs"],
            Error: "not a value of <BitButilScriptScan>"),

        // A Blazor Web App, the way the Boilerplate ships one. The client writes service-worker-assets.js during
        // its own publish, from its own trimmed bundle; the server trims again, to one more module, and serves
        // that. Left alone the manifest lists bit-butil.js under the client's hash (#13926) - and an installed
        // PWA keeps a cached file for as long as its listed hash stays the same, so it never sees a new bundle.
        new("a Blazor Web App's service worker lists the bundle its server trimmed, under that bundle's hash",
            [],
            Bundle: HostedServerModules,
            Project: HostedFixture,
            ServiceWorker: ServiceWorkerRewrite.Rehash),

        // The same on .NET 8, which the SDK fingerprints and compresses differently: no content fingerprint in
        // an asset's path, and the identity file's hash on a compressed endpoint as a weak ETag instead.
        new("a .NET 8 Blazor Web App's service worker lists the bundle its server trimmed, under that bundle's hash",
            ["FixtureFramework=net8.0"],
            Bundle: HostedServerModules,
            Project: HostedFixture,
            ServiceWorker: ServiceWorkerRewrite.Rehash),

        // Lazy scripts, with a client that does not trim: it lists every module file, and the server publishes
        // four of them. A service worker that is still told about the rest requests each one and gets a 404 -
        // which fails the install of Blazor's own template service worker outright.
        new("a lazy-scripts Blazor Web App's service worker stops listing the module files its server dropped",
            ["BitButilLazyScripts=true", "FixtureClientTrimScripts=false"],
            Modules: HostedServerModules,
            Project: HostedFixture,
            ServiceWorker: ServiceWorkerRewrite.Drop),

        // Lazy scripts trimmed on both sides, which is what the default gives: the server keeps a superset of
        // the client's modules, so everything listed is served as listed and the manifest is not touched. The
        // module only the server's code calls is not precached - it is fetched when the server's code calls it,
        // and that code does not run offline.
        new("a lazy-scripts Blazor Web App trimmed on both sides publishes its client's manifest as it is",
            ["BitButilLazyScripts=true"],
            Modules: HostedServerModules,
            Project: HostedFixture,
            ServiceWorker: ServiceWorkerRewrite.None),

        // The client on its own, a standalone PWA. The SDK writes its manifest after the trimming, from the
        // trimmed files, so there is nothing to rewrite. Here so that an SDK moving the manifest ahead of the
        // trimming - or a change moving the trimming after it - shows up as a manifest that is wrong again.
        new("a standalone PWA's service worker lists its own trimmed bundle, with nothing to rewrite",
            [],
            Bundle: HostedClientModules,
            Project: HostedClientFixture,
            ServiceWorker: ServiceWorkerRewrite.None),
    ];

    public static (int Passed, int Failed) Run(bool trimmed, List<string> failures)
    {
        var checks = new ScriptBundling.Checks(failures, "script publishing");
        if (trimmed) return (0, 0);

        var butilRoot = ScriptTrimming.LocateButilProject();
        var fixtureProject = butilRoot is null ? null : Path.Combine(butilRoot, "..", "Tests", Fixture, FixtureProject);
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
            var project = Path.Combine(butilRoot!, "..", "Tests", candidate.Project, candidate.Project + ".csproj");
            if (Publish(checks, project, output, candidate, out var log) is false) continue;

            if (candidate.Error is not null)
            {
                checks.That(log.Contains(candidate.Error, StringComparison.Ordinal), candidate.Name,
                    $"the publish did fail, but over something else: {FirstError(log)}");
                continue;
            }

            var expectedBundle = candidate.FullBundle ? manifest.Order : candidate.Bundle;

            CheckBundle(checks, candidate, Path.Combine(output, ContentPath, "bit-butil.js"), expectedBundle, manifest);
            CheckModules(checks, candidate, Path.Combine(output, ContentPath, "modules"), manifest);
            CheckLazyScriptsSwitch(checks, candidate, output);
            CheckEndpoints(checks, candidate, output);

            var butilFiles = Directory.Exists(Path.Combine(output, ContentPath))
                ? Directory.GetFiles(Path.Combine(output, ContentPath), "*.js", SearchOption.AllDirectories)
                : [];
            CheckCompressedCopies(checks, $"{candidate.Name} (the compressed copies of Bit.Butil's files are those files)", output, butilFiles);

            if (candidate.ServiceWorker is { } rewrite) CheckServiceWorker(checks, candidate, rewrite, output, log);
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
    /// Both halves of the lazy-scripts switch say what the scenario asked for: the module initializer compiled
    /// into the app, and the runtime-configuration option beside it.
    /// </summary>
    /// <remarks>
    /// The JavaScript checks cannot see this. A bundle publish still carrying the initializer of an earlier
    /// lazy one ships exactly the right files, and then calls BitButil.UseLazyScripts() at startup and
    /// <c>import()</c>s module files the publish no longer has. That is what an incremental publish did when the
    /// property was turned off through a -p:, the way every scenario here sets it: the generated file joined
    /// @(Compile) after the hash that decides whether CoreCompile is up to date, so dropping it changed nothing
    /// CoreCompile looked at. Read from the published assembly's metadata rather than by loading it, since this
    /// process has its own copy of Bit.Butil loaded already.
    /// <br/>
    /// Not the standalone WebAssembly client: its assemblies are published as Webcil under _framework, which a
    /// PEReader cannot open, and its runtime options live in the boot manifest rather than a runtimeconfig.json.
    /// The hosted server, which publishes that client too, is read from its own assembly.
    /// </remarks>
    private static void CheckLazyScriptsSwitch(ScriptBundling.Checks checks, Scenario scenario, string output)
    {
        if (scenario.Project == HostedClientFixture) return;

        var lazy = scenario.Properties.Contains("BitButilLazyScripts=true", StringComparer.OrdinalIgnoreCase);
        var assemblyPath = Path.Combine(output, scenario.Project + ".dll");
        var runtimeConfigPath = Path.Combine(output, scenario.Project + ".runtimeconfig.json");

        if (File.Exists(assemblyPath) is false || File.Exists(runtimeConfigPath) is false)
        {
            checks.That(false, $"{scenario.Name} (lazy-scripts switch) was not checked", $"{Path.GetFileName(assemblyPath)} or its runtimeconfig.json was not published");
            return;
        }

        bool initializer;
        using (var reader = new PEReader(File.OpenRead(assemblyPath)))
        {
            var metadata = reader.GetMetadataReader();
            initializer = metadata.TypeDefinitions.Any(handle => metadata.StringComparer.Equals(metadata.GetTypeDefinition(handle).Name, "BitButilLazyScriptsInitializer"));
        }

        checks.That(initializer == lazy, $"{scenario.Name} (and compiles the lazy-scripts initializer in only when lazy)",
            lazy
                ? "BitButilLazyScriptsInitializer is not in the published assembly, so a .NET 8 WebAssembly app would never turn lazy scripts on"
                : "BitButilLazyScriptsInitializer is still compiled into the published assembly, so the app turns lazy scripts on at startup and import()s module files this publish does not carry - CoreCompile was skipped as up to date");

        using var runtimeConfig = JsonDocument.Parse(File.ReadAllText(runtimeConfigPath));
        var option = runtimeConfig.RootElement.TryGetProperty("runtimeOptions", out var options)
            && options.TryGetProperty("configProperties", out var properties)
            && properties.TryGetProperty("Bit.Butil.LazyScripts", out var value)
            && value.ValueKind == JsonValueKind.True;

        checks.That(option == lazy, $"{scenario.Name} (and sets the Bit.Butil.LazyScripts runtime option only when lazy)",
            lazy
                ? "the runtimeconfig.json has no Bit.Butil.LazyScripts option, so the runtime half of the switch is off"
                : "the runtimeconfig.json still sets Bit.Butil.LazyScripts, so the app loads per module from files this publish does not carry");
    }

    /// <summary>
    /// The guard build.mjs wraps every chunk in, which names the namespace that chunk registers. Matching on
    /// it rather than on any occurrence of the module's name keeps a module that merely <em>mentions</em>
    /// another from being read as that other one being present.
    /// </summary>
    /// <remarks>
    /// Closing paren included, because a module name can be a prefix of another's: the guard reads
    /// <c>window.BitButil.cryptoKeys)</c>, and without the paren a bundle holding only that chunk would
    /// answer yes to <c>crypto</c> as well - reporting a module the app cannot reach, or hiding one it
    /// needs behind a sibling that happens to be there.
    /// </remarks>
    private static string Guard(string module) => $"window.BitButil.{(module == "butil" ? "version" : module)})";

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

    /// <summary>
    /// The precache list a service worker installs from, against the files the publish serves: every URL listed
    /// is published under the hash listed - a service worker caches by that hash, and one that checks integrity
    /// rejects any other - and the script carries the list's version, the change a browser installs an update over.
    /// </summary>
    /// <remarks>
    /// Whether the publish rewrote the manifest is read off the task's own line in the build log, and asserted
    /// either way. Positively, so that a scenario which stopped exercising the mismatch it was written for - a
    /// fixture change leaving the client and the server trimming to the same set - fails rather than passing over
    /// a manifest nobody had to touch; negatively, so that a manifest with nothing wrong in it is not rewritten
    /// into a new version that makes every installed app download its precache again.
    /// </remarks>
    private static void CheckServiceWorker(ScriptBundling.Checks checks, Scenario scenario, ServiceWorkerRewrite expected, string output, string log)
    {
        var webRoot = Path.Combine(output, "wwwroot");
        var manifestPath = Path.Combine(webRoot, "service-worker-assets.js");
        var scriptPath = Path.Combine(webRoot, "service-worker.js");

        var rewrite = Regex.Match(log, @"Bit\.Butil: \S+ lists (?<dropped>\d+) fewer assets and (?<rehashed>\d+) trimmed hashes");
        var dropped = rewrite.Success ? int.Parse(rewrite.Groups["dropped"].Value, CultureInfo.InvariantCulture) : 0;
        var rehashed = rewrite.Success ? int.Parse(rewrite.Groups["rehashed"].Value, CultureInfo.InvariantCulture) : 0;

        switch (expected)
        {
            case ServiceWorkerRewrite.None:
                checks.That(rewrite.Success is false, $"{scenario.Name} (and publishes the manifest its client wrote, untouched)", $"it was rewritten: {rewrite.Value}");
                break;
            case ServiceWorkerRewrite.Rehash:
                checks.That(rehashed > 0 && dropped == 0, $"{scenario.Name} (by giving the bundle's entry a new hash)",
                    rewrite.Success ? $"the rewrite was: {rewrite.Value}" : "the manifest was never rewritten - either the rewrite did not run, or the client and the server now trim alike and the fixture no longer tests a mismatch");
                break;
            case ServiceWorkerRewrite.Drop:
                checks.That(dropped > 0 && rehashed == 0, $"{scenario.Name} (by dropping entries)",
                    rewrite.Success ? $"the rewrite was: {rewrite.Value}" : "the manifest was never rewritten - either the rewrite did not run, or the client now lists only what the server publishes and the fixture no longer tests a mismatch");
                break;
        }

        if (File.Exists(manifestPath) is false || File.Exists(scriptPath) is false)
        {
            checks.That(false, $"{scenario.Name} (and publishes a service worker)", "service-worker-assets.js or service-worker.js is missing from the publish output");
            return;
        }

        var text = File.ReadAllText(manifestPath);
        string? version;
        var listed = new List<string>();
        var wrong = new List<string>();

        try
        {
            using var manifest = JsonDocument.Parse(text[text.IndexOf('{')..(text.LastIndexOf('}') + 1)]);
            version = manifest.RootElement.GetProperty("version").GetString();

            foreach (var asset in manifest.RootElement.GetProperty("assets").EnumerateArray())
            {
                var url = asset.GetProperty("url").GetString()!;
                var path = Path.Combine(webRoot, url.Replace('/', Path.DirectorySeparatorChar));
                listed.Add(url);

                if (File.Exists(path) is false && url.EndsWith(".map", StringComparison.Ordinal)) continue;
                if (File.Exists(path) is false) wrong.Add($"{url} is listed but not published");
                else if ($"sha256-{Sha256(path)}" != asset.GetProperty("hash").GetString()) wrong.Add($"{url} is listed under another file's hash");
            }
        }
        catch (Exception exception) when (exception is JsonException or KeyNotFoundException or ArgumentOutOfRangeException)
        {
            checks.That(false, $"{scenario.Name} (and publishes a manifest that parses)", exception.Message);
            return;
        }

        if (scenario.Bundle is not null && listed.Contains(BundleUrl) is false) wrong.Add("bit-butil.js is not listed at all");

        checks.That(wrong.Count == 0, $"{scenario.Name} (every file listed is published, under its own hash)", Summarize(wrong));
        checks.That(File.ReadAllText(scriptPath).Contains($"{ButilServiceWorkerManifest.VersionMarker}{version}", StringComparison.Ordinal),
            $"{scenario.Name} (and service-worker.js carries the manifest's version)", $"it does not name version {version}, so a browser has no new service worker to install");

        // The script's own copies only where the publish rewrote it. The .br/.gz copies the SDK itself publishes of
        // service-worker.js lack the version line it writes into the file (the 10.0.402 SDK's do, in a clean
        // standalone publish with no Bit.Butil involved), which is not this feature's to fix; a script Bit.Butil
        // rewrote is compressed afresh from the rewritten file, and that much is checked.
        string[] compressed = [manifestPath, .. listed.Select(url => Path.Combine(webRoot, url.Replace('/', Path.DirectorySeparatorChar))), .. rewrite.Success ? [scriptPath] : Array.Empty<string>()];
        CheckCompressedCopies(checks, $"{scenario.Name} (the compressed copies of the manifest and of what it lists are those files)", output, compressed);
    }

    /// <summary>
    /// Every endpoint the published app serves names a file that was published, and describes that file: its
    /// length, its ETag, its integrity. MapStaticAssets answers straight off this manifest - those headers are
    /// written into it at publish time, not read off the file - so an asset swapped without its endpoints is a
    /// response with another file's Content-Length (a truncated or padded download) or integrity (a script the
    /// browser refuses to run).
    /// </summary>
    /// <remarks>
    /// A compressed response describes two files, and each value is checked against the right one: Content-Length
    /// and the strong ETag are the compressed file's, while the integrity and the weak ETag the .NET 8 SDK adds are
    /// those of the file it decompresses to - the identity file the same route serves without compression.
    /// </remarks>
    private static void CheckEndpoints(ScriptBundling.Checks checks, Scenario scenario, string output)
    {
        var what = $"{scenario.Name} (every endpoint describes the file it serves)";
        var manifests = Directory.GetFiles(output, "*.staticwebassets.endpoints.json");

        if (manifests.Length != 1)
        {
            checks.That(false, what, $"{manifests.Length} endpoint manifests were published, where one was expected");
            return;
        }

        var webRoot = Path.Combine(output, "wwwroot");
        var wrong = new List<string>();

        using var json = JsonDocument.Parse(File.ReadAllBytes(manifests[0]));
        foreach (var endpoint in json.RootElement.GetProperty("Endpoints").EnumerateArray())
        {
            var route = endpoint.GetProperty("Route").GetString();
            var assetFile = endpoint.GetProperty("AssetFile").GetString()!;
            var path = Path.Combine(webRoot, assetFile.Replace('/', Path.DirectorySeparatorChar));

            if (File.Exists(path) is false)
            {
                wrong.Add($"{route} is served from {assetFile}, which was not published");
                continue;
            }

            var encoded = endpoint.TryGetProperty("Selectors", out var selectors)
                && selectors.EnumerateArray().Any(selector => selector.GetProperty("Name").GetString() == "Content-Encoding");
            var identity = encoded ? path[..^Path.GetExtension(path).Length] : path;

            if (File.Exists(identity) is false)
            {
                wrong.Add($"{route} serves {assetFile} compressed, and the file it decompresses to was not published");
                continue;
            }

            foreach (var header in endpoint.GetProperty("ResponseHeaders").EnumerateArray())
            {
                var name = header.GetProperty("Name").GetString();
                var value = header.GetProperty("Value").GetString()!;

                if (name == "Content-Length" && value != new FileInfo(path).Length.ToString(CultureInfo.InvariantCulture))
                {
                    wrong.Add($"{route} ({assetFile}) answers with Content-Length {value} for a {new FileInfo(path).Length:N0}-byte file");
                }
                else if (name == "ETag")
                {
                    var weak = value.StartsWith("W/", StringComparison.Ordinal);
                    if ((weak ? value[2..] : value) != $"\"{Sha256(weak ? identity : path)}\"") wrong.Add($"{route} ({assetFile}) answers with another file's {(weak ? "weak " : string.Empty)}ETag");
                }
            }

            if (endpoint.TryGetProperty("EndpointProperties", out var properties))
            {
                foreach (var property in properties.EnumerateArray())
                {
                    if (property.GetProperty("Name").GetString() == "integrity" && property.GetProperty("Value").GetString() != $"sha256-{Sha256(identity)}")
                    {
                        wrong.Add($"{route} ({assetFile}) carries another file's integrity");
                    }
                }
            }
        }

        checks.That(wrong.Count == 0, what, Summarize(wrong));
    }

    /// <summary>
    /// The .br and .gz copies published next to each of <paramref name="files"/> decompress to that file. The
    /// server picks a copy by the request's Accept-Encoding, so a copy left over from the file a publish replaced
    /// is the old file served to every browser that accepts compression - which is every browser.
    /// </summary>
    private static void CheckCompressedCopies(ScriptBundling.Checks checks, string what, string output, IEnumerable<string> files)
    {
        var wrong = new List<string>();

        foreach (var file in files.Where(File.Exists).Distinct(StringComparer.OrdinalIgnoreCase))
        {
            foreach (var (extension, decompress) in new (string, Func<Stream, Stream>)[]
            {
                (".gz", stream => new GZipStream(stream, CompressionMode.Decompress)),
                (".br", stream => new BrotliStream(stream, CompressionMode.Decompress)),
            })
            {
                var copy = file + extension;
                if (File.Exists(copy) is false) continue;

                using var content = new MemoryStream();
                using (var stream = decompress(File.OpenRead(copy))) stream.CopyTo(content);

                if (content.ToArray().AsSpan().SequenceEqual(File.ReadAllBytes(file)) is false)
                {
                    wrong.Add($"{Path.GetRelativePath(output, copy).Replace('\\', '/')} is not {Path.GetFileName(file)} compressed");
                }
            }
        }

        checks.That(wrong.Count == 0, what, Summarize(wrong));
    }

    private static readonly Dictionary<string, string> Hashes = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>A file's SHA-256 in base64, the form integrity, ETags and service worker manifests all use.</summary>
    private static string Sha256(string path)
    {
        var key = $"{path}|{File.GetLastWriteTimeUtc(path).Ticks}";
        if (Hashes.TryGetValue(key, out var hash) is false) Hashes[key] = hash = Convert.ToBase64String(SHA256.HashData(File.ReadAllBytes(path)));
        return hash;
    }

    /// <summary>The first few of a list of problems, which is enough to act on, and how many there were.</summary>
    private static string Summarize(List<string> wrong) =>
        string.Join("; ", wrong.Take(5)) + (wrong.Count > 5 ? $"; and {wrong.Count - 5} more" : string.Empty);

    private static bool Publish(ScriptBundling.Checks checks, string project, string output, Scenario scenario, out string log)
    {
        // Normal verbosity where the service worker checks read the rewrite's own line out of the log.
        string[] arguments =
        [
            "publish", project, "-c", "Debug", "--nologo", "-v", scenario.ServiceWorker is null ? "quiet" : "normal", "-tl:off", "-o", output,
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

    /// <summary>
    /// The first error line of a build log, which is the one that says what went wrong. An MSBuild diagnostic
    /// first, since a log at normal verbosity also carries compiler command lines with "/errorreport" in them.
    /// </summary>
    private static string FirstError(string log)
    {
        var lines = log.Split('\n');
        var line = (lines.FirstOrDefault(candidate => candidate.Contains(": error ", StringComparison.Ordinal))
            ?? lines.FirstOrDefault(candidate => candidate.Contains("error", StringComparison.OrdinalIgnoreCase)))?.Trim();

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
