using System.Text.Json;
using System.Text.Json.Nodes;

namespace Boilerplate.Workbench.Infrastructure.Services;

/// <summary>
/// Holds a page's state for a few minutes under an id, so the headless browser's preview can restore what was on the screen.
/// </summary>
public sealed class WorkbenchSnapshots
{
    /// <summary>The longest page state kept, in characters; the circuit's message size limit and the saved stores follow it.</summary>
    public const int MaxLength = 512 * 1024;

    private const int MaxCount = 256;

    private static readonly TimeSpan lifetime = TimeSpan.FromMinutes(10);

    private readonly ConcurrentDictionary<string, (string State, DateTimeOffset ExpiresAt)> entries = [];

    /// <summary>Keeps a state that is a JSON object within the size limit; returns its id, or null.</summary>
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

        // Drops the expired states, then the oldest ones while the store is full.
        foreach (var (key, entry) in entries)
        {
            if (entry.ExpiresAt < now)
            {
                entries.TryRemove(key, out _);
            }
        }

        foreach (var key in entries.OrderBy(entry => entry.Value.ExpiresAt).Take(entries.Count - MaxCount + 1).Select(entry => entry.Key))
        {
            entries.TryRemove(key, out _);
        }

        var id = Guid.NewGuid().ToString("N");
        entries[id] = (state, now + lifetime);

        return id;
    }

    /// <summary>The state kept under this id, or null once it has expired.</summary>
    public JsonObject? Find(string? id)
    {
        if (id is null || entries.TryGetValue(id, out var entry) is false || entry.ExpiresAt < DateTimeOffset.UtcNow)
            return null;

        return JsonNode.Parse(entry.State) as JsonObject;
    }
}
