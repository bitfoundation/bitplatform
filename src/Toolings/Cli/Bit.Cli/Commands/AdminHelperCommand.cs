using System.CommandLine;
using Bit.Cli.Tools;

namespace Bit.Cli.Commands;

public static class AdminHelperCommand
{
    public static Command Create(Func<CliServices> services)
    {
        var pipeOption = new Option<string>("--pipe") { Required = true, Description = "The named pipe the bit process that started this one listens on." };
        var parentOption = new Option<int>("--parent") { Required = true, Description = "That bit process's id; the pipe must belong to it." };

        var command = new Command(AdministratorSession.CommandName, "Run the administrator steps another bit process sends, in the elevated process Windows started for it.")
        {
            pipeOption,
            parentOption
        };

        command.Hidden = true;

        command.SetAction(async (parseResult, cancellationToken) => OperatingSystem.IsWindows()
            ? await AdministratorSession.ServeAsync(services(), parseResult.GetValue(pipeOption)!, parseResult.GetValue(parentOption), cancellationToken)
            : CliApp.ExitUsage);

        return command;
    }
}
