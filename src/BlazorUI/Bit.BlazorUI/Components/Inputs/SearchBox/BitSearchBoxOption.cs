namespace Bit.BlazorUI;

public partial class BitSearchBoxOption : ComponentBase, IDisposable
{
    private bool _disposed;

    [CascadingParameter] protected BitSearchBox<BitSearchBoxOption> Parent { get; set; } = default!;
    internal bool IsSelected { get; set; }


    /// <summary>
    /// The aria label attribute for the suggested option.
    /// </summary>
    [Parameter] public string? AriaLabel { get; set; }

    /// <summary>
    /// Custom CSS class for the suggested option.
    /// </summary>
    [Parameter] public string? Class { get; set; }

    /// <summary>
    /// The id for the suggested option.
    /// </summary>
    [Parameter] public string? Id { get; set; }

    /// <summary>
    /// Custom CSS style for the suggested option.
    /// </summary>
    [Parameter] public string? Style { get; set; }

    /// <summary>
    /// The text to render for the suggested option.
    /// </summary>
    [Parameter] public string? Text { get; set; }

    /// <summary>
    /// The title attribute for the suggested option.
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
        if (_disposed || disposing is false) return;

        Parent.UnregisterOption(this);

        _disposed = true;
    }
}
