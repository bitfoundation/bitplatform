using System.CommandLine;
using Bit.Cli.Infrastructure;
using Bit.Cli.Projects;
using Bit.Cli.Tools;
using Spectre.Console;

namespace Bit.Cli.Commands;

public static class ApiCommand
{
    private const int ShownFindings = 15;

    public static Command Create(Func<CliServices> services)
    {
        return new Command("api", "Keep a bit Boilerplate project's api working for the app versions its users still run: openapi/<version>.json keeps the OpenAPI document of each release, and the api is compared with every one the server still supports.")
        {
            Check(services),
            Release(services)
        };
    }

    private static Command Check(Func<CliServices> services)
    {
        var noBuildOption = NoBuildOption();
        var releaseOption = new Option<bool>("--release") { Description = "Also require openapi/<version>.json, for the version in src/Directory.Build.props, to describe this api exactly, as it must on a commit Production deploys." };

        var command = new Command("check", "Build Server.Api, which writes openapi/current.json, and fail if it breaks the apps of a released version the server still supports: one at or above the lowest SupportedAppVersions minimum in Server.Api's appsettings.json, or the start of the server's major version where none is set.")
        {
            noBuildOption,
            releaseOption
        };

        command.SetAction((parseResult, cancellationToken) => RunAsync(services(), parseResult.GetValue(noBuildOption) is false, parseResult.GetValue(releaseOption), record: false, cancellationToken));

        return command;
    }

    private static Command Release(Func<CliServices> services)
    {
        var noBuildOption = NoBuildOption();

        var command = new Command("release", "Run once src/Directory.Build.props has the version a release ships: check the api, then keep its document as openapi/<version>.json and delete the documents of versions the server no longer supports. It changes nothing when the check fails.")
        {
            noBuildOption
        };

        command.SetAction((parseResult, cancellationToken) => RunAsync(services(), parseResult.GetValue(noBuildOption) is false, requireRecorded: false, record: true, cancellationToken));

        return command;
    }

    private static Option<bool> NoBuildOption() => new("--no-build") { Description = "Don't build Server.Api: compare the openapi/current.json its last Debug build wrote." };

