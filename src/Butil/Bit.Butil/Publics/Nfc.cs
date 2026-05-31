using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace Bit.Butil;

/// <summary>
/// Wraps the <see href="https://developer.mozilla.org/en-US/docs/Web/API/Web_NFC_API">Web NFC API</see>
/// (<c>NDEFReader</c>). Available on Chromium for Android only.
/// </summary>
public class Nfc(IJSRuntime js)
{
    /// <summary>True when the runtime exposes <c>NDEFReader</c>.</summary>
    public ValueTask<bool> IsSupported() => js.Invoke<bool>("BitButil.nfc.isSupported");

    /// <summary>
    /// Starts scanning for NDEF tags. Use the returned <see cref="IAsyncDisposable"/> to stop.
    /// </summary>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(NdefMessage))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(NdefRecord))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(NdefListenersManager))]
    public async Task<IAsyncDisposable> Scan(Action<NdefMessage>? onReading, Action<string>? onError = null)
    {
        if (onReading is null && onError is null)
            throw new ArgumentException("At least one of onReading/onError must be provided.");

        var listener = new NdefListenersManager.Listener { OnReading = onReading, OnError = onError };
        var id = NdefListenersManager.Add(listener);

        await js.InvokeVoid("BitButil.nfc.scan",
            id,
            NdefListenersManager.ReadingMethodName,
            NdefListenersManager.ErrorMethodName);

        return new ScanHandle(js, id);
    }

    /// <summary>
    /// Writes a single NDEF text record to the next tag tapped against the device.
    /// </summary>
    public ValueTask<bool> WriteText(string text, string? lang = null, string? id = null)
        => js.Invoke<bool>("BitButil.nfc.writeText", text, lang, id);

    /// <summary>
    /// Writes a single NDEF URL record to the next tag tapped against the device.
    /// </summary>
    public ValueTask<bool> WriteUrl(string url, string? id = null)
        => js.Invoke<bool>("BitButil.nfc.writeUrl", url, id);

    private sealed class ScanHandle(IJSRuntime js, Guid id) : IAsyncDisposable
    {
        private bool _disposed;

        public async ValueTask DisposeAsync()
        {
            if (_disposed) return;
            _disposed = true;
            NdefListenersManager.Remove(id);
            try { await js.InvokeVoid("BitButil.nfc.stop", id); }
            catch (JSDisconnectedException) { }
        }
    }
}
