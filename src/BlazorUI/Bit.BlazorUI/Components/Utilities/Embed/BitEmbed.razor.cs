namespace Bit.BlazorUI;

/// <summary>
/// BitEmbed frames embedded content - a video, a map, a document of another site - in an iframe kept at an aspect ratio
/// that scales with its container.
/// </summary>
/// <remarks>
/// An iframe has a fixed size of its own (300 by 150 pixels unless it is told otherwise), so a video embedded at its
/// width and height does not shrink on a phone. The frame of BitEmbed takes the width of its container instead, up to
/// <see cref="MaxWidth"/>, and the height its <see cref="AspectRatio"/> gives that width, and the iframe fills it.
/// <br />
/// The iframe is lazy by default (<see cref="Loading"/>), and with a <see cref="PlaceholderTemplate"/> it is not rendered
/// at all until the user activates the placeholder - the click-to-load facade that keeps a page with several videos from
/// loading every player up front, and that sends nothing to the site serving the content before the user asks for it.
/// <br />
/// The address is checked before it is written into the iframe: only an http or https address, or a relative one, is
/// rendered, so a <c>javascript:</c> or a <c>data:</c> one never reaches the page.
/// <br />
/// <see cref="BitComponentBase.Disabled"/> keeps the placeholder from being activated; an embedded page already loaded
/// stays live, as it is another site's content rather than a control of this one.
/// </remarks>
public partial class BitEmbed : BitComponentBase
{
    /// <summary>
    /// The address actually rendered: <see cref="Src"/> once it has passed the scheme check, null otherwise.
    /// </summary>
    private string? _src;

    /// <summary>
    /// Whether the user has activated the placeholder, after which the iframe is rendered in its place.
    /// </summary>
    private bool _activated;

    /// <summary>
    /// Set by the activation of the placeholder, so the render that brings the iframe in also hands it the focus the
    /// placeholder had - which would otherwise fall back to the start of the page with the button that held it.
    /// </summary>
    private bool _focusFrame;

    private ElementReference _frameElement;



    /// <summary>
    /// Gets or sets the cascading parameters for the embed component.
    /// </summary>
    /// <remarks>
    /// This property receives its value from an ancestor component via Blazor's cascading parameter mechanism.
    /// <br />
    /// The intended use is to allow shared configuration or settings to be applied to multiple embed components
    /// through the <see cref="BitParams"/> component.
    /// </remarks>
    [CascadingParameter(Name = BitEmbedParams.ParamName)]
    public BitEmbedParams? CascadingParameters { get; set; }



    /// <summary>
    /// The features the embedded content may use, as the value of the allow attribute of the iframe - its permissions
    /// policy (e.g. "autoplay; encrypted-media; picture-in-picture").
    /// </summary>
    [Parameter] public string? Allow { get; set; }

    /// <summary>
    /// Lets the embedded content go full screen (the allowfullscreen attribute of the iframe), which a video player
    /// needs for its full screen button to work.
    /// <br />
    /// The default value is <strong>false</strong>.
    /// </summary>
    [Parameter] public bool AllowFullScreen { get; set; }

    /// <summary>
    /// The aspect ratio of the frame, as a CSS aspect-ratio value (e.g. "16 / 9", "4 / 3" or "1").
    /// Default: <strong>16 / 9</strong>, the shape of a video.
    /// </summary>
    /// <remarks>
    /// The frame takes the width of its container and the height this ratio gives that width, so the embedded content
    /// scales with the page, and the room it takes is reserved before it has loaded. It wins over the
    /// --bit-Embed-aspect-ratio variable, which restyles the default.
    /// </remarks>
    [Parameter, ResetStyleBuilder]
    public string? AspectRatio { get; set; }

    /// <summary>
    /// Custom CSS classes for the different parts of the embed.
    /// </summary>
    [Parameter, ResetClassBuilder] public BitEmbedClassStyles? Classes { get; set; }

    /// <summary>
    /// Allows for browser-level loading of the iframe (lazy or eager).
    /// The default value is <strong>BitImageLoading.Lazy</strong>.
    /// </summary>
    /// <remarks>
    /// A lazy iframe is only loaded once the browser expects it to be scrolled into view, which is what an embed further
    /// down a page wants: a video player is heavy, and one the reader never scrolls to costs nothing.
    /// </remarks>
    [Parameter] public BitImageLoading Loading { get; set; } = BitImageLoading.Lazy;

    /// <summary>
    /// The largest width of the frame, as a CSS length (e.g. "800px" or "50rem"); below it the frame takes the width of
    /// its container. By default it has no limit.
    /// </summary>
    /// <remarks>
    /// It wins over the --bit-Embed-max-width variable, which restyles the default.
    /// </remarks>
    [Parameter, ResetStyleBuilder]
    public string? MaxWidth { get; set; }

    /// <summary>
    /// The accessible name of the placeholder button. Default: <see cref="BitStrings.LoadFormat"/> of
    /// <see cref="BitStrings.Current"/> filled in with the <see cref="Title"/> ("Load {Title}").
    /// </summary>
    [Parameter] public string? PlaceholderAriaLabel { get; set; }

    /// <summary>
    /// The content shown in the frame in place of the iframe until the user activates it - a thumbnail of the video and
    /// a play icon, most of the time. With it the iframe is not rendered, and so nothing of it is requested, until the
    /// placeholder is clicked or activated with the keyboard.
    /// </summary>
    /// <remarks>
    /// It is rendered inside a button that fills the frame, so it should hold no interactive content of its own. The
    /// button is named by <see cref="PlaceholderAriaLabel"/>, and once activated, the iframe takes its place and its focus.
    /// </remarks>
    [Parameter] public RenderFragment? PlaceholderTemplate { get; set; }

