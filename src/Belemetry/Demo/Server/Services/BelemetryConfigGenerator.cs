using System.Text;
using Bit.Belemetry.Demo.Server.Dtos;

namespace Bit.Belemetry.Demo.Server.Services;

/// <summary>
/// Writes the Belemetry wiring for a host, a set of exporters and a consent model: the shared configure method,
/// each project's registration, the App.razor and Routes.razor lines, and a consent banner when the model needs one.
/// <para>
/// The names it writes - option properties, enum values, components - are checked against the reflected API at
/// generation time (<see cref="BelemetryApiCatalog"/>), so a rename in the library breaks this class's tests
/// rather than handing an agent code that does not compile.
/// </para>
/// </summary>
public static class BelemetryConfigGenerator
{
    /// <summary>The exporter ids <c>exporters</c> accepts.</summary>
    public static readonly string[] ExporterIds = ["otlp", "azure", "console"];

    /// <summary>The consent models <c>consent</c> accepts.</summary>
    public static readonly string[] ConsentModels = ["opt-in", "opt-out", "none"];

    /// <summary>Generates the files; unknown inputs are reported in the notes rather than thrown.</summary>
    public static BelemetryGeneratedConfigDto Generate(string? host, string? exporters, string? consent, string? features, string? transport)
    {
        var notes = new List<string>();
        var files = new List<BelemetryGeneratedFileDto>();

        var found = BelemetrySetupGuide.Find(host);
        if (found is null)
        {
            notes.Add($"'{host}' is not a known host; generated for 'web-app'. Use one of: {string.Join(", ", BelemetrySetupGuide.HostIds)}.");
            found = BelemetrySetupGuide.Find("web-app")!;
        }

        var exporterSet = (exporters ?? "otlp").Split([',', ' ', ';', '+'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                                               .Select(e => e.ToLowerInvariant() switch { "azure-monitor" or "azuremonitor" or "appinsights" or "application-insights" => "azure", var x => x })
                                               .Distinct()
                                               .ToList();

        foreach (var unknown in exporterSet.Except(ExporterIds).ToArray())
        {
            notes.Add($"Unknown exporter '{unknown}' ignored. Use any of: {string.Join(", ", ExporterIds)} - or implement IBelemetryExporter and call options.AddExporter(...).");
            exporterSet.Remove(unknown);
        }

        if (exporterSet.Count == 0) exporterSet.Add("otlp");

        var consentModel = (consent ?? "opt-in").Trim().ToLowerInvariant();
        if (ConsentModels.Contains(consentModel) is false)
        {
            notes.Add($"Unknown consent model '{consent}'; generated 'opt-in'. Use one of: {string.Join(", ", ConsentModels)}.");
            consentModel = "opt-in";
        }

        var featureExpression = FeatureExpression(features, notes);
        var transportValue = TransportValue(transport, found.Id, notes);

        var hasClientProject = found.Id is "web-app" or "webassembly" or "auto";
        var setupFile = hasClientProject ? "Client/BelemetrySetup.cs" : "BelemetrySetup.cs";

        files.Add(new(setupFile, "C#", SetupClass(exporterSet, consentModel, featureExpression, transportValue, found.Id)));

        switch (found.Id)
        {
            case "hybrid":
                files.Add(new("MauiProgram.cs", "C#", """
                    // MauiProgram.CreateMauiApp (WPF/WinForms: the host's ServiceCollection)
                    builder.Services.AddMauiBlazorWebView();
                    builder.Services.AddBitBelemetryServices(BelemetrySetup.Configure);
                    """));
                files.Add(new("App.xaml.cs", "C#", """
                    public partial class App(IServiceProvider services) : Application
                    {
                        // A mobile OS may suspend a backgrounded app and kill it without another callback,
                        // so whatever is still queued is exported as soon as the app goes to the background.
                        protected override async void OnSleep()
                        {
                            base.OnSleep();

                            try
                            {
                                // IBelemetry is scoped (one per BlazorWebView); the queue it flushes is app-wide.
                                await using var scope = services.CreateAsyncScope();
                                await scope.ServiceProvider.GetRequiredService<IBelemetry>().FlushAsync();
                            }
                            catch (Exception)
                            {
                                // async void: an exception here would crash the app on its way to the background.
                            }
                        }
                    }
                    """));
                break;

            case "standalone-wasm":
                files.Add(new("Program.cs", "C#", """
                    builder.Services.AddBitBelemetryServices(BelemetrySetup.Configure);
                    """));
                files.Add(new("wwwroot/index.html", "HTML", """
                    <!-- Optional: starts the script before Blazor boots, to see the initial load and early errors. -->
                    <script type="module" src="_content/Bit.Belemetry/bit-belemetry.js"></script>
                    """));
                break;

            default:
                var server = new StringBuilder();
                server.AppendLine("// Server/Program.cs");
                server.AppendLine("builder.Services.AddBitBelemetryServices(BelemetrySetup.Configure);");
                if (found.Id is "server" or "static-ssr" or "auto" or "web-app")
                {
                    server.AppendLine();
                    server.AppendLine("// The server's own logs, request spans and metrics. Off by default (most servers have their own");
                    server.AppendLine("// OpenTelemetry setup); governed by Server.Features, an operator decision, not a user's consent.");
                    server.AppendLine("builder.Services.AddBitBelemetryServices(options => options.Server.CollectDotnetTelemetry = true);");
                }
                files.Add(new("Server/Program.cs", "C#", server.ToString().TrimEnd()));

                if (hasClientProject)
                {
                    files.Add(new("Client/Program.cs", "C#", """
                        // The same configuration in the WebAssembly container: the .NET pipeline runs in the browser.
                        builder.Services.AddBitBelemetryServices(BelemetrySetup.Configure);
                        """));
                }

                files.Add(new("Server/Components/App.razor", "Razor", """
                    <head>
                        ...
                        @* As early as possible: it starts before Blazor boots and also covers static SSR pages. *@
                        <BelemetryScript />
                        <HeadOutlet />
                    </head>
                    """));
                break;
        }

        if (found.Id != "static-ssr")
        {
            files.Add(new(hasClientProject ? "Client/Routes.razor" : found.Id == "standalone-wasm" ? "App.razor" : "Routes.razor", "Razor", """
                <Router AppAssembly="typeof(Program).Assembly">...</Router>

                @* Connects the browser script and .NET once the app is interactive: consent, session and user flow
                   both ways, and the WebAssembly pipeline gets its unload flush. Once, at the root. *@
                <BelemetryInitializer />
                """));
        }

        files.Add(new("_Imports.razor", "Razor", "@using Bit.Belemetry"));

        if (found.Id is "web-app" or "webassembly" or "auto" or "standalone-wasm")
        {
            notes.Add("Metrics in WebAssembly: the WebAssembly SDK turns System.Diagnostics.Metrics off by default, and the Bit.Belemetry package turns it back on (MetricsSupport). Do not set <MetricsSupport>false</MetricsSupport> in the client .csproj, or no Meter records anything (Belemetry logs a warning at startup when that happens).");
        }

        if (consentModel != "none" && found.Id != "static-ssr")
        {
            files.Add(new("Shared/ConsentBanner.razor", "Razor", ConsentBanner(consentModel)));
            notes.Add("Place <ConsentBanner /> once in the main layout. The choice is remembered (Consent.Persist), so it is shown only until the user decides.");
        }

        if (exporterSet.Contains("azure"))
        {
            files.Add(new("appsettings.json", "JSON", """
                {
                  "ApplicationInsights": {
                    "ConnectionString": "InstrumentationKey=<key>;IngestionEndpoint=https://<region>.in.applicationinsights.azure.com/"
                  }
                }
                """));
            notes.Add("Azure Monitor: the connection string is not a secret in the browser sense (the ingestion endpoint accepts anonymous telemetry), but with an AccessTokenProvider (Entra ID) the exporter is .NET-only and the browser's records need the Relay transport.");
        }

        if (exporterSet.Contains("otlp"))
        {
            notes.Add(transportValue == "Relay" || found.Id == "hybrid"
                ? "OTLP with the Relay transport: the browser hands its records to .NET, so only the .NET side talks to the collector - no CORS needed, and headers stay out of the page."
                : "OTLP with the Direct transport: the browser posts to the endpoint itself, so the collector must allow the site's origin (CORS), and any header you add (an API key) is visible in the page. Use Browser.Transport = Relay or a BrowserEndpoint without credentials for keyed backends.");
            notes.Add("The OTLP endpoint is the base URL (e.g. http://localhost:4318); /v1/traces, /v1/logs and /v1/metrics are appended. Use TracesEndpoint/LogsEndpoint/MetricsEndpoint for full per-signal URLs.");
        }

        notes.Add(consentModel switch
        {
            "opt-in" => "Opt-in: nothing is sent until the user grants a feature; records of undecided features are held in memory (Consent.PendingLimit) and sent or dropped once they decide.",
            "opt-out" => "Opt-out: the configured features are granted before any decision. In the EU/UK that is only lawful for what is strictly necessary or covered by legitimate interest - keep Analytics out of Consent.Default unless you are sure.",
            _ => "No consent UI: Consent.Default grants everything configured. Only for apps that need no consent (internal tools, jurisdictions without such rules); Global Privacy Control still removes Analytics.",
        });

        return new([.. files], [.. notes]);
    }

    private static string SetupClass(List<string> exporters, string consentModel, string? features, string? transport, string host)
    {
        var builder = new StringBuilder();

        builder.AppendLine("using Bit.Belemetry;");
        builder.AppendLine();
        builder.AppendLine("/// <summary>");
        builder.AppendLine("/// The one Belemetry configuration of the app. Every container that registers Belemetry calls it, so the");
        builder.AppendLine("/// server (which renders the browser script's configuration) and the client never disagree.");
        builder.AppendLine("/// </summary>");
        builder.AppendLine("public static class BelemetrySetup");
        builder.AppendLine("{");
        builder.AppendLine("    public static void Configure(BelemetryOptions options)");
        builder.AppendLine("    {");
        builder.AppendLine("        options.ServiceName = \"MyApp\";");
        builder.AppendLine("        options.ServiceVersion = typeof(BelemetrySetup).Assembly.GetName().Version?.ToString();");

        if (features is not null)
        {
            builder.AppendLine();
            builder.AppendLine("        // The instrumentations switched on at all. Consent can only narrow this further.");
            builder.AppendLine($"        options.Features = {features};");
        }

        builder.AppendLine();
        switch (consentModel)
        {
            case "opt-in":
                builder.AppendLine("        // Opt-in (the default, written out): nothing is granted until the user decides.");
                builder.AppendLine("        options.Consent.Default = BelemetryFeatures.None;");
                break;
            case "opt-out":
                builder.AppendLine("        // Opt-out: granted until the user revokes. Keep it to what needs no consent where you operate.");
                builder.AppendLine("        options.Consent.Default = BelemetryFeatures.Diagnostics | BelemetryFeatures.Monitoring;");
                break;
            default:
                builder.AppendLine("        // No consent UI: everything configured is granted.");
                builder.AppendLine("        options.Consent.Default = BelemetryFeatures.All;");
                break;
        }

        if (transport is not null)
        {
            builder.AppendLine();
            builder.AppendLine($"        options.Browser.Transport = BelemetryBrowserTransport.{transport};");
        }

        foreach (var exporter in exporters)
        {
            builder.AppendLine();

            switch (exporter)
            {
                case "otlp":
                    builder.AppendLine("        // OTLP/HTTP: the base URL - /v1/traces, /v1/logs and /v1/metrics are appended.");
                    builder.AppendLine("        options.AddOtlpExporter(\"https://otel.example.com\", otlp =>");
                    builder.AppendLine("        {");
                    builder.AppendLine("            otlp.Protocol = BelemetryOtlpProtocol.HttpProtobuf;");
                    if (host != "hybrid" && transport != "Relay")
                    {
                        builder.AppendLine("            // Direct transport: these headers would be visible in the page. Keep keyed backends .NET-only");
                        builder.AppendLine("            // (Browser.Transport = Relay) or put a collector without credentials in front of them.");
                        builder.AppendLine("            // otlp.Headers[\"x-api-key\"] = \"...\";");
                    }
                    else
                    {
                        builder.AppendLine("            // otlp.Headers[\"x-api-key\"] = \"...\";   // stays in .NET: the browser relays its records");
                    }
                    builder.AppendLine("        });");
                    break;

                case "azure":
                    builder.AppendLine("        // Azure Monitor (Application Insights), natively - no collector in between.");
                    builder.AppendLine("        options.AddAzureMonitorExporter(\"InstrumentationKey=<key>;IngestionEndpoint=https://<region>.in.applicationinsights.azure.com/\", azure =>");
                    builder.AppendLine("        {");
                    builder.AppendLine("            azure.MaskIp = true;");
                    builder.AppendLine("            azure.EnableLiveMetrics = true;");
                    builder.AppendLine("        });");
                    break;

                case "console":
                    builder.AppendLine("        // Every record, printed: the server/app console, and the browser console for the browser's records.");
                    builder.AppendLine("        options.AddConsoleExporter();");
                    break;
            }
        }

        builder.AppendLine("    }");
        builder.AppendLine("}");

        return builder.ToString().TrimEnd();
    }

    private static string ConsentBanner(string consentModel)
    {
        var intro = consentModel == "opt-in"
            ? "We would like to measure how this app is used and how it performs. Nothing is sent until you choose."
            : "This app measures errors and performance. You can turn that off, or allow usage analytics too.";

        return $$"""
            @implements IDisposable
            @inject IBelemetryConsent Consent

            @* Shown until the user decides. Rendered only once interactive: during prerendering the persisted
               choice is not known yet, and the banner would flash for every user who already decided. *@
            @if (_ready && Consent.IsDecided is false)
            {
                <section class="consent-banner" aria-label="Telemetry consent">
                    <p>{{intro}}</p>
                    <button @onclick="() => Consent.SetAsync(BelemetryFeatures.All)">Accept all</button>
                    <button @onclick="() => Consent.SetAsync(BelemetryFeatures.Diagnostics)">Error reports only</button>
                    <button @onclick="() => Consent.SetAsync(BelemetryFeatures.None)">Reject</button>
                </section>
            }

            @code {
                private bool _ready;

                protected override void OnInitialized() => Consent.Changed += OnChanged;

                protected override async Task OnAfterRenderAsync(bool firstRender)
                {
                    if (firstRender is false || RendererInfo.IsInteractive is false) return;

                    await Consent.RefreshAsync();   // adopts the choice the script restored from localStorage
                    _ready = true;
                    StateHasChanged();
                }

                private void OnChanged(object? sender, EventArgs e) => InvokeAsync(StateHasChanged);

                public void Dispose() => Consent.Changed -= OnChanged;
            }
            """;
    }

    private static string? FeatureExpression(string? features, List<string> notes)
    {
        if (string.IsNullOrWhiteSpace(features)) return null;

        var names = new List<string>();

        foreach (var raw in features.Split([',', '|', ' ', ';', '+'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var name = raw.StartsWith("BelemetryFeatures.", StringComparison.OrdinalIgnoreCase) ? raw["BelemetryFeatures.".Length..] : raw;

            if (Enum.TryParse<BelemetryFeatures>(name, ignoreCase: true, out var value) && int.TryParse(name, out _) is false)
            {
                names.Add($"BelemetryFeatures.{value}");
            }
            else
            {
                notes.Add($"'{raw}' is not a BelemetryFeatures value and was left out. Values: {string.Join(", ", Enum.GetNames<BelemetryFeatures>())}.");
            }
        }

        return names.Count == 0 ? null : string.Join(" | ", names.Distinct());
    }

    private static string? TransportValue(string? transport, string host, List<string> notes)
    {
        if (string.IsNullOrWhiteSpace(transport)) return null;

        if (Enum.TryParse<BelemetryBrowserTransport>(transport.Trim(), ignoreCase: true, out var value) && int.TryParse(transport, out _) is false)
        {
            if (host == "hybrid" && value == BelemetryBrowserTransport.Direct)
            {
                notes.Add("Direct transport in Hybrid makes the WebView post to the collector itself: it needs CORS for the app's origin (e.g. https://0.0.0.0) and loses the offline file persistence. Relay (the Hybrid default) is usually right.");
            }

            return value == BelemetryBrowserTransport.Auto ? null : value.ToString();
        }

        notes.Add($"Unknown transport '{transport}'; left at Auto. Use one of: {string.Join(", ", Enum.GetNames<BelemetryBrowserTransport>())}.");
        return null;
    }
}
