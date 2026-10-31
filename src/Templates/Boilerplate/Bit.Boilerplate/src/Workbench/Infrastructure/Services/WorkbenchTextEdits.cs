using System.Text;
using System.Globalization;
using System.Resources;
using System.Text.RegularExpressions;

namespace Boilerplate.Workbench.Infrastructure.Services;

public sealed partial class WorkbenchTextEdits
{
    private static readonly Dictionary<string, Type> resources = new()
    {
        [nameof(WorkbenchStrings)] = typeof(WorkbenchStrings),
        [nameof(AppStrings)] = typeof(AppStrings)
    };

    private readonly ConcurrentDictionary<(string Resource, string Culture, string Key), string> edited = [];

    public bool CanWriteFiles => WorkbenchFolders.Project is not null;

    public string? Find(string resource, string key)
    {
        return edited.TryGetValue((resource, CultureInfo.CurrentUICulture.Name, key), out var value) ? value : null;
    }

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

    public async Task<bool> Save(string resource, string key, string culture, string value)
    {
        if (WorkbenchFolders.Project is not { } folder || ResxFile(folder, resource, culture) is not { } path || File.Exists(path) is false)
            return false;

        var raw = await File.ReadAllBytesAsync(path);
        var hasBom = raw.AsSpan().StartsWith(Encoding.UTF8.Preamble);
        var text = Encoding.UTF8.GetString(hasBom ? raw[3..] : raw);
        var escaped = value.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");
        var entry = new Regex($@"(<data name=""{Regex.Escape(key)}""[^>]*>\s*<value>)(.*?)(</value>)", RegexOptions.Singleline);

        text = entry.IsMatch(text)
            ? entry.Replace(text, match => match.Groups[1].Value + escaped + match.Groups[3].Value, 1)
            : text.Insert(text.LastIndexOf("</root>", StringComparison.Ordinal), $"    <data name=\"{key}\" xml:space=\"preserve\">\n        <value>{escaped}</value>\n    </data>\n");

        await File.WriteAllBytesAsync(path, [.. (hasBom ? Encoding.UTF8.Preamble.ToArray() : []), .. Encoding.UTF8.GetBytes(text)]);

        edited[(resource, culture, key)] = value;

        return true;
    }

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
}
