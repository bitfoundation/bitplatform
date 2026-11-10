using System.Text.Json.Nodes;
using Bit.Cli.Infrastructure;
using Spectre.Console;

namespace Bit.Cli.Pipelines;

public sealed record GhCall(string[] Arguments, string? Input = null);

public sealed record RepositorySetting(string Name, bool On, string Done, IReadOnlyList<GhCall> Changes);

public sealed partial class PipelineSetup
{
    public const string ProductionBranch = "main";

    public static readonly string[] RepositoryDefaults = ["-X", "PATCH", "-f", "pull_request_creation_policy=collaborators_only", "--silent"];

    private bool isPublic;
    private bool settingsUnread;

    private async Task<IReadOnlyList<RepositorySetting>> ReadSettingsAsync(IReadOnlyList<string> environments, CancellationToken cancellationToken)
    {
        var settings = new List<RepositorySetting>();
        var details = await Gh(["api", $"repos/{repository}", "--jq", ".visibility, .pull_request_creation_policy"], cancellationToken);
        var workflow = await Gh(["api", $"repos/{repository}/actions/permissions/workflow", "--jq", ".default_workflow_permissions, .can_approve_pull_request_reviews"], cancellationToken);

        if (details.Succeeded is false || workflow.Succeeded is false)
        {
            cli.Console.Warn($"Couldn't read the pull request and Actions settings of {repository}: {LastLine(details.Succeeded ? workflow : details)}. Changing them needs admin rights.");
            settingsUnread = true;
            return settings;
        }

        if (Lines(details) is [var visibility, var policy])
        {
            isPublic = visibility is "public";

            if (policy is not "null")
            {
                settings.Add(new("Pull requests from collaborators only", policy is "collaborators_only", "Only collaborators can open pull requests",
                    [new(["api", $"repos/{repository}", "-X", "PATCH", "-f", "pull_request_creation_policy=collaborators_only", "--silent"])]));
            }
        }

        if (Lines(workflow) is [var permissions, var canApprove])
        {
            settings.Add(new("Read-only GITHUB_TOKEN that can't approve pull requests", permissions is "read" && canApprove is "false", "The workflows' GITHUB_TOKEN is read-only and can't approve pull requests",
                [new(["api", "-X", "PUT", $"repos/{repository}/actions/permissions/workflow", "-f", "default_workflow_permissions=read", "-F", "can_approve_pull_request_reviews=false", "--silent"])]));
        }

        if (isPublic && (await Gh(["api", $"repos/{repository}/actions/permissions/fork-pr-contributor-approval", "--jq", ".approval_policy"], cancellationToken)) is { Succeeded: true } approval && approval.Output.Trim() is { Length: > 0 } approvalPolicy)
        {
            settings.Add(new("Approval for every outside contributor's workflows", approvalPolicy is "all_external_contributors", "Every outside contributor's workflows wait for approval",
                [new(["api", "-X", "PUT", $"repos/{repository}/actions/permissions/fork-pr-contributor-approval", "-f", "approval_policy=all_external_contributors", "--silent"])]));
        }

        var alerts = await Gh(["api", $"repos/{repository}/vulnerability-alerts", "--silent"], cancellationToken);

        if (alerts.Succeeded || alerts.Output.Contains("HTTP 404", StringComparison.Ordinal))
        {
            settings.Add(new("Dependabot alerts", alerts.Succeeded, "Dependabot alerts about vulnerable dependencies are on",
                [new(["api", "-X", "PUT", $"repos/{repository}/vulnerability-alerts", "--silent"])]));
        }

        if (environments.FirstOrDefault(e => string.Equals(e, "Production", StringComparison.OrdinalIgnoreCase)) is { } production && await ProductionBranchRuleAsync(production, cancellationToken) is { } rule)
        {
            settings.Add(rule);
        }

        return settings;
    }

    private async Task<RepositorySetting?> ProductionBranchRuleAsync(string environment, CancellationToken cancellationToken)
    {
        var path = $"repos/{repository}/environments/{Uri.EscapeDataString(environment)}";
        var current = await Gh(["api", path], cancellationToken);
        JsonObject? existing = null;

        if (current.Succeeded)
        {
            try
            {
                existing = JsonNode.Parse(current.Output) as JsonObject;
            }
            catch (System.Text.Json.JsonException)
            {
            }

            if (existing is null)
                return null;
        }
        else if (current.Output.Contains("HTTP 404", StringComparison.Ordinal) is false)
            return null;

        var custom = existing?["deployment_branch_policy"] is JsonObject branchPolicy && branchPolicy["custom_branch_policies"]?.GetValue<bool>() is true;
        var policies = new List<(string Id, string Type, string Name)>();

        if (custom)
        {
            var listed = await Gh(["api", $"{path}/deployment-branch-policies", "--paginate", "--jq", ".branch_policies[] | \"\\(.id) \\(.type) \\(.name)\""], cancellationToken);

            if (listed.Succeeded is false)
                return null;

            policies = [.. Lines(listed).Select(l => l.Split(' ', 3)).Where(p => p.Length == 3).Select(p => (p[0], p[1], p[2]))];
        }

        var hasMain = policies.Any(p => p is { Type: "branch", Name: ProductionBranch });
        var others = policies.Where(p => p is not { Type: "branch", Name: ProductionBranch }).ToList();
        var on = custom && hasMain && others.Count == 0;

        var body = new JsonObject { ["deployment_branch_policy"] = new JsonObject { ["protected_branches"] = false, ["custom_branch_policies"] = true } };

        foreach (var rule in existing?["protection_rules"] as JsonArray ?? [])
        {
            if (rule?["type"]?.GetValue<string>() is "wait_timer" && rule["wait_timer"] is { } waitTimer)
            {
                body["wait_timer"] = waitTimer.DeepClone();
            }
            else if (rule?["type"]?.GetValue<string>() is "required_reviewers" && rule["reviewers"] is JsonArray reviewers)
            {
                body["prevent_self_review"] = rule["prevent_self_review"]?.DeepClone();
                body["reviewers"] = new JsonArray([.. reviewers.Select(r => new JsonObject { ["type"] = r?["type"]?.DeepClone(), ["id"] = r?["reviewer"]?["id"]?.DeepClone() })]);
            }
        }

        List<GhCall> changes = [new(["api", "-X", "PUT", path, "--input", "-", "--silent"], body.ToJsonString())];
        changes.AddRange(others.Select(p => new GhCall(["api", "-X", "DELETE", $"{path}/deployment-branch-policies/{p.Id}", "--silent"])));

        if (hasMain is false)
        {
            changes.Add(new(["api", "-X", "POST", $"{path}/deployment-branch-policies", "-f", $"name={ProductionBranch}", "-f", "type=branch", "--silent"]));
        }

        return new($"{environment} deploys from {ProductionBranch} only", on, $"{environment} deploys from {ProductionBranch} only", changes);
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
            ProcessResult? failure = null;

            foreach (var change in setting.Changes)
            {
                var result = await Runner.RunAsync(new ProcessSpec { FileName = "gh", Arguments = change.Arguments, WorkingDirectory = root, StandardInput = change.Input, Timeout = TimeSpan.FromMinutes(2) }, cancellationToken);

                if (result.Succeeded is false)
                {
                    failure = result;
                    break;
                }
            }

            cli.Console.Step(failure is null ? StepStatus.Succeeded : StepStatus.Failed, failure is null ? setting.Done : $"Couldn't turn on: {setting.Name}", failure is null ? null : LastLine(failure));

            if (failure is not null)
            {
                cli.Console.StepAttempt(failure.Spec!.Attempt);
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
