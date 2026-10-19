namespace Bit.Bonboarding;

/// <summary>An event listener that advances the tour when the user acts on the page.</summary>
public sealed record BonboardingAdvanceTriggerDescriptor
{
    public required string EventName { get; init; }
    public string? Selector { get; init; }
    public string? MatchSelector { get; init; }
    public int DelayMs { get; init; }
}
