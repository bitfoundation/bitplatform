using ModelContextProtocol.Server;
using System.ComponentModel;
using Bit.Bswup.Demo.Client;
using Bit.Bswup.Demo.Server.Dtos;
using Bit.Bswup.Demo.Server.Services;
using Microsoft.AspNetCore.Components.Web;

namespace Bit.Bswup.Demo.Server.Controllers;

/// <summary>
/// The Bswup MCP server: the tools an AI agent calls to add offline support, install progress and
/// controlled updates to a Blazor app without guessing at the API.
/// <para>
/// Every tool answers from the shipped library or from this site's own content - the TypeScript
/// sources compiled into Bit.Bswup's JavaScript bundles, the docs pages rendered by the very site
/// that documents them, and this site's and the samples' service-worker files - so an agent gets
/// what the current version actually does rather than a snapshot someone wrote down.
/// <see cref="InspectBswupServiceWorker"/> goes further and runs an app's own service-worker file
/// through the same rules the shipped worker applies, which is the only way to answer "will this
/// configuration cache that asset?" without deploying it. The same methods are exposed as plain
/// HTTP GET endpoints under /api/mcp/..., which makes each of them inspectable from a browser -
/// and that one, whose input is a whole file, takes a POST with a JSON body at the same URL,
/// because a query string cannot carry one.
/// </para>
/// <para>
/// The surface is kept deliberately small, and every answer as narrow as the question allows. A
/// tool list is spent from every client's context window before a single question is asked, and
/// every character a tool returns is spent again on each call - so a tool whose answer another
/// tool already contains is not a convenience here, it is a tax. That is why there is no overview
/// tool restating this list, no second copy of the documentation behind a second pair of tools,
/// and why the reference tools take a <c>name</c>: an agent after one setting should not be made
/// to pay for twenty-four.
/// </para>
/// </summary>
[ApiController]
[McpServerToolType]
[Route("api/[controller]/[action]")]
public class McpController(HtmlRenderer htmlRenderer, ILogger<McpController> logger) : ControllerBase
{
    /// <summary>
    /// The most text one call hands back. The docs pages land below it; the library's TypeScript
    /// sources - the service worker alone runs past 120,000 characters - do not, which is what
    /// <c>startLine</c> on <see cref="GetBswupSourceFile"/> is for. Roughly four thousand tokens:
    /// enough for any answer worth reading in one piece, and well short of the point where a
    /// single call crowds out the conversation it was meant to inform.
    /// </summary>
    private const int MaxDocumentLength = 16_000;

    // The most asset URLs one inspection will decide on.
    private const int MaxAnalyzedAssetUrls = 200;

    [HttpGet]
    [McpServerTool(Name = nameof(SearchBswup), Title = "Search everything about Bswup",
               ReadOnly = true, Idempotent = true, Destructive = false, OpenWorld = false, UseStructuredContent = true)]
    [Description("Searches everything this server knows at once - the documentation pages, every script attribute and service-worker setting, the mode presets, the lifecycle events, the JavaScript API, the progress UI and the sample sources - and returns the best matches, each with the exact follow-up call that returns its full text. Call it first unless you already know the page, setting or event you want, then call the hit's tool verbatim: those calls are narrowed to the hit, so following one costs a fraction of the same tool called bare. Example queries: 'app never picks up new versions', 'cache an external CDN script', 'offline deep link shows home page'.")]
    public BswupSearchHitDto[] SearchBswup(
        [Description("What you are trying to do or what goes wrong, in your own words - e.g. 'cache an external CDN script'. Setting, event and attribute names work too.")] string query,
        [Description("How many hits to return. 1-50; anything outside that is clamped.")] int limit = 12)
    {
        return BswupSearchIndex.Search(query, limit);
    }

