using System.CommandLine;

namespace Bit.Cli.Commands;

public static class UpdateCommand
{
    public static Command Create(Func<CliServices> services)
    {
        var command = new Command("update", "Update bit to the newest version on nuget.org. bit new does it by itself.");

        command.SetAction(async (_, cancellationToken) => await services().SelfUpdate.UpdateAsync(cancellationToken));

        return command;
    }
}
