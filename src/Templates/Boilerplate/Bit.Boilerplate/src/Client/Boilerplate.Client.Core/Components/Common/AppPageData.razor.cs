namespace Boilerplate.Client.Core.Components.Common;

public partial class AppPageData
{
    private string? _lastPublishedMessage;

    [AutoInject] private PubSubService pubSubService = default!;
    [AutoInject] private NavigationManager navigationManager = default!;
    [AutoInject] private IStringLocalizer<AppStrings> localizer = default!;

    [Parameter] public string? PageTitle { get; set; }
    [Parameter] public string? Title { get; set; }
    [Parameter] public string? SubTitle { get; set; }
    [Parameter] public bool ShowGoBackButton { get; set; }

    /// <summary>
    /// This page's own meta description. Left unset, the app-wide one is used as a fallback.
    /// </summary>
    [Parameter] public string? Description { get; set; }

    /// <summary>
    /// Whatever else this page wants in the document head - a sharing card, a schema. It renders INSIDE this
    /// component's one HeadContent rather than as a HeadContent of the page's own, because a page has a single
    /// HeadOutlet and only the last HeadContent rendered into it survives.
    /// </summary>
    [Parameter] public RenderFragment? Head { get; set; }

    /// <summary>
    /// Without it, <c>?utm_source=x</c> is a page of its own to a search engine: <c>AppResponseCachePolicy</c>'s
    /// <c>QueryKeys = "*"</c> gives every query variant its own cache entry and its own crawlable url.
    /// </summary>
    private string CanonicalUrl => new Uri(navigationManager.Uri).GetCanonicalUrl();

    protected override void OnParametersSet()
    {
        base.OnParametersSet();

        Publish();
    }

    private void Publish()
    {
        var publishMessage = $"{PageTitle}-{Title}-{SubTitle}-{ShowGoBackButton}";

        if (_lastPublishedMessage == publishMessage) return;

        _lastPublishedMessage = publishMessage;

        pubSubService.Publish(ClientAppMessages.PAGE_DATA_CHANGED, (Title, SubTitle, ShowGoBackButton), persistent: true);
    }
}
