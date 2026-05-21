namespace Bit.Brouter;

internal class RouteTemplate
{
    private static readonly char[] _separators = ['/'];

    /// <summary>Route path separator characters (immutable view).</summary>
    public static ReadOnlySpan<char> Separators => _separators;

    public string Template { get; }
    public TemplateSegment[] TemplateSegments { get; }

    public RouteTemplate(string template, TemplateSegment[] segments)
    {
        Template = template;
        TemplateSegments = segments;
    }
}
