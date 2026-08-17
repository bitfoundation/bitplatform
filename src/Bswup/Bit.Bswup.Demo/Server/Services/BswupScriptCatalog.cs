using System.Collections.Frozen;
using Bit.Bswup.Demo.Server.Dtos;
using System.Text.RegularExpressions;

namespace Bit.Bswup.Demo.Server.Services;

/// <summary>
/// Builds the reference for everything Bswup exposes to a page - the script-tag attributes, the
/// service-worker settings, the mode presets, the lifecycle messages and the JavaScript API - by
/// reading the library's own TypeScript sources, which are embedded in this assembly.
/// <para>
/// A C# library can be reflected over; Bswup ships as JavaScript, so its sources are the
/// equivalent surface. Every name, default value and message string below therefore comes out of
/// the code that is actually shipped, and the hand-written prose only supplies what code cannot:
/// what the option is for. The README already demonstrates why this matters - it documents the
/// default asset includes as <c>/\.wasm/</c> while the shipped worker uses
/// <c>/\.wasm(\.br|\.gz)?$/</c> - so a tool answering from prose alone would hand an agent a
/// pattern that no longer exists.
/// </para>
/// <para>
/// Anything the parse cannot confirm is reported with <c>VerifiedFromSource: false</c> rather than
/// dropped or silently presented as fact.
/// </para>
/// </summary>
public static partial class BswupScriptCatalog
{
    private const string PageScriptPath = "Library/Scripts/bit-bswup.ts";
    private const string WorkerScriptPath = "Library/Scripts/bit-bswup.sw.ts";

    private static readonly Lazy<string> _pageScript = new(() => BswupSourceCatalog.GetSourceFile(PageScriptPath) ?? string.Empty);
    private static readonly Lazy<string> _workerScript = new(() => BswupSourceCatalog.GetSourceFile(WorkerScriptPath) ?? string.Empty);

    private static readonly Lazy<string> _version = new(ReadVersion);
    private static readonly Lazy<BswupOptionDto[]> _scriptOptions = new(BuildScriptOptions);
    private static readonly Lazy<BswupOptionDto[]> _workerSettings = new(BuildWorkerSettings);
    private static readonly Lazy<BswupModeDto[]> _modes = new(BuildModes);
    private static readonly Lazy<BswupEventDto[]> _events = new(BuildEvents);
    private static readonly Lazy<BswupJsApiDto[]> _jsApi = new(BuildJsApi);
    private static readonly Lazy<string[]> _defaultInclude = new(() => ReadPatternArray(_workerScript.Value, "DEFAULT_ASSETS_INCLUDE"));
    private static readonly Lazy<string[]> _defaultExclude = new(() => ReadPatternArray(_workerScript.Value, "DEFAULT_ASSETS_EXCLUDE"));
    private static readonly Lazy<FrozenSet<string>> _settingNames = new(() =>
        _workerSettings.Value.Select(setting => setting.Name).ToFrozenSet(StringComparer.Ordinal));

    /// <summary>The version string the shipped scripts stamp onto the page and the worker.</summary>
    public static string Version => _version.Value;

    /// <summary>Every attribute of the <c>bit-bswup.js</c> script tag.</summary>
    public static BswupOptionDto[] ScriptOptions => _scriptOptions.Value;

    /// <summary>Every <c>self.*</c> setting the service-worker file can assign before importing the engine.</summary>
    public static BswupOptionDto[] WorkerSettings => _workerSettings.Value;

    /// <summary>The <c>self.mode</c> presets, with the settings each of them fills in.</summary>
    public static BswupModeDto[] Modes => _modes.Value;

    /// <summary>The lifecycle messages handed to the page's handler function.</summary>
    public static BswupEventDto[] Events => _events.Value;

    /// <summary>The members of the global <c>BitBswup</c> object.</summary>
    public static BswupJsApiDto[] JsApi => _jsApi.Value;

    /// <summary>The built-in asset include patterns, as the shipped worker declares them.</summary>
    public static string[] DefaultAssetsInclude => _defaultInclude.Value;

