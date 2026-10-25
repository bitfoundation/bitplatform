using System.CommandLine;
using Bit.Cli.Translation;

namespace Bit.Cli.Commands;

public static class TranslateCommand
{
    public static Command Create(Func<CliServices> services)
    {
        var configOption = new Option<string?>("--config") { Description = $"The settings file. Default: the {ResxTranslatorSettings.FileName} in this folder or the nearest one above it." };
        var languageOption = new Option<string[]>("--language", "-l") { Description = "Translate only these languages, e.g. -l fa -l de. Default: every one in SupportedLanguages.", AllowMultipleArgumentsPerToken = true };
        var checkOption = new Option<bool>("--check") { Description = "Call no model; exit with code 3 when any translation is missing. For CI." };
        var dryRunOption = new Option<bool>("--dry-run") { Description = "List what would be translated, and change nothing." };

        var command = new Command("translate", "Fill in the missing translations of .resx files with an OpenAI-compatible LLM, keeping existing ones.")
        {
            configOption,
            languageOption,
            checkOption,
            dryRunOption
        };

        command.SetAction((parseResult, cancellationToken) =>
        {
            var request = new TranslateRequest(
                parseResult.GetValue(configOption),
                SharedOptions.SplitList(parseResult.GetValue(languageOption)),
                parseResult.GetValue(checkOption),
                parseResult.GetValue(dryRunOption));

            return new TranslationRun(services()).RunAsync(request, cancellationToken);
        });

        return command;
    }
}
