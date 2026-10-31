using System.Drawing;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.JavaScript;
using System.Runtime.Versioning;
using Bit.Bmage.Memory;

namespace Bit.Bmage.Interop;

/// <summary>
/// Moves pixels and files between .NET and the browser module in the same process, on WebAssembly.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="Microsoft.JSInterop.IJSRuntime"/> serialises every argument, copies a <c>byte[]</c> into JavaScript and
/// streams results back in chunks, which for a 12 megapixel photo means 48 MB arrays allocated on both sides and
/// several copies. These <c>[JSImport]</c> calls hand JavaScript a view of the .NET buffer instead, so each direction
/// costs exactly one copy and no allocation on the .NET side. Blazor Server keeps using
/// <see cref="BrowserImageInterop"/>, since its browser is in another process.
/// </para>
/// <para>
/// The functions live on <c>globalThis.BitBmagePixels</c>, which bit-bmage.js sets when it loads, so every call first
/// makes sure the module has been imported through <see cref="BrowserImageInterop.GetModuleAsync"/>.
/// </para>
/// </remarks>
[SupportedOSPlatform("browser")]
internal static partial class BrowserPixels
{
    /// <summary>Resizes pixels with the browser's high quality resampler, or returns null when the browser cannot.</summary>
    public static async ValueTask<ImageBuffer?> ScaleAsync(BrowserImageInterop interop, ImageBuffer source, Size size, IPixelAllocator allocator, CancellationToken cancellationToken)
    {
        var id = 0;
        try
        {
            await interop.GetModuleAsync(cancellationToken).ConfigureAwait(false);
            id = Stage(source.Bytes, source.Width, source.Height);
            await Scale(id, size.Width, size.Height).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            if (Width(id) != size.Width || Height(id) != size.Height || Length(id) != size.Width * size.Height * 4) return null;
            var scaled = ImageBuffer.Create(size.Width, size.Height, allocator, clear: false);
            try
            {
                Read(id, scaled.Bytes);
            }
            catch
            {
                scaled.Dispose();
                throw;
            }
            id = 0;
            scaled.Metadata = source.Metadata;
            return scaled;
        }
        // A module that failed to load, or a browser that rejects the resize, leaves the proxy to the managed resampler.
        catch (Exception ex) when (ex is JSException or ImageCapabilityException)
        {
            return null;
        }
        finally
        {
            if (id != 0) Release(id);
        }
    }

    /// <summary>
    /// Decodes a file with the browser's decoders, reading the pixels straight into a new buffer. The arguments are those
    /// of bit-bmage.js's decode options; zero means not set.
    /// </summary>
    public static async ValueTask<ImageBuffer> DecodeAsync(BrowserImageInterop interop, ReadOnlyMemory<byte> data, string mimeType, bool autoOrient,
        int maxWidth, int maxHeight, long maxPixels, int sourceWidth, int sourceHeight, ImageLimits limits, IPixelAllocator allocator, CancellationToken cancellationToken)
    {
        await interop.GetModuleAsync(cancellationToken).ConfigureAwait(false);
        var id = 0;
        try
        {
            // Staging copies the file into the browser, which fails like any other call into it (out of memory, or an
            // outdated cached module without these functions), so it is converted the same way.
            id = Stage(MemoryMarshal.AsMemory(data).Span, 0, 0);
            await Decode(id, mimeType, autoOrient, maxWidth, maxHeight, maxPixels, sourceWidth, sourceHeight).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            int width = Width(id), height = Height(id);
            if (width <= 0 || height <= 0) throw new ImageDecodeException("The browser returned no image data.");
            limits.Validate(width, height);
            var expected = (long)width * height * 4;
            if (Length(id) != expected)
                throw new ImageDecodeException($"The browser returned {Length(id)} pixel bytes for a {width}x{height} image, expected {expected}.");
            var image = ImageBuffer.Create(width, height, allocator, clear: false, limits);
            try
            {
                Read(id, image.Bytes);
            }
            catch
            {
                image.Dispose();
                throw;
            }
            id = 0;
            return image;
        }
        catch (JSException ex)
        {
            throw AsInteropException(ex);
        }
        finally
        {
            if (id != 0) Release(id);
        }
    }

    /// <summary>Encodes pixels with the browser's encoders and returns the file.</summary>
    public static async ValueTask<byte[]> EncodeAsync(BrowserImageInterop interop, ImageBuffer image, string mimeType, double quality, string? background, CancellationToken cancellationToken)
    {
        await interop.GetModuleAsync(cancellationToken).ConfigureAwait(false);
        var id = 0;
        try
        {
            id = Stage(image.Bytes, image.Width, image.Height);
            await Encode(id, mimeType, quality, background).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            var bytes = new byte[Length(id)];
            Read(id, bytes);
            id = 0;
            return bytes;
        }
        catch (JSException ex)
        {
            throw AsInteropException(ex);
        }
        finally
        {
            if (id != 0) Release(id);
        }
    }

    /// <summary>Paints pixels into the canvas element with the given id. Returns false when there is no such canvas.</summary>
    public static async ValueTask<bool> PaintAsync(BrowserImageInterop interop, string canvasId, ImageBuffer image)
    {
        await interop.GetModuleAsync().ConfigureAwait(false);
        return Paint(canvasId, image.Bytes, image.Width, image.Height);
    }

    /// <summary>
    /// The exception the <see cref="Microsoft.JSInterop.IJSRuntime"/> path throws for the same failure, so callers
    /// handle both transports alike.
    /// </summary>
    private static Microsoft.JSInterop.JSException AsInteropException(JSException ex) => new(ex.Message, ex);

    [JSImport("globalThis.BitBmagePixels.stage")]
    private static partial int Stage([JSMarshalAs<JSType.MemoryView>] Span<byte> data, int width, int height);

    [JSImport("globalThis.BitBmagePixels.scale")]
    private static partial Task Scale(int id, int width, int height);

    [JSImport("globalThis.BitBmagePixels.decode")]
    private static partial Task Decode(int id, string mimeType, bool autoOrient, int maxWidth, int maxHeight, [JSMarshalAs<JSType.Number>] long maxPixels, int sourceWidth, int sourceHeight);

    [JSImport("globalThis.BitBmagePixels.encode")]
    private static partial Task Encode(int id, string mimeType, double quality, string? background);

    [JSImport("globalThis.BitBmagePixels.width")]
    private static partial int Width(int id);

    [JSImport("globalThis.BitBmagePixels.height")]
    private static partial int Height(int id);

    [JSImport("globalThis.BitBmagePixels.length")]
    private static partial int Length(int id);

    [JSImport("globalThis.BitBmagePixels.read")]
    private static partial void Read(int id, [JSMarshalAs<JSType.MemoryView>] Span<byte> destination);

    [JSImport("globalThis.BitBmagePixels.release")]
    private static partial void Release(int id);

    [JSImport("globalThis.BitBmagePixels.paint")]
    private static partial bool Paint(string canvasId, [JSMarshalAs<JSType.MemoryView>] Span<byte> pixels, int width, int height);
}
