using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Bit.Butil;

/// <summary>
/// Wraps <see href="https://developer.mozilla.org/en-US/docs/Web/API/Element/attachShadow">attachShadow</see>
/// and the shadow roots it produces: a subtree with its own styles that the page's CSS does not
/// reach into and whose own CSS does not leak out.
/// </summary>
/// <remarks>
/// Two reasons to be here. One is <em>interoperating</em> with third-party web components, whose
/// real markup lives inside a shadow root where an ordinary <c>querySelector</c> cannot see it -
/// <see cref="GetShadowRoot(ElementReference)"/> is the way in. The other is <em>building</em> one:
/// somewhere to put markup whose styles cannot be broken by the host page, which is the only
/// reliable form of style isolation the platform has.
/// <br/>
/// A <b>closed</b> root is closed to your own page too - the element reports no root at all, and
/// there is no way back in. That is what closing it means, and why open is the default here.
/// </remarks>
[ButilService(typeof(ShadowDom))]
public class ShadowDom(IJSRuntime js, Dom dom)
{
    /// <summary>True when the runtime exposes <c>attachShadow</c>, which is everywhere current.</summary>
    /// <remarks>
    /// During prerender/SSR (no JS runtime) this returns <c>default</c> (<c>false</c>) rather than
    /// throwing, so the result can't be distinguished from a genuine value.
    /// </remarks>
    public ValueTask<bool> IsSupported() => js.Invoke<bool>("BitButil.shadowDom.isSupported");

    /// <summary>
    /// Attaches a shadow root to one of your own rendered elements.
    /// </summary>
    /// <param name="host">The element to attach to.</param>
    /// <param name="open">
    /// True (the default) to keep the root reachable through <see cref="GetShadowRoot(ElementReference)"/>.
    /// False closes it - to your own code as much as anyone else's, permanently.
    /// </param>
    /// <param name="delegatesFocus">
    /// True to make focusing the host focus the first focusable thing inside it, which is what a
    /// custom input control wants.
    /// </param>
    /// <returns>
    /// A handle, or null when the element already has a root or is one of the many that cannot have
    /// one at all - <c>&lt;input&gt;</c>, <c>&lt;img&gt;</c>, <c>&lt;textarea&gt;</c> and the rest.
    /// </returns>
    /// <remarks>
    /// Blazor renders into the host's light DOM, and what is in the light DOM of a shadow host is
    /// not displayed unless the shadow root has a <c>&lt;slot&gt;</c> for it. Attach to a container
    /// your markup leaves empty.
    /// </remarks>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(DomNodeDto))]
    public async ValueTask<ShadowRootHandle?> AttachShadow(ElementReference host, bool open = true, bool delegatesFocus = false)
        => Wrap(await js.Invoke<DomNodeDto?>("BitButil.shadowDom.attachToElement", host, open, delegatesFocus));

    /// <summary>
    /// Attaches a shadow root to an element found or created through <see cref="Dom"/>.
    /// </summary>
    /// <returns>A handle, or null when the element already has a root or cannot have one.</returns>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(DomNodeDto))]
    public async ValueTask<ShadowRootHandle?> AttachShadow(DomHandle host, bool open = true, bool delegatesFocus = false)
    {
        ArgumentNullException.ThrowIfNull(host);
        return Wrap(await js.Invoke<DomNodeDto?>("BitButil.shadowDom.attachToHandle", host.Id, open, delegatesFocus));
    }

    /// <summary>
    /// The shadow root already on one of your own rendered elements.
    /// </summary>
    /// <returns>A handle, or null when there is no root - or when it is closed, which is indistinguishable from there being none.</returns>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(DomNodeDto))]
    public async ValueTask<ShadowRootHandle?> GetShadowRoot(ElementReference host)
        => Wrap(await js.Invoke<DomNodeDto?>("BitButil.shadowDom.fromElement", host));

    /// <summary>
    /// The shadow root already on an element found through <see cref="Dom"/> - the way into a
    /// third-party web component's real markup.
    /// </summary>
    /// <returns>A handle, or null when there is no open root.</returns>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(DomNodeDto))]
    public async ValueTask<ShadowRootHandle?> GetShadowRoot(DomHandle host)
    {
        ArgumentNullException.ThrowIfNull(host);
        return Wrap(await js.Invoke<DomNodeDto?>("BitButil.shadowDom.fromHandle", host.Id));
    }

    private ShadowRootHandle? Wrap(DomNodeDto? node) => node is null ? null : new ShadowRootHandle(js, dom, node.Id);
}
