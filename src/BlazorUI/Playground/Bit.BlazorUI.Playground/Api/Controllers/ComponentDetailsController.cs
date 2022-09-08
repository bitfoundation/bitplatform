using System.Reflection;
using System.Xml.Linq;
using Bit.BlazorUI.Playground.Shared.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace Bit.BlazorUI.Playground.Api.Controllers;

[ApiController]
[Route("api/[controller]/[action]")]
public class ComponentDetailsController : ControllerBase
{
    private static XDocument SummariesXmlDocument = null;
    private static readonly Assembly ComponentsAssembly = Assembly.GetAssembly(typeof(BitButton));

    [HttpGet]
    public async Task<ActionResult<List<ComponentPropertyDetailsDto>>> GetProperties(string name)
    {
        SummariesXmlDocument ??= await LoadSummariesXmlDocumentAsync();

        if (string.IsNullOrWhiteSpace(name))
            return BadRequest("Component Name is empty.");

        var componentType = ComponentsAssembly.ExportedTypes
                                              .FirstOrDefault(type =>
                                              {
                                                  if (type.IsGenericType)
                                                  {
                                                      var typeName = type.Name[..type.Name.IndexOf("`")];
                                                      return typeName.Equals(name, StringComparison.InvariantCultureIgnoreCase);
                                                  }

                                                  return type.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase);
                                              });

        if (componentType is null)
            return NotFound("No component type found.");

        var concreteComponentType = componentType.IsGenericType ? componentType.MakeGenericType(typeof(string)) : componentType;

        var componentInstance = Activator.CreateInstance(concreteComponentType);

        var componentNamePrefix = $"{componentType.FullName}.";

        var baseComponentType = typeof(BitComponentBase);
        var baseComponentNamePrefix = $"{baseComponentType.FullName}.";

        return Ok(componentType.GetProperties()
                              .Where(p => Attribute.IsDefined(p, typeof(Microsoft.AspNetCore.Components.ParameterAttribute)))
                              .Select(prop =>
                              {
                                  var xmlProperty = SummariesXmlDocument?.Descendants()
                                                            .Attributes()
                                                            .FirstOrDefault(a => a.Value.Contains(componentNamePrefix + prop.Name) || a.Value.Contains(baseComponentNamePrefix + prop.Name));

                                  var typeName = GetTypeName(prop.PropertyType);
                                  return new
                                  {
                                      prop.Name,
                                      Type = typeName,
                                      DefaultValue = GetDefaulValue(prop, componentInstance, typeName, concreteComponentType),
                                      Description = xmlProperty?.Parent.Element("summary")?.Value.Trim(),
                                  };
                              }));
    }

    private static async Task<XDocument> LoadSummariesXmlDocumentAsync()
    {
        string path = Path.Combine(AppContext.BaseDirectory, $"{ComponentsAssembly.GetName().Name}.xml");

        if (System.IO.File.Exists(path) is false) return null;

        var stream = System.IO.File.OpenRead(path);
        return await XDocument.LoadAsync(stream, LoadOptions.None, default);
    }

    private static string GetTypeName(Type type)
    {
        if (type.IsGenericType)
        {
            var arguments = string.Join(", ", type.GetGenericArguments().Select(x => x.Name));
            var mainType = type.Name[..type.Name.IndexOf("`")];
            return $"{mainType}<{GetTypeNameOrAlias(arguments)}>";
        }

        return GetTypeNameOrAlias(type.Name);
    }

    private static string GetTypeNameOrAlias(string typeName) =>
                typeName switch
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

    private static string GetDefaulValue(PropertyInfo property, object instance, string typeName, Type concreteComponentType)
    {
        if (concreteComponentType.IsGenericType)
        {
            property = concreteComponentType.GetProperty(property.Name);
        }

        var value = property.GetValue(instance)?.ToString();

        if (string.IsNullOrWhiteSpace(value) || property.PropertyType.IsGenericType is false) return value;

        return $"new {typeName}()";
    }
}
