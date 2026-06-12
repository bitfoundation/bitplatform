using System.Xml.Linq;
using System.Reflection;
using System.ComponentModel;
using ModelContextProtocol.Server;

namespace Bit.BlazorUI.Demo.Server.Controllers;

[ApiController]
[McpServerToolType]
[Route("api/[controller]/[action]")]
public partial class ComponentEnumsController : AppControllerBase
{
    private static XDocument? SummariesXmlDocument = null;

    private static readonly Assembly[] ComponentsAssemblies = [typeof(_Imports).Assembly, typeof(Extras._Imports).Assembly];

    private static readonly Type[] EnumTypes = [.. ComponentsAssemblies.SelectMany(asm => asm.GetExportedTypes().Where(t => t.IsEnum))];

    [HttpGet]
    [McpServerTool(Name = nameof(GetEnumDetails))]
    [Description("Gets the details of a specified Bit.BlazorUI enum including its values and descriptions.")]
    public async Task<EnumValueDetailsDto[]?> GetEnumDetails(string enumName)
    {
        if (string.IsNullOrWhiteSpace(enumName))
            return null;

        SummariesXmlDocument ??= await LoadSummariesXmlDocumentAsync();

        var enumType = EnumTypes.FirstOrDefault(t =>
            t.Name.Equals(enumName, StringComparison.OrdinalIgnoreCase));

        if (enumType is null)
            return null;

        var values = Enum.GetNames(enumType).Select(name =>
        {
            var fieldXmlMember = SummariesXmlDocument?.Descendants("member")
                                    .FirstOrDefault(m => m.Attribute("name")?.Value == $"F:{enumType.FullName}.{name}");

            return new EnumValueDetailsDto
            {
                Name = name,
                Description = fieldXmlMember?.Element("summary")?.Value?.Trim()
            };
        }).ToArray();

        return values;
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
}