    /// <summary>The built-in asset exclude patterns, as the shipped worker declares them.</summary>
    public static string[] DefaultAssetsExclude => _defaultExclude.Value;

    /// <summary>Whether the shipped worker knows a setting by this name (the check behind the typo warnings).</summary>
    public static bool IsKnownSetting(string name) => _settingNames.Value.Contains(name);

    /// <summary>The one-line summary of a service-worker setting, or null when it is not one.</summary>
    public static string? GetSettingSummary(string name)
        => _workerSettings.Value.FirstOrDefault(setting => setting.Name == name)?.Summary;

    private static string ReadVersion()
    {
        var match = VersionRegex().Match(_pageScript.Value);

        return match.Success ? match.Groups["version"].Value : "unknown";
    }

    private static BswupOptionDto[] BuildScriptOptions()
    {
        var script = JavaScriptSource.StripComments(_pageScript.Value);

        // The attributes the script actually reads. Anything documented but missing here would be
        // an attribute that stopped being honored - which is exactly what has to be visible.
        var attributes = AttributeReadRegex().Matches(script)
                                             .Select(match => match.Groups["name"].Value)
                                             .ToHashSet(StringComparer.Ordinal);

        // The literal defaults, read off the `defaultoptions` object the script merges over.
        var defaults = new Dictionary<string, string>(StringComparer.Ordinal);
        var body = JavaScriptSource.ReadObjectLiteral(script, "const defaultoptions =");
        if (body is not null)
        {
            foreach (var (key, value) in JavaScriptSource.ReadObjectEntries(body))
            {
                // `blazorScript: defaultBlazorScripts` is a reference to a list declared elsewhere;
                // naming the variable would be a worse answer than the prose below, which spells
                // the two scripts out.
                if (TryReadLiteral(value, out var literal)) defaults[key] = literal;
            }
        }

        return
        [
            .. _scriptOptionDocs.Select(doc => new BswupOptionDto
            {
                Name = doc.Name,
                Kind = "Script attribute",
                SetIn = "<script src=\"_content/Bit.Bswup/bit-bswup.js\"> attribute (or a property of the global object named by the `options` attribute)",
                Type = doc.Type,
                // The parsed default wins: it is what the shipped script will actually apply.
                Default = defaults.GetValueOrDefault(doc.DefaultKey ?? doc.Name) ?? doc.Default,
                Summary = doc.Summary,
                Remarks = doc.Remarks,
                VerifiedFromSource = attributes.Contains(doc.Name),
                Docs = "GetBswupDocsPage(slug: \"script-options\")"
            })
        ];
    }

    private static BswupOptionDto[] BuildWorkerSettings()
    {
        var worker = _workerScript.Value;

        // The settings and their one-line summaries come from the worker's own configuration
        // interface, where each knob is declared next to a comment describing it.
        var declarations = SettingDeclarationRegex().Matches(InterfaceBody(worker))
            .Select(match => (Name: match.Groups["name"].Value, Summary: match.Groups["summary"].Value.Trim()))
            .Where(setting => _nonSettings.Contains(setting.Name) is false)
            .ToArray();

        var defaults = ReadWorkerDefaults(worker);

        var settings = declarations.Select(setting => new BswupOptionDto
        {
            Name = setting.Name,
            Kind = "Service worker setting",
            SetIn = "service-worker.js, assigned on `self` BEFORE the importScripts line",
            Type = _settingTypes.GetValueOrDefault(setting.Name),
            Default = defaults.GetValueOrDefault(setting.Name) ?? _settingDefaults.GetValueOrDefault(setting.Name),
            Summary = setting.Summary.Length > 0 ? setting.Summary : null,
            Remarks = _settingRemarks.GetValueOrDefault(setting.Name),
            VerifiedFromSource = true,
            Docs = "GetBswupDocsPage(slug: \"service-worker\")"
        }).ToList();

        // A setting the interface no longer declares is still worth answering for: an app may have
        // it in its file today, and "this no longer exists" is the useful answer.
        foreach (var (name, remarks) in _settingRemarks)
        {
            if (settings.Any(setting => setting.Name == name)) continue;

            settings.Add(new BswupOptionDto
            {
                Name = name,
                Kind = "Service worker setting",
                SetIn = "service-worker.js, assigned on `self` BEFORE the importScripts line",
                Summary = "This setting is documented here but the shipped service worker no longer declares it - treat it as removed and verify against GetBswupSourceFile(path: \"Library/Scripts/bit-bswup.sw.ts\").",
                Remarks = remarks,
                VerifiedFromSource = false,
                Docs = "GetBswupDocsPage(slug: \"service-worker\")"
            });
        }

        return [.. settings];
    }

