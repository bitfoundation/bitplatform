namespace Boilerplate.Workbench.Infrastructure.Services.StandIns;

/// <summary>
/// Whether the view's session has lately confirmed it's the user, the app's elevated access (AuthManager's
/// TryEnterElevatedAccessMode): what removing a session, changing the email or deleting the account first ask for. A view
/// confirmed a minute ago, so those go straight through, unless its page's <see cref="Switch"/> says the app asks first:
/// then the app's own ElevatedAccessModal opens, and a code or a passkey it accepts confirms the view for five minutes.
/// A confirmation is gone once the view's sign-in changes, as signing out drops the tokens that carried it: another demo
/// user, or the same one signed in again, has to confirm again.
/// </summary>
public sealed class WorkbenchElevation : IDisposable
{
    /// <summary>The switch of a page whose actions ask to confirm it's you.</summary>
    public const string Switch = "confirm";

    /// <summary>The <see cref="Switch"/> value under which the app asks for a code before such an action.</summary>
    public const string AskCase = "ask";

    /// <summary>How long a confirmation lasts, as the server's access token with it does.</summary>
    private static readonly TimeSpan lifetime = TimeSpan.FromMinutes(5);

    private readonly WorkbenchScenarioState scenarioState;

    private DateTimeOffset? confirmedUntil;

    /// <summary>The sign-in <see cref="confirmedUntil"/> belongs to.</summary>
    private (WorkbenchAuthState Auth, int User) signIn;

    public WorkbenchElevation(WorkbenchScenarioState scenarioState)
    {
        this.scenarioState = scenarioState;
        signIn = SignIn;
        scenarioState.Changed += FollowTheSignIn;
    }

    private (WorkbenchAuthState Auth, int User) SignIn => (scenarioState.Scenario.Auth, scenarioState.Scenario.User);

    /// <summary>Until when the view's session is elevated, or null when the app would ask first.</summary>
    public DateTimeOffset? ElevatedUntil
    {
        get
        {
            FollowTheSignIn();

            var now = DateTimeOffset.UtcNow;

            if (confirmedUntil > now)
                return confirmedUntil;

            return scenarioState.Scenario.GetSwitch(Switch) is AskCase ? null : now.Add(lifetime);
        }
    }

    /// <summary>What the server does with a code or a passkey it accepts.</summary>
    public void Confirm()
    {
        FollowTheSignIn();
        confirmedUntil = DateTimeOffset.UtcNow.Add(lifetime);
    }

    /// <summary>Forgets the confirmation once the view is signed out or signed in as someone else.</summary>
    private void FollowTheSignIn()
    {
        if (SignIn == signIn)
            return;

        signIn = SignIn;
        confirmedUntil = null;
    }

    public void Dispose() => scenarioState.Changed -= FollowTheSignIn;
}
