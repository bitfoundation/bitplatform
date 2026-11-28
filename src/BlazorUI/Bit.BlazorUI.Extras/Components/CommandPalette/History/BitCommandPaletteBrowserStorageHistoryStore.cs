using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.JSInterop;

namespace Bit.BlazorUI;

/// <summary>Persists usage in the browser's <c>localStorage</c> so recents survive reloads.</summary>
public sealed class BitCommandPaletteBrowserStorageHistoryStore : IBitCommandPaletteHistoryStore
{
    private readonly IJSRuntime _js;
    private readonly string _key;

    public BitCommandPaletteBrowserStorageHistoryStore(IJSRuntime js, BitCommandPaletteOptions options)
    {
        _js = js;
        _key = options.HistoryStorageKey;
    }

    public async ValueTask<IDictionary<string, BitCommandPaletteCommandUsage>> LoadAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var json = await _js.InvokeAsync<string?>("localStorage.getItem", cancellationToken, _key).ConfigureAwait(false);
            if (string.IsNullOrEmpty(json))
            {
                return new Dictionary<string, BitCommandPaletteCommandUsage>(StringComparer.Ordinal);
            }

            var data = JsonSerializer.Deserialize(json, BitCommandPaletteHistoryJsonContext.Default.DictionaryStringBitCommandPaletteCommandUsage);
            return data ?? new Dictionary<string, BitCommandPaletteCommandUsage>(StringComparer.Ordinal);
        }
        catch (Exception ex) when (ex is JSException or JsonException or InvalidOperationException or JSDisconnectedException)
        {
            // Prerendering, disabled storage, or corrupt data: start empty.
            return new Dictionary<string, BitCommandPaletteCommandUsage>(StringComparer.Ordinal);
        }
    }

    public async ValueTask SaveAsync(IReadOnlyDictionary<string, BitCommandPaletteCommandUsage> usage, CancellationToken cancellationToken = default)
    {
        try
        {
            var json = JsonSerializer.Serialize(new Dictionary<string, BitCommandPaletteCommandUsage>(usage), BitCommandPaletteHistoryJsonContext.Default.DictionaryStringBitCommandPaletteCommandUsage);
            await _js.InvokeVoidAsync("localStorage.setItem", cancellationToken, _key, json).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is JSException or InvalidOperationException or JSDisconnectedException)
        {
            // Ignore: history is best-effort.
        }
    }
}