    [HttpGet]
    [McpServerTool(Name = nameof(GetBswupSetupGuide), Title = "Setup guide for a hosting model",
               ReadOnly = true, Idempotent = true, Destructive = false, OpenWorld = false)]
    [Description("Gets the complete wiring for adding bit Bswup to a Blazor app under one hosting model, as the real files of a working project: 'standalone-wasm' (wwwroot/index.html is the host document) or 'blazor-web-app' (a server-rendered App.razor hosting an InteractiveWebAssembly client). Call it before writing any setup code - where the splash markup can live, and which assets the client's manifest does NOT list, differ between the two. It is a long answer standing in for several shorter ones: do not also fetch the getting-started page for the same task.")]
    public string GetBswupSetupGuide(
        [Description("'standalone-wasm' for an app whose wwwroot/index.html is the host document, or 'blazor-web-app' for one whose server-rendered Components/App.razor hosts an InteractiveWebAssembly client.")] string hostingModel)
    {
        return BswupSetupGuide.Get(hostingModel)
            ?? $"'{hostingModel}' is not a known hosting model. Use one of: {string.Join(", ", BswupSetupGuide.HostingModels)}.";
    }

    [HttpGet]
    [McpServerTool(Name = nameof(GetBswupScriptOptions), Title = "Script tag attributes",
               ReadOnly = true, Idempotent = true, Destructive = false, OpenWorld = false, UseStructuredContent = true)]
    [Description("Lists the attributes of the bit-bswup.js script tag - scope, log, sw, handler, blazorScript, updateInterval, updateOnVisibility, stallTimeout, persistStorage, options - each with the default read off the shipped script, what it does and the caveats. Every one is written as an attribute of that tag, or as a property of the global object its 'options' attribute names; the 'script-options' documentation page has the prose. Call it before writing the script tag; pass 'name' when only one is in question.")]
    public BswupOptionDto[] GetBswupScriptOptions(
        [Description("One attribute name, e.g. 'updateInterval'. Omit for all ten; a name matching none of them returns all of them.")] string? name = null)
    {
        return Narrow(BswupScriptCatalog.ScriptOptions, name, option => option.Name);
    }

    [HttpGet]
    [McpServerTool(Name = nameof(GetBswupServiceWorkerSettings), Title = "Service worker settings",
               ReadOnly = true, Idempotent = true, Destructive = false, OpenWorld = false, UseStructuredContent = true)]
    [Description("Lists the self.* settings an app assigns in service-worker.js before importing the Bswup engine - the asset include/exclude lists, externalAssets, defaultUrl, the URL routing lists, passive mode, error tolerance, retries, diagnostics, cache versioning, mode - each with its type, default and caveats. Every one of them is assigned on self, ABOVE the importScripts line; the 'service-worker' documentation page has the prose. The built-in asset patterns and the self.mode presets ('NoPrerender', 'InitialPrerender', 'AlwaysPrerender', 'FullOffline') come with the settings they explain. Pass 'name' when only one setting is in question: the whole list is long.")]
    public BswupServiceWorkerSettingsDto GetBswupServiceWorkerSettings(
        [Description("One setting name, with or without the 'self.' prefix, e.g. 'assetsExclude'. Omit for all of them; a name matching none of them returns all of them.")] string? name = null)
    {
        var settings = Narrow(BswupScriptCatalog.WorkerSettings, StripSelf(name), setting => setting.Name);

        // The two bulky extras ride along only when the settings they explain are in the answer.
        // Asked about `errorTolerance`, nobody wants eighteen asset-matching patterns with it.
        var wantsPatterns = settings.Any(setting => setting.Name is "assetsInclude" or "assetsExclude" or "ignoreDefaultInclude" or "ignoreDefaultExclude");
        var wantsModes = settings.Any(setting => setting.Name is "mode");

        // A named record rather than an anonymous type: this is the shape the tool publishes as its
        // output schema, and an anonymous type has none to publish.
        return new BswupServiceWorkerSettingsDto
        {
            Settings = settings,
            DefaultAssetsInclude = wantsPatterns ? BswupScriptCatalog.DefaultAssetsInclude : null,
            DefaultAssetsExclude = wantsPatterns ? BswupScriptCatalog.DefaultAssetsExclude : null,
            Modes = wantsModes ? BswupScriptCatalog.Modes : null,
            Notes =
            [
                "Every setting must be assigned BEFORE `self.importScripts('_content/Bit.Bswup/bit-bswup.sw.js')` - the engine reads them as it is imported.",
                "Whatever you set here, set in service-worker.published.js as well: that is the file deployed builds ship.",
                "The URL-matching lists (assetsInclude, assetsExclude, prohibitedUrls, serverHandledUrls, serverRenderedUrls) accept a RegExp, used as written, or a string, which is regex-escaped and matched as a literal SUBSTRING of the URL.",
                "An exclude always beats an include. The default excludes keep the service-worker scripts themselves out of the cache; caching those corrupts the update cycle.",
                "A self.mode preset only fills in settings the file has not assigned itself, so an explicit assignment always wins - including an explicitly falsy one.",
                "Call InspectBswupServiceWorker with your file to have it checked, and pass it the asset URLs you care about to see which of them it will cache.",
            ]
        };
    }

