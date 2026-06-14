using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace Bit.Butil;

/// <summary>
/// Wraps the <see href="https://developer.mozilla.org/en-US/docs/Web/API/EyeDropper">EyeDropper API</see>.
/// </summary>
public class EyeDropper(IJSRuntime js)
{
    /// <summary>True when the runtime exposes <c>window.EyeDropper</c>.</summary>
    public ValueTask<bool> IsSupported() => js.Invoke<bool>("BitButil.eyeDropper.isSupported");

    /// <summary>
    /// Opens the eyedropper and returns the picked sRGB color in hex form (e.g. <c>#1f2937</c>).
    /// Returns null when the user cancels or the runtime can't show the picker.
    /// </summary>
    /// <remarks>
    /// Must be called from a user-gesture handler - the browser will reject the request otherwise.
    /// </remarks>
    public ValueTask<string?> Open() => js.Invoke<string?>("BitButil.eyeDropper.open");
}
