using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace Bit.Butil;

/// <summary>
/// Wraps <see href="https://developer.mozilla.org/en-US/docs/Web/API/Keyboard/lock">navigator.keyboard.lock()</see>:
/// while the page is fullscreen, keys the browser would normally swallow are delivered to it instead.
/// </summary>
/// <remarks>
/// This is not the same thing as <see cref="Keyboard"/>, which registers app-level shortcuts on top
/// of ordinary key events. Keyboard lock changes which keys reach the page at all - Escape, F11,
/// Ctrl+W, Alt+Tab on some platforms - which is what a game or a remote-desktop client needs.
/// <br/>
/// <b>Preconditions:</b> a secure context, a top-level browsing context, and <b>fullscreen</b> - the
/// lock is silently dropped the moment fullscreen ends, so there is no way to trap a user in it.
/// While Escape is locked, holding it still exits fullscreen; that escape hatch cannot be taken away.
/// <br/>
/// Chromium desktop only.
/// </remarks>
[ButilService(typeof(KeyboardLock))]
public class KeyboardLock(IJSRuntime js)
{
    /// <summary>True when the runtime exposes <c>navigator.keyboard.lock</c>.</summary>
    /// <remarks>
    /// During prerender/SSR (no JS runtime) this returns <c>default</c> (e.g. <c>false</c>/<c>0</c>)
    /// rather than throwing, so the result can't be distinguished from a genuine value. If you
    /// branch on it, defer the read to <c>OnAfterRenderAsync</c>.
    /// </remarks>
    public ValueTask<bool> IsSupported() => js.Invoke<bool>("BitButil.keyboardLock.isSupported");

    /// <summary>
    /// Captures the given keys for the page.
    /// </summary>
    /// <param name="codes">
    /// The physical keys to capture, as <c>KeyboardEvent.code</c> values - <c>"Escape"</c>,
    /// <c>"KeyW"</c>, <c>"F11"</c>. Pass none to capture everything the platform allows.
    /// </param>
    /// <returns>
    /// False when the runtime has no keyboard lock, or refused: the page isn't fullscreen, isn't
    /// top-level, or the platform wouldn't hand the keys over.
    /// </returns>
    /// <remarks>
    /// Enter fullscreen first - locking outside fullscreen fails. Calling this again replaces the
    /// previous set rather than adding to it.
    /// </remarks>
    public ValueTask<bool> Lock(params string[] codes) => js.Invoke<bool>("BitButil.keyboardLock.lock", codes);

    /// <summary>
    /// Releases every captured key. Safe to call when nothing is locked.
    /// </summary>
    /// <remarks>
    /// Leaving fullscreen unlocks too, so this is for the case where the page stays fullscreen and
    /// simply stops needing the keys.
    /// </remarks>
    public ValueTask Unlock() => js.InvokeVoid("BitButil.keyboardLock.unlock");
}
