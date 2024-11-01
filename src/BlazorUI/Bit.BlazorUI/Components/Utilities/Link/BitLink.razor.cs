namespace Bit.BlazorUI;

public partial class BitLink : BitComponentBase
{
    private string? _rel;



    [Inject] private IJSRuntime _js { get; set; } = default!;



    /// <summary>
    /// The content of the link, can be any custom tag or a text.
    /// </summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// URL the link points to.
    /// </summary>
    [Parameter]
    [CallOnSet(nameof(OnSetHrefAndRel))]
    public string? Href { get; set; }

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
    public BitAnchorRel? Rel { get; set; }

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
    }

    protected virtual async Task HandleClick(MouseEventArgs e)
    {
        if (IsEnabled is false) return;

        await OnClick.InvokeAsync(e);
    }

    private async Task ScrollIntoView()
    {
        if (IsEnabled is false) return;

        await _js.ScrollElementIntoView(Href![1..]);
    }

    private void OnSetHrefAndRel()
    {
        if (Rel.HasValue is false || Href.HasNoValue() || Href!.StartsWith('#'))
        {
            _rel = null;
            return;
        }

        _rel = string.Join(" ", Enum.GetValues(typeof(BitAnchorRel)).Cast<BitAnchorRel>().Where(r => Rel.Value.HasFlag(r)).Select(r => r.ToString().ToLower()));
    }
}
