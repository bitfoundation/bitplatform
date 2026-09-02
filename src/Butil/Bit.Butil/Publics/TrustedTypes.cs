using System;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Bit.Butil;

/// <summary>
/// Wraps the <see href="https://developer.mozilla.org/en-US/docs/Web/API/Trusted_Types_API">Trusted Types API</see>:
/// policies that turn a string into a value the browser will accept at a dangerous sink, and the CSP
/// that makes every other string fail.
/// </summary>
/// <remarks>
/// Trusted Types is the "no unreviewed strings reach <c>innerHTML</c>" rule, enforced by the browser
/// rather than by review. With <c>Content-Security-Policy: require-trusted-types-for 'script'</c> on
/// the response, assigning a plain string to <c>innerHTML</c>, <c>script.src</c> or <c>eval</c>
/// throws; only a value produced by a policy goes through. What remains to audit is the policies,
/// which are few and in one place.
/// <br/>
/// A policy's transform has to run synchronously, and every call back into .NET is asynchronous - so
/// the policies here are declared through <see cref="TrustedTypePolicyOptions"/> rather than written
/// as C# callbacks. Sanitizing is done by the browser's own
/// <see href="https://developer.mozilla.org/en-US/docs/Web/API/HTML_Sanitizer_API">Sanitizer</see>,
/// optionally one you configured through <see cref="Sanitizer.Create"/>.
/// <br/>
/// The trusted value itself never crosses to .NET - it would arrive as its string and lose exactly
/// the type that makes it trusted - so the writing happens here too: <see cref="SetHtml"/> and
/// <see cref="SetScriptSrc"/> create the value and assign it in one call.
/// </remarks>
[ButilService(typeof(TrustedTypes))]
public class TrustedTypes(IJSRuntime js) : IAsyncDisposable
{
    internal const string InvokeMethodName = nameof(InvokeTrustedTypeViolation);

    private readonly ConcurrentDictionary<Guid, Action<TrustedTypeViolation>> _handlers = new();

    // Per-instance callback reference (see Keyboard): listeners are isolated per circuit / WASM app
    // and released on disposal - no static state, no cross-circuit leak.
    private DotNetObjectReference<TrustedTypes>? _dotNetRef;
    private DotNetObjectReference<TrustedTypes> DotNetRef => DotNetObjectReferenceHelper.GetOrCreate(ref _dotNetRef, this);

    /// <summary>True when the runtime exposes <c>window.trustedTypes</c>.</summary>
    /// <remarks>
    /// During prerender/SSR (no JS runtime) this returns <c>default</c> (e.g. <c>false</c>/<c>0</c>)
    /// rather than throwing, so the result can't be distinguished from a genuine value. If you
    /// branch on it, defer the read to <c>OnAfterRenderAsync</c>.
    /// </remarks>
    public ValueTask<bool> IsSupported() => js.Invoke<bool>("BitButil.trustedTypes.isSupported");

    /// <summary>
    /// Whether a CSP is actually enforcing Trusted Types on this document - the difference between
    /// "the API is here" and "plain strings now throw".
    /// </summary>
    /// <remarks>
    /// Nothing in the platform reports this, so it is asked the only way it can be: a plain string is
    /// assigned to a sink on a detached element, which is a no-op when nothing is enforced and a
    /// <c>TypeError</c> when it is. The element is never in the document, so the probe has no effect.
    /// </remarks>
    public ValueTask<bool> IsEnforced() => js.Invoke<bool>("BitButil.trustedTypes.isEnforced");

    /// <summary>
    /// Creates a policy.
    /// </summary>
    /// <param name="name">
    /// The policy's name. It has to be listed in the CSP's <c>trusted-types</c> directive, and a name
    /// can only be created once per document. <c>"default"</c> is special: the browser falls back to
    /// it for any string assigned to a sink without a policy, which is the way to make existing code
    /// keep working under enforcement.
    /// </param>
    /// <param name="options">What the policy allows. Defaults to sanitizing HTML and refusing every script URL.</param>
    /// <param name="sanitizer">
    /// An optional configured sanitizer (see <see cref="Sanitizer.Create"/>) for the policy to
    /// sanitize with, instead of the browser's default configuration.
    /// </param>
    /// <returns>
    /// False when the CSP doesn't list this name, when the name is already taken, or when the runtime
    /// has no Trusted Types - all configuration facts rather than exceptions.
    /// </returns>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(TrustedTypePolicyOptions))]
    public ValueTask<bool> CreatePolicy(string name, TrustedTypePolicyOptions? options = null, SanitizerHandle? sanitizer = null)
        => js.Invoke<bool>("BitButil.trustedTypes.createPolicy", name, options ?? new TrustedTypePolicyOptions(), sanitizer?.Id);

