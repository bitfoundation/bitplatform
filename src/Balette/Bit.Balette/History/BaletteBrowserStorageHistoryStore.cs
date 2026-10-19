using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.JSInterop;

namespace Bit.Balette;

/// <summary>Persists usage in the browser's <c>localStorage</c> so recents survive reloads.</summary>
public sealed class BaletteBrowserStorageHistoryStore : IBaletteHistoryStore
{
    private readonly IJSRuntime _js;
    private readonly string _key;

    public BaletteBrowserStorageHistoryStore(IJSRuntime js, BaletteOptions options)
    {
        _js = js;
        _key = options.HistoryStorageKey;
    }

    public async ValueTask<IDictionary<string, BaletteCommandUsage>> LoadAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var json = await _js.InvokeAsync<string?>("localStorage.getItem", cancellationToken, _key).ConfigureAwait(false);
            if (string.IsNullOrEmpty(json))
            {
                return new Dictionary<string, BaletteCommandUsage>(StringComparer.Ordinal);
            }

            var data = JsonSerializer.Deserialize(json, BaletteHistoryJsonContext.Default.DictionaryStringBaletteCommandUsage);
            return data ?? new Dictionary<string, BaletteCommandUsage>(StringComparer.Ordinal);
        }
        catch (Exception ex) when (ex is JSException or JsonException or InvalidOperationException or JSDisconnectedException)
        {
            // Prerendering, disabled storage, or corrupt data: start empty.
            return new Dictionary<string, BaletteCommandUsage>(StringComparer.Ordinal);
        }
    }

    public async ValueTask SaveAsync(IReadOnlyDictionary<string, BaletteCommandUsage> usage, CancellationToken cancellationToken = default)
    {
        try
        {
            var json = JsonSerializer.Serialize(new Dictionary<string, BaletteCommandUsage>(usage), BaletteHistoryJsonContext.Default.DictionaryStringBaletteCommandUsage);
            await _js.InvokeVoidAsync("localStorage.setItem", cancellationToken, _key, json).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is JSException or InvalidOperationException or JSDisconnectedException)
        {
            // Ignore: history is best-effort.
        }
    }
}

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(Dictionary<string, BaletteCommandUsage>))]
internal sealed partial class BaletteHistoryJsonContext : JsonSerializerContext
{
}
