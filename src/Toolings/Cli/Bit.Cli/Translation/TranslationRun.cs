using System.ClientModel;
using Bit.Cli.Infrastructure;
using Bit.Cli.Telemetry;
using Spectre.Console;

namespace Bit.Cli.Translation;

public sealed record TranslateRequest(string? Config, IReadOnlyList<string> Languages, bool Check, bool DryRun);

public sealed class TranslationRun(CliServices cli, Func<ResxTranslatorSettings, ITranslationClient>? clientFactory = null)
{
    public const int BatchSize = 250;

    private long inputTokens;
    private long outputTokens;
    private int translatedKeys;
    private int batchCount;

    public async Task<int> RunAsync(TranslateRequest request, CancellationToken cancellationToken)
    {
        var configPath = request.Config is not null
            ? Path.GetFullPath(request.Config, cli.Environment.CurrentDirectory)
            : ResxTranslatorSettings.FindConfig(cli.Environment.CurrentDirectory);

        if (configPath is null || File.Exists(configPath) is false)
        {
            cli.Console.Fail($"No {ResxTranslatorSettings.FileName} in this folder or above it. Run bit translate in a bit Boilerplate project, or pass --config <file>.");
            return CliApp.ExitUsage;
        }

        var settings = ResxTranslatorSettings.Load(configPath, cli.Environment.Variables);
        var problems = settings.Validate().ToList();

        foreach (var unknown in request.Languages.Where(l => settings.SupportedLanguages.Contains(l, StringComparer.OrdinalIgnoreCase) is false))
        {
            problems.Add($"--language {unknown} isn't in SupportedLanguages.");
        }

        if (problems.Count > 0)
        {
            foreach (var problem in problems)
            {
                cli.Console.Fail($"{configPath}: {problem}");
            }

            return CliApp.ExitUsage;
        }

        var baseFiles = ResxCatalog.FindBaseFiles(settings, cli.Console.Warn);
        var work = ResxCatalog.Pairs(settings, baseFiles, request.Languages).Select(ResxCatalog.Plan).ToList();
        var pending = work.Where(w => w.Missing.Count > 0).ToList();

        cli.Telemetry.SetTag(TelemetryFields.TranslateLanguages, settings.SupportedLanguages.Length);
        cli.Telemetry.SetTag(TelemetryFields.TranslateProvider, settings.OpenAI.ProviderKind);

        cli.Console.Out.MarkupLine($"[grey]{Markup.Escape(Path.GetRelativePath(cli.Environment.CurrentDirectory, configPath))}:[/] {Markup.Escape(settings.DefaultLanguage)} [grey]to[/] {Markup.Escape(string.Join(", ", request.Languages.Count > 0 ? request.Languages : settings.SupportedLanguages))}[grey], {baseFiles.Count} .resx file{(baseFiles.Count == 1 ? "" : "s")}[/]");

        if (baseFiles.Count == 0)
        {
            cli.Console.Warn("No .resx files matched ResxPaths.");
            return CliApp.ExitFailed;
        }

        if (pending.Count == 0)
        {
            cli.Console.Step(StepStatus.Succeeded, "Every language is up to date");
            return CliApp.ExitOk;
        }

        foreach (var item in pending)
        {
            cli.Console.Out.MarkupLine($"  [grey]{Markup.Escape(Path.GetRelativePath(settings.RootDirectory, item.Pair.TargetPath))}[/]  {item.Missing.Count} missing");
        }

        var totalMissing = pending.Sum(w => w.Missing.Count);

        if (request.Check)
        {
            cli.Console.Fail($"{totalMissing} translation{(totalMissing == 1 ? " is" : "s are")} missing. Run bit translate to fill them in.");
            return CliApp.ExitCheckFailed;
        }

        if (request.DryRun)
        {
            cli.Console.Out.MarkupLine($"[grey]Dry run: {totalMissing} keys would be translated in {pending.Count} files, in about {pending.Sum(w => (w.Missing.Count + BatchSize - 1) / BatchSize)} requests.[/]");
            return CliApp.ExitOk;
        }

        if (string.IsNullOrWhiteSpace(settings.OpenAI.ApiKey))
        {
            cli.Console.Warn($"Nothing was translated: there's no API key. Set the OpenAI__ApiKey environment variable (or OpenAI:ApiKey in {ResxTranslatorSettings.FileName}).");
            return CliApp.ExitOk;
        }

        var client = clientFactory?.Invoke(settings) ?? ChatTranslationClient.Create(settings);
        var steps = new StepRunner(cli);

        foreach (var item in pending)
        {
            var name = Path.GetFileName(item.Pair.TargetPath);

            await steps.RunAsync("translate", $"Translating {name}", async (progress, ct) =>
            {
                try
                {
                    var (translations, skipped) = await TranslateAsync(client, item, progress, ct);

                    if (translations.Count > 0)
                    {
                        ResxCatalog.Write(item.Pair, translations);
                    }

                    var detail = $"+{translations.Count} key{(translations.Count == 1 ? "" : "s")}";

                    return skipped == 0
                        ? StepResult.Succeeded($"Translated {name}", detail)
                        : StepResult.Warning($"Translated {name}", $"{detail}, {skipped} kept out: their placeholders didn't match", "bit translate");
                }
                catch (ClientResultException exp)
                {
                    cli.Telemetry.SetTag(TelemetryFields.HttpStatus, exp.Status);
                    return StepResult.Failed($"Couldn't translate {name}", $"the model endpoint answered {exp.Status}", "bit translate", resultCode: $"translate.http.{exp.Status}", hint: exp.Message.Split('\n')[0]);
                }
                catch (HttpRequestException exp)
                {
                    return StepResult.Failed($"Couldn't translate {name}", "the model endpoint couldn't be reached", "bit translate", resultCode: "translate.network", hint: exp.Message);
                }
                catch (InvalidOperationException exp)
                {
                    return StepResult.Failed($"Couldn't translate {name}", "the model's answer wasn't usable", "bit translate", resultCode: "translate.response", hint: exp.Message);
                }
            }, cancellationToken);
        }

        cli.Telemetry.SetTag(TelemetryFields.TranslateKeys, translatedKeys);
        cli.Telemetry.SetTag(TelemetryFields.TranslateBatches, batchCount);
        cli.Telemetry.SetTag(TelemetryFields.TranslateInputTokens, inputTokens);
        cli.Telemetry.SetTag(TelemetryFields.TranslateOutputTokens, outputTokens);

        cli.Console.Out.WriteLine();
        cli.Console.Out.MarkupLine($"[bold]Translated {translatedKeys} key{(translatedKeys == 1 ? "" : "s")}[/] [grey]in {batchCount} request{(batchCount == 1 ? "" : "s")}, {inputTokens:N0} input and {outputTokens:N0} output tokens[/]");

        return steps.AnyFailed ? CliApp.ExitFailed : CliApp.ExitOk;
    }

