using System.Diagnostics.CodeAnalysis;

namespace Bit.BlazorUI;

/// <summary>
/// Links lead to another part of an app, other pages, or help articles. They can also be used to initiate commands.
/// </summary>
/// <remarks>
/// The component renders the element the destination asks for: an anchor when <see cref="Href"/> names one, a
/// button when it does not - so a link that runs a command is a button wearing the link's look, and answers the
/// keyboard the way a button does. An <see cref="Href"/> starting with <c>#</c> is the third case, an in-page
/// link that scrolls the named element into view and takes the focus with it.
/// <br />
/// What the browser already gets right is passed through rather than reimplemented: <see cref="Target"/>,
/// <see cref="Download"/> and <see cref="Rel"/> are the anchor attributes of the same names, and anything else
/// an anchor accepts goes through the splatted HTML attributes, which the component reads back rather than
/// overwrites. What it gets wrong for an app is defaulted: a <c>_blank</c> link is given <c>noopener</c> so the
/// page it opens cannot reach back into the one that opened it, and is announced as opening in a new tab
/// (<see cref="NewTabHint"/>).
/// </remarks>
public partial class BitLink : BitComponentBase
{
    /// <summary>
    /// The rel values named by <see cref="Rel"/>, rendered as the space separated list an HTML rel attribute
    /// holds. The automatic <c>noopener</c> of a new-tab link is not part of it: that one depends on the
    /// target actually on the element, which may have arrived as a splatted attribute rather than as the
    /// parameter, and is therefore decided at render time.
    /// </summary>
    private string? _rel;

    private string? _tabIndex;

    /// <summary>
    /// The text a <c>_blank</c> link is announced with when nothing else is said - see <see cref="NewTabHint"/>.
    /// </summary>
    private const string DefaultNewTabHint = "(opens in a new tab)";

    private static readonly Dictionary<BitNavAriaCurrent, string> _ariaCurrentMap = new()
    {
        [BitNavAriaCurrent.Page] = "page",
        [BitNavAriaCurrent.Step] = "step",
        [BitNavAriaCurrent.Location] = "location",
        [BitNavAriaCurrent.Time] = "time",
        [BitNavAriaCurrent.Date] = "date",
        [BitNavAriaCurrent.True] = "true"
    };



    [Inject] private IJSRuntime _js { get; set; } = default!;



    /// <summary>
    /// Gets or sets the cascading parameters for the link component.
    /// </summary>
    /// <remarks>
    /// This property receives its value from an ancestor component via Blazor's cascading parameter mechanism.
    /// <br />
    /// The intended use is to allow shared configuration or settings to be applied to multiple link components
    /// through the <see cref="BitParams"/> component.
    /// </remarks>
    [CascadingParameter(Name = BitLinkParams.ParamName)]
    public BitLinkParams? CascadingParameters { get; set; }



    /// <summary>
    /// Keeps the disabled link focusable and discoverable by assistive technologies.
    /// When enabled, the disabled state is conveyed using the <c>aria-disabled</c> attribute instead of removing
    /// the element from the tab order, so keyboard and screen reader users can still find the link while its
    /// navigation and click action stay suppressed.
    /// </summary>
    [Parameter] public bool AllowDisabledFocus { get; set; }

    /// <summary>
    /// Gives the link the focus as soon as it is rendered, through the <c>autofocus</c> attribute.
    /// </summary>
    /// <remarks>
    /// The browser honors this once per document, on the first element that asks for it, so only the one
    /// element a page opens on should carry it. Being taken somewhere unasked is a jump the reader did not
    /// make, so reserve it for a destination that is the point of the page.
    /// </remarks>
    [Parameter] public bool AutoFocus { get; set; }

    /// <summary>
    /// A longer description of the link for the benefit of screen readers, rendered as visually hidden text the
    /// link points at through <c>aria-describedby</c>.
    /// </summary>
    /// <remarks>
    /// A description is read out after the name and is not part of it, which is what makes it the place for what
    /// the reader would want to know before following the link but not in the words on the page: the size of a
    /// file, the format it is in, what the page it leads to is going to ask for. Unlike a <see cref="Title"/>,
    /// which the browser only ever shows to a mouse, this reaches everyone a screen reader is reading to.
    /// </remarks>
    [Parameter] public string? AriaDescription { get; set; }

