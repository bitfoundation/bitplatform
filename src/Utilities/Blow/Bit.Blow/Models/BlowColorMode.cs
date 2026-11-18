namespace Bit.Blow;

/// <summary>
/// Color theme applied to the flow.
/// </summary>
public enum BlowColorMode
{
    /// <summary>The light theme (the default).</summary>
    Light,

    /// <summary>The dark theme.</summary>
    Dark,

    /// <summary>Follows the OS <c>prefers-color-scheme</c> setting.</summary>
    System
}
