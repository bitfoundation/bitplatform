namespace Bit.Brouter;

internal class RouteTemplate
{
    private static readonly char[] _separators = ['/'];

    /// <summary>Route path separator characters (immutable view).</summary>
    public static ReadOnlySpan<char> Separators => _separators;

    public string Template { get; }
    public IReadOnlyList<TemplateSegment> TemplateSegments { get; }

    public RouteTemplate(string template, TemplateSegment[] segments)
    {
        ArgumentNullException.ThrowIfNull(template);
        ArgumentNullException.ThrowIfNull(segments);
        Template = template;
        // Defensive copy so callers can't mutate internal state via the original array reference.
        var copy = new TemplateSegment[segments.Length];
        Array.Copy(segments, copy, segments.Length);
        // Wrap in a read-only collection so callers can't downcast TemplateSegments back to
        // TemplateSegment[] and mutate it; the array is still defensively copied above.
        TemplateSegments = Array.AsReadOnly(copy);
    }
}
