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
