namespace Boilerplate.Workbench.Infrastructure.Services.StandIns;

/// <summary>
/// The access token the app's client reads where it checks a claim the sign-in state doesn't carry, such as whether the
/// session is elevated (AuthManager's TryEnterElevatedAccessMode) or which token goes with an upload: one of the view's own
/// claims (See <see cref="WorkbenchClaims"/>), made afresh on every read, so it follows the inspector's sign-in.
/// </summary>
public sealed class WorkbenchAuthTokenProvider(WorkbenchScenarioState scenarioState, WorkbenchElevation elevation) : IAuthTokenProvider
{
    public Task<string?> GetAccessToken()
    {
        var claims = WorkbenchClaims.Of(scenarioState.Scenario);

        return Task.FromResult(claims.Count is 0 ? null : WorkbenchClaims.AccessToken(claims, DateTimeOffset.UtcNow, elevation.ElevatedUntil));
    }
}
