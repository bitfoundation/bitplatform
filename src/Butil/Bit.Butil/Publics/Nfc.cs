using System;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace Bit.Butil;

/// <summary>
/// Wraps the <see href="https://developer.mozilla.org/en-US/docs/Web/API/Web_NFC_API">Web NFC API</see>
/// (<c>NDEFReader</c>). Available on Chromium for Android only.
/// </summary>
public class Nfc(IJSRuntime js) : IAsyncDisposable
{
    internal const string ReadingMethodName = nameof(InvokeNdefReading);
    internal const string ErrorMethodName = nameof(InvokeNdefError);

    private readonly ConcurrentDictionary<Guid, Listener> _listeners = new();

    // Per-instance callback reference (see Keyboard): scans are isolated per circuit / WASM app
    // and released on disposal - no static state, no cross-circuit leak.
    private DotNetObjectReference<Nfc>? _dotNetRef;
    private DotNetObjectReference<Nfc> DotNetRef => DotNetObjectReferenceHelper.GetOrCreate(ref _dotNetRef, this);

    /// <summary>True when the runtime exposes <c>NDEFReader</c>.</summary>
    /// <remarks>
    /// During prerender/SSR (no JS runtime) this returns <c>default</c> (e.g. <c>false</c>/<c>0</c>)
    /// rather than throwing, so the result can't be distinguished from a genuine value. If you
    /// branch on it, defer the read to <c>OnAfterRenderAsync</c>.
    /// </remarks>
    public ValueTask<bool> IsSupported() => js.Invoke<bool>("BitButil.nfc.isSupported");

    /// <summary>
    /// Invoked from JS when a tag is read. Public + <see cref="JSInvokableAttribute"/> so it can be
    /// dispatched through the per-instance <see cref="DotNetObjectReference{T}"/>.
    /// </summary>
    [JSInvokable(ReadingMethodName)]
    public void InvokeNdefReading(Guid id, NdefMessage message)
    {
        if (_listeners.TryGetValue(id, out var l)) l.OnReading?.Invoke(message);
    }

    /// <summary>Invoked from JS on a scan/read error. See <see cref="InvokeNdefReading"/>.</summary>
    [JSInvokable(ErrorMethodName)]
    public void InvokeNdefError(Guid id, string message)
    {
        if (_listeners.TryGetValue(id, out var l)) l.OnError?.Invoke(message);
    }

    /// <summary>
    /// Starts scanning for NDEF tags. Use the returned <see cref="IAsyncDisposable"/> to stop.
    /// </summary>
    [DynamicDependency(nameof(InvokeNdefReading), typeof(Nfc))]
    [DynamicDependency(nameof(InvokeNdefError), typeof(Nfc))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(NdefMessage))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(NdefRecord))]
    public async Task<IAsyncDisposable> Scan(Action<NdefMessage>? onReading, Action<string>? onError = null)
    {
        if (onReading is null && onError is null)
            throw new ArgumentException("At least one of onReading/onError must be provided.");

        var id = Guid.NewGuid();
        _listeners.TryAdd(id, new Listener { OnReading = onReading, OnError = onError });

        await js.InvokeVoid("BitButil.nfc.scan", id, DotNetRef);

        return new ScanHandle(this, js, id);
    }

    /// <summary>
    /// Writes a single NDEF text record to the next tag tapped against the device.
    /// </summary>
    /// <remarks>
    /// During prerender/SSR (no JS runtime) this returns <c>default</c> (e.g. <c>false</c>/<c>0</c>)
    /// rather than throwing, so the result can't be distinguished from a genuine value. If you
    /// branch on it, defer the read to <c>OnAfterRenderAsync</c>.
    /// </remarks>
    public ValueTask<bool> WriteText(string text, string? lang = null, string? id = null)
        => js.Invoke<bool>("BitButil.nfc.writeText", text, lang, id);

    /// <summary>
    /// Writes a single NDEF URL record to the next tag tapped against the device.
    /// </summary>
    /// <remarks>
    /// During prerender/SSR (no JS runtime) this returns <c>default</c> (e.g. <c>false</c>/<c>0</c>)
    /// rather than throwing, so the result can't be distinguished from a genuine value. If you
    /// branch on it, defer the read to <c>OnAfterRenderAsync</c>.
    /// </remarks>
    public ValueTask<bool> WriteUrl(string url, string? id = null)
        => js.Invoke<bool>("BitButil.nfc.writeUrl", url, id);

    public async ValueTask DisposeAsync()
    {
        try
        {
            var ids = _listeners.Keys.ToArray();
            _listeners.Clear();
            foreach (var id in ids)
            {
                await js.InvokeVoid("BitButil.nfc.stop", id);
            }
        }
        catch (Exception ex) when (ex.IsIgnorableDisposalException()) { } // teardown: circuit gone, cancelled, or already disposed
        finally
        {
            _dotNetRef?.Dispose();
            _dotNetRef = null;
        }
        GC.SuppressFinalize(this);
    }

    private class Listener
    {
        public Action<NdefMessage>? OnReading { get; set; }
        public Action<string>? OnError { get; set; }
    }

    private sealed class ScanHandle(Nfc owner, IJSRuntime js, Guid id) : IAsyncDisposable
    {
        private bool _disposed;

        public async ValueTask DisposeAsync()
        {
            if (_disposed) return;
            _disposed = true;
            owner._listeners.TryRemove(id, out _);
            try { await js.InvokeVoid("BitButil.nfc.stop", id); }
            catch (Exception ex) when (ex.IsIgnorableDisposalException()) { } // teardown: circuit gone, cancelled, or already disposed
        }
    }
}
