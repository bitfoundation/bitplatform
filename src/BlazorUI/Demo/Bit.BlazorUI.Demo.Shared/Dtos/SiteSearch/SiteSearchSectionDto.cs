namespace Bit.BlazorUI.Demo.Shared.Dtos.SiteSearch;

/// <summary>
/// One place inside a component page the site's command palette can jump to: an example section, or a topic of a page
/// split into topics. The component pages themselves are listed from the nav on the client; what is inside them is
/// only known to the server, which reads the pages' markup for the MCP server.
/// </summary>
public class SiteSearchSectionDto
{
    /// <summary>The section's title, or the topic's.</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>The component the page documents (<c>CommandPalette</c>).</summary>
    public string Component { get; set; } = string.Empty;

    /// <summary>The topic the section belongs to, for a page split into topics.</summary>
    public string? Topic { get; set; }

    /// <summary>Where it is: the page, the topic and the section's anchor (<c>/components/commandpalette/search#example31</c>).</summary>
    public string Url { get; set; } = string.Empty;
}
