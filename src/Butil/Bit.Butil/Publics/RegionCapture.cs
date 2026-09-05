using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Bit.Butil;

/// <summary>
/// Wraps the <see href="https://developer.mozilla.org/en-US/docs/Web/API/Screen_Capture_API/Region_Capture">Region Capture API</see>:
/// crops a capture of this tab down to the rectangle one element occupies.
/// </summary>
/// <remarks>
/// The case this exists for: a video call where the user shares "this tab" and your app wants only
/// the slide area to go out, not the chat panel, the toolbar or the participant list beside it. Ask
/// for the tab once through <see cref="MediaDevices.GetDisplayMedia"/>, then crop the track to the
/// element - no second picker, and nothing outside the crop is ever encoded.
/// <br/>
/// Crops by rectangle: whatever sits on top of the element inside that rectangle - a dropdown, a
/// tooltip, a modal - is captured too. <see cref="ElementCapture"/> is the sibling that captures the
/// element's own content instead, which is the one to reach for when overlays are the problem.
/// <br/>
/// Only applies to a capture of this very tab. A screen or window share refuses the crop, and the
/// call returns false rather than throwing.
/// </remarks>
[ButilService(typeof(RegionCapture))]
public class RegionCapture(IJSRuntime js)
{
    /// <summary>True when the runtime exposes <c>CropTarget.fromElement</c>.</summary>
    /// <remarks>
    /// Chromium only. During prerender/SSR (no JS runtime) this returns <c>default</c>
    /// (e.g. <c>false</c>/<c>0</c>) rather than throwing, so the result can't be distinguished from a
    /// genuine value. If you branch on it, defer the read to <c>OnAfterRenderAsync</c>.
    /// </remarks>
    public ValueTask<bool> IsSupported() => js.Invoke<bool>("BitButil.regionCapture.isSupported");

    /// <summary>
    /// Crops a captured stream to the rectangle an element occupies. Takes effect on the next frame,
    /// and follows the element as it moves or resizes.
    /// </summary>
    /// <param name="stream">A stream from <see cref="MediaDevices.GetDisplayMedia"/> - one where the user chose this tab.</param>
    /// <param name="element">The element to crop to.</param>
    /// <returns>
    /// False when the API is unavailable, when the capture isn't of this tab, or when the element
    /// isn't rendered - all ordinary outcomes rather than exceptions.
    /// </returns>
    public ValueTask<bool> CropTo(MediaStreamHandle stream, ElementReference element)
    {
        ArgumentNullException.ThrowIfNull(stream);
        return js.Invoke<bool>("BitButil.regionCapture.cropTo", stream.Id, element);
    }

    /// <summary>
    /// Removes the crop: the stream goes back to the whole captured surface.
    /// </summary>
    /// <param name="stream">The stream to un-crop.</param>
    public ValueTask<bool> Clear(MediaStreamHandle stream)
    {
        ArgumentNullException.ThrowIfNull(stream);
        return js.Invoke<bool>("BitButil.regionCapture.clear", stream.Id);
    }
}
