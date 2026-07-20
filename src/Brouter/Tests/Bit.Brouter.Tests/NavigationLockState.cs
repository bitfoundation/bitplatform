namespace Bit.Brouter.Tests;

/// <summary>
/// Test-controlled behavior for a <see cref="NavigationLockProbe"/>: whether it blocks the pending
/// navigation, redirects it, or parks awaiting a simulated confirmation dialog - plus a log of
/// every lock callback it received (label:kind:details).
/// </summary>
public class NavigationLockState
{
    /// <summary>While true, the probe cancels every deactivating/renavigating navigation.</summary>
    public bool Locked { get; set; }

    /// <summary>When set, the probe redirects the pending navigation here instead of cancelling.</summary>
    public string? RedirectTo { get; set; }

    /// <summary>
    /// When set, the probe awaits it before deciding - the custom-confirmation-dialog flow. The
    /// result is the user's answer: <c>true</c> blocks the navigation, <c>false</c> lets it proceed.
    /// </summary>
    public TaskCompletionSource<bool>? Prompt { get; set; }

    /// <summary>Every lock callback the probe received, in order.</summary>
    public List<string> Log { get; } = [];
}
