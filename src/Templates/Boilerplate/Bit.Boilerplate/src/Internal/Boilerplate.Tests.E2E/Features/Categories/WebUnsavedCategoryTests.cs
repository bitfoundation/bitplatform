using Boilerplate.Tests.E2E.Infrastructure.Services;

namespace Boilerplate.Tests.E2E.Features.Categories;

/// <summary>
/// Leaving is the browser's back button. Not parallelized: the store admin's sign in code is read off the mail job
/// addressed to it, which another test signing it in at the same time could take (See WebProductConcurrencyTests).
/// </summary>
[TestClass, TestCategory(TestCategories.Web), DoNotParallelize, Retry(2)]
public partial class WebUnsavedCategoryTests : UnsavedCategoryTestsBase
{
    protected override IAppOpener AppOpener => new WebAppOpener();

    /// <summary>adminpanel.bitplatform.dev routes with Brouter, adminpanel.bitplatform.cc with Blazor's Router.</summary>
    [TestMethod]
    [DataRow(App.AdminPanel, DisplayName = nameof(App.AdminPanel))]
    [DataRow(App.AdminPanelWasmStandalone, DisplayName = nameof(App.AdminPanelWasmStandalone))]
    public override Task LeavingWithAnUnsavedCategoryName_Should_BeRefused(App app) => base.LeavingWithAnUnsavedCategoryName_Should_BeRefused(app);

    /// <summary>Commit only: a history step inside the app never loads a document.</summary>
    protected override async Task GoBack(IPage page) => await page.GoBackAsync(new() { WaitUntil = WaitUntilState.Commit });
}
