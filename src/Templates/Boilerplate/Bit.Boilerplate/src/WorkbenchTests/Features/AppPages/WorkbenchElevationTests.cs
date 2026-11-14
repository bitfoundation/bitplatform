using Boilerplate.Workbench.Infrastructure.Services.StandIns;

namespace Boilerplate.WorkbenchTests.Features.AppPages;

/// <summary>
/// The view's confirmation that it's the user, which the settings page's code prompt gives: it lasts for the sign-in that
/// made it only, as signing out drops the tokens that carried it.
/// </summary>
[TestClass, TestCategory("UnitTest")]
public class WorkbenchElevationTests
{
    private static readonly WorkbenchScenario asks = new WorkbenchScenario().WithSwitch(WorkbenchElevation.Switch, WorkbenchElevation.AskCase);

    [TestMethod]
    public void AConfirmation_Should_LastForTheSignInThatMadeIt()
    {
        var scenarioState = new WorkbenchScenarioState();
        scenarioState.Set(asks);

        using var elevation = new WorkbenchElevation(scenarioState);

        Assert.IsNull(elevation.ElevatedUntil, "The app asks for a code first.");

        elevation.Confirm();

        Assert.IsNotNull(elevation.ElevatedUntil);
    }

    [TestMethod]
    public void SigningOutAndBackIn_Should_AskForACodeAgain()
    {
        var scenarioState = new WorkbenchScenarioState();
        scenarioState.Set(asks);

        using var elevation = new WorkbenchElevation(scenarioState);

        elevation.Confirm();

        scenarioState.Set(asks with { Auth = WorkbenchAuthState.SignedOut });
        scenarioState.Set(asks);

        Assert.IsNull(elevation.ElevatedUntil);
    }

    [TestMethod]
    public void AnotherUser_Should_BeAskedForACode()
    {
        var scenarioState = new WorkbenchScenarioState();
        scenarioState.Set(asks);

        using var elevation = new WorkbenchElevation(scenarioState);

        elevation.Confirm();

        scenarioState.Set(asks with { User = 1 });

        Assert.IsNull(elevation.ElevatedUntil);

        scenarioState.Set(asks);

        Assert.IsNull(elevation.ElevatedUntil, "Back to the first user, who confirmed before the other one came.");
    }
}
