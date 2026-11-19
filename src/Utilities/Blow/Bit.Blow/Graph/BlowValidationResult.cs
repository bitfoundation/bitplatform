namespace Bit.Blow;

/// <summary>What a validation found.</summary>
public sealed class BlowValidationResult
{
    /// <summary>A result with no issues.</summary>
    public static readonly BlowValidationResult Empty = new([], truncated: false);

    private readonly Dictionary<string, List<BlowValidationIssue>> _byNode = new(StringComparer.Ordinal);
    private readonly Dictionary<string, List<BlowValidationIssue>> _byEdge = new(StringComparer.Ordinal);

    /// <summary>Wraps a list of issues.</summary>
    public BlowValidationResult(IReadOnlyList<BlowValidationIssue> issues, bool truncated = false)
    {
        ArgumentNullException.ThrowIfNull(issues);
        Issues = issues;
        IsTruncated = truncated;
        foreach (var issue in issues)
        {
            if (issue.NodeId is { } node) Add(_byNode, node, issue);
            if (issue.EdgeId is { } edge) Add(_byEdge, edge, issue);
        }

        static void Add(Dictionary<string, List<BlowValidationIssue>> map, string key, BlowValidationIssue issue)
        {
            if (!map.TryGetValue(key, out var list)) map[key] = list = [];
            list.Add(issue);
        }
    }

    /// <summary>Every issue found, in the order the rules reported them.</summary>
    public IReadOnlyList<BlowValidationIssue> Issues { get; }

    /// <summary>True when the validator stopped collecting at its <see cref="BlowGraphValidator.MaxIssues"/>.</summary>
    public bool IsTruncated { get; }

    /// <summary>True when no issue is an error.</summary>
    public bool IsValid => !Issues.Any(i => i.Severity == BlowValidationSeverity.Error);

    /// <summary>The issues that concern the given node, including those about its ports.</summary>
    public IReadOnlyList<BlowValidationIssue> ForNode(string nodeId)
        => _byNode.TryGetValue(nodeId, out var list) ? list : [];

    /// <summary>The issues that concern the given edge.</summary>
    public IReadOnlyList<BlowValidationIssue> ForEdge(string edgeId)
        => _byEdge.TryGetValue(edgeId, out var list) ? list : [];

    /// <summary>The most serious severity among the node's issues, or null when it has none.</summary>
    public BlowValidationSeverity? SeverityOf(BlowNode node) => Highest(ForNode(node.Id));

    /// <summary>The most serious severity among the edge's issues, or null when it has none.</summary>
    public BlowValidationSeverity? SeverityOf(BlowEdge edge) => Highest(ForEdge(edge.Id));

    private static BlowValidationSeverity? Highest(IReadOnlyList<BlowValidationIssue> issues)
        => issues.Count == 0 ? null : issues.Max(i => i.Severity);
}
