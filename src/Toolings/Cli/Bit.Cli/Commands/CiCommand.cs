using System.CommandLine;
using Bit.Cli.Projects;

namespace Bit.Cli.Commands;

public static class CiCommand
{
    public static Command Create(Func<CliServices> services)
    {
        var actArguments = new Argument<string[]>("act-arguments")
        {
            Description = "More arguments for act, after --, e.g. bit ci -- --list.",
            Arity = ArgumentArity.ZeroOrMore
        };

        var command = new Command("ci", "Run the project's CI workflow, .github/workflows/ci.yml, on this machine with act, with the tools bit setup installed here, without spending Actions minutes.")
        {
            actArguments
        };

        command.SetAction(async (parseResult, cancellationToken) =>
        {
            var cli = services();
            var directory = cli.Environment.CurrentDirectory;

            if (ProjectContext.FindProjectName(directory) is null)
            {
                return cli.UsageError("not-a-project", $"{directory} isn't a bit Boilerplate project folder (no *.Web.slnf or *.slnx). Run bit ci in the project's folder.");
            }

            var ci = new LocalCi(cli, directory);

            if (File.Exists(ci.Workflow) is false)
            {
                return cli.UsageError("ci-workflow-missing", $"The project has no {Path.GetRelativePath(directory, ci.Workflow)} for bit ci to run.");
            }

            if (LocalCi.Labels(await File.ReadAllTextAsync(ci.Workflow, cancellationToken)) is null)
            {
                return cli.UsageError("ci-runs-on", $"bit ci runs each job of {Path.GetRelativePath(directory, ci.Workflow)} on this machine, so its runs-on has to be a runner label, such as ubuntu-24.04, or a list of them: act skips a job whose runs-on is an expression or another form, and the run would pass without it.");
            }

            return await ci.RunAsync(parseResult.GetValue(actArguments) ?? [], cancellationToken);
        });

        return command;
    }
}
