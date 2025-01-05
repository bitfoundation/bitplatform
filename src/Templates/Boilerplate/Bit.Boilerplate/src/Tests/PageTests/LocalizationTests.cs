using System.Reflection;
using Boilerplate.Tests.Services;
using Boilerplate.Tests.PageTests.PageModels;
using Boilerplate.Tests.Extensions;

namespace Boilerplate.Tests.PageTests.BlazorServer;

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
        await homePage.AssertCultureCombobox(cultureName, cultureDisplayName);
    }
    public async Task SetCultureInBrowserContext(BrowserNewContextOptions options, string cultureName, string _) => options.Locale = cultureName;

    [TestMethod]
    [CultureData]
    public async Task LanguageDropDown(string cultureName, string cultureDisplayName)
    {
        var localizer = StringLocalizerFactory.Create<AppStrings>(cultureName);
        var homePage = new MainHomePage(Page, WebAppServerAddress);
        await homePage.Open();
        await homePage.ChangeCulture(cultureDisplayName);
        await homePage.AssertLocalized(localizer, cultureName, cultureDisplayName);
        await homePage.AssertCultureCombobox(cultureName, cultureDisplayName);
    }

    [TestMethod]
    [CultureData]
    public async Task QueryString(string cultureName, string cultureDisplayName)
    {
        var localizer = StringLocalizerFactory.Create<AppStrings>(cultureName);
        var homePage = new MainHomePage(Page, WebAppServerAddress);
        await Page.GotoAsync($"{WebAppServerAddress}?culture={cultureName}");
        await Page.WaitForHydrationToComplete();
        await homePage.AssertLocalized(localizer, cultureName, cultureDisplayName);
        await homePage.AssertCultureCombobox(cultureName, cultureDisplayName);
    }

    [TestMethod]
    [CultureData]
    public async Task UrlSegment(string cultureName, string cultureDisplayName)
    {
        var localizer = StringLocalizerFactory.Create<AppStrings>(cultureName);
        var homePage = new MainHomePage(Page, WebAppServerAddress);
        await Page.GotoAsync(new Uri(WebAppServerAddress, cultureName).ToString());
        await Page.WaitForHydrationToComplete();
        await homePage.AssertLocalized(localizer, cultureName, cultureDisplayName);
        await homePage.AssertCultureCombobox(cultureName, cultureDisplayName);
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
