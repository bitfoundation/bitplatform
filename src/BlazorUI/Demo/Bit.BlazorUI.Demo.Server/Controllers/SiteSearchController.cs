using Bit.BlazorUI.Demo.Server.Services.Mcp;
using Bit.BlazorUI.Demo.Shared.Dtos.SiteSearch;

namespace Bit.BlazorUI.Demo.Server.Controllers;

/// <summary>
/// What the site's command palette (Ctrl+K) searches beyond the component pages it lists from the nav itself: every
/// topic of a page split into topics and every example section, each with the address that lands on it. Read off the
/// same markup the MCP server answers from (<see cref="BlazorUIDemoSource"/>), which only the server carries - the
/// client would otherwise have to ship every demo page's markup to every visitor to search it.
/// </summary>
[ApiController]
[Route("api/[controller]/[action]")]
public partial class SiteSearchController : AppControllerBase
{
    private static readonly Lazy<List<SiteSearchSectionDto>> _sections = new(Build, LazyThreadSafetyMode.ExecutionAndPublication);

    /// <summary>Every topic and every example section of the component pages, in nav and page order.</summary>
    [HttpGet]
    [ResponseCache(Duration = 3600, Location = ResponseCacheLocation.Any)]
    public List<SiteSearchSectionDto> GetSections() => _sections.Value;

    private static List<SiteSearchSectionDto> Build()
    {
        var sections = new List<SiteSearchSectionDto>();

        foreach (var component in BlazorUIComponentCatalog.Components)
        {
            var url = component.Url.TrimEnd('/');
            var topics = component.Topics;

            foreach (var topic in topics.Where(t => t.Route is not null))
            {
                sections.Add(new() { Title = topic.Title, Component = component.ShortName, Url = $"{url}/{topic.Route}" });
            }

            // Only the sections an address can land on. A page split into topics renders each topic at an address of
            // its own; a page of pivot tabs renders the first tab alone until another is picked, so an anchor of any
            // other tab lands nowhere - and a multi-API page's other tabs are the same sections again anyway.
            var firstTab = component.Examples.FirstOrDefault()?.Tab;

            foreach (var example in component.Examples)
            {
                if (example.Id is null) continue;

                var isTopic = example.Tab is not null && topics.Any(t => t.Title == example.Tab);

                if (example.Tab is not null && isTopic is false && example.Tab != firstTab) continue;

                sections.Add(new()
                {
                    Title = example.Title,
                    Component = component.ShortName,
                    Topic = isTopic ? example.Tab : null,
                    Url = $"{url}{(example.TopicRoute is null ? null : $"/{example.TopicRoute}")}#{example.Id}"
                });
            }
        }

        return sections;
    }
}
