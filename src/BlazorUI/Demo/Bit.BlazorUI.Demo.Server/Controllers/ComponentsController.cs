using System.Xml.Linq;
using System.Reflection;
using System.ComponentModel;
using ModelContextProtocol.Server;
using Bit.BlazorUI.Demo.Client.Core.Shared;

namespace Bit.BlazorUI.Demo.Server.Controllers;

[ApiController]
[McpServerToolType]
[Route("api/[controller]/[action]")]
public partial class ComponentsController : AppControllerBase
{
    private static XDocument? SummariesXmlDocument = null;
    private static readonly Assembly[] ComponentsAssemblies = [typeof(_Imports).Assembly, typeof(Extras._Imports).Assembly];

    [HttpGet]
    [McpServerTool(Name = nameof(GetBitBlazorUIComponentsList))]
    [Description("Gets the list of all available components with their details.")]
    public async Task<ComponentListDetailsDto[]> GetBitBlazorUIComponentsList()
    {
        SummariesXmlDocument ??= await LoadSummariesXmlDocumentAsync();

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

                    var xmlProperty = SummariesXmlDocument?.Descendants("member")
                                            .FirstOrDefault(a => a.Attribute("name")?.Value == $"T:Bit.BlazorUI.{name}");

                    if (xmlProperty is null)
                    {
                        xmlProperty = SummariesXmlDocument?.Descendants("member")
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

        return components.ToArray();
    }

    private static async Task<XDocument?> LoadSummariesXmlDocumentAsync()
    {
        XDocument? mergedDoc = null;
        foreach (var asm in ComponentsAssemblies)
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
