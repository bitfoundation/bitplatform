namespace Bit.Bheadless;

/// <summary>Cascading context provided by <see cref="BitBhRadioGroup"/> to its radios.</summary>
public sealed class BitBhRadioGroupContext
{
    internal BitBhRadioGroupContext(
        string? selectedValue,
        bool disabled,
        bool required,
        BitBhRadioGroupOrientation orientation,
        BitBhRadioGroup group,
        BitBhCollection<BitBhRadio> radios)
    {
        SelectedValue = selectedValue;
        Disabled = disabled;
        Required = required;
        Orientation = orientation;
        Group = group;
        Radios = radios;
    }

    internal BitBhRadioGroup Group { get; }
    internal BitBhCollection<BitBhRadio> Radios { get; }

    /// <summary>The checked value, or null.</summary>
    public string? SelectedValue { get; }

    /// <summary>Whether the whole group is disabled.</summary>
    public bool Disabled { get; }

    /// <summary>Whether the group is required.</summary>
    public bool Required { get; }

    /// <summary>The orientation.</summary>
    public BitBhRadioGroupOrientation Orientation { get; }

    /// <summary>Checks the radio with <paramref name="value"/>.</summary>
    public Task SelectAsync(string value) => Group.SelectAsync(value);
}