    /// <summary>True when a policy of this name was created through this API.</summary>
    public ValueTask<bool> HasPolicy(string name) => js.Invoke<bool>("BitButil.trustedTypes.hasPolicy", name);

    /// <summary>The names of the policies created through this API.</summary>
    public ValueTask<string[]> GetPolicyNames() => js.Invoke<string[]>("BitButil.trustedTypes.policyNames");

    /// <summary>
    /// Runs markup through a policy and returns the resulting text - for display, logging or a
    /// comparison. The trusted value itself stays in the browser; use <see cref="SetHtml"/> to write it.
    /// </summary>
    /// <returns>Null when there is no such policy, or the policy refused the value.</returns>
    public ValueTask<string?> CreateHtml(string policyName, string html)
        => js.Invoke<string?>("BitButil.trustedTypes.createHtml", policyName, html ?? string.Empty);

    /// <summary>
    /// Runs a script URL through a policy and returns the resulting text.
    /// </summary>
    /// <returns>Null when there is no such policy, or the URL isn't one the policy allows.</returns>
    public ValueTask<string?> CreateScriptUrl(string policyName, string url)
        => js.Invoke<string?>("BitButil.trustedTypes.createScriptUrl", policyName, url ?? string.Empty);

    /// <summary>
    /// Writes markup into an element through a policy - which is the point of having one: under
    /// enforcement this succeeds where assigning a string to <c>innerHTML</c> throws.
    /// </summary>
    /// <returns>False when there is no such policy or the policy refused the markup; the element is left alone.</returns>
    public ValueTask<bool> SetHtml(ElementReference element, string policyName, string html)
        => js.Invoke<bool>("BitButil.trustedTypes.setHtml", element, policyName, html ?? string.Empty);

    /// <summary>
    /// Sets a <c>&lt;script&gt;</c> element's <c>src</c> through a policy.
    /// </summary>
    /// <returns>False when there is no such policy, or the URL isn't one the policy allows.</returns>
    public ValueTask<bool> SetScriptSrc(ElementReference scriptElement, string policyName, string url)
        => js.Invoke<bool>("BitButil.trustedTypes.setScriptSrc", scriptElement, policyName, url ?? string.Empty);

    /// <summary>
    /// Invoked from JS on each Trusted Types violation. Public + <see cref="JSInvokableAttribute"/> so
    /// it can be dispatched through the per-instance <see cref="DotNetObjectReference{T}"/>.
    /// </summary>
    [JSInvokable(InvokeMethodName)]
    public void InvokeTrustedTypeViolation(Guid id, TrustedTypeViolation violation)
    {
        if (_handlers.TryGetValue(id, out var handler)) handler.Invoke(violation);
    }

    /// <summary>
    /// Reports every Trusted Types violation on the document - the sinks still being written to as
    /// plain strings. Dispose the returned subscription to stop.
    /// </summary>
    /// <remarks>
    /// The point of a report-only rollout: serve
    /// <c>Content-Security-Policy-Report-Only: require-trusted-types-for 'script'</c>, subscribe here,
    /// and every violation is a call site to fix before switching to enforcement.
    /// </remarks>
    [DynamicDependency(nameof(InvokeTrustedTypeViolation), typeof(TrustedTypes))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(TrustedTypeViolation))]
    public async ValueTask<ButilSubscription> OnViolation(Action<TrustedTypeViolation> handler)
    {
        ArgumentNullException.ThrowIfNull(handler);

        var id = Guid.NewGuid();
        _handlers[id] = handler;
        await js.InvokeVoid("BitButil.trustedTypes.onViolation", DotNetRef, InvokeMethodName, id);

        return new ButilSubscription(id, async () =>
        {
            _handlers.TryRemove(id, out _);
            await js.InvokeVoid("BitButil.trustedTypes.offViolation", id);
        });
    }

    /// <summary>
    /// On scope/circuit teardown, detaches any violation listener whose
    /// <see cref="ButilSubscription"/> was never disposed.
    /// </summary>
    /// <remarks>
    /// The policies themselves are deliberately left in place: the browser refuses to create a policy
    /// name twice, so dropping them would leave a re-created scope unable to get them back.
    /// </remarks>
    public async ValueTask DisposeAsync()
    {
        try
        {
            _handlers.Clear();
            await js.InvokeVoid("BitButil.trustedTypes.disposeAll");
        }
        catch (Exception ex) when (ex.IsIgnorableDisposalException()) { } // teardown: circuit gone, cancelled, or already disposed
        finally
        {
            _dotNetRef?.Dispose();
            _dotNetRef = null;
        }
        GC.SuppressFinalize(this);
    }
}
