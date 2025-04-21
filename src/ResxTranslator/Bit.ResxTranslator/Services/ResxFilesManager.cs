using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

using System.Xml.Linq;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Bit.ResxTranslator.Services;

public class ResxFile
{
    public required string Language { get; init; }

    public required CultureInfo CultureInfo { get; init; }

    public required string Path { get; init; }

    public ResxFile[] RelatedResxFiles { get; set; } = [];
}

public partial class ResxFilesManager(ResxTranslatorSettings settings,
    ILogger<ResxFilesManager> logger,
    IChatClient chatClient)
{
    public async Task Run()
    {
        foreach (var resxGroup in GetResxGroups())
        {
            var defaultLanguageKeyValues = await DeserializeXmlToDictionary(resxGroup.Path);

            foreach (var relatedResx in resxGroup.RelatedResxFiles)
            {
                var relatedLanguageKeyValues = await DeserializeXmlToDictionary(relatedResx.Path);

                var notTranslatedKeyValues = defaultLanguageKeyValues
                    .Where(kvp => relatedLanguageKeyValues.ContainsKey(kvp.Key) is false)
                    .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

                var _ = await chatClient.GetResponseAsync(new ChatMessage(ChatRole.System, @$"
You act as a translator. Your task is to retrive the values by calling `GetSourceValuesToBeTranslated` task which have values in [{resxGroup.CultureInfo.NativeName} - {resxGroup.CultureInfo.EnglishName}] language. 
Translate the values to {relatedResx.CultureInfo.NativeName} language and save them by calling `SaveTranslatedValues` tool and passing translated values."),
                    options: new()
                    {
                        Tools = [
                            AIFunctionFactory.Create(() =>
                            {
                                return notTranslatedKeyValues.Values.Take(10);
                            }, name: "GetSourceValuesToBeTranslated", description: $"Returns [{resxGroup.CultureInfo.NativeName} - {resxGroup.CultureInfo.EnglishName}] values."),

                            AIFunctionFactory.Create((string[] updatedRelatedLanguageKeyValues) =>
                            {
                                foreach ((string translate, int index) in updatedRelatedLanguageKeyValues.Select((translate, index) => (translate, index)))
                                {
                                    relatedLanguageKeyValues.Add(notTranslatedKeyValues.ElementAt(index).Key, translate);
                                }
                            }, name: "SaveTranslatedValues", description: $"Saves [{relatedResx.CultureInfo.NativeName} - {relatedResx.CultureInfo.EnglishName}] values.")
                        ]
                    });
            }
        }
    }

    public ResxFile[] GetResxGroups()
    {
        return [.. settings.ResxPaths
            .SelectMany(path => FindResxFiles(Environment.CurrentDirectory, path))
            .Where(path => LanguageNameRegex.IsMatch(path) is false)
            .Distinct()
            .Select(defaultResxFilePath => new ResxFile
            {
                Path = defaultResxFilePath,
                Language = settings.DefaultLanguage!,
                CultureInfo = new CultureInfo(settings.DefaultLanguage!),
                RelatedResxFiles = [.. settings.AdditionalLanguages.Select(additionalLanguage => new ResxFile {
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

    [GeneratedRegex(@"\.[a-zA-Z]{2}(?=\.resx$)")]
    private static partial Regex LanguageNameRegexBuilder();

    static async Task<Dictionary<string, string?>> DeserializeXmlToDictionary(string filePath)
    {
        if (File.Exists(filePath) is false)
            return [];

        await using var file = File.OpenRead(filePath);

        var dictionary = new Dictionary<string, string?>();

        XDocument doc = await XDocument.LoadAsync(file, LoadOptions.None, default);

        foreach (XElement dataElement in doc.Root!.Elements("data"))
        {
            string name = dataElement.Attribute("name")!.Value;
            string value = dataElement.Element("value")!.Value;

            if (!string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(value))
            {
                dictionary[name] = value;
            }
        }

        return dictionary;
    }
}
