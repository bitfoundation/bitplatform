using Bit.Bswup.Demo.Server.Dtos;
using System.Text.RegularExpressions;

namespace Bit.Bswup.Demo.Server.Services;

/// <summary>
/// Reads an app's own <c>service-worker.js</c> the way the shipped worker does, and reports what
/// that configuration will actually do.
/// <para>
/// A misconfigured service worker does not fail loudly: it caches the wrong document, or silently
/// stops caching a whole file type, or - the classic one - assigns its settings after the
/// <c>importScripts</c> line, where the engine has already read them and moved on. None of that
/// produces an error anyone sees until an offline user does. Checking the file against the
/// settings the shipped worker actually declares, and running the real include/exclude lists over
/// concrete asset URLs, turns those into findings before they ship.
/// </para>
/// </summary>
public static partial class BswupServiceWorkerInspector
{
    private const string Engine = "bit-bswup.sw.js";
    private const string CleanupEngine = "bit-bswup.sw-cleanup.js";

    /// <summary>The URL-matching lists, whose entries carry the string-vs-RegExp semantics worth calling out.</summary>
    private static readonly string[] _urlLists =
        ["assetsInclude", "assetsExclude", "prohibitedUrls", "serverHandledUrls", "serverRenderedUrls"];

    public static BswupServiceWorkerInspectionDto Inspect(string? script)
    {
        var code = JavaScriptSource.StripComments(script ?? string.Empty);

        var problems = new List<string>();
        var warnings = new List<string>();
        var notes = new List<string>();

        var import = FindImport(code, Engine);
        var cleanupImport = FindImport(code, CleanupEngine);

        var assignments = JavaScriptSource.ReadAssignments(code, "self")
                                          // importScripts is a call, not a setting.
                                          .Where(assignment => assignment.Name != "importScripts")
                                          .ToArray();

        var settings = assignments.Select(assignment => new BswupSettingAssignmentDto
        {
            Name = assignment.Name,
            Value = Collapse(assignment.Value),
            Recognized = BswupScriptCatalog.IsKnownSetting(assignment.Name),
            AfterImport = import.Index >= 0 && assignment.Index > import.Index,
            Summary = BswupScriptCatalog.GetSettingSummary(assignment.Name)
        }).ToArray();

        var values = settings.GroupBy(setting => setting.Name, StringComparer.Ordinal)
                             .ToDictionary(group => group.Key, group => group.Last().Value, StringComparer.Ordinal);

        if (cleanupImport.Index >= 0)
        {
            notes.Add($"This is the CLEANUP worker ({CleanupEngine}), not the Bswup engine: it activates immediately, purges this app's Bswup and Blazor caches, unregisters itself and signals open tabs to detach. Every self.* setting in the file is irrelevant while it is deployed.");
        }
        else if (import.Index < 0)
        {
            problems.Add($"The file never imports the Bswup engine. Add `self.importScripts('_content/Bit.Bswup/{Engine}');` as the LAST line - it is the only mandatory line in this file.");
        }

        foreach (var setting in settings)
        {
            if (setting.Recognized is false)
            {
                var candidates = BswupScriptCatalog.WorkerSettings
                    .Where(known => known.Name.Contains(setting.Name, StringComparison.OrdinalIgnoreCase) ||
                                    setting.Name.Contains(known.Name, StringComparison.OrdinalIgnoreCase))
                    .Select(known => known.Name)
                    .Take(3)
                    .ToArray();

                problems.Add($"`self.{setting.Name}` is not a setting the shipped service worker reads - it is set and then ignored." +
                             (candidates.Length > 0 ? $" Did you mean: {string.Join(", ", candidates)}?" : " Call GetBswupServiceWorkerSettings for the full list."));
            }

            if (setting.AfterImport)
            {
                problems.Add($"`self.{setting.Name}` is assigned AFTER the importScripts line. The engine reads every setting while it is being imported, so this assignment has no effect - move it above the import.");
            }
        }

        foreach (var duplicate in settings.GroupBy(setting => setting.Name, StringComparer.Ordinal).Where(group => group.Count() > 1))
        {
            warnings.Add($"`self.{duplicate.Key}` is assigned {duplicate.Count()} times; only the last assignment before the import counts.");
        }

        InspectMode(values, problems, notes);
        InspectDefaultUrl(values, Effective(values), warnings, notes);
        InspectLists(settings, warnings, notes);
        InspectFlags(values, warnings, notes);

        return new BswupServiceWorkerInspectionDto
        {
            ImportsBswup = import.Index >= 0,
            Import = import.Text,
            Settings = settings,
            Problems = [.. problems],
            Warnings = [.. warnings],
            Notes = [.. notes]
        };
    }

