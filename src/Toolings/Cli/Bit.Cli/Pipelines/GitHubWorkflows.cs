using System.Text.RegularExpressions;
using YamlDotNet.Core;
using YamlDotNet.RepresentationModel;

namespace Bit.Cli.Pipelines;

public sealed record WorkflowSecret(string Name, string? Environment, IReadOnlyList<string> UsedBy);

public sealed record WorkflowScan(IReadOnlyList<WorkflowSecret> Secrets, IReadOnlyList<string> Notes, IReadOnlyList<string> Texts)
{
    public IReadOnlyList<string> Environments => [.. Secrets.Select(s => s.Environment).OfType<string>().Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal)];
}

public sealed partial class GitHubWorkflows(string repositoryRoot, Func<string, CancellationToken, Task<string?>>? fetchRemote = null)
{
    private const int MaxDepth = 10;

    private readonly Dictionary<(string Name, string? Environment), SortedSet<string>> found = [];
    private readonly List<string> notes = [];
    private readonly Dictionary<string, (YamlMappingNode? Workflow, string? Text)> loaded = new(StringComparer.Ordinal);

    public async Task<WorkflowScan> ScanAsync(CancellationToken cancellationToken)
    {
        var directory = Path.Combine(repositoryRoot, ".github", "workflows");

        if (Directory.Exists(directory) is false)
            return new([], [], []);

        var files = Directory.EnumerateFiles(directory)
            .Where(f => f.EndsWith(".yml", StringComparison.OrdinalIgnoreCase) || f.EndsWith(".yaml", StringComparison.OrdinalIgnoreCase))
            .Select(f => $".github/workflows/{Path.GetFileName(f)}")
            .Order(StringComparer.Ordinal);

        foreach (var file in files)
        {
            if (await LoadAsync(file, cancellationToken) is { } workflow && IsTriggeredOnItsOwn(workflow))
            {
                await VisitAsync(file, workflow, new Dictionary<string, string?>(), name => [name], [], cancellationToken);
            }
        }

        var secrets = found
            .Select(f => new WorkflowSecret(f.Key.Name, f.Key.Environment, [.. f.Value]))
            .OrderBy(s => s.Environment ?? "", StringComparer.Ordinal)
            .ThenBy(s => s.Name, StringComparer.Ordinal)
            .ToList();

        return new(secrets, [.. notes.Distinct(StringComparer.Ordinal)], [.. loaded.Values.Select(l => l.Text).OfType<string>()]);
    }

    private async Task VisitAsync(string file, YamlMappingNode workflow, IReadOnlyDictionary<string, string?> given, Func<string, IEnumerable<string>> passedSecrets, IReadOnlyList<string> chain, CancellationToken cancellationToken)
    {
        if (chain.Count >= MaxDepth || chain.Contains(file, StringComparer.Ordinal))
        {
            notes.Add($"{file} calls itself through {string.Join(" > ", chain)}, so bit stopped following it");
            return;
        }

        var inputs = InputDefaults(workflow);

        foreach (var (key, value) in given)
        {
            inputs[key] = value;
        }

        var workflowSecrets = workflow.Children
            .Where(c => Scalar(c.Key) is not "jobs" and not "on")
            .SelectMany(c => SecretsIn(c.Value))
            .ToList();

        if (workflow.Children.TryGetValue(new YamlScalarNode("jobs"), out var jobsNode) is false || jobsNode is not YamlMappingNode jobs)
            return;

        foreach (var (jobKey, jobNode) in jobs.Children)
        {
            if (jobNode is not YamlMappingNode job)
                continue;

            var jobId = Scalar(jobKey) ?? "";

            if (Child(job, "uses") is { } uses)
            {
                await CallAsync(file, jobId, uses, job, inputs, passedSecrets, [.. chain, file], cancellationToken);
                continue;
            }

            var names = SecretsIn(job).Concat(workflowSecrets).Distinct(StringComparer.Ordinal).ToList();

            if (names.Count == 0)
                continue;

            var (environment, unresolved) = EnvironmentOf(job, inputs);

            if (unresolved is not null)
            {
                notes.Add($"{file} job {jobId} deploys to the environment {unresolved}, which bit can't tell before it runs, so its secrets are listed as the repository's");
            }

            foreach (var name in names)
            {
                foreach (var stored in environment is null ? passedSecrets(name) : [name])
                {
                    Add(stored, environment, $"{file} > {jobId}");
                }
            }
        }
    }

