using System.Xml.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using Bit.Client.Web.BlazorUI.Playground.Shared.Dtos;

namespace Bit.Client.Web.BlazorUI.Playground.Api.Controllers;

[ApiController]
[Route("component-details")]
public class ComponentDetailsController : ControllerBase
{
    private static XDocument SummariesXmlDocument = null;
    private static readonly Assembly ComponentsAssembly = Assembly.GetAssembly(typeof(BitButton));

    [HttpGet("properties")]
    public async Task<ActionResult<List<ComponentPropertyDetailsDto>>> GetProperties(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return BadRequest("Component Name is empty.");

        var componentType = ComponentsAssembly.ExportedTypes
                                              .Where(c => c.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase))
                                              .FirstOrDefault();

        if (componentType == null)
            return NotFound("No component type found.");

        if (SummariesXmlDocument == null)
        {
            SummariesXmlDocument = await LoadSummariesXmlDocumentAsync();
        }

        var componentInstance = Activator.CreateInstance(componentType);

        var prefix = $"{componentType.FullName}.";
        return Ok(componentType.GetProperties()
                              .Where(p => Attribute.IsDefined(p, typeof(Microsoft.AspNetCore.Components.ParameterAttribute)))
                              .Select(p =>
                              {
                                  var xmlProperty = SummariesXmlDocument?.Descendants()
                                                            .Attributes()
                                                            .Where(a => a.Value.Contains(prefix + p.Name))
                                                            .FirstOrDefault();

                                  return new
                                  {
                                      p.Name,
                                      Type = p.PropertyType.Name,
                                      DefaultValue = p.GetValue(componentInstance)?.ToString(),
                                      Description = xmlProperty?.Parent.Element("summary")?.Value.Trim(),
                                  };
                              }));
    }

    private static async Task<XDocument> LoadSummariesXmlDocumentAsync()
    {
        string path = Path.Combine(AppContext.BaseDirectory, $"{ComponentsAssembly.GetName().Name}.xml");

        if (System.IO.File.Exists(path) == false) return null;

        var stream = System.IO.File.OpenRead(path);
        return await XDocument.LoadAsync(stream, LoadOptions.None, default);
    }
}

