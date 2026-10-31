//+:cnd:noEmit
// [mirror] the claims a signed-in user carries - keep in sync with:
// - src/Server/Boilerplate.Server.Api/Features/Identity/Services/AppUserClaimsPrincipalFactory.cs
using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;

namespace Boilerplate.Workbench.Infrastructure.Services.StandIns;

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

        if (current == shown)
            return;

        shown = current;
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    }

    private static ClaimsPrincipal CreateUser(WorkbenchScenario scenario)
    {
        if (scenario.Auth is WorkbenchAuthState.SignedOut)
            return new(new ClaimsIdentity());

        var user = WorkbenchDemoUsers.Find(scenario.User);
        var features = user.Role is AppRoles.Demo ? AppFeatures.GetDemoFeatures() : AppFeatures.GetRoleImpliedFeatures(role => role == user.Role);

        Claim[] claims =
        [
            new(ClaimTypes.NameIdentifier, new Guid(scenario.User + 1, 0, 0, new byte[8]).ToString()),
            new(ClaimTypes.Name, user.Email),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Role, user.Role),
            new(AppClaimTypes.PRIVILEGED_SESSION, "true"),
            //#if (multitenant == true)
            new(AppClaimTypes.TENANT_ID, new Guid(1, 0, 0, new byte[8]).ToString()),
            //#endif
            .. features.Select(feature => new Claim(AppClaimTypes.FEATURES, feature.Value))
        ];

        return new(new ClaimsIdentity(claims, authenticationType: "Workbench"));
    }

    public void Dispose() => scenarioState.Changed -= OnScenarioChanged;
}
