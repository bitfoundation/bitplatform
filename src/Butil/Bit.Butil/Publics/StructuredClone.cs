using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.JSInterop;
using static Bit.Butil.LinkerFlags;

namespace Bit.Butil;

/// <summary>
/// Wraps <see href="https://developer.mozilla.org/en-US/docs/Web/API/Window/structuredClone">structuredClone()</see>:
/// the browser's own deep-copy algorithm, and the one that decides what <c>postMessage</c>,
/// IndexedDB and <c>history.pushState</c> will accept.
/// </summary>
/// <remarks>
/// Two things this is genuinely for, and one it is not.
/// <br/>
/// It is for <see cref="CanClone"/>: before handing a payload to <see cref="BroadcastChannel"/>,
/// <see cref="IndexedDb"/> or <see cref="History"/>, this answers whether the browser will take it,
/// instead of finding out from a <c>DataCloneError</c> at the far end. And it is for
/// <see cref="Clone"/> as a deep copy with no <c>ICloneable</c> to implement and no copy constructor
/// to keep in sync - the value goes out and a detached copy comes back.
/// <br/>
/// It is not a way to smuggle types past the interop boundary. Every value crossing that boundary is
/// marshalled as JSON in both directions, so what the browser clones is the marshalled shape: a
/// <c>DateTime</c> arrives as a string and comes back a string, a cyclic graph never makes it out of
/// .NET at all, and <c>Map</c>, <c>Set</c>, <c>ArrayBuffer</c> and the rest of the types structured
/// clone preserves have no representation to preserve here. Within those limits - plain data - the
/// round trip is faithful.
/// </remarks>
[ButilService(typeof(StructuredClone))]
public class StructuredClone(IJSRuntime js)
{
    /// <summary>True when the runtime exposes <c>structuredClone</c>.</summary>
    /// <remarks>
    /// During prerender/SSR (no JS runtime) this returns <c>default</c> (e.g. <c>false</c>/<c>0</c>)
    /// rather than throwing, so the result can't be distinguished from a genuine value. If you
    /// branch on it, defer the read to <c>OnAfterRenderAsync</c>.
    /// </remarks>
    public ValueTask<bool> IsSupported() => js.Invoke<bool>("BitButil.structuredClone.isSupported");

    /// <summary>
    /// Deep-copies a value through the browser's structured clone algorithm.
    /// </summary>
    /// <param name="value">The value to clone. Marshalled as JSON on the way out and back - see the remarks on <see cref="StructuredClone"/>.</param>
    /// <returns>The clone, or <c>default</c> when the runtime has no <c>structuredClone</c> or the value isn't cloneable.</returns>
    public ValueTask<T?> Clone<[DynamicallyAccessedMembers(JsonSerialized)] T>(T value)
        => js.Invoke<T?>("BitButil.structuredClone.clone", value);

    /// <summary>
    /// Whether the browser's structured clone algorithm accepts this value - the same test that
    /// decides whether <c>postMessage</c>, IndexedDB and <c>history.pushState</c> will take it.
    /// </summary>
    /// <remarks>
    /// The value is tested in the shape it arrives in after marshalling, which is the shape those
    /// APIs would be handed by a Butil call - so a false here is a real answer about your payload,
    /// not an artefact of the boundary.
    /// </remarks>
    public ValueTask<bool> CanClone<[DynamicallyAccessedMembers(JsonSerialized)] T>(T value)
        => js.Invoke<bool>("BitButil.structuredClone.canClone", value);
}
