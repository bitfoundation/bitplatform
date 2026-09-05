using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;

namespace Bit.Butil;

/// <summary>
/// The per-element half of the CSS Typed OM:
/// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Element/computedStyleMap">computedStyleMap()</see>
/// for reading and
/// <see href="https://developer.mozilla.org/en-US/docs/Web/API/HTMLElement/attributeStyleMap">attributeStyleMap</see>
/// for writing.
/// </summary>
/// <remarks>
/// The difference from the old CSSOM is types: a computed length arrives as the number 16 with the
/// unit <c>"px"</c> rather than the string <c>"16px"</c>, and a width is written as a number and a
/// unit rather than as concatenated text. Fewer parse steps, and no chance of building a malformed
/// declaration.
/// <br/>
/// Chromium only. Where <see cref="IsStyleMapSupported"/> is false these all return null / false, so
/// the ordinary string-based style APIs remain the portable choice.
/// <br/>
/// Blazor owns the DOM it rendered, and a diff can undo anything written here on the next render -
/// the same caveat as every other inline-style write.
/// </remarks>
public static class ElementReferenceStyleMapExtensions
{
    /// <summary>True when the runtime implements the CSS Typed OM.</summary>
    public static ValueTask<bool> IsStyleMapSupported(this ElementReference element)
        => ElementReferenceExtensions.GetRuntime(element).Invoke<bool>("BitButil.css.isSupported");

    /// <summary>
    /// Reads a computed value as a number and a unit.
    /// </summary>
    /// <param name="element">The element to read from.</param>
    /// <param name="property">A CSS property name, including custom properties (<c>"--brand-hue"</c>).</param>
    /// <returns>The value, or null when the runtime has no Typed OM.</returns>
    /// <remarks>
    /// Computed, so this reflects the cascade and layout - a percentage width comes back resolved to
    /// pixels.
    /// </remarks>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(CssValue))]
    public static ValueTask<CssValue?> GetComputedValue(this ElementReference element, string property)
        => ElementReferenceExtensions.GetRuntime(element).Invoke<CssValue?>("BitButil.css.computedValue", element, property);

    /// <summary>
    /// Every property name in the element's computed style map. Empty when the runtime has no Typed OM.
    /// </summary>
    /// <remarks>Hundreds of entries - useful for exploring, rarely for shipping.</remarks>
    public static ValueTask<string[]> GetComputedProperties(this ElementReference element)
        => ElementReferenceExtensions.GetRuntime(element).Invoke<string[]>("BitButil.css.computedProperties", element);

    /// <summary>
    /// Reads an <b>inline</b> style value as a number and a unit - what the element's own
    /// <c>style</c> attribute says, not what the cascade computed.
    /// </summary>
    /// <returns>The value, or null when it isn't set inline or the runtime has no Typed OM.</returns>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(CssValue))]
    public static ValueTask<CssValue?> GetStyleValue(this ElementReference element, string property)
        => ElementReferenceExtensions.GetRuntime(element).Invoke<CssValue?>("BitButil.css.styleValue", element, property);

    /// <summary>
    /// Writes an inline style value as a typed number.
    /// </summary>
    /// <param name="element">The element to style.</param>
    /// <param name="property">A CSS property name.</param>
    /// <param name="value">The numeric part.</param>
    /// <param name="unit">
    /// The unit factory to use - <c>"px"</c>, <c>"percent"</c>, <c>"deg"</c>, <c>"rem"</c>,
    /// <c>"fr"</c>, <c>"s"</c>… Empty for a unitless number.
    /// </param>
    /// <returns>False when the runtime has no Typed OM, or the value isn't valid for the property.</returns>
    public static ValueTask<bool> SetStyleValue(this ElementReference element, string property, double value, string unit = "px")
        => ElementReferenceExtensions.GetRuntime(element).Invoke<bool>("BitButil.css.setStyleValue", element, property, value, unit);

    /// <summary>
    /// Writes an inline style value from text, for the values that aren't a number and a unit -
    /// a colour, a keyword, a gradient.
    /// </summary>
    /// <returns>False when the runtime has no Typed OM, or the text isn't valid for the property.</returns>
    /// <remarks>
    /// Unlike a plain <c>style</c> write, an invalid value is rejected rather than silently ignored,
    /// which is the reason to prefer this even for text.
    /// </remarks>
    public static ValueTask<bool> SetStyleText(this ElementReference element, string property, string value)
        => ElementReferenceExtensions.GetRuntime(element).Invoke<bool>("BitButil.css.setStyleText", element, property, value);

    /// <summary>Removes one inline style property. False when the runtime has no Typed OM.</summary>
    public static ValueTask<bool> DeleteStyleValue(this ElementReference element, string property)
        => ElementReferenceExtensions.GetRuntime(element).Invoke<bool>("BitButil.css.deleteStyleValue", element, property);

    /// <summary>Removes every inline style property. False when the runtime has no Typed OM.</summary>
    public static ValueTask<bool> ClearStyleValues(this ElementReference element)
        => ElementReferenceExtensions.GetRuntime(element).Invoke<bool>("BitButil.css.clearStyleValues", element);

    /// <summary>Every property name currently set inline. Empty when the runtime has no Typed OM.</summary>
    public static ValueTask<string[]> GetStyleProperties(this ElementReference element)
        => ElementReferenceExtensions.GetRuntime(element).Invoke<string[]>("BitButil.css.styleProperties", element);
}
