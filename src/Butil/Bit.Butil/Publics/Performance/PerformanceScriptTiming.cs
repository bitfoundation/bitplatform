namespace Bit.Butil;

/// <summary>
/// One script execution inside a long animation frame, named down to the function and the source
/// position - the attribution the long-task API never had.
/// <br />
/// <see href="https://developer.mozilla.org/en-US/docs/Web/API/PerformanceScriptTiming">https://developer.mozilla.org/en-US/docs/Web/API/PerformanceScriptTiming</see>
/// </summary>
public class PerformanceScriptTiming : PerformanceEntry
{
    /// <summary>How the script was entered: <c>"user-callback"</c>, <c>"event-listener"</c>, <c>"resolve-promise"</c>, <c>"classic-script"</c>, <c>"module-script"</c>...</summary>
    public string? InvokerType { get; set; }

    /// <summary>What did the entering, e.g. <c>"BUTTON#save.onclick"</c> or the script's URL.</summary>
    public string? Invoker { get; set; }

    /// <summary>When the script's own code started running, after any compilation.</summary>
    public double ExecutionStart { get; set; }

    /// <summary>The URL of the script's source.</summary>
    public string? SourceUrl { get; set; }

    /// <summary>The name of the function that was entered.</summary>
    public string? SourceFunctionName { get; set; }

    /// <summary>The character offset of that function within its source.</summary>
    public long SourceCharPosition { get; set; }

    /// <summary>Milliseconds spent inside synchronous blocking calls such as <c>alert()</c>.</summary>
    public double PauseDuration { get; set; }

    /// <summary>Milliseconds the script spent forcing style and layout - the classic layout-thrashing cost.</summary>
    public double ForcedStyleAndLayoutDuration { get; set; }

    /// <summary>Which window the script belongs to relative to this one: <c>"self"</c>, <c>"descendant"</c>, <c>"ancestor"</c>, <c>"same-page"</c> or <c>"other"</c>.</summary>
    public string? WindowAttribution { get; set; }
}
