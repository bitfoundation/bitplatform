using Boilerplate.Tests.E2E.Infrastructure.Services;

namespace Boilerplate.Tests.E2E.Features.Identity;

/// <summary>
/// No browser chrome to go back with, so back is the app's own: IdentityHeader's "Back to home" link. Not
/// parallelized: every Client.Windows app answers on the same CDP port (See WindowsSmokeTests).
/// </summary>
[TestClass, TestCategory(TestCategories.Windows), DoNotParallelize, Retry(2)]
public partial class WindowsTwoFactorBackNavigationTests : TwoFactorBackNavigationTestsBase
{
    protected override IAppOpener AppOpener => new WindowsAppOpener();

    protected override async Task GoBack(IPage page) => await page.GetByText(AppStrings.BackToHome, new() { Exact = true }).ClickAsync();
}
