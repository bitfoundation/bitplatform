namespace Bit.Bheadless;

/// <summary>Whether an <see cref="BitBhAccordion"/> allows one or many open items.</summary>
public enum BitBhAccordionType
{
    /// <summary>Only one item can be open at a time.</summary>
    Single,

    /// <summary>Any number of items can be open.</summary>
    Multiple,
}

/// <summary>
/// Cascading context provided by <see cref="BitBhAccordion"/> to its items.
/// </summary>
public sealed class BitBhAccordionContext
{
    private readonly Func<string, bool> _isOpen;
    private readonly Action<string> _toggle;

    internal BitBhAccordionContext(
        Func<string, bool> isOpen,
        Action<string> toggle,
        bool disabled,
        BitBhAccordionType type)
    {
        _isOpen = isOpen;
        _toggle = toggle;
        Disabled = disabled;
        Type = type;
    }

    internal BitBhAccordion? Accordion { get; init; }

    internal BitBhCollection<BitBhAccordionTrigger>? Triggers { get; init; }

    /// <summary>Whether a single-mode accordion lets its open item be closed.</summary>
    public bool Collapsible { get; init; }

    /// <summary>The keyboard navigation orientation.</summary>
    public BitBhOrientation Orientation { get; init; } = BitBhOrientation.Vertical;

    /// <summary>Whether the item with <paramref name="value"/> is open.</summary>
    public bool IsOpen(string value) => _isOpen(value);

    /// <summary>Toggles the item with <paramref name="value"/>.</summary>
    public void Toggle(string value) => _toggle(value);

    /// <summary>Toggles the item with <paramref name="value"/>.</summary>
    public Task ToggleAsync(string value) => Accordion?.ToggleAsync(value) ?? Task.CompletedTask;

    /// <summary>Whether the whole accordion is disabled.</summary>
    public bool Disabled { get; }

    /// <summary>Single or multiple mode.</summary>
    public BitBhAccordionType Type { get; }
}