    /// <summary>
    /// Reports the link as the current item of the set it belongs to, through the <c>aria-current</c> attribute.
    /// </summary>
    /// <remarks>
    /// A menu draws the link to the page already open differently from the rest, and a color is all that says
    /// so - which is nothing at all to a reader who is not looking at it. This is the same statement made in a
    /// way a screen reader announces: <see cref="BitNavAriaCurrent.Page"/> in a navigation menu,
    /// <see cref="BitNavAriaCurrent.Step"/> in a wizard, <see cref="BitNavAriaCurrent.Location"/> in a
    /// breadcrumb. Only one link of a set is ever the current one.
    /// </remarks>
    [Parameter] public BitNavAriaCurrent? AriaCurrent { get; set; }
    /// <summary>
    /// The content of the link, can be any custom tag or a text.
    /// </summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// The general color of the link.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public BitColor? Color { get; set; }

    /// <summary>
    /// The value of the download attribute of the link when the Href parameter is provided.
    /// Instructs the browser to download the linked resource instead of navigating to it, using the provided value
    /// (if any) as the suggested file name (only works for same-origin, blob: and data: URLs).
    /// </summary>
    [Parameter] public string? Download { get; set; }

    /// <summary>
    /// URL the link points to. If provided, the component renders an anchor tag, otherwise a button.
    /// A value starting with the <c>#</c> character makes the link smooth-scroll the element with that id into view
    /// and move the focus to it, while a bare <c>#</c> renders an inert link that navigates nowhere.
    /// </summary>
    [Parameter]
    [CallOnSet(nameof(OnSetHrefAndRel))]
    public string? Href { get; set; }

    /// <summary>
    /// Gets or sets the icon rendered beside the link content, using custom CSS classes for external icon libraries.
    /// Takes precedence over <see cref="IconName"/> when both are set.
    /// </summary>
    /// <remarks>
    /// The glyph is normalized back to the size of the text beside it, so an icon from any set sits on the same
    /// line as the words whatever type scale it came with.
    /// </remarks>
    /// <example>
    /// Bootstrap: Icon="BitIconInfo.Bi("gear-fill")"
    /// FontAwesome: Icon="BitIconInfo.Fa("solid house")"
    /// Custom CSS: Icon="BitIconInfo.Css("my-icon-class")"
    /// </example>
    [Parameter] public BitIconInfo? Icon { get; set; }

    /// <summary>
    /// Gets or sets the name of the icon rendered beside the link content, from the built-in Fluent UI icons.
    /// </summary>
    /// <remarks>
    /// Browse available names in <c>BitIconName</c> of the <c>Bit.BlazorUI.Icons</c> nuget package or the gallery:
    /// <see href="https://blazorui.bitplatform.dev/iconography"/>.
    /// <br />
    /// For external icon libraries, use <see cref="Icon"/> instead.
    /// </remarks>
    [Parameter] public string? IconName { get; set; }

    /// <summary>
    /// Gets or sets the position of the icon relative to the link content.
    /// </summary>
    /// <remarks>
    /// The icon goes in front of the text by default, the way it does everywhere else in the library.
    /// <see cref="BitIconPosition.End"/> puts it after the text instead, which is where the two glyphs a link
    /// carries most often - the arrow of a link opening a new tab and the tray of a download - belong.
    /// <br />
    /// The icon is drawn as decoration and hidden from assistive technologies, so whatever it says has to be
    /// said by the link text or by an <see cref="BitComponentBase.AriaLabel"/> as well.
    /// </remarks>
    [Parameter, ResetClassBuilder]
    public BitIconPosition? IconPosition { get; set; }

    /// <summary>
    /// Replaces the text a new-tab link is announced with, for translating it or for saying it another way.
    /// </summary>
    /// <remarks>
    /// A link opening a new tab takes the reader somewhere the back button no longer returns from, which is a
    /// change of context nothing on the page predicts on its own. So a <c>_blank</c> link carries the sentence
    /// saying so - "<c>(opens in a new tab)</c>" unless this replaces it - as visually hidden text after its
    /// content, or appended to its <see cref="BitComponentBase.AriaLabel"/> when it has one, since an aria-label
    /// replaces the content rather than adding to it.
    /// <br />
    /// An empty value takes the announcement off, the same as <see cref="NoNewTabHint"/> does.
    /// </remarks>
    [Parameter] public string? NewTabHint { get; set; }

    /// <summary>
    /// Removes applying any foreground color to the link content, letting it keep its own color.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public bool NoColor { get; set; }

    /// <summary>
    /// Stops a new-tab link from announcing that it opens in a new tab.
    /// </summary>
    /// <remarks>
    /// Only set this where the announcement would be made twice - beside a visible "opens in a new tab" label of
    /// your own, or inside a list whose heading already says that every link in it opens a new tab. See
    /// <see cref="NewTabHint"/> for what is being taken off.
    /// </remarks>
    [Parameter] public bool NoNewTabHint { get; set; }

    /// <summary>
    /// Styles the link to have no underline at any state.
    /// </summary>
    /// <remarks>
    /// This wins over <see cref="Underlined"/> when both are set.
    /// </remarks>
    [Parameter, ResetClassBuilder]
    public bool NoUnderline { get; set; }

