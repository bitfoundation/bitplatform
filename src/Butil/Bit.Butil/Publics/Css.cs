using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Bit.Butil;

/// <summary>
/// Wraps the CSS object model: <see href="https://developer.mozilla.org/en-US/docs/Web/API/Window/getComputedStyle">getComputedStyle</see>,
/// <c>CSS.supports</c>, <c>CSS.escape</c>, <c>CSS.registerProperty</c>, stylesheet rules, and the
/// <see href="https://developer.mozilla.org/en-US/docs/Web/API/CSS_Custom_Highlight_API">CSS Custom Highlight API</see>.
/// </summary>
/// <remarks>
/// Butil's element extensions read and write <em>inline</em> style, which is what an element was
/// told; this reads what it actually <em>is</em>. The computed value is the answer after the
/// cascade, inheritance, and the browser resolving relative units into pixels - so a width set in
/// <c>em</c> comes back in <c>px</c>, and a colour set as a keyword comes back as <c>rgb()</c>.
/// <br/>
/// Computing a style is not free: it forces layout if anything has changed since the last one, which
/// is why reading a style inside a loop that also writes one is the classic way to make a page
/// crawl. Read what you need in one call, then write.
/// </remarks>
[ButilService(typeof(Css))]
public class Css(IJSRuntime js) : IAsyncDisposable
{
    /// <summary>True when the runtime exposes <c>getComputedStyle</c>, which is everywhere.</summary>
    /// <remarks>
    /// During prerender/SSR (no JS runtime) this returns <c>default</c> (<c>false</c>) rather than
    /// throwing, so the result can't be distinguished from a genuine value.
    /// </remarks>
    public ValueTask<bool> IsSupported() => js.Invoke<bool>("BitButil.css.isSupported");

    /// <summary>True when the runtime exposes <c>CSS.supports</c>.</summary>
    public ValueTask<bool> IsSupportsAvailable() => js.Invoke<bool>("BitButil.css.isSupportsAvailable");

    /// <summary>True when the runtime exposes <c>CSS.registerProperty</c>.</summary>
    public ValueTask<bool> IsRegisterPropertyAvailable() => js.Invoke<bool>("BitButil.css.isRegisterPropertyAvailable");

    /// <summary>
    /// True when the runtime has constructable stylesheets. <see cref="CreateStyleSheet"/> works
    /// either way - where they are missing it appends a <c>&lt;style&gt;</c> element instead, which
    /// behaves the same from here.
    /// </summary>
    public ValueTask<bool> IsConstructableStyleSheetAvailable() => js.Invoke<bool>("BitButil.css.isConstructableStyleSheetAvailable");

    /// <summary>True when the runtime has the CSS Custom Highlight API.</summary>
    public ValueTask<bool> IsHighlightAvailable() => js.Invoke<bool>("BitButil.css.isHighlightAvailable");

    /// <summary>
    /// The resolved value of each named property.
    /// </summary>
    /// <param name="element">The element to measure.</param>
    /// <param name="properties">
    /// The property names, in CSS spelling: <c>"font-size"</c>, not <c>"fontSize"</c>. Custom
    /// properties work too - <c>"--brand"</c>.
    /// </param>
    /// <param name="pseudoElement">
    /// A pseudo-element to compute instead, as <c>"::before"</c>. This is the only way to read
    /// generated content's style at all - it has no element to reach for.
    /// </param>
    /// <returns>Property name to resolved value, or null when the element is not there.</returns>
    /// <remarks>
    /// A list rather than the whole style object on purpose: a computed style has some 350
    /// properties on it, and marshalling all of them to answer a question about two is most of the
    /// cost of the call. <see cref="GetComputedStyleAll"/> is there when you really do want them.
    /// </remarks>
    public ValueTask<Dictionary<string, string>?> GetComputedStyle(ElementReference element, string[] properties, string? pseudoElement = null)
    {
        ArgumentNullException.ThrowIfNull(properties);
        return js.Invoke<Dictionary<string, string>?>("BitButil.css.computed", element, properties, pseudoElement);
    }

    /// <summary>
    /// One resolved value.
    /// </summary>
    /// <returns>The value, or an empty string when the property is not set or the element is not there.</returns>
    public async ValueTask<string> GetComputedStyleValue(ElementReference element, string property, string? pseudoElement = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(property);

        var values = await GetComputedStyle(element, [property], pseudoElement);
        return values is not null && values.TryGetValue(property, out var value) ? value : string.Empty;
    }

    /// <summary>
    /// Every resolved property. Some 350 of them, so reach for
    /// <see cref="GetComputedStyle"/> unless you genuinely want the lot - dumping a style for
    /// diagnostics is the case where you do.
    /// </summary>
    public ValueTask<Dictionary<string, string>?> GetComputedStyleAll(ElementReference element, string? pseudoElement = null)
        => js.Invoke<Dictionary<string, string>?>("BitButil.css.computedAll", element, pseudoElement);

