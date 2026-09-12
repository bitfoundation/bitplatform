using Boilerplate.Tests.E2E.Infrastructure.Services;

namespace Boilerplate.Tests.E2E.Features.Realtime;

/// <summary>
/// The Android app is the watcher: a browser session of the same user uploads, and the app's header has to change with
/// no reload. Not parallelized: both apps run on the single connected device/emulator (See AndroidSmokeTests).
/// </summary>
[TestClass, TestCategory(TestCategories.Android), DoNotParallelize, Retry(2)]
public partial class AndroidRealtimeProfileTests : RealtimeProfileTestsBase
{
    protected override IAppOpener AppOpener => new AndroidAppOpener();

    [TestMethod]
    [DataRow(App.Todo, DisplayName = "Todo")]
    [DataRow(App.AdminPanel, DisplayName = "AdminPanel")]
    public async Task AProfilePictureChangedInTheBrowser_Should_ShowInTheAndroidAppAtOnce(App app)
    {
        await AProfilePictureChange_Should_ReachTheUsersOtherSession(app, theAppUploads: false);
    }
}
