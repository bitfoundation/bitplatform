namespace Bit.Bmotion;

/// <summary>
/// Options for <see cref="BmotionScrollTracker.ObserveAsync(BmScrollOptions, Func{BmScrollInfo, Task})"/> -
/// motion.dev's <c>useScroll({ container, target, offset })</c>:
/// <code>
/// new BmScrollOptions
/// {
///     TargetId = "hero",                       // track this element's journey through the viewport
///     Offset = ["start end", "end start"],     // default: enters at the bottom → leaves at the top
/// }
/// </code>
/// Each offset is <c>"&lt;targetEdge&gt; &lt;containerEdge&gt;"</c> where an edge is
/// <c>start</c> (0), <c>center</c> (0.5), <c>end</c> (1) or a 0–1 number. The first offset is
/// where progress is 0 (the alignment at which tracking begins), the second where it is 1.
/// </summary>
public class BmScrollOptions
{
    /// <summary>The scroll container element id, or <c>null</c> for the window.</summary>
    public string? ContainerId { get; set; }

    /// <summary>
    /// Element id whose progress through the container is tracked (reported via
    /// <see cref="BmScrollInfo.TargetProgress"/>). Null = plain container scroll progress.
    /// </summary>
    public string? TargetId { get; set; }

    /// <summary>
    /// The two alignment points that map to progress 0 and 1. Default:
    /// <c>["start end", "end start"]</c> (target enters at the container's end edge → target's
    /// end passes the container's start edge). Vertical axis.
    /// </summary>
    public string[]? Offset { get; set; }

    internal object? ToJsObject()
    {
        if (TargetId is null) return null;
        // Surface a malformed Offset early instead of silently substituting the default pair.
        if (Offset is { Length: not 2 })
            throw new ArgumentException(
                $"Scroll Offset must contain exactly 2 entries (progress 0 and 1), got {Offset.Length}.");
        var offset = Offset ?? ["start end", "end start"];
        return new Dictionary<string, object?>
        {
            ["targetId"] = TargetId,
            ["offsets"] = new[] { ParseOffset(offset[0]), ParseOffset(offset[1]) },
        };
    }

    private static double[] ParseOffset(string offset)
    {
        var parts = offset.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length != 2)
            throw new ArgumentException(
                $"Scroll offset '{offset}' must be \"<targetEdge> <containerEdge>\" (e.g. \"start end\").");
        return [ParseEdge(parts[0], offset), ParseEdge(parts[1], offset)];
    }

    private static double ParseEdge(string edge, string offset) => edge.ToLowerInvariant() switch
    {
        "start" => 0,
        "center" => 0.5,
        "end" => 1,
        _ => double.TryParse(edge, System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out var v) && v is >= 0 and <= 1
            ? v
            : throw new ArgumentException(
                $"Unknown edge '{edge}' in scroll offset '{offset}'. Use start, center, end or a 0-1 number."),
    };
}