    /// <summary>
    /// Whether the browser understands a property/value pair - <c>Supports("display", "grid")</c>.
    /// </summary>
    /// <remarks>
    /// The honest way to feature-detect CSS: it asks the parser, rather than inferring support from
    /// a browser's name. False where <c>CSS.supports</c> itself is missing, which is old enough that
    /// treating it as "no" is right.
    /// </remarks>
    public ValueTask<bool> Supports(string property, string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(property);
        ArgumentNullException.ThrowIfNull(value);
        return js.Invoke<bool>("BitButil.css.supports", property, value);
    }

    /// <summary>
    /// Whether the browser understands a whole condition, written as it would be inside
    /// <c>@supports</c>: <c>"(display: grid) and (gap: 1rem)"</c>, or
    /// <c>"selector(:has(a))"</c>.
    /// </summary>
    public ValueTask<bool> SupportsCondition(string condition)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(condition);
        return js.Invoke<bool>("BitButil.css.supportsCondition", condition);
    }

    /// <summary>
    /// Escapes a string for use in a selector.
    /// </summary>
    /// <remarks>
    /// An id that starts with a digit, or contains a dot or a space, is perfectly legal HTML and
    /// illegal in a selector without this. Any id that came from data rather than from your own
    /// markup should go through it.
    /// </remarks>
    public ValueTask<string> Escape(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return js.Invoke<string>("BitButil.css.escape", value);
    }

    /// <summary>
    /// Teaches the browser what a custom property means.
    /// </summary>
    /// <param name="name">The property, including its leading dashes: <c>"--brand"</c>.</param>
    /// <param name="syntax">
    /// What it holds, in the syntax grammar: <c>"&lt;color&gt;"</c>, <c>"&lt;length&gt;"</c>,
    /// <c>"&lt;number&gt;"</c>, <c>"*"</c> for anything.
    /// </param>
    /// <param name="inherits">Whether children inherit it.</param>
    /// <param name="initialValue">Its value before anything sets one. Required for every syntax except <c>"*"</c>.</param>
    /// <returns>Null on success, or the reason it failed.</returns>
    /// <remarks>
    /// This is what lets a custom property be animated or transitioned: an unregistered one is just
    /// a string to the browser, and strings do not interpolate - which is why a transition on
    /// <c>--brand</c> does nothing until it is registered. Registering the same name twice fails, so
    /// do it once at startup.
    /// </remarks>
    public ValueTask<string?> RegisterProperty(string name, string syntax = "*", bool inherits = false, string? initialValue = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(syntax);
        return js.Invoke<string?>("BitButil.css.registerProperty", name, syntax, inherits, initialValue);
    }

    /// <summary>
    /// Creates a stylesheet of your own, already in the document.
    /// </summary>
    /// <returns>A handle, or null when it could not be created.</returns>
    /// <remarks>
    /// Rules added here apply to the whole page, exactly as a <c>&lt;style&gt;</c> block would. It
    /// is the way to write a rule that a selector reaches - a theme, a print stylesheet, a
    /// <c>::highlight()</c> - rather than setting a style on one element at a time.
    /// </remarks>
    public async ValueTask<StyleSheetHandle?> CreateStyleSheet()
    {
        var id = Guid.NewGuid();
        var created = await js.Invoke<bool>("BitButil.css.createSheet", id);
        return created ? new StyleSheetHandle(js, id) : null;
    }

    /// <summary>
    /// Highlights every occurrence of some text inside an element, without touching the DOM.
    /// </summary>
    /// <param name="name">
    /// A name for this highlight, which is what a <c>::highlight(name)</c> rule styles. Nothing is
    /// visible until such a rule exists - <see cref="CreateStyleSheet"/> is how to add one.
    /// </param>
    /// <param name="element">Where to search.</param>
    /// <param name="search">The text to find.</param>
    /// <param name="caseSensitive">Whether case matters. False by default, which is what a search box wants.</param>
    /// <returns>How many occurrences were highlighted, or -1 where the API is missing.</returns>
    /// <remarks>
    /// This is the reason the Custom Highlight API exists. Wrapping matches in <c>&lt;mark&gt;</c>
    /// mutates the DOM - which breaks a Blazor diff, invalidates anything measured around it, and
    /// has to be undone before the next search. A highlight is painted over the text and changes
    /// nothing underneath it.
    /// </remarks>
    public ValueTask<int> HighlightText(string name, ElementReference element, string search, bool caseSensitive = false)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(search);
        return js.Invoke<int>("BitButil.css.highlightText", name, element, search, caseSensitive);
    }

    /// <summary>Removes a highlight by name. Removing one that is not there is not an error.</summary>
    public ValueTask ClearHighlight(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        return js.InvokeVoid("BitButil.css.clearHighlight", name);
    }

    /// <summary>
    /// On scope/circuit teardown, removes every stylesheet whose handle was never disposed, so a
    /// page's rules do not outlive the component that added them.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        try { await js.InvokeVoid("BitButil.css.disposeAll"); }
        catch (Exception ex) when (ex.IsIgnorableDisposalException()) { } // teardown: circuit gone, cancelled, or already disposed

        GC.SuppressFinalize(this);
    }
}
