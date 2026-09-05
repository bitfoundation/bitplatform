using System;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Bit.Butil;

/// <summary>
/// Wraps the <see href="https://developer.mozilla.org/en-US/docs/Web/API/HTML_Sanitizer_API">HTML Sanitizer API</see>:
/// the browser's own XSS filter, configurable and reusable.
/// </summary>
/// <remarks>
/// Sanitizing already happens inside <c>ElementReferenceExtensions.SetHtml</c>, but implicitly - with
/// the browser's default configuration, on one element, and with no way to see what it did. This is
/// the same filter as a thing of its own: sanitize to a string before storing it, keep a
/// configuration that says exactly which elements a comment field allows, and reuse it across every
/// comment rather than rebuilding it per call.
/// <br/>
/// The parser doing the work is the browser's, which is the point: a hand-written filter in C# has to
/// predict how the browser will parse mangled markup, and that is precisely where filters are
/// defeated. What is left after sanitizing is what that browser would have parsed.
/// <br/>
/// Support is recent (Chromium first, others following), so <see cref="IsSupported"/> is not a
/// formality here - <see cref="Sanitize"/> returns null rather than unsanitized markup when the API
/// is missing, so an unsupported browser can never be handed the input untouched.
/// </remarks>
[ButilService(typeof(Sanitizer))]
public class Sanitizer(IJSRuntime js) : IAsyncDisposable
{
    /// <summary>True when the runtime exposes the configurable <c>Sanitizer</c> constructor.</summary>
    /// <remarks>
    /// During prerender/SSR (no JS runtime) this returns <c>default</c> (e.g. <c>false</c>/<c>0</c>)
    /// rather than throwing, so the result can't be distinguished from a genuine value. If you
    /// branch on it, defer the read to <c>OnAfterRenderAsync</c>.
    /// </remarks>
    public ValueTask<bool> IsSupported() => js.Invoke<bool>("BitButil.sanitizer.isSupported");

    /// <summary>
    /// True when the runtime exposes the sanitizing sink <c>Element.setHTML</c>, which is what
    /// sanitizing without a configuration needs. A browser can have this without having the
    /// configurable <c>Sanitizer</c> object.
    /// </summary>
    public ValueTask<bool> IsSetHtmlSupported() => js.Invoke<bool>("BitButil.sanitizer.isSetHtmlSupported");

    /// <summary>
    /// Sanitizes markup with the browser's default configuration and returns the result.
    /// </summary>
    /// <param name="html">The untrusted markup.</param>
    /// <returns>
    /// The sanitized markup, or null when the runtime has no sanitizing sink - never the input
    /// unchanged.
    /// </returns>
    /// <remarks>
    /// The markup is parsed into a detached element that is never in the document, so nothing in it
    /// loads, runs or is announced on the way through.
    /// </remarks>
    public ValueTask<string?> Sanitize(string html)
        => js.Invoke<string?>("BitButil.sanitizer.sanitize", html ?? string.Empty, null);

    /// <summary>
    /// Sanitizes markup straight into an element - the same thing <c>SetHtml</c> does, with an
    /// explicit configuration when you create one through <see cref="Create"/>.
    /// </summary>
    /// <param name="element">The element to fill.</param>
    /// <param name="html">The untrusted markup.</param>
    /// <returns>False when the runtime has no sanitizing sink; the element is left alone in that case.</returns>
    public ValueTask<bool> SanitizeInto(ElementReference element, string html)
        => js.Invoke<bool>("BitButil.sanitizer.sanitizeInto", element, html ?? string.Empty, null);

    /// <summary>
    /// Builds a reusable sanitizer from a configuration.
    /// </summary>
    /// <param name="config">Which elements and attributes to allow or remove.</param>
    /// <returns>
    /// A handle to sanitize with, or null when the runtime has no configurable <c>Sanitizer</c>, the
    /// configuration is invalid (an allow-list and a deny-list for the same thing, for instance), or
    /// the runtime is too old to understand it. Never a handle backed by a sanitizer the
    /// configuration didn't reach - that would let through exactly what the allow-list excludes.
    /// </returns>
    /// <remarks>
    /// Worth creating once and keeping: building a sanitizer is the expensive half of sanitizing one
    /// short fragment, so a list of a hundred comments should share a single handle. Dispose it when
    /// the component goes away.
    /// </remarks>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(SanitizerConfig))]
    public async ValueTask<SanitizerHandle?> Create(SanitizerConfig config)
    {
        ArgumentNullException.ThrowIfNull(config);

        var id = Guid.NewGuid();
        var created = await js.Invoke<bool>("BitButil.sanitizer.create", id, config);
        return created ? new SanitizerHandle(js, id) : null;
    }

    /// <summary>
    /// The browser's default configuration, expanded: every element and attribute its baseline
    /// allows. Null when the runtime has no configurable <c>Sanitizer</c>.
    /// </summary>
    /// <remarks>
    /// Returned as raw JSON rather than a <see cref="SanitizerConfig"/> because what comes back is
    /// not the shape a configuration goes in as - each entry is an object carrying a name and a
    /// namespace, and the list is long. Read it to answer "is this element really allowed"; don't
    /// try to feed it back in.
    /// </remarks>
    public ValueTask<JsonElement?> GetDefaultConfig()
        => js.Invoke<JsonElement?>("BitButil.sanitizer.getConfig", [null]);

    /// <summary>
    /// On scope/circuit teardown, drops any sanitizers whose <see cref="SanitizerHandle"/> was never
    /// disposed.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        try
        {
            await js.InvokeVoid("BitButil.sanitizer.disposeAll");
        }
        catch (Exception ex) when (ex.IsIgnorableDisposalException()) { } // teardown: circuit gone, cancelled, or already disposed

        GC.SuppressFinalize(this);
    }
}
