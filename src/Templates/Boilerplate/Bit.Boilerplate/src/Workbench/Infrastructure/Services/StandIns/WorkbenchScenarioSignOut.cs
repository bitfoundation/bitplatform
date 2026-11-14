namespace Boilerplate.Workbench.Infrastructure.Services.StandIns;

/// <summary>
/// The app signed the view's device out, as it does after the account is deleted: the page that hosts the app's page tells
/// the shell, so the view's sign-in switch follows and the app's not-authorized page shows, as it would on the device.
/// </summary>
public sealed class WorkbenchScenarioSignOut(WorkbenchScenarioState scenarioState)
{
    /// <summary>Raised when the app signs out; a host reports the signed-out scenario to the shell.</summary>
    public event Func<WorkbenchScenario, Task>? SignedOut;

    public void SignOut()
    {
        var signedOut = scenarioState.Scenario with { Auth = WorkbenchAuthState.SignedOut };

        if (SignedOut is { } handler)
        {
            _ = handler(signedOut);
            return;
        }

        scenarioState.Set(signedOut);
    }
}
