using ModelContextProtocol.Server;
using System.ComponentModel;

namespace Bit.Bswup.Demo.Server.Controllers;

/// <summary>
/// Ready-made workflows for the four things people actually ask a service-worker library for: add
/// it to an app, tune what it caches, work out why an app will not update, and take it back out.
/// <para>
/// Each prompt spends its words on the order to call the tools in and on the mistakes that are
/// expensive to make - the ones that ship green and fail on a user's machine, offline, a week
/// later - because the failure mode of an agent with a dozen tools is not ignorance, it is calling
/// them in a sequence that skips the check which would have caught the bug.
/// </para>
/// </summary>
[McpServerPromptType]
public static class McpPrompts
{
    [McpServerPrompt(Name = "add-bswup-to-app", Title = "Add Bswup to a Blazor app")]
    [Description("Walks through adding bit Bswup to an existing Blazor WebAssembly app, in the right order for its hosting model.")]
    public static string AddBswupToApp(
        [Description("The app's hosting model: standalone-wasm or blazor-web-app. Pass 'unknown' to have it determined from the project first.")] string hostingModel = "unknown")
    {
        return $"""
            Add bit Bswup to this Blazor app. Its hosting model is: {hostingModel}.

            Work in this order:

            1. If the hosting model is 'unknown', determine it from the project first: a `wwwroot/index.html` with
               `<div id="app">` is standalone WebAssembly; a server project with `Components/App.razor` and
               `AddInteractiveWebAssemblyComponents` is a Blazor Web App. Then continue with what you find.
            2. Call `GetBswupSetupGuide` with that hosting model and follow it. The host document differs, and so
               does what the client's asset manifest can see: in a Blazor Web App the app shell (`/`),
               `blazor.web.js` and the fingerprinted resource collection belong to the HOST project and must be
               listed in `self.externalAssets` by hand, or the app will not boot offline.
            3. Create BOTH `wwwroot/service-worker.js` and `wwwroot/service-worker.published.js` and keep their
               settings identical. The published file is the one deployed builds ship; a setting added to only one
               of them works in development and fails in production.
            4. Add `autostart="false"` to the Blazor entry script and reference `bit-bswup.js` after it. Do not
               call `Blazor.start()` yourself - Bswup owns startup now.
            5. Give the page a handler: `<BswupProgress />` with `bit-bswup.progress.js` and its stylesheet
               (call `GetBswupProgressUI` for the parameters), or a custom function named by the `handler`
               attribute (call `GetBswupEvents` for the messages it must handle).
            6. Before finishing, call `InspectBswupServiceWorker` with the content of each of the two
               service-worker files and resolve everything it reports as a problem.
            7. Build the app and fix what the compiler says.

            Show me the diff for each file you change, and say explicitly which settings you put in BOTH
            service-worker files.
            """;
    }

    [McpServerPrompt(Name = "configure-bswup-caching", Title = "Configure what Bswup caches")]
    [Description("Configures what bit Bswup caches and how it updates - external assets, excluded files, passive mode, API bypass, cache versioning - using its real settings rather than guessed ones.")]
    public static string ConfigureBswupCaching(
        [Description("What the caching should do, in your own words - e.g. 'cache the Google Fonts stylesheet too', 'never cache /api', 'do not block the first paint on the full download'.")] string requirement)
    {
        return $"""
            Configure bit Bswup so that: {requirement}

            Work in this order:

            1. Call `SearchBswup` with the request above. It searches the guide, the docs pages, every setting and
               the demo's own service-worker files at once, and every hit tells you the exact follow-up call.
            2. Call `GetBswupServiceWorkerSettings` and match the setting names, types and defaults exactly. Do not
               infer a setting from Workbox or from the standard Microsoft template - the names differ.
            3. If a `self.mode` preset covers the intent, call `GetBswupServiceWorkerModes` first: a preset is a
               bundle of settings, and it only fills in what the file has not assigned itself.
            4. Write the settings ABOVE the `self.importScripts('_content/Bit.Bswup/bit-bswup.sw.js')` line, in
               BOTH `service-worker.js` and `service-worker.published.js`.
            5. Call `InspectBswupServiceWorker` with the resulting file, then `AnalyzeBswupAssetCaching` with the
               same file and the concrete asset URLs this change is about - including one URL that must NOT be
               cached. Do not report the work as done until the analysis shows the intended answer for each.
            6. Prefer the framework's own mechanism over hand-rolling one: `serverHandledUrls` over a fetch
               listener of your own, `externalAssets` over a manual `caches.put`, `cacheVersion` over deleting
               buckets by hand.

            If this documentation site already does the same thing, fetch its configuration with
            `GetBswupSourceFile(path: "Demo/Client/wwwroot/service-worker.published.js")` and follow its shape.
            """;
    }

