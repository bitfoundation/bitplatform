namespace Bit.BlazorUI;

public partial class BitLink : BitComponentBase
{
    [Inject] private IJSRuntime _js { get; set; } = default!;



    /// <summary>
    /// URL the link points to
    /// </summary>
    [Parameter] public string? Href { get; set; }

    /// <summary>
    /// If Href provided, specifies how to open the link
    /// </summary>
    [Parameter] public string? Target { get; set; }

    /// <summary>
    /// The content of link, can be any custom tag or a text
    /// </summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Whether the link is styled with an underline or not.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public bool Underlined { get; set; }

    /// <summary>
    /// Callback for when the link clicked
    /// </summary>
    [Parameter] public EventCallback<MouseEventArgs> OnClick { get; set; }



    protected override string RootElementClass => "bit-lnk";

    protected override void RegisterCssClasses()
    {
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
}
