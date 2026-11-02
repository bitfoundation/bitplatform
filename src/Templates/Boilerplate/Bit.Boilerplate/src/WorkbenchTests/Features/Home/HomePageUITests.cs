namespace Boilerplate.WorkbenchTests.Features.Home;

/// <summary>The home page: what it promises, the way into the pages and its own language and theme.</summary>
[TestClass, TestCategory("UITest")]
public class HomePageUITests : WorkbenchPageTest
{
    [TestMethod]
    public async Task TheHomePage_Should_ListEveryPage()
    {
        await Page.GotoAsync(Server.Address.ToString());

        var cards = Page.Locator("a.page-card");

        await Expect(cards).ToHaveCountAsync(WorkbenchPageCatalog.Pages.Count);

        await cards.Filter(new() { HasText = WorkbenchStrings.BuildingsTitle }).ClickAsync();

        await Expect(Page).ToHaveURLAsync(new Regex("/workbench/buildings"));
        await WaitForViews();
    }
}
