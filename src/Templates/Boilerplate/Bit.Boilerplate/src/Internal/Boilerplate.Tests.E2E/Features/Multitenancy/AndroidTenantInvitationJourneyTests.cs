using Boilerplate.Tests.E2E.Infrastructure.Services;

namespace Boilerplate.Tests.E2E.Features.Multitenancy;

/// <summary>
/// The journey with the invitee in the installed Android app, where the invitation link is handed to the OS and the
/// app link verification is what has to route it back into the app. Not parallelized: one connected device.
/// </summary>
[TestClass, TestCategory(TestCategories.Android), Retry(2), DoNotParallelize]
public partial class AndroidTenantInvitationJourneyTests : TenantInvitationJourneyTestBase
{
    /// <summary>The link opens the app at its root, which Routes.OpenUniversalLink may culture prefix.</summary>
    private static readonly Regex appRoot = new(@"^/([A-Za-z]{2}-[A-Za-z]{2}/?)?$");

    protected override IAppOpener AppOpener => new AndroidAppOpener();

    protected override async Task OpenInvitationLink(IPage page, string invitationLink)
    {
        await Playwright.OpenAndroidAppLink(invitationLink);

        // The assertion, not a wait: the app leaving the route it was parked on is what proves the intent reached it
        // instead of a browser.
        await page.WaitForURLAsync(url => appRoot.IsMatch(new Uri(url).AbsolutePath),
            new() { WaitUntil = WaitUntilState.NetworkIdle });
    }
}