    private async Task CallAsync(string file, string jobId, string uses, YamlMappingNode job, IReadOnlyDictionary<string, string?> inputs, Func<string, IEnumerable<string>> passedSecrets, IReadOnlyList<string> chain, CancellationToken cancellationToken)
    {
        var target = uses.StartsWith("./", StringComparison.Ordinal) ? uses[2..] : uses;

        if (await LoadAsync(target, cancellationToken) is not { } callee)
        {
            notes.Add($"{file} job {jobId} calls {uses}, which bit couldn't read, so the secrets it needs aren't listed");
            return;
        }

        var given = new Dictionary<string, string?>(StringComparer.Ordinal);

        if (job.Children.TryGetValue(new YamlScalarNode("with"), out var withNode) && withNode is YamlMappingNode with)
        {
            foreach (var (key, value) in with.Children)
            {
                if (Scalar(key) is { } name)
                {
                    given[name] = Scalar(value) is { } text ? Resolve(text, inputs) : null;
                }
            }
        }

        Func<string, IEnumerable<string>> calleeSecrets = job.Children.TryGetValue(new YamlScalarNode("secrets"), out var secretsNode) switch
        {
            true when Scalar(secretsNode) is "inherit" => passedSecrets,
            true when secretsNode is YamlMappingNode mapping => name => mapping.Children
                .Where(c => Scalar(c.Key) == name)
                .SelectMany(c => SecretsIn(c.Value))
                .SelectMany(passedSecrets),
            _ => _ => []
        };

        await VisitAsync(target, callee, given, calleeSecrets, chain, cancellationToken);
    }

    private async Task<YamlMappingNode?> LoadAsync(string path, CancellationToken cancellationToken)
    {
        if (loaded.TryGetValue(path, out var cached))
            return cached.Workflow;

        string? text = null;

        try
        {
            if (path.Contains('@', StringComparison.Ordinal))
            {
                text = fetchRemote is null ? null : await fetchRemote(path, cancellationToken);
            }
            else
            {
                var root = Path.GetFullPath(repositoryRoot);
                var full = Path.GetFullPath(Path.Combine(root, path));
                text = full.StartsWith(root, StringComparison.Ordinal) && File.Exists(full) ? await File.ReadAllTextAsync(full, cancellationToken) : null;
            }
        }
        catch (IOException exp)
        {
            notes.Add($"couldn't read {path}: {exp.Message}");
        }

        YamlMappingNode? workflow = null;

        if (text is not null)
        {
            try
            {
                var stream = new YamlStream();
                stream.Load(new StringReader(text));
                workflow = stream.Documents.FirstOrDefault()?.RootNode as YamlMappingNode;
            }
            catch (YamlException exp)
            {
                notes.Add($"{path} isn't valid YAML (line {exp.Start.Line}: {exp.Message}), so bit skipped it");
            }
            catch (InvalidOperationException exp)
            {
                notes.Add($"{path} isn't valid YAML ({exp.Message}), so bit skipped it");
            }
        }

        loaded[path] = (workflow, text);
        return workflow;
    }

    private void Add(string name, string? environment, string usedBy)
    {
        if (string.Equals(name, "GITHUB_TOKEN", StringComparison.OrdinalIgnoreCase))
            return;

        if (found.TryGetValue((name, environment), out var users) is false)
        {
            found[(name, environment)] = users = new(StringComparer.Ordinal);
        }

        users.Add(usedBy);
    }

