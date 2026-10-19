namespace Bit.Bonboarding;

/// <summary>
/// Keeps two <c>BonboardingRoot</c> hosts from rendering the same tour twice.
/// </summary>
/// <remarks>
/// A host given an explicit <c>Session</c> claims it. Hosts without one -- typically the single
/// host in the application layout -- render whichever tour is active unless another host has
/// claimed it. That is what lets a page take over the rendering of one particular tour, for a
/// custom card or a second popover, while the layout keeps handling everything else.
/// </remarks>
internal sealed class BonboardingHostRegistry
{
    private readonly HashSet<string> _claimed = new(StringComparer.Ordinal);

    /// <summary>Raised when a claim is taken or released, so ambient hosts can re-evaluate.</summary>
    public event EventHandler? Changed;

    public bool IsClaimed(string sessionId) => _claimed.Contains(sessionId);

    public void Claim(string sessionId)
    {
        if (_claimed.Add(sessionId)) Changed?.Invoke(this, EventArgs.Empty);
    }

    public void Release(string sessionId)
    {
        if (_claimed.Remove(sessionId)) Changed?.Invoke(this, EventArgs.Empty);
    }
}
