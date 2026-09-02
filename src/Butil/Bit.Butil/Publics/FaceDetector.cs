using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Bit.Butil;

/// <summary>
/// Wraps <see href="https://developer.mozilla.org/en-US/docs/Web/API/FaceDetector">FaceDetector</see>,
/// the face half of the Shape Detection API: finds where faces are in an image or a video frame,
/// using the platform's own detector rather than a JavaScript one.
/// </summary>
/// <remarks>
/// Detection, not recognition. It answers "is there a face, and where" - useful for framing a
/// profile-photo crop, focusing a camera preview, or blurring faces before an upload. It never says
/// whose face it is, and computes nothing that could.
/// <br/>
/// Support is thin even by the standards of its siblings: Chromium only, off by default on desktop,
/// and behind a flag or an origin trial where it exists at all. Treat it as an enhancement -
/// <see cref="IsSupported"/> false is the common case, and <see cref="Detect"/> returns an empty
/// array rather than throwing. <see cref="BarcodeDetector"/> is the one member of this family with
/// real deployment.
/// </remarks>
[ButilService(typeof(FaceDetector))]
public class FaceDetector(IJSRuntime js)
{
    /// <summary>True when the runtime exposes <c>FaceDetector</c>.</summary>
    /// <remarks>
    /// During prerender/SSR (no JS runtime) this returns <c>default</c> (e.g. <c>false</c>/<c>0</c>)
    /// rather than throwing, so the result can't be distinguished from a genuine value. If you
    /// branch on it, defer the read to <c>OnAfterRenderAsync</c>.
    /// </remarks>
    public ValueTask<bool> IsSupported() => js.Invoke<bool>("BitButil.faceDetector.isSupported");

    /// <summary>
    /// Scans a single frame of an element - a <c>&lt;video&gt;</c>, <c>&lt;img&gt;</c> or
    /// <c>&lt;canvas&gt;</c> - once.
    /// </summary>
    /// <param name="element">The element to read.</param>
    /// <param name="maxFaces">Stop after this many faces. 0 leaves it to the platform.</param>
    /// <param name="fastMode">Trade accuracy for speed - what a live camera preview wants.</param>
    /// <returns>Every face found, or an empty array - including when the API is unavailable or the video has no frame yet.</returns>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(DetectedFace))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(FaceLandmark))]
    public ValueTask<DetectedFace[]> Detect(ElementReference element, int maxFaces = 0, bool fastMode = false)
        => js.Invoke<DetectedFace[]>("BitButil.faceDetector.detect", element, maxFaces, fastMode);

    /// <summary>
    /// Scans an encoded image - a PNG, JPEG or anything else the browser can decode.
    /// </summary>
    /// <param name="imageBytes">The image file's bytes, not raw pixels.</param>
    /// <param name="mimeType">The image's type, e.g. <c>"image/png"</c>.</param>
    /// <param name="maxFaces">Stop after this many faces. 0 leaves it to the platform.</param>
    /// <param name="fastMode">Trade accuracy for speed.</param>
    /// <returns>Every face found, or an empty array when the image couldn't be decoded.</returns>
    /// <remarks>The decoded bitmap is released as soon as the scan finishes - it holds uncompressed pixels.</remarks>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(DetectedFace))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(FaceLandmark))]
    public ValueTask<DetectedFace[]> DetectImage(byte[] imageBytes, string mimeType = "image/png", int maxFaces = 0, bool fastMode = false)
    {
        ArgumentNullException.ThrowIfNull(imageBytes);
        return js.Invoke<DetectedFace[]>("BitButil.faceDetector.detectBytes", imageBytes, mimeType, maxFaces, fastMode);
    }
}