    /// <summary>
    /// Runs asset URLs through the include/exclude lists the given service-worker file produces -
    /// the built-in lists first, then the file's own - and reports what the worker will manage.
    /// </summary>
    public static BswupAssetAnalysisDto AnalyzeAssets(string? script, IEnumerable<string> urls)
    {
        var code = JavaScriptSource.StripComments(script ?? string.Empty);
        var assigned = JavaScriptSource.ReadAssignments(code, "self")
                                       .GroupBy(assignment => assignment.Name, StringComparer.Ordinal)
                                       .ToDictionary(group => group.Key, group => group.Last().Value, StringComparer.Ordinal);

        // A mode preset changes what gets cached (isPassive) and how patterns match
        // (caseInsensitiveUrl), so the analysis has to run against the settings the worker ends up
        // with, not only the ones spelled out in the file.
        var settings = Effective(assigned);

        var caseInsensitive = IsTruthy(settings.GetValueOrDefault("caseInsensitiveUrl"));
        var notes = new List<string>();

        var include = BuildPatterns("include", settings, "assetsInclude", "ignoreDefaultInclude",
                                    BswupScriptCatalog.DefaultAssetsInclude, caseInsensitive, notes);
        var exclude = BuildPatterns("exclude", settings, "assetsExclude", "ignoreDefaultExclude",
                                    BswupScriptCatalog.DefaultAssetsExclude, caseInsensitive, notes);

        var decisions = urls.Select(url =>
        {
            var excluded = exclude.FirstOrDefault(pattern => pattern.Matches(url));
            if (excluded is not null)
            {
                return new BswupAssetDecisionDto
                {
                    Url = url,
                    Cached = false,
                    Reason = $"excluded by {excluded.Description}"
                };
            }

            var included = include.FirstOrDefault(pattern => pattern.Matches(url));

            return new BswupAssetDecisionDto
            {
                Url = url,
                Cached = included is not null,
                Reason = included is not null
                    ? $"included by {included.Description}"
                    : "no include pattern matches it - the worker never caches this asset (it is fetched from the network every time)"
            };
        }).ToArray();

        if (caseInsensitive) notes.Add("caseInsensitiveUrl is on, so every pattern is compiled with the 'i' flag (patterns that already carry it are left alone).");

        notes.Add("The URLs compared here are the ones as written in service-worker-assets.js / externalAssets, e.g. `_framework/blazor.boot.json`, not absolute URLs.");
        notes.Add($"The running worker's own script is excluded on top of these lists, by its actual URL rather than by the hardcoded '{Engine}' name - so a worker registered under a custom file name is never precached either.");

        if (IsTruthy(settings.GetValueOrDefault("isPassive")))
        {
            notes.Add("isPassive is on: the assets below are not precached during install, they are cached on first request - and a first install still tops the cache up in the background once Blazor has started, so the set that ends up cached is the same.");
        }

        return new BswupAssetAnalysisDto
        {
            Include = [.. include.Select(pattern => pattern.Description)],
            Exclude = [.. exclude.Select(pattern => pattern.Description)],
            Assets = decisions,
            Notes = [.. notes]
        };
    }

    private static void InspectMode(Dictionary<string, string> values, List<string> problems, List<string> notes)
    {
        if (values.TryGetValue("mode", out var raw) is false) return;

        var mode = Unquote(raw);
        var preset = BswupScriptCatalog.Modes.FirstOrDefault(m => m.Name == mode);

        if (preset is null)
        {
            problems.Add($"`self.mode = '{mode}'` is not a mode the shipped worker knows, so no preset is applied at all. Valid modes: {string.Join(", ", BswupScriptCatalog.Modes.Select(m => m.Name))}.");
            return;
        }

        var applied = preset.Settings
            .Where(setting => values.ContainsKey(setting.Key) is false)
            .Select(setting => $"{setting.Key} = {setting.Value}");

        var overridden = preset.Settings
            .Where(setting => values.ContainsKey(setting.Key))
            .Select(setting => setting.Key);

        notes.Add($"The '{mode}' preset fills in: {string.Join(", ", applied.DefaultIfEmpty("nothing - every setting it covers is assigned explicitly"))}.");

        if (overridden.Any())
        {
            notes.Add($"The file's own assignments win over the preset for: {string.Join(", ", overridden)}.");
        }
    }

    /// <summary>
    /// The settings as the worker will see them: what the file assigns, plus whatever a mode preset
    /// fills in around it. Checking the file's own assignments alone would report a shell URL as
    /// missing when the preset supplies it.
    /// </summary>
    private static Dictionary<string, string> Effective(Dictionary<string, string> values)
    {
        var effective = new Dictionary<string, string>(values, StringComparer.Ordinal);

        if (values.TryGetValue("mode", out var raw) is false) return effective;

        var preset = BswupScriptCatalog.Modes.FirstOrDefault(mode => mode.Name == Unquote(raw));
        if (preset is null) return effective;

        foreach (var setting in preset.Settings)
        {
            // The preset only fills what the file left alone - including an explicitly falsy value.
            // Stored unquoted, the way the preset states it, so an empty noPrerenderQuery stays falsy.
            if (effective.ContainsKey(setting.Key) is false) effective[setting.Key] = setting.Value;
        }

        return effective;
    }

