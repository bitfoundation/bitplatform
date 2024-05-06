namespace Bit.BlazorUI;

public partial class BitBreadcrumbOption : IDisposable
{
    private bool _disposed;

    [CascadingParameter] protected BitBreadcrumb<BitBreadcrumbOption> Parent { get; set; } = default!;

    /// <summary>
    /// A unique value to use as a key of the breadcrumb option.
    /// </summary>
    [Parameter] public string? Key { get; set; }

    /// <summary>
    /// Text to display in the breadcrumb option.
    /// </summary>
    [Parameter] public string? Text { get; set; }

    /// <summary>
    /// URL to navigate to when the breadcrumb option is clicked.
    /// If provided, the breadcrumb option will be rendered as a link.
    /// </summary>
    [Parameter] public string? Href { get; set; }

    /// <summary>
    /// Display the breadcrumb option as the selected option.
    /// </summary>
    [Parameter] public bool IsSelected { get; set; }

    /// <summary>
    /// Click event handler of the breadcrumb option.
    /// </summary>
    [Parameter] public EventCallback<BitBreadcrumbOption> OnClick { get; set; }

    protected override string RootElementClass => "bit-bro";

    protected override async Task OnInitializedAsync()
    {
        Parent.RegisterOptions(this);

        await base.OnInitializedAsync();
    }

    protected override async Task OnParametersSetAsync()
    {
        Parent.InternalStateHasChanged();

        await base.OnParametersSetAsync();
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed || disposing is false) return;

        Parent.UnregisterOptions(this);

        _disposed = true;
    }
}
