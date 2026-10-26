namespace Bit.BlazorHeadlessUI;

/// <summary>Cascading context provided by <see cref="BitBhAccordionItem"/> to its header, trigger and content.</summary>
public sealed class BitBhAccordionItemContext
{
    /// <summary>The item's value.</summary>
    public required string Value { get; init; }

    /// <summary>Whether the item is open.</summary>
    public required bool IsOpen { get; init; }

    /// <summary>Whether the item is disabled.</summary>
    public required bool Disabled { get; init; }

    /// <summary>The trigger id.</summary>
    public required string TriggerId { get; init; }

    /// <summary>The content id.</summary>
    public required string ContentId { get; init; }

    /// <summary>Toggles the item.</summary>
    public required Action Toggle { get; init; }

    internal BitBhAccordionContext? Accordion { get; init; }
}

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