    private static bool IsTriggeredOnItsOwn(YamlMappingNode workflow)
    {
        if (workflow.Children.TryGetValue(new YamlScalarNode("on"), out var on) is false)
            return false;

        IEnumerable<string?> triggers = on switch
        {
            YamlScalarNode scalar => [scalar.Value],
            YamlSequenceNode sequence => sequence.Children.Select(Scalar),
            YamlMappingNode mapping => mapping.Children.Keys.Select(Scalar),
            _ => []
        };

        return triggers.Any(t => t is not null and not "workflow_call");
    }

    private static Dictionary<string, string?> InputDefaults(YamlMappingNode workflow)
    {
        var defaults = new Dictionary<string, string?>(StringComparer.Ordinal);

        if (workflow.Children.TryGetValue(new YamlScalarNode("on"), out var on) is false || on is not YamlMappingNode triggers)
            return defaults;

        foreach (var trigger in new[] { "workflow_call", "workflow_dispatch" })
        {
            if (triggers.Children.TryGetValue(new YamlScalarNode(trigger), out var node) && node is YamlMappingNode mapping
                && mapping.Children.TryGetValue(new YamlScalarNode("inputs"), out var inputsNode) && inputsNode is YamlMappingNode inputs)
            {
                foreach (var (key, value) in inputs.Children)
                {
                    if (Scalar(key) is { } name && defaults.ContainsKey(name) is false)
                    {
                        defaults[name] = value is YamlMappingNode input ? Child(input, "default") : null;
                    }
                }
            }
        }

        return defaults;
    }

    private static (string? Environment, string? Unresolved) EnvironmentOf(YamlMappingNode job, IReadOnlyDictionary<string, string?> inputs)
    {
        if (job.Children.TryGetValue(new YamlScalarNode("environment"), out var node) is false)
            return (null, null);

        var text = node is YamlMappingNode mapping ? Child(mapping, "name") : Scalar(node);

        if (string.IsNullOrWhiteSpace(text))
            return (null, null);

        return Resolve(text, inputs) is { Length: > 0 } environment ? (environment, null) : (null, text.Trim());
    }

    private static string? Resolve(string text, IReadOnlyDictionary<string, string?> inputs)
    {
        if (text.Contains("${{", StringComparison.Ordinal) is false)
            return text.Trim();

        return InputExpressionRegex().Match(text.Trim()) is { Success: true } match && inputs.TryGetValue(match.Groups["name"].Value, out var value) ? value : null;
    }

    private static IEnumerable<string> SecretsIn(YamlNode node)
    {
        switch (node)
        {
            case YamlScalarNode { Value: { } value }:
                foreach (Match expression in ExpressionRegex().Matches(value))
                {
                    foreach (Match secret in SecretRegex().Matches(expression.Groups["body"].Value))
                    {
                        yield return secret.Groups["dot"].Success ? secret.Groups["dot"].Value : secret.Groups["index"].Value;
                    }
                }
                break;

            case YamlSequenceNode sequence:
                foreach (var name in sequence.Children.SelectMany(SecretsIn))
                {
                    yield return name;
                }
                break;

            case YamlMappingNode mapping:
                foreach (var name in mapping.Children.SelectMany(c => SecretsIn(c.Key).Concat(SecretsIn(c.Value))))
                {
                    yield return name;
                }
                break;
        }
    }

    private static string? Child(YamlMappingNode mapping, string key) => mapping.Children.TryGetValue(new YamlScalarNode(key), out var value) ? Scalar(value) : null;

    private static string? Scalar(YamlNode node) => (node as YamlScalarNode)?.Value;

    [GeneratedRegex(@"\$\{\{(?<body>.*?)\}\}", RegexOptions.Singleline)]
    private static partial Regex ExpressionRegex();

    [GeneratedRegex(@"\bsecrets\s*(\.\s*(?<dot>[A-Za-z_][A-Za-z0-9_-]*)|\[\s*['""](?<index>[^'""]+)['""]\s*\])")]
    private static partial Regex SecretRegex();

    [GeneratedRegex(@"^\$\{\{\s*(github\.event\.)?inputs\.(?<name>[A-Za-z_][A-Za-z0-9_-]*)\s*\}\}$")]
    private static partial Regex InputExpressionRegex();
}
