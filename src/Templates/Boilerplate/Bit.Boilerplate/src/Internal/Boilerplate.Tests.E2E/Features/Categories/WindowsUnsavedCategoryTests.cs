using Boilerplate.Tests.E2E.Infrastructure.Services;

namespace Boilerplate.Tests.E2E.Features.Categories;

/// <summary>
/// Leaving is a history step back - what the header's back button (Header.GoBack) and the mouse's back button both do;
/// the categories page shows no back button of its own. Not parallelized: every Client.Windows app answers on the same
/// CDP port (See WindowsSmokeTests).
/// </summary>
[TestClass, TestCategory(TestCategories.Windows), DoNotParallelize, Retry(2)]
public partial class WindowsUnsavedCategoryTests : UnsavedCategoryTestsBase
{
    protected override IAppOpener AppOpener => new WindowsAppOpener();

    protected override async Task GoBack(IPage page) => await page.EvaluateAsync("() => history.back()");
}
