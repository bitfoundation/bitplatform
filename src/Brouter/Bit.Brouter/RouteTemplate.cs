namespace Bit.Brouter;

internal class RouteTemplate
{
    public static readonly char[] Separators = ['/'];

    public string Template { get; }
    public TemplateSegment[] TemplateSegments { get; }

    public RouteTemplate(string template, TemplateSegment[] segments)
    {
        Template = template;
        TemplateSegments = segments;
    }
}
