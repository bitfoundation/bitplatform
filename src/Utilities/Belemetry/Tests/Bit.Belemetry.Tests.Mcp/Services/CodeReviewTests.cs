namespace Bit.Belemetry.Tests.Mcp.Services;

/// <summary>Each review rule fires on the mistake it is about, and stays quiet on the correct form of the same code.</summary>
[TestClass]
public class CodeReviewTests
{
    private static string[] Rules(string code) => [.. BelemetryCodeReview.Review(code).Findings.Select(f => f.Rule)];

    [TestMethod]
    public void OldNames_AreReported()
    {
        CollectionAssert.Contains(Rules("builder.Services.AddBelemetryServices();"), "BLM001");
        CollectionAssert.Contains(Rules("window.Belemetry.getContext()"), "BLM001");
        CollectionAssert.DoesNotContain(Rules("builder.Services.AddBitBelemetryServices(); BitBelemetry.getContext();"), "BLM001");
    }

    [TestMethod]
    public void ASignalPathInTheBaseEndpoint_IsAnError_ButAFullTracesEndpointIsNot()
    {
        var finding = BelemetryCodeReview.Review("o.AddOtlpExporter(\"http://collector:4318/v1/traces\");").Findings.Single(f => f.Rule == "BLM002");

        Assert.AreEqual("error", finding.Severity);
        Assert.AreEqual(1, finding.Line);

        CollectionAssert.Contains(Rules("otlp.Endpoint = new Uri(\"https://otel.example.com/v1/logs\");"), "BLM002");
        CollectionAssert.DoesNotContain(Rules("otlp.TracesEndpoint = new Uri(\"https://otel.example.com/v1/traces\");"), "BLM002");
        CollectionAssert.DoesNotContain(Rules("// o.AddOtlpExporter(\"http://collector:4318/v1/traces\");"), "BLM002");
    }

    [TestMethod]
    public void CredentialHeaders_AreAnError_UnlessTheBrowserNeverSeesThem()
    {
        const string Keyed = "o.AddOtlpExporter(\"https://api.honeycomb.io\", otlp => otlp.Headers[\"x-honeycomb-team\"] = key);";

        CollectionAssert.Contains(Rules(Keyed), "BLM003");
        CollectionAssert.DoesNotContain(Rules(Keyed + "\no.Browser.Transport = BelemetryBrowserTransport.Relay;"), "BLM003");
        CollectionAssert.DoesNotContain(Rules(Keyed + "\notlp.IncludeInBrowser = false;"), "BLM003");
        CollectionAssert.DoesNotContain(Rules("otlp.Headers[\"x-tenant\"] = \"contoso\";"), "BLM003");
    }

    [TestMethod]
    public void AConsentDefaultThatGrantsAnalytics_IsReported_ButErrorsIsNot()
    {
        CollectionAssert.Contains(Rules("o.Consent.Default = BelemetryFeatures.All;"), "BLM004");
        CollectionAssert.Contains(Rules("o.Consent.Default = BelemetryFeatures.Errors | BelemetryFeatures.PageViews;"), "BLM004");
        CollectionAssert.DoesNotContain(Rules("o.Consent.Default = BelemetryFeatures.Errors;"), "BLM004");
        CollectionAssert.DoesNotContain(Rules("o.Consent.Default = BelemetryFeatures.None;"), "BLM004");
    }

    [TestMethod]
    public void AMissingScriptOrInitializer_IsReported_OnlyWhenTheFileItBelongsInIsThere()
    {
        CollectionAssert.Contains(Rules("<html><head><HeadOutlet /></head></html>"), "BLM005");
        CollectionAssert.DoesNotContain(Rules("<html><head><BelemetryScript /><HeadOutlet /></head></html>"), "BLM005");
        CollectionAssert.DoesNotContain(Rules("builder.Services.AddBitBelemetryServices();"), "BLM005");

        CollectionAssert.Contains(Rules("<Router AppAssembly=\"typeof(Program).Assembly\" />"), "BLM006");
        CollectionAssert.DoesNotContain(Rules("<Router AppAssembly=\"typeof(Program).Assembly\" />\n<BelemetryInitializer />"), "BLM006");

        CollectionAssert.Contains(Rules("<BelemetryInitializer />\n<BelemetryInitializer />"), "BLM007");
    }

    [TestMethod]
    public void AServerHostWithoutTheServerPipeline_GetsANote()
    {
        const string Server = "var builder = WebApplication.CreateBuilder(args);\nbuilder.Services.AddBitBelemetryServices(o => { });";

        var finding = BelemetryCodeReview.Review(Server).Findings.Single(f => f.Rule == "BLM008");

        Assert.AreEqual("info", finding.Severity);
        Assert.AreEqual(2, finding.Line);
        CollectionAssert.DoesNotContain(Rules(Server + "\no.Server.CollectDotnetTelemetry = true;"), "BLM008");
    }

    [TestMethod]
    public void TheRemainingRules_FireOnTheirMistakes()
    {
        CollectionAssert.Contains(Rules("o.AddOtlpExporter(\"/otlp\");"), "BLM009");
        CollectionAssert.Contains(Rules("o.AddOtlpExporter(\"http://localhost:4317\");"), "BLM010");
        CollectionAssert.Contains(Rules("o.Browser.CaptureInteractionText = true;"), "BLM011");
        CollectionAssert.Contains(Rules("o.Privacy.ScrubPatterns = false;"), "BLM012");
        CollectionAssert.Contains(Rules("o.Browser.SessionSampleRatio = 25;"), "BLM013");
        CollectionAssert.DoesNotContain(Rules("o.Browser.SessionSampleRatio = 0.25;"), "BLM013");
        CollectionAssert.Contains(Rules("Belemetry.FlushAsync().GetAwaiter().GetResult();"), "BLM014");
        CollectionAssert.Contains(Rules("azure.AccessTokenProvider = GetTokenAsync;"), "BLM015");
        CollectionAssert.Contains(Rules("o.Persistence.Directory = \"wwwroot/telemetry\";"), "BLM016");
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

        Assert.AreEqual(0, BelemetryCodeReview.Review(Correct).Findings.Length);

        var findings = BelemetryCodeReview.Review("o.Consent.Default = BelemetryFeatures.All;\no.AddOtlpExporter(\"http://c:4318/v1/traces\");").Findings;

        CollectionAssert.AreEqual(new int?[] { 1, 2 }, findings.Select(f => f.Line).ToArray());
        StringAssert.Contains(BelemetryCodeReview.Review("").Summary, "Nothing to review");
    }
}
