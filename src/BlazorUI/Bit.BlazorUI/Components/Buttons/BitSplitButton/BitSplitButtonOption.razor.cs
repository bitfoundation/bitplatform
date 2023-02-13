
using Microsoft.AspNetCore.Components;

namespace Bit.BlazorUI;

public partial class BitSplitButtonOption : IDisposable
{
    private bool _disposed;

    [CascadingParameter] protected BitSplitButton<BitSplitButtonOption> Parent { get; set; } = default!;

    /// <summary>
    /// Name of an icon to render next to the option text
    /// </summary>
    [Parameter] public BitIconName? IconName { get; set; }

    /// <summary>
    /// Text to render in the option
    /// </summary>
    [Parameter] public string? Text { get; set; }

    /// <summary>
    /// A unique value to use as a key of the option
    /// </summary>
    [Parameter] public string? Key { get; set; }

    protected override string RootElementClass => "bit-splo";

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
