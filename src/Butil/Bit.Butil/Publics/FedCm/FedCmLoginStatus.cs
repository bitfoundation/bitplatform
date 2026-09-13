namespace Bit.Butil;

/// <summary>
/// What an identity provider tells the browser about its own session, through
/// <see cref="FedCm.SetLoginStatus"/>.
/// <br/>
/// <see href="https://developer.mozilla.org/en-US/docs/Web/API/NavigatorLogin/setStatus">NavigatorLogin.setStatus()</see>
/// </summary>
public enum FedCmLoginStatus
{
    /// <summary>
    /// The user has an account signed in with this provider. Until it is set, the browser skips the
    /// provider's accounts endpoint entirely and FedCM finds nothing.
    /// </summary>
    LoggedIn,

    /// <summary>
    /// No account is signed in here. Set on sign-out, so the browser stops offering the provider.
    /// </summary>
    LoggedOut,
}
