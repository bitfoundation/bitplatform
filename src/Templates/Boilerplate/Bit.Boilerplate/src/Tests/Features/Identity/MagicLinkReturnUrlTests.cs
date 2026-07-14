using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Boilerplate.Server.Api.Infrastructure.Data;

namespace Boilerplate.Tests.Features.Identity;

[TestClass, TestCategory("UITest")]
public partial class MagicLinkReturnUrlTests : AppPageTest
{
    /// <summary>
    /// A signed-out visitor on a public product page starts signing in from the user menu and finishes by opening the
    /// magic link e-mailed to her: opening it must bring her back to the very same product page, not the default home page.
    /// <list type="number">
    /// <item>Pick any seeded product's ShortId straight from the server database. Reading it with <c>IgnoreQueryFilters</c>
    /// is required because this bare DB scope has no HttpContext, so the tenant-aware row level security query filter has no
    /// current tenant to resolve and would otherwise throw (See <c>TenantProvider.GetCurrentTenantId</c>).</item>
    /// <item>Open its public product page - a page that needs no sign-in - while still signed-out.</item>
    /// <item>From the user menu, click "Sign in". Its link carries the product page as the return-url (See <c>AppMenu</c>),
    /// which is exactly what must survive all the way through the magic link.</item>
    /// <item>On the sign-in page, request a magic link for a brand-new random e-mail (the same flow as the identity tests).
    /// A new account makes the server e-mail the confirmation magic link, which now carries that same return-url.</item>
    /// <item>Open the captured magic link: it confirms the account, signs her in and must land back on the product page.</item>
    /// </list>
    /// </summary>
    [TestMethod]
    public async Task MagicLink_Should_ReturnToOriginatingProductPage_AfterSignIn()
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

        Assert.AreNotEqual(0, productShortId, "The test database should have at least one seeded product for this test.");

        // The product page is public, so she can open it while still signed-out.
        await Page.GotoAsync(new Uri(serverAddress, $"{PageUrls.Product}/{productShortId}").ToString(),
            new() { WaitUntil = WaitUntilState.NetworkIdle });

        // Open the user menu (the header's BitDropMenu) and click its "Sign in" entry. That link carries the current page
        // (this product page) as its return-url; opening the menu's Sign in navigates to the Sign in page carrying it.
        await Page.Locator("header .bit-drm-btn").First.ClickAsync();
        await Page.Locator(".app-menu-callout").GetByRole(AriaRole.Link, new() { Name = AppStrings.SignIn }).ClickAsync();

        // Now on the Sign in page (with the product page as its return-url), request a magic link for a brand-new e-mail.
        // The new account makes the server e-mail the confirmation magic link and reveal the OTP panel.
        var email = MagicLinkSignInUtils.NewTestEmail();
        await MagicLinkSignInUtils.RequestMagicLinkAndOtpOnCurrentPanel(Page, email);

        // Read the confirmation e-mail's magic link (it carries the product page as its return-url) and open it.
        var (confirmUrl, _) = await MagicLinkSignInUtils.ReadConfirmationEmail(server, email, TestContext.CancellationToken);
        await Page.GotoAsync(confirmUrl, new() { WaitUntil = WaitUntilState.NetworkIdle });

        // Signing in through the magic link must bring her back to the very same product page, now authenticated.
        await Expect(Page).ToHaveURLAsync(new Regex($@"{Regex.Escape(PageUrls.Product)}/{productShortId}(/|\?|$)"));
        await Expect(Page.Locator(".bit-prs.persona").First).ToContainTextAsync(email);
    }
}
