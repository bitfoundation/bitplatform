using Boilerplate.Tests.PageTests.PageModels;

namespace Boilerplate.Tests.PageTests.BlazorServer.PreRendering;

[TestClass]
public partial class IdentityPagesTests : BlazorServer.IdentityPagesTests
{
    public override bool PreRenderEnabled => true;
}

[TestClass]
public partial class LocalizationTests : BlazorServer.LocalizationTests
{
    public override bool PreRenderEnabled => true;
}

[TestClass]
public partial class PreRenderingTests : PageTestBase
{
    public override bool PreRenderEnabled => true;

    [TestMethod]
    [ConfigureBrowserContext(nameof(JavaScriptDisabled))]
    public async Task PreRenderedHtml()
    {
        var homePage = new MainHomePage(Page, WebAppServerAddress);
        await homePage.Open();
        await homePage.AssertOpen();
    }

    public async Task JavaScriptDisabled(BrowserNewContextOptions options) => options.JavaScriptEnabled = false;
}
