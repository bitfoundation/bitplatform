namespace Bit.Belemetry.Tests.Mcp.Controllers;

/// <summary>
/// The tools called as C#: what each one answers, and that the answers come from the shipped library and the
/// embedded documents rather than from text that could drift.
/// </summary>
[TestClass]
public class McpControllerTests
{
    private readonly McpController _mcp = new();

    [TestMethod]
    [DataRow("web-app")]
    [DataRow("server")]
    [DataRow("webassembly")]
    [DataRow("auto")]
    [DataRow("static-ssr")]
    [DataRow("standalone-wasm")]
    [DataRow("hybrid")]
    public void SetupGuide_EveryHost_SaysWhereEachPieceGoes(string host)
    {
        var guide = _mcp.GetBelemetrySetupGuide(host);

        Assert.Contains("AddBitBelemetryServices", guide);
        Assert.Contains("dotnet add package Bit.Belemetry", guide);

        // Static SSR pages have no interactive root to host the initializer; every other host needs it.
        if (host != "static-ssr") Assert.Contains("BelemetryInitializer", guide);
        if (host != "hybrid" && host != "standalone-wasm") Assert.Contains("<BelemetryScript />", guide);
    }

    [TestMethod]
    public void SetupGuide_ShowsTheRealFilesOfAWorkingProject()
    {
        var guide = _mcp.GetBelemetrySetupGuide("web-app");

        Assert.Contains("### Samples/Wasm/Bit.Belemetry.Samples.Wasm.Client/Program.cs", guide);
        Assert.IsFalse(guide.Contains("[Truncated", StringComparison.Ordinal), "The guide does not fit in one answer.");
        Assert.Contains(BelemetrySourceCatalog.GetSource("Demo/Client/Routes.razor")!.Trim(), guide);
    }

    [TestMethod]
    public void SetupGuide_AcceptsCommonSpellings_AndNamesTheHostsForAnUnknownOne()
    {
        Assert.Contains("Blazor Hybrid", _mcp.GetBelemetrySetupGuide("MAUI"));
        Assert.Contains("InteractiveWebAssembly", _mcp.GetBelemetrySetupGuide("wasm"));

        var unknown = _mcp.GetBelemetrySetupGuide("winui");

        Assert.Contains("not a known host", unknown);
        Assert.Contains("standalone-wasm", unknown);
    }

    [TestMethod]
    public void ApiDetails_ReflectsTheRealDefaults_FromAFreshOptionsObject()
    {
        var browser = _mcp.GetBelemetryApiDetails("BelemetryBrowserOptions").Details!;
        var fresh = new BelemetryBrowserOptions();

        Assert.AreEqual(fresh.WebVitals ? "true" : "false", browser.Members.Single(m => m.Name == "WebVitals").Default);
        Assert.AreEqual("[\"click\"]", browser.Members.Single(m => m.Name == "InteractionEvents").Default);
        Assert.AreEqual("BelemetryBrowserTransport.Auto", browser.Members.Single(m => m.Name == "Transport").Default);
        Assert.AreEqual("00:00:05", browser.Members.Single(m => m.Name == "FlushInterval").Default);
    }

    [TestMethod]
    public void ApiDetails_CarriesTheXmlDocumentation()
    {
        var consent = _mcp.GetBelemetryApiDetails("Bit.Belemetry.IBelemetryConsent").Details!;

        Assert.AreEqual("Interface", consent.Kind);
        Assert.IsFalse(string.IsNullOrWhiteSpace(consent.Summary));

        var set = consent.Members.Single(m => m.Name == "SetAsync");

        Assert.AreEqual("Method", set.Kind);
        Assert.AreEqual("(BelemetryFeatures features)", set.Signature);
        Assert.IsFalse(string.IsNullOrWhiteSpace(set.Summary), "The method's XML summary was not found.");
    }

