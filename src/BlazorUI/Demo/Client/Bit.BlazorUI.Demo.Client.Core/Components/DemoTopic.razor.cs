namespace Bit.BlazorUI.Demo.Client.Core.Components;

/// <summary>
/// A topic of a component demo page that is too long to read, render or navigate as one page: its examples
/// get a route of their own (<c>/components/&lt;component&gt;/&lt;Route&gt;</c>), a place in the topic strip
/// under the page's title, a card on the overview and a step in the topic pager. See
/// <see cref="DemoPage.HasTopics"/>.
/// </summary>
public partial class DemoTopic
{
    /// <summary>The page this topic belongs to, which lists it and decides whether it is the one on screen.</summary>
    [CascadingParameter] public DemoPage? Page { get; set; }

    [CascadingParameter(Name = nameof(RenderForMcpClient))] public bool RenderForMcpClient { get; set; }

    /// <summary>The examples of the topic.</summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>One line saying what the topic covers: the lead under its heading, its card and its meta description.</summary>
    [Parameter] public string? Description { get; set; }

    /// <summary>
    /// The last segment of the topic's address, in lower case (<c>commands</c> is
    /// <c>/components/commandpalette/commands</c>). Left unset for the overview: the topic the component's
    /// own address shows, which holds the notes and the first examples. "api" is taken by the API reference.
    /// </summary>
    [Parameter] public string? Route { get; set; }

    /// <summary>What the topic is called in the strip, on its card, in its heading and in its page title.</summary>
    [Parameter, EditorRequired] public string Title { get; set; } = default!;

    /// <summary>Whether this is the overview, the topic the component's own address shows.</summary>
    public bool IsOverview => Route.HasValue() is false;

    protected override Task OnInitAsync()
    {
        Page?.RegisterTopic(this);

        return base.OnInitAsync();
    }

    protected override async ValueTask DisposeAsync(bool disposing)
    {
        if (disposing)
        {
            Page?.UnregisterTopic(this);
        }

        await base.DisposeAsync(disposing);
    }
}
