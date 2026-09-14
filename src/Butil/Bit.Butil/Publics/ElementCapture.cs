using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Bit.Butil;

/// <summary>
/// Wraps the <see href="https://developer.mozilla.org/en-US/docs/Web/API/Screen_Capture_API/Element_Capture">Element Capture API</see>:
/// restricts a capture of this tab to one element's own content.
/// </summary>
/// <remarks>
/// The stricter sibling of <see cref="RegionCapture"/>. Region Capture crops to the rectangle an
/// element occupies, so anything drawn on top of it inside that rectangle - a dropdown, a tooltip, a
/// notification, a modal - is shared too. Element Capture shares the element's own subtree and
/// nothing else: the overlay is invisible to the capture even while it covers the element on screen.
/// That is what makes it the right one for sharing a document or a map surface while your own UI
/// keeps working over it.
/// <br/>
/// It asks more of the element in return: it has to form its own stacking context and be laid out on
/// its own (no transform that flattens it into its parent, no fragmentation across columns). The
/// browser refuses otherwise, and <see cref="RestrictTo"/> returns false.
/// <br/>
/// Only applies to a capture of this very tab. A screen or window share refuses the restriction.
/// </remarks>
[ButilService(typeof(ElementCapture))]
public class ElementCapture(IJSRuntime js)
{
    /// <summary>True when the runtime exposes <c>RestrictionTarget.fromElement</c>.</summary>
    /// <remarks>
    /// Chromium only, and newer than <see cref="RegionCapture"/>. During prerender/SSR (no JS runtime)
    /// this returns <c>default</c> (e.g. <c>false</c>/<c>0</c>) rather than throwing, so the result
    /// can't be distinguished from a genuine value. If you branch on it, defer the read to
    /// <c>OnAfterRenderAsync</c>.
    /// </remarks>
    public ValueTask<bool> IsSupported() => js.Invoke<bool>("BitButil.elementCapture.isSupported");

    /// <summary>
    /// Restricts a captured stream to one element's own content. Takes effect on the next frame.
    /// </summary>
    /// <param name="stream">A stream from <see cref="MediaDevices.GetDisplayMedia"/> - one where the user chose this tab.</param>
    /// <param name="element">The element to capture. It has to form its own stacking context.</param>
    /// <returns>
    /// False when the API is unavailable, when the capture isn't of this tab, or when the element
    /// doesn't meet the layout requirements - all ordinary outcomes rather than exceptions.
    /// </returns>
    public ValueTask<bool> RestrictTo(MediaStreamHandle stream, ElementReference element)
    {
        ArgumentNullException.ThrowIfNull(stream);
        return js.Invoke<bool>("BitButil.elementCapture.restrictTo", stream.Id, element);
    }

    /// <summary>
    /// Removes the restriction: the stream goes back to the whole captured surface.
    /// </summary>
    /// <param name="stream">The stream to un-restrict.</param>
    public ValueTask<bool> Clear(MediaStreamHandle stream)
    {
        ArgumentNullException.ThrowIfNull(stream);
        return js.Invoke<bool>("BitButil.elementCapture.clear", stream.Id);
    }
}