    /// <summary>
    /// The body of the worker's configuration interface. Scoping the declaration scan to it keeps
    /// the other ambient interfaces in the file (and every `name: any` inside them) out.
    /// </summary>
    private static string InterfaceBody(string worker)
    {
        var start = worker.IndexOf("interface BitBswupGlobals", StringComparison.Ordinal);
        if (start < 0) return string.Empty;

        var open = worker.IndexOf('{', start);
        var close = open < 0 ? -1 : worker.IndexOf("\n}", open, StringComparison.Ordinal);

        return open < 0 || close < 0 ? string.Empty : worker[open..close];
    }

    /// <summary>
    /// The defaults the worker applies to a setting the app leaves unset. Each of the three shapes
    /// below is the worker's own way of saying "when this is not set, use that".
    /// </summary>
    private static Dictionary<string, string> ReadWorkerDefaults(string worker)
    {
        var defaults = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (Match match in TypeofDefaultRegex().Matches(worker))
        {
            defaults[match.Groups["name"].Value] = Literal(match.Groups["value"].Value);
        }

        foreach (Match match in NormalizedDefaultRegex().Matches(worker))
        {
            defaults[match.Groups["name"].Value] = Literal(match.Groups["value"].Value);
        }

        foreach (Match match in OrAssignDefaultRegex().Matches(worker))
        {
            defaults[match.Groups["name"].Value] = Literal(match.Groups["value"].Value);
        }

        return defaults;
    }

    private static BswupModeDto[] BuildModes()
    {
        var worker = JavaScriptSource.StripComments(_workerScript.Value);

        var start = worker.IndexOf("switch (self.mode)", StringComparison.Ordinal);
        if (start < 0) return [];

        var body = worker[start..];
        var end = body.IndexOf("\n}", StringComparison.Ordinal);
        if (end > 0) body = body[..end];

        var modes = new List<BswupModeDto>();

        foreach (var block in body.Split("break;", StringSplitOptions.None))
        {
            var names = ModeCaseRegex().Matches(block).Select(match => match.Groups["name"].Value).ToArray();
            if (names.Length == 0) continue;

            var settings = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (Match match in PresetDefaultRegex().Matches(block))
            {
                settings[match.Groups["name"].Value] = Literal(match.Groups["value"].Value);
            }

            // Cases that fall through share one settings block, and each of them is a value an
            // app can write, so each gets its own entry rather than one entry naming several.
            foreach (var name in names)
            {
                modes.Add(new BswupModeDto
                {
                    Name = name,
                    Settings = new Dictionary<string, string>(settings, StringComparer.Ordinal),
                    Note = "A preset only fills settings the service-worker file has not assigned itself, so any explicit assignment wins - including an explicitly falsy one such as `self.caseInsensitiveUrl = false`."
                });
            }
        }

        return [.. modes];
    }

