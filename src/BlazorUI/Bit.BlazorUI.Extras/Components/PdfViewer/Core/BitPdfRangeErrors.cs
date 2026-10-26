using System.Net.Http;

namespace Bit.BlazorUI;

/// <summary>Availability is independent of whether the source owns a full buffer.</summary>
internal interface IBitPdfPartialByteSource : IBitPdfByteSource, IBitPdfByteSourceLifetime
{
    bool IsDataAvailable(int offset, int length);
}

// These are source/control-flow failures, never malformed-content recovery.
internal sealed class BitPdfRangeFallbackException(string message, HttpResponseMessage? response = null)
    : BitPdfByteSourceIOException(message)
{
    internal HttpResponseMessage? Response { get; } = response;
}

internal sealed class BitPdfRangeVersionException(string message)
    : BitPdfByteSourceIOException(message);

internal sealed class BitPdfRangeBudgetException()
    : BitPdfByteSourceIOException("The PDF operation exceeds the configured raw-byte cache budget. Increase CacheSize or use buffered loading.");
