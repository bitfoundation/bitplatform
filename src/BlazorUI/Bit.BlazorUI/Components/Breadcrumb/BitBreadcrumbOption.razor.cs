namespace Bit.BlazorUI;

public partial class BitBreadcrumbOption : IDisposable
{
    protected override bool UseVisual => false;

    private bool _disposed;

    [CascadingParameter] protected BitBreadcrumb<BitBreadcrumbOption> Parent { get; set; } = default!;

    /// <summary>
    /// URL to navigate to when this BitBreadOption is clicked.
    /// If provided, the BitBreadOption will be rendered as a link.
    /// </summary>
    [Parameter] public string? Href { get; set; }

    /// <summary>
    /// Set the Selected option.
    /// </summary>
    [Parameter]
    public bool IsSelected { get; set; }

    /// <summary>
    /// Text to display in the BitBreadOption option.
    /// </summary>
    [Parameter] public string? Text { get; set; }

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