    private static BswupEventDto[] BuildEvents()
    {
        var script = JavaScriptSource.StripComments(_pageScript.Value);
        var body = JavaScriptSource.ReadObjectLiteral(script, "var BswupMessage");

        var messages = body is null
            ? []
            : JavaScriptSource.ReadObjectEntries(body).Select(entry => (entry.Key, Message: Literal(entry.Value))).ToArray();

        var events = messages.Select(message =>
        {
            var doc = _eventDocs.FirstOrDefault(e => e.Name == message.Key);

            return new BswupEventDto
            {
                Name = message.Key,
                Message = message.Message,
                Payload = doc?.Payload,
                Summary = doc?.Summary ?? "A message the shipped script declares but this server has no description for - read the handler sample with GetBswupDocsPage(slug: \"events\").",
                Deprecated = doc?.Deprecated,
                VerifiedFromSource = true
            };
        }).ToList();

        // A documented message the shipped script no longer declares would leave handlers matching
        // on a constant that resolves to undefined; say so instead of quietly dropping it.
        foreach (var doc in _eventDocs)
        {
            if (events.Any(e => e.Name == doc.Name)) continue;

            events.Add(new BswupEventDto
            {
                Name = doc.Name,
                Message = "(not declared by the shipped script)",
                Payload = doc.Payload,
                Summary = doc.Summary,
                Deprecated = doc.Deprecated,
                VerifiedFromSource = false
            });
        }

        return [.. events];
    }

    private static BswupJsApiDto[] BuildJsApi()
    {
        var script = JavaScriptSource.StripComments(_pageScript.Value);

        // Presence, not values: each of these is assigned a function expression, and what matters is
        // whether the shipped script still installs a member by that name. A plain scan finds every
        // assignment, including the ones nested inside the setup closure.
        var members = ApiMemberRegex().Matches(script)
                                      .Select(match => match.Groups["name"].Value)
                                      .ToHashSet(StringComparer.Ordinal);

        return
        [
            .. _jsApiDocs.Select(doc => new BswupJsApiDto
            {
                Name = doc.Name,
                Signature = doc.Signature,
                Returns = doc.Returns,
                Summary = doc.Summary,
                VerifiedFromSource = members.Contains(doc.Name)
            })
        ];
    }

    /// <summary>
    /// The regular-expression and string literals of a named array in the shipped worker - the two
    /// default asset lists, kept exactly as written so a pattern can be pasted back into a file.
    /// </summary>
    private static string[] ReadPatternArray(string worker, string name)
    {
        var code = JavaScriptSource.StripComments(worker);

        var start = code.IndexOf($"const {name} =", StringComparison.Ordinal);
        if (start < 0) return [];

        var open = code.IndexOf('[', start);
        if (open < 0) return [];

        var depth = 0;
        var end = -1;
        for (int i = open; i < code.Length; i++)
        {
            if (code[i] == '[') depth++;
            else if (code[i] == ']' && --depth == 0) { end = i; break; }
        }

        if (end < 0) return [];

        return [.. JavaScriptSource.ReadLiterals(code[(open + 1)..end])];
    }

    /// <summary>The value of a JavaScript literal without its quotes; anything else verbatim.</summary>
    private static string Literal(string value)
    {
        value = value.Trim().TrimEnd(',');

        if (value.Length >= 2 && (value[0] == '\'' || value[0] == '"') && value[^1] == value[0])
        {
            return value[1..^1];
        }

        return value;
    }

    /// <summary>
    /// The value, when it is a literal a reader can act on - a string, a number or a boolean.
    /// An expression (a variable, a call) is not a default anyone can copy into their own file.
    /// </summary>
    private static bool TryReadLiteral(string value, out string literal)
    {
        literal = Literal(value);

        return literal != value.Trim().TrimEnd(',')                     // it was quoted
            || literal is "true" or "false" or "null"
            || (literal.Length > 0 && literal.All(c => char.IsDigit(c) || c is '.' or '-'));
    }

    private record ScriptOptionDoc(string Name, string Type, string? Default, string Summary, string? Remarks = null, string? DefaultKey = null);

