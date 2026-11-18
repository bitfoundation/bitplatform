namespace Bit.Bheadless;

/// <summary>State of a <see cref="BitBhToggleGroup"/> cascaded to its items.</summary>
public sealed class BitBhToggleGroupContext
{
    internal BitBhToggleGroupContext(BitBhToggleGroup group, BitBhToggleGroupType type, IReadOnlyCollection<string> pressed, bool disabled)
    {
        Group = group;
        Type = type;
        Pressed = pressed;
        Disabled = disabled;
    }

    internal BitBhToggleGroup Group { get; }

    /// <summary>Single or multiple.</summary>
    public BitBhToggleGroupType Type { get; }

    /// <summary>The pressed values.</summary>
    public IReadOnlyCollection<string> Pressed { get; }

    /// <summary>Whether the group is disabled.</summary>
    public bool Disabled { get; }

    /// <summary>Whether <paramref name="value"/> is pressed.</summary>
    public bool IsPressed(string value) => Pressed.Contains(value);
}
