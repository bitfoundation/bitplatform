using Microsoft.EntityFrameworkCore;
using Boilerplate.Tests.Infrastructure.Components;
using Boilerplate.Server.Api.Infrastructure.Data;

namespace Boilerplate.Tests.Features.Identity;

[TestClass, TestCategory("UITest")]
public partial class QuickSignInUITests : AppPageTest
{
    /// <summary>
    /// A brand-new (anonymous) shopper buys a seeded product end-to-end:
    /// <list type="number">
    /// <item>Pick any seeded product straight from the server database. Reading it with <c>IgnoreQueryFilters</c> is required because this bare DB scope has no HttpContext, so the tenant-aware row level security query filter (See <c>AppDbContext.ConfigureTenantAwareEntity</c>) has no tenant to resolve.</item>
    /// <item>Open its public product page - a page that needs no sign-in - and click Buy.</item>
    /// <item>Buying while signed-out pops the <c>SignInModal</c> (See <c>ProductPage.Buy</c> / <c>SignInModalService</c>); she signs in for the very first time with a magic link OTP for a random e-mail, reusing the same helpers as the identity tests.</item>
    /// <item>The successful sign-in closes the modal and lets the purchase go through, so the success snackbar shows up.</item>
    /// </list>
    /// </summary>
    [TestMethod]
    public async Task AnonymousUser_Should_BuyProduct_AfterMagicLinkSignIn()
    {
        await using var server = new AppTestServer(Context);
        await server.Build().Start(TestContext.CancellationToken);

        var serverAddress = server.WebAppServerAddress;

        // The ShortId is the human-friendly id the product page URL uses. Ignore the tenant-aware global query filter
        // here: without an HttpContext the current tenant can't be resolved, so an un-ignored read would throw.
        int productShortId;
        await using (var scope = server.WebApp.Services.CreateAsyncScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            productShortId = await dbContext.Products
                .IgnoreQueryFilters()
                .Select(product => product.ShortId)
                .FirstOrDefaultAsync(TestContext.CancellationToken);
        }

        Assert.AreNotEqual(0, productShortId, "The test database should have at least one seeded product to buy.");

        // The product page is public, so opening it needs no sign-in.
        await Page.GotoAsync(new Uri(serverAddress, $"{PageUrls.Product}/{productShortId}").ToString(),
            new() { WaitUntil = WaitUntilState.NetworkIdle });

        // Buying while signed-out opens the SignInModal over the product page.
        await Page.GetByRole(AriaRole.Button, new() { Name = AppStrings.Buy, Exact = true }).ClickAsync();

        // She signs in for the very first time with a magic link OTP right inside the modal, the same way the identity
        // test does, only without navigating to the Sign in page first (the modal already hosts the SignInPanel).
        var email = MagicLinkSignInUtils.NewTestEmail();
        await MagicLinkSignInUtils.RequestMagicLinkAndOtpOnCurrentPanel(Page, email);

        var (_, otpCode) = await MagicLinkSignInUtils.ReadConfirmationEmail(server, email, TestContext.CancellationToken);
        await BitOtpInputUtils.FillOtpInputs(Page, otpCode);

        // Filling the last digit signs her in, which closes the modal and completes the purchase, so its success
        // snackbar appears (See ProductPage.Buy -> SnackBarService.Success).
        await Expect(Page.GetByText(AppStrings.PurchaseSuccessful)).ToBeVisibleAsync();
    }
}
