using System.Drawing;
using Bit.Bmage.Interop;
using Microsoft.JSInterop;

namespace Bit.Bmage.Codecs.Browser;

internal static class BrowserImageScaler
{
    public static async ValueTask<ImageBuffer?> ScaleAsync(BrowserImageInterop interop, ImageBuffer source, Size size)
    {
        try
        {
            using var result = await interop.InvokeForBytesAsync<BrowserImageInfo>("scale", CancellationToken.None, source.ToArray(), source.Width, source.Height, size.Width, size.Height).ConfigureAwait(false);
            if (result.Info is not { } info || info.Width != size.Width || info.Height != size.Height || result.Bytes.Length != size.Width * size.Height * 4) return null;
            var scaled = ImageBuffer.Create(size.Width, size.Height, clear: false);
            result.Bytes.Span.CopyTo(scaled.Bytes);
            scaled.Metadata = source.Metadata;
            return scaled;
        }
        catch (Exception ex) when (ex is JSException or ImageCapabilityException)
        {
            return null;
        }
    }
}