    /// <summary>
    /// Specifies how much of the address of the current page is sent to the site serving the embedded content (the
    /// referrerpolicy attribute of the iframe). By default the browser's own policy applies.
    /// </summary>
    [Parameter] public BitImageReferrerPolicy? ReferrerPolicy { get; set; }

    /// <summary>
    /// Applies extra restrictions to the embedded content, as the value of the sandbox attribute of the iframe: an empty
    /// string applies them all, and each token lifts one of them (e.g. "allow-scripts allow-same-origin").
    /// By default the iframe is not sandboxed.
    /// </summary>
    [Parameter] public string? Sandbox { get; set; }

    /// <summary>
    /// The address of the content to embed (e.g. "https://www.youtube-nocookie.com/embed/VIDEO_ID").
    /// </summary>
    /// <remarks>
    /// Only an http or https address, or a relative one, is rendered; any other scheme (<c>javascript:</c>,
    /// <c>data:</c>, <c>blob:</c>, ...) is refused and the frame is left empty, since the address of an iframe is run as
    /// a page.
    /// </remarks>
    [Parameter, CallOnSet(nameof(OnSetSrc))]
    public string? Src { get; set; }

    /// <summary>
    /// Custom CSS styles for the different parts of the embed.
    /// </summary>
    [Parameter, ResetStyleBuilder] public BitEmbedClassStyles? Styles { get; set; }

    /// <summary>
    /// The title of the iframe, which is how a screen reader names the embedded content (e.g. "Getting started video").
    /// </summary>
    /// <remarks>
    /// An iframe needs one to be accessible (WCAG 4.1.2), so the compiler warns about a BitEmbed written without it.
    /// It also names the placeholder button through <see cref="PlaceholderAriaLabel"/>'s default.
    /// </remarks>
    [Parameter, EditorRequired]
    public string? Title { get; set; }



    private bool _ShowsPlaceholder => PlaceholderTemplate is not null && _activated is false;

    private string _PlaceholderAriaLabel => PlaceholderAriaLabel.HasValue()
        ? PlaceholderAriaLabel!
        : string.Format(BitStrings.Current.LoadFormat, Title).Trim();



    protected override string RootElementClass => "bit-emb";

    protected override void RegisterCssClasses()
    {
        ClassBuilder.Register(() => Classes?.Root);
    }

    protected override void RegisterCssStyles()
    {
        StyleBuilder.Register(() => Styles?.Root);

        StyleBuilder.Register(() => AspectRatio.HasValue() ? $"--bit-emb-ar:{AspectRatio}" : string.Empty);

        StyleBuilder.Register(() => MaxWidth.HasValue() ? $"--bit-emb-mxw:{MaxWidth}" : string.Empty);
    }

    protected override void OnParametersSet()
    {
        CascadingParameters?.UpdateParameters(this);

        base.OnParametersSet();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);

        if (_focusFrame is false) return;

        _focusFrame = false;

        // The iframe is only there when the address passed the check; with none there is nothing to hand the focus to.
        if (_frameElement.Context is null) return;

        try
        {
            await _frameElement.FocusAsync();
        }
        catch (JSDisconnectedException) { } // we can ignore this exception here
        catch (JSException) { } // the iframe is no longer in the document, failing to focus it is not fatal
    }



    private void OnSetSrc()
    {
        _src = GetSafeSrc(Src);
    }

    private void HandleOnPlaceholderClick()
    {
        if (Disabled) return;

        _activated = true;
        _focusFrame = true;
    }

    private string? GetReferrerPolicy() => ReferrerPolicy switch
    {
        BitImageReferrerPolicy.NoReferrer => "no-referrer",
        BitImageReferrerPolicy.NoReferrerWhenDowngrade => "no-referrer-when-downgrade",
        BitImageReferrerPolicy.Origin => "origin",
        BitImageReferrerPolicy.OriginWhenCrossOrigin => "origin-when-cross-origin",
        BitImageReferrerPolicy.SameOrigin => "same-origin",
        BitImageReferrerPolicy.StrictOrigin => "strict-origin",
        BitImageReferrerPolicy.StrictOriginWhenCrossOrigin => "strict-origin-when-cross-origin",
        BitImageReferrerPolicy.UnsafeUrl => "unsafe-url",
        _ => null
    };

    /// <summary>
    /// The address an iframe may be given: an http or https one, or a relative one, which the browser resolves against
    /// the page. Every other scheme is refused, the address of an iframe being run as a page of its own -
    /// <c>javascript:</c> in the page that holds it.
    /// </summary>
    internal static string? GetSafeSrc(string? src)
    {
        if (src.HasNoValue()) return null;

        // A browser strips the spaces and control characters around an address, and drops a tab or a line break
        // anywhere inside one - so "java&#9;script:" is javascript: to it. An address holding a control character is
        // refused outright rather than cleaned up into something else.
        var trimmed = src!.Trim();

        foreach (var c in trimmed)
        {
            if (char.IsControl(c)) return null;
        }

        // The text before the first colon is a scheme only when no slash, question mark or hash comes before it;
        // otherwise the colon belongs to the path, the query or the fragment of a relative address.
        var colon = trimmed.IndexOf(':');

        if (colon < 0) return trimmed;

        var delimiter = trimmed.IndexOfAny(['/', '?', '#']);

        if (delimiter >= 0 && delimiter < colon) return trimmed;

        var scheme = trimmed[..colon];

        return scheme.Equals("http", StringComparison.OrdinalIgnoreCase) ||
               scheme.Equals("https", StringComparison.OrdinalIgnoreCase)
            ? trimmed
            : null;
    }
}
