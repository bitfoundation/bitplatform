using Bit.Cli.Infrastructure;
using Spectre.Console;

namespace Bit.Cli.Pipelines;

public sealed record RepositorySetting(string Name, bool On, string Done, string[] Change);

public sealed partial class PipelineSetup
{
    public static readonly string[] CollaboratorsOnly = ["-X", "PATCH", "-f", "pull_request_creation_policy=collaborators_only", "--silent"];

    private bool isPublic;

    private async Task<IReadOnlyList<RepositorySetting>> ReadSettingsAsync(CancellationToken cancellationToken)
    {
        var settings = new List<RepositorySetting>();
        var details = await Gh(["api", $"repos/{repository}", "--jq", ".visibility, .pull_request_creation_policy"], cancellationToken);
        var workflow = await Gh(["api", $"repos/{repository}/actions/permissions/workflow", "--jq", ".default_workflow_permissions, .can_approve_pull_request_reviews"], cancellationToken);

        if (details.Succeeded is false || workflow.Succeeded is false)
        {
            cli.Console.Warn($"Couldn't read the pull request and Actions settings of {repository}: {LastLine(details.Succeeded ? workflow : details)}. Changing them needs admin rights.");
            return settings;
        }

        if (Lines(details) is [var visibility, var policy] && policy is not "null")
        {
            isPublic = visibility is "public";
            settings.Add(new("Pull requests from collaborators only", policy is "collaborators_only", "Only collaborators can open pull requests",
                ["api", $"repos/{repository}", .. CollaboratorsOnly]));
        }

        if (Lines(workflow) is [var permissions, var canApprove])
        {
            settings.Add(new("Read-only GITHUB_TOKEN that can't approve pull requests", permissions is "read" && canApprove is "false", "The workflows' GITHUB_TOKEN is read-only and can't approve pull requests",
                ["api", "-X", "PUT", $"repos/{repository}/actions/permissions/workflow", "-f", "default_workflow_permissions=read", "-F", "can_approve_pull_request_reviews=false", "--silent"]));
        }

        // GitHub refuses this one on a private repository, where the workflows of a fork's pull request don't run anyway.
        if (isPublic && (await Gh(["api", $"repos/{repository}/actions/permissions/fork-pr-contributor-approval", "--jq", ".approval_policy"], cancellationToken)) is { Succeeded: true } approval && approval.Output.Trim() is { Length: > 0 } policyName)
        {
            settings.Add(new("Approval for every outside contributor's workflows", policyName is "all_external_contributors", "Every outside contributor's workflows wait for approval",
                ["api", "-X", "PUT", $"repos/{repository}/actions/permissions/fork-pr-contributor-approval", "-f", "approval_policy=all_external_contributors", "--silent"]));
        }

        return settings;
    }

    private void ShowSettings(IReadOnlyList<RepositorySetting> settings)
    {
        if (settings.Count == 0)
            return;

        var width = settings.Max(s => s.Name.Length) + 2;
        cli.Console.Out.MarkupLine("[bold]Repository settings[/]");

        foreach (var setting in settings)
        {
            cli.Console.Out.MarkupLine($"  {Markup.Escape(setting.Name.PadRight(width))}{(setting.On ? "[green]on[/]" : "[yellow]off[/] [grey]bit sets it[/]")}");
        }
    }

    private async Task ApplySettingsAsync(IEnumerable<RepositorySetting> settings, CancellationToken cancellationToken)
    {
        foreach (var setting in settings)
        {
            var result = await Gh(setting.Change, cancellationToken);
            cli.Console.Step(result.Succeeded ? StepStatus.Succeeded : StepStatus.Failed, result.Succeeded ? setting.Done : $"Couldn't turn on: {setting.Name}", result.Succeeded ? null : LastLine(result));

            if (result.Succeeded is false)
            {
                cli.Console.StepAttempt(result.Spec!.Attempt);
                failed = true;
            }
        }
    }

    private void WarnIfPublicWithSelfHostedRunner()
    {
        if (isPublic && workflowTexts.Any(t => t.Contains("self-hosted", StringComparison.Ordinal)))
        {
            cli.Console.Warn($"{repository} is public, and its workflows run jobs on a self-hosted runner, with everything that machine can reach. Make it private, or keep the deploy jobs in a separate private repository.");
        }
    }

    private static string[] Lines(ProcessResult result) => [.. result.OutputLines.Select(l => l.Trim()).Where(l => l.Length > 0)];
}
