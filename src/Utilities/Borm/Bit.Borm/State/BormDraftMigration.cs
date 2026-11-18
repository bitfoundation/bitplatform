namespace Bit.Borm;

/// <summary>A draft being brought up to date. See <see cref="BormDraftOptions.Migrate"/>.</summary>
public sealed class BormDraftMigration
{
    private readonly BormDraftContent _content;

    internal BormDraftMigration(BormDraftContent content, string? toVersion)
    {
        _content = content;
        ToVersion = toVersion;
    }

    /// <summary>The version of the form the draft was saved against, or null when it did not say.</summary>
    public string? FromVersion => _content.Version;

    /// <summary>The version of the form it is being restored into.</summary>
    public string? ToVersion { get; }

    /// <summary>Every saved value by path — edit it in place.</summary>
    public IDictionary<string, object?> Values => _content.Values;

    /// <summary>How many rows each repeater had, by path — edit it in place.</summary>
    public IDictionary<string, int> Rows => _content.Rows;

    /// <summary>
    /// Moves a field that was renamed or moved — its value, and for a container everything saved
    /// beneath it, rows included: <c>Rename("Phone", "Contact.Phone")</c>.
    /// </summary>
    public BormDraftMigration Rename(string from, string to)
    {
        ArgumentException.ThrowIfNullOrEmpty(from);
        ArgumentException.ThrowIfNullOrEmpty(to);
        Move(_content.Values, from, to);
        Move(_content.Rows, from, to);
        return this;

        static void Move<T>(IDictionary<string, T> map, string from, string to)
        {
            foreach (var key in map.Keys.Where(k => BormPath.IsAtOrUnder(k, from)).ToList())
            {
                var value = map[key];
                map.Remove(key);
                map[to + key[from.Length..]] = value;
            }
        }
    }

    /// <summary>Drops a field that no longer exists, and everything saved beneath it.</summary>
    public BormDraftMigration Remove(string path)
    {
        foreach (var key in _content.Values.Keys.Where(k => BormPath.IsAtOrUnder(k, path)).ToList()) _content.Values.Remove(key);
        foreach (var key in _content.Rows.Keys.Where(k => BormPath.IsAtOrUnder(k, path)).ToList()) _content.Rows.Remove(key);
        return this;
    }
}
