using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Bit.Butil;

/// <summary>
/// Wraps the parts of <see href="https://developer.mozilla.org/en-US/docs/Web/API/HTMLCanvasElement">HTMLCanvasElement</see>
/// that get a picture in and bytes out: <c>drawImage</c>, <c>toDataURL</c> and <c>toBlob</c>.
/// </summary>
/// <remarks>
/// This is deliberately not a 2D drawing API. Every path, gradient and text call from C# would be an
/// interop round trip, and a drawing loop written that way is slower than the same loop in
/// JavaScript by orders of magnitude - if you are <em>drawing</em>, write the drawing in a script.
/// <br/>
/// What C# genuinely could not do before is the other half: take a frame from a video, a photo from
/// a camera stream, or the contents of a canvas, and get it into a <c>byte[]</c> - a thumbnail, a
/// screenshot, an upload. That is what this covers.
/// <br/>
/// <b>Tainted canvases.</b> A canvas that has drawn a cross-origin image cannot be read back at all:
/// the browser refuses, to stop a page using a canvas to read pictures the user can see but the page
/// is not allowed to have. Every export here answers with null in that case rather than throwing,
/// because drawing someone else's picture is a normal thing to do. Serve the image same-origin, or
/// with CORS headers and <c>crossorigin="anonymous"</c> on the element.
/// </remarks>
[ButilService(typeof(Canvas))]
public class Canvas(IJSRuntime js)
{
    /// <summary>True when the runtime can create a 2D canvas context, which is everywhere.</summary>
    /// <remarks>
    /// During prerender/SSR (no JS runtime) this returns <c>default</c> (<c>false</c>) rather than
    /// throwing, so the result can't be distinguished from a genuine value.
    /// </remarks>
    public ValueTask<bool> IsSupported() => js.Invoke<bool>("BitButil.canvas.isSupported");

    /// <summary>
    /// True when the runtime exposes <c>OffscreenCanvas</c>, which <see cref="Capture"/> uses when it
    /// is there. Capture works either way - the fallback is a canvas element that is never attached
    /// to the document.
    /// </summary>
    public ValueTask<bool> IsOffscreenSupported() => js.Invoke<bool>("BitButil.canvas.isOffscreenSupported");

    /// <summary>
    /// True when a WebGL 2 context can be created. Butil does not wrap WebGL - a draw call per
    /// interop hop is not a rendering API - but knowing whether it is available is often the
    /// question behind "can this device do the thing at all".
    /// </summary>
    public ValueTask<bool> IsWebGlSupported() => js.Invoke<bool>("BitButil.canvas.isWebGlSupported");

    /// <summary>
    /// True when <c>navigator.gpu</c> is present. Same caveat as <see cref="IsWebGlSupported"/>: a
    /// capability probe, not a wrapper.
    /// </summary>
    public ValueTask<bool> IsWebGpuSupported() => js.Invoke<bool>("BitButil.canvas.isWebGpuSupported");

    /// <summary>
    /// The canvas's pixel buffer size, alongside the size CSS is displaying it at.
    /// </summary>
    /// <returns>Null when the element is not a canvas.</returns>
    /// <remarks>
    /// The two disagreeing is the usual cause of a blurry canvas: the buffer is what you draw into,
    /// CSS is what the user sees, and the browser scales one to the other. Match them - multiplied by
    /// <see cref="CanvasSize.DevicePixelRatio"/> - for a sharp result.
    /// </remarks>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(CanvasSize))]
    public ValueTask<CanvasSize?> GetSize(ElementReference canvas) => js.Invoke<CanvasSize?>("BitButil.canvas.size", canvas);

    /// <summary>
    /// Resizes the canvas's pixel buffer.
    /// </summary>
    /// <returns>False when the element is not a canvas.</returns>
    /// <remarks>
    /// Setting either dimension <em>clears</em> the canvas, even to the value it already had. That
    /// is the specification's rule, and it is also the shortest way to erase one.
    /// </remarks>
    public ValueTask<bool> SetSize(ElementReference canvas, int width, int height)
        => js.Invoke<bool>("BitButil.canvas.setSize", canvas, width, height);

    /// <summary>Erases everything on the canvas, leaving it transparent.</summary>
    /// <returns>False when the element is not a canvas.</returns>
    public ValueTask<bool> Clear(ElementReference canvas) => js.Invoke<bool>("BitButil.canvas.clear", canvas);

