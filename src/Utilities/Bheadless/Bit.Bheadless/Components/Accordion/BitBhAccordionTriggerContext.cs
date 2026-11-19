namespace Bit.Bheadless;

/// <summary>Render-fragment context exposed by <see cref="BitBhAccordionTrigger"/>.</summary>
public sealed record BitBhAccordionTriggerContext
{
    /// <summary>Whether the item is open.</summary>
    public required bool IsOpen { get; init; }

    /// <summary>Whether the item is disabled.</summary>
    public required bool Disabled { get; init; }

    /// <summary>Whether the trigger can be activated.</summary>
    public bool Interactive => !Disabled;
}
