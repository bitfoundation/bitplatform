using Boilerplate.WorkbenchTests.Features.Review;

namespace Boilerplate.WorkbenchTests.Features.Home;

/// <summary>What the home page's cards say about the review: a page's status and its open comments, which a copy without an agent shows too.</summary>
[TestClass, TestCategory("UITest")]
public class HomePageReviewUITests : ReviewPageTest
{
    private const string PageKey = "assistant";

    private ILocator Card => Page.Locator("a.page-card").Filter(new() { HasText = WorkbenchStrings.AssistantTitle });

    private static string OpenComments(int count) => string.Format(CultureInfo.InvariantCulture, WorkbenchStrings.OpenCommentsCount, count);

    private async Task OpenHome()
    {
        await Page.GotoAsync(Server.Address.ToString());
        await WaitUntilInteractive();
    }

    [TestMethod, EverySurface]
    public async Task ThePagesCard_Should_ShowItsReviewOnlyOnceItHasOne(WorkbenchSurface surface)
    {
        await ClearComments(PageKey);
        await ClearPageStatus(PageKey);

        await OpenHome();

        await Expect(Card.Locator(".page-title")).ToBeVisibleAsync();
        await Expect(Card.Locator(".page-review")).ToHaveCountAsync(0);

        await SeedComment(PageKey, Unique("A comment for the home page"));

        await Expect(Card.Locator(".review-count")).ToHaveTextAsync(OpenComments(1));
    }

    [TestMethod, EverySurface]
    public async Task ThePagesCard_Should_CountOnlyItsOpenComments(WorkbenchSurface surface)
    {
        await ClearComments(PageKey);

        await SeedComment(PageKey, Unique("Open"));
        await SeedComment(PageKey, Unique("Open too"));

        var done = await SeedComment(PageKey, Unique("Done"));

        await Server.ReviewHost.EditAsync(WorkbenchReviewDocument.Id, document => WorkbenchReviewDocument.SetStatus(document.GetMap("threads"), done, WorkbenchReviewStatus.Done, fromAgent: true));

        await OpenHome();

        await Expect(Card.Locator(".review-count")).ToHaveTextAsync(OpenComments(2));
    }

    [TestMethod, EverySurface]
    public async Task ThePagesCard_Should_ShowItsStatus(WorkbenchSurface surface)
    {
        await ClearComments(PageKey);
        await ClearPageStatus(PageKey);

        await Server.ReviewHost.EditAsync(WorkbenchReviewDocument.Id, document => WorkbenchReviewDocument.SetPageStatus(document.GetMap("pages"), PageKey, WorkbenchPageStatus.Approved, "Sara"));

        await OpenHome();

        await Expect(Card.Locator(".review-status")).ToHaveTextAsync(WorkbenchStrings.PageStatusApproved);
        await Expect(Card.Locator(".review-count")).ToHaveCountAsync(0);

        await ClearPageStatus(PageKey);
    }
}