    [McpServerPrompt(Name = "debug-bswup", Title = "Diagnose a Bswup problem")]
    [Description("Diagnoses a bit Bswup problem - an app that never updates, a splash that never finishes, assets missing offline, a deep link that shows the wrong page.")]
    public static string DebugBswup(
        [Description("What goes wrong, with the URL, the console output and the service-worker settings involved if you know them.")] string symptom)
    {
        return $"""
            Diagnose this bit Bswup problem: {symptom}

            Work in this order:

            1. Call `GetBswupDocsPage(slug: "troubleshooting")` - it maps the symptoms people actually report back
               to their causes, and the cause is often there verbatim.
            2. Get the app's `service-worker.js` AND `service-worker.published.js` and run each through
               `InspectBswupServiceWorker`. Two failures dominate real reports and neither produces any visible
               error: a setting assigned after the `importScripts` line, and a setting present in only one of the
               two files (so it is missing in production).
            3. If an asset is missing offline, call `AnalyzeBswupAssetCaching` with that file and the asset's URL
               as it appears in `service-worker-assets.js`. It reports which pattern decided the outcome.
            4. If the app will not pick up new versions, check the HTTP layer before the configuration: a
               `service-worker.js` served with a long `Cache-Control` keeps clients on the old worker no matter
               what the app does. Then look at `updateInterval` / `updateOnVisibility` (`GetBswupScriptOptions`)
               and `BitBswup.checkForUpdate()` (`GetBswupJsApi`).
            5. Confirm the API you are relying on with `GetBswupEvents` or `GetBswupProgressUI` before concluding
               it is a bug - check the actual default of the parameter involved. `AutoReload` defaults to `false`
               since v-10-6-0, which is the cause behind most "updates no longer apply themselves" reports.
            6. Call `SearchBswup` with the symptom for anything the troubleshooting page did not cover.

            Tell me the cause and the fix, and cite the tool call that established it.
            """;
    }

    [McpServerPrompt(Name = "remove-bswup", Title = "Remove Bswup / recover stuck clients")]
    [Description("Removes bit Bswup from a deployed app, or recovers clients stuck on a broken service worker or cache, using the self-destructing cleanup worker.")]
    public static string RemoveBswup()
    {
        return """
            Remove bit Bswup from this app (or recover clients stuck on a broken worker/cache).

            Work in this order:

            1. Call `GetBswupDocsPage(slug: "cleanup")` and `GetBswupGuideSection(heading: "Backing out of Bswup (the cleanup worker)")`
               and follow them.
            2. Replace the CONTENT of both `wwwroot/service-worker.js` and `wwwroot/service-worker.published.js`
               with the single cleanup import. Deleting the files instead is the classic mistake: a client that
               already has the old worker never learns it should stop, because a 404 on the worker script leaves
               the installed one in place.
            3. Keep the `bit-bswup.js` script tag on the page until no client has loaded the old app for as long
               as your cache headers require. Every later load then just repeats a silent register/unregister
               cycle. Only afterwards remove the tag, the `BswupProgress` component and the package reference.
            4. Verify with `InspectBswupServiceWorker` on the new file: it should report the cleanup worker, not
               the engine.
            5. If you only need to reset ONE client rather than back the whole app out, that is
               `BitBswup.forceRefresh()` instead - call `GetBswupJsApi` for what it clears by default.

            Show me the diff for each file you change.
            """;
    }
}
