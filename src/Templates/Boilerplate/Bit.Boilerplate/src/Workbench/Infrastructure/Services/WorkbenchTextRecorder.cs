using System.Globalization;
using System.Text.RegularExpressions;

namespace Boilerplate.Workbench.Infrastructure.Services;

public sealed partial class WorkbenchTextRecorder
{
    private const int MaxEntries = 50_000;

    private readonly ConcurrentDictionary<(string Resource, string Key, string Culture), (string Raw, string Shown)> entries = [];

    public void Record(string resource, string key, string raw, string shown)
    {
        var entry = (resource, key, CultureInfo.CurrentUICulture.Name);

        if (entries.Count < MaxEntries || entries.ContainsKey(entry))
        {
            entries[entry] = (raw, shown);
        }
    }

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

    private static bool FormatMatches(string raw, string wanted)
    {
        if (raw.Contains('{') is false)
            return false;

        var pattern = PlaceholderRegex().Replace(Regex.Escape(Normalize(raw)), ".+?");

        return Regex.IsMatch(wanted, $"^{pattern}$", RegexOptions.Singleline, TimeSpan.FromMilliseconds(50));
    }

    private static string Normalize(string value) => string.Join(' ', value.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));

    [GeneratedRegex(@"\\\{\d+(?::[^}]*)?}")]
    private static partial Regex PlaceholderRegex();
}
