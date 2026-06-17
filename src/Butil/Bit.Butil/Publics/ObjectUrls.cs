using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace Bit.Butil;

/// <summary>
/// Wraps <c>URL.createObjectURL</c> / <c>URL.revokeObjectURL</c> for arbitrary byte payloads.
/// </summary>
/// <remarks>
/// Object URLs leak memory if not revoked. The instance tracks every URL it creates so
/// disposal automatically revokes outstanding ones. Call <see cref="Create(byte[], string, bool)"/>
/// with <c>track: false</c> when you want the URL to outlive disposal and call <see cref="Revoke"/> yourself.
/// </remarks>
public class ObjectUrls(IJSRuntime js) : IAsyncDisposable
{
    private readonly ConcurrentBag<string> _tracked = new();

    /// <summary>Creates an object URL from raw bytes plus an optional MIME type.</summary>
    public async ValueTask<string> Create(byte[] data, string mimeType = "application/octet-stream", bool track = true)
    {
        var url = await js.Invoke<string>("BitButil.objectUrls.create", data, mimeType);
        if (track) _tracked.Add(url);
        return url;
    }

    /// <summary>Revokes a previously created object URL.</summary>
    public ValueTask Revoke(string objectUrl) => js.InvokeVoid("BitButil.objectUrls.revoke", objectUrl);

    public async ValueTask DisposeAsync()
    {
        try
        {
            while (_tracked.TryTake(out var url))
            {
                await Revoke(url);
            }
        }
        catch (Exception ex) when (ex.IsIgnorableDisposalException()) { } // teardown: circuit gone, cancelled, or already disposed
        GC.SuppressFinalize(this);
    }
}
