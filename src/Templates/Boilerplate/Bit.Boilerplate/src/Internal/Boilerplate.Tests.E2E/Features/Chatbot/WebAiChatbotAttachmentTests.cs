using Boilerplate.Shared.Features.Attachments;
using Boilerplate.Tests.E2E.Infrastructure.Services;

namespace Boilerplate.Tests.E2E.Features.Chatbot;

/// <summary>
/// Showing the assistant a picture - one of the deployment's own product photos rather than a fixture, pulled from
/// the same database the answer is checked against, so no binary is committed and the car is one this shop sells.
/// <para>
/// The file input is set directly rather than clicking the paperclip, which opens the OS file dialog - not the app's
/// and not Playwright's to drive.
/// </para>
/// </summary>
[TestClass, TestCategory(TestCategories.Web), Retry(2)]
public partial class WebAiChatbotAttachmentTests : AppTestBase
{
    private const string Question = "What car is in this picture?";

    protected override IAppOpener AppOpener => new WebAppOpener();

    [TestMethod]
    public async Task Assistant_Should_DescribeAProductImage_TheUserAttached()
    {
        var globalApiClient = await DeployedApiClientProvider.GetGlobalApiClient(TestContext.CancellationToken);
        await using var dbContext = await globalApiClient.DbContextFactory!.CreateDbContextAsync(TestContext.CancellationToken);

        // Unfiltered: the products belong to the deployment's store tenant, and this host carries no tenant of its own.
        var product = await dbContext.Products.IgnoreQueryFilters()
            .Where(product => product.HasPrimaryImage)
            .Select(product => new { product.Id, product.Version, Name = product.Name!, Manufacturer = product.Category!.Name })
            .FirstOrDefaultAsync(TestContext.CancellationToken);

        Assert.IsNotNull(product, "No product in the deployment's database has a primary image, so there is no photo of a real car to show the assistant.");

        // The same url ProductDto.GetPrimaryMediumImageUrl builds for the app's own pages, so it's the picture a
        // visitor sees.
        var imageUrl = new Uri(new Uri(DeployedApps.Sales), $"/api/v1/Attachment/GetAttachment/{product.Id}/{AttachmentKind.ProductPrimaryImageMedium}?v={product.Version}");

        var imageFile = Path.Combine(Path.GetTempPath(), $"{product.Id}.webp");
        using (var httpClient = new HttpClient())
        {
            await File.WriteAllBytesAsync(imageFile, await httpClient.GetByteArrayAsync(imageUrl, TestContext.CancellationToken), TestContext.CancellationToken);
        }

        var page = await OpenApp(App.Sales);

        await WaitUntilInteractive(page);

        // Sending an image uploads it first, and that upload needs an account (See AppAiChatPanel.UploadPendingAttachment).
        await SignIn(page, StoreUser.Email, StoreUser.Password);

        var panel = await AiChatPanel.Open(page);

        await panel.AttachmentInput.SetInputFilesAsync(imageFile);

        // The thumbnail is the panel saying it took the file; without it the message goes up as text alone.
        await Expect(page.Locator(".pending-attachment")).ToBeVisibleAsync();

        var answer = await panel.Ask(Question);

        await AiAnswerJudge.AssertAnswer(Question,
            $"""
            The assistant describes a car it was shown a photograph of, and what it says is consistent with that car
            being a {product.Manufacturer} {product.Name}. It need not name the model exactly - recognising the make,
            the body style or the segment counts - but it must be talking about a car in a picture. Saying it cannot
            see an image, or describing something that is not a car, fails.
            """,
            answer, TestContext.CancellationToken);
    }
}
