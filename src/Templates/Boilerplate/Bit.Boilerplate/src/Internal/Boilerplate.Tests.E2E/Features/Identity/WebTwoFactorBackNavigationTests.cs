using Boilerplate.Tests.E2E.Infrastructure.Services;

namespace Boilerplate.Tests.E2E.Features.Identity;

/// <summary>Back is the browser's back button.</summary>
[TestClass, TestCategory(TestCategories.Web), Retry(2)]
public partial class WebTwoFactorBackNavigationTests : TwoFactorBackNavigationTestsBase
{
    protected override IAppOpener AppOpener => new WebAppOpener();

    /// <summary>adminpanel.bitplatform.dev routes with Brouter, adminpanel.bitplatform.cc with Blazor's Router.</summary>
    [TestMethod]
    [DataRow(App.AdminPanel, DisplayName = nameof(App.AdminPanel))]
    [DataRow(App.AdminPanelWasmStandalone, DisplayName = nameof(App.AdminPanelWasmStandalone))]
    public override Task GoingBackFromTheSecondFactor_Should_ReturnToTheSignInForm(App app) => base.GoingBackFromTheSecondFactor_Should_ReturnToTheSignInForm(app);

    /// <summary>Commit only: a history step inside the app never loads a document.</summary>
    protected override async Task GoBack(IPage page) => await page.GoBackAsync(new() { WaitUntil = WaitUntilState.Commit });
}