    // The prose the sources cannot supply. Names and defaults are NOT taken from here - they are
    // read off the shipped script above; this only says what each option is for.
    private static readonly ScriptOptionDoc[] _scriptOptionDocs =
    [
        new("scope", "string", "/",
            "The service-worker scope, which is also what namespaces the cache buckets (`bit-bswup:<scope-path> - <version>`).",
            "A worker only controls URLs beneath its own folder unless the server sends a Service-Worker-Allowed header, so an app mounted on a sub-path must set this to that sub-path. A scope the browser refuses is retried with the default scope (the folder holding the worker script) so offline support is not lost outright."),

        new("log", "'none' | 'error' | 'warn' | 'info' | 'verbose' | 'debug'", "warn",
            "The log level of the Bswup page logger. Each level includes the ones above it.",
            "Use 'none' to silence all output. This only affects the page script; the worker's own logging is controlled by the enableDiagnostics / enableFetchDiagnostics service-worker settings."),

        new("sw", "string", "service-worker.js",
            "The path of the app's service-worker file - the one that assigns the self.* settings and imports the Bswup engine."),

        new("handler", "string", "bitBswupHandler",
            "The name of the global function that receives every lifecycle message.",
            "The default is also the name bit-bswup.progress.js registers, so the built-in UI wires up with no configuration. The handler is re-resolved until found, so it may be registered after bit-bswup.js loads; if none is ever registered a first install still completes on its own, while updates stay staged until the next full restart.",
            DefaultKey: "handlerName"),

        new("blazorScript", "string", "auto-detected: _framework/blazor.web.js and _framework/blazor.webassembly.js",
            "The path of the Blazor entry-point script - the one carrying autostart=\"false\".",
            "Only needed for a non-default path: both default entry scripts are auto-detected, and matching tolerates the fingerprinted names .NET 9+ emits for @Assets[\"...\"] / ImportMap references."),

        new("updateInterval", "number (seconds)", "0 (disabled)",
            "How often Bswup calls reg.update() on a timer, in seconds.",
            "Browsers only re-check a worker on navigation and roughly every 24 hours, so a long-lived tab can run a stale version for a long time. Checks are skipped while the tab is in the background and resume when it becomes visible."),

        new("updateOnVisibility", "boolean", "false",
            "Check for an update every time the tab returns to the foreground (the visibilitychange event)."),

        new("stallTimeout", "number (seconds)", "60",
            "Seconds of complete service-worker silence after which a FIRST INSTALL gives up waiting and starts Blazor from the network.",
            "The last line of defense against an install that fails silently - most notably the browser killing it mid-install (Chromium caps installs at ~5 minutes) - which would otherwise leave the app frozen behind the splash. Every progress message resets the timer, so a slow-but-healthy download never trips it. Set 0 to disable. Updates are unaffected: the app is already running."),

        new("persistStorage", "boolean", "false",
            "Ask the browser for eviction-resistant storage (navigator.storage.persist()) at startup.",
            "Off by default because the request can prompt (Firefox) and grant odds are engagement-based elsewhere; calling BitBswup.persistStorage() from a user gesture has better odds. Without it the caches are best-effort: browsers reclaim them under disk pressure, and Safari erases all storage for a site untouched for seven days."),

        new("options", "string", "bitBswup",
            "The name of a global object to read every option above from, e.g. window.bitBswup = { sw: 'my-sw.js' }.",
            "The object is merged over the built-in defaults first and a matching script-tag attribute then overrides it. This is how to configure Bswup when the script is injected dynamically, where attributes may not be readable."),
    ];

    // Members of the worker's configuration interface that an app never assigns.
    private static readonly HashSet<string> _nonSettings = new(StringComparer.Ordinal)
    {
        "clients", "skipWaiting", "registration", "assetsManifest"
    };

