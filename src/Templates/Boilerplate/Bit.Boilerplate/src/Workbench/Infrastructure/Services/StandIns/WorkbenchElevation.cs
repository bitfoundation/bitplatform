namespace Boilerplate.Workbench.Infrastructure.Services.StandIns;

/// <summary>
/// Whether the view's session has lately confirmed it's the user, the app's elevated access (AuthManager's
/// TryEnterElevatedAccessMode): what removing a session, changing the email or deleting the account first ask for. A view
/// confirmed a minute ago, so those go straight through, unless its page's <see cref="Switch"/> says the app asks first:
/// then the app's own ElevatedAccessModal opens, and a code or a passkey it accepts confirms the view for five minutes.
/// A confirmation is the sign-in's that made it: another demo user, or signing out, has to confirm again.
/// </summary>
public sealed class WorkbenchElevation(WorkbenchScenarioState scenarioState)
{
    /// <summary>The switch of a page whose actions ask to confirm it's you.</summary>
    public const string Switch = "confirm";

    /// <summary>The <see cref="Switch"/> value under which the app asks for a code before such an action.</summary>
    public const string AskCase = "ask";

    /// <summary>How long a confirmation lasts, as the server's access token with it does.</summary>
    private static readonly TimeSpan lifetime = TimeSpan.FromMinutes(5);

    private (DateTimeOffset Until, (WorkbenchAuthState Auth, int User) SignIn)? confirmed;

    private (WorkbenchAuthState Auth, int User) SignIn => (scenarioState.Scenario.Auth, scenarioState.Scenario.User);

    /// <summary>Until when the view's session is elevated, or null when the app would ask first.</summary>
    public DateTimeOffset? ElevatedUntil
    {
        get
        {
            var now = DateTimeOffset.UtcNow;

            if (confirmed is { } confirmation && confirmation.SignIn == SignIn && confirmation.Until > now)
                return confirmation.Until;

            return scenarioState.Scenario.GetSwitch(Switch) is AskCase ? null : now.Add(lifetime);
        }
    }

    /// <summary>What the server does with a code or a passkey it accepts.</summary>
    public void Confirm() => confirmed = (DateTimeOffset.UtcNow.Add(lifetime), SignIn);
}
