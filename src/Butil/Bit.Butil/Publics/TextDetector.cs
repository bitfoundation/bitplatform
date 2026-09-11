using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Bit.Butil;

/// <summary>
/// Wraps <see href="https://developer.mozilla.org/en-US/docs/Web/API/TextDetector">TextDetector</see>,
/// the OCR half of the Shape Detection API: reads printed text out of an image or a video frame using
/// the platform's own recognizer.
/// </summary>
/// <remarks>
/// The useful case is a camera pointed at something printed - a serial number, an invoice reference,
/// a label - where typing it in is the alternative. It recognizes text and where it is; it does not
/// tell you what the text means, and it is not a document scanner.
/// <br/>
/// Support is thin: Chromium only, and only where the underlying platform ships a text recognizer -
/// which in practice means Android and ChromeOS more than desktop. <see cref="IsSupported"/> false is
/// the common case, and <see cref="Detect"/> returns an empty array rather than throwing.
/// </remarks>
[ButilService(typeof(TextDetector))]
public class TextDetector(IJSRuntime js)
{
    /// <summary>True when the runtime exposes <c>TextDetector</c>.</summary>
    /// <remarks>
    /// During prerender/SSR (no JS runtime) this returns <c>default</c> (e.g. <c>false</c>/<c>0</c>)
    /// rather than throwing, so the result can't be distinguished from a genuine value. If you
    /// branch on it, defer the read to <c>OnAfterRenderAsync</c>.
    /// </remarks>
    public ValueTask<bool> IsSupported() => js.Invoke<bool>("BitButil.textDetector.isSupported");

    /// <summary>
    /// Reads a single frame of an element - a <c>&lt;video&gt;</c>, <c>&lt;img&gt;</c> or
    /// <c>&lt;canvas&gt;</c> - once.
    /// </summary>
    /// <param name="element">The element to read.</param>
    /// <returns>Every block of text found, or an empty array - including when the API is unavailable or the video has no frame yet.</returns>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(DetectedText))]
    public ValueTask<DetectedText[]> Detect(ElementReference element)
        => js.Invoke<DetectedText[]>("BitButil.textDetector.detect", element);

    /// <summary>
    /// Reads an encoded image - a PNG, JPEG or anything else the browser can decode.
    /// </summary>
    /// <param name="imageBytes">The image file's bytes, not raw pixels.</param>
    /// <param name="mimeType">The image's type, e.g. <c>"image/png"</c>.</param>
    /// <returns>Every block of text found, or an empty array when the image couldn't be decoded.</returns>
    /// <remarks>The decoded bitmap is released as soon as the scan finishes - it holds uncompressed pixels.</remarks>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(DetectedText))]
    public ValueTask<DetectedText[]> DetectImage(byte[] imageBytes, string mimeType = "image/png")
    {
        ArgumentNullException.ThrowIfNull(imageBytes);
        return js.Invoke<DetectedText[]>("BitButil.textDetector.detectBytes", imageBytes, mimeType);
    }
}
