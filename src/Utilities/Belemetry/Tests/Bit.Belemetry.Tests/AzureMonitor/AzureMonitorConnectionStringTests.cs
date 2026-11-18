namespace Bit.Belemetry.Tests.AzureMonitor;

[TestClass]
public class AzureMonitorConnectionStringTests
{
    private const string IKey = "11111111-2222-3333-4444-555555555555";

    [TestMethod]
    public void Defaults()
    {
        var cs = BelemetryAzureMonitorConnectionString.Parse($"InstrumentationKey={IKey}");
        Assert.AreEqual(IKey, cs.InstrumentationKey);
        Assert.AreEqual(new Uri("https://dc.services.visualstudio.com/"), cs.IngestionEndpoint);
        Assert.AreEqual(new Uri("https://rt.services.visualstudio.com/"), cs.LiveEndpoint);
        Assert.AreEqual("https://dc.services.visualstudio.com/v2.1/track", cs.TrackUrl);
    }

    [TestMethod]
    public void ExplicitEndpoints_KeysAreCaseInsensitive()
    {
        var cs = BelemetryAzureMonitorConnectionString.Parse($" instrumentationkey = {IKey} ; IngestionEndpoint=https://westeurope-5.in.applicationinsights.azure.com;LIVEENDPOINT=https://westeurope.livediagnostics.monitor.azure.com/;ApplicationId=abc;");
        Assert.AreEqual(IKey, cs.InstrumentationKey);
        Assert.AreEqual("https://westeurope-5.in.applicationinsights.azure.com/v2.1/track", cs.TrackUrl);
        Assert.AreEqual(new Uri("https://westeurope.livediagnostics.monitor.azure.com/"), cs.LiveEndpoint);
    }

    [TestMethod]
    public void EndpointSuffix_DerivesEndpoints()
    {
        var cs = BelemetryAzureMonitorConnectionString.Parse($"InstrumentationKey={IKey};EndpointSuffix=applicationinsights.us;Location=usgovvirginia");
        Assert.AreEqual(new Uri("https://usgovvirginia.dc.applicationinsights.us/"), cs.IngestionEndpoint);
        Assert.AreEqual(new Uri("https://usgovvirginia.live.applicationinsights.us/"), cs.LiveEndpoint);

        var noLocation = BelemetryAzureMonitorConnectionString.Parse($"InstrumentationKey={IKey};EndpointSuffix=.applicationinsights.azure.cn.");
        Assert.AreEqual(new Uri("https://dc.applicationinsights.azure.cn/"), noLocation.IngestionEndpoint);

        var explicitWins = BelemetryAzureMonitorConnectionString.Parse($"InstrumentationKey={IKey};EndpointSuffix=applicationinsights.us;IngestionEndpoint=https://custom.test/");
        Assert.AreEqual(new Uri("https://custom.test/"), explicitWins.IngestionEndpoint);
        Assert.AreEqual(new Uri("https://live.applicationinsights.us/"), explicitWins.LiveEndpoint);
    }

    [TestMethod]
    public void InvalidInstrumentationKey_Throws()
    {
        Assert.ThrowsExactly<FormatException>(() => BelemetryAzureMonitorConnectionString.Parse("InstrumentationKey=not-a-guid"));
        Assert.ThrowsExactly<FormatException>(() => BelemetryAzureMonitorConnectionString.Parse("IngestionEndpoint=https://x.test/"));
        Assert.ThrowsExactly<FormatException>(() => BelemetryAzureMonitorConnectionString.Parse("garbage"));
        Assert.Throws<ArgumentException>(() => BelemetryAzureMonitorConnectionString.Parse(" "));
    }

    [TestMethod]
    public void InvalidEndpoint_FallsBackToTheDefault()
    {
        var cs = BelemetryAzureMonitorConnectionString.Parse($"InstrumentationKey={IKey};IngestionEndpoint=not a uri");
        Assert.AreEqual(new Uri("https://dc.services.visualstudio.com/"), cs.IngestionEndpoint);
    }
}