    /// <summary>
    /// Draws an image source onto a canvas.
    /// </summary>
    /// <param name="canvas">The destination canvas element.</param>
    /// <param name="source">
    /// A <c>&lt;video&gt;</c>, <c>&lt;img&gt;</c> or another <c>&lt;canvas&gt;</c>. A video is drawn
    /// at whatever frame it is showing, which is how a still is taken from a camera stream.
    /// </param>
    /// <param name="options">Which part of the source, and where on the canvas. Defaults to all of it, stretched to fill.</param>
    /// <returns>
    /// False when either element is wrong, or when the source has nothing to draw yet - a video
    /// before its first frame has decoded, most often, which is worth retrying rather than reporting.
    /// </returns>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(CanvasDrawOptions))]
    public ValueTask<bool> DrawImage(ElementReference canvas, ElementReference source, CanvasDrawOptions? options = null)
        => js.Invoke<bool>("BitButil.canvas.drawImage", canvas, source, options);

    /// <summary>
    /// Encodes the canvas as a <c>data:</c> URL - ready to put straight into an <c>&lt;img src&gt;</c>.
    /// </summary>
    /// <param name="canvas">The canvas to export.</param>
    /// <param name="type">An image MIME type: <c>"image/png"</c> (the default and lossless), <c>"image/jpeg"</c>, <c>"image/webp"</c>. An unsupported type silently falls back to PNG.</param>
    /// <param name="quality">0 to 1, for the lossy formats. Ignored for PNG.</param>
    /// <returns>Null when the element is not a canvas, or the canvas is tainted by a cross-origin image.</returns>
    /// <remarks>
    /// A data URL is base64, so it is about a third larger than the bytes it carries and it all sits
    /// in a string. For anything but a small preview, prefer <see cref="ToBytes"/>.
    /// </remarks>
    public ValueTask<string?> ToDataUrl(ElementReference canvas, string type = "image/png", double quality = 0.92)
        => js.Invoke<string?>("BitButil.canvas.toDataUrl", canvas, type, quality);

    /// <summary>
    /// Encodes the canvas as image bytes - ready to upload, hash or save.
    /// </summary>
    /// <param name="canvas">The canvas to export.</param>
    /// <param name="type">An image MIME type. Defaults to PNG.</param>
    /// <param name="quality">0 to 1, for the lossy formats.</param>
    /// <returns>Null when the element is not a canvas, or the canvas is tainted by a cross-origin image.</returns>
    public ValueTask<byte[]?> ToBytes(ElementReference canvas, string type = "image/png", double quality = 0.92)
        => js.Invoke<byte[]?>("BitButil.canvas.toBytes", canvas, type, quality);

    /// <summary>
    /// Takes a picture from a video, image or canvas and encodes it, with no canvas of your own
    /// involved.
    /// </summary>
    /// <param name="source">A <c>&lt;video&gt;</c>, <c>&lt;img&gt;</c> or <c>&lt;canvas&gt;</c>.</param>
    /// <param name="width">
    /// The width to scale to. Leave both this and <paramref name="height"/> at 0 for the source's own
    /// size; give one and leave the other 0 to keep the aspect ratio - so a thumbnail is one number,
    /// not two.
    /// </param>
    /// <param name="height">The height to scale to. See <paramref name="width"/>.</param>
    /// <param name="type">An image MIME type. Defaults to PNG; JPEG or WebP are the ones to reach for when size matters.</param>
    /// <param name="quality">0 to 1, for the lossy formats.</param>
    /// <returns>
    /// The encoded bytes, or null when the source has nothing to show yet, or when it is a
    /// cross-origin image the browser will not let a page read back.
    /// </returns>
    /// <remarks>
    /// This is the thumbnail-and-screenshot path: the source's <em>intrinsic</em> size is what gets
    /// captured, not the size CSS is displaying it at, so a video laid out at 320px still yields its
    /// full frame.
    /// </remarks>
    public ValueTask<byte[]?> Capture(ElementReference source, int width = 0, int height = 0,
                                      string type = "image/png", double quality = 0.92)
        => js.Invoke<byte[]?>("BitButil.canvas.capture", source, width, height, type, quality);

    /// <summary>
    /// The same capture as <see cref="Capture"/>, encoded as a <c>data:</c> URL for showing straight
    /// away.
    /// </summary>
    /// <returns>Null in the same cases as <see cref="Capture"/>.</returns>
    public ValueTask<string?> CaptureToDataUrl(ElementReference source, int width = 0, int height = 0,
                                               string type = "image/png", double quality = 0.92)
        => js.Invoke<string?>("BitButil.canvas.captureToDataUrl", source, width, height, type, quality);
}