    /// <summary>
    /// Callback for when the link is clicked.
    /// It is invoked in every render mode of the link: on anchor links it runs alongside the navigation,
    /// and on button links (no Href) it is the sole click action.
    /// </summary>
    [Parameter] public EventCallback<MouseEventArgs> OnClick { get; set; }

    /// <summary>
    /// Suppresses the navigation a click on the link would otherwise perform, leaving <see cref="OnClick"/> as
    /// the whole of what the click does.
    /// </summary>
    /// <remarks>
    /// The anchor keeps its <see cref="Href"/>, so the destination is still what the status bar shows, what a
    /// middle click opens in a new tab and what "copy link address" copies - only the plain click is answered by
    /// the handler instead of by the browser. That is what a link needs to confirm before leaving, or to save a
    /// draft first and navigate from the handler afterwards.
    /// <br />
    /// An in-page (<c>#</c>) link always suppresses its own navigation, since it scrolls rather than navigates,
    /// so this changes nothing there.
    /// </remarks>
    [Parameter] public bool PreventDefault { get; set; }

    /// <summary>
    /// If Href provided, specifies the relationship between the current document and the linked document.
    /// Ignored for empty or hash-only (#) hrefs.
    /// <br />
    /// When <see cref="Target"/> is set to <c>_blank</c> and no opener-related rel
    /// (<c>NoOpener</c>, <c>NoReferrer</c> or <c>Opener</c>) is provided, <c>noopener</c> is added automatically.
    /// </summary>
    [Parameter]
    [CallOnSet(nameof(OnSetHrefAndRel))]
    public BitLinkRels? Rel { get; set; }

    /// <summary>
    /// Sets the preset size of the link text.
    /// </summary>
    /// <remarks>
    /// A link is a piece of text before it is a control, so with nothing set here it takes the font size of
    /// whatever it sits in - which is what keeps a link inside a paragraph the same size as the sentence around
    /// it. A size is for the link that stands on its own, where there is no surrounding text to take one from.
    /// </remarks>
    [Parameter, ResetClassBuilder]
    public BitSize? Size { get; set; }

    /// <summary>
    /// If true, stops the propagation of the click event to the parent elements.
    /// Useful when the link is placed inside clickable containers like rows or cards.
    /// </summary>
    [Parameter] public bool StopPropagation { get; set; }

    /// <summary>
    /// If Href provided, specifies how to open the link (e.g. <c>_blank</c> to open it in a new tab).
    /// <br />
    /// When set to <c>_blank</c> and no opener-related <see cref="Rel"/> is provided, <c>noopener</c> is added to the rel attribute automatically.
    /// </summary>
    [Parameter] public string? Target { get; set; }

    /// <summary>
    /// The tooltip to show when the mouse is placed on the link.
    /// </summary>
    /// <remarks>
    /// A title is shown by the browser only after a hover long enough to count as one, and is reached by neither
    /// touch nor the keyboard, so nothing the reader has to have belongs only here. It is the place for what a
    /// full URL, or a longer wording of the link text, adds to the words already on screen.
    /// </remarks>
    [Parameter] public string? Title { get; set; }

    /// <summary>
    /// Styles the link with a fixed underline at all states.
    /// </summary>
    /// <remarks>
    /// <see cref="NoUnderline"/> wins over this when both are set.
    /// </remarks>
    [Parameter, ResetClassBuilder]
    public bool Underlined { get; set; }



    protected override string RootElementClass => "bit-lnk";

    protected override void RegisterCssClasses()
    {
        ClassBuilder.Register(() => NoUnderline ? "bit-lnk-nun" : string.Empty);

        // The two underline parameters ask for opposite things, and a stylesheet can only answer with whichever
        // rule it happens to declare last. Deciding it here instead makes the answer the same wherever the link
        // is used: taking the underline off is the narrower request, so it is the one that wins.
        ClassBuilder.Register(() => Underlined && NoUnderline is false ? "bit-lnk-und" : string.Empty);

        ClassBuilder.Register(() => NoColor ? "bit-lnk-ncl" : string.Empty);

        ClassBuilder.Register(() => Size switch
        {
            BitSize.Small => "bit-lnk-sm",
            BitSize.Medium => "bit-lnk-md",
            BitSize.Large => "bit-lnk-lg",
            _ => string.Empty
        });

        ClassBuilder.Register(() => Color switch
        {
            BitColor.Primary => "bit-lnk-pri",
            BitColor.Secondary => "bit-lnk-sec",
            BitColor.Tertiary => "bit-lnk-ter",
            BitColor.Info => "bit-lnk-inf",
            BitColor.Success => "bit-lnk-suc",
            BitColor.Warning => "bit-lnk-wrn",
            BitColor.SevereWarning => "bit-lnk-swr",
            BitColor.Error => "bit-lnk-err",
            BitColor.PrimaryBackground => "bit-lnk-pbg",
            BitColor.SecondaryBackground => "bit-lnk-sbg",
            BitColor.TertiaryBackground => "bit-lnk-tbg",
            BitColor.PrimaryForeground => "bit-lnk-pfg",
            BitColor.SecondaryForeground => "bit-lnk-sfg",
            BitColor.TertiaryForeground => "bit-lnk-tfg",
            BitColor.PrimaryBorder => "bit-lnk-pbr",
            BitColor.SecondaryBorder => "bit-lnk-sbr",
            BitColor.TertiaryBorder => "bit-lnk-tbr",
            _ => "bit-lnk-pri"
        });
    }