    private static async Task<int> RunAsync(CliServices cli, bool build, bool requireRecorded, bool record, CancellationToken cancellationToken)
    {
        var directory = ProjectContext.FindProjectDirectory(cli.Environment.CurrentDirectory);

        if (directory is null || ProjectContext.FindProjectName(directory) is not { } name)
            return cli.UsageError("not-a-project", $"{cli.Environment.CurrentDirectory} isn't in a bit Boilerplate project: neither it nor a folder above it has a *.Web.slnf or *.slnx. Run bit api in the project's folder.");

        var api = new ApiCompatibility(cli, directory, name);

        if (File.Exists(api.ServerApiProject) is false)
            return cli.UsageError("server-api-missing", $"{api.Relative(api.ServerApiProject)} isn't there, and its api is what bit api checks.");

        var (version, versionProblem) = api.ReadVersion();

        if (version is null)
            return cli.UsageError("version-unreadable", versionProblem!);

        var (supported, settingsProblem) = api.ReadSupportedAppVersions(version);

        if (supported is null)
            return cli.UsageError("supported-versions-unreadable", settingsProblem!);

        cli.Console.Out.MarkupLine($"[bold]{Markup.Escape(name)}[/]'s api at version {version} [grey]in {Markup.Escape(directory)}[/]");
        cli.Console.Out.MarkupLine($"[grey]It supports the apps from {supported.Lowest} on: {Markup.Escape(string.Join(", ", supported.Platforms.Select(p => $"{p.Platform} {p.Minimum}")))}[/]");

        var steps = new StepRunner(cli);

        if (build)
        {
            var built = await steps.RunAsync("api-build", $"Building {name}.Server.Api", api.BuildAsync, cancellationToken);

            if (built.Status is StepStatus.Failed)
                return Finish(cli, steps, [], CliApp.ExitFailed);
        }
        else if (File.Exists(api.CurrentDocument) is false)
        {
            return cli.UsageError("current-document-missing", $"{api.Relative(api.CurrentDocument)} isn't there: build {name}.Server.Api in Debug, or run this without --no-build.");
        }

        var released = api.ReleasedVersions();
        var unsupported = released.Where(v => v < supported.Lowest).ToList();
        var compared = released.Where(v => v >= supported.Lowest).ToList();
        var comparisons = new List<ApiComparison>();
        var toolFailed = false;

        if (compared.Count == 0)
        {
            steps.Add("api-compare", StepResult.Skipped("Nothing to compare with yet", "no supported release's document"));
        }
        else
        {
            var oasdiff = Oasdiff.ExecutablePath(cli.Environment);

            if (File.Exists(oasdiff) is false)
            {
                var got = await steps.RunAsync("api-oasdiff", $"Downloading oasdiff {Oasdiff.Version}", async (_, token) =>
                {
                    using var handler = new HttpClientHandler();
                    await Oasdiff.EnsureAsync(cli.Environment, handler, token);
                    return StepResult.Succeeded($"Downloaded oasdiff {Oasdiff.Version}", Path.GetDirectoryName(oasdiff));
                }, cancellationToken);

                if (got.Status is StepStatus.Failed)
                    return Finish(cli, steps, [], CliApp.ExitFailed);
            }

            foreach (var releasedVersion in compared)
            {
                ApiComparison? comparison = null;

                await steps.RunAsync("api-compare", $"Comparing the api with {releasedVersion}'s", async (_, token) =>
                {
                    comparison = await api.CompareAsync(oasdiff, releasedVersion, token);

                    return comparison switch
                    {
                        { Errors.Count: > 0 } => StepResult.Failed($"Breaks the apps at {releasedVersion}", Count(comparison.Errors.Count, "breaking change")) with { Attempt = comparison.Attempt },
                        { Warnings.Count: > 0 } => StepResult.Warning($"Works with the apps at {releasedVersion}", Count(comparison.Warnings.Count, "warning")) with { Attempt = comparison.Attempt },
                        _ => StepResult.Succeeded($"Works with the apps at {releasedVersion}")
                    };
                }, cancellationToken);

                if (comparison is null)
                {
                    toolFailed = true;
                    continue;
                }

                comparisons.Add(comparison);
                WriteFindings(cli, comparison.Errors, cli.Console.FailSymbol, "red");
                WriteFindings(cli, comparison.Warnings, cli.Console.WarnSymbol, "yellow");
            }
        }

        if (unsupported.Count > 0 && record is false)
        {
            steps.Add("api-unsupported", StepResult.Skipped($"Didn't compare with {string.Join(", ", unsupported)}", $"below {supported.Lowest}, so bit api release deletes their documents"));
        }

        var broken = comparisons.Where(c => c.Errors.Count > 0).Select(c => c.Version).ToList();
        var recordedFile = api.Relative(api.DocumentOf(version));
        var refused = supported.Platforms.Where(p => p.Minimum > version).ToList();

        if ((requireRecorded || record) && refused.Count > 0)
        {
            steps.Add("api-minimum", StepResult.Failed($"The server would refuse the apps {version} ships",
                string.Join(", ", refused.Select(p => $"{p.Platform} needs {p.Minimum}")),
                $"Raise the version in {api.Relative(api.VersionFile)} to {refused.Max(p => p.Minimum)} or above, or lower SupportedAppVersions in {api.Relative(api.SettingsFile)}, then run this again."));
        }

        if (requireRecorded)
        {
            steps.Add("api-recorded", api.DescribesCurrentApi(version)
                ? StepResult.Succeeded($"{recordedFile} describes this api")
                : StepResult.Failed(File.Exists(api.DocumentOf(version)) ? $"{recordedFile} describes another api" : $"{recordedFile} isn't there",
                    "so no release recorded the api this commit runs",
                    $"Raise the version in {api.Relative(api.VersionFile)} and run bit api release before the change reaches the branch Production deploys from."));
        }

        if (record && broken.Count == 0 && refused.Count == 0 && toolFailed is false)
        {
            var recorded = Record(api, version, recordedFile);
            steps.Add("api-record", recorded);

            if (recorded.Status is not StepStatus.Failed && unsupported.Count > 0)
            {
                foreach (var old in unsupported)
                {
                    File.Delete(api.DocumentOf(old));
                }

                steps.Add("api-unsupported", StepResult.Succeeded($"Deleted the documents of {string.Join(", ", unsupported)}", $"below {supported.Lowest}, the lowest supported version"));
            }
        }

        var guidance = new List<string>();

        if (broken.Count > 0)
        {
            var newest = broken.Max()!;
            var lowestWorking = compared.Append(version).Where(v => v > newest).Min();
            var raise = lowestWorking is null
                ? $"Or raise SupportedAppVersions above {newest} in {api.Relative(api.SettingsFile)}, to the version the next release gives {api.Relative(api.VersionFile)},"
                : $"Or raise SupportedAppVersions to {lowestWorking} in {api.Relative(api.SettingsFile)},";

            guidance.Add($"Keep the api working for the apps at {string.Join(", ", broken)}: bring back what the list above names, or add the new shape next to the old one.");
            guidance.Add($"{raise} so those apps have to update before they call the server again{(record ? ", then run this again" : "")}.");
        }

        return Finish(cli, steps, guidance, toolFailed ? CliApp.ExitFailed : broken.Count > 0 || steps.AnyFailed ? CliApp.ExitCheckFailed : CliApp.ExitOk);
    }

