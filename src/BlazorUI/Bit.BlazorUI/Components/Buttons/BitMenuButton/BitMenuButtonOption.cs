namespace Bit.BlazorUI;

public class BitMenuButtonOption : ComponentBase, IDisposable
{
    private bool _disposed;

    [CascadingParameter] protected BitMenuButton<BitMenuButtonOption> Parent { get; set; } = default!;


    /// <summary>
    /// The custom CSS classes of the option.
    /// </summary>
    [Parameter] public string? Class { get; set; }

    /// <summary>
    /// Name of an icon to render next to the option text.
    /// </summary>
    [Parameter] public string? IconName { get; set; }

    /// <summary>
    /// Whether or not the option is enabled.
    /// </summary>
    [Parameter] public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// Determines the selection state of the item.
    /// </summary>
    [Parameter] public bool IsSelected { get; set; }

    /// <summary>
    /// A unique value to use as a key of the option.
    /// </summary>
    [Parameter] public string? Key { get; set; }

    /// <summary>
    /// Click event handler of the option.
    /// </summary>
    [Parameter] public EventCallback<BitMenuButtonOption> OnClick { get; set; }

    /// <summary>
    /// The custom value for the style attribute of the option.
    /// </summary>
    [Parameter] public string? Style { get; set; }

    /// <summary>
    /// The custom template for the option.
    /// </summary>
    [Parameter] public RenderFragment<BitMenuButtonOption>? Template { get; set; }

    /// <summary>
    /// Text to render in the option.
    /// </summary>
    [Parameter] public string? Text { get; set; }


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
