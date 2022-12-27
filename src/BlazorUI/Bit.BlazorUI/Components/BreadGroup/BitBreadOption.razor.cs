
namespace Bit.BlazorUI;

public partial class BitBreadOption : IDisposable
{
    private bool _disposed;
    internal ElementStyleBuilder _styleBuilder => StyleBuilder;
    internal ElementClassBuilder _classBuilder => ClassBuilder;

    [CascadingParameter] protected BitBreadGroup? BreadGroup { get; set; }

    /// <summary>
    /// URL to navigate to when this BitBreadOption is clicked.
    /// If provided, the BitBreadOption will be rendered as a link.
    /// </summary>
    [Parameter] public string? Href { get; set; }

    /// <summary>
    /// By default, the Selected option is the last option. But it can also be specified manually.
    /// </summary>
    [Parameter] public bool IsSelected { get; set; }

    /// <summary>
    /// Callback for when the BitBreadOption clicked and Href is empty.
    /// </summary>
    [Parameter] public EventCallback<MouseEventArgs> OnClick { get; set; }

    /// <summary>
    /// Text to display in the BitBreadOption option.
    /// </summary>
    [Parameter] public string? Text { get; set; }

    protected override string RootElementClass => "bit-bro";

    protected override async Task OnInitializedAsync()
    {
        if (BreadGroup is not null)
        {
            BreadGroup.RegisterOptions(this);
        }
        
        await base.OnInitializedAsync();
    }

    internal async Task HandleOnOptionClick(MouseEventArgs e)
    {
        if (IsEnabled is false) return;
        if (Href.HasValue()) return;

        await OnClick.InvokeAsync(e);
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed) return;
        if (!disposing) return;

        if (BreadGroup is not null)
        {
            BreadGroup.UnregisterOptions(this);
        }

        _disposed = true;
    }
}
