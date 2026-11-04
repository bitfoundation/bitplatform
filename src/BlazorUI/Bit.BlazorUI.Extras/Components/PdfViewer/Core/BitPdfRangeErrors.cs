using System.Net.Http;

namespace Bit.BlazorUI;

/// <summary>Availability is independent of whether the source owns a full buffer.</summary>
internal interface IBitPdfPartialByteSource : IBitPdfByteSource, IBitPdfByteSourceLifetime
{
    bool IsDataAvailable(int offset, int length);
    int ChunkSize => 4096;
}

// These are source/control-flow failures, never malformed-content recovery.
internal class BitPdfRangePermanentException(string message)
    : BitPdfByteSourceIOException(message);

internal sealed class BitPdfRangeFallbackException(string message, HttpResponseMessage? response = null)
    : BitPdfRangePermanentException(message)
{
    internal HttpResponseMessage? Response { get; } = response;
}

internal sealed class BitPdfRangeVersionException(string message)
    : BitPdfByteSourceIOException(message);

internal sealed class BitPdfRangeBudgetException()
    : BitPdfRangePermanentException("The PDF operation exceeds the configured raw-byte cache budget. Increase CacheSize or use buffered loading.");

internal sealed class BitPdfRangeLimitException(string message)
    : BitPdfRangePermanentException(message);
