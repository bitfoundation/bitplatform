using System;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Bit.Butil;

/// <summary>
/// A configured, reusable
/// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Sanitizer">Sanitizer</see> created by
/// <see cref="Bit.Butil.Sanitizer.Create"/>.
/// </summary>
/// <remarks>
/// Build one per policy - "what a comment may contain", "what a rich-text note may contain" - and
/// reuse it for every fragment under that policy: constructing the sanitizer is the expensive part,
/// sanitizing a short fragment with it is not.
/// </remarks>
public sealed class SanitizerHandle : IAsyncDisposable
{
    private readonly IJSRuntime _js;
    private readonly Guid _id;
    private bool _disposed;

    internal SanitizerHandle(IJSRuntime js, Guid id) { _js = js; _id = id; }

    /// <summary>The internal sanitizer id.</summary>
    public Guid Id => _id;

    /// <summary>
    /// Sanitizes markup under this configuration and returns the result.
    /// </summary>
    /// <returns>The sanitized markup, or null when the sanitizer is gone - never the input unchanged.</returns>
    public ValueTask<string?> Sanitize(string html)
        => _js.Invoke<string?>("BitButil.sanitizer.sanitize", html ?? string.Empty, _id);

    /// <summary>
    /// Sanitizes markup straight into an element under this configuration.
    /// </summary>
    /// <returns>False when the sanitizer is gone or the runtime has no sanitizing sink; the element is left alone.</returns>
    public ValueTask<bool> SanitizeInto(ElementReference element, string html)
        => _js.Invoke<bool>("BitButil.sanitizer.sanitizeInto", element, html ?? string.Empty, _id);

    /// <summary>
    /// This configuration as the browser expanded it - the answer to "is that element really
    /// allowed", which the configuration you passed in doesn't give on its own.
    /// </summary>
    /// <remarks>Raw JSON: what comes back is not the shape a configuration goes in as. See <see cref="Bit.Butil.Sanitizer.GetDefaultConfig"/>.</remarks>
    public ValueTask<JsonElement?> GetConfig() => _js.Invoke<JsonElement?>("BitButil.sanitizer.getConfig", _id);

    /// <summary>Drops the sanitizer. Calling it again does nothing.</summary>
    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;
        try { await _js.InvokeVoid("BitButil.sanitizer.dispose", _id); }
        catch (Exception ex) when (ex.IsIgnorableDisposalException()) { } // teardown: circuit gone, cancelled, or already disposed
    }
}