    private static readonly Dictionary<string, string> _settingTypes = new(StringComparer.Ordinal)
    {
        ["assetsInclude"] = "(RegExp | string)[]",
        ["assetsExclude"] = "(RegExp | string)[]",
        ["externalAssets"] = "(string | RegExp | { url: string | RegExp, hash?: string })[]",
        ["defaultUrl"] = "string",
        ["assetsUrl"] = "string",
        ["prohibitedUrls"] = "(RegExp | string)[]",
        ["caseInsensitiveUrl"] = "boolean",
        ["serverHandledUrls"] = "(RegExp | string)[]",
        ["serverRenderedUrls"] = "(RegExp | string)[]",
        ["noPrerenderQuery"] = "string",
        ["ignoreDefaultInclude"] = "boolean",
        ["ignoreDefaultExclude"] = "boolean",
        ["isPassive"] = "boolean",
        ["enableIntegrityCheck"] = "boolean",
        ["errorTolerance"] = "'lax' | 'strict'",
        ["maxRetries"] = "number",
        ["retryDelay"] = "number (ms)",
        ["enableDiagnostics"] = "boolean",
        ["enableFetchDiagnostics"] = "boolean",
        ["disableHashlessAssetsUpdate"] = "boolean",
        ["forcePrerender"] = "boolean",
        ["enableCacheControl"] = "boolean",
        ["cacheVersion"] = "string",
        ["mode"] = "'NoPrerender' | 'InitialPrerender' | 'AlwaysPrerender' | 'FullOffline'",
    };

    // Only for settings whose default the worker does not state in one of the three readable
    // shapes; everything else is answered from the shipped code.
    private static readonly Dictionary<string, string> _settingDefaults = new(StringComparer.Ordinal)
    {
        ["assetsInclude"] = "[] (added to the built-in include list)",
        ["assetsExclude"] = "[] (added to the built-in exclude list)",
        ["externalAssets"] = "[]",
        ["prohibitedUrls"] = "[]",
        ["serverHandledUrls"] = "[]",
        ["serverRenderedUrls"] = "[]",
        ["caseInsensitiveUrl"] = "false",
        ["noPrerenderQuery"] = "'' (nothing appended)",
        ["ignoreDefaultInclude"] = "false",
        ["ignoreDefaultExclude"] = "false",
        ["isPassive"] = "false",
        ["enableIntegrityCheck"] = "false",
        ["enableDiagnostics"] = "false",
        ["enableFetchDiagnostics"] = "false",
        ["disableHashlessAssetsUpdate"] = "false",
        ["forcePrerender"] = "false",
        ["enableCacheControl"] = "false",
        ["cacheVersion"] = "the Blazor asset manifest version (rotates whenever an asset hash changes)",
        ["mode"] = "unset (no preset)",
    };

