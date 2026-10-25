using System.Globalization;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace Bit.Cli.Translation;

public sealed record ResxPair(string BasePath, string TargetPath, CultureInfo Source, CultureInfo Target);

public sealed record ResxWork(ResxPair Pair, IReadOnlyDictionary<string, string> Missing, int Existing);

public static partial class ResxCatalog
{
    public static IReadOnlyList<string> FindBaseFiles(ResxTranslatorSettings settings, Action<string>? warn = null)
    {
        return [.. settings.ResxPaths
            .SelectMany(pattern => Find(settings.RootDirectory, pattern, warn))
            .Where(path => IsCultureSpecific(path) is false)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Order(StringComparer.OrdinalIgnoreCase)];
    }

    public static IReadOnlyList<ResxPair> Pairs(ResxTranslatorSettings settings, IReadOnlyList<string> baseFiles, IReadOnlyCollection<string>? languages = null)
    {
        var source = CultureInfo.GetCultureInfo(settings.DefaultLanguage);
        var targets = settings.SupportedLanguages
            .Where(l => languages is null || languages.Count == 0 || languages.Contains(l, StringComparer.OrdinalIgnoreCase))
            .Select(l => CultureInfo.GetCultureInfo(l))
            .ToList();

        return [.. baseFiles.SelectMany(baseFile => targets.Select(target => new ResxPair(baseFile, TargetPath(baseFile, target.Name), source, target)))];
    }

    public static string TargetPath(string baseFile, string language)
    {
        return Path.Combine(Path.GetDirectoryName(baseFile)!, $"{Path.GetFileNameWithoutExtension(baseFile)}.{language}.resx");
    }

    public static ResxWork Plan(ResxPair pair)
    {
        var source = Read(pair.BasePath);
        var target = File.Exists(pair.TargetPath) ? Read(pair.TargetPath) : [];

        var missing = source
            .Where(kvp => target.ContainsKey(kvp.Key) is false)
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

        return new ResxWork(pair, missing, source.Count - missing.Count);
    }

    public static Dictionary<string, string> Read(string path)
    {
        var document = XDocument.Load(path);
        var values = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var data in document.Root!.Elements("data"))
        {
            var name = data.Attribute("name")?.Value;

            if (string.IsNullOrWhiteSpace(name) || data.Attribute("type") is not null)
                continue;

            values[name] = data.Element("value")?.Value ?? "";
        }

        return values;
    }

    public static void Write(ResxPair pair, IReadOnlyDictionary<string, string> translations)
    {
        var existing = File.Exists(pair.TargetPath) ? Read(pair.TargetPath) : [];
        var document = XDocument.Load(pair.BasePath, LoadOptions.PreserveWhitespace);

        foreach (var data in document.Root!.Elements("data").ToList())
        {
            var name = data.Attribute("name")?.Value;

            if (name is null || data.Attribute("type") is not null)
                continue;

            if (translations.TryGetValue(name, out var translated) || existing.TryGetValue(name, out translated))
            {
                var value = data.Element("value");

                if (value is null)
                {
                    data.Add(value = new XElement("value"));
                }

                value.Value = translated;
            }
            else
            {
                if (data.PreviousNode is XText whitespace && string.IsNullOrWhiteSpace(whitespace.Value))
                {
                    whitespace.Remove();
                }

                data.Remove();
            }
        }

        using var stream = new MemoryStream();
        document.Save(stream, SaveOptions.DisableFormatting);
        Infrastructure.AtomicFile.WriteAllText(pair.TargetPath, System.Text.Encoding.UTF8.GetString(stream.ToArray()).TrimStart('\uFEFF'));
    }

    private static IEnumerable<string> Find(string root, string pattern, Action<string>? warn)
    {
        var normalized = pattern.TrimStart('/', '\\').Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar);
        string directory;
        bool recursive;

        if (normalized.Contains("**", StringComparison.Ordinal))
        {
            directory = normalized[..normalized.IndexOf("**", StringComparison.Ordinal)];
            recursive = true;
        }
        else
        {
            directory = Path.GetDirectoryName(normalized) ?? "";
            recursive = false;
        }

        var fullDirectory = Path.Combine(root, directory);

        if (Directory.Exists(fullDirectory) is false)
        {
            warn?.Invoke($"{fullDirectory} doesn't exist; '{pattern}' matched nothing.");
            return [];
        }

        var filePattern = Path.GetFileName(normalized) is { Length: > 0 } name && name.Contains('*') ? name : "*.resx";

        return Directory.EnumerateFiles(fullDirectory, filePattern, new EnumerationOptions
        {
            RecurseSubdirectories = recursive,
            IgnoreInaccessible = true
        }).Where(path => path.EndsWith(".resx", StringComparison.OrdinalIgnoreCase)
            && path.Replace('\\', '/').Contains("/bin/", StringComparison.OrdinalIgnoreCase) is false
            && path.Replace('\\', '/').Contains("/obj/", StringComparison.OrdinalIgnoreCase) is false
            && path.Replace('\\', '/').Contains("/node_modules/", StringComparison.OrdinalIgnoreCase) is false);
    }

    public static bool IsCultureSpecific(string path)
    {
        var name = Path.GetFileNameWithoutExtension(path);
        var dot = name.LastIndexOf('.');

        if (dot < 0 || CultureNameRegex().IsMatch(name[(dot + 1)..]) is false)
            return false;

        try
        {
            return CultureInfo.GetCultureInfo(name[(dot + 1)..], predefinedOnly: true).Name.Length > 0;
        }
        catch (CultureNotFoundException)
        {
            return false;
        }
    }

    [GeneratedRegex(@"^[a-zA-Z]{2,3}(-[a-zA-Z0-9]{2,8})*$")]
    private static partial Regex CultureNameRegex();
}
