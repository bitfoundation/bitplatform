using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Components.CompilerServices;

namespace Bit.BlazorUI;

/// <summary>
/// A component to render content based on CSS media queries, using the browser's matchMedia API.
/// It offers the predefined bit BlazorUI screen queries, built from the live theme breakpoints so
/// customized themes are honored, and also accepts any custom media query, including non-viewport
/// features such as orientation or prefers-color-scheme.
/// </summary>
/// <remarks>
/// This is the layout decision CSS cannot express: rendering a different component, or none at all,
/// rather than restyling one. The two states are written as <see cref="Matched"/> and
/// <see cref="NotMatched"/>, or as the single <see cref="Template"/> that receives the state and
/// keeps its content across the flip. The state itself is readable from <see cref="IsMatched"/>,
/// bindable with <c>@bind-IsMatched</c>, and reported through <see cref="OnChange"/>, so a page can
/// take the answer without rendering anything through the component at all.
/// <br />
/// A flip that removes the focused element (zooming in crosses breakpoints for a keyboard user too)
/// hands the focus to the content that replaced it - the element with the same id, else the first
/// focusable one - instead of leaving it to fall back to the top of the page, and scrolls it into
/// view when the new layout has put it out of sight. That takes the root
/// element, so it is not done with <see cref="NoWrapper"/>; a <see cref="Template"/> keeps the focused
/// element itself.
/// </remarks>
public partial class BitMediaQuery : BitComponentBase
{
    private string? _query;
    private string? _elementId;
    private bool _isSetup;
    private bool _isSeeded;
    private bool _queryFromCascade;
    private bool _noWrapperFromCascade;
    private bool _elementFromCascade;
    private DotNetObjectReference<BitMediaQuery>? _dotnetObj;



    [Inject] private IJSRuntime _js { get; set; } = default!;



    /// <summary>
    /// Gets or sets the theme cascaded from an enclosing <see cref="BitThemeProvider"/>.
    /// </summary>
    /// <remarks>
    /// Only the breakpoints of the theme are read, and only to resolve a <see cref="ScreenQuery"/>.
    /// They take precedence over the <c>--bit-bp-*</c> CSS variables of the rendered element, which
    /// is what keeps a scoped theme reachable in the two cases where there is no element of this
    /// component's own to read them from: <see cref="NoWrapper"/>, and a usage with no content at all.
    /// </remarks>
    [CascadingParameter] public BitTheme? CascadingTheme { get; set; }

    /// <summary>
    /// Gets or sets the cascading parameters for the media query component.
    /// </summary>
    /// <remarks>
    /// This property receives its value from an ancestor component via Blazor's cascading parameter mechanism.
    /// <br />
    /// The intended use is to allow shared configuration or settings to be applied to multiple media query components through the <see cref="BitParams"/> component.
    /// <see cref="Query"/> and <see cref="ScreenQuery"/> are one decision there: neither is cascaded to a media query
    /// that sets either of them itself.
    /// </remarks>
    [CascadingParameter(Name = BitMediaQueryParams.ParamName)]
    public BitMediaQueryParams? CascadingParameters { get; set; }



    /// <summary>
    /// The content of the element to render if the specified query is matched.
    /// </summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// The initial matched state to render with until the actual result of the query arrives from
    /// the browser. Useful to avoid a flash of the wrong content during prerendering (or before the
    /// JavaScript runtime becomes available), where the query cannot be evaluated yet.
    /// </summary>
    /// <remarks>
    /// Ignored when <see cref="IsMatched"/> is bound, since the value handed over is then the initial
    /// state already.
    /// </remarks>
    [Parameter] public bool DefaultMatched { get; set; }

    /// <summary>
    /// The custom html element used for the root node. The default is "div".
    /// </summary>
    /// <remarks>
    /// A div is block content, which HTML does not allow where only phrasing content may go - inside a
    /// button, a link, a label or a paragraph - and which breaks the line of text it lands in. A "span"
    /// is the wrapper for those places, and an "li" or a "td" the one for a list or a table row, while
    /// the component keeps everything its element does for it: the focus kept across a flip, the
    /// themed scope the breakpoints are read from, the class, the style and the accessible name - all of
    /// which <see cref="NoWrapper"/> gives up.
    /// <br />
    /// The name is used as written, but only while it is a name a tag can have and one that may hold
    /// content; anything else (a name carrying whitespace or a "&lt;", a void element such as "br")
    /// falls back to the default tag.
    /// </remarks>
    [Parameter] public string? Element { get; set; }

