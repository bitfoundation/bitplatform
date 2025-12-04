using System.ComponentModel.DataAnnotations;

namespace Bit.ResxTranslator;

public class ResxTranslatorSettings
{
    [Required]
    public string? DefaultLanguage { get; set; } = "en";

    public string[] SupportedLanguages { get; set; } = [];

    public string[] ResxPaths { get; set; } = [];

    public OpenAIOptions? OpenAI { get; set; }

    public OpenAIOptions? AzureOpenAI { get; set; }
}

public class OpenAIOptions
{
    public string? Model { get; set; }
    public Uri? Endpoint { get; set; }
    public string? ApiKey { get; set; }
}
