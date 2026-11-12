namespace Bit.Belemetry.Tests.Mcp.Services;

/// <summary>Each review rule fires on the mistake it is about, and stays quiet on the correct form of the same code.</summary>
[TestClass]
public class CodeReviewTests
{
    private static string[] Rules(string code) => [.. BelemetryCodeReview.Review(code).Findings.Select(f => f.Rule)];

    [TestMethod]
    public void OldNames_AreReported()
    {
        Assert.Contains("BLM001", Rules("builder.Services.AddBelemetryServices();"));
        Assert.Contains("BLM001", Rules("window.Belemetry.getContext()"));
        Assert.DoesNotContain("BLM001", Rules("builder.Services.AddBitBelemetryServices(); BitBelemetry.getContext();"));
        Assert.DoesNotContain("BLM001", Rules("// builder.Services.AddBelemetryServices();"));
    }

    [TestMethod]
    public void ASignalPathInTheBaseEndpoint_IsAnError_ButAFullTracesEndpointIsNot()
    {
        var finding = BelemetryCodeReview.Review("o.AddOtlpExporter(\"http://collector:4318/v1/traces\");").Findings.Single(f => f.Rule == "BLM002");

        Assert.AreEqual("error", finding.Severity);
        Assert.AreEqual(1, finding.Line);

        Assert.Contains("BLM002", Rules("otlp.Endpoint = new Uri(\"https://otel.example.com/v1/logs\");"));
        Assert.DoesNotContain("BLM002", Rules("otlp.TracesEndpoint = new Uri(\"https://otel.example.com/v1/traces\");"));
        Assert.DoesNotContain("BLM002", Rules("// o.AddOtlpExporter(\"http://collector:4318/v1/traces\");"));
    }

    [TestMethod]
    public void CredentialHeaders_AreAnError_UnlessTheBrowserNeverSeesThem()
    {
        const string Keyed = "o.AddOtlpExporter(\"https://api.honeycomb.io\", otlp => otlp.Headers[\"x-honeycomb-team\"] = key);";

        Assert.Contains("BLM003", Rules(Keyed));
        Assert.DoesNotContain("BLM003", Rules(Keyed + "\no.Browser.Transport = BelemetryBrowserTransport.Relay;"));
        Assert.DoesNotContain("BLM003", Rules(Keyed + "\notlp.IncludeInBrowser = false;"));
        Assert.DoesNotContain("BLM003", Rules("otlp.Headers[\"x-tenant\"] = \"contoso\";"));
    }

    [TestMethod]
    public void AConsentDefaultThatGrantsAnalytics_IsReported_ButErrorsIsNot()
    {
        Assert.Contains("BLM004", Rules("o.Consent.Default = BelemetryFeatures.All;"));
        Assert.Contains("BLM004", Rules("o.Consent.Default = BelemetryFeatures.Errors | BelemetryFeatures.PageViews;"));
        Assert.DoesNotContain("BLM004", Rules("o.Consent.Default = BelemetryFeatures.Errors;"));
        Assert.DoesNotContain("BLM004", Rules("o.Consent.Default = BelemetryFeatures.None;"));
    }

    [TestMethod]
    public void AMissingScriptOrInitializer_IsReported_OnlyWhenTheFileItBelongsInIsThere()
    {
        Assert.Contains("BLM005", Rules("<html><head><HeadOutlet /></head></html>"));
        Assert.DoesNotContain("BLM005", Rules("<html><head><BelemetryScript /><HeadOutlet /></head></html>"));
        Assert.DoesNotContain("BLM005", Rules("builder.Services.AddBitBelemetryServices();"));

        Assert.Contains("BLM006", Rules("<Router AppAssembly=\"typeof(Program).Assembly\" />"));
        Assert.DoesNotContain("BLM006", Rules("<Router AppAssembly=\"typeof(Program).Assembly\" />\n<BelemetryInitializer />"));

        Assert.Contains("BLM007", Rules("<BelemetryInitializer />\n<BelemetryInitializer />"));
    }

    [TestMethod]
    public void AServerHostWithoutTheServerPipeline_GetsANote()
    {
        const string Server = "var builder = WebApplication.CreateBuilder(args);\nbuilder.Services.AddBitBelemetryServices(o => { });";

        var finding = BelemetryCodeReview.Review(Server).Findings.Single(f => f.Rule == "BLM008");

        Assert.AreEqual("info", finding.Severity);
        Assert.AreEqual(2, finding.Line);
        Assert.DoesNotContain("BLM008", Rules(Server + "\no.Server.CollectDotnetTelemetry = true;"));
    }

    [TestMethod]
    public void TheRemainingRules_FireOnTheirMistakes()
    {
        Assert.Contains("BLM009", Rules("o.AddOtlpExporter(\"/otlp\");"));
        Assert.Contains("BLM010", Rules("o.AddOtlpExporter(\"http://localhost:4317\");"));
        Assert.Contains("BLM011", Rules("o.Browser.CaptureInteractionText = true;"));
        Assert.Contains("BLM012", Rules("o.Privacy.ScrubPatterns = false;"));
        Assert.Contains("BLM013", Rules("o.Browser.SessionSampleRatio = 25;"));
        Assert.DoesNotContain("BLM013", Rules("o.Browser.SessionSampleRatio = 0.25;"));
        Assert.Contains("BLM014", Rules("Belemetry.FlushAsync().GetAwaiter().GetResult();"));
        Assert.Contains("BLM015", Rules("azure.AccessTokenProvider = GetTokenAsync;"));
        Assert.Contains("BLM016", Rules("o.Persistence.Directory = \"wwwroot/telemetry\";"));
    }

    [TestMethod]
    public void ACorrectSetup_HasNoErrorsOrWarnings_AndFindingsAreInLineOrder()
    {
        const string Correct = """
            builder.Services.AddBitBelemetryServices(o =>
            {
                o.ServiceName = "Shop";
                o.Consent.Default = BelemetryFeatures.None;
                o.AddOtlpExporter("http://localhost:4318", otlp => otlp.Protocol = BelemetryOtlpProtocol.HttpJson);
            });
            <head><BelemetryScript /></head>
            <Router AppAssembly="typeof(Program).Assembly" />
            <BelemetryInitializer />
            """;

        Assert.IsEmpty(BelemetryCodeReview.Review(Correct).Findings);

        var findings = BelemetryCodeReview.Review("o.Consent.Default = BelemetryFeatures.All;\no.AddOtlpExporter(\"http://c:4318/v1/traces\");").Findings;

        Assert.AreSequenceEqual(new int?[] { 1, 2 }, findings.Select(f => f.Line).ToArray());
        Assert.Contains("Nothing to review", BelemetryCodeReview.Review("").Summary);
    }

    [TestMethod]
    public void AnOversizedSnippet_IsRefused_AndEachRulesFindingsAreCapped()
    {
        var oversized = BelemetryCodeReview.Review(new string('x', 64 * 1024 + 1));
        Assert.StartsWith("Not reviewed", oversized.Summary);
        Assert.IsEmpty(oversized.Findings);

        var repeated = BelemetryCodeReview.Review(string.Concat(Enumerable.Repeat("builder.Services.AddBelemetryServices();\n", 500)) + "o.Consent.Default = BelemetryFeatures.All;");
        Assert.HasCount(10, repeated.Findings.Where(f => f.Rule == "BLM001"));
        Assert.Contains("BLM004", repeated.Findings.Select(f => f.Rule).ToArray(), "A flood of one rule's findings does not hide another rule's.");
        Assert.Contains("at most 10", repeated.Summary);
    }
}
