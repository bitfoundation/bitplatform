namespace Bit.BlazorUI;

/// <summary>
/// Determines the corner shape of the <see cref="BitTag"/>.
/// </summary>
public enum BitTagShape
{
    /// <summary>
    /// Takes the chip corner of the current theme, which is a pill in Cupertino and a small radius in Fluent and Material.
    /// </summary>
    Rounded,

    /// <summary>
    /// Rounds the corner fully, so the tag is always a pill whatever the theme says.
    /// </summary>
    Circular,

    /// <summary>
    /// Drops the corner altogether, so the tag is a rectangle.
    /// </summary>
    Square
}
