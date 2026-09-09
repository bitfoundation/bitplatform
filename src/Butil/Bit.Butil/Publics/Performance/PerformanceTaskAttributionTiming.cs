namespace Bit.Butil;

/// <summary>
/// The browsing context a long task is blamed on. It is deliberately coarse: the specification only
/// identifies the frame, never the script or the function, so this says <i>where</i> the work
/// happened and never <i>what</i> it was.
/// <br />
/// <see href="https://developer.mozilla.org/en-US/docs/Web/API/TaskAttributionTiming">https://developer.mozilla.org/en-US/docs/Web/API/TaskAttributionTiming</see>
/// </summary>
public class PerformanceTaskAttributionTiming : PerformanceEntry
{
    /// <summary>The kind of container the task ran in: <c>"iframe"</c>, <c>"embed"</c>, <c>"object"</c> or <c>"window"</c>.</summary>
    public string? ContainerType { get; set; }

    /// <summary>The container element's <c>src</c>.</summary>
    public string? ContainerSrc { get; set; }

    /// <summary>The container element's <c>id</c>.</summary>
    public string? ContainerId { get; set; }

    /// <summary>The container element's <c>name</c>.</summary>
    public string? ContainerName { get; set; }
}
