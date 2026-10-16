using System.Text.RegularExpressions;
using Bit.Belemetry.Demo.Server.Dtos;

namespace Bit.Belemetry.Demo.Server.Services;

/// <summary>
/// Lints Belemetry wiring - a Program.cs, an App.razor, a Routes.razor, or all of them pasted together - for the
/// mistakes that compile and then silently send nothing, send too much, or leak a credential into every page.
/// <para>
/// The rules are textual on purpose: an agent hands over a snippet, not a project, so there is no semantic model
/// to bind against. Each rule therefore looks for a shape that is wrong wherever it appears, and the rules that
/// depend on something being absent (no <c>&lt;BelemetryScript /&gt;</c>) only fire when the snippet contains the
/// file the thing belongs in.
/// </para>
/// </summary>
public static partial class BelemetryCodeReview
{
    private sealed record Rule(string Id, string Severity, Func<string, IEnumerable<(int Index, string Message, string Fix)>> Check);

    private static readonly Rule[] _rules =
    [
        new("BLM001", "error", RenamedApi),
        new("BLM002", "error", SignalPathInEndpoint),
        new("BLM003", "error", CredentialsInBrowser),
        new("BLM004", "warning", ConsentDefault),
        new("BLM005", "warning", MissingScript),
        new("BLM006", "warning", MissingInitializer),
        new("BLM007", "warning", DuplicateInitializer),
        new("BLM008", "info", ServerPipeline),
        new("BLM009", "error", RelativeEndpoint),
        new("BLM010", "warning", GrpcPort),
        new("BLM011", "warning", InteractionText),
        new("BLM012", "info", PrivacyWeakened),
        new("BLM013", "error", RatioOutOfRange),
        new("BLM014", "error", BlockingWait),
        new("BLM015", "info", TokenProviderInBrowser),
        new("BLM016", "warning", PersistenceOnServer),
    ];

    /// <summary>The rule ids, for the docs.</summary>
    public static string[] RuleIds => [.. _rules.Select(r => r.Id)];

    /// <summary>Reviews <paramref name="code"/> and returns every finding, in line order.</summary>
    public static BelemetryReviewDto Review(string? code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return new("Nothing to review: pass the Belemetry registration (Program.cs) and, ideally, App.razor and Routes.razor.", []);
        }

        var text = code.Replace("\r\n", "\n", StringComparison.Ordinal);
        var findings = new List<BelemetryReviewFindingDto>();

        foreach (var rule in _rules)
        {
            foreach (var (index, message, fix) in rule.Check(text))
            {
                findings.Add(new(rule.Id, rule.Severity, index < 0 ? null : LineOf(text, index), message, fix));
            }
        }

        var ordered = findings.OrderBy(f => f.Line ?? int.MaxValue).ThenBy(f => f.Rule, StringComparer.Ordinal).ToArray();

        var errors = ordered.Count(f => f.Severity == "error");
        var warnings = ordered.Count(f => f.Severity == "warning");
        var infos = ordered.Length - errors - warnings;

        var summary = ordered.Length == 0
            ? "No problems found by the rules. They are textual: they catch the common mistakes, not every one - check consent and the exporter endpoints by running the app with options.Debug and Browser.Debug on."
            : $"{errors} error(s), {warnings} warning(s), {infos} note(s).";

