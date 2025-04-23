using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

using System.Xml.Linq;
using System.Globalization;
using System.Text.RegularExpressions;

using Bit.ResxTranslator.Models;

namespace Bit.ResxTranslator.Services;

public partial class Translator(ResxTranslatorSettings settings,
    ILogger<Translator> logger,
    IChatClient chatClient)
{
    public async Task UpdateResxTranslations()
    {
        await Parallel.ForEachAsync(GetResxGroups(), async (resxGroup, cancellationToken) =>
        {
            var defaultLanguageKeyValues = await DeserializeResxToDictionary(resxGroup.Path, cancellationToken);

            foreach (var relatedResx in resxGroup.RelatedResxFiles)
            {
                var relatedLanguageKeyValues = await DeserializeResxToDictionary(relatedResx.Path, cancellationToken);

                var notTranslatedKeyValues = defaultLanguageKeyValues
                    .Where(kvp => relatedLanguageKeyValues.ContainsKey(kvp.Key) is false)
                    .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

                if (notTranslatedKeyValues.Count == 0)
                {
                    logger.LogInformation("No new translations needed for {ResxGroup} to {RelatedResx}.", resxGroup.CultureInfo.Name, relatedResx.CultureInfo.Name);
                    continue;
                }

                logger.LogInformation("Translating from {ResxGroup} to {RelatedResx}...", resxGroup.CultureInfo.Name, relatedResx.CultureInfo.Name);

                var _ = await chatClient.GetResponseAsync(new ChatMessage(ChatRole.System, @$"You act as a translator for software resource files.
Your task is to retrieve the values that need translation by calling the `GetSourceValuesToBeTranslated` tool,
which returns an array of strings in [{resxGroup.CultureInfo.NativeName} - {resxGroup.CultureInfo.EnglishName}] language.
These strings are used in a software application and may contain placeholders such as {0}, {1}, etc.
Translate each string to {relatedResx.CultureInfo.NativeName} language, ensuring that any placeholders are preserved exactly as they are,
including their numbers, and are placed correctly in the translated sentence according to the target language's grammar and syntax.
The translation should be accurate and suitable for a software application context.
After translating, call the `SaveTranslatedValues` tool and pass an array of the translated strings in the same order as the source strings."), options: new()
                {
                    Temperature = 0,
                    Tools =
                    [
                        AIFunctionFactory.Create(() =>
                        {
                            return notTranslatedKeyValues.Values;
                        }, name: "GetSourceValuesToBeTranslated", description: $"Returns [{resxGroup.CultureInfo.NativeName} - {resxGroup.CultureInfo.EnglishName}] values."),

                        AIFunctionFactory.Create((string[] updatedRelatedLanguageValues) =>
                        {
                            foreach ((string translate, int index) in updatedRelatedLanguageValues.Select((translate, index) => (translate, index)))
                            {
                                relatedLanguageKeyValues.Add(notTranslatedKeyValues.ElementAt(index).Key, translate);
                            }
                        }, name: "SaveTranslatedValues", description: $"Saves [{relatedResx.CultureInfo.NativeName} - {relatedResx.CultureInfo.EnglishName}] values.")
                    ]
                }, cancellationToken: cancellationToken);

                var defaultFileContent = await File.ReadAllTextAsync(resxGroup.Path, cancellationToken);
                var doc = XDocument.Parse(defaultFileContent);
                foreach (var data in doc.Root!.Elements("data"))
                {
                    var key = data.Attribute("name")?.Value;
                    if (key is not null && relatedLanguageKeyValues.TryGetValue(key, out var tr))
                        data.Element("value")!.Value = tr ?? string.Empty; // XDocument escapes for you
                }
                File.Delete(relatedResx.Path);
                await using var relatedResxFile = File.OpenWrite(relatedResx.Path);
                await doc.SaveAsync(relatedResxFile, SaveOptions.None, cancellationToken);
            }
        });
    }

    public ResxFile[] GetResxGroups()
    {
        var primaryResxFiles =
            settings.ResxPaths
            .SelectMany(path => FindResxFiles(Environment.CurrentDirectory, path))
            .Where(path => LanguageNameRegex.IsMatch(path) is false)
            .Distinct();

        return [.. primaryResxFiles
            .Select(defaultResxFilePath => new ResxFile
            {
                Path = defaultResxFilePath,
                Language = settings.DefaultLanguage!,
                CultureInfo = new CultureInfo(settings.DefaultLanguage!),
                RelatedResxFiles = [.. settings.SupportedLanguages.Select(additionalLanguage => new ResxFile {
                    Language = additionalLanguage,
                    CultureInfo = new CultureInfo(additionalLanguage),
                    Path = defaultResxFilePath.Replace(".resx", $".{additionalLanguage}.resx"),
                })]
            })];
    }

    private IEnumerable<string> FindResxFiles(string rootPath, string path)
    {
        try
        {
            // Normalize pattern (remove leading slashes, handle platform-specific separators)
            path = path.TrimStart('/', '\\').Replace('/', Path.DirectorySeparatorChar);

            // Split pattern into directory path and file pattern
            string dirPath;
            bool isRecursive;
            if (path.Contains("**"))
            {
                // Recursive pattern (e.g., src/**/*.resx)
                dirPath = path[..path.IndexOf("**")];
                isRecursive = true;
            }
            else
            {
                // Non-recursive pattern (e.g., src/Shared/Resources/*.resx)
                dirPath = Path.GetDirectoryName(path) ?? "";
                isRecursive = false;
            }

            string fullDirPath = Path.Combine(rootPath, dirPath);

            if (!Directory.Exists(fullDirPath))
            {
                logger.LogWarning("Directory {DirPath} not found.", fullDirPath);
                return [];
            }

            return Directory.EnumerateFiles(
                fullDirPath,
                "*.resx",
                isRecursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly
            );
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing pattern {Path}: {Message}", path, ex.Message);
            return [];
        }
    }

    private static readonly Regex LanguageNameRegex = LanguageNameRegexBuilder();

    [GeneratedRegex(@"\.[a-zA-Z]{2,4}(-[a-zA-Z]{2,8})?(?=\.resx$)", RegexOptions.IgnoreCase)]
    private static partial Regex LanguageNameRegexBuilder();

    static async Task<Dictionary<string, string?>> DeserializeResxToDictionary(string filePath, CancellationToken cancellationToken)
    {
        if (File.Exists(filePath) is false)
            return [];

        await using var file = File.OpenRead(filePath);

        var dictionary = new Dictionary<string, string?>();

        XDocument doc = await XDocument.LoadAsync(file, LoadOptions.None, cancellationToken);

        foreach (XElement dataElement in doc.Root!.Elements("data"))
        {
            string name = dataElement.Attribute("name")!.Value;
            string value = dataElement.Element("value")!.Value;

            if (string.IsNullOrWhiteSpace(name))
                continue;

            dictionary[name] = value;
        }

        return dictionary;
    }
}