    private static void InspectDefaultUrl(
        Dictionary<string, string> values,
        Dictionary<string, string> effective,
        List<string> warnings,
        List<string> notes)
    {
        var externalAssets = values.GetValueOrDefault("externalAssets") ?? string.Empty;
        var hasDefaultUrl = effective.TryGetValue("defaultUrl", out var raw);
        var defaultUrl = hasDefaultUrl ? Unquote(raw!) : "index.html";

        // The pairing that actually breaks offline navigation: a shell URL no asset serves. Only
        // the externalAssets side is visible here - a manifest entry cannot be checked from the
        // file - so the check is limited to the root shell, which is never in the manifest.
        var isRootShell = defaultUrl is "/" or "";
        var declaresRootAsset = RootAssetRegex().IsMatch(externalAssets);

        if (isRootShell && declaresRootAsset is false)
        {
            warnings.Add("defaultUrl is the root document ('/') but externalAssets declares no `{ url: '/' }` entry. service-worker-assets.js never lists the root, so nothing matches the default URL: offline navigation silently falls through to the network and the worker logs a 'defaultUrl ... matches no asset' warning at startup.");
        }

        if (hasDefaultUrl is false && declaresRootAsset)
        {
            notes.Add("externalAssets declares the root document but defaultUrl is left at its default ('index.html'). For an app whose shell IS the root (a _Host.cshtml / Blazor Web App host page), also set `self.defaultUrl = '/'` - otherwise the cached root is never used as the offline app shell.");
        }

        if (isRootShell && IsTruthy(effective.GetValueOrDefault("noPrerenderQuery")) is false)
        {
            notes.Add("A server-rendered shell is cached as-is. If the host prerenders it, set `self.noPrerenderQuery` (and read that query back in the host page) so the cached shell is route-agnostic - otherwise every offline deep link flashes the prerendered home page first.");
        }
    }

    private static void InspectLists(BswupSettingAssignmentDto[] settings, List<string> warnings, List<string> notes)
    {
        foreach (var setting in settings.Where(setting => _urlLists.Contains(setting.Name)))
        {
            var literals = JavaScriptSource.ReadLiterals(setting.Value);
            var strings = literals.Where(literal => literal.StartsWith('/') is false).ToArray();

            if (strings.Length > 0)
            {
                warnings.Add($"`self.{setting.Name}` contains string entries ({string.Join(", ", strings)}). A string is regex-escaped and matched as a LITERAL SUBSTRING of the URL - it cannot express \"ends with\". Releases before v-10-6-0 ignored string entries entirely, so these take effect for the first time after upgrading. Use a RegExp for anything anchored.");
            }

            if (literals.Count == 0 && setting.Value.StartsWith('['))
            {
                notes.Add($"`self.{setting.Name}` holds no literal patterns this check can evaluate (it is built from variables or calls), so its entries were not analyzed.");
            }
        }

        if (settings.Any(setting => setting.Name == "prohibitedUrls"))
        {
            notes.Add("prohibitedUrls is a client-side convenience, not a security boundary: matches are answered with 403 by the worker only, which is bypassed on any uncontrolled page (a first visit, a hard reload) and by anything talking to the server directly. Enforce access control on the server.");
        }
    }

    private static void InspectFlags(Dictionary<string, string> values, List<string> warnings, List<string> notes)
    {
        if (IsTruthy(values.GetValueOrDefault("ignoreDefaultExclude")))
        {
            warnings.Add("ignoreDefaultExclude drops the built-in exclude list, which is what keeps the service-worker scripts themselves out of the cache. A cached service-worker script corrupts the update cycle - only the browser should handle those files. Re-add them to assetsExclude if you need this flag for something else.");
        }

        if (IsTruthy(values.GetValueOrDefault("ignoreDefaultInclude")))
        {
            notes.Add("ignoreDefaultInclude drops the built-in include list, so ONLY the patterns in assetsInclude are cached - the .dll/.wasm/.js/.css assets the app boots from included.");
        }

        if (values.TryGetValue("errorTolerance", out var tolerance))
        {
            var value = Unquote(tolerance);
            if (value is not ("lax" or "strict"))
            {
                warnings.Add($"errorTolerance is '{value}'; the worker only knows 'lax' and 'strict' and falls back to 'lax' for anything else.");
            }
            else if (value == "strict")
            {
                notes.Add("errorTolerance 'strict': any asset that fails to download or store rejects the install, the partial cache is discarded and the previous worker keeps serving. On a first install there is no previous worker, so Bswup starts the app from the network and retries on the next load.");
            }
        }

        if (IsTruthy(values.GetValueOrDefault("enableFetchDiagnostics")))
        {
            warnings.Add("enableFetchDiagnostics logs on EVERY fetch. It is a debugging aid - leaving it on in production floods the console of every user.");
        }

        if (IsTruthy(values.GetValueOrDefault("enableIntegrityCheck")))
        {
            notes.Add("enableIntegrityCheck requires assets to be served byte-identically (a rewriting CDN or proxy will break the check), and it disables the no-cors fallback for cross-origin externalAssets, since an opaque response cannot be verified.");
        }
    }

