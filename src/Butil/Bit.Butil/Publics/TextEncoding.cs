using System;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace Bit.Butil;

/// <summary>
/// Wraps the <see href="https://developer.mozilla.org/en-US/docs/Web/API/Encoding_API">Encoding API</see>
/// (<c>TextDecoder</c> and <c>TextEncoder</c>): decoding bytes written in a legacy character set -
/// Shift_JIS, windows-1256, ISO-8859-7, GBK, Big5 and the rest of the WHATWG encoding table.
/// </summary>
/// <remarks>
/// This matters more on WebAssembly than it looks. .NET only carries the Unicode encodings by
/// default; everything else needs <c>System.Text.Encoding.CodePages</c>, which is a package and a
/// registration call, and its data is dead weight in a published browser app. The browser already
/// implements every label in the encoding standard, so borrowing its decoder is often the only path
/// to reading a CSV a customer exported from a Japanese ERP or a text file off an old Windows share.
/// <br/>
/// Encoding is the other way round: .NET encodes UTF-8 perfectly well on its own, so
/// <see cref="Encode"/> is here for symmetry (and for measuring) rather than because it is needed -
/// and note that <c>TextEncoder</c> only ever produces UTF-8; there is no legacy encoder in the
/// platform, by design.
/// </remarks>
[ButilService(typeof(TextEncoding))]
public class TextEncoding(IJSRuntime js) : IAsyncDisposable
{
    /// <summary>True when the runtime exposes <c>TextDecoder</c>.</summary>
    /// <remarks>
    /// During prerender/SSR (no JS runtime) this returns <c>default</c> (e.g. <c>false</c>/<c>0</c>)
    /// rather than throwing, so the result can't be distinguished from a genuine value. If you
    /// branch on it, defer the read to <c>OnAfterRenderAsync</c>.
    /// </remarks>
    public ValueTask<bool> IsSupported() => js.Invoke<bool>("BitButil.textEncoding.isSupported");

    /// <summary>
    /// True when this runtime can decode <paramref name="label"/> - any label or alias from the
    /// <see href="https://encoding.spec.whatwg.org/#names-and-labels">encoding standard's table</see>,
    /// e.g. <c>"shift_jis"</c>, <c>"windows-1256"</c>, <c>"euc-kr"</c>, <c>"iso-8859-7"</c>.
    /// </summary>
    /// <remarks>
    /// There is no list of supported encodings anywhere in the platform, so this is answered by
    /// constructing a decoder and seeing whether it throws. Every engine implements the whole table,
    /// so a false here really means the label was misspelled.
    /// </remarks>
    public ValueTask<bool> IsEncodingSupported(string label) => js.Invoke<bool>("BitButil.textEncoding.isEncodingSupported", label);

    /// <summary>
    /// The canonical name of an encoding label - <c>"shift-jis"</c> and <c>"sjis"</c> both come back
    /// as <c>"shift_jis"</c> - or null when the label is not one the runtime knows.
    /// </summary>
    public ValueTask<string?> GetCanonicalName(string label) => js.Invoke<string?>("BitButil.textEncoding.canonicalName", label);

    /// <summary>
    /// Decodes bytes written in <paramref name="label"/>'s encoding.
    /// </summary>
    /// <param name="bytes">The bytes to decode.</param>
    /// <param name="label">An encoding label, e.g. <c>"shift_jis"</c>. Defaults to UTF-8.</param>
    /// <param name="fatal">
    /// When true, a byte sequence that is not valid in this encoding fails the call (null) instead of
    /// being replaced with U+FFFD. Worth turning on when you are guessing at the encoding and want to
    /// know you guessed wrong.
    /// </param>
    /// <param name="ignoreBom">When true, a leading byte-order mark is kept as a character rather than consumed.</param>
    /// <returns>The decoded text, or null when the label is unknown or - with <paramref name="fatal"/> - the bytes don't fit the encoding.</returns>
    public ValueTask<string?> Decode(byte[] bytes, string label = "utf-8", bool fatal = false, bool ignoreBom = false)
    {
        ArgumentNullException.ThrowIfNull(bytes);
        return js.Invoke<string?>("BitButil.textEncoding.decode", bytes, label, fatal, ignoreBom);
    }

    /// <summary>
    /// Encodes text as UTF-8, the only encoding <c>TextEncoder</c> produces.
    /// </summary>
    /// <remarks>
    /// <c>System.Text.Encoding.UTF8.GetBytes</c> does the same thing without an interop call; this is
    /// here for symmetry with <see cref="Decode"/>.
    /// </remarks>
    public ValueTask<byte[]> Encode(string text) => js.Invoke<byte[]>("BitButil.textEncoding.encode", text ?? string.Empty);

    /// <summary>
    /// How many bytes <paramref name="text"/> occupies as UTF-8, without moving the bytes across the
    /// interop boundary - useful for a length check against a server-side limit.
    /// </summary>
    public ValueTask<int> GetUtf8ByteLength(string text) => js.Invoke<int>("BitButil.textEncoding.byteLength", text ?? string.Empty);

    /// <summary>
    /// Opens a streaming decoder for content that arrives in chunks - a download, a
    /// <see cref="Fetch"/> body, a file read piece by piece.
    /// </summary>
    /// <param name="label">An encoding label, e.g. <c>"shift_jis"</c>. Defaults to UTF-8.</param>
    /// <param name="fatal">When true, invalid bytes fail the chunk (null) rather than becoming U+FFFD.</param>
    /// <param name="ignoreBom">When true, a leading byte-order mark is kept as a character rather than consumed.</param>
    /// <returns>A handle to feed chunks to, or null when the label is not one the runtime knows.</returns>
    /// <remarks>
    /// A decoder is needed rather than repeated <see cref="Decode"/> calls because a character can
    /// straddle a chunk boundary: decoding each chunk on its own turns the split character into two
    /// replacement characters. The handle holds the pending bytes between calls, so dispose it when
    /// the stream ends.
    /// </remarks>
    public async ValueTask<TextDecoderHandle?> CreateDecoder(string label = "utf-8", bool fatal = false, bool ignoreBom = false)
    {
        var id = Guid.NewGuid();
        var created = await js.Invoke<bool>("BitButil.textEncoding.createDecoder", id, label, fatal, ignoreBom);
        return created ? new TextDecoderHandle(js, id) : null;
    }

    /// <summary>
    /// On scope/circuit teardown, drops any streaming decoders whose <see cref="TextDecoderHandle"/>
    /// was never disposed.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        try
        {
            await js.InvokeVoid("BitButil.textEncoding.disposeAll");
        }
        catch (Exception ex) when (ex.IsIgnorableDisposalException()) { } // teardown: circuit gone, cancelled, or already disposed

        GC.SuppressFinalize(this);
    }
}
