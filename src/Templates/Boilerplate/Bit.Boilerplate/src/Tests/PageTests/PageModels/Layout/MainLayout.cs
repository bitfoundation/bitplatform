namespace Boilerplate.Tests.PageTests.PageModels.Layout;

public partial class MainLayout(IPage page, Uri serverAddress, string pagePath, string pageTitle)
{
    private IResponse response;

    public virtual async Task Open()
    {
        response = (await page.GotoAsync(new Uri(serverAddress, pagePath).ToString()))!;
    }

    public virtual async Task AssertOpen()
    {
        Assert.IsNotNull(response);
        Assert.AreEqual(StatusCodes.Status200OK, response.Status);

        await Assertions.Expect(page).ToHaveTitleAsync(pageTitle);
    }
}