    [HttpGet]
    [McpServerTool(Name = nameof(InspectBswupServiceWorker), Title = "Review a service worker file",
               ReadOnly = true, Idempotent = true, Destructive = false, OpenWorld = false, UseStructuredContent = true)]
    [Description("Checks a service-worker.js file against the shipped Bswup worker and reports what it will actually do: which self.* settings it assigns, which of those names the worker does not know (a typo it silently ignores), settings assigned after the importScripts line (where the engine can no longer see them), a missing engine import, a defaultUrl no asset serves, string entries in the URL lists, and what a mode preset adds. Pass 'assetUrls' and the same call also decides those assets, running them through the include/exclude lists this file produces, built-in patterns first - which answers 'will this be cached?' without deploying. Run it on every service-worker file you write or change: none of these failures produce an error anyone sees until a user is offline.")]
    public BswupServiceWorkerInspectionDto InspectBswupServiceWorker(
        [Description("The full content of the service-worker.js (or service-worker.published.js) file to check, verbatim.")] string script,
        [Description("Optional asset URLs to decide under this file, written as they appear in service-worker-assets.js (e.g. '_framework/blazor.boot.json', 'css/app.css') - one per line, or comma- or semicolon-separated. Pass the handful the question is about, including one that must NOT be cached; do not paste a whole manifest.")] string? assetUrls = null)
    {
        var inspection = BswupServiceWorkerInspector.Inspect(script);

        if (string.IsNullOrWhiteSpace(assetUrls)) return inspection;

        var urls = assetUrls.Split(['\n', '\r', ',', ';'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        // Every URL is run against every pattern, so an agent that pastes a whole manifest in
        // turns one call into a lot of matching. The cap sits far above the handful of assets a
        // real question is about, and the answer says out loud when it was applied - a silently
        // truncated list would read as 'these are all of them'.
        var analysis = BswupServiceWorkerInspector.AnalyzeAssets(script, urls.Take(MaxAnalyzedAssetUrls));

        if (urls.Length > MaxAnalyzedAssetUrls)
        {
            analysis = analysis with
            {
                Notes = [.. analysis.Notes, $"Only the first {MaxAnalyzedAssetUrls} of the {urls.Length} URLs passed were analyzed; ask again with the rest to cover them."]
            };
        }

        return inspection with { Assets = analysis };
    }

    /// <summary>
    /// The POST form of <see cref="InspectBswupServiceWorker"/>: same answer, with the file in the
    /// request body. The GET mirror carries the script in the query string, which a real
    /// service-worker file overruns - a request line has a length limit (8 KB by default in
    /// Kestrel) and the request is rejected before it reaches this controller.
    /// </summary>
    [HttpPost]
    [ActionName(nameof(InspectBswupServiceWorker))]
    public BswupServiceWorkerInspectionDto InspectBswupServiceWorkerFromBody([FromBody] BswupInspectRequestDto request)
    {
        return InspectBswupServiceWorker(request.Script, request.AssetUrls);
    }

    [HttpGet]
    [McpServerTool(Name = nameof(GetBswupEvents), Title = "Lifecycle events",
               ReadOnly = true, Idempotent = true, Destructive = false, OpenWorld = false, UseStructuredContent = true)]
    [Description("Lists the lifecycle messages Bswup hands to the page's handler function - downloadStarted, downloadProgress, downloadFinished, activate, firstInstallClaimed, updateReady, updateFound, updateNotFound, updateCheckFailed, stateChanged, error, updateInstalled - each with the string the constant resolves to and the shape of its data payload. Call it before writing a custom handler; pass 'name' when only one is in question.")]
    public BswupEventDto[] GetBswupEvents(
        [Description("One event name, e.g. 'updateReady'. Omit for all of them; a name matching none of them returns all of them.")] string? name = null)
    {
        return Narrow(BswupScriptCatalog.Events, name, message => message.Name);
    }

    [HttpGet]
    [McpServerTool(Name = nameof(GetBswupJsApi), Title = "JavaScript API (BitBswup)",
               ReadOnly = true, Idempotent = true, Destructive = false, OpenWorld = false, UseStructuredContent = true)]
    [Description("Gets the global BitBswup object the page script installs: checkForUpdate, persistStorage, skipWaiting, forceRefresh and version - what each resolves with and when to call it. Use it for a 'check for updates' button, a custom poller or a 'reset app' action; pass 'name' when only one member is in question.")]
    public BswupJsApiDto[] GetBswupJsApi(
        [Description("One member name, e.g. 'forceRefresh'. Omit for all five; a name matching none of them returns all of them.")] string? name = null)
    {
        return Narrow(BswupScriptCatalog.JsApi, name, member => member.Name);
    }

    [HttpGet]
    [McpServerTool(Name = nameof(GetBswupProgressUI), Title = "Built-in progress UI reference",
               ReadOnly = true, Idempotent = true, Destructive = false, OpenWorld = false, UseStructuredContent = true)]
    [Description("Gets the built-in progress UI reference: every parameter of the BswupProgress component with the default read off the shipped assembly, the element ids bit-bswup.progress.js drives (what a custom ChildContent splash has to render), the runtime config call, and the script and stylesheet the page needs. The parameters are set on the <BswupProgress /> tag in the host document; the 'progress-ui' documentation page has the prose.")]
    public BswupProgressUiDto GetBswupProgressUI()
    {
        return BswupProgressCatalog.ProgressUi;
    }

    [HttpGet]
    [McpServerTool(Name = nameof(GetBswupDocsPage), Title = "Read a documentation page",
               ReadOnly = true, Idempotent = true, Destructive = false, OpenWorld = false)]
    [Description("Gets one page of the bit Bswup documentation site as Markdown, code samples included - the prose behind the reference tools, for when a name and a default are not enough. A whole page costs several times what a named reference call does, so reach for it second. The slugs: 'introduction', 'getting-started', 'how-it-works' (who starts Blazor, how an update is staged), 'script-options', 'service-worker', 'events', 'progress-ui', 'javascript-api', 'playground', 'mcp-server', 'recipes' (hosting headers, sub-paths, cache versions, update banners, API bypass), 'troubleshooting' (symptoms mapped to causes), 'cleanup', 'migration' (v-10-6-0).")]
    public async Task<string> GetBswupDocsPage(
        [Description("One of the slugs listed in this tool's description, e.g. 'service-worker'. Omit for the introduction.")] string? slug = null)
    {
        // The introduction's own slug is the empty string; agents reach for a word instead, and
        // DocsCatalog.FindBySlug maps those words for every caller.
        var page = DocsCatalog.FindBySlug(slug);

        if (page is null)
        {
            var slugs = string.Join(", ", DocsCatalog.AllPages.Select(p => p.Slug.Length == 0 ? "introduction" : p.Slug));

            return $"No documentation page has the slug '{slug}'. Available slugs: {slugs}.";
        }

        // The page is rendered by the same component the site serves, so the documentation an agent
        // reads is the documentation a human reads - there is no second copy that could go stale.
        // The renderer caches it, so this and the bswup://docs resource render each page once between them.
        var (rendered, error) = await DocsPageRenderer.GetMarkdownAsync(htmlRenderer, page, logger);

        if (rendered is null) return DocsPageRenderer.Unavailable(page, error);

        // The page renders its own <h1>, so only its source is prepended here.
        return Truncate($"bit Bswup documentation page: {page.Url}\n\n{rendered}");
    }

    [HttpGet]
    [McpServerTool(Name = nameof(GetBswupSourceFiles), Title = "List available source files",
               ReadOnly = true, Idempotent = true, Destructive = false, OpenWorld = false, UseStructuredContent = true)]
    [Description("Lists the working Bswup source files this server can hand out: this documentation site's own host document and service-worker files, the minimal samples for both hosting models, and the library's own TypeScript (the page script, the service worker, the progress UI, the cleanup worker). Use it to pick a path for GetBswupSourceFile. These are worked examples and the shipped implementation - for what an option means, the reference tools answer in a fraction of the characters.")]
    public BswupSourceFileDto[] GetBswupSourceFiles()
    {
        return BswupSourceCatalog.SourceFiles;
    }

    [HttpGet]
    [McpServerTool(Name = nameof(GetBswupSourceFile), Title = "Read a source file",
               ReadOnly = true, Idempotent = true, Destructive = false, OpenWorld = false)]
    [Description("Gets one source file listed by GetBswupSourceFiles, verbatim - e.g. 'Demo/Client/wwwroot/service-worker.published.js' for a deployed Blazor Web App's configuration, 'Sample/BasicSample/wwwroot/index.html' for a complete hand-written splash and handler, or 'Library/Scripts/bit-bswup.sw.ts' for the engine itself. The library's TypeScript runs to tens of thousands of characters, so a long file comes back one window at a time and names the line to continue from - read a window, not a whole file, unless you truly need all of it.")]
    public string GetBswupSourceFile(
        [Description("A path from GetBswupSourceFiles, e.g. 'Demo/Client/wwwroot/service-worker.published.js' or 'Library/Scripts/bit-bswup.sw.ts'.")] string path,
        [Description("The 1-based line to start reading at. Defaults to the start of the file; a windowed answer names the line to pass here to continue.")] int startLine = 1)
    {
        var content = BswupSourceCatalog.GetSourceFile(path);

        if (content is null)
        {
            var candidates = BswupSourceCatalog.SourceFiles
                .Where(f => f.Path.Contains(path ?? string.Empty, StringComparison.OrdinalIgnoreCase))
                .Select(f => f.Path)
                .Take(10)
                .ToArray();

            return candidates.Length > 0
                ? $"No source file at '{path}'. Did you mean: {string.Join(", ", candidates)}?"
                : $"No source file at '{path}'. Call GetBswupSourceFiles for the full list.";
        }

        return Window(content, startLine, path);
    }

    /// <summary>A setting name with the <c>self.</c> callers habitually write in front of it removed.</summary>
    private static string? StripSelf(string? name)
    {
        var trimmed = name?.Trim();

        return trimmed?.StartsWith("self.", StringComparison.OrdinalIgnoreCase) is true ? trimmed["self.".Length..] : trimmed;
    }

    /// <summary>
    /// The entries whose name is <paramref name="name"/>, or all of them when nothing was asked for
    /// - or when what was asked for is not among them. An empty answer to a misspelled name would
    /// read as "this library has no such thing", which is the one conclusion that must not be drawn
    /// from a typo; the full list lets the caller see the name it meant.
    /// </summary>
    private static T[] Narrow<T>(T[] entries, string? name, Func<T, string> nameOf)
    {
        if (string.IsNullOrWhiteSpace(name)) return entries;

        var wanted = entries.Where(entry => string.Equals(nameOf(entry), name.Trim(), StringComparison.OrdinalIgnoreCase)).ToArray();

        return wanted.Length > 0 ? wanted : entries;
    }

    private static string Truncate(string text)
    {
        return text.Length <= MaxDocumentLength
            ? text
            : $"{text[..MaxDocumentLength]}\n\n[truncated - the full text is longer than {MaxDocumentLength} characters]";
    }

    /// <summary>
    /// At most <see cref="MaxDocumentLength"/> characters of <paramref name="content"/>, starting
    /// at <paramref name="startLine"/> and cut at a line boundary. A window that does not reach the
    /// end says where it stopped and how to go on, because a caller who cannot tell a partial
    /// answer from a complete one reads the missing half as "not there".
    /// </summary>
    private static string Window(string content, int startLine, string path)
    {
        if (startLine <= 1 && content.Length <= MaxDocumentLength) return content;

        var lines = content.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n');
        var first = Math.Clamp(startLine, 1, lines.Length);

        var taken = new List<string>();
        var length = 0;

        for (var i = first - 1; i < lines.Length; i++)
        {
            // The first line is always taken, however long it is: a minified file is one line, and
            // a window that could hold none of it would answer every call with an empty string.
            if (taken.Count > 0 && length + lines[i].Length + 1 > MaxDocumentLength) break;

            taken.Add(lines[i]);
            length += lines[i].Length + 1;
        }

        var last = first + taken.Count - 1;
        var body = string.Join('\n', taken);

        if (first == 1 && last == lines.Length) return body;

        var more = last < lines.Length
            ? $"continue with GetBswupSourceFile(path: \"{path}\", startLine: {last + 1})"
            : "this is the end of the file";

        return $"[lines {first}-{last} of {lines.Length} - {more}]\n\n{body}";
    }
}
