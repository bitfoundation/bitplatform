namespace Bit.Butil;

/// <summary>
/// The visibility of one of the browser's own bars (menu, toolbar, status bar and friends). Modern
/// engines report a fixed answer rather than the real state - typically <c>true</c> in a normal tab
/// and <c>false</c> in a popup - so this reads as "is this a chrome-less window", not as "is that
/// particular bar on screen".
/// <br/>
/// <see href="https://developer.mozilla.org/en-US/docs/Web/API/BarProp">BarProp</see>
/// </summary>
public class BarProp
{
    /// <summary>Whether the bar is reported as visible.</summary>
    public bool Visible { get; set; }
}
