using System.Reflection;
using Boilerplate.Tests.PageTests.PageModels;
using Boilerplate.Tests.Services;

namespace Boilerplate.Tests.PageTests;

[TestClass]
public partial class LocalizationTests : PageTestBase
{
    [TestMethod]
    [CultureData]
    [ConfigureBrowserContext(nameof(SetCultureInBrowserContext))]
    public async Task AcceptLanguageHeader(string cultureName, string cultureDisplayName)
    {
        var localizer = StringLocalizerFactory.Create<AppStrings>(cultureName);
        var homePage = new MainHomePage(Page, WebAppServerAddress);
        await homePage.Open();
        await homePage.AssertLocalized(localizer, cultureName, cultureDisplayName);
    }
    private async Task SetCultureInBrowserContext(BrowserNewContextOptions options, string cultureName, string _) => options.Locale = cultureName;

    [TestMethod]
    [CultureData]
    public async Task LanguageDropDown(string cultureName, string cultureDisplayName)
    {
        var localizer = StringLocalizerFactory.Create<AppStrings>(cultureName);
        var homePage = new MainHomePage(Page, WebAppServerAddress);
        await homePage.Open();
        await homePage.ChangeCulture(cultureDisplayName);
        await homePage.AssertLocalized(localizer, cultureName, cultureDisplayName);
    }

    [TestMethod]
    [CultureData]
    public async Task QueryString(string cultureName, string cultureDisplayName)
    {
        var localizer = StringLocalizerFactory.Create<AppStrings>(cultureName);
        var homePage = new MainHomePage(Page, WebAppServerAddress);
        await Page.GotoAsync($"{WebAppServerAddress}?culture={cultureName}");
        await homePage.AssertLocalized(localizer, cultureName, cultureDisplayName);
    }

    [TestMethod]
    [CultureData]
    public async Task UrlSegment(string cultureName, string cultureDisplayName)
    {
        var localizer = StringLocalizerFactory.Create<AppStrings>(cultureName);
        var homePage = new MainHomePage(Page, WebAppServerAddress);
        await Page.GotoAsync(new Uri(WebAppServerAddress, cultureName).ToString());
        await homePage.AssertLocalized(localizer, cultureName, cultureDisplayName);
    }
}

public class CultureDataAttribute : Attribute, ITestDataSource
{
    public IEnumerable<object[]> GetData(MethodInfo methodInfo)
    {
        return CultureInfoManager.SupportedCultures.Select(p => new object[] { p.Culture.Name, p.DisplayName });
        //OR using below code
        //yield return ["en-US", "English US"];
        //yield return ["fa-IR", "فارسی"];
    }

    public string GetDisplayName(MethodInfo methodInfo, object[] data)
    {
        return $"{methodInfo.Name} ({string.Join(", ", data)})";
    }
}