    private static readonly Dictionary<string, string> _settingRemarks = new(StringComparer.Ordinal)
    {
        ["assetsInclude"] = "Added to the built-in include list unless ignoreDefaultInclude is set. A RegExp is used as written; a STRING is regex-escaped and matched as a literal substring of the URL (releases before v-10-6-0 ignored string entries entirely).",
        ["assetsExclude"] = "An exclude always beats an include. Service-worker scripts are excluded by default - caching them corrupts the update cycle.",
        ["externalAssets"] = "For assets service-worker-assets.js cannot list (the '/' app shell of a _Host.cshtml app, a host-owned blazor.web.js, a CDN script). A RegExp entry caches server-generated names lazily and keeps the newest three generations per pattern. Cross-origin entries fall back to a no-cors opaque response when the host sends no CORS headers - unless integrity checking is on, which an opaque body cannot satisfy.",
        ["defaultUrl"] = "Must match an entry that actually exists in service-worker-assets.js or externalAssets; the comparison uses resolved URLs. When nothing matches, offline navigation silently stops working and the worker logs a 'defaultUrl ... matches no asset' warning.",
        ["assetsUrl"] = "Resolved relative to the service-worker script's own location, which is where Blazor publishes the file - so a sub-path app needs no configuration. A leading '/' makes it origin-absolute.",
        ["prohibitedUrls"] = "Matches are answered with 403 Forbidden for every method (405 before v-10-6-0). A client-side convenience, NOT a security boundary: it is bypassed on any uncontrolled page (first visit, hard reload) and by anything talking to the server directly.",
        ["caseInsensitiveUrl"] = "Applies to asset cache matching AND to every URL-matching list, which are then compiled with the 'i' flag. Patterns that already carry 'i' are left alone.",
        ["serverHandledUrls"] = "URLs the worker never touches - /api, /swagger and friends. Keeping the API out of the worker is what stops a cached response from outliving its data.",
        ["serverRenderedUrls"] = "Navigations that must reach the server instead of being answered with the cached app shell.",
        ["noPrerenderQuery"] = "Appended to the default-document request so the cached app shell is the NON-prerendered one; without it the shell is the prerendered home page and every offline deep link flashes home content first. The host page has to read this query back and switch prerendering off - see Demo/Server/Components/App.razor.",
        ["isPassive"] = "Passive mode does not skip the download: after Blazor starts, a first install still tops the cache up in the background, so the app ends up fully offline-capable. What it buys is a first paint that is never blocked behind a full precache.",
        ["enableIntegrityCheck"] = "Requires byte-identical serving; it also disables the opaque no-cors fallback for cross-origin external assets, since an opaque body cannot be verified.",
        ["errorTolerance"] = "'lax' (the default) never fails an install on an asset error - failures are reported with fatal:false and lazy-filled on first use. 'strict' mirrors the Microsoft template / Workbox: any failure rejects the install, the partial cache is discarded and the previous worker keeps serving. On a first install there is nothing to fall back to, so Bswup starts the app from the network and retries on the next load.",
        ["maxRetries"] = "Additional attempts after the first, for transient failures only (rejected fetch, 408/429/5xx). 404/403 and integrity mismatches are never retried - identical bytes would fail identically.",
        ["retryDelay"] = "Attempt n waits retryDelay * 2^(n-1) plus jitter, so a mass failure does not re-hit the origin in one synchronized burst.",
        ["cacheVersion"] = "Only the cache bucket name is affected; per-asset cache busting (?v=) and integrity still use each asset's own hash. Feed it a build-stamped value (commit SHA, build timestamp) so it bumps once per publish instead of on every noisy rebuild.",
        ["mode"] = "A preset bundle of isPassive, defaultUrl, forcePrerender, errorTolerance, caseInsensitiveUrl and noPrerenderQuery. It only fills settings the file has not assigned itself. Call GetBswupServiceWorkerModes for what each one expands to.",
    };

    private record EventDoc(string Name, string? Payload = null, string? Summary = null, string? Deprecated = null);

    private static readonly EventDoc[] _eventDocs =
    [
        new("downloadStarted", "{ version, firstInstall }",
            "The worker began downloading assets. firstInstall says whether this is the initial install (the only case that owns the screen) or a background update."),
        new("downloadProgress", "{ percent (0-100), index (1-based), asset: { url, reqUrl, hash }, firstInstall }",
            "One asset finished. This is what drives a progress bar; percent is already a percentage, not a fraction."),
        new("downloadFinished", "{ reload(), cleanup(), firstInstall }",
            "Every asset has been handled. reload() activates the staged version: on a first install it claims the page and starts Blazor with no reload, on an update it skips waiting and reloads. cleanup() optionally prunes this app's stale cache buckets right away - most apps never need it."),
        new("activate", "{ version }",
            "The new version's worker activated."),
        new("firstInstallClaimed", null,
            "The first-install claim handshake completed end-to-end, immediately before Blazor is started. A pending reload() promise after this means 'the app is booting', not 'the handshake stalled'. Update flows never raise it."),
        new("updateReady", "{ reload() }",
            "A new version has finished staging and is waiting to activate. With AutoReload off this is what surfaces an update; call reload() when the user accepts it."),
        new("updateFound", "the browser's updatefound event",
            "The browser found a new worker and started installing it."),
        new("updateNotFound", null,
            "An update check completed and the app is already on the latest version - stop the spinner, show an 'up to date' message."),
        new("updateCheckFailed", "{ reason, message, reload() }",
            "An update check failed for a transient reason (offline, a server hiccup, a throttled background tab). Deliberately not the install-path error message, so the built-in UI does not hide the app; the app keeps running on the current version."),
        new("stateChanged", "the statechange event (data.currentTarget.state)",
            "The installing/waiting worker changed state."),
        new("error", "{ reason, message, fatal, firstInstall, url?, hash?, reload() }",
            "A structured install failure. reason is one of manifest | integrity | fetch | cache | request | install-incomplete | install-aborted | install-infra. fatal:false (the default lax tolerance) means one asset was skipped and will be fetched from the network on first use - a warning, not a dead app. fatal:true means no usable staged version reached this page."),
        new("updateInstalled", null,
            "Never raised by any version of Bswup.",
            Deprecated: "Declared but never emitted and never documented; it is kept only so an existing `case BswupMessage.updateInstalled:` keeps resolving. Use updateReady instead."),
    ];