    private record Pattern(string Description, Regex? Regex, string? Literal)
    {
        public bool Matches(string url)
        {
            if (Regex is not null) return Regex.IsMatch(url);

            return Literal is not null && url.Contains(Literal, StringComparison.OrdinalIgnoreCase);
        }
    }

    private static Pattern[] BuildPatterns(
        string kind,
        Dictionary<string, string> settings,
        string listName,
        string ignoreName,
        string[] defaults,
        bool caseInsensitive,
        List<string> notes)
    {
        var patterns = new List<Pattern>();

        if (IsTruthy(settings.GetValueOrDefault(ignoreName)))
        {
            notes.Add($"{ignoreName} is set, so the built-in {kind} list is not applied.");
        }
        else
        {
            patterns.AddRange(defaults.Select(pattern => Compile(pattern, $"{pattern} (built-in)", caseInsensitive, notes)));
        }

        if (settings.TryGetValue(listName, out var list))
        {
            patterns.AddRange(JavaScriptSource.ReadLiterals(list)
                                              .Select(pattern => Compile(pattern, $"{pattern} (self.{listName})", caseInsensitive, notes)));
        }

        return [.. patterns.Where(pattern => pattern.Regex is not null || pattern.Literal is not null)];
    }

    /// <summary>
    /// Compiles one list entry the way the worker does: a RegExp literal keeps its pattern and
    /// flags, a string is matched literally as a substring.
    /// </summary>
    private static Pattern Compile(string literal, string description, bool caseInsensitive, List<string> notes)
    {
        if (literal.StartsWith('/') is false)
        {
            return new Pattern(description, null, Unquote(literal));
        }

        var end = literal.LastIndexOf('/');
        var body = literal[1..end];
        var flags = literal[(end + 1)..];

        var options = RegexOptions.None;
        if (flags.Contains('i', StringComparison.Ordinal) || caseInsensitive) options |= RegexOptions.IgnoreCase;
        if (flags.Contains('m', StringComparison.Ordinal)) options |= RegexOptions.Multiline;
        if (flags.Contains('s', StringComparison.Ordinal)) options |= RegexOptions.Singleline;

        try
        {
            return new Pattern(description, new Regex(body, options), null);
        }
        catch (ArgumentException exception)
        {
            // A pattern .NET cannot compile is reported rather than silently dropped: the URLs it
            // would have decided are then decided by the remaining patterns, which is a different
            // answer, and the caller has to know that.
            notes.Add($"The pattern {literal} could not be evaluated here ({exception.Message}); it was left out of this analysis.");

            return new Pattern(description, null, null);
        }
    }

    private static (int Index, string? Text) FindImport(string code, string engine)
    {
        var index = code.IndexOf(engine, StringComparison.OrdinalIgnoreCase);
        if (index < 0) return (-1, null);

        var start = code.LastIndexOf("importScripts", index, StringComparison.Ordinal);
        if (start < 0) return (-1, null);

        var end = code.IndexOf(')', index);

        return (start, end < 0 ? code[start..] : Collapse(code[start..(end + 1)]));
    }

    /// <summary>Whether a JavaScript expression is truthy, for the literal values a config file holds.</summary>
    private static bool IsTruthy(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return false;

        var text = value.Trim();

        return text is not ("false" or "0" or "null" or "undefined" or "''" or "\"\"" or "``");
    }

    private static string Unquote(string value)
    {
        var text = value.Trim().TrimEnd(',');

        return text.Length >= 2 && (text[0] is '\'' or '"' or '`') && text[^1] == text[0] ? text[1..^1] : text;
    }

    private static string Collapse(string value) => WhitespaceRegex().Replace(value, " ").Trim();

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespaceRegex();

    /// <summary>An externalAssets entry for the root document, in any of the shapes the worker accepts.</summary>
    [GeneratedRegex(@"(url\s*:\s*(['""])/\2)|(^|[\[\s,])(['""])/\4")]
    private static partial Regex RootAssetRegex();
}