    [TestMethod]
    public void ApiDetails_DescribesEnumsComponentsAndExtensionMethods()
    {
        var features = _mcp.GetBelemetryApiDetails("BelemetryFeatures").Details!;

        Assert.AreEqual("Enum", features.Kind);
        Assert.AreEqual("4095", features.Members.Single(m => m.Name == "All").Default);

        var script = _mcp.GetBelemetryApiDetails("BelemetryScript").Details!;

        Assert.AreEqual("Component", script.Kind);
        Assert.IsTrue(script.Members.All(m => m.Kind == "Parameter"));
        Assert.Contains(m => m.Name == "Nonce", script.Members);

        var registration = _mcp.GetBelemetryApiDetails("Belemetry").Details!;

        Assert.StartsWith("(this IServiceCollection services", registration.Members.Single(m => m.Name == "AddBitBelemetryServices").Signature!);
    }

    [TestMethod]
    public void ApiDetails_AnUnknownName_SuggestsTheClosestTypes()
    {
        var result = _mcp.GetBelemetryApiDetails("OtlpOptions");

        Assert.IsNull(result.Details);
        Assert.Contains("BelemetryOtlpExporterOptions", result.Message!);
    }

    [TestMethod]
    public void Generator_WritesASharedConfigure_RegisteredInBothProjects_ForAWebApp()
    {
        var result = _mcp.GenerateBelemetryConfiguration("webassembly", "otlp,console", "opt-in");

        var setup = result.Files.Single(f => f.Path == "Client/BelemetrySetup.cs").Content;

        Assert.Contains("options.Consent.Default = BelemetryFeatures.None;", setup);
        Assert.Contains("options.AddOtlpExporter(\"https://otel.example.com\"", setup);
        Assert.Contains("options.AddConsoleExporter();", setup);

        Assert.Contains(f => f.Path == "Client/Program.cs" && f.Content.Contains("AddBitBelemetryServices(BelemetrySetup.Configure)"), result.Files);
        Assert.Contains(f => f.Path == "Server/Program.cs" && f.Content.Contains("AddBitBelemetryServices(BelemetrySetup.Configure)"), result.Files);
        Assert.Contains(f => f.Content.Contains("<BelemetryScript />"), result.Files);
        Assert.Contains(f => f.Content.Contains("<BelemetryInitializer />"), result.Files);
        Assert.Contains(f => f.Path.EndsWith("ConsentBanner.razor", StringComparison.Ordinal), result.Files);
    }

    [TestMethod]
    public void Generator_TheCodeItWrites_PassesTheReviewer()
    {
        foreach (var host in BelemetrySetupGuide.HostIds)
        {
            var result = _mcp.GenerateBelemetryConfiguration(host, "otlp,azure,sentry,console", "opt-in", "Diagnostics,PageViews", "Relay");
            var code = string.Join("\n", result.Files.Select(f => f.Content));

            Assert.Contains("options.AddSentryExporter(", code, $"{host}: the Sentry exporter asked for was not generated.");

            var review = _mcp.ReviewBelemetryConfiguration(code);

            Assert.DoesNotContain(f => f.Severity is "error" or "warning", review.Findings,
                           $"{host}: {string.Join("; ", review.Findings.Select(f => $"{f.Rule} {f.Message}"))}");
        }
    }

    [TestMethod]
    public void Generator_MapsFeatureNamesToTheEnum_AndReportsUnknownInputs()
    {
        var result = _mcp.GenerateBelemetryConfiguration("server", "otlp,datadog", "maybe", "errors, Analytics, Clicks", "sideways");

        var setup = result.Files.Single(f => f.Path == "BelemetrySetup.cs").Content;

        Assert.Contains("options.Features = BelemetryFeatures.Errors | BelemetryFeatures.Analytics;", setup);
        Assert.Contains(n => n.Contains("datadog", StringComparison.Ordinal), result.Notes);
        Assert.Contains(n => n.Contains("maybe", StringComparison.Ordinal), result.Notes);
        Assert.Contains(n => n.Contains("Clicks", StringComparison.Ordinal), result.Notes);
        Assert.Contains(n => n.Contains("sideways", StringComparison.Ordinal), result.Notes);
    }

    [TestMethod]
    public void Generator_RefusesAnOversizedArgument()
    {
        var result = _mcp.GenerateBelemetryConfiguration("server", string.Join(",", Enumerable.Range(0, 1000).Select(i => "x" + i)));

        Assert.IsEmpty(result.Files);
        Assert.StartsWith("Not generated", result.Notes.Single());
    }

