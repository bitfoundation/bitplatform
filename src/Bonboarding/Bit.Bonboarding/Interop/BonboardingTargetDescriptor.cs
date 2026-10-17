using Microsoft.AspNetCore.Components;

namespace Bit.Bonboarding;

/// <summary>A target flattened into something the browser layer can look up.</summary>
public sealed record BonboardingTargetDescriptor
{
    /// <summary>A descriptor that matches nothing.</summary>
    public static BonboardingTargetDescriptor None { get; } = new() { Kind = BonboardingTargetKind.None };

    public BonboardingTargetKind Kind { get; init; }

    /// <summary>Set when <see cref="Kind"/> is <see cref="BonboardingTargetKind.Selector"/>.</summary>
    public string? Selector { get; init; }

    /// <summary>Set when <see cref="Kind"/> is <see cref="BonboardingTargetKind.Element"/>.</summary>
    public ElementReference? Element { get; init; }

    /// <summary>Flattens a <see cref="BonboardingStepTarget"/>; dynamic targets must be resolved first.</summary>
    public static BonboardingTargetDescriptor From(BonboardingStepTarget target) => target switch
    {
        BonboardingStepTarget.CssTarget css => new BonboardingTargetDescriptor { Kind = BonboardingTargetKind.Selector, Selector = css.Selector },
        BonboardingStepTarget.AnchorTarget anchor => new BonboardingTargetDescriptor { Kind = BonboardingTargetKind.Selector, Selector = anchor.Selector },
        BonboardingStepTarget.ElementTarget element => new BonboardingTargetDescriptor { Kind = BonboardingTargetKind.Element, Element = element.Reference },
        _ => None,
    };
}
