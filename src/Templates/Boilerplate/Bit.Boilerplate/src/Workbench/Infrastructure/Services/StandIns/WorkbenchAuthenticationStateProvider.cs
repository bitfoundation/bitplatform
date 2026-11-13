using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;

namespace Boilerplate.Workbench.Infrastructure.Services.StandIns;

/// <summary>
/// Signs the view in as the scenario's demo user, with the claims the app's server would issue (See WorkbenchClaims),
/// and tells the page when the scenario's sign-in changes.
/// </summary>
public sealed class WorkbenchAuthenticationStateProvider : AuthenticationStateProvider, IDisposable
{
    private readonly WorkbenchScenarioState scenarioState;
    private (WorkbenchAuthState Auth, int User) shown;

    public WorkbenchAuthenticationStateProvider(WorkbenchScenarioState scenarioState)
    {
        this.scenarioState = scenarioState;
        shown = (scenarioState.Scenario.Auth, scenarioState.Scenario.User);
        scenarioState.Changed += OnScenarioChanged;
    }

    public override Task<AuthenticationState> GetAuthenticationStateAsync() => Task.FromResult(new AuthenticationState(CreateUser(scenarioState.Scenario)));

    private void OnScenarioChanged()
    {
        var current = (scenarioState.Scenario.Auth, scenarioState.Scenario.User);

        // Other scenario changes leave the user as is, so they don't re-run the page's authorization.
        if (current == shown)
            return;

        shown = current;
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    }

    private static ClaimsPrincipal CreateUser(WorkbenchScenario scenario) => WorkbenchClaims.Of(scenario) switch
    {
        [] => new(new ClaimsIdentity()),
        var claims => new(new ClaimsIdentity(claims, authenticationType: "Workbench"))
    };

    public void Dispose() => scenarioState.Changed -= OnScenarioChanged;
}
