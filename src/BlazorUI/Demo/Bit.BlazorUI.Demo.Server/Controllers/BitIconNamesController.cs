using System.Reflection;
using System.ComponentModel;
using ModelContextProtocol.Server;

namespace Bit.BlazorUI.Demo.Server.Controllers;

[ApiController]
[McpServerToolType]
[Route("api/[controller]/[action]")]
public partial class BitIconNamesController : AppControllerBase
{
    private static string[]? _allIconNames = null;

    [HttpGet]
    [McpServerTool(Name = nameof(GetAllBitIconNames))]
    [Description("Gets all available BitIconName constant values.")]
    public string[] GetAllBitIconNames()
    {
        return _allIconNames ??= [.. typeof(BitIconName)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(f => f.IsLiteral && !f.IsInitOnly && f.FieldType == typeof(string))
            .Select(f => (string)f.GetValue(null)!)
            .OrderBy(n => n)];
    }
}
