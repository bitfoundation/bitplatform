//+:cnd:noEmit
using Boilerplate.Client.Web;
using Boilerplate.Tests.Extensions;
using Boilerplate.Tests.PageTests.PageModels;
using Boilerplate.Tests.PageTests.PageModels.Layout;

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
        {
            Assert.Inconclusive("Multilingual is enabled. " +
                "You can disable it via <MultilingualEnabled>false</MultilingualEnabled> setting in Directiory.Build.props.");
            return;
        }

        var homePage = new MainHomePage(Page, WebAppServerAddress);
        await homePage.Open();
        await homePage.AssertOpen();

        var contains = PlaywrightAssetCachingExtensions.ContainsAsset("icudt_hybrid");
        Assert.IsFalse(contains, "The 'icudt_hybrid.dat' file must not be loaded when Multilingual is disabled.");
    }
}

[TestClass]
public partial class DownloadSizeTests : PageTestBase
{
    public override BlazorWebAppMode BlazorRenderMode => BlazorWebAppMode.BlazorWebAssembly;
    public override Uri WebAppServerAddress => new("http://localhost:5000/");
    public override bool EnableBlazorWasmCaching => false;

    //#if (sample == true)
    [TestMethod]
    [AutoAuthenticate]
    [AutoStartTestServer(false)]
    public async Task TodoDownloadSize()
    {
        await AssertDownloadSize(new IdentityHomePage(Page, WebAppServerAddress), expectedTotalSizeKB: 3_200, expectedWasmSizeKB: 2_700);
    }
    //#endif
    //#if (module == "Admin")
    [TestMethod]
    [AutoAuthenticate]
    [AutoStartTestServer(false)]
    public async Task AdminDownloadSize()
    {
        await AssertDownloadSize(new IdentityHomePage(Page, WebAppServerAddress), expectedTotalSizeKB: 3_200, expectedWasmSizeKB: 2_700);
    }
    //#else
    [TestMethod]
    [AutoStartTestServer(false)]
    public async Task SimpleDownloadSize()
    {
        await AssertDownloadSize(new MainHomePage(Page, WebAppServerAddress), expectedTotalSizeKB: 3_200, expectedWasmSizeKB: 2_700);
    }
    //#endif

    private async Task AssertDownloadSize<TPage>(TPage page, int expectedTotalSizeKB, int expectedWasmSizeKB, int toleranceKB = 50)
        where TPage : RootLayout
    {
        var downloadSizeTests = Environment.GetEnvironmentVariable(nameof(DownloadSizeTests));
        if (Convert.ToBoolean(downloadSizeTests) is false)
        {
            Assert.Inconclusive("Download size tests are disabled. " +
                "You can enable it via an environment variable `DownloadSizeTests=true`");
            return;
        }

        await using var networkSession = await Page.OpenNetworkSession();

        await page.Open();
        await page.AssertOpen();

        await Page.WaitForHydrationToComplete();
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var totalSizeKB = networkSession.TotalDownloaded / 1000;
        var wasmSizeKB = networkSession.DownloadedResponses
            .Where(x => PlaywrightAssetCachingExtensions.BlazorWasmRegex().IsMatch(new Uri(x.Url).PathAndQuery))
            .Sum(x => x.EncodedDataLength) / 1000;

        Assert.AreEqual(expectedTotalSizeKB, totalSizeKB, toleranceKB, "Total size is not within tolerance.");
        Assert.AreEqual(expectedWasmSizeKB, wasmSizeKB, toleranceKB, "Wasm size is not within tolerance.");

        Console.WriteLine($"Total size: {totalSizeKB} KB");
    }
}
