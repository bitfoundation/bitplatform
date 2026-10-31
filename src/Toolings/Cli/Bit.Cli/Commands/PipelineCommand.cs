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

        var checkOption = new Option<bool>("--check") { Description = "Only list the secrets, and exit with code 3 when one the workflows need is missing." };
        var yesOption = new Option<bool>("--yes", "-y") { Description = "Store the secrets bit creates without asking, and skip the ones only you can give." };
        var rotateOption = new Option<bool>("--rotate-certificate") { Description = "Give each environment a new app certificate, keeping the old one as APP_CERTIFICATE_RETIRED so nobody is signed out." };

        var command = new Command("pipeline", "Fill in the secrets your GitHub Actions workflows use: bit reads them from the workflows, stores them in the repository, creates the keys it can and asks for the rest.")
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