    private async Task<(Dictionary<string, string> Translations, int Skipped)> TranslateAsync(ITranslationClient client, ResxWork item, Action<string> progress, CancellationToken cancellationToken)
    {
        var translations = new Dictionary<string, string>(StringComparer.Ordinal);
        var batches = item.Missing.Chunk(BatchSize).ToList();
        var skipped = 0;

        for (var index = 0; index < batches.Count; index++)
        {
            progress($"batch {index + 1} of {batches.Count}");

            var batch = batches[index];
            var result = await TranslateBatchAsync(client, item, [.. batch.Select(kvp => kvp.Value)], reminder: false, cancellationToken);
            var retry = new List<int>();

            for (var i = 0; i < batch.Length; i++)
            {
                if (ChatTranslationClient.PlaceholdersMatch(batch[i].Value, result[i]))
                {
                    translations[batch[i].Key] = result[i];
                }
                else
                {
                    retry.Add(i);
                }
            }

            if (retry.Count > 0)
            {
                progress($"batch {index + 1} of {batches.Count}, retrying {retry.Count} with placeholder problems");
                var second = await TranslateBatchAsync(client, item, [.. retry.Select(i => batch[i].Value)], reminder: true, cancellationToken);

                for (var r = 0; r < retry.Count; r++)
                {
                    var original = batch[retry[r]];

                    if (ChatTranslationClient.PlaceholdersMatch(original.Value, second[r]))
                    {
                        translations[original.Key] = second[r];
                    }
                    else
                    {
                        skipped++;
                    }
                }
            }
        }

        Interlocked.Add(ref translatedKeys, translations.Count);
        return (translations, skipped);
    }

    private async Task<IReadOnlyList<string>> TranslateBatchAsync(ITranslationClient client, ResxWork item, IReadOnlyList<string> values, bool reminder, CancellationToken cancellationToken)
    {
        var output = await client.TranslateAsync(new TranslationBatch(item.Pair.Source, item.Pair.Target, values, reminder), cancellationToken);

        Interlocked.Increment(ref batchCount);
        Interlocked.Add(ref inputTokens, output.InputTokens ?? 0);
        Interlocked.Add(ref outputTokens, output.OutputTokens ?? 0);

        if (output.Translations.Count != values.Count)
            throw new InvalidOperationException($"Asked for {values.Count} translations and got {output.Translations.Count}.");

        return output.Translations;
    }
}
