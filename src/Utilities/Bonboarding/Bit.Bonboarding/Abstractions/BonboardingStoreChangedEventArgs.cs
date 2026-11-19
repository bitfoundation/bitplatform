namespace Bit.Bonboarding;

/// <summary>Payload of <see cref="IBonboardingStoreEvents.Changed"/>.</summary>
public sealed class BonboardingStoreChangedEventArgs(string? tourId) : EventArgs
{
    /// <summary>
    /// The record that changed, as passed to <see cref="IBonboardingStore.GetAsync"/> (a tour id, or
    /// <c>checklist:{id}</c>); <see langword="null"/> when every record may have changed, as after a storage clear.
    /// </summary>
    public string? TourId { get; } = tourId;
}