    /// <summary>
    /// Gets or sets the current matched state of the provided query.
    /// </summary>
    /// <remarks>
    /// This is an output of the component rather than an input: the browser owns the state, and the
    /// component writes the latest result it reports here. Bind it with <c>@bind-IsMatched</c> to
    /// keep a field of the page in step with the query without handling <see cref="OnChange"/>.
    /// <br />
    /// Set one way (without a <c>Changed</c> callback beside it) the value belongs to the page, which
    /// freezes the state at whatever the page hands over; to seed the state before the browser has
    /// answered, leave this alone and use <see cref="DefaultMatched"/> instead.
    /// </remarks>
    [Parameter, TwoWayBound] public bool IsMatched { get; set; }

    /// <summary>
    /// The content to be rendered if the provided query is matched (an alias for ChildContent).
    /// </summary>
    [Parameter] public RenderFragment? Matched { get; set; }

    /// <summary>
    /// The content to be rendered if the provided query is not matched.
    /// </summary>
    [Parameter] public RenderFragment? NotMatched { get; set; }

    /// <summary>
    /// Renders the active content directly, without the wrapping root element.
    /// </summary>
    /// <remarks>
    /// Since no element is rendered, everything that describes one - the class, the style, the id,
    /// the direction and the splatted attributes - has nowhere to land and is ignored, and
    /// <see cref="BitComponentBase.RootElement"/> is never captured. The one exception is a
    /// <see cref="BitVisibility.Collapsed"/> <see cref="BitComponentBase.Visibility"/>, which asks
    /// for the component to be out of the DOM and needs no element of its own to say so: nothing is
    /// rendered at all, not even the content.
    /// <br />
    /// Where only the div is in the way - inside a button, a paragraph or a list - an
    /// <see cref="Element"/> of the right kind keeps all of that instead.
    /// <br />
    /// A <see cref="ScreenQuery"/> is unaffected: with no element to read the <c>--bit-bp-*</c>
    /// variables from, the breakpoints of an enclosing <see cref="BitThemeProvider"/> are taken from
    /// the cascading theme (see <see cref="CascadingTheme"/>) and the document body answers for the
    /// rest, so a scoped theme is honored here as it is anywhere else.
    /// </remarks>
    [Parameter] public bool NoWrapper { get; set; }

    /// <summary>
    /// The event callback to be called when the state of the media query has been changed.
    /// It is also called once with the initial matched state, right after the query gets evaluated
    /// by the browser for the first time.
    /// </summary>
    [Parameter] public EventCallback<bool> OnChange { get; set; }

    /// <summary>
    /// Specifies the custom query to be matched. Any valid CSS media query is accepted, including
    /// non-viewport features such as orientation, pointer, or prefers-color-scheme.
    /// Takes precedence over <see cref="ScreenQuery"/> when both are provided.
    /// </summary>
    /// <remarks>
    /// A leading <c>@media</c> keyword is dropped, so a query copied out of a stylesheet works as is.
    /// </remarks>
    [Parameter] public string? Query { get; set; }

    /// <summary>
    /// Defines the screen query to be matched, amongst the predefined Bit screen media queries.
    /// The actual query is built at runtime from the live theme breakpoints (the
    /// <c>--bit-bp-*</c> CSS variables), so customized theme breakpoints are honored.
    /// </summary>
    [Parameter] public BitScreenQuery? ScreenQuery { get; set; }

    /// <summary>
    /// The content to be rendered for both states of the query, receiving the current matched state.
    /// </summary>
    /// <remarks>
    /// This is the one fragment that spans the flip, for the common case where the two states are
    /// the same markup told apart by a value: a size, a variant, a class, an attribute. Since it is
    /// one fragment in one position of the render tree, the content is updated rather than replaced
    /// when the query flips - which is what keeps the state of the components inside it (a form that
    /// is half filled in, a scroll position, a running animation) across a change of the viewport.
    /// <br />
    /// It takes precedence over <see cref="Matched"/>, <see cref="ChildContent"/> and
    /// <see cref="NotMatched"/>, which are not rendered while it is set.
    /// </remarks>
    [Parameter] public RenderFragment<bool>? Template { get; set; }



