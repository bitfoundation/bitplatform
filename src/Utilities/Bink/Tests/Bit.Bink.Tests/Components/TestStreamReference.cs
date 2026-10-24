using Microsoft.JSInterop;

namespace Bit.Bink.Tests.Components;

/// <summary>A browser stream for tests: what <c>DotNet.createJSStreamReference</c> hands .NET, over fixed bytes.</summary>
internal sealed class TestStreamReference : IJSStreamReference
{
    private readonly byte[] _bytes;

    public TestStreamReference(byte[] bytes)
    {
        // Blazor refuses to make a stream of nothing ("Length must be a positive value"), so a test that needs an
        // empty one is testing a contract the browser does not offer.
        ArgumentOutOfRangeException.ThrowIfZero(bytes.Length);
        _bytes = bytes;
    }

    public long Length => _bytes.Length;

    public bool Disposed { get; private set; }

    /// <summary>When set, reading waits for it, as a large stream takes several round trips to arrive.</summary>
    public Task? Arrival { get; init; }

    public async ValueTask<Stream> OpenReadStreamAsync(long maxAllowedSize = 512000, CancellationToken cancellationToken = default)
    {
        // The real reference refuses a stream over the limit rather than reading on, with this exception and message.
        if (_bytes.Length > maxAllowedSize) throw new ArgumentOutOfRangeException(nameof(maxAllowedSize), $"The incoming data stream of length {_bytes.Length} exceeds the maximum allowed length {maxAllowedSize}.");
        if (Arrival is not null) await Arrival.ConfigureAwait(false);
        return new MemoryStream(_bytes, writable: false);
    }

    public ValueTask DisposeAsync()
    {
        Disposed = true;
        return ValueTask.CompletedTask;
    }
}