    /// <summary>
    /// Gives focus to the root element of the link.
    /// </summary>
    /// <remarks>
    /// A disabled link is only focusable when <see cref="AllowDisabledFocus"/> keeps it in the tab order;
    /// otherwise the browser ignores the call.
    /// </remarks>
    /// <returns>
    /// A ValueTask that represents the asynchronous focus operation.
    /// </returns>
    public ValueTask FocusAsync() => RootElement.FocusAsync();

    /// <summary>
    /// Gives focus to the root element of the link, optionally without scrolling it into view.
    /// </summary>
    /// <param name="preventScroll">
    /// True to leave the page scrolled where it is instead of bringing the link into view.
    /// </param>
    /// <returns>
    /// A ValueTask that represents the asynchronous focus operation.
    /// </returns>
    public ValueTask FocusAsync(bool preventScroll) => RootElement.FocusAsync(preventScroll);



    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(BitLinkParams))]
    protected override void OnParametersSet()
    {
        if (CascadingParameters is not null)
        {
            CascadingParameters.UpdateParameters(this);

            // The rel string is built as the Rel parameter is set, which has already happened by now, so a rel
            // arriving from the cascade instead would otherwise never reach the attribute.
            OnSetHrefAndRel();
        }

        _tabIndex = IsEnabled
            ? TabIndex
            : AllowDisabledFocus
                ? (TabIndex ?? (Href.HasValue() ? "0" : null))
                : Href.HasValue() ? null : "-1";

        base.OnParametersSet();
    }



    protected virtual async Task HandleClick(MouseEventArgs e)
    {
        if (IsEnabled is false) return;

        await OnClick.InvokeAsync(e);

        if (Href.HasValue() && Href!.StartsWith('#') && Href!.Length > 1)
        {
            // The scroll takes the focus with it: an in-page link that only scrolls leaves the keyboard where
            // it was, so the next Tab carries on from the link rather than from what it pointed at, and a
            // screen reader is never told the page moved at all.
            await _js.BitUtilsScrollElementIntoView(Href![1..], true);
        }
    }

    private void OnSetHrefAndRel()
    {
        _rel = Href.HasNoValue() || Href!.StartsWith('#') || Rel.HasValue is false
                ? null
                : BitLinkRelUtils.GetRels(Rel!.Value);
    }

    /// <summary>
    /// Merges the rel values the link was given with the one a new-tab link is not safe without.
    /// </summary>
    /// <remarks>
    /// The page a <c>_blank</c> link opens is handed a reference back to the one that opened it, which it can
    /// navigate somewhere else; <c>noopener</c> is what severs that. It is added unless the rel list already
    /// says what the opener relationship should be - an author asking for <c>opener</c> back means it, and
    /// <c>noreferrer</c> already implies <c>noopener</c>.
    /// </remarks>
    private static string? BuildRel(string? rel, string? target)
    {
        if (target is not "_blank") return rel;

        var tokens = rel?.Split(' ', StringSplitOptions.RemoveEmptyEntries) ?? [];

        foreach (var token in tokens)
        {
            if (token is "noopener" or "noreferrer" or "opener") return rel;
        }

        return tokens.Length > 0 ? $"{rel} noopener" : "noopener";
    }

    /// <summary>
    /// The sentence a new-tab link is announced with, or null where there is nothing to announce.
    /// </summary>
    private string? GetNewTabHint(string? target)
    {
        if (NoNewTabHint || target is not "_blank") return null;

        var hint = NewTabHint ?? DefaultNewTabHint;

        return hint.HasValue() ? hint : null;
    }

    /// <summary>
    /// The value of the <c>aria-current</c> attribute, or null where the link is not the current item.
    /// </summary>
    private string? GetAriaCurrent(string? splattedAriaCurrent)
    {
        return AriaCurrent.HasValue ? _ariaCurrentMap[AriaCurrent.Value] : splattedAriaCurrent;
    }
}
