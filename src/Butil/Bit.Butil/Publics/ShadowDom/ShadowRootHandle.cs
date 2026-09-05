using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace Bit.Butil;

/// <summary>
/// A shadow root, from <see cref="ShadowDom"/>. Queries run against it the way they run against the
/// document - which is the point: from outside, nothing reaches in.
/// </summary>
public sealed class ShadowRootHandle : IAsyncDisposable
{
    private readonly IJSRuntime _js;
    private readonly Dom _dom;
    private bool _released;

    internal ShadowRootHandle(IJSRuntime js, Dom dom, Guid id)
    {
        _js = js;
        _dom = dom;
        Id = id;
    }

    /// <summary>The internal handle id.</summary>
    public Guid Id { get; }

    /// <summary>
    /// The first element inside the shadow root matching a selector.
    /// </summary>
    /// <returns>A handle, or null when nothing matches or the selector is invalid.</returns>
    /// <remarks>
    /// A selector from outside cannot reach in here at all, which is the isolation working. Note
    /// that <see cref="DomHandle.AsElementReference"/> does not resolve for an element inside a
    /// shadow root - Blazor's reference lookup does not pierce shadow boundaries either.
    /// </remarks>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(DomNodeDto))]
    public async ValueTask<DomHandle?> Query(string selector)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(selector);
        return _dom.Wrap(await _js.Invoke<DomNodeDto?>("BitButil.dom.query", Id, selector));
    }

    /// <summary>Every element inside the shadow root matching a selector, as a snapshot.</summary>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(DomNodeDto))]
    public async ValueTask<DomHandle[]> QueryAll(string selector)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(selector);
        return _dom.WrapAll(await _js.Invoke<DomNodeDto[]>("BitButil.dom.queryAll", Id, selector));
    }

    /// <summary>The element this root is attached to - the way back out.</summary>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(DomNodeDto))]
    public async ValueTask<DomHandle?> GetHost() => _dom.Wrap(await _js.Invoke<DomNodeDto?>("BitButil.shadowDom.host", Id));

    /// <summary><c>"open"</c> or <c>"closed"</c> - though a closed root is not something you can get a handle to from outside.</summary>
    public ValueTask<string> GetMode() => _js.Invoke<string>("BitButil.shadowDom.mode", Id);

    /// <summary>The root's contents as HTML text.</summary>
    public ValueTask<string> GetHtml() => _js.Invoke<string>("BitButil.dom.html", Id);

    /// <summary>
    /// Replaces the root's contents with parsed HTML.
    /// </summary>
    /// <remarks>
    /// <b>This is an injection point</b>, exactly as it is on an ordinary element: markup from a
    /// user or a server can carry an <c>onerror</c> handler that runs. Being inside a shadow root
    /// isolates styles, not script. Build the contents with <see cref="Dom.Create"/> and
    /// <see cref="Append"/> for anything you did not write yourself.
    /// </remarks>
    public ValueTask<bool> SetHtml(string html)
    {
        ArgumentNullException.ThrowIfNull(html);
        return _js.Invoke<bool>("BitButil.dom.setHtml", Id, html);
    }

    /// <summary>Appends an element into the root.</summary>
    public ValueTask<bool> Append(DomHandle child)
    {
        ArgumentNullException.ThrowIfNull(child);
        return _js.Invoke<bool>("BitButil.dom.append", Id, child.Id);
    }

    /// <summary>
    /// Adds a stylesheet scoped to this root.
    /// </summary>
    /// <remarks>
    /// This is the isolation people attach a shadow root for: the page's stylesheet does not reach
    /// these elements, and these rules do not escape. Selectors here are written against the root's
    /// own contents, with <c>:host</c> for the element the root is attached to.
    /// </remarks>
    public ValueTask<bool> AddStyle(string css)
    {
        ArgumentNullException.ThrowIfNull(css);
        return _js.Invoke<bool>("BitButil.shadowDom.addStyle", Id, css);
    }

    /// <summary>
    /// Releases the handle. Idempotent, and safe during teardown.
    /// </summary>
    /// <remarks>
    /// The root itself is untouched, and cannot be detached in any case: a shadow root, once
    /// attached, is attached for the life of its host.
    /// </remarks>
    public async ValueTask DisposeAsync()
    {
        if (_released) return;
        _released = true;

        try { await _js.InvokeVoid("BitButil.dom.release", Id); }
        catch (Exception ex) when (ex.IsIgnorableDisposalException()) { } // teardown: circuit gone, cancelled, or already disposed
    }
}
