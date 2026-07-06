using System.Reflection;
using System.ComponentModel;
using ModelContextProtocol.Server;
using Microsoft.AspNetCore.Components.Web;

namespace Bit.BlazorUI.Demo.Server.Controllers;

[ApiController]
[McpServerToolType]
[Route("api/[controller]/[action]")]
public partial class ComponentDetailsController : AppControllerBase
{
    [AutoInject] private HtmlRenderer htmlRenderer = default!;
    [AutoInject] private IHttpContextAccessor httpContextAccessor = default!;

    private static readonly Assembly[] ComponentsAssemblies = [typeof(_Imports).Assembly, typeof(Extras._Imports).Assembly];

    [HttpGet]
    [McpServerTool(Name = nameof(GetBitBlazorUIComponentDocs))]
    [Description("Gets the docs/examples of a specified component.")]
    public async Task<IActionResult> GetBitBlazorUIComponentDocs(string componentName)
    {
        if (string.IsNullOrWhiteSpace(componentName))
            return BadRequest("Component name is required.");

        var demoPageType = typeof(Client.Core.Routes).Assembly
            .GetExportedTypes()
            .SingleOrDefault(t => string.Equals(t.Name, $"{componentName}Demo", StringComparison.OrdinalIgnoreCase));

        if (demoPageType is null)
            return NotFound("No demo page found for the specified component.");

        httpContextAccessor.HttpContext!.Items["RenderForMcpClient"] = true;

        var body = await htmlRenderer.Dispatcher.InvokeAsync(async () =>
        {
            var renderedComponent = await htmlRenderer.RenderComponentAsync(demoPageType);

            return renderedComponent.ToHtmlString();
        });

        return Content(body.ToLlmFriendlyHtml(), "text/markdown");
    }
}
