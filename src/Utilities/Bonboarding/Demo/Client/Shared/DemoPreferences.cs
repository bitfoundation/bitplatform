namespace Bit.Bonboarding.Demo.Client.Shared;

/// <summary>
/// What the visitor of the demo chose, for this circuit or app only: the "turn tips off" switch of the lifecycle page,
/// which <c>BonboardingOptions.CanAutoStart</c> reads.
/// </summary>
public sealed class DemoPreferences
{
    public bool MuteTips { get; set; }
}
