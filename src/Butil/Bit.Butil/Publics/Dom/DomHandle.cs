using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Bit.Butil;

/// <summary>
/// A handle to an element Blazor did not render, from <see cref="Dom"/>.
/// </summary>
/// <remarks>
/// The handle and the element are different things. Releasing the handle
/// (<see cref="DisposeAsync"/>) forgets the element; <see cref="Remove"/> takes the element off the
/// page. A handle to an element that has been removed keeps working - it is simply no longer
/// <see cref="IsConnected"/>.
/// <br/>
/// For anything beyond finding, creating and placing, use <see cref="AsElementReference"/> and the
/// element extensions: classes, ARIA, styles, scrolling, events and the rest are all already there.
/// </remarks>
public sealed class DomHandle : IAsyncDisposable
{
    private readonly IJSRuntime _js;
    private readonly Dom _owner;
    private bool _released;

    internal DomHandle(IJSRuntime js, Dom owner, Guid id, string tagName)
    {
        _js = js;
        _owner = owner;
        Id = id;
        TagName = tagName;
    }

    /// <summary>The internal handle id.</summary>
    public Guid Id { get; }

    /// <summary>The element's tag name in lower case, as it was when the handle was made.</summary>
    public string TagName { get; }

    /// <summary>
    /// An <see cref="ElementReference"/> for this element, so every element extension in Butil -
    /// classes, ARIA, styles, scrolling, layout, events - works on it.
    /// </summary>
    /// <returns>A reference, or null when the element cannot carry one.</returns>
    /// <remarks>
    /// This works by stamping the attribute Blazor's own reference lookup searches for, which is the
    /// one place Butil leans on a Blazor internal. It is covered by a test, so a future change to
    /// that convention shows up here rather than in your app.
    /// <br/>
    /// Two consequences: the element has to be <em>in the document</em> for the lookup to find it -
    /// append it first - and the lookup does not pierce shadow roots, so a reference to an element
    /// inside one will not resolve.
    /// <br/>
    /// The attribute is stamped once per element, so calling this repeatedly returns the same
    /// reference rather than accumulating attributes on the element.
    /// </remarks>
    public async ValueTask<ElementReference?> AsElementReference()
    {
        var referenceId = await _js.Invoke<string?>("BitButil.dom.elementReferenceId", Id);
        if (referenceId is null) return null;

        // The context is not decoration: the element extensions reach the JS runtime through it, so
        // a reference built without one throws the moment it is used rather than when it is made.
        return new ElementReference(referenceId, _owner.ReferenceContext);
    }

