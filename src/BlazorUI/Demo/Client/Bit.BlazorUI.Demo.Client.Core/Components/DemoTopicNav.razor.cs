namespace Bit.BlazorUI.Demo.Client.Core.Components;

/// <summary>Which of the ways between the topics of a component page a <see cref="DemoTopicNav"/> draws.</summary>
public enum DemoTopicNavKind
{
    /// <summary>The row of links under the page's title, the one on screen marked.</summary>
    Strip,

    /// <summary>A card per topic, with its description, closing the overview.</summary>
    Cards,

    /// <summary>The previous and the next topic, at the foot of each one.</summary>
    Pager
}

public partial class DemoTopicNav
{
    private sealed record TopicLink(string Title, string? Description, string Url, bool Active);

    [CascadingParameter] public DemoPage? Page { get; set; }

    /// <summary>
    /// The route of the topic on screen. Only passed so that moving to another topic of the same page - the same
    /// page component, handed another route - draws this again: nothing else it is given changes.
    /// </summary>
    [Parameter] public string? ActiveRoute { get; set; }

    [Parameter] public DemoTopicNavKind Kind { get; set; }

    protected override Task OnInitAsync()
    {
        if (Page is not null)
        {
            Page.TopicsChanged += OnTopicsChanged;
        }

        return base.OnInitAsync();
    }

    private void OnTopicsChanged() => StateHasChanged();

    /// <summary>Every topic in page order, then the API reference, which every split page has.</summary>
    private IEnumerable<TopicLink> Links()
    {
        foreach (var topic in Page!.Topics)
        {
            yield return new(topic.Title, topic.Description, Page.TopicUrl(topic.Route), Page.IsTopicActive(topic));
        }

        yield return new(DemoPage.API_TOPIC_TITLE, $"Every parameter, public member, sub-class, enum and CSS variable of Bit{Page.Name}.",
                         Page.TopicUrl(DemoPage.API_TOPIC_ROUTE), Page.IsApiTopic);
    }

    private BitNavItem[] PagerItems() => [.. Links().Select(l => new BitNavItem { Text = l.Title, Url = l.Url })];

    protected override async ValueTask DisposeAsync(bool disposing)
    {
        if (disposing && Page is not null)
        {
            Page.TopicsChanged -= OnTopicsChanged;
        }

        await base.DisposeAsync(disposing);
    }
}
