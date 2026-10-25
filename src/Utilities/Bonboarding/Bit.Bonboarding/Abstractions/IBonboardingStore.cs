namespace Bit.Bonboarding;

/// <summary>
/// Where onboarding progress lives. Swap the implementation to persist server-side, per tenant, or
/// into an existing user-preferences store.
/// </summary>
public interface IBonboardingStore
{
    /// <summary>Reads the record for a tour, or <see langword="null"/> when the user has no history with it.</summary>
    ValueTask<BonboardingRecord?> GetAsync(string tourId, CancellationToken cancellationToken = default);

    /// <summary>Writes (or overwrites) the record for a tour.</summary>
    ValueTask SetAsync(BonboardingRecord record, CancellationToken cancellationToken = default);

    /// <summary>Forgets one tour, so it can run again from the start.</summary>
    ValueTask RemoveAsync(string tourId, CancellationToken cancellationToken = default);

    /// <summary>Forgets every tour this store knows about.</summary>
    ValueTask ClearAsync(CancellationToken cancellationToken = default);
}
