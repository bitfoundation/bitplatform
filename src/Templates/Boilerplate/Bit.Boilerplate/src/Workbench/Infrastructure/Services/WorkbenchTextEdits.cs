using System.Text;
using System.Security;
using System.Globalization;
using System.Resources;
using System.Text.RegularExpressions;

namespace Boilerplate.Workbench.Infrastructure.Services;

/// <summary>
/// Saves texts changed in Edit text mode into the WorkbenchStrings or AppStrings resx files of each culture, and keeps them
/// in memory, so pages show them before a restart picks up the resx.
/// </summary>
public sealed partial class WorkbenchTextEdits
{
    private static readonly Dictionary<string, Type> resources = new()
    {
        [nameof(WorkbenchStrings)] = typeof(WorkbenchStrings),
        [nameof(AppStrings)] = typeof(AppStrings)
    };

    [AutoInject] private IHttpContextAccessor httpContextAccessor = default!;

    private readonly ConcurrentDictionary<(string Resource, string Culture, string Key), string> edited = [];
    private readonly ConcurrentDictionary<string, SemaphoreSlim> fileLocks = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Whether the project folder, and with it the resx files, is on this machine.</summary>
    public bool CanWriteFiles => WorkbenchFolders.Project is not null;

    /// <summary>A text saved since startup in the current UI culture, or null.</summary>
    public string? Find(string resource, string key)
    {
        return edited.TryGetValue((resource, CultureInfo.CurrentUICulture.Name, key), out var value) ? value : null;
    }

    /// <summary>An entry's text in every supported culture, with the texts saved since startup.</summary>
    public WorkbenchTextTranslation[] Translations(string resource, string key)
    {
        if (resources.TryGetValue(resource, out var type) is false)
            return [];

        var manager = new ResourceManager(type);

        return [.. CultureInfoManager.SupportedCultures.Select(supported =>
        {
            var culture = supported.Culture;
            var value = edited.TryGetValue((resource, culture.Name, key), out var edit) ? edit : manager.GetString(key, culture);
            return new WorkbenchTextTranslation(culture.Name, supported.DisplayName, value);
        })];
    }

    /// <summary>
    /// Whether a text may replace an entry's: it must be a valid format string with the same placeholders, like {0}, as the
    /// entry's text in the default culture. An entry whose text isn't a format string takes any text.
    /// </summary>
    public bool KeepsPlaceholders(string resource, string key, string value)
    {
        var placeholders = PlaceholdersOf(value);
        var original = edited.TryGetValue((resource, CultureInfoManager.DefaultCulture.Name, key), out var edit)
            ? edit
            : resources.TryGetValue(resource, out var type) ? new ResourceManager(type).GetString(key, CultureInfoManager.DefaultCulture) : null;

        if (original is null)
            return placeholders is not null;

        return PlaceholdersOf(original) is not { } expected || (placeholders is not null && placeholders.SetEquals(expected));
    }

    /// <summary>
    /// Writes an entry's text into the culture's resx, adding the entry when the file lacks it. Returns false when the request
    /// isn't from this machine, the text changes the entry's placeholders, or the file isn't there.
    /// </summary>
    public async Task<bool> Save(string resource, string key, string culture, string value)
    {
        if (IsLocalRequest is false || KeepsPlaceholders(resource, key, value) is false)
            return false;

        if (WorkbenchFolders.Project is not { } folder || ResxFile(folder, resource, culture) is not { } path || File.Exists(path) is false)
            return false;

        var fileLock = fileLocks.GetOrAdd(Path.GetFullPath(path), _ => new(1, 1));

        await fileLock.WaitAsync();

        try
        {
            // The resx is edited as text, so the rest of the file, its BOM included, stays as it was.
            var raw = await File.ReadAllBytesAsync(path);
            var hasBom = raw.AsSpan().StartsWith(Encoding.UTF8.Preamble);
            var text = Encoding.UTF8.GetString(hasBom ? raw[3..] : raw);
            var name = SecurityElement.Escape(key) ?? key;
            var escaped = value.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");
            var entry = new Regex($@"(<data name=""{Regex.Escape(name)}""[^>]*>\s*<value>)(.*?)(</value>)", RegexOptions.Singleline);

            text = entry.IsMatch(text)
                ? entry.Replace(text, match => match.Groups[1].Value + escaped + match.Groups[3].Value, 1)
                : text.Insert(text.LastIndexOf("</root>", StringComparison.Ordinal), $"    <data name=\"{name}\" xml:space=\"preserve\">\n        <value>{escaped}</value>\n    </data>\n");

            await File.WriteAllBytesAsync(path, [.. (hasBom ? Encoding.UTF8.Preamble.ToArray() : []), .. Encoding.UTF8.GetBytes(text)]);
        }
        finally
        {
            fileLock.Release();
        }

        edited[(resource, culture, key)] = value;

        return true;
    }

    /// <summary>Whether the current request, or the circuit it started, came from this machine.</summary>
    private bool IsLocalRequest => httpContextAccessor.HttpContext is { } context && WorkbenchLocalRequests.IsLocal(context);

    /// <summary>The resx of a resource in a culture: the neutral file for the default culture, otherwise the language's file.</summary>
    private static string? ResxFile(string projectFolder, string resource, string culture)
    {
        var folder = resource switch
        {
            nameof(WorkbenchStrings) => Path.Combine(projectFolder, "Resources"),
            nameof(AppStrings) => Path.Combine(projectFolder, "..", "Shared", "Resources"),
            _ => null
        };

        if (folder is null || CultureInfoManager.GetCultureInfo(culture) is not { } info)
            return null;

        return info.Name == CultureInfoManager.DefaultCulture.Name
            ? Path.Combine(folder, $"{resource}.resx")
            : Path.Combine(folder, $"{resource}.{info.TwoLetterISOLanguageName}.resx");
    }

    /// <summary>The indexes of a format string's placeholders, or null when the text isn't a valid format string.</summary>
    private static HashSet<int>? PlaceholdersOf(string text)
    {
        CompositeFormat format;

        try
        {
            format = CompositeFormat.Parse(text);
        }
        catch (FormatException)
        {
            return null;
        }

        HashSet<int> used = [];
        object?[] arguments = [.. Enumerable.Range(0, format.MinimumArgumentCount).Select(index => new Placeholder(index, used))];

        _ = string.Format(CultureInfo.InvariantCulture, format, arguments);

        return used;
    }

    /// <summary>A format argument that notes the index of the placeholder that formats it.</summary>
    private sealed class Placeholder(int index, HashSet<int> used) : IFormattable
    {
        public string ToString(string? format, IFormatProvider? formatProvider)
        {
            used.Add(index);
            return string.Empty;
        }

        public override string ToString() => ToString(null, null);
    }
}
