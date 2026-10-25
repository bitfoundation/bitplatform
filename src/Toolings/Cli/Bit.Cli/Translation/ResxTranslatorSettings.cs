using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;

namespace Bit.Cli.Translation;

public sealed class ResxTranslatorSettings
{
    public const string FileName = "Bit.ResxTranslator.json";

    public string DefaultLanguage { get; set; } = "en";

    public string[] SupportedLanguages { get; set; } = [];

    public string[] ResxPaths { get; set; } = [];

    public OpenAIOptions OpenAI { get; set; } = new();

    public ChatOptions ChatOptions { get; set; } = new();

    public required string RootDirectory { get; init; }

    public required string ConfigPath { get; init; }

    public static string? FindConfig(string startDirectory)
    {
        for (var directory = new DirectoryInfo(startDirectory); directory is not null; directory = directory.Parent)
        {
            var candidate = Path.Combine(directory.FullName, FileName);

            if (File.Exists(candidate))
                return candidate;
        }

        return null;
    }

    public static ResxTranslatorSettings Load(string configPath, IReadOnlyDictionary<string, string> environmentVariables)
    {
        var configuration = new ConfigurationBuilder()
            .AddJsonFile(configPath, optional: false)
            .AddInMemoryCollection(environmentVariables
                .Where(v => v.Key.StartsWith("OpenAI__", StringComparison.OrdinalIgnoreCase))
                .Select(v => new KeyValuePair<string, string?>(v.Key.Replace("__", ":", StringComparison.Ordinal), v.Value)))
            .Build();

        var settings = new ResxTranslatorSettings { RootDirectory = Path.GetDirectoryName(Path.GetFullPath(configPath))!, ConfigPath = Path.GetFullPath(configPath) };
        configuration.Bind(settings);

        var chatOptions = new ChatOptions();
        configuration.GetSection("ChatOptions").Bind(chatOptions);
        settings.ChatOptions = chatOptions;

        return settings;
    }

    public IEnumerable<string> Validate()
    {
        if (string.IsNullOrWhiteSpace(DefaultLanguage))
            yield return "DefaultLanguage is empty.";

        if (SupportedLanguages.Length == 0)
            yield return "SupportedLanguages is empty.";

        if (ResxPaths.Length == 0)
            yield return "ResxPaths is empty.";

        foreach (var language in SupportedLanguages.Append(DefaultLanguage).Where(l => string.IsNullOrWhiteSpace(l) is false))
        {
            if (IsKnownCulture(language) is false)
                yield return $"'{language}' isn't a culture .NET knows.";
        }
    }

    private static bool IsKnownCulture(string name)
    {
        try
        {
            _ = System.Globalization.CultureInfo.GetCultureInfo(name, predefinedOnly: true);
            return true;
        }
        catch (System.Globalization.CultureNotFoundException)
        {
            return false;
        }
    }
}

public sealed class OpenAIOptions
{
    public string? Model { get; set; }

    public Uri? Endpoint { get; set; }

    public string? ApiKey { get; set; }

    public string ProviderKind => Endpoint?.Host switch
    {
        null => "openai",
        "api.openai.com" => "openai",
        "models.inference.ai.azure.com" or "models.github.ai" => "github",
        "generativelanguage.googleapis.com" => "google",
        "api.x.ai" => "xai",
        var host when host.EndsWith(".openai.azure.com", StringComparison.OrdinalIgnoreCase) || host.EndsWith(".services.ai.azure.com", StringComparison.OrdinalIgnoreCase) => "azure",
        _ => "other"
    };
}
