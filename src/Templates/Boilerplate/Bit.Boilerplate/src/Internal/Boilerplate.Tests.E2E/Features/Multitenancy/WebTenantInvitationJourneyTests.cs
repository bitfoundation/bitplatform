using Boilerplate.Tests.E2E.Infrastructure.Services;

namespace Boilerplate.Tests.E2E.Features.Multitenancy;

/// <summary>
/// The journey with the invitee in a browser, where the invitation link is simply a url to navigate to.
/// </summary>
[TestClass, TestCategory(TestCategories.Web), Retry(2), DoNotParallelize]
public partial class WebTenantInvitationJourneyTests : TenantInvitationJourneyTestBase
{
    protected override IAppOpener AppOpener => new WebAppOpener();

    protected override Task OpenInvitationLink(IPage page, string invitationLink)
        => page.GotoAsync(invitationLink, new() { WaitUntil = WaitUntilState.NetworkIdle });
}