    [JSInvokable("OnMatchChange")]
    public async ValueTask _OnMatchChange(bool isMatched)
    {
        if (IsDisposed) return;

        try
        {
            // The state can already be the reported one - the first notification carries the initial
            // result of the query, which a DefaultMatched may well have guessed right. Nothing on
            // screen changes then, so the render is skipped; the callback below still runs, since a
            // handler waiting for the first real answer of the browser has not had one yet.
            if (IsMatched != isMatched)
            {
                await InvokeAsync(async () =>
                {
                    await AssignIsMatched(isMatched);

                    StateHasChanged();
                });
            }

            await OnChange.InvokeAsync(isMatched);
        }
        catch (Exception ex) when (ex is not ObjectDisposedException)
        {
            // This method is called from JavaScript, so an exception leaving it rejects the call
            // there - and the only thing the JS side can read into a rejected call is that the .NET
            // object is gone, which stops the listener for good. A handler of this page throwing
            // once would otherwise silently take the media query down with it, so the exception is
            // handed to Blazor's own error handling (an error boundary, the circuit, the logger)
            // instead of being reported back over the interop call. A disposal racing the
            // notification is the one case the JS side is right to read that way, and is rethrown.
            await DispatchExceptionAsync(ex);
        }
    }



    protected override string RootElementClass => "bit-mdq";

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        if (_HasContent is false || NoWrapper)
        {
            // No element is rendered any more, so the reference a previous render captured is stale.
            RootElement = default;

            // A collapsed component is asked to be out of the DOM, which still means something without an element of
            // its own: the content it would have wrapped goes with it.
            if (_HasContent && Visibility is not BitVisibility.Collapsed)
            {
                BuildContent(builder, 0);
            }

            return;
        }

        var element = _Element;