        return new(summary, ordered);
    }

    private static int LineOf(string text, int index) => text.AsSpan(0, Math.Min(index, text.Length)).Count('\n') + 1;

    /// <summary>Whether the match sits in a <c>//</c> comment: commented-out code is a suggestion, not configuration.</summary>
    private static bool IsCommented(string text, int index)
    {
        var lineStart = index == 0 ? 0 : text.LastIndexOf('\n', index - 1) + 1;
        var inString = false;

        // A "//" inside a string literal ("https://...") is a URL, not a comment.
        for (var i = lineStart; i < index - 1; i++)
        {
            if (text[i] == '"' && (i == 0 || text[i - 1] != '\\')) inString = !inString;
            else if (inString is false && text[i] == '/' && text[i + 1] == '/') return true;
        }

        return false;
    }

    private static IEnumerable<(int, string, string)> RenamedApi(string code)
    {
        foreach (Match m in OldNamesRegex().Matches(code))
        {
            yield return (m.Index,
                          $"'{m.Value}' is the old name; it does not exist in Bit.Belemetry.",
                          m.Value.StartsWith("Add", StringComparison.Ordinal) ? "Call builder.Services.AddBitBelemetryServices(...)." : "The browser global is window.BitBelemetry (e.g. BitBelemetry.getContext()).");
        }
    }

    private static IEnumerable<(int, string, string)> SignalPathInEndpoint(string code)
    {
        foreach (Match m in EndpointWithSignalRegex().Matches(code))
        {
            if (IsCommented(code, m.Index)) continue;

            yield return (m.Index,
                          $"The OTLP endpoint '{m.Groups["url"].Value}' already ends in a signal path. Endpoint is the BASE URL: Belemetry appends /v1/traces, /v1/logs and /v1/metrics, so this exports to .../v1/traces/v1/traces and every signal but one goes to the wrong place.",
                          "Pass the base (e.g. \"http://localhost:4318\"), or set TracesEndpoint/LogsEndpoint/MetricsEndpoint for full per-signal URLs.");
        }
    }

    private static IEnumerable<(int, string, string)> CredentialsInBrowser(string code)
    {
        if (RelayRegex().IsMatch(code) || IncludeInBrowserFalseRegex().IsMatch(code)) yield break;

        foreach (Match m in CredentialHeaderRegex().Matches(code))
        {
            if (IsCommented(code, m.Index)) continue;

            yield return (m.Index,
                          $"The exporter header '{m.Groups["key"].Value}' looks like a credential. With the Direct transport (the default outside Hybrid) the browser script exports with the same headers, so it is written into every page for anyone to read.",
                          "Set options.Browser.Transport = BelemetryBrowserTransport.Relay (the browser hands its records to .NET), or set IncludeInBrowser = false on this exporter and give the browser a BrowserEndpoint without credentials (a collector).");
        }
    }

    private static IEnumerable<(int, string, string)> ConsentDefault(string code)
    {
        foreach (Match m in ConsentDefaultRegex().Matches(code))
        {
            var value = m.Groups["value"].Value;

            if (value.Contains("All", StringComparison.Ordinal) || value.Contains("Analytics", StringComparison.Ordinal)
                || value.Contains("UserTracking", StringComparison.Ordinal) || value.Contains("Sessions", StringComparison.Ordinal)
                || value.Contains("Interactions", StringComparison.Ordinal) || value.Contains("PageViews", StringComparison.Ordinal))
            {
                yield return (m.Index,
                              $"Consent.Default = {value.Trim()} grants usage analytics before the user has decided. Under GDPR/ePrivacy that needs consent; it is only right for apps that need none (internal tools, jurisdictions without such rules).",
                              "Keep Consent.Default = BelemetryFeatures.None (the default) and show a consent banner that calls IBelemetryConsent.SetAsync(...). At most default to what legitimate interest covers, e.g. BelemetryFeatures.Errors.");
            }
        }
    }

    private static IEnumerable<(int, string, string)> MissingScript(string code)
    {
        var head = HeadRegex().Match(code);

        if (head.Success && code.Contains("<BelemetryScript", StringComparison.Ordinal) is false)
        {
            yield return (head.Index,
                          "This App.razor has no <BelemetryScript />. Without it the browser script only starts when an interactive <BelemetryInitializer /> runs, so it misses the initial page load, the web vitals, early errors, and every static SSR page.",
                          "Add <BelemetryScript /> inside <head>, before <HeadOutlet />.");
        }
    }

    private static IEnumerable<(int, string, string)> MissingInitializer(string code)
    {
        var router = RouterRegex().Match(code);

        if (router.Success && code.Contains("<BelemetryInitializer", StringComparison.Ordinal) is false)
        {
            yield return (router.Index,
                          "There is a <Router> but no <BelemetryInitializer />. Without it .NET never connects to the browser script: IBelemetryConsent cannot read or set the user's choice, the session and user do not reach .NET records, the Relay transport has nothing to relay to, and WebAssembly gets no unload flush.",
                          "Add <BelemetryInitializer /> once, next to the <Router> in Routes.razor (or in the main layout).");
        }
    }

    private static IEnumerable<(int, string, string)> DuplicateInitializer(string code)
    {
        var matches = InitializerRegex().Matches(code);

        if (matches.Count > 1)
        {
            yield return (matches[1].Index,
                          "<BelemetryInitializer /> appears more than once. It is the one bridge between the script and .NET and belongs at the root; per-page copies only reconnect it on every navigation.",
                          "Keep a single <BelemetryInitializer /> in Routes.razor or the main layout.");
        }
    }

    private static IEnumerable<(int, string, string)> ServerPipeline(string code)
    {
        if (code.Contains("WebApplication.CreateBuilder", StringComparison.Ordinal) is false) yield break;
        if (code.Contains("AddBitBelemetryServices", StringComparison.Ordinal) is false) yield break;
        if (CollectDotnetRegex().IsMatch(code)) yield break;

        var registration = code.IndexOf("AddBitBelemetryServices", StringComparison.Ordinal);

        yield return (registration,
                      "On an ASP.NET Core host Belemetry's .NET pipeline is off by default: this server serves the browser script, but its own ILogger logs, request spans and metrics are not collected (most servers have their own OpenTelemetry setup).",
                      "If the server has no other telemetry, set options.Server.CollectDotnetTelemetry = true (governed by options.Server.Features, not by user consent). With WebAssembly/Auto, also register AddBitBelemetryServices in the client project - that is where the .NET pipeline of the components runs.");
    }

    private static IEnumerable<(int, string, string)> RelativeEndpoint(string code)
    {
        foreach (Match m in AddOtlpStringRegex().Matches(code))
        {
            var url = m.Groups["url"].Value;

            if (url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || url.StartsWith("https://", StringComparison.OrdinalIgnoreCase)) continue;

            yield return (m.Index,
                          $"AddOtlpExporter(\"{url}\") needs an absolute URL - the string overload parses it with UriKind.Absolute and throws at startup.",
                          "Pass an absolute URL. In WebAssembly build it from the app's origin: builder.HostEnvironment.BaseAddress + \"otlp\". For a browser-only relative route, set BrowserEndpoint = new Uri(\"/otlp\", UriKind.Relative) on the exporter instead.");
        }
    }

    private static IEnumerable<(int, string, string)> GrpcPort(string code)
    {
        foreach (Match m in Port4317Regex().Matches(code))
        {
            yield return (m.Index,
                          "Port 4317 is the OTLP/gRPC port. Belemetry speaks OTLP over HTTP only (protobuf or JSON), which collectors serve on 4318.",
                          "Use http://<collector>:4318 (the Aspire dashboard's OTLP/HTTP port is configured separately - see its DOTNET_DASHBOARD_OTLP_HTTP_ENDPOINT_URL).");
        }
    }

    private static IEnumerable<(int, string, string)> InteractionText(string code)
    {
        foreach (Match m in CaptureTextRegex().Matches(code))
        {
            yield return (m.Index,
                          "CaptureInteractionText records the text of what users click - names, e-mail addresses and order numbers included.",
                          "Prefer data-bit-belemetry-name on the elements worth naming; if text is needed, keep MaxInteractionTextLength short and exclude sensitive regions with data-bit-belemetry-ignore.");
        }
    }

    private static IEnumerable<(int, string, string)> PrivacyWeakened(string code)
    {
        foreach (Match m in PrivacyOffRegex().Matches(code))
        {
            yield return (m.Index,
                          $"'{m.Value.Trim()}' turns off a privacy protection that is on by default.",
                          "Keep it on unless the backend is covered by the same data-protection terms as the app and nothing it removes can reach it.");
        }
    }

    private static IEnumerable<(int, string, string)> RatioOutOfRange(string code)
    {
        foreach (Match m in RatioRegex().Matches(code))
        {
            if (double.TryParse(m.Groups["value"].Value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var value) && (value < 0 || value > 1))
            {
                yield return (m.Index,
                              $"{m.Groups["name"].Value} is a fraction between 0 and 1; {value} is outside it.",
                              "Use e.g. 0.25 for a quarter of sessions/traces.");
            }
        }
    }

    private static IEnumerable<(int, string, string)> BlockingWait(string code)
    {
        foreach (Match m in BlockingRegex().Matches(code))
        {
            yield return (m.Index,
                          "A blocking wait on a Belemetry call. Browser WebAssembly is single-threaded: the export it waits for can never run, so this deadlocks the app.",
                          "await it (await Belemetry.FlushAsync()).");
        }
    }

    private static IEnumerable<(int, string, string)> TokenProviderInBrowser(string code)
    {
        var provider = TokenProviderRegex().Match(code);

        if (provider.Success && RelayRegex().IsMatch(code) is false)
        {
            yield return (provider.Index,
                          "An Azure Monitor exporter with an AccessTokenProvider (Entra ID) is .NET-only: the browser cannot obtain the token, so with the Direct transport the browser's own records (page views, clicks, web vitals, JS errors) do not go to this resource.",
                          "Set options.Browser.Transport = BelemetryBrowserTransport.Relay so the browser's records are exported by .NET, or add a second exporter for the browser.");
        }
    }

    private static IEnumerable<(int, string, string)> PersistenceOnServer(string code)
    {
        var directory = PersistenceDirectoryRegex().Match(code);

        if (directory.Success && directory.Groups["dir"].Value.StartsWith("wwwroot", StringComparison.OrdinalIgnoreCase))
        {
            yield return (directory.Index,
                          "The persistence directory is under wwwroot, so unsent telemetry would be served as static files.",
                          "Use a private, writable directory (e.g. Path.Combine(FileSystem.AppDataDirectory, \"belemetry\") in MAUI, or a data volume on a server).");
        }
    }

    [GeneratedRegex(@"\bAddBelemetryServices\b|\bwindow\.Belemetry\b|(?<![\w.])Belemetry\.(?=[a-z])")]
    private static partial Regex OldNamesRegex();

    [GeneratedRegex(@"(?:AddOtlpExporter\s*\(\s*|(?<!\w)(?:Browser)?Endpoint\s*=\s*new\s+Uri\s*\(\s*|(?<!\w)(?:Browser)?Endpoint\s*=\s*)""(?<url>[^""]*/v1/(?:traces|logs|metrics)/?)""")]
    private static partial Regex EndpointWithSignalRegex();

    [GeneratedRegex(@"Transport\s*=\s*BelemetryBrowserTransport\.Relay")]
    private static partial Regex RelayRegex();

    [GeneratedRegex(@"IncludeInBrowser\s*=\s*false")]
    private static partial Regex IncludeInBrowserFalseRegex();

    [GeneratedRegex(@"Headers\s*(?:\[\s*|\.Add\s*\(\s*|\.TryAdd\s*\(\s*)""(?<key>[^""]*(?:key|token|auth|secret|honeycomb|signoz|uptrace|dd-api|password)[^""]*)""", RegexOptions.IgnoreCase)]
    private static partial Regex CredentialHeaderRegex();

    [GeneratedRegex(@"Consent\.Default\s*=\s*(?<value>[^;\n]+)")]
    private static partial Regex ConsentDefaultRegex();

    [GeneratedRegex(@"<head[\s>]", RegexOptions.IgnoreCase)]
    private static partial Regex HeadRegex();

    [GeneratedRegex(@"<Router[\s>]")]
    private static partial Regex RouterRegex();

    [GeneratedRegex(@"<BelemetryInitializer\b")]
    private static partial Regex InitializerRegex();

    [GeneratedRegex(@"CollectDotnetTelemetry\s*=\s*true")]
    private static partial Regex CollectDotnetRegex();

    [GeneratedRegex(@"AddOtlpExporter\s*\(\s*""(?<url>[^""]*)""")]
    private static partial Regex AddOtlpStringRegex();

    [GeneratedRegex(@"""[a-z]+://[^""/]+:4317(?:/[^""]*)?""", RegexOptions.IgnoreCase)]
    private static partial Regex Port4317Regex();

    [GeneratedRegex(@"CaptureInteractionText\s*=\s*true")]
    private static partial Regex CaptureTextRegex();

    [GeneratedRegex(@"(?:StripFragment|ScrubPatterns)\s*=\s*false|RedactQueryParameters\.Clear\s*\(\s*\)|MaskIp\s*=\s*false")]
    private static partial Regex PrivacyOffRegex();

    [GeneratedRegex(@"(?<name>SessionSampleRatio|SamplingRatio)\s*=\s*(?<value>-?[0-9]+(?:\.[0-9]+)?)")]
    private static partial Regex RatioRegex();

    [GeneratedRegex(@"\.(?:FlushAsync|SetAsync|GrantAsync|RevokeAsync|RefreshAsync|SetUserAsync)\s*\([^()]*\)\s*(?:\.Result\b|\.Wait\s*\(|\.GetAwaiter\s*\(\s*\)\s*\.GetResult\s*\()")]
    private static partial Regex BlockingRegex();

    [GeneratedRegex(@"AccessTokenProvider\s*=")]
    private static partial Regex TokenProviderRegex();

    [GeneratedRegex(@"Persistence\.Directory\s*=\s*(?:Path\.Combine\s*\([^""]*)?""(?<dir>[^""]*)""")]
    private static partial Regex PersistenceDirectoryRegex();
}
