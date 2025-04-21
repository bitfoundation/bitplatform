using Microsoft.Extensions.Logging;

using System.Text.RegularExpressions;

namespace Bit.ResxTranslator.Services;

public class ResxFile
{
    public required string Language { get; init; }

    public required string Path { get; init; }

    public ResxFile[] RelatedResxFiles { get; set; } = [];
}

public partial class ResxFilesManager(ResxTranslatorSettings settings,
    ILogger<ResxFilesManager> logger)
{
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
                RelatedResxFiles = [.. settings.AdditionalLanguages.Select(additionalLanguage => new ResxFile {
                    Language = additionalLanguage,
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
}
