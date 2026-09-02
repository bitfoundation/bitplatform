using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace Bit.Butil;

/// <summary>
/// Finds and creates elements Blazor did not render - <c>querySelector</c>,
/// <c>getElementById</c>, <c>createElement</c> and node traversal.
/// </summary>
/// <remarks>
/// Every other DOM API in Butil is anchored to an <see cref="ElementReference"/>, which means an
/// element your own markup rendered. That is a deliberate boundary: inside your own components,
/// reaching for the DOM by selector is how Blazor's diffing and your code end up disagreeing about
/// what is on the page.
/// <br/>
/// This is the way out of it for the cases the boundary does not cover: a third-party widget's
/// markup, something a script put on the page, an element you are building to hand to a library.
/// <b>Do not use it to reach into your own components</b> - Blazor will re-render over whatever you
/// changed, at a moment you do not control.
/// <br/>
/// The bridge back is <see cref="DomHandle.AsElementReference"/>: it turns a found or created
/// element into an <see cref="ElementReference"/>, and every element extension in the rest of Butil
/// then works on it.
/// </remarks>
[ButilService(typeof(Dom))]
public class Dom(IJSRuntime js) : IAsyncDisposable
{
    /// <summary>True when there is a document to query, which is any real browser.</summary>
    /// <remarks>
    /// During prerender/SSR (no JS runtime) this returns <c>default</c> (<c>false</c>) rather than
    /// throwing, so the result can't be distinguished from a genuine value.
    /// </remarks>
    public ValueTask<bool> IsSupported() => js.Invoke<bool>("BitButil.dom.isSupported");

    /// <summary>
    /// The first element matching a CSS selector, searched from the document.
    /// </summary>
    /// <returns>A handle, or null when nothing matches or the selector is invalid.</returns>
    /// <remarks>
    /// An invalid selector answers null rather than throwing: a selector is usually built from
    /// something a user or a configuration file supplied, so a bad one is an input error, not an
    /// exceptional condition.
    /// </remarks>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(DomNodeDto))]
    public async ValueTask<DomHandle?> Query(string selector)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(selector);
        return Wrap(await js.Invoke<DomNodeDto?>("BitButil.dom.query", null, selector));
    }

    /// <summary>
    /// Every element matching a CSS selector.
    /// </summary>
    /// <returns>The matches, or an empty array when there are none or the selector is invalid.</returns>
    /// <remarks>
    /// This is a snapshot, not a live list: elements added afterwards are not in it, and handles to
    /// elements since removed keep working on a detached element. Dispose the ones you keep.
    /// </remarks>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(DomNodeDto))]
    public async ValueTask<DomHandle[]> QueryAll(string selector)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(selector);
        return WrapAll(await js.Invoke<DomNodeDto[]>("BitButil.dom.queryAll", null, selector));
    }

    /// <summary>The element with this <c>id</c>, or null.</summary>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(DomNodeDto))]
    public async ValueTask<DomHandle?> ById(string elementId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(elementId);
        return Wrap(await js.Invoke<DomNodeDto?>("BitButil.dom.byId", elementId));
    }

    /// <summary>The document's <c>&lt;body&gt;</c>.</summary>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(DomNodeDto))]
    public async ValueTask<DomHandle?> Body() => Wrap(await js.Invoke<DomNodeDto?>("BitButil.dom.body"));

    /// <summary>The document's <c>&lt;head&gt;</c> - where a script or a stylesheet you are adding goes.</summary>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(DomNodeDto))]
    public async ValueTask<DomHandle?> Head() => Wrap(await js.Invoke<DomNodeDto?>("BitButil.dom.head"));

    /// <summary>The root <c>&lt;html&gt;</c> element - the usual place for a theme attribute or class.</summary>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(DomNodeDto))]
    public async ValueTask<DomHandle?> DocumentElement() => Wrap(await js.Invoke<DomNodeDto?>("BitButil.dom.documentElement"));

    /// <summary>
    /// Creates an element. It is not on the page until something appends it.
    /// </summary>
    /// <param name="tagName">The element name - <c>"div"</c>, <c>"my-widget"</c>, <c>"script"</c>.</param>
    /// <param name="namespaceUri">
    /// The namespace for a non-HTML element: <c>"http://www.w3.org/2000/svg"</c> for SVG,
    /// <c>"http://www.w3.org/1998/Math/MathML"</c> for MathML. Creating an SVG element without it
    /// produces something that looks right in the tree and renders as nothing.
    /// </param>
    /// <returns>A handle, or null when the name is not a valid element name.</returns>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(DomNodeDto))]
    public async ValueTask<DomHandle?> Create(string tagName, string? namespaceUri = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tagName);
        return Wrap(await js.Invoke<DomNodeDto?>("BitButil.dom.create", tagName, namespaceUri));
    }

    // The context an ElementReference needs to be usable: Butil's element extensions - and Blazor's
    // own FocusAsync - reach the JS runtime through it, so a reference built without one throws when
    // it is used rather than when it is made. One per service instance, which is one per circuit or
    // WebAssembly app, matching the runtime it wraps.
    private WebElementReferenceContext? _referenceContext;
    internal WebElementReferenceContext ReferenceContext => _referenceContext ??= new WebElementReferenceContext(js);

    internal DomHandle? Wrap(DomNodeDto? node) => node is null ? null : new DomHandle(js, this, node.Id, node.TagName ?? string.Empty);

    internal DomHandle[] WrapAll(DomNodeDto[]? nodes)
        => nodes is null ? [] : Array.ConvertAll(nodes, node => new DomHandle(js, this, node.Id, node.TagName ?? string.Empty));

    /// <summary>
    /// On scope/circuit teardown, releases every handle. The elements themselves are untouched -
    /// releasing a handle is not removing an element.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        try { await js.InvokeVoid("BitButil.dom.releaseAll"); }
        catch (Exception ex) when (ex.IsIgnorableDisposalException()) { } // teardown: circuit gone, cancelled, or already disposed

        GC.SuppressFinalize(this);
    }
}
