using System.Xml;
using System.Xml.Linq;
using System.Text.RegularExpressions;

namespace Boilerplate.Workbench.Infrastructure.Services;

/// <summary>
/// The summaries of components' parameters, read from the XML documentation their packages ship, which the build copies
/// next to each assembly.
/// </summary>
public static partial class WorkbenchComponentDocs
{
    private static readonly ConcurrentDictionary<Assembly, Dictionary<string, string>> summariesByAssembly = new();

    /// <summary>A parameter's summary; an override without one of its own takes the base type's.</summary>
    public static string? SummaryOf(PropertyInfo property)
    {
        for (var type = property.DeclaringType; type is not null && type != typeof(object); type = type.BaseType)
        {
            var definition = type.IsGenericType ? type.GetGenericTypeDefinition() : type;

            if (SummariesOf(definition.Assembly).GetValueOrDefault($"P:{definition.FullName?.Replace('+', '.')}.{property.Name}") is { } summary)
                return summary;
        }

        return null;
    }

    private static Dictionary<string, string> SummariesOf(Assembly assembly) => summariesByAssembly.GetOrAdd(assembly, Load);

    private static Dictionary<string, string> Load(Assembly assembly)
    {
        var path = string.IsNullOrEmpty(assembly.Location) ? null : Path.ChangeExtension(assembly.Location, ".xml");

        if (path is null || File.Exists(path) is false)
            return [];

        try
        {
            return XDocument.Load(path)
                            .Descendants("member")
                            .Where(member => ((string?)member.Attribute("name"))?.StartsWith("P:", StringComparison.Ordinal) is true && member.Element("summary") is not null)
                            .GroupBy(member => (string)member.Attribute("name")!)
                            .ToDictionary(group => group.Key, group => TextOf(group.First().Element("summary")!));
        }
        catch (Exception exception) when (exception is IOException or XmlException or UnauthorizedAccessException)
        {
            return [];
        }
    }

    /// <summary>An element's text with references written as the names they point at, and its white space collapsed.</summary>
    private static string TextOf(XElement element)
    {
        var text = string.Concat(element.Nodes().Select(node => node switch
        {
            XText plain => plain.Value,
            XElement { Name.LocalName: "see" or "seealso" } reference when reference.Value.Length is 0
                => ShortName((string?)reference.Attribute("cref") ?? (string?)reference.Attribute("langword") ?? string.Empty),
            XElement { Name.LocalName: "paramref" or "typeparamref" } reference => (string?)reference.Attribute("name") ?? string.Empty,
            XElement other => TextOf(other),
            _ => string.Empty
        }));

        return WhiteSpace().Replace(text, " ").Trim();
    }

    /// <summary>A cref's last name without its kind or generic arity, like BitColor for T:Bit.BlazorUI.BitColor.</summary>
    private static string ShortName(string cref)
    {
        var name = cref.Length > 2 && cref[1] is ':' ? cref[2..] : cref;
        name = name.Split('(')[0];

        return name[(name.LastIndexOf('.') + 1)..].Split('`')[0];
    }

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhiteSpace();
}
