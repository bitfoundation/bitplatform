using System.ComponentModel;
using ModelContextProtocol.Server;

namespace Bit.Belemetry.Demo.Server.Controllers;

/// <summary>
/// Ready-made workflows for the three things people ask an agent to do with Belemetry: add it to an app,
/// check an existing setup, and put a consent banner in front of it. Each one is an order of tool calls -
/// the order matters more than any single call, because the host decides what the rest of the setup is.
/// </summary>
[McpServerPromptType]
public static class McpPrompts
{
    [McpServerPrompt(Name = "set_up_belemetry")]
    [Description("Adds bit Belemetry telemetry to a Blazor app - registration, script, initializer, exporters and consent - using the real API, then reviews the result.")]
    public static string SetUpBelemetry(
        [Description("The host: web-app, server, webassembly, auto, static-ssr, standalone-wasm or hybrid. Pass 'unknown' to have it determined from the project first.")] string host = "unknown",
        [Description("Where the telemetry should go, in your own words - e.g. 'the Aspire dashboard', 'Application Insights', 'Honeycomb'.")] string destination = "an OpenTelemetry Collector")
    {
        return $"""
            Add bit Belemetry (NuGet package Bit.Belemetry) to this app. Telemetry should go to: {destination}.
            The host is: {host}.

            Work in this order:

            1. If the host is 'unknown', determine it from the project first: AddInteractiveServerComponents /
               AddInteractiveWebAssemblyComponents in Program.cs, a separate .Client project, a WebAssemblyHostBuilder
               (standalone), or a BlazorWebView (hybrid). The host decides which projects register the services.
            2. Call `GetBelemetrySetupGuide` with the host and follow it - it shows the real files of a working project.
            3. Call `GenerateBelemetryConfiguration` with the host, the exporters the destination needs (otlp, azure,
               console) and consent 'opt-in' unless told otherwise. Adapt the generated files to the project.
            4. For the destination, call `SearchBelemetry` (e.g. "Aspire dashboard", "Honeycomb") - CORS and credential
               rules differ per backend - and `GetBelemetryApiDetails` for every options class you set.
            5. Call `ReviewBelemetryConfiguration` on everything you changed and fix every error and warning.

            Rules that are not visible in any signature:
            - Nothing is sent before the user consents (Consent.Default = None). The app needs a consent UI that calls
              IBelemetryConsent.SetAsync, or nothing will ever arrive.
            - An OTLP Endpoint is the base URL; /v1/traces, /v1/logs and /v1/metrics are appended.
            - With the Direct transport, exporter headers are visible in the page. Keyed backends need the Relay transport.
            - On ASP.NET Core hosts the server's own .NET telemetry is off unless Server.CollectDotnetTelemetry = true.
            """;
    }

    [McpServerPrompt(Name = "review_belemetry_setup")]
    [Description("Reviews an existing bit Belemetry setup for mistakes that send nothing, send too much, or leak credentials, and proposes fixes.")]
    public static string ReviewBelemetrySetup(
        [Description("The code to review (Program.cs, App.razor, Routes.razor), or 'project' to collect it from the open project.")] string code = "project")
    {
        var fromProject = code == "project";

        var collect = fromProject
            ? "Collect every Program.cs / MauiProgram.cs that calls AddBitBelemetryServices, App.razor and Routes.razor (or the main layout) from the project."
            : "The code to review is at the end of this message.";

        var attached = fromProject ? string.Empty : $"\n\n```\n{code}\n```";

        return $"""
            Review this app's bit Belemetry setup.

            1. {collect}
            2. Call `ReviewBelemetryConfiguration` with all of it at once (the rules about missing pieces need every file).
            3. For each finding, check the option involved with `GetBelemetryApiDetails` before proposing the fix.
            4. Check that every project the components run in registers AddBitBelemetryServices with the same configuration
               (`GetBelemetrySetupGuide` for the host says which).
            5. Report the findings with their fixes, most severe first.
            """ + attached;
    }

    [McpServerPrompt(Name = "add_consent_banner")]
    [Description("Adds a GDPR consent banner wired to IBelemetryConsent, so users can grant or refuse each telemetry feature.")]
    public static string AddConsentBanner()
    {
        return """
            Add a telemetry consent banner to this app, wired to bit Belemetry's IBelemetryConsent.

            1. Call `GetBelemetrySourceFile` with path "Demo/Client/Shared/ConsentBanner.razor" (the demo's banner) and
               "Demo/Client/Pages/ConsentPage.razor" (a per-feature consent panel), and `GetBelemetryApiDetails` for
               IBelemetryConsent and BelemetryFeatures.
            2. Show the banner only once the app is interactive and only while IBelemetryConsent.IsDecided is false; call
               RefreshAsync on the first interactive render, so a returning user who already decided never sees it.
            3. Offer at least "Accept all" (BelemetryFeatures.All), a reduced choice (e.g. BelemetryFeatures.Diagnostics)
               and "Reject" (BelemetryFeatures.None), and a place (a privacy page) to change the choice per feature
               with GrantAsync/RevokeAsync.
            4. Subscribe to IBelemetryConsent.Changed and unsubscribe on Dispose.
            5. Keep Consent.Default = BelemetryFeatures.None. Call `ReviewBelemetryConfiguration` on the result.
            """;
    }
}