    /// <summary>The first descendant matching a selector. Null when nothing matches or the selector is invalid.</summary>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(DomNodeDto))]
    public async ValueTask<DomHandle?> Query(string selector)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(selector);
        return _owner.Wrap(await _js.Invoke<DomNodeDto?>("BitButil.dom.query", Id, selector));
    }

    /// <summary>Every descendant matching a selector, as a snapshot.</summary>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(DomNodeDto))]
    public async ValueTask<DomHandle[]> QueryAll(string selector)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(selector);
        return _owner.WrapAll(await _js.Invoke<DomNodeDto[]>("BitButil.dom.queryAll", Id, selector));
    }

    /// <summary>The containing element, or null at the root.</summary>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(DomNodeDto))]
    public async ValueTask<DomHandle?> GetParent() => _owner.Wrap(await _js.Invoke<DomNodeDto?>("BitButil.dom.parent", Id));

    /// <summary>The child <em>elements</em> - text and comment nodes are not included.</summary>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(DomNodeDto))]
    public async ValueTask<DomHandle[]> GetChildren() => _owner.WrapAll(await _js.Invoke<DomNodeDto[]>("BitButil.dom.children", Id));

    /// <summary>The first child element, or null.</summary>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(DomNodeDto))]
    public async ValueTask<DomHandle?> GetFirstChild() => _owner.Wrap(await _js.Invoke<DomNodeDto?>("BitButil.dom.firstChild", Id));

    /// <summary>The last child element, or null.</summary>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(DomNodeDto))]
    public async ValueTask<DomHandle?> GetLastChild() => _owner.Wrap(await _js.Invoke<DomNodeDto?>("BitButil.dom.lastChild", Id));

    /// <summary>
    /// The next sibling element, or null. Whitespace between two elements is a text node, and this
    /// skips it - which is why raw node traversal so rarely does what you meant.
    /// </summary>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(DomNodeDto))]
    public async ValueTask<DomHandle?> GetNextSibling() => _owner.Wrap(await _js.Invoke<DomNodeDto?>("BitButil.dom.nextSibling", Id));

    /// <summary>The previous sibling element, or null.</summary>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(DomNodeDto))]
    public async ValueTask<DomHandle?> GetPreviousSibling() => _owner.Wrap(await _js.Invoke<DomNodeDto?>("BitButil.dom.previousSibling", Id));

    /// <summary>The nearest ancestor matching a selector, this element included. Null when none does.</summary>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(DomNodeDto))]
    public async ValueTask<DomHandle?> Closest(string selector)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(selector);
        return _owner.Wrap(await _js.Invoke<DomNodeDto?>("BitButil.dom.closest", Id, selector));
    }

    /// <summary>Whether this element matches a selector.</summary>
    public ValueTask<bool> Matches(string selector)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(selector);
        return _js.Invoke<bool>("BitButil.dom.matches", Id, selector);
    }

    /// <summary>
    /// Appends <paramref name="child"/> as this element's last child. Moving an element that is
    /// already on the page moves it - a node is in one place at a time.
    /// </summary>
    public ValueTask<bool> Append(DomHandle child)
    {
        ArgumentNullException.ThrowIfNull(child);
        return _js.Invoke<bool>("BitButil.dom.append", Id, child.Id);
    }

    /// <summary>Inserts <paramref name="child"/> as this element's first child.</summary>
    public ValueTask<bool> Prepend(DomHandle child)
    {
        ArgumentNullException.ThrowIfNull(child);
        return _js.Invoke<bool>("BitButil.dom.prepend", Id, child.Id);
    }

    /// <summary>Inserts <paramref name="sibling"/> immediately before this element.</summary>
    public ValueTask<bool> InsertBefore(DomHandle sibling)
    {
        ArgumentNullException.ThrowIfNull(sibling);
        return _js.Invoke<bool>("BitButil.dom.insertBefore", Id, sibling.Id);
    }

    /// <summary>
    /// Appends this element into one of your own rendered elements.
    /// </summary>
    /// <remarks>
    /// Careful: Blazor owns the children of an element it rendered, and its next diff of that
    /// element may remove what you put there. Append into a container your markup leaves empty and
    /// never re-renders.
    /// </remarks>
    public ValueTask<bool> AppendTo(ElementReference parent) => _js.Invoke<bool>("BitButil.dom.appendTo", parent, Id);

    /// <summary>Takes the element off the page. The handle keeps working on the detached element.</summary>
    public ValueTask<bool> Remove() => _js.Invoke<bool>("BitButil.dom.remove", Id);

    /// <summary>The element's text, with the text of everything inside it.</summary>
    public ValueTask<string> GetText() => _js.Invoke<string>("BitButil.dom.text", Id);

    /// <summary>
    /// Replaces everything inside with this text. Safe with any input - markup in it becomes visible
    /// characters rather than elements.
    /// </summary>
    public ValueTask<bool> SetText(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return _js.Invoke<bool>("BitButil.dom.setText", Id, value);
    }

    /// <summary>The element's inner HTML as text.</summary>
    public ValueTask<string> GetHtml() => _js.Invoke<string>("BitButil.dom.html", Id);

    /// <summary>
    /// Replaces everything inside with parsed HTML.
    /// </summary>
    /// <remarks>
    /// <b>This is an injection point.</b> Anything in <paramref name="html"/> that came from a user,
    /// a URL or a server response can carry script - not a <c>&lt;script&gt;</c> tag, which this
    /// will not run, but an <c>onerror</c> on an <c>&lt;img&gt;</c>, which it will. Use
    /// <see cref="SetText"/> for anything you did not write yourself, and reach for this only when
    /// the markup is yours.
    /// </remarks>
    public ValueTask<bool> SetHtml(string html)
    {
        ArgumentNullException.ThrowIfNull(html);
        return _js.Invoke<bool>("BitButil.dom.setHtml", Id, html);
    }

    /// <summary>An attribute's value, or null when it is not set.</summary>
    public ValueTask<string?> GetAttribute(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        return _js.Invoke<string?>("BitButil.dom.attribute", Id, name);
    }

    /// <summary>Sets an attribute. False when the name is not a valid attribute name.</summary>
    public ValueTask<bool> SetAttribute(string name, string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(value);
        return _js.Invoke<bool>("BitButil.dom.setAttribute", Id, name, value);
    }

    /// <summary>Removes an attribute. Removing one that is not there is not an error.</summary>
    public ValueTask<bool> RemoveAttribute(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        return _js.Invoke<bool>("BitButil.dom.removeAttribute", Id, name);
    }

    /// <summary>The tag name as it is now, in lower case.</summary>
    public ValueTask<string> GetTagName() => _js.Invoke<string>("BitButil.dom.tagName", Id);

    /// <summary>
    /// Whether the element is still in the document. False for one that was created and never
    /// appended, and for one that has since been removed.
    /// </summary>
    public ValueTask<bool> IsConnected() => _js.Invoke<bool>("BitButil.dom.isConnected", Id);

    /// <summary>
    /// Releases the handle. Idempotent, and safe during teardown.
    /// </summary>
    /// <remarks>
    /// The element is untouched - this forgets it, it does not remove it. Call <see cref="Remove"/>
    /// first if taking it off the page is what you meant.
    /// </remarks>
    public async ValueTask DisposeAsync()
    {
        if (_released) return;
        _released = true;

        try { await _js.InvokeVoid("BitButil.dom.release", Id); }
        catch (Exception ex) when (ex.IsIgnorableDisposalException()) { } // teardown: circuit gone, cancelled, or already disposed
    }
}
