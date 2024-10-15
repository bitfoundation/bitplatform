namespace Boilerplate.Tests.PageTests.PageModels.Layout;

public partial class MainLayout(IPage page, Uri serverAddress, string pagePath, string pageTitle)
{
    public virtual async Task Open()
    {
        await page.GotoAsync(new Uri(serverAddress, pagePath).ToString());
    }

    public virtual async Task AssertOpen()
    {
        await Assertions.Expect(page).ToHaveTitleAsync(pageTitle);
    }
}