    private record JsApiDoc(string Name, string Signature, string? Returns, string Summary);

    private static readonly JsApiDoc[] _jsApiDocs =
    [
        new("checkForUpdate", "BitBswup.checkForUpdate()", "Promise<void>",
            "Re-fetches the service-worker script and checks for a new version. A found update flows through updateFound/updateReady, 'nothing new' through updateNotFound, and a transient failure through updateCheckFailed rather than error. Registration-aware and safe to call as often as you like - it is what the built-in polling uses."),
        new("persistStorage", "BitBswup.persistStorage()", "Promise<boolean>",
            "Requests durable, eviction-resistant storage for the origin and resolves with whether storage is now persistent. Best odds from a user gesture (after login, from an 'install app' button). Safe to call repeatedly; unsupported browsers resolve false."),
        new("skipWaiting", "BitBswup.skipWaiting()", "Promise<boolean>",
            "Activates an update that has finished downloading and is waiting - the same thing the reload callback of updateReady/downloadFinished does. Resolves false when there was no waiting worker. Activating a first install this way completes the seamless claim-and-start flow instead of reloading."),
        new("forceRefresh", "BitBswup.forceRefresh(cacheFilter?)", "Promise<void>",
            "Last-resort reset: clears caches, unregisters the worker controlling this page and reloads. By default it clears only this app's own scoped buckets, legacy scope-less buckets and blazor-resources - a sibling app's caches and app-owned data caches are spared. cacheFilter takes a string (prefix), a RegExp or a predicate; pass () => true to wipe every cache on the origin."),
        new("version", "BitBswup.version", "string",
            "The version of the loaded page script, also published as window['bit-bswup version']."),
    ];

    // Greedy on purpose: the assignment chains through window['bit-bswup version'], so the LAST
    // quoted value on the line is the version and the first one is the key it is also published under.
    [GeneratedRegex(@"BitBswup\.version\s*=[^;]*'(?<version>[^']+)'")]
    private static partial Regex VersionRegex();

    [GeneratedRegex(@"attrs\['(?<name>\w+)'\]")]
    private static partial Regex AttributeReadRegex();

    [GeneratedRegex(@"\bBitBswup\.(?<name>\w+)\s*=(?!=)")]
    private static partial Regex ApiMemberRegex();

    [GeneratedRegex(@"^\s*(?<name>\w+):\s*any\s*(//\s*(?<summary>.*?))?\s*$", RegexOptions.Multiline)]
    private static partial Regex SettingDeclarationRegex();

    [GeneratedRegex(@"typeof self\.(?<name>\w+) === 'string'\)?\s*\?\s*self\.\w+\s*:\s*(?<value>'[^']*')")]
    private static partial Regex TypeofDefaultRegex();

    [GeneratedRegex(@"normalizeNonNegativeInt\(self\.(?<name>\w+),\s*(?<value>[^)]+)\)")]
    private static partial Regex NormalizedDefaultRegex();

    [GeneratedRegex(@"self\.(?<name>\w+)\s*\|\|=\s*(?<value>[^;]+);")]
    private static partial Regex OrAssignDefaultRegex();

    [GeneratedRegex(@"case '(?<name>\w+)':")]
    private static partial Regex ModeCaseRegex();

    [GeneratedRegex(@"presetDefault\('(?<name>\w+)',\s*(?<value>[^)]+)\)")]
    private static partial Regex PresetDefaultRegex();
}
