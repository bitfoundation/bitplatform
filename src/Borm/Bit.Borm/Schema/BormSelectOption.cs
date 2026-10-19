namespace Bit.Borm;

/// <summary>
/// A selectable option for <see cref="BormFieldType.Select"/>, <see cref="BormFieldType.MultiSelect"/>
/// and <see cref="BormFieldType.Radio"/> fields.
/// </summary>
/// <param name="Value">The underlying value stored on the model when the option is chosen.</param>
/// <param name="Label">The human-readable label shown to the user.</param>
/// <param name="Disabled">Whether the option is selectable.</param>
/// <param name="Group">Optional group name, allowing options to be rendered under option-groups.</param>
public sealed record BormSelectOption(string Value, string Label, bool Disabled = false, string? Group = null)
{
    /// <summary>Creates an option where the value and label are identical.</summary>
    public static BormSelectOption Of(string value) => new(value, value);
}
