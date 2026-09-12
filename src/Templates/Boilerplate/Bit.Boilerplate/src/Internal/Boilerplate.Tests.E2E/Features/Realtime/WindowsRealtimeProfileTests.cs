using Boilerplate.Tests.E2E.Infrastructure.Services;

namespace Boilerplate.Tests.E2E.Features.Realtime;

/// <summary>
/// The Windows app is the watcher: a browser session of the same user uploads, and the app's header has to change with
/// no reload. Not parallelized: every Client.Windows app answers on the same CDP port (See WindowsSmokeTests).
/// </summary>
[TestClass, TestCategory(TestCategories.Windows), DoNotParallelize, Retry(2)]
public partial class WindowsRealtimeProfileTests : RealtimeProfileTestsBase
{
    protected override IAppOpener AppOpener => new WindowsAppOpener();

    [TestMethod]
    [DataRow(App.Todo, DisplayName = "Todo")]
    [DataRow(App.AdminPanel, DisplayName = "AdminPanel")]
    [DataRow(App.Sales, DisplayName = "Sales")]
    public async Task AProfilePictureChangedInTheBrowser_Should_ShowInTheWindowsAppAtOnce(App app)
    {
        await AProfilePictureChange_Should_ReachTheUsersOtherSession(app, theAppUploads: false);
    }
}
