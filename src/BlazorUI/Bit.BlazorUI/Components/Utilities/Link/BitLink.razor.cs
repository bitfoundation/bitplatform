namespace Bit.BlazorUI;

/// <summary>
/// Links lead to another part of an app, other pages, or help articles. They can also be used to initiate commands.
/// </summary>
public partial class BitLink : BitComponentBase
{
    private string? _rel;



    [Inject] private IJSRuntime _js { get; set; } = default!;



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
    /// URL the link points to.
    /// </summary>
    [Parameter]
    [CallOnSet(nameof(OnSetHrefAndRel))]
    public string? Href { get; set; }

    /// <summary>
    /// Removes the applying any foreground color to the link content.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public bool NoColor { get; set; }

    /// <summary>
    /// Styles the link to have no underline at any state.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public bool NoUnderline { get; set; }

    /// <summary>
    /// Callback for when the link clicked.
    /// </summary>
    [Parameter] public EventCallback<MouseEventArgs> OnClick { get; set; }

    /// <summary>
    /// If Href provided, specifies the relationship between the current document and the linked document.
    /// </summary>
    [Parameter]
    [CallOnSet(nameof(OnSetHrefAndRel))]
    public BitLinkRel? Rel { get; set; }

    /// <summary>
    /// If Href provided, specifies how to open the link.
    /// </summary>
    [Parameter] public string? Target { get; set; }

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

    protected virtual async Task HandleClick(MouseEventArgs e)
    {
        if (IsEnabled is false) return;

        await OnClick.InvokeAsync(e);
    }

    private async Task ScrollIntoView()
    {
        if (IsEnabled is false) return;

        await _js.BitUtilsScrollElementIntoView(Href![1..]);
    }

    private void OnSetHrefAndRel()
    {
        if (Rel.HasValue is false || Href.HasNoValue() || Href!.StartsWith('#'))
        {
            _rel = null;
            return;
        }

        _rel = BitLinkRelUtils.GetRels(Rel.Value);
    }
}
