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
    [AutoAuthenticate]
    public async Task PreRenderedHtml()
    {
        await Page.GotoAsync($"view-source:{WebAppServerAddress}");

        await Assertions.Expect(Page.GetByText(TestData.DefaultTestEmail).First).ToBeVisibleAsync();
        await Assertions.Expect(Page.GetByText(TestData.DefaultTestFullName).First).ToBeVisibleAsync();
    }
}
