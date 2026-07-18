using System.Xml.Linq;
using System.Reflection;
using System.ComponentModel;
using Bit.BlazorUI.Demo.Server.Services;
using Bit.BlazorUI.Demo.Client.Core.Shared;
using ModelContextProtocol.Server;
using Microsoft.AspNetCore.Components.Web;

namespace Bit.BlazorUI.Demo.Server.Controllers;

[ApiController]
[McpServerToolType]
[Route("api/[controller]/[action]")]
public partial class McpController : AppControllerBase
{
    [AutoInject] private HtmlRenderer htmlRenderer = default!;
    [AutoInject] private IHttpContextAccessor httpContextAccessor = default!;

    private static string[]? _allIconNames = null;
    private static XDocument? _summariesXmlDocument = null;
    private static readonly Assembly[] _componentsAssemblies = [typeof(_Imports).Assembly, typeof(Extras._Imports).Assembly];
    private static readonly Type[] _enumTypes = [.. _componentsAssemblies.SelectMany(asm => asm.GetExportedTypes().Where(t => t.IsEnum))];

    [HttpGet]
    [McpServerTool(Name = nameof(GetBitBlazorUIComponentsList))]
    [Description("Gets the list of all available components with their details.")]
    public async Task<ComponentListDetailsDto[]> GetBitBlazorUIComponentsList()
    {
        _summariesXmlDocument ??= await LoadSummariesXmlDocumentAsync();

        var components = new List<ComponentListDetailsDto>();

        void AddNodes(IEnumerable<BitNavItem> items)
        {
            foreach (var item in items)
            {
                if (item.ChildItems?.Any() == true)
                {
                    AddNodes(item.ChildItems);
                }

                if (!string.IsNullOrEmpty(item.Url) && item.Url.StartsWith("/components/", StringComparison.OrdinalIgnoreCase))
                {
                    var name = $"Bit{item.Text}";

                    var xmlProperty = _summariesXmlDocument?.Descendants("member")
                                            .FirstOrDefault(a => a.Attribute("name")?.Value == $"T:Bit.BlazorUI.{name}");

                    if (xmlProperty is null)
                    {
                        xmlProperty = _summariesXmlDocument?.Descendants("member")
                                                .FirstOrDefault(a => a.Attribute("name")?.Value.StartsWith($"T:Bit.BlazorUI.{name}`") == true);
                    }

                    var alsoKnownAs = item.Description?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

                    components.Add(new ComponentListDetailsDto
                    {
                        Name = name,
                        AlsoKnownAs = alsoKnownAs,
                        Description = xmlProperty?.Element("summary")?.Value?.Trim()
                    });
                }
            }
        }

        AddNodes(MainLayout.NavItems);

        return [.. components];
    }

    [HttpGet]
    [McpServerTool(Name = nameof(GetBitBlazorUIComponentDocs))]
    [Description("Gets the docs/examples of a specified component.")]
    public async Task<string> GetBitBlazorUIComponentDocs(string componentName)
    {
        if (string.IsNullOrWhiteSpace(componentName))
            return "Component name is required.";

        var demoPageType = typeof(Client.Core.Routes).Assembly
            .GetExportedTypes()
            .SingleOrDefault(t => string.Equals(t.Name, $"{componentName}Demo", StringComparison.OrdinalIgnoreCase));

        if (demoPageType is null)
            return "No demo page found for the specified component.";

        httpContextAccessor.HttpContext!.Items["RenderForMcpClient"] = true;

        var body = await htmlRenderer.Dispatcher.InvokeAsync(async () =>
        {
            var renderedComponent = await htmlRenderer.RenderComponentAsync(demoPageType);

            return renderedComponent.ToHtmlString();
        });

        var friendlyHtml = HtmlToLLMTextService.ToLlmFriendlyHtml(body);

        return friendlyHtml[..Math.Min(friendlyHtml.Length, 35_000)]; // Limit to 35K, while optimizing the docs with PreventRenderForMcp parameter.
    }

    [HttpGet]
    [McpServerTool(Name = nameof(GetBitBlazorUIEnumDetails))]
    [Description("Gets the details of a specified Bit.BlazorUI enum including its values and descriptions.")]
    public async Task<EnumValueDetailsDto[]?> GetBitBlazorUIEnumDetails(string enumName)
    {
        if (string.IsNullOrWhiteSpace(enumName))
            return null;

        _summariesXmlDocument ??= await LoadSummariesXmlDocumentAsync();

        var enumType = _enumTypes.FirstOrDefault(t =>
            t.Name.Equals(enumName, StringComparison.OrdinalIgnoreCase));

        if (enumType is null)
            return null;

        var values = Enum.GetNames(enumType).Select(name =>
        {
            var fieldXmlMember = _summariesXmlDocument?.Descendants("member")
                                    .FirstOrDefault(m => m.Attribute("name")?.Value == $"F:{enumType.FullName}.{name}");

            return new EnumValueDetailsDto
            {
                Name = name,
                Description = fieldXmlMember?.Element("summary")?.Value?.Trim()
            };
        }).ToArray();

        return values;
    }

    [HttpGet]
    [McpServerTool(Name = nameof(GetAllBitBlazorUIIconNames))]
    [Description("Gets all available BitIconName constant values.")]
    public string[] GetAllBitBlazorUIIconNames()
    {
        return _allIconNames ??= [.. typeof(BitIconName)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(f => f.IsLiteral && !f.IsInitOnly && f.FieldType == typeof(string))
            .Select(f => (string)f.GetValue(null)!)
            .OrderBy(n => n)];
    }

    private static async Task<XDocument?> LoadSummariesXmlDocumentAsync()
    {
        XDocument? mergedDoc = null;
        foreach (var asm in _componentsAssemblies)
        {
            string path = Path.Combine(AppContext.BaseDirectory, $"{asm.GetName().Name}.xml");
            if (!System.IO.File.Exists(path)) continue;
            using var stream = System.IO.File.OpenRead(path);
            var doc = await XDocument.LoadAsync(stream, LoadOptions.None, default);
            if (mergedDoc is null) mergedDoc = doc;
            else
            {
                var membersElement = doc.Root?.Element("members");
                if (membersElement != null)
                {
                    foreach (var member in membersElement.Elements("member"))
                    {
                        mergedDoc.Root!.Element("members")?.Add(member);
                    }
                }
            }
        }
        return mergedDoc;
    }
}