        builder.OpenElement(10, element);
        // The splatted attributes come first so everything the component builds itself is written over them. The values
        // it would otherwise write as null are resolved against them, since a null written over a splatted attribute
        // does not leave that attribute alone - it removes it.
        builder.AddMultipleAttributes(11, RuntimeHelpers.TypeCheck(HtmlAttributes));
        builder.AddAttribute(12, "id", _RootId);
        builder.AddAttribute(13, "role", _GetRole(element));
        builder.AddAttribute(14, "aria-label", _AriaLabel);
        builder.AddAttribute(15, "style", JoinStyles(GetSplattedAttribute("style"), StyleBuilder.Value));
        builder.AddAttribute(16, "class", JoinClasses(ClassBuilder.Value, GetSplattedAttribute("class")));
        builder.AddAttribute(17, "dir", Dir?.ToString().ToLowerInvariant() ?? GetSplattedAttribute("dir"));
        builder.AddElementReferenceCapture(18, v => RootElement = v);
        BuildContent(builder, 19);
        builder.CloseElement();
    }

    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(BitMediaQueryParams))]
    protected override void OnParametersSet()
    {
        // A value the cascade handed down is not one the component keeps once the cascade stops carrying it: it goes
        // back to its default. A value assigned on the component is left alone. The two are cleared one by one: a
        // ScreenQuery the component now sets itself must not be left behind a cascaded Query that would outrank it.
        if (_queryFromCascade)
        {
            if (HasNotBeenSet(nameof(Query))) Query = null;
            if (HasNotBeenSet(nameof(ScreenQuery))) ScreenQuery = null;
        }

        if (_noWrapperFromCascade && HasNotBeenSet(nameof(NoWrapper)))
        {
            NoWrapper = false;
        }

        if (_elementFromCascade && HasNotBeenSet(nameof(Element)))
        {
            Element = null;
        }

        _queryFromCascade = CascadingParameters?.AppliesQuery(this) is true;
        _noWrapperFromCascade = CascadingParameters?.NoWrapper.HasValue is true && HasNotBeenSet(nameof(NoWrapper));
        _elementFromCascade = CascadingParameters?.Element.HasValue() is true && HasNotBeenSet(nameof(Element));

        CascadingParameters?.UpdateParameters(this);

        base.OnParametersSet();
    }

    protected override async Task OnParametersSetAsync()
    {
        // Render with the DefaultMatched state until the browser reports the actual result of the
        // query (e.g. during prerendering); the first JS notification then takes over. A bound
        // IsMatched hands its own initial value over and owns this instead.
        // Seeded here rather than on initialization, so a DefaultMatched a BitParams cascades is
        // already in place: the cascade is applied with the rest of the parameters, after it.
        if (_isSeeded is false)
        {
            _isSeeded = true;

            if (IsMatchedHasBeenSet is false && DefaultMatched)
            {
                await AssignIsMatched(true);
            }
        }

        await base.OnParametersSetAsync();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);

        if (IsDisposed) return;

        // Created after the disposal check above, so a component torn down before its first render
        // callback ran does not leave a reference behind that nothing disposes any more.
        _dotnetObj ??= DotNetObjectReference.Create(this);

        // A custom Query takes precedence; otherwise defer to the predefined ScreenQuery, whose
        // media query is built on the JS side from the live theme breakpoints so a customized
        // BitTheme.Layout.Breakpoints is honored (rather than baking fixed px here).
        // A blank Query is treated as absent so a bound-but-empty value still lets ScreenQuery win.
        var customQuery = NormalizeQuery(Query);
        var screenQuery = customQuery is null ? ScreenQuery?.ToString() : null;
        var effectiveKey = customQuery ?? screenQuery;
        var elementId = _ElementId;

        if (effectiveKey.HasValue())
        {
            // For a predefined ScreenQuery the actual media-query expression is resolved on the JS
            // side from the live theme breakpoints, so it can change while the enum name stays the
            // same (e.g. after new breakpoints are applied, or when the element the tokens are read
            // from moves into another themed scope). Re-invoke setup on every render in that case
            // and let the JS side reuse the existing listener when the resolved expression is
            // unchanged; a custom Query is verbatim, so the key comparison suffices - plus the
            // element, which is where the focus is kept across a flip: a new Id, a NoWrapper toggle
            // or content that appears later changes it, and the JS side takes the new one in without
            // rebuilding the listener. The listener itself is keyed by the component's unique id.
            if (effectiveKey != _query || elementId != _elementId || _isSetup is false || screenQuery is not null)
            {
                _query = effectiveKey;
                _elementId = elementId;
                _isSetup = true;

                try
                {
                    await _js.BitMediaQuerySetup(UniqueId, elementId, customQuery, screenQuery, _ThemeBreakpoints, _dotnetObj);
                }
                catch (JSDisconnectedException)
                {
                    // Circuit gone; nothing to set up. Recorded as not set up so a later render -
                    // there is none on a gone circuit, but the state stays honest either way -
                    // tries again rather than assuming a listener that was never created.
                    _isSetup = false;
                }
            }
        }
        else if (_isSetup)
        {
            // Neither a Query nor a ScreenQuery resolves anymore: tear down the previous listener
            // and reset so a later (re)assignment sets up cleanly.
            _query = null;
            _elementId = null;
            _isSetup = false;
            try
            {
                await _js.BitMediaQueryDispose(UniqueId);
            }
            catch (JSDisconnectedException) { } // circuit gone; nothing to tear down
        }
    }



    // A query is written the way a stylesheet writes it more often than not - the "@media" at-rule
    // keyword included, as the BitScreenQuery docs show theirs - and matchMedia rejects that keyword
    // (the query then silently never matches), so it is dropped here rather than left to trip over.
    private static string? NormalizeQuery(string? query)
    {
        if (query.HasValue() is false) return null;

        var normalized = query!.Trim();

        const string atRule = "@media";
        if (normalized.StartsWith(atRule, StringComparison.OrdinalIgnoreCase)
            && (normalized.Length == atRule.Length || char.IsWhiteSpace(normalized[atRule.Length]) || normalized[atRule.Length] == '('))
        {
            normalized = normalized[atRule.Length..].TrimStart();
        }

        return normalized.HasValue() ? normalized : null;
    }

    // The id of the element the theme breakpoints are read from and the focus is kept in, or null
    // when this component renders no element of its own - in no-wrapper mode, and when there is
    // nothing at all to render. The id is not the listener key: any other element that happens to
    // carry the same id (the rendered content itself, in no-wrapper mode) is not this component's
    // element and is deliberately not read.
    private string? _ElementId => NoWrapper is false && _HasContent ? _RootId : null;

    // The id the root element is rendered with: an id splatted in an attribute dictionary is kept
    // rather than written over, and the JS side looks the element up by whichever it is.
    private string _RootId => Id.HasValue() ? Id! : (GetSplattedAttribute("id") ?? _Id);

    // The tag the root element is rendered as. A name no tag can have would write markup of its own,
    // and a void element holds no content, so both fall back to the default.
    private string _Element => ResolveContentElement(Element, "div");

    // A splatted aria-label is resolved here rather than written over: the null a markup attribute
    // written after the splat carries would otherwise remove it, since it binds no parameter.
    private string? _AriaLabel => AriaLabel ?? GetSplattedAttribute("aria-label");

    // ARIA prohibits naming an element with no role, so a named div or span - the two generic
    // wrappers - is a group, the generic container a name can be given to, unless the page gives it a
    // role of its own. Any other tag keeps its native role (a list item, a navigation landmark), which
    // a group would only overwrite.
    private string? _GetRole(string element)
    {
        var role = GetSplattedAttribute("role");
        if (role is not null) return role;

        if (element.Equals("div", StringComparison.OrdinalIgnoreCase) is false
            && element.Equals("span", StringComparison.OrdinalIgnoreCase) is false) return null;

        return _AriaLabel.HasValue() || GetSplattedAttribute("aria-labelledby").HasValue() ? "group" : null;
    }

    // One fragment for both states when there is a Template, so its content keeps its place in the
    // render tree across a flip of the query and is updated rather than built again; the two sides of
    // Matched and NotMatched sit at sequences of their own, so a flip replaces one with the other.
    private void BuildContent(RenderTreeBuilder builder, int sequence)
    {
        if (Template is not null)
        {
            builder.AddContent(sequence, Template(IsMatched));
        }
        else if (IsMatched)
        {
            builder.AddContent(sequence + 1, Matched ?? ChildContent);
        }
        else
        {
            builder.AddContent(sequence + 2, NotMatched);
        }
    }

    private bool _HasContent => Template is not null || Matched is not null || ChildContent is not null || NotMatched is not null;

    // The breakpoints an enclosing BitThemeProvider overrides, as the JS side wants them. Only the
    // ones actually set are sent: everything else is left to the CSS variables and the built-in
    // defaults, so a provider that re-values a single breakpoint does not flatten the rest of the
    // scale to whatever this theme object happens to hold.
    private Dictionary<string, string>? _ThemeBreakpoints
    {
        get
        {
            var breakpoints = CascadingTheme?.Layout?.Breakpoints;
            if (breakpoints is null) return null;

            Dictionary<string, string>? result = null;

            if (breakpoints.Xs.HasValue()) (result ??= [])["xs"] = breakpoints.Xs!;
            if (breakpoints.Sm.HasValue()) (result ??= [])["sm"] = breakpoints.Sm!;
            if (breakpoints.Md.HasValue()) (result ??= [])["md"] = breakpoints.Md!;
            if (breakpoints.Lg.HasValue()) (result ??= [])["lg"] = breakpoints.Lg!;
            if (breakpoints.Xl.HasValue()) (result ??= [])["xl"] = breakpoints.Xl!;
            if (breakpoints.Xxl.HasValue()) (result ??= [])["xxl"] = breakpoints.Xxl!;

            return result;
        }
    }



    protected override async ValueTask DisposeAsync(bool disposing)
    {
        if (IsDisposed || disposing is false) return;

        await base.DisposeAsync(disposing);

        try
        {
            if (_isSetup)
            {
                // Tear the JS listener down before disposing the .NET reference, so a media change
                // firing in between cannot invoke an already disposed object.
                await _js.BitMediaQueryDispose(UniqueId);
            }
        }
        catch (JSDisconnectedException) { } // we can ignore this exception here
        finally
        {
            // In a finally so the reference is released even where the teardown above failed for a
            // reason of its own: it is a .NET object, and nothing on the JS side can free it.
            _dotnetObj?.Dispose();
        }
    }
}
