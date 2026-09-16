using Boilerplate.Tests.E2E.Infrastructure.Services;

namespace Boilerplate.Tests.E2E.Features.Identity;

/// <summary>
/// Back is Android's back button, which would close the app if the WebView let it through. Not parallelized: both
/// apps run on the single connected device/emulator (See AndroidSmokeTests).
/// </summary>
[TestClass, TestCategory(TestCategories.Android), DoNotParallelize, Retry(2)]
public partial class AndroidTwoFactorBackNavigationTests : TwoFactorBackNavigationTestsBase
{
    protected override IAppOpener AppOpener => new AndroidAppOpener();

    /// <summary>TfaPanel focuses its code input, so the keyboard is open here (See PressAndroidBack).</summary>
    protected override async Task GoBack(IPage page) => await Playwright.PressAndroidBack();
}
