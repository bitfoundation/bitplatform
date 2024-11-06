using Boilerplate.Client.Web;
using Boilerplate.Tests.Extensions;
using Boilerplate.Tests.PageTests.PageModels;

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

    [TestMethod]
    [TestCategory("MultilingualDisabled")]
    public async Task MultilingualDisabled()
    {
        if (CultureInfoManager.MultilingualEnabled)
            Assert.Inconclusive("Ignored because Multilingual is enabled");

        var homePage = new MainHomePage(Page, WebAppServerAddress);
        await homePage.Open();
        await homePage.AssertOpen();

        var contains = PlaywrightCacheExtensions.ContainsAsset(new(@"\/_framework\/icudt_hybrid\.dat\?v=sha256-.+"));
        Assert.IsFalse(contains, "The 'icudt_hybrid.dat' file must not be loaded when Multilingual is disabled.");
    }
}
