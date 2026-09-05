using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace Bit.Butil;

/// <summary>
/// Wraps the <see href="https://developer.mozilla.org/en-US/docs/Web/API/CSS">CSS</see> namespace
/// object: feature queries, identifier escaping, custom-property registration, and the Houdini
/// worklets.
/// </summary>
/// <remarks>
/// The per-element half of the Typed OM - reading computed values as numbers, writing inline styles
/// without string concatenation - lives on <see cref="ElementReferenceStyleMapExtensions"/>.
/// <br/>
/// <c>registerProperty</c> is the piece most worth knowing about: it is what turns a custom property
/// into something the animation engine can interpolate, so a gradient or an angle can be animated at
/// all.
/// </remarks>
[ButilService(typeof(Css))]
public class Css(IJSRuntime js)
{
    /// <summary>True when the runtime implements the CSS Typed OM's unit factories (<c>CSS.px</c> and friends).</summary>
    /// <remarks>
    /// During prerender/SSR (no JS runtime) this returns <c>default</c> (e.g. <c>false</c>/<c>0</c>)
    /// rather than throwing, so the result can't be distinguished from a genuine value. If you
    /// branch on it, defer the read to <c>OnAfterRenderAsync</c>.
    /// </remarks>
    public ValueTask<bool> IsSupported() => js.Invoke<bool>("BitButil.css.isSupported");

    /// <summary>True when the runtime implements the Houdini paint worklet - Chromium only.</summary>
    public ValueTask<bool> SupportsPaintWorklet() => js.Invoke<bool>("BitButil.css.supportsPaintWorklet");

    /// <summary>True when the runtime implements the Houdini layout worklet - behind a flag even in Chromium.</summary>
    public ValueTask<bool> SupportsLayoutWorklet() => js.Invoke<bool>("BitButil.css.supportsLayoutWorklet");

    /// <summary>
    /// Whether the browser understands a declaration - <c>CSS.supports("display", "grid")</c>.
    /// <br/>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/CSS/supports_static">https://developer.mozilla.org/en-US/docs/Web/API/CSS/supports_static</see>
    /// </summary>
    public ValueTask<bool> Supports(string property, string value) => js.Invoke<bool>("BitButil.css.supports", property, value);

    /// <summary>
    /// Escapes a string so it is safe to use as an identifier in a selector - the fix for a
    /// generated id that starts with a digit or contains a colon.
    /// <br/>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/CSS/escape_static">https://developer.mozilla.org/en-US/docs/Web/API/CSS/escape_static</see>
    /// </summary>
    public ValueTask<string> Escape(string value) => js.Invoke<string>("BitButil.css.escape", value);

    /// <summary>
    /// Registers a custom property with a type, so the browser can interpolate it.
    /// <br/>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/CSS/registerProperty_static">https://developer.mozilla.org/en-US/docs/Web/API/CSS/registerProperty_static</see>
    /// </summary>
    /// <returns>
    /// False when the runtime has no <c>registerProperty</c>, the property is already registered, or
    /// the initial value doesn't parse as the declared syntax.
    /// </returns>
    /// <remarks>
    /// A registration lasts for the document's lifetime and cannot be undone or replaced, so
    /// registering the same name twice returns false rather than throwing - which is what makes this
    /// safe to call from a component that mounts more than once.
    /// </remarks>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(CssPropertyDefinition))]
    public ValueTask<bool> RegisterProperty(CssPropertyDefinition definition)
        => js.Invoke<bool>("BitButil.css.registerProperty", definition);

    /// <summary>
    /// Loads a paint worklet - a script that draws a custom <c>paint()</c> image the way a canvas
    /// does, but as a live CSS value.
    /// <br/>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/PaintWorklet">https://developer.mozilla.org/en-US/docs/Web/API/PaintWorklet</see>
    /// </summary>
    /// <param name="url">
    /// URL of the worklet module. It is fetched as a classic worklet script, so it has to be a real
    /// file the server serves - not something bundled into the app's own JS.
    /// </param>
    /// <returns>False when the runtime has no paint worklet, or the module failed to load.</returns>
    /// <remarks>
    /// The worklet runs in its own global scope with no DOM: it receives the size and the custom
    /// properties it declared an interest in, and draws. That is what makes it fast, and what makes
    /// it unable to reach anything in the page.
    /// </remarks>
    public ValueTask<bool> AddPaintWorklet(string url) => js.Invoke<bool>("BitButil.css.addPaintWorklet", url);

    /// <summary>
    /// Loads a layout worklet - a script that implements a custom <c>display: layout(…)</c>.
    /// <br/>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/CSS/layoutWorklet_static">https://developer.mozilla.org/en-US/docs/Web/API/CSS/layoutWorklet_static</see>
    /// </summary>
    /// <returns>False when the runtime has no layout worklet, or the module failed to load.</returns>
    /// <remarks>
    /// The least-shipped part of Houdini - behind a flag even in Chromium. Treat a true here as a
    /// pleasant surprise rather than a platform you can build on.
    /// </remarks>
    public ValueTask<bool> AddLayoutWorklet(string url) => js.Invoke<bool>("BitButil.css.addLayoutWorklet", url);
}
