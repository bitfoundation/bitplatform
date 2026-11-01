using System.Globalization;
using System.Text.RegularExpressions;

namespace Boilerplate.Workbench.Infrastructure.Services;

/// <summary>
/// Remembers the localized texts the pages showed, per culture, so a text picked on the page can be traced to its resource entry.
/// </summary>
public sealed partial class WorkbenchTextRecorder
{
    private const int MaxEntries = 50_000;

    private int count;
    private readonly ConcurrentDictionary<(string Resource, string Key, string Culture), (string Raw, string Shown)> entries = [];
    private readonly ConcurrentDictionary<string, Regex> formats = [];

    /// <summary>
    /// Records a text as shown and as its resource has it; past the cap, only entries already known are updated.
    /// Called for every text a page renders, so an unchanged entry is only read.
    /// </summary>
    public void Record(string resource, string key, string raw, string shown)
    {
        var entry = (resource, key, CultureInfo.CurrentUICulture.Name);

        if (entries.TryGetValue(entry, out var known))
        {
            if (known != (raw, shown))
            {
                entries[entry] = (raw, shown);
            }

            return;
        }

        if (Volatile.Read(ref count) < MaxEntries && entries.TryAdd(entry, (raw, shown)))
        {
            Interlocked.Increment(ref count);
        }
    }

    /// <summary>
    /// Up to eight entries whose text, or format with any values, is the picked text; failing that, those the picked text contains.
    /// </summary>
    public WorkbenchTextMatch[] Find(string text, string culture)
    {
        var wanted = Normalize(text);

        if (wanted.Length == 0)
            return [];

        var candidates = entries.Where(entry => entry.Key.Culture == culture).ToArray();

        var found = candidates.Where(entry => Normalize(entry.Value.Shown) == wanted || FormatMatches(entry.Value.Raw, wanted)).ToArray();

        if (found.Length == 0)
        {
            found = [.. candidates.Where(entry => entry.Value.Shown.Length > 2 && wanted.Contains(Normalize(entry.Value.Shown), StringComparison.Ordinal))];
        }

        return [.. found.OrderByDescending(entry => entry.Value.Shown.Length)
                        .Take(8)
                        .Select(entry => new WorkbenchTextMatch(entry.Key.Resource, entry.Key.Key, entry.Value.Raw))];
    }

    /// <summary>Whether a format string, with each placeholder matching any text, matches the picked text; a match that takes too long counts as none.</summary>
    private bool FormatMatches(string raw, string wanted)
    {
        if (raw.Contains('{') is false)
            return false;

        var format = formats.GetOrAdd(raw, _ => new($"^{PlaceholderRegex().Replace(Regex.Escape(Normalize(raw)), ".+?")}$", RegexOptions.Singleline, TimeSpan.FromMilliseconds(50)));

        try
        {
            return format.IsMatch(wanted);
        }
        catch (RegexMatchTimeoutException)
        {
            return false;
        }
    }

    /// <summary>Collapses whitespace, which the rendered page doesn't keep as the resource has it.</summary>
    private static string Normalize(string value) => string.Join(' ', value.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));

    [GeneratedRegex(@"\\\{\d+(?::[^}]*)?}")]
    private static partial Regex PlaceholderRegex();
}
