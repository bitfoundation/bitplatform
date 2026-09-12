using Boilerplate.Tests.E2E.Infrastructure.Services;

namespace Boilerplate.Tests.E2E.Features.Realtime;

/// <summary>Two browsers of the same user: the one that uploads is the app this test opened.</summary>
[TestClass, TestCategory(TestCategories.Web), Retry(2)]
public partial class WebRealtimeProfileTests : RealtimeProfileTestsBase
{
    protected override IAppOpener AppOpener => new WebAppOpener();

    [TestMethod]
    [DataRow(App.Sales, DisplayName = "Sales (integrated API)")]
    [DataRow(App.Todo, DisplayName = "Todo (standalone API)")]
    [DataRow(App.AdminPanel, DisplayName = "AdminPanel (standalone API, bit Brouter)")]
    public async Task AProfilePictureChangedInOneBrowser_Should_ShowInTheOtherAtOnce(App app)
    {
        await AProfilePictureChange_Should_ReachTheUsersOtherSession(app, theAppUploads: true);
    }
}
