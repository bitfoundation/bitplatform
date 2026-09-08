namespace Bit.Butil;

/// <summary>
/// One CSS value read through the Typed OM - a number and its unit rather than a string that has to
/// be parsed.
/// </summary>
/// <remarks>
/// The whole point of the Typed OM is that <c>"16px"</c> arrives as 16 and <c>"px"</c>, so nobody
/// has to write another regex. Values that aren't numeric (a keyword, a colour, a transform list)
/// come back with <see cref="IsNumeric"/> false and only <see cref="Text"/> filled in.
/// </remarks>
public class CssValue
{
    /// <summary>The numeric part, or 0 when the value isn't numeric.</summary>
    public double Value { get; set; }

    /// <summary>
    /// The unit: <c>"px"</c>, <c>"percent"</c>, <c>"deg"</c>, <c>"number"</c>… Empty when the value
    /// isn't numeric.
    /// </summary>
    public string Unit { get; set; } = string.Empty;

    /// <summary>How the value serializes - always filled in, whatever the kind.</summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>True when <see cref="Value"/> and <see cref="Unit"/> mean something.</summary>
    public bool IsNumeric { get; set; }
}
