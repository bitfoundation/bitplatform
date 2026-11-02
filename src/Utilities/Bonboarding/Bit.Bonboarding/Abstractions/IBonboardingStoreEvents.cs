namespace Bit.Bonboarding;

/// <summary>
/// Implemented by an <see cref="IBonboardingStore"/> that can tell when its records change somewhere else: in another
/// browser tab (the default store, through the <c>storage</c> event), or on another device (a server-backed store
/// pushing changes over SignalR, say). The service listens when the registered store implements it: a tour finished
/// elsewhere stops here instead of running on and overwriting that, and checklists and tour lists refresh.
/// </summary>
public interface IBonboardingStoreEvents
{
    /// <summary>
    /// Raised when a record was changed outside this scope. A store raising it must already return the new value
    /// from <see cref="IBonboardingStore.GetAsync"/>.
    /// </summary>
    event EventHandler<BonboardingStoreChangedEventArgs>? Changed;
}

/// <summary>Payload of <see cref="IBonboardingStoreEvents.Changed"/>.</summary>
public sealed class BonboardingStoreChangedEventArgs(string? tourId) : EventArgs
{
    /// <summary>
    /// The record that changed, as passed to <see cref="IBonboardingStore.GetAsync"/> (a tour id, or
    /// <c>checklist:{id}</c>); <see langword="null"/> when every record may have changed, as after a storage clear.
    /// </summary>
    public string? TourId { get; } = tourId;
}
