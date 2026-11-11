using System.CommandLine;
using Bit.Cli.Pipelines;

namespace Bit.Cli.Commands;

public static class PipelineCommand
{
    public static Command Create(Func<CliServices> services)
    {
        var environmentOption = new Option<string[]>("--env")
        {
            Description = "Only these GitHub environments. Default: every environment the workflows deploy to.",
            AllowMultipleArgumentsPerToken = true,
            HelpName = "Production,Test"
        };

        var checkOption = new Option<bool>("--check") { Description = "Only list the secrets and the repository's settings, and exit with code 3 when one the workflows need is missing or a setting is off." };
        var yesOption = new Option<bool>("--yes", "-y") { Description = "Store the secrets bit creates and turn on the repository's settings without asking, and skip the secrets only you can give." };
        var rotateOption = new Option<bool>("--rotate-certificate") { Description = "Give each environment a new app certificate, keeping the old one as APP_CERTIFICATE_RETIRED so nobody is signed out." };

        var command = new Command("pipeline", "Fill in the secrets your GitHub Actions workflows use: bit reads them from the workflows, creates the environments they deploy to, creates the keys it can and asks for the rest. It also locks down the repository: only collaborators open pull requests, the workflows' token is read-only, Production deploys from main only and Dependabot alerts are on.")
        {
            environmentOption,
            checkOption,
            yesOption,
            rotateOption
        };

        command.SetAction(async (parseResult, cancellationToken) =>
        {
            var options = new PipelineOptions(
                SharedOptions.SplitList(parseResult.GetValue(environmentOption)),
                parseResult.GetValue(checkOption),
                parseResult.GetValue(yesOption),
                parseResult.GetValue(rotateOption));

            return await new PipelineSetup(services(), options).RunAsync(cancellationToken);
        });

        return command;
    }
}
