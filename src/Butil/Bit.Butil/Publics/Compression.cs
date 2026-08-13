using System;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace Bit.Butil;

/// <summary>
/// Wraps the <see href="https://developer.mozilla.org/en-US/docs/Web/API/Compression_Streams_API">Compression Streams API</see>:
/// gzip and deflate, done by the browser's own native codec.
/// </summary>
/// <remarks>
/// .NET has <c>GZipStream</c>, and on Blazor Server that is the better tool. This earns its place
/// on WebAssembly, where compressing a few megabytes in managed code runs in the single UI thread
/// while the browser's implementation is native - and where not pulling the managed compression
/// code into the published bundle is itself worth something.
/// </remarks>
public class Compression(IJSRuntime js)
{
    /// <summary>True when the runtime exposes <c>CompressionStream</c>.</summary>
    /// <remarks>
    /// During prerender/SSR (no JS runtime) this returns <c>default</c> (e.g. <c>false</c>/<c>0</c>)
    /// rather than throwing, so the result can't be distinguished from a genuine value. If you
    /// branch on it, defer the read to <c>OnAfterRenderAsync</c>.
    /// </remarks>
    public ValueTask<bool> IsSupported() => js.Invoke<bool>("BitButil.compression.isSupported");

    /// <summary>
    /// Compresses bytes.
    /// </summary>
    /// <param name="data">The bytes to compress.</param>
    /// <param name="format">Which codec to use. Defaults to gzip.</param>
    /// <returns>The compressed bytes, or null when the API is unavailable or the stream errored.</returns>
    /// <remarks>
    /// Compressing already-compressed data (a JPEG, a zip) usually makes it slightly larger - the
    /// result is returned either way, so compare the lengths if that matters.
    /// </remarks>
    public ValueTask<byte[]?> Compress(byte[] data, CompressionFormat format = CompressionFormat.Gzip)
    {
        ArgumentNullException.ThrowIfNull(data);
        return js.Invoke<byte[]?>("BitButil.compression.compress", data, ToName(format));
    }

    /// <summary>
    /// Decompresses bytes.
    /// </summary>
    /// <param name="data">The compressed bytes.</param>
    /// <param name="format">The codec the data was compressed with. Defaults to gzip.</param>
    /// <returns>
    /// The original bytes, or null when the API is unavailable, the input is corrupt, or the format
    /// doesn't match what the data actually is. Nothing is thrown - a bad payload from the network
    /// is a normal outcome, not an exceptional one.
    /// </returns>
    public ValueTask<byte[]?> Decompress(byte[] data, CompressionFormat format = CompressionFormat.Gzip)
    {
        ArgumentNullException.ThrowIfNull(data);
        return js.Invoke<byte[]?>("BitButil.compression.decompress", data, ToName(format));
    }

    /// <summary>
    /// Compresses text as UTF-8. A convenience over <see cref="Compress(byte[], CompressionFormat)"/>
    /// for the common case of shrinking JSON before storing it.
    /// </summary>
    /// <param name="text">The text to compress.</param>
    /// <param name="format">Which codec to use. Defaults to gzip.</param>
    /// <returns>The compressed bytes, or null when the API is unavailable.</returns>
    public ValueTask<byte[]?> CompressText(string text, CompressionFormat format = CompressionFormat.Gzip)
    {
        ArgumentNullException.ThrowIfNull(text);
        return Compress(System.Text.Encoding.UTF8.GetBytes(text), format);
    }

    /// <summary>
    /// Decompresses bytes back into UTF-8 text.
    /// </summary>
    /// <param name="data">The compressed bytes.</param>
    /// <param name="format">The codec the data was compressed with. Defaults to gzip.</param>
    /// <returns>The text, or null when the payload couldn't be decompressed.</returns>
    public async ValueTask<string?> DecompressText(byte[] data, CompressionFormat format = CompressionFormat.Gzip)
    {
        var bytes = await Decompress(data, format);
        return bytes is null ? null : System.Text.Encoding.UTF8.GetString(bytes);
    }

    private static string ToName(CompressionFormat format) => format switch
    {
        CompressionFormat.Deflate => "deflate",
        CompressionFormat.DeflateRaw => "deflate-raw",
        _ => "gzip",
    };
}
