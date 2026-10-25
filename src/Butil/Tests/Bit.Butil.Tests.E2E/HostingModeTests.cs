using Bit.Butil.Tests.E2E.Infrastructure;
using Microsoft.Playwright;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.Butil.Tests.E2E;

/// <summary>
/// That the run is exercising the host it asked for (<c>BUTIL_E2E_HOST</c>), and what is specific to how that host
/// gets the page on screen. Every other fixture assumes both: a Server run that silently served WebAssembly, or a
/// prerendered page clicked before it was interactive, would make the whole suite prove less than it claims.
/// </summary>
[TestClass]
public class HostingModeTests : ButilPageTest
{
    [TestMethod]
    public async Task The_Page_Runs_On_The_Runtime_The_Host_Is_Meant_To_Use()
    {
        var status = Page.Locator("#status");

        var platform = await status.GetAttributeAsync("data-platform");
        Assert.Contains(platform!, HarnessHostKinds.ExpectedPlatforms(Host), $"{Host} ran the page on '{platform}'.");

        // RendererInfo arrived in .NET 9; the page reports "unknown" before that.
        var renderer = await status.GetAttributeAsync("data-renderer");
        if (renderer is not "unknown")
        {
            Assert.Contains(renderer!, HarnessHostKinds.ExpectedRenderers(Host), $"{Host} rendered the page with '{renderer}'.");
        }
    }

    [TestMethod]
    public async Task The_First_Response_Is_Prerendered_Exactly_Where_The_Host_Prerenders()
    {
        if (Host is HarnessHostKinds.Hybrid)
            Assert.Inconclusive("A BlazorWebView page is served from the WebView's virtual host, not by an HTTP response.");

        var response = await Page.Context.APIRequest.GetAsync(Page.Url);
        var html = await response.TextAsync();

        var prerendered = html.Contains("id=\"status\"", StringComparison.Ordinal);
        Assert.AreEqual(HarnessHostKinds.Prerenders(Host), prerendered,
            prerendered ? $"{Host} is not meant to prerender, but its first response carries the page." : $"{Host} is meant to prerender, but its first response does not carry the page.");
    }

    [TestMethod]
    public async Task The_First_Calls_After_The_Page_Turns_Interactive_Reach_The_Browser()
    {
        // The window right after hydration is where the runtimes differ: a Server circuit's runtime reports itself
        // initialized, a BlazorWebView's attaches its IPC channel. A call Butil still treated as "prerendering" there
        // would come back as a silent safe default, so both a write and a read are checked against the page itself.
        await ClickAndExpectAsync("doc-title", "doc:title:butil-e2e-title");
        await Assertions.Expect(Page).ToHaveTitleAsync("butil-e2e-title");

        // True: the href Location reads back contains the harness route.
        await ClickAndExpectAsync("loc-href", "loc:href:True");
    }
}
