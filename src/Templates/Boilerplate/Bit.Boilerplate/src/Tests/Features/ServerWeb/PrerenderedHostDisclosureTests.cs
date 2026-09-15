using System.Runtime.InteropServices;

namespace Boilerplate.Tests.Features.ServerWeb;

/// <summary>
/// While a page prerenders it runs in the web server's process, so whatever the client asks its own runtime is
/// answered for the host. <c>/about</c> and <c>/diagnostic</c> both ask exactly that, both are anonymous, and the html
/// they emit is public.
/// </summary>
[TestClass, TestCategory("IntegrationTest"), TestCategory("PreRendering")]
public class PrerenderedHostDisclosureTests
{
    public TestContext TestContext { get; set; } = default!;

    [TestMethod]
    public async Task PrerenderedPages_Should_NameAGenericServer_RatherThanTheHostsOperatingSystem()
    {
        await using var server = new AppTestServer();
        await server.Build(
            configureTestServices: services => services.AddIntegrationApiOnlyTestsServices().FakeExternalStatistics(),
            configureTestConfigurations: configuration => configuration["WebAppRender:PrerenderEnabled"] = "true")
            .Start(TestContext.CancellationToken);

        // A bare HttpClient, so what is asserted is what the server wrote and nothing a browser would do afterwards.
        using var visitorHttpClient = new HttpClient { BaseAddress = server.WebAppServerAddress };

        // The very string the client would report about itself once it is running on a device.
        var hostOperatingSystem = RuntimeInformation.OSDescription;

        var about = await visitorHttpClient.GetStringAsync(PageUrls.About, TestContext.CancellationToken);

        Assert.Contains("Generic Server", about,
            "The about page prerenders with no device to describe, so it should say so rather than leave the field to whatever ITelemetryContext holds.");

        Assert.DoesNotContain(hostOperatingSystem, about,
            $"The about page told an anonymous visitor the server runs '{hostOperatingSystem}', on a page whose subject is the visitor's own device.");

        var diagnostic = await visitorHttpClient.GetStringAsync("/diagnostic", TestContext.CancellationToken);

        Assert.DoesNotContain(hostOperatingSystem, diagnostic,
            $"The diagnostic page prerendered the server's '{hostOperatingSystem}' as the client's Platform, and that page is anonymous too.");
    }
}
