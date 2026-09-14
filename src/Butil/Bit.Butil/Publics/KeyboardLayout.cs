using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace Bit.Butil;

/// <summary>
/// Wraps <see href="https://developer.mozilla.org/en-US/docs/Web/API/Keyboard/getLayoutMap">navigator.keyboard.getLayoutMap()</see>:
/// what each physical key actually prints on the user's keyboard layout.
/// </summary>
/// <remarks>
/// The problem it solves: a shortcut is bound to a physical key (<c>KeyboardEvent.code</c>), but the
/// hint shown to the user has to be the character that key produces. Hard-coding "W" is wrong on
/// AZERTY, where the same key prints "Z". Look the code up here and show what comes back.
/// <br/>
/// Chromium desktop only, and a secure context. Where <see cref="IsSupported"/> is false, fall back
/// to deriving the label from the code.
/// </remarks>
[ButilService(typeof(KeyboardLayout))]
public class KeyboardLayout(IJSRuntime js)
{
    /// <summary>True when the runtime exposes <c>navigator.keyboard.getLayoutMap</c>.</summary>
    /// <remarks>
    /// During prerender/SSR (no JS runtime) this returns <c>default</c> (e.g. <c>false</c>/<c>0</c>)
    /// rather than throwing, so the result can't be distinguished from a genuine value. If you
    /// branch on it, defer the read to <c>OnAfterRenderAsync</c>.
    /// </remarks>
    public ValueTask<bool> IsSupported() => js.Invoke<bool>("BitButil.keyboardLayout.isSupported");

    /// <summary>
    /// The whole layout: every writing-system key the browser knows about, paired with the character
    /// it prints.
    /// </summary>
    /// <returns>
    /// The entries, or an empty array when the runtime can't report a layout. Only keys that print
    /// something are included - modifiers and function keys are not in the map.
    /// </returns>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(KeyboardLayoutEntry))]
    public ValueTask<KeyboardLayoutEntry[]> GetLayoutMap()
        => js.Invoke<KeyboardLayoutEntry[]>("BitButil.keyboardLayout.getLayoutMap");

    /// <summary>
    /// What one physical key prints on this layout.
    /// </summary>
    /// <param name="code">A <c>KeyboardEvent.code</c> value, e.g. <c>"KeyW"</c>.</param>
    /// <returns>
    /// The character to show the user, or null when the runtime can't report a layout or the key
    /// prints nothing.
    /// </returns>
    public ValueTask<string?> Get(string code) => js.Invoke<string?>("BitButil.keyboardLayout.get", code);
}
