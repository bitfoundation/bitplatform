using System.Text.Json;
using System.Text.Json.Nodes;

namespace Boilerplate.Workbench.Infrastructure.Services;

public sealed class WorkbenchSnapshots
{
    public const int MaxLength = 512 * 1024;

    private const int MaxCount = 256;

    private static readonly TimeSpan lifetime = TimeSpan.FromMinutes(10);

    private readonly ConcurrentDictionary<string, (string State, DateTimeOffset ExpiresAt)> entries = [];

    public string? Add(string? state)
    {
        if (string.IsNullOrEmpty(state) || state.Length > MaxLength)
            return null;

        try
        {
            if (JsonNode.Parse(state) is not JsonObject)
                return null;
        }
        catch (JsonException)
        {
            return null;
        }

        var now = DateTimeOffset.UtcNow;

        foreach (var (key, entry) in entries)
        {
            if (entry.ExpiresAt < now || entries.Count >= MaxCount)
            {
                entries.TryRemove(key, out _);
            }
        }

        var id = Guid.NewGuid().ToString("N");
        entries[id] = (state, now + lifetime);

        return id;
    }

    public JsonObject? Find(string? id)
    {
        if (id is null || entries.TryGetValue(id, out var entry) is false || entry.ExpiresAt < DateTimeOffset.UtcNow)
            return null;

        return JsonNode.Parse(entry.State) as JsonObject;
    }
}
