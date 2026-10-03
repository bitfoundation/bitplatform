using System.CommandLine;
using Bit.Cli.Projects;
using Bit.Cli.Templates;

namespace Bit.Cli.Commands;

public sealed record NewRequest
{
    public string? Name { get; init; }

    public string? Output { get; init; }

    public IReadOnlyList<string> Platforms { get; init; } = [];

    public bool PlatformsGiven { get; init; }

    public IReadOnlyList<string> Tools { get; init; } = [];

    public bool ToolsGiven { get; init; }

    public string? Ide { get; init; }

    public string? TemplateVersion { get; init; }

    public string? TemplatePackage { get; init; }

    public bool Yes { get; init; }

    public bool NonInteractive { get; init; }

    public bool DryRun { get; init; }

    public bool NoSetup { get; init; }

    public bool NoTools { get; init; }

    public bool NoCertificate { get; init; }

    public bool NoGit { get; init; }

    public bool NoWorkloads { get; init; }

    public bool NoRestore { get; init; }

    public bool NoBuild { get; init; }

    public bool NoFormat { get; init; }

    public bool NoMigration { get; init; }

    public bool NoTrust { get; init; }

    public bool NoOpen { get; init; }

    public IReadOnlyDictionary<string, string> TemplateValues { get; init; } = new Dictionary<string, string>();

    public IReadOnlyList<string> ExtraTemplateArguments { get; init; } = [];
}

public static class NewCommand
{
    public static Command Create(Func<CliServices> services)
    {
        var manifest = TemplateManifest.Embedded;
        var shared = new SharedOptions();

        var nameArgument = new Argument<string?>("name") { Description = "The project name, e.g. Contoso.Shop.", Arity = ArgumentArity.ZeroOrOne };
        var outputOption = new Option<string?>("--output", "-o") { Description = "The folder to create the project in. Default: ./<name>." };
        var ideOption = new Option<string?>("--ide") { Description = "Open the project in this IDE when done: code, vs, rider or none.", HelpName = "code|vs|rider|none" };
        ideOption.AcceptOnlyFromAmong("code", "vs", "rider", "none");
        var templateVersionOption = new Option<string?>("--template-version") { Description = "The bit Boilerplate version to use. Default: the same as this CLI." };
        var templatePackageOption = new Option<string?>("--template-package") { Description = "Create the project from a local Bit.Boilerplate .nupkg, the way CI does." };
        var dryRunOption = new Option<bool>("--dry-run") { Description = "Show what would happen, and change nothing." };
        var noSetup = new Option<bool>("--no-setup") { Description = "Only create the project: no tools, workloads, restore or build. Run bit setup in its folder later." };
        var noCertificate = new Option<bool>("--no-certificate") { Description = "Keep the template's shared development certificate." };
        var noGit = new Option<bool>("--no-git") { Description = "Don't initialize git." };
        var noFormat = new Option<bool>("--no-format") { Description = "Don't run dotnet format." };
        var noMigration = new Option<bool>("--no-migration") { Description = "Don't add the initial EF Core migration." };
        var noTrust = new Option<bool>("--no-trust") { Description = "Don't mark the folder as trusted for VS Code and the AI coding tools." };
        var noOpen = new Option<bool>("--no-open") { Description = "Don't open an IDE." };

        var command = new Command("new", "Create a bit Boilerplate project and get it ready to run: tools, workloads, packages, build, git, migration, trust and IDE.")
        {
            nameArgument,
            outputOption,
            ideOption,
            templateVersionOption,
            templatePackageOption,
            dryRunOption,
            noSetup,
            noCertificate,
            noGit,
            noFormat,
            noMigration,
            noTrust,
            noOpen
        };

        shared.AddTo(command);

        var templateOptions = new Dictionary<string, Option<string?>>(StringComparer.OrdinalIgnoreCase);

        foreach (var parameter in manifest.Parameters)
        {
            var option = new Option<string?>($"--{parameter.Name}")
            {
                Arity = ArgumentArity.ZeroOrOne,
                Description = TemplateLabels.For(parameter) + (parameter.Type is TemplateParameterType.String ? "" : $". Default: {parameter.DefaultValue}."),
                HelpName = parameter.Type switch
                {
                    TemplateParameterType.Bool => "true|false",
                    TemplateParameterType.Choice => string.Join('|', parameter.Choices.Select(c => c.Value)),
                    _ => "value"
                }
            };

            templateOptions[parameter.Name] = option;
            command.Options.Add(option);
        }

        command.TreatUnmatchedTokensAsErrors = false;

        command.SetAction(async (parseResult, cancellationToken) =>
        {
            var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            foreach (var (name, option) in templateOptions)
            {
                if (parseResult.GetResult(option) is { } result)
                {
                    values[name] = result.Tokens.Count == 0 ? "true" : result.Tokens[0].Value;
                }
            }

            var skipSetup = parseResult.GetValue(noSetup);

            var request = new NewRequest
            {
                Name = parseResult.GetValue(nameArgument),
                Output = parseResult.GetValue(outputOption),
                Platforms = SharedOptions.SplitList(parseResult.GetValue(shared.Platforms)),
                PlatformsGiven = parseResult.GetResult(shared.Platforms) is not null,
                Tools = SharedOptions.SplitList(parseResult.GetValue(shared.Tools)),
                ToolsGiven = parseResult.GetResult(shared.Tools) is not null,
                Ide = parseResult.GetValue(ideOption),
                TemplateVersion = parseResult.GetValue(templateVersionOption),
                TemplatePackage = parseResult.GetValue(templatePackageOption),
                Yes = parseResult.GetValue(shared.Yes),
                NonInteractive = parseResult.GetValue(shared.NonInteractive),
                DryRun = parseResult.GetValue(dryRunOption),
                NoSetup = skipSetup,
                NoTools = skipSetup || parseResult.GetValue(shared.NoTools),
                NoCertificate = parseResult.GetValue(noCertificate),
                NoGit = parseResult.GetValue(noGit),
                NoWorkloads = skipSetup || parseResult.GetValue(shared.NoWorkloads),
                NoRestore = skipSetup || parseResult.GetValue(shared.NoRestore),
                NoBuild = skipSetup || parseResult.GetValue(shared.NoBuild),
                NoFormat = parseResult.GetValue(noFormat),
                NoMigration = parseResult.GetValue(noMigration),
                NoTrust = parseResult.GetValue(noTrust),
                NoOpen = parseResult.GetValue(noOpen),
                TemplateValues = values,
                ExtraTemplateArguments = [.. parseResult.UnmatchedTokens]
            };

            return await new NewWorkflow(services()).RunAsync(request, cancellationToken);
        });

        return command;
    }
}
