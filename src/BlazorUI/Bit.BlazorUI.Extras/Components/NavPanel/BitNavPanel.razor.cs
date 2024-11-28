using System.Diagnostics.CodeAnalysis;

namespace Bit.BlazorUI;

public partial class BitNavPanel : IDisposable
{
    private bool _disposed;

    public void Dispose()
    {
        if (_disposed) return;

        _disposed = true;
    }
}
