using System.Text.Json.Serialization;

namespace Bit.Bonboarding;

/// <summary>How a target is described to the browser layer.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<BonboardingTargetKind>))]
public enum BonboardingTargetKind
{
    /// <summary>No element; the step is a centred card.</summary>
    None,
    /// <summary>A CSS selector, re-queried on every measurement so re-renders are survivable.</summary>
    Selector,
    /// <summary>A captured element reference.</summary>
    Element,
}
