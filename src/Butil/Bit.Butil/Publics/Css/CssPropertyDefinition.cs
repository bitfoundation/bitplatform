namespace Bit.Butil;

/// <summary>
/// A custom property registered through
/// <see href="https://developer.mozilla.org/en-US/docs/Web/API/CSS/registerProperty_static">CSS.registerProperty()</see> -
/// which is what makes a <c>--custom-property</c> animatable and type-checked instead of an opaque
/// string.
/// </summary>
/// <remarks>
/// An unregistered custom property is a token stream: the browser can't interpolate it, so a
/// transition on it snaps rather than animates. Register it with a syntax and it becomes a real
/// typed value the animation engine understands.
/// </remarks>
public class CssPropertyDefinition
{
    /// <summary>The property name, including the leading dashes: <c>"--brand-hue"</c>.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The grammar the value must match: <c>"&lt;color&gt;"</c>, <c>"&lt;length&gt;"</c>,
    /// <c>"&lt;number&gt;"</c>, <c>"&lt;length-percentage&gt;"</c>, or <c>"*"</c> for anything
    /// (which gives up the animation the registration was for). Empty means <c>"*"</c>.
    /// </summary>
    public string Syntax { get; set; } = string.Empty;

    /// <summary>Whether descendants inherit it, like a normal custom property.</summary>
    public bool Inherits { get; set; }

    /// <summary>
    /// The value used where nothing set it. Required for any syntax other than <c>"*"</c> - the
    /// registration fails without one.
    /// </summary>
    public string InitialValue { get; set; } = string.Empty;
}
