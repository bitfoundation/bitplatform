using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Bit.Butil;

/// <summary>
/// Wraps <see href="https://developer.mozilla.org/en-US/docs/Web/API/ElementInternals">ElementInternals</see>
/// and its <see href="https://developer.mozilla.org/en-US/docs/Web/API/CustomStateSet">CustomStateSet</see>:
/// a component's own state, exposed to CSS as <c>:state(name)</c>, plus the ARIA semantics it
/// declares for itself.
/// </summary>
/// <remarks>
/// <b>What this is for:</b> a Blazor component that wants a real state selector instead of a class.
/// A class like <c>.is-loading</c> is in the same namespace as every other class on the page and can
/// be overridden by accident; <c>:state(loading)</c> belongs to the element and cannot be set from
/// outside it.
/// <br/>
/// <b>Why <see cref="Define"/> exists:</b> internals can only be attached from inside a custom
/// element's constructor, which is JS that has to run before the element is upgraded. Butil defines a
/// minimal element for you - it does nothing but keep its internals - so <c>&lt;my-panel&gt;</c>
/// rendered from a Blazor component gains states and ARIA defaults without any JS of your own.
/// <br/>
/// The tag has to contain a dash, must be defined before the element is rendered, and a definition
/// lasts for the document's lifetime.
/// </remarks>
[ButilService(typeof(CustomElements))]
public class CustomElements(IJSRuntime js)
{
    /// <summary>True when the runtime implements <c>ElementInternals.states</c>.</summary>
    /// <remarks>
    /// During prerender/SSR (no JS runtime) this returns <c>default</c> (e.g. <c>false</c>/<c>0</c>)
    /// rather than throwing, so the result can't be distinguished from a genuine value. If you
    /// branch on it, defer the read to <c>OnAfterRenderAsync</c>.
    /// </remarks>
    public ValueTask<bool> IsSupported() => js.Invoke<bool>("BitButil.customElements.isSupported");

    /// <summary>Whether a tag name has already been defined - by Butil or by anything else.</summary>
    public ValueTask<bool> IsDefined(string tagName) => js.Invoke<bool>("BitButil.customElements.isDefined", tagName);

    /// <summary>
    /// Defines a minimal custom element that carries internals, so elements with this tag can hold
    /// states and ARIA defaults.
    /// </summary>
    /// <param name="tagName">The tag, which must contain a dash: <c>"my-panel"</c>.</param>
    /// <returns>
    /// True when the tag is defined - including when it already was. False for an invalid name, or a
    /// runtime without <c>ElementInternals</c>.
    /// </returns>
    /// <remarks>
    /// Call it before the element is rendered; an element already in the DOM is upgraded, but any
    /// state set before the upgrade is lost. A definition can never be replaced, so calling this
    /// twice is a no-op rather than an error.
    /// </remarks>
    public ValueTask<bool> Define(string tagName) => js.Invoke<bool>("BitButil.customElements.define", tagName);

    /// <summary>Whether this element was upgraded by a definition from <see cref="Define"/> and therefore has internals.</summary>
    public ValueTask<bool> HasInternals(ElementReference element)
        => js.Invoke<bool>("BitButil.customElements.hasInternals", element);

    /// <summary>
    /// Adds a state, matched in CSS as <c>:state(<paramref name="state"/>)</c>.
    /// </summary>
    /// <returns>False when the element has no internals - see <see cref="Define"/>.</returns>
    public ValueTask<bool> AddState(ElementReference element, string state)
        => js.Invoke<bool>("BitButil.customElements.addState", element, state);

    /// <summary>Removes a state. False when it wasn't set, or the element has no internals.</summary>
    public ValueTask<bool> DeleteState(ElementReference element, string state)
        => js.Invoke<bool>("BitButil.customElements.deleteState", element, state);

    /// <summary>Whether a state is set.</summary>
    public ValueTask<bool> HasState(ElementReference element, string state)
        => js.Invoke<bool>("BitButil.customElements.hasState", element, state);

    /// <summary>Every state currently set on the element. Empty when it has no internals.</summary>
    public ValueTask<string[]> GetStates(ElementReference element)
        => js.Invoke<string[]>("BitButil.customElements.getStates", element);

    /// <summary>Removes every state.</summary>
    public ValueTask<bool> ClearStates(ElementReference element)
        => js.Invoke<bool>("BitButil.customElements.clearStates", element);

    /// <summary>
    /// Sets an ARIA <b>default</b> through the element's internals - the semantics the component
    /// claims for itself.
    /// </summary>
    /// <param name="element">An element with internals.</param>
    /// <param name="property">
    /// The <c>ElementInternals</c> member name: <c>"role"</c>, <c>"ariaLabel"</c>,
    /// <c>"ariaExpanded"</c>, <c>"ariaDisabled"</c>, and so on.
    /// </param>
    /// <param name="value">The value, or null to clear it.</param>
    /// <returns>False when the element has no internals.</returns>
    /// <remarks>
    /// A default, not an override: an author-supplied attribute on the element still wins, which is
    /// exactly what makes this safe for a reusable component to do.
    /// </remarks>
    public ValueTask<bool> SetAria(ElementReference element, string property, string? value)
        => js.Invoke<bool>("BitButil.customElements.setAria", element, property, value);
}
