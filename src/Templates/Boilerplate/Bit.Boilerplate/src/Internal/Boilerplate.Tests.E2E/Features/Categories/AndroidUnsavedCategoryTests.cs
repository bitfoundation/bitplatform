using Boilerplate.Tests.E2E.Infrastructure.Services;

namespace Boilerplate.Tests.E2E.Features.Categories;

/// <summary>
/// Leaving is Android's back button. Not parallelized: both apps run on the single connected device/emulator (See
/// AndroidSmokeTests).
/// </summary>
[TestClass, TestCategory(TestCategories.Android), DoNotParallelize, Retry(2)]
public partial class AndroidUnsavedCategoryTests : UnsavedCategoryTestsBase
{
    protected override IAppOpener AppOpener => new AndroidAppOpener();

    /// <summary>The name was just typed, so the keyboard is open here (See PressAndroidBack).</summary>
    protected override async Task GoBack(IPage page) => await Playwright.PressAndroidBack();
}
