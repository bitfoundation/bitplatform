namespace Boilerplate.Client.Core.Components.Common;

public partial class AppPageData
{
    /// <summary>
    /// The head section <see cref="AppHeadCoordinator"/> hosts an outlet for, so a page's head tags and the
    /// app-wide ones both reach the document's single HeadOutlet.
    /// </summary>
    public const string HeadSectionName = "app-page-head";

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
    /// The absolute url of the picture a link to this page shows where it is shared, such as a product's photo, which
    /// also gets the large card. Left unset, the app's icon, in the small one.
    /// </summary>
    [Parameter] public string? Image { get; set; }

    /// <summary>
    /// The page's <c>og:type</c>; <c>website</c> unless it describes something more specific, such as a product.
    /// </summary>
    [Parameter] public string OpenGraphType { get; set; } = "website";

    /// <summary>
    /// Whatever else this page wants in the document head, such as a schema. It renders into
    /// <see cref="HeadSectionName"/> rather than as a HeadContent of the page's own, which would shadow every other one.
    /// A tag every page needs goes in <see cref="Layout.AppHead"/> instead, and the sharing tags are already rendered
    /// from <see cref="Title"/>, <see cref="Description"/> and <see cref="Image"/>.
    /// </summary>
    [Parameter] public RenderFragment? Head { get; set; }

    /// <summary>
    /// Without it, <c>?utm_source=x</c> is a page of its own to a search engine: <c>AppResponseCachePolicy</c>'s
    /// <c>QueryKeys = "*"</c> gives every query variant its own cache entry and its own crawlable url.
    /// </summary>
    private string CanonicalUrl => new Uri(navigationManager.Uri).GetCanonicalUrl();

    private string MetaDescription => Description ?? localizer[nameof(AppStrings.DescriptionMetaTagValue)];

    /// <summary>
    /// Absolute, because an unfurler does not resolve a relative one, and never empty, because several draw a broken
    /// image rather than none.
    /// </summary>
    private string ShareImageUrl => Image ?? new Uri(new Uri(navigationManager.BaseUri), "images/icons/bit-icon-512.png").ToString();

    /// <inheritdoc cref="NoIndexAttribute"/>
    private bool NoIndex => PageUrls.IsNoIndex(new Uri(navigationManager.Uri));

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