    [TestMethod]
    public void Generator_Hybrid_FlushesOnSleep_AndHasNoScriptTag()
    {
        var result = _mcp.GenerateBelemetryConfiguration("hybrid");

        Assert.Contains(f => f.Content.Contains("OnSleep") && f.Content.Contains("FlushAsync"), result.Files);
        Assert.DoesNotContain(f => f.Content.Contains("<BelemetryScript"), result.Files);
    }

    [TestMethod]
    public void Conventions_FindRowsInTheReadmeAndTheContracts_AndTheConstants()
    {
        var text = _mcp.LookupBelemetryConventions("web_vital");

        Assert.Contains("browser.web_vital", text);
        Assert.Contains("(CLAUDE.md contracts)", text);
        Assert.Contains("BelemetrySemanticConventions.WebVitalName", text);
    }

    [TestMethod]
    public void Conventions_AnEmptyQuery_ListsTheTables()
    {
        var text = _mcp.LookupBelemetryConventions(null);

        Assert.Contains("Feature bits", text);
        Assert.Contains("Storage keys", text);
    }

    [TestMethod]
    public void Conventions_NothingMatching_SaysSo()
    {
        Assert.Contains("Nothing in the reference tables matches", _mcp.LookupBelemetryConventions("zzqqxx"));
    }

    [TestMethod]
    public void SourceFile_ListsReadsAndSlices()
    {
        Assert.Contains("Demo/Client/Pages/ConsentPage.razor", _mcp.GetBelemetrySourceFile());

        var program = _mcp.GetBelemetrySourceFile("Demo\\Server\\Program.cs");

        Assert.Contains("MapOtlpReceiver", program);

        var firstTwo = _mcp.GetBelemetrySourceFile("Demo/Server/Program.cs", 1, 2);

        Assert.HasCount(2, firstTwo.Split('\n'));

        Assert.Contains("is not a single file", _mcp.GetBelemetrySourceFile("Demo/Client/Pages"));
        Assert.Contains("No embedded file matches", _mcp.GetBelemetrySourceFile("nothing/here.cs"));
    }

    [TestMethod]
    public void SourceFile_ReadsTheReadme_WholeOrBySection()
    {
        Assert.StartsWith("# bit Belemetry", _mcp.GetBelemetrySourceFile("README.md"));
        Assert.Contains("bit Belemetry", _mcp.GetBelemetrySourceFile("README.md", 1, 3));

        var exporters = _mcp.GetBelemetrySourceFile("README.md#exporters");

        Assert.StartsWith("## Exporters", exporters);
        Assert.Contains("### OTLP", exporters);

        Assert.Contains("has no section", _mcp.GetBelemetrySourceFile("README.md#no-such-section"));
    }

    [TestMethod]
    public void Search_FindsGuidePagesApiAndConventions_EachWithAFollowUpCall()
    {
        var consent = _mcp.SearchBelemetry("consent banner");

        Assert.Contains(hit => hit.Kind == "page" && hit.Location == "/consent", consent);

        var api = _mcp.SearchBelemetry("BelemetryOtlpExporterOptions", 3);

        Assert.AreEqual("api", api[0].Kind);
        Assert.Contains("GetBelemetryApiDetails", api[0].FollowUp);

        Assert.Contains(hit => hit.Kind == "convention", _mcp.SearchBelemetry("storage keys localStorage"));
        Assert.Contains(hit => hit.Kind == "guide", _mcp.SearchBelemetry("offline persistence"));
        Assert.IsEmpty(_mcp.SearchBelemetry("   "));
    }

    [TestMethod]
    public void Catalog_ListsEveryToolWithItsParameters()
    {
        var catalog = _mcp.GetMcpCatalog();

        Assert.HasCount(7, catalog.Tools);

        var generator = catalog.Tools.Single(t => t.Name == nameof(McpController.GenerateBelemetryConfiguration));

        Assert.AreSequenceEqual(new[] { "host", "exporters", "consent", "features", "transport" }, generator.Parameters.Select(p => p.Name).ToArray());
        Assert.IsTrue(generator.Parameters[0].Required);
        Assert.IsFalse(generator.Parameters[1].Required);
        Assert.IsTrue(catalog.Tools.All(t => t.Parameters.All(p => string.IsNullOrWhiteSpace(p.Description) is false)));
    }
}
