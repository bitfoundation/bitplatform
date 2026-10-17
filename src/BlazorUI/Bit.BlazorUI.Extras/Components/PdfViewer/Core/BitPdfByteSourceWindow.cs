using System;
using System.Threading;

namespace Bit.BlazorUI;

/// <summary>
/// A borrowed raw-byte window. Keep the window alive for as long as its memory
/// is used, and dispose it before releasing that storage. Memory-backed windows
/// need no lease and do not allocate a lease object.
/// </summary>
internal readonly struct BitPdfByteSourceWindow : IDisposable
{
    private readonly IDisposable? _lease;

    public BitPdfByteSourceWindow(ReadOnlyMemory<byte> memory, IDisposable? lease = null)
    {
        Memory = memory;
        _lease = lease;
    }

    public ReadOnlyMemory<byte> Memory { get; }
    public ReadOnlySpan<byte> Span => Memory.Span;
    public int Length => Memory.Length;
    public bool IsEmpty => Memory.IsEmpty;
    public byte[] ToArray() => Memory.ToArray();
    public void Dispose() => _lease?.Dispose();
}

/// <summary>Resource lifetime, separate from the memory source's hot path.</summary>
internal interface IBitPdfByteSourceLifetime
{
    void ThrowIfDisposed();
    void CheckRead();
}

/// <summary>An idempotent release, including when a window struct is copied.</summary>
internal sealed class BitPdfByteSourceLease(Action release) : IDisposable
{
    private Action? _release = release;
    public void Dispose() => Interlocked.Exchange(ref _release, null)?.Invoke();
}

/// <summary>Source failures must not be treated as malformed PDF content.</summary>
internal sealed class BitPdfByteSourceIOException(string message, Exception? inner = null)
    : System.IO.IOException(message, inner);

internal static class BitPdfByteSourceErrors
{
    public static bool IsContentError(Exception exception)
        => exception is not OperationCanceledException
            and not ObjectDisposedException
            and not BitPdfByteSourceIOException;
}
