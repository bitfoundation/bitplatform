using Microsoft.JSInterop;

namespace Bit.Bink.Demo.Client.Shared;

/// <summary>Saves bytes the app produced as a file, for formats the canvas does not export itself.</summary>
public sealed class FileDownloads(IJSRuntime js) : IAsyncDisposable
{
    private IJSObjectReference? _module;

    /// <summary>Offers the bytes to the browser as a download.</summary>
    public async Task SaveAsync(string fileName, byte[] bytes, string mimeType)
    {
        _module ??= await js.InvokeAsync<IJSObjectReference>("import", "./js/files.js");
        await _module.InvokeVoidAsync("download", fileName, bytes, mimeType);
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_module is not null)
        {
            try
            {
                await _module.DisposeAsync();
            }
            catch (JSDisconnectedException)
            {
                // The page is going away; nothing to release.
            }
        }
    }
}
