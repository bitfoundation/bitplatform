namespace Bit.Bonboarding;

/// <summary>
/// Receives every <see cref="BonboardingEvent"/>. Register any number of implementations in DI to
/// forward tour telemetry to your analytics provider.
/// </summary>
public interface IBonboardingObserver
{
    ValueTask OnEventAsync(BonboardingEvent onboardingEvent, CancellationToken cancellationToken = default);
}
