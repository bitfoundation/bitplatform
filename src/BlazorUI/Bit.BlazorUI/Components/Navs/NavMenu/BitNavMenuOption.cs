namespace Bit.BlazorUI;

public partial class BitNavMenuOption : ComponentBase, IDisposable
{
    private bool _disposed;



    [CascadingParameter] protected BitNavMenu<BitNavMenuOption> NavMenu { get; set; } = default!;



    /// <summary>
    /// Custom CSS class for the nav option.
    /// </summary>
    [Parameter] public string? Class { get; set; }

    /// <summary>
    /// The custom data for the nav option to provide additional state.
    /// </summary>
    [Parameter] public object? Data { get; set; }

    /// <summary>
    /// Name of an icon to render next to the nav option.
    /// </summary>
    [Parameter] public string? IconName { get; set; }

    /// <summary>
    /// Whether or not the nav option is enabled.
    /// </summary>
    [Parameter] public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// A unique value to use as a key or id of the nav option.
    /// </summary>
    [Parameter] public string? Key { get; set; }

    /// <summary>
    /// Custom CSS style for the nav option.
    /// </summary>
    [Parameter] public string? Style { get; set; }

    /// <summary>
    /// Link target, specifies how to open the nav option's link.
    /// </summary>
    [Parameter] public string? Target { get; set; }

    /// <summary>
    /// The custom template for the nav option to render.
    /// </summary>
    [Parameter] public RenderFragment<BitNavMenuOption>? Template { get; set; }

    /// <summary>
    /// Text to render for the nav option.
    /// </summary>
    [Parameter] public string Text { get; set; } = string.Empty;

    /// <summary>
    /// Text for the tooltip of the nav option.
    /// </summary>
    [Parameter] public string? Title { get; set; }

    /// <summary>
    /// The nav option's link URL.
    /// </summary>
    [Parameter] public string? Url { get; set; }

    /// <summary>
    /// Alternative URLs to be considered when auto mode tries to detect the selected nav option by the current URL.
    /// </summary>
    [Parameter] public IEnumerable<string>? AdditionalUrls { get; set; }



    protected override async Task OnInitializedAsync()
    {
        NavMenu?.RegisterOption(this);

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

        NavMenu?.UnregisterOption(this);

        _disposed = true;
    }
}
