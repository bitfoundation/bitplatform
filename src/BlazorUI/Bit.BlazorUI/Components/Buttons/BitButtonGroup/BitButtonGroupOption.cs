namespace Bit.BlazorUI;

public partial class BitButtonGroupOption : ComponentBase, IDisposable
{
    private bool _disposed;


    [CascadingParameter] protected BitButtonGroup<BitButtonGroupOption> Parent { get; set; } = default!;


    /// <summary>
    /// The custom CSS classes of the option.
    /// </summary>
    [Parameter] public string? Class { get; set; }

    /// <summary>
    /// Name of an icon to render next to the option text
    /// </summary>
    [Parameter] public string? IconName { get; set; }

    /// <summary>
    /// Whether or not the option is enabled.
    /// </summary>
    [Parameter] public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// A unique value to use as a key of the option
    /// </summary>
    [Parameter] public string? Key { get; set; }

    /// <summary>
    /// The icon of the option when it is not checked in toggle mode.
    /// </summary>
    [Parameter] public string? OffIconName { get; set; }

    /// <summary>
    /// The text of the option when it is not checked in toggle mode.
    /// </summary>
    [Parameter] public string? OffText { get; set; }

    /// <summary>
    /// The title of the option when it is not checked in toggle mode.
    /// </summary>
    [Parameter] public string? OffTitle { get; set; }

    /// <summary>
    /// The icon of the option when it is checked in toggle mode.
    /// </summary>
    [Parameter] public string? OnIconName { get; set; }

    /// <summary>
    /// The text of the option when it is checked in toggle mode.
    /// </summary>
    [Parameter] public string? OnText { get; set; }

    /// <summary>
    /// The title of the option when it is checked in toggle mode.
    /// </summary>
    [Parameter] public string? OnTitle { get; set; }

    /// <summary>
    /// Click event handler of the option.
    /// </summary>
    [Parameter] public EventCallback<BitButtonGroupOption> OnClick { get; set; }

    /// <summary>
    /// Reverses the positions of the icon and the main content of the option.
    /// </summary>
    [Parameter] public bool ReversedIcon { get; set; }

    /// <summary>
    /// The custom value for the style attribute of the option.
    /// </summary>
    [Parameter] public string? Style { get; set; }

    /// <summary>
    /// The custom template for the option.
    /// </summary>
    [Parameter] public RenderFragment<BitButtonGroupOption>? Template { get; set; }

    /// <summary>
    /// Text to render in the option
    /// </summary>
    [Parameter] public string? Text { get; set; }

    /// <summary>
    /// Title to render in the option
    /// </summary>
    [Parameter] public string? Title { get; set; }



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
