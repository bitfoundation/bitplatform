using Boilerplate.Client.Web;

namespace Boilerplate.Tests.PageTests.BlazorWebAssembly;

[TestClass]
public partial class IdentityPagesTests : BlazorServer.IdentityPagesTests
{
    public override BlazorWebAppMode BlazorRenderMode => BlazorWebAppMode.BlazorWebAssembly;
}

[TestClass]
public partial class LocalizationTests : BlazorServer.LocalizationTests
{
    public override BlazorWebAppMode BlazorRenderMode => BlazorWebAppMode.BlazorWebAssembly;

#if MultilingualEnabled == false
    [TestMethod]
    [TestCategory("MultilingualDisabled")]
    public async Task MultilingualDisabled()
    {
        var homePage = new PageModels.MainHomePage(Page, WebAppServerAddress);
        await homePage.Open();
        await homePage.AssertOpen();

        var contains = Extensions.PlaywrightCacheExtensions.ContainsAsset(new(@"\/_framework\/icudt_hybrid\.dat\?v=sha256-.+"));
        Assert.IsFalse(contains, "The 'icudt_hybrid.dat' file must not be loaded when Multilingual is disabled.");
    }
#endif
}
