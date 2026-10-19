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

        StringAssert.Contains(guide, "AddBitBelemetryServices");
        StringAssert.Contains(guide, "dotnet add package Bit.Belemetry");

        // Static SSR pages have no interactive root to host the initializer; every other host needs it.
        if (host != "static-ssr") StringAssert.Contains(guide, "BelemetryInitializer");
        if (host != "hybrid" && host != "standalone-wasm") StringAssert.Contains(guide, "<BelemetryScript />");
    }

    [TestMethod]
    public void SetupGuide_ShowsTheRealFilesOfAWorkingProject()
    {
        var guide = _mcp.GetBelemetrySetupGuide("web-app");

        StringAssert.Contains(guide, "### Samples/Wasm/Bit.Belemetry.Samples.Wasm.Client/Program.cs");
        Assert.IsFalse(guide.Contains("[Truncated", StringComparison.Ordinal), "The guide does not fit in one answer.");
        StringAssert.Contains(guide, BelemetrySourceCatalog.GetSource("Demo/Client/Routes.razor")!.Trim());
    }

    [TestMethod]
    public void SetupGuide_AcceptsCommonSpellings_AndNamesTheHostsForAnUnknownOne()
    {
        StringAssert.Contains(_mcp.GetBelemetrySetupGuide("MAUI"), "Blazor Hybrid");
        StringAssert.Contains(_mcp.GetBelemetrySetupGuide("wasm"), "InteractiveWebAssembly");

        var unknown = _mcp.GetBelemetrySetupGuide("winui");

        StringAssert.Contains(unknown, "not a known host");
        StringAssert.Contains(unknown, "standalone-wasm");
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
        Assert.IsTrue(script.Members.Any(m => m.Name == "Nonce"));

        var registration = _mcp.GetBelemetryApiDetails("Belemetry").Details!;

        StringAssert.StartsWith(registration.Members.Single(m => m.Name == "AddBitBelemetryServices").Signature!, "(this IServiceCollection services");
    }

    [TestMethod]
    public void ApiDetails_AnUnknownName_SuggestsTheClosestTypes()
    {
        var result = _mcp.GetBelemetryApiDetails("OtlpOptions");

        Assert.IsNull(result.Details);
        StringAssert.Contains(result.Message!, "BelemetryOtlpExporterOptions");
    }

    [TestMethod]
    public void Generator_WritesASharedConfigure_RegisteredInBothProjects_ForAWebApp()
    {
        var result = _mcp.GenerateBelemetryConfiguration("webassembly", "otlp,console", "opt-in");

        var setup = result.Files.Single(f => f.Path == "Client/BelemetrySetup.cs").Content;

        StringAssert.Contains(setup, "options.Consent.Default = BelemetryFeatures.None;");
        StringAssert.Contains(setup, "options.AddOtlpExporter(\"https://otel.example.com\"");
        StringAssert.Contains(setup, "options.AddConsoleExporter();");

        Assert.IsTrue(result.Files.Any(f => f.Path == "Client/Program.cs" && f.Content.Contains("AddBitBelemetryServices(BelemetrySetup.Configure)")));
        Assert.IsTrue(result.Files.Any(f => f.Path == "Server/Program.cs" && f.Content.Contains("AddBitBelemetryServices(BelemetrySetup.Configure)")));
        Assert.IsTrue(result.Files.Any(f => f.Content.Contains("<BelemetryScript />")));
        Assert.IsTrue(result.Files.Any(f => f.Content.Contains("<BelemetryInitializer />")));
        Assert.IsTrue(result.Files.Any(f => f.Path.EndsWith("ConsentBanner.razor", StringComparison.Ordinal)));
    }

    [TestMethod]
    public void Generator_TheCodeItWrites_PassesTheReviewer()
    {
        foreach (var host in BelemetrySetupGuide.HostIds)
        {
            var result = _mcp.GenerateBelemetryConfiguration(host, "otlp,azure,console", "opt-in", "Diagnostics,PageViews", "Relay");
            var code = string.Join("\n", result.Files.Select(f => f.Content));

            var review = _mcp.ReviewBelemetryConfiguration(code);

            Assert.IsFalse(review.Findings.Any(f => f.Severity is "error" or "warning"),
                           $"{host}: {string.Join("; ", review.Findings.Select(f => $"{f.Rule} {f.Message}"))}");
        }
    }

    [TestMethod]
    public void Generator_MapsFeatureNamesToTheEnum_AndReportsUnknownInputs()
    {
        var result = _mcp.GenerateBelemetryConfiguration("server", "otlp,datadog", "maybe", "errors, Analytics, Clicks", "sideways");

        var setup = result.Files.Single(f => f.Path == "BelemetrySetup.cs").Content;

        StringAssert.Contains(setup, "options.Features = BelemetryFeatures.Errors | BelemetryFeatures.Analytics;");
        Assert.IsTrue(result.Notes.Any(n => n.Contains("datadog", StringComparison.Ordinal)));
        Assert.IsTrue(result.Notes.Any(n => n.Contains("maybe", StringComparison.Ordinal)));
        Assert.IsTrue(result.Notes.Any(n => n.Contains("Clicks", StringComparison.Ordinal)));
        Assert.IsTrue(result.Notes.Any(n => n.Contains("sideways", StringComparison.Ordinal)));
    }

    [TestMethod]
    public void Generator_Hybrid_FlushesOnSleep_AndHasNoScriptTag()
    {
        var result = _mcp.GenerateBelemetryConfiguration("hybrid");

        Assert.IsTrue(result.Files.Any(f => f.Content.Contains("OnSleep") && f.Content.Contains("FlushAsync")));
        Assert.IsFalse(result.Files.Any(f => f.Content.Contains("<BelemetryScript")));
    }

    [TestMethod]
    public void Conventions_FindRowsInTheReadmeAndTheContracts_AndTheConstants()
    {
        var text = _mcp.LookupBelemetryConventions("web_vital");

        StringAssert.Contains(text, "browser.web_vital");
        StringAssert.Contains(text, "(CLAUDE.md contracts)");
        StringAssert.Contains(text, "BelemetrySemanticConventions.WebVitalName");
    }

    [TestMethod]
    public void Conventions_AnEmptyQuery_ListsTheTables()
    {
        var text = _mcp.LookupBelemetryConventions(null);

        StringAssert.Contains(text, "Feature bits");
        StringAssert.Contains(text, "Storage keys");
    }

    [TestMethod]
    public void Conventions_NothingMatching_SaysSo()
    {
        StringAssert.Contains(_mcp.LookupBelemetryConventions("zzqqxx"), "Nothing in the reference tables matches");
    }

    [TestMethod]
    public void SourceFile_ListsReadsAndSlices()
    {
        StringAssert.Contains(_mcp.GetBelemetrySourceFile(), "Demo/Client/Pages/ConsentPage.razor");

        var program = _mcp.GetBelemetrySourceFile("Demo\\Server\\Program.cs");

        StringAssert.Contains(program, "MapOtlpReceiver");

        var firstTwo = _mcp.GetBelemetrySourceFile("Demo/Server/Program.cs", 1, 2);

        Assert.AreEqual(2, firstTwo.Split('\n').Length);

        StringAssert.Contains(_mcp.GetBelemetrySourceFile("Demo/Client/Pages"), "is not a single file");
        StringAssert.Contains(_mcp.GetBelemetrySourceFile("nothing/here.cs"), "No embedded file matches");
    }

    [TestMethod]
    public void SourceFile_ReadsTheReadme_WholeOrBySection()
    {
        StringAssert.StartsWith(_mcp.GetBelemetrySourceFile("README.md"), "# bit Belemetry");
        StringAssert.Contains(_mcp.GetBelemetrySourceFile("README.md", 1, 3), "bit Belemetry");

        var exporters = _mcp.GetBelemetrySourceFile("README.md#exporters");

        StringAssert.StartsWith(exporters, "## Exporters");
        StringAssert.Contains(exporters, "### OTLP");

        StringAssert.Contains(_mcp.GetBelemetrySourceFile("README.md#no-such-section"), "has no section");
    }

    [TestMethod]
    public void Search_FindsGuidePagesApiAndConventions_EachWithAFollowUpCall()
    {
        var consent = _mcp.SearchBelemetry("consent banner");

        Assert.IsTrue(consent.Any(hit => hit.Kind == "page" && hit.Location == "/consent"));

        var api = _mcp.SearchBelemetry("BelemetryOtlpExporterOptions", 3);

        Assert.AreEqual("api", api[0].Kind);
        StringAssert.Contains(api[0].FollowUp, "GetBelemetryApiDetails");

        Assert.IsTrue(_mcp.SearchBelemetry("storage keys localStorage").Any(hit => hit.Kind == "convention"));
        Assert.IsTrue(_mcp.SearchBelemetry("offline persistence").Any(hit => hit.Kind == "guide"));
        Assert.AreEqual(0, _mcp.SearchBelemetry("   ").Length);
    }

    [TestMethod]
    public void Catalog_ListsEveryToolWithItsParameters()
    {
        var catalog = _mcp.GetMcpCatalog();

        Assert.AreEqual(7, catalog.Tools.Length);

        var generator = catalog.Tools.Single(t => t.Name == nameof(McpController.GenerateBelemetryConfiguration));

        CollectionAssert.AreEqual(new[] { "host", "exporters", "consent", "features", "transport" }, generator.Parameters.Select(p => p.Name).ToArray());
        Assert.IsTrue(generator.Parameters[0].Required);
        Assert.IsFalse(generator.Parameters[1].Required);
        Assert.IsTrue(catalog.Tools.All(t => t.Parameters.All(p => string.IsNullOrWhiteSpace(p.Description) is false)));
    }
}
