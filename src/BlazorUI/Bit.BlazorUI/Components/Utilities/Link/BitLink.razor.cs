namespace Bit.BlazorUI;

/// <summary>
/// Links lead to another part of an app, other pages, or help articles. They can also be used to initiate commands.
/// </summary>
public partial class BitLink : BitComponentBase
{
    private string? _rel;
    private string? _tabIndex;



    [Inject] private IJSRuntime _js { get; set; } = default!;



    /// <summary>
    /// Keeps the disabled link focusable and discoverable by assistive technologies.
    /// When enabled, the disabled state is conveyed using the <c>aria-disabled</c> attribute instead of removing
    /// the element from the tab order, so keyboard and screen reader users can still find the link while its
    /// navigation and click action stay suppressed.
    /// </summary>
    [Parameter] public bool AllowDisabledFocus { get; set; }

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
    /// A value starting with the <c>#</c> character makes the link smooth-scroll the element with that id into view,
    /// while a bare <c>#</c> renders an inert link that navigates nowhere.
    /// </summary>
    [Parameter]
    [CallOnSet(nameof(OnSetHrefRelAndTarget))]
    public string? Href { get; set; }

    /// <summary>
    /// Removes applying any foreground color to the link content, letting it keep its own color.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public bool NoColor { get; set; }

    /// <summary>
    /// Styles the link to have no underline at any state.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public bool NoUnderline { get; set; }

    /// <summary>
    /// Callback for when the link is clicked.
    /// It is invoked in every render mode of the link: on anchor links it runs alongside the navigation,
    /// and on button links (no Href) it is the sole click action.
    /// </summary>
    [Parameter] public EventCallback<MouseEventArgs> OnClick { get; set; }

    /// <summary>
    /// If Href provided, specifies the relationship between the current document and the linked document.
    /// Ignored for empty or hash-only (#) hrefs.
    /// <br />
    /// When <see cref="Target"/> is set to <c>_blank</c> and no opener-related rel
    /// (<c>NoOpener</c>, <c>NoReferrer</c> or <c>Opener</c>) is provided, <c>noopener</c> is added automatically.
    /// </summary>
    [Parameter]
    [CallOnSet(nameof(OnSetHrefRelAndTarget))]
    public BitLinkRels? Rel { get; set; }

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
    [Parameter]
    [CallOnSet(nameof(OnSetHrefRelAndTarget))]
    public string? Target { get; set; }

    /// <summary>
    /// Styles the link with a fixed underline at all states.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public bool Underlined { get; set; }



    protected override string RootElementClass => "bit-lnk";

    protected override void RegisterCssClasses()
    {
        ClassBuilder.Register(() => NoUnderline ? "bit-lnk-nun" : string.Empty);

        ClassBuilder.Register(() => Underlined ? "bit-lnk-und" : string.Empty);

        ClassBuilder.Register(() => NoColor ? "bit-lnk-ncl" : string.Empty);

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



    protected override void OnParametersSet()
    {
        _tabIndex = IsEnabled
            ? TabIndex
            : AllowDisabledFocus
                ? (TabIndex ?? (Href.HasValue() ? "0" : null))
                : "-1";

        base.OnParametersSet();
    }



    protected virtual async Task HandleClick(MouseEventArgs e)
    {
        if (IsEnabled is false) return;

        await OnClick.InvokeAsync(e);

        if (Href.HasValue() && Href!.StartsWith('#') && Href!.Length > 1)
        {
            await _js.BitUtilsScrollElementIntoView(Href![1..]);
        }
    }

    private void OnSetHrefRelAndTarget()
    {
        if (Href.HasNoValue() || Href!.StartsWith('#'))
        {
            _rel = null;
            return;
        }

        var rel = Rel.HasValue ? BitLinkRelUtils.GetRels(Rel.Value) : null;

        var hasOpenerRel = Rel.HasValue && (Rel.Value.HasFlag(BitLinkRels.NoOpener) ||
                                            Rel.Value.HasFlag(BitLinkRels.NoReferrer) ||
                                            Rel.Value.HasFlag(BitLinkRels.Opener));

        if (Target is "_blank" && hasOpenerRel is false)
        {
            rel = rel.HasValue() ? $"{rel} noopener" : "noopener";
        }

        _rel = rel;
    }
}
