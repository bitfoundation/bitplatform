using System.CommandLine;
using Bit.Cli.Infrastructure;
using Bit.Cli.Projects;
using Bit.Cli.Templates;

namespace Bit.Cli.Commands;

public static class DiffCommand
{
    public static Command Create(Func<CliServices> services)
    {
        var fromArgument = new Argument<string?>("from") { Description = "The bit Boilerplate version the project comes from. Default: the one the project's bit.json records.", Arity = ArgumentArity.ZeroOrOne };
        var toArgument = new Argument<string?>("to") { Description = $"The version to compare it with. Default: this bit's own, {BuildInfo.Version}.", Arity = ArgumentArity.ZeroOrOne };
        var projectOption = new Option<string?>("--project") { Description = "The project whose bit.json gives the name, version and options. Default: the current folder. It isn't changed." };
        var nameOption = new Option<string?>("--name") { Description = "The name to create both versions with. Default: the project's own." };
        var outputOption = new Option<string?>("--output", "-o") { Description = "The folder of the git repository both versions are committed to. Default: <temp>/bit-diff/<name>-<from>-<to>." };
        var templatePackageOption = new Option<string?>("--template-package") { Description = "Compare with a local Bit.Boilerplate .nupkg, or the template's folder in a bitplatform checkout." };

        var command = new Command("diff", "Show what changed in bit Boilerplate between two versions: creates the project with each into one git repository, so the second commit is the upgrade.")
        {
            fromArgument,
            toArgument,
            projectOption,
            nameOption,
            outputOption,
            templatePackageOption
        };

        var templateOptions = new Dictionary<string, Option<string?>>(StringComparer.OrdinalIgnoreCase);

        foreach (var parameter in TemplateManifest.Embedded.Parameters)
        {
            var option = new Option<string?>($"--{parameter.Name}")
            {
                Arity = ArgumentArity.ZeroOrOne,
                Description = $"{TemplateLabels.For(parameter)}. Default: what the project's bit.json records."
            };

            templateOptions[parameter.Name] = option;
            command.Options.Add(option);
        }

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

            var request = new DiffRequest
            {
                From = parseResult.GetValue(fromArgument),
                To = parseResult.GetValue(toArgument),
                Project = parseResult.GetValue(projectOption),
                Name = parseResult.GetValue(nameOption),
                Output = parseResult.GetValue(outputOption),
                TemplatePackage = parseResult.GetValue(templatePackageOption),
                TemplateValues = values
            };

            return await new TemplateDiff(services()).RunAsync(request, cancellationToken);
        });

        return command;
    }
}
