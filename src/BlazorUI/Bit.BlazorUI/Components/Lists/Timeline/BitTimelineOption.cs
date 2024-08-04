namespace Bit.BlazorUI;

public partial class BitTimelineOption : ComponentBase, IDisposable
{
    private bool _disposed;


    [CascadingParameter] protected BitTimeline<BitTimelineOption> Parent { get; set; } = default!;


    /// <summary>
    /// The custom CSS classes of the timeline option.
    /// </summary>
    [Parameter] public string? Class { get; set; }

    /// <summary>
    /// The general color of the timeline option.
    /// </summary>
    [Parameter] public BitColor? Color { get; set; }

    /// <summary>
    /// The custom template for the timeline option's dot.
    /// </summary>
    [Parameter] public RenderFragment<BitTimelineOption>? DotTemplate { get; set; }

    /// <summary>
    /// Hides the timeline option's dot.
    /// </summary>
    [Parameter] public bool HideDot { get; set; }

    /// <summary>
    /// Name of an icon to render in the timeline option.
    /// </summary>
    [Parameter] public string? IconName { get; set; }

    /// <summary>
    /// Whether or not the timeline option is enabled.
    /// </summary>
    [Parameter] public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// A unique value to use as a key of the timeline option
    /// </summary>
    [Parameter] public string? Key { get; set; }

    /// <summary>
    /// Click event handler of the timeline option.
    /// </summary>
    [Parameter] public EventCallback<BitTimelineOption> OnClick { get; set; }

    /// <summary>
    /// The primary content of the timeline option.
    /// </summary>
    [Parameter] public RenderFragment<BitTimelineOption>? PrimaryContent { get; set; }

    /// <summary>
    /// The primary text of the timeline option.
    /// </summary>
    [Parameter] public string? PrimaryText { get; set; }

    /// <summary>
    /// Reverses the timeline option direction.
    /// </summary>
    [Parameter] public bool Reversed { get; set; }

    /// <summary>
    /// The secondary content of the timeline option.
    /// </summary>
    [Parameter] public RenderFragment<BitTimelineOption>? SecondaryContent { get; set; }

    /// <summary>
    /// The secondary text of the timeline option.
    /// </summary>
    [Parameter] public string? SecondaryText { get; set; }

    /// <summary>
    /// The custom value for the style attribute of the timeline option.
    /// </summary>
    [Parameter] public string? Style { get; set; }

    /// <summary>
    /// The size of the timeline option.
    /// </summary>
    [Parameter] public BitSize? Size { get; set; }

    /// <summary>
    /// The custom template for the timeline option.
    /// </summary>
    [Parameter] public RenderFragment<BitTimelineOption>? Template { get; set; }



    protected override async Task OnInitializedAsync()
    {
        Parent.RegisterOption(this);

        await base.OnInitializedAsync();
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing is false || _disposed) return;

        Parent.UnregisterOption(this);

        _disposed = true;
    }
}
