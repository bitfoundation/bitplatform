using System.Xml.Linq;
using System.Reflection;
using System.Collections;
using System.ComponentModel;
using ModelContextProtocol.Server;

namespace Bit.BlazorUI.Demo.Server.Controllers;

// Subject to change:
// This controller / mcp endpoint is designed to be used for experimental purposes.

[ApiController]
[McpServerToolType]
[Route("api/[controller]/[action]")]
public partial class ComponentDetailsController : AppControllerBase
{
    private static XDocument? SummariesXmlDocument = null;

    private static readonly Assembly[] ComponentsAssemblies = [typeof(_Imports).Assembly, typeof(Extras._Imports).Assembly];

    private static readonly Type[] ComponentTypes = [.. ComponentsAssemblies.SelectMany(asm => asm.GetExportedTypes()
                                                        .Where(type => typeof(BitComponentBase).IsAssignableFrom(type) && !type.IsAbstract))];

    [HttpGet]
    [McpServerTool(Name = nameof(GetParameters))]
    [Description("Gets the parameters of a specified component.")]
    public async Task<ComponentParameterDetailsDto[]> GetParameters(string componentName)
    {
        if (string.IsNullOrWhiteSpace(componentName))
            return [];

        SummariesXmlDocument ??= await LoadSummariesXmlDocumentAsync();

        var componentType = ComponentTypes.FirstOrDefault(type =>
        {
            var typeName = type.IsGenericType ? type.Name[..type.Name.IndexOf('`')] : type.Name;

            return typeName.Equals(componentName, StringComparison.InvariantCultureIgnoreCase);
        });

        if (componentType is null)
            return [];

        var concreteComponentType = componentType.IsGenericType ? componentType.MakeGenericType([.. Enumerable.Repeat(typeof(object), componentType.GetGenericArguments().Length)]) : componentType;

        var componentInstance = Activator.CreateInstance(concreteComponentType)
            ?? throw new InvalidOperationException($"Could not create an instance of {concreteComponentType.FullName}.");

        var componentNamePrefix = $"{componentType.FullName}.";

        var baseComponentType = typeof(BitComponentBase);
        var baseComponentNamePrefix = $"{baseComponentType.FullName}.";

        return [.. componentType.GetProperties()
                              .Where(p => Attribute.IsDefined(p, typeof(Microsoft.AspNetCore.Components.ParameterAttribute)))
                              .Select(prop =>
                              {
                                  var xmlProperty = SummariesXmlDocument?.Descendants()
                                                            .Attributes()
                                                            .FirstOrDefault(a => a.Value.Contains(componentNamePrefix + prop.Name) || a.Value.Contains(baseComponentNamePrefix + prop.Name));

                                  var typeName = GetTypeName(prop.PropertyType);

                                  return new ComponentParameterDetailsDto
                                  {
                                      Name = prop.Name,
                                      Type = typeName,
                                      DefaultValue = GetDefaultValue(prop, componentInstance!, typeName, concreteComponentType),
                                      Description = xmlProperty?.Parent?.Element("summary")?.Value?.Trim(),
                                  };
                              })];
    }

    private static async Task<XDocument?> LoadSummariesXmlDocumentAsync()
    {
        XDocument? mergedDoc = null;

        foreach (var asm in ComponentsAssemblies)
        {
            string path = Path.Combine(AppContext.BaseDirectory, $"{asm.GetName().Name}.xml");

            using var stream = System.IO.File.OpenRead(path);

            var doc = await XDocument.LoadAsync(stream, LoadOptions.None, default);

            if (mergedDoc is null)
            {
                mergedDoc = doc;
            }
            else
            {
                var membersElement = doc.Root?.Element("members");
                foreach (var member in membersElement!.Elements("member"))
                {
                    mergedDoc.Root!.Element("members")?.Add(member);
                }
            }
        }

        return mergedDoc;
    }

    private static string GetTypeName(Type type)
    {
        if (type.IsGenericType)
        {
            var arguments = string.Join(", ", type.GetGenericArguments().Select(x => x.Name));
            var mainType = type.Name[..type.Name.IndexOf('`', StringComparison.Ordinal)];
            return $"{mainType}<{GetTypeNameOrAlias(arguments)}>";
        }

        return GetTypeNameOrAlias(type.Name);
    }

    private static string GetTypeNameOrAlias(string typeName) => typeName switch
    {
        "Boolean" => "bool",
        "Byte" => "byte",
        "SByte" => "sbyte",
        "Char" => "char",
        "Decimal" => "decimal",
        "Double" => "double",
        "Single" => "float",
        "Int16" => "short",
        "UInt16" => "ushort",
        "Int32" => "int",
        "UInt32" => "uint",
        "Int64" => "long",
        "UInt64" => "ulong",
        "Object" => "object",
        "String" => "string",
        _ => typeName
    };

    private static string? GetDefaultValue(PropertyInfo? property, object instance, string typeName, Type concreteComponentType)
    {
        if (property is null) return null;

        if (concreteComponentType.IsGenericType)
        {
            property = concreteComponentType.GetProperty(property!.Name);
        }

        if (property!.PropertyType?.Name?.Contains("EventCallback", StringComparison.Ordinal) is true) return null;

        var value = property.GetValue(instance);

        if (value is null) return null;
        if (value is IList list && list.Count == 0) return "[]";
        if (value is IDictionary dictionary && dictionary.Count == 0) return "[]";

        return value.ToString();
    }
}
