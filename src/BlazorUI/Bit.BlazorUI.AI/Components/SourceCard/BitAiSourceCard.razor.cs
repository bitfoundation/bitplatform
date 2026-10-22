using System.Diagnostics.CodeAnalysis;

namespace Bit.BlazorUI;

/// <summary>
/// BitAiSourceCard shows one source a message cited - its number, its title and the site it comes from - on a
/// <see cref="BitCard"/>.
/// </summary>
/// <remarks>
/// The card is a link to the source when the source is addressable, so the browser gives the user the usual middle
/// click and context menu, and a button when <see cref="OnActivate"/> is set. Source URLs come from the model or from
/// retrieved content, so only a web address ever becomes a link, and only a web address or an inline raster image is
/// ever loaded as the favicon.
/// </remarks>
public partial class BitAiSourceCard : BitAiComponentBase
{
    private readonly EventCallback<MouseEventArgs> _onCardClick;



    public BitAiSourceCard()
    {
        // Held in a field rather than created per render, so the card is handed the same delegate on every render.
        _onCardClick = EventCallback.Factory.Create<MouseEventArgs>(this, HandleOnCardClick);
    }



    /// <summary>
    /// Gets or sets the cascading parameters for the source card component.
    /// </summary>
    /// <remarks>
    /// This property receives its value from an ancestor component via Blazor's cascading parameter mechanism.
    /// <br />
    /// The intended use is to allow shared configuration or settings to be applied to multiple source card components
    /// through the <see cref="BitParams"/> component.
    /// </remarks>
    [CascadingParameter(Name = BitAiSourceCardParams.ParamName)]
    public BitAiSourceCardParams? CascadingParameters { get; set; }



    /// <summary>
    /// Custom CSS classes for different parts of the source card.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public BitAiSourceCardClassStyles? Classes { get; set; }

    /// <summary>
    /// The marker number shown beside the title, which matches the inline citation markers of the source.
    /// </summary>
    [Parameter] public int? Number { get; set; }

    /// <summary>
    /// The callback for when the card is activated. When set, the card becomes a button instead of a link.
    /// </summary>
    [Parameter] public EventCallback<BitAiCitationSource> OnActivate { get; set; }

    /// <summary>
    /// The source to render.
    /// </summary>
    [Parameter, EditorRequired]
    public BitAiCitationSource Source { get; set; } = default!;

    /// <summary>
    /// Custom CSS styles for different parts of the source card.
    /// </summary>
    [Parameter, ResetStyleBuilder]
    public BitAiSourceCardClassStyles? Styles { get; set; }



    protected override string RootElementClass => "bit-aisrc";

    protected override void RegisterCssClasses()
    {
        ClassBuilder.Register(() => Classes?.Root);
    }

    protected override void RegisterCssStyles()
    {
        StyleBuilder.Register(() => Styles?.Root);
    }

    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(BitAiSourceCardParams))]
    protected override void OnParametersSet()
    {
        CascadingParameters?.UpdateParameters(this);

        base.OnParametersSet();
    }



    /// <summary>
    /// Returns the URL only when it is an absolute web address.
    /// </summary>
    /// <remarks>
    /// Source URLs come from the model or from retrieved content, and Blazor writes an href as
    /// given, so a javascript: URL would run in the host page when clicked. Anything that is not
    /// http or https renders as a plain card instead of a link.
    /// </remarks>
    internal static string? SafeUrl(string? url) =>
        Uri.TryCreate(url?.Trim(), UriKind.Absolute, out var uri)
        && (uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp)
            ? url!.Trim()
            : null;

    /// <summary>
    /// Returns an icon URL only when it is a web address or an inline image, the two forms a
    /// favicon legitimately takes. An image src is less exposed than an href, but a data: URL of a
    /// non-image type or a scheme the browser hands to a plugin is still not worth loading.
    /// </summary>
    internal static string? SafeIconUrl(string? url)
    {
        if (SafeUrl(url) is { } web) return web;

        var trimmed = url?.Trim();
        return trimmed is not null && trimmed.StartsWith("data:image/", StringComparison.OrdinalIgnoreCase)
            && !trimmed.Contains("svg", StringComparison.OrdinalIgnoreCase)
            ? trimmed
            : null;
    }

    private static string? HostOf(string? url) =>
        SafeUrl(url) is { } safe && Uri.TryCreate(safe, UriKind.Absolute, out var uri) ? uri.Host : null;

    private async Task HandleOnCardClick()
    {
        // The card guards its own Disabled, but a disabled source card is one whose source is not handed out either.
        if (Disabled) return;

        await OnActivate.InvokeAsync(Source);
    }
}
