namespace Bit.BlazorUI;

public partial class BitBreadcrumbOption : BitComponentBase, IDisposable
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
    /// Name of an icon to render next to the item text.
    /// </summary>
    [Parameter] public string? IconName { get; set; }

    /// <summary>
    /// Reverses the positions of the icon and the item text of the item content.
    /// </summary>
    [Parameter] public bool? ReversedIcon { get; set; }

    /// <summary>
    /// Display the breadcrumb option as the selected option.
    /// </summary>
    [Parameter] public bool IsSelected { get; set; }

    /// <summary>
    /// Click event handler of the breadcrumb option.
    /// </summary>
    [Parameter] public EventCallback<BitBreadcrumbOption> OnClick { get; set; }

    /// <summary>
    /// The custom template for the option in overflow list.
    /// </summary>
    [Parameter] public RenderFragment<BitBreadcrumbOption>? OverflowTemplate { get; set; }

    /// <summary>
    /// The custom template for the option.
    /// </summary>
    [Parameter] public RenderFragment<BitBreadcrumbOption>? Template { get; set; }

    protected override string RootElementClass => "bit-bro";

    protected override async Task OnInitializedAsync()
    {
        Parent.RegisterOptions(this);

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

        Parent.UnregisterOptions(this);

        _disposed = true;
    }
}