    private static int Finish(CliServices cli, StepRunner steps, IEnumerable<string> guidance, int exitCode)
    {
        var lines = steps.Reports.Where(r => r.Result is { Status: StepStatus.Failed or StepStatus.Warning, FollowUp: not null }).Select(r => r.Result.FollowUp!).Concat(guidance).ToList();

        if (lines.Count > 0)
        {
            cli.Console.Out.WriteLine();
            cli.Console.Out.MarkupLine("  [bold]To go on[/]");

            foreach (var line in lines)
            {
                cli.Console.Out.MarkupLine($"    {Markup.Escape(line)}");
            }
        }

        return exitCode;
    }

    private static StepResult Record(ApiCompatibility api, Version version, string file)
    {
        if (File.Exists(api.DocumentOf(version)) is false)
        {
            File.Copy(api.CurrentDocument, api.DocumentOf(version));
            return StepResult.Succeeded($"Recorded the api of {version} in {file}");
        }

        return api.DescribesCurrentApi(version)
            ? StepResult.Succeeded($"{file} already describes this api")
            : StepResult.Failed($"{file} describes another api", "and a released version's document never changes",
                $"Raise the version in {api.Relative(api.VersionFile)}, or delete {file} if {version} was never released, then run this again.");
    }

    private static void WriteFindings(CliServices cli, IReadOnlyList<ApiFinding> findings, string symbol, string color)
    {
        foreach (var finding in findings.Take(ShownFindings))
        {
            cli.Console.Out.MarkupLine($"      [{color}]{symbol}[/] {Markup.Escape(finding.Text)} [grey]{Markup.Escape(finding.Id)}[/]");

            if (finding.Operations.Count > 0)
            {
                var shown = string.Join(", ", finding.Operations.Take(3));
                cli.Console.Out.MarkupLine($"        [grey]{Markup.Escape(finding.Operations.Count > 3 ? $"{shown} and {finding.Operations.Count - 3} more" : shown)}[/]");
            }
        }

        if (findings.Count > ShownFindings)
        {
            cli.Console.Out.MarkupLine($"      [grey]and {findings.Count - ShownFindings} more, which the command above lists[/]");
        }
    }

    private static string Count(int count, string what) => count == 1 ? $"1 {what}" : $"{count} {what}s";
}
