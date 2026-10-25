namespace Bit.Blow;

/// <summary>How serious a validation issue is.</summary>
public enum BlowValidationSeverity
{
    /// <summary>Worth knowing; nothing is wrong.</summary>
    Info,

    /// <summary>Probably a mistake, but the graph can still be used.</summary>
    Warning,

    /// <summary>The graph is not valid: it cannot be saved, run or published as it is.</summary>
    Error,
}

/// <summary>
/// One problem a validation found, and where: the node, edge and port it concerns, so a UI can mark the element
/// and take the user to it.
/// </summary>
/// <param name="Code">A stable, machine-readable code; the built-in ones are listed in <see cref="BlowValidationCodes"/>.</param>
/// <param name="Severity">How serious the problem is.</param>
/// <param name="Message">An English description naming the elements involved. Localize by <see cref="Code"/>.</param>
public sealed record BlowValidationIssue(string Code, BlowValidationSeverity Severity, string Message)
{
    /// <summary>The node the issue concerns, if any.</summary>
    public string? NodeId { get; init; }

    /// <summary>The edge the issue concerns, if any.</summary>
    public string? EdgeId { get; init; }

    /// <summary>The id of the port the issue concerns (null for a node's unnamed port), when <see cref="PortType"/> is set.</summary>
    public string? PortId { get; init; }

    /// <summary>The type of the port the issue concerns, or null when it concerns no port.</summary>
    public BlowHandleType? PortType { get; init; }
}

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

/// <summary>What a validation rule is given: the graph as handed over, and an index over it.</summary>
public sealed class BlowValidationContext
{
    internal BlowValidationContext(IReadOnlyList<BlowNode> nodes, IReadOnlyList<BlowEdge> edges, CancellationToken cancellationToken)
    {
        Nodes = nodes;
        Edges = edges;
        Graph = new BlowGraphIndex(nodes, edges);
        CancellationToken = cancellationToken;
    }

    /// <summary>The nodes exactly as given, duplicates included - which is what a rule about duplicates needs.</summary>
    public IReadOnlyList<BlowNode> Nodes { get; }

    /// <summary>The edges exactly as given.</summary>
    public IReadOnlyList<BlowEdge> Edges { get; }

    /// <summary>Lookups and adjacency over the graph, for rules that ask about connections.</summary>
    public BlowGraphIndex Graph { get; }

    /// <summary>Signalled when the caller no longer wants the result; long rules should check it.</summary>
    public CancellationToken CancellationToken { get; }
}

/// <summary>A check over a graph that reports what it finds as issues.</summary>
public interface IBlowValidationRule
{
    /// <summary>The issues this rule finds in the graph; none when it is satisfied.</summary>
    IEnumerable<BlowValidationIssue> Validate(BlowValidationContext context);
}

/// <summary>
/// Checks a graph against a set of rules and reports every problem it finds, each tied to the node, edge or port
/// it concerns. Runs with no UI: validate before saving, before running a workflow, on a server receiving a graph,
/// or after every edit to mark problems on the canvas.
/// </summary>
/// <example>
/// <code>
/// var validator = BlowGraphValidator.CreateDefault()
///     .Add(BlowValidationRules.NoCycles())
///     .Add(BlowValidationRules.ForNodes("missing-owner", n =&gt; n.Type == "task" &amp;&amp; n.Data is null ? "Every task needs an owner." : null));
///
/// var result = validator.Validate(nodes, edges);
/// if (!result.IsValid) ShowProblems(result.Issues);
/// </code>
/// </example>
public sealed class BlowGraphValidator
{
    private readonly List<IBlowValidationRule> _rules;

    /// <summary>A validator with the given rules, and no others.</summary>
    public BlowGraphValidator(params IBlowValidationRule[] rules) => _rules = [.. rules];

    /// <summary>
    /// A validator with the rules every graph should pass: <see cref="BlowValidationRules.Structure"/> (unique
    /// ids, edges and parents that exist, no parent cycles, edges to declared ports) and
    /// <see cref="BlowValidationRules.Ports"/> (connection counts and data types of declared ports).
    /// </summary>
    /// <param name="mode">
    /// The canvas's <c>ConnectionMode</c>. In <see cref="BlowConnectionMode.Loose"/> mode any handle can be either end
    /// of an edge, so an edge may name a port of the other type at its end, and a port's connections are counted at
    /// both ends.
    /// </param>
    public static BlowGraphValidator CreateDefault(BlowConnectionMode mode = BlowConnectionMode.Strict)
        => new(BlowValidationRules.Structure(mode), BlowValidationRules.Ports(mode: mode));

    /// <summary>The rules, in the order they run.</summary>
    public IReadOnlyList<IBlowValidationRule> Rules => _rules;

    /// <summary>
    /// The most issues collected before the validation stops, so a hopelessly broken (or hostile) graph produces a
    /// bounded report in bounded time. 1,000 by default.
    /// </summary>
    public int MaxIssues { get; set; } = 1000;

    /// <summary>Adds a rule, and returns the validator for chaining.</summary>
    public BlowGraphValidator Add(IBlowValidationRule rule)
    {
        ArgumentNullException.ThrowIfNull(rule);
        _rules.Add(rule);
        return this;
    }

    /// <summary>Adds a rule written as a function, and returns the validator for chaining.</summary>
    public BlowGraphValidator Add(Func<BlowValidationContext, IEnumerable<BlowValidationIssue>> rule)
        => Add(new DelegateRule(rule ?? throw new ArgumentNullException(nameof(rule))));

    /// <summary>Validates a graph.</summary>
    /// <exception cref="OperationCanceledException">The token was signalled.</exception>
    public BlowValidationResult Validate(IEnumerable<BlowNode> nodes, IEnumerable<BlowEdge> edges,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(nodes);
        ArgumentNullException.ThrowIfNull(edges);

        var context = new BlowValidationContext(nodes.ToList(), edges.ToList(), cancellationToken);
        var issues = new List<BlowValidationIssue>();
        foreach (var rule in _rules)
        {
            cancellationToken.ThrowIfCancellationRequested();
            foreach (var issue in rule.Validate(context))
            {
                if (issues.Count >= MaxIssues) return new BlowValidationResult(issues, truncated: true);
                issues.Add(issue);
            }
        }
        return new BlowValidationResult(issues);
    }

    private sealed class DelegateRule(Func<BlowValidationContext, IEnumerable<BlowValidationIssue>> rule) : IBlowValidationRule
    {
        public IEnumerable<BlowValidationIssue> Validate(BlowValidationContext context) => rule(context);
    }
}

/// <summary>The <see cref="BlowValidationIssue.Code"/> values of the built-in rules.</summary>
public static class BlowValidationCodes
{
    /// <summary>Two or more nodes share an id.</summary>
    public const string DuplicateNodeId = BlowErrorCode.DuplicateNodeId;

    /// <summary>Two or more edges share an id.</summary>
    public const string DuplicateEdgeId = BlowErrorCode.DuplicateEdgeId;

    /// <summary>An edge names a source or target node that is not in the graph.</summary>
    public const string MissingEdgeEndpoint = BlowErrorCode.MissingEdgeEndpoint;

    /// <summary>A node names a parent that is not in the graph.</summary>
    public const string MissingParentNode = BlowErrorCode.MissingParentNode;

    /// <summary>Following parents from a node leads back to it.</summary>
    public const string ParentCycle = BlowErrorCode.ParentCycle;

    /// <summary>An edge names a handle that its node, which declares ports, does not declare.</summary>
    public const string UnknownPort = "unknown-port";

    /// <summary>A port has fewer edges than its <c>MinConnections</c>.</summary>
    public const string TooFewConnections = "too-few-connections";

    /// <summary>A port has more edges than its <c>MaxConnections</c>.</summary>
    public const string TooManyConnections = "too-many-connections";

    /// <summary>An edge joins two ports whose data types are not compatible.</summary>
    public const string IncompatibleTypes = "incompatible-types";

    /// <summary>The graph has a cycle where it must not.</summary>
    public const string Cycle = "cycle";

    /// <summary>A node has no edges at all.</summary>
    public const string IsolatedNode = "isolated-node";

    /// <summary>A node cannot be reached from any start node.</summary>
    public const string UnreachableNode = "unreachable-node";
}

/// <summary>Ready-made validation rules, and helpers for writing your own.</summary>
public static class BlowValidationRules
{
    /// <summary>
    /// The structural rules: unique node and edge ids, edges whose endpoints exist, parents that exist and do not
    /// form a cycle, and - for a node that declares <see cref="BlowNode.Ports"/> - edges that name ports it declares.
    /// All are errors.
    /// </summary>
    /// <param name="mode">The canvas's <c>ConnectionMode</c>; in loose mode an edge may name a port of either type.</param>
    public static IBlowValidationRule Structure(BlowConnectionMode mode = BlowConnectionMode.Strict)
        => new DelegateRule(context => StructureIssues(context, mode));

    /// <summary>
    /// The declared port an edge's end names: one of the end's own type, or in loose mode - where any handle can be
    /// either end - one of the other type with the same id.
    /// </summary>
    internal static BlowPort? PortAt(BlowNode node, BlowHandleType end, string? handleId, BlowConnectionMode mode)
        => node.FindPort(end, handleId)
           ?? (mode == BlowConnectionMode.Loose
               ? node.FindPort(end == BlowHandleType.Source ? BlowHandleType.Target : BlowHandleType.Source, handleId)
               : null);

    private static IEnumerable<BlowValidationIssue> StructureIssues(BlowValidationContext context, BlowConnectionMode mode)
    {
        const BlowValidationSeverity error = BlowValidationSeverity.Error;
        var graph = context.Graph;

        var nodeIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (var n in context.Nodes)
        {
            if (!nodeIds.Add(n.Id))
                yield return new(BlowValidationCodes.DuplicateNodeId, error, $"More than one node uses the id '{n.Id}'.") { NodeId = n.Id };
        }

        var edgeIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (var e in context.Edges)
        {
            if (!edgeIds.Add(e.Id))
                yield return new(BlowValidationCodes.DuplicateEdgeId, error, $"More than one edge uses the id '{e.Id}'.") { EdgeId = e.Id };
        }

        foreach (var e in graph.DanglingEdges)
        {
            var missing = graph.ContainsNode(e.Source) ? e.Target : e.Source;
            yield return new(BlowValidationCodes.MissingEdgeEndpoint, error,
                $"Edge '{e.Id}' references node '{missing}', which is not in the graph.") { EdgeId = e.Id };
        }

        foreach (var n in graph.Nodes)
        {
            if (n.ParentId is { } parent && !graph.ContainsNode(parent))
            {
                yield return new(BlowValidationCodes.MissingParentNode, error,
                    $"Node '{n.Id}' names parent '{parent}', which is not in the graph.") { NodeId = n.Id };
            }
        }

        foreach (var n in ParentCycleMembers(graph))
        {
            context.CancellationToken.ThrowIfCancellationRequested();
            yield return new(BlowValidationCodes.ParentCycle, error,
                $"Node '{n.Id}' is its own ancestor: following its parents leads back to it.") { NodeId = n.Id };
        }

        foreach (var e in graph.Edges)
        {
            if (graph.GetNode(e.Source) is { Ports.Count: > 0 } source && PortAt(source, BlowHandleType.Source, e.SourceHandle, mode) is null)
            {
                yield return new(BlowValidationCodes.UnknownPort, error,
                    $"Edge '{e.Id}' leaves node '{e.Source}' from output '{e.SourceHandle ?? "(unnamed)"}', which the node does not declare.")
                    { EdgeId = e.Id, NodeId = e.Source, PortId = e.SourceHandle, PortType = BlowHandleType.Source };
            }

            if (graph.GetNode(e.Target) is { Ports.Count: > 0 } target && PortAt(target, BlowHandleType.Target, e.TargetHandle, mode) is null)
            {
                yield return new(BlowValidationCodes.UnknownPort, error,
                    $"Edge '{e.Id}' enters node '{e.Target}' at input '{e.TargetHandle ?? "(unnamed)"}', which the node does not declare.")
                    { EdgeId = e.Id, NodeId = e.Target, PortId = e.TargetHandle, PortType = BlowHandleType.Target };
            }
        }
    }

    /// <summary>The nodes that lie on a cycle of parent links, each once, in node order.</summary>
    internal static List<BlowNode> ParentCycleMembers(BlowGraphIndex graph)
    {
        // 0: not seen, 1: on the chain being walked, 2: settled.
        var state = new Dictionary<string, int>(StringComparer.Ordinal);
        var onCycle = new HashSet<string>(StringComparer.Ordinal);
        var chain = new List<BlowNode>();

        foreach (var start in graph.Nodes)
        {
            if (state.GetValueOrDefault(start.Id) != 0) continue;

            chain.Clear();
            var current = start;
            while (true)
            {
                state[current.Id] = 1;
                chain.Add(current);
                if (current.ParentId is not { } pid || graph.GetNode(pid) is not { } parent) break;

                var parentState = state.GetValueOrDefault(parent.Id);
                if (parentState == 1)
                {
                    // Walked back into the chain: everything from the parent onwards is the cycle.
                    for (var i = chain.FindIndex(n => ReferenceEquals(n, parent)); i < chain.Count; i++) onCycle.Add(chain[i].Id);
                    break;
                }
                if (parentState == 2) break;
                current = parent;
            }
            foreach (var n in chain) state[n.Id] = 2;
        }

        return graph.Nodes.Where(n => onCycle.Contains(n.Id)).ToList();
    }

    /// <summary>
    /// The port rules, for nodes that declare <see cref="BlowNode.Ports"/>: a port with fewer edges than its
    /// <c>MinConnections</c> (a required input left unconnected) or more than its <c>MaxConnections</c>, and an
    /// edge between two ports whose data types are not compatible. All are errors.
    /// </summary>
    /// <param name="typeCompatibility">
    /// The data-type rule; null for <see cref="BlowConnectionRules.AreTypesCompatible"/>. Pass the same function as
    /// the canvas's <c>ConnectionTypeCompatibility</c>.
    /// </param>
    /// <param name="mode">
    /// The canvas's <c>ConnectionMode</c>. In loose mode a port's edges are counted at both ends, as the canvas counts
    /// them, and an edge's end may name a port of the other type.
    /// </param>
    public static IBlowValidationRule Ports(Func<string?, string?, bool>? typeCompatibility = null,
        BlowConnectionMode mode = BlowConnectionMode.Strict)
        => new DelegateRule(context => PortIssues(context, typeCompatibility ?? BlowConnectionRules.AreTypesCompatible, mode));

    private static IEnumerable<BlowValidationIssue> PortIssues(BlowValidationContext context, Func<string?, string?, bool> compatible,
        BlowConnectionMode mode)
    {
        const BlowValidationSeverity error = BlowValidationSeverity.Error;
        var graph = context.Graph;

        foreach (var n in graph.Nodes)
        {
            if (n.Ports is not { Count: > 0 } ports) continue;
            foreach (var port in ports)
            {
                var count = graph.CountAt(n.Id, port.Type, port.Id);
                if (mode == BlowConnectionMode.Loose)
                {
                    var other = port.Type == BlowHandleType.Source ? BlowHandleType.Target : BlowHandleType.Source;
                    count += graph.CountAt(n.Id, other, port.Id);
                }
                var kind = port.Type == BlowHandleType.Source ? "output" : "input";
                var name = port.Label ?? port.Id ?? "(unnamed)";

                if (count < port.MinConnections)
                {
                    yield return new(BlowValidationCodes.TooFewConnections, error, port.MinConnections == 1
                        ? $"The {kind} '{name}' of node '{n.Id}' must be connected."
                        : $"The {kind} '{name}' of node '{n.Id}' needs at least {port.MinConnections} connections; it has {count}.")
                        { NodeId = n.Id, PortId = port.Id, PortType = port.Type };
                }

                if (port.MaxConnections is { } max && count > max)
                {
                    yield return new(BlowValidationCodes.TooManyConnections, error,
                        $"The {kind} '{name}' of node '{n.Id}' allows {max} connection(s); it has {count}.")
                        { NodeId = n.Id, PortId = port.Id, PortType = port.Type };
                }
            }
        }

        foreach (var e in graph.Edges)
        {
            if (graph.GetNode(e.Source) is not { } source || graph.GetNode(e.Target) is not { } target) continue;
            var sourcePort = PortAt(source, BlowHandleType.Source, e.SourceHandle, mode);
            var targetPort = PortAt(target, BlowHandleType.Target, e.TargetHandle, mode);
            if (sourcePort is null && targetPort is null) continue;

            if (!compatible(sourcePort?.DataType, targetPort?.DataType))
            {
                yield return new(BlowValidationCodes.IncompatibleTypes, error,
                    $"Edge '{e.Id}' joins '{sourcePort?.DataType}' data from node '{e.Source}' to an input of node '{e.Target}' that takes '{targetPort?.DataType}'.")
                    { EdgeId = e.Id };
            }
        }
    }

    /// <summary>Reports every node that lies on a directed cycle (self-loops included).</summary>
    public static IBlowValidationRule NoCycles(BlowValidationSeverity severity = BlowValidationSeverity.Error)
        => new DelegateRule(context => BlowGraphAlgorithms.FindCycles(context.Graph)
            .SelectMany(cycle => cycle.Select(n => new BlowValidationIssue(BlowValidationCodes.Cycle, severity,
                $"Node '{n.Id}' is on a cycle: {string.Join(" -> ", cycle.Select(c => c.Id))}.") { NodeId = n.Id })));

    /// <summary>Reports every node with no edges at all.</summary>
    public static IBlowValidationRule NoIsolatedNodes(BlowValidationSeverity severity = BlowValidationSeverity.Warning)
        => new DelegateRule(context => BlowGraphAlgorithms.GetIsolatedNodes(context.Graph)
            .Select(n => new BlowValidationIssue(BlowValidationCodes.IsolatedNode, severity,
                $"Node '{n.Id}' is not connected to anything.") { NodeId = n.Id }));

    /// <summary>
    /// Reports every node that cannot be reached from a start node - in a workflow, the steps that can never run.
    /// </summary>
    /// <param name="isStart">Which nodes count as starts; for instance <c>n =&gt; n.Type == "input"</c>.</param>
    /// <param name="severity">How serious an unreachable node is.</param>
    public static IBlowValidationRule AllReachableFrom(Func<BlowNode, bool> isStart,
        BlowValidationSeverity severity = BlowValidationSeverity.Warning)
    {
        ArgumentNullException.ThrowIfNull(isStart);
        return new DelegateRule(context =>
        {
            var starts = context.Graph.Nodes.Where(isStart).Select(n => n.Id).ToList();
            return BlowGraphAlgorithms.GetUnreachable(context.Graph, starts)
                .Select(n => new BlowValidationIssue(BlowValidationCodes.UnreachableNode, severity,
                    $"Node '{n.Id}' cannot be reached from any start node.") { NodeId = n.Id });
        });
    }

    /// <summary>
    /// A rule over each node: <paramref name="check"/> returns a message describing the problem, or null when the
    /// node is fine.
    /// </summary>
    public static IBlowValidationRule ForNodes(string code, Func<BlowNode, string?> check,
        BlowValidationSeverity severity = BlowValidationSeverity.Error)
    {
        ArgumentNullException.ThrowIfNull(code);
        ArgumentNullException.ThrowIfNull(check);
        return new DelegateRule(context => context.Graph.Nodes
            .Select(n => (Node: n, Message: check(n)))
            .Where(r => r.Message is not null)
            .Select(r => new BlowValidationIssue(code, severity, r.Message!) { NodeId = r.Node.Id }));
    }

    /// <summary>
    /// A rule over each edge whose two nodes exist: <paramref name="check"/> receives the edge with its source and
    /// target and returns a message describing the problem, or null when the edge is fine.
    /// </summary>
    public static IBlowValidationRule ForEdges(string code, Func<BlowEdge, BlowNode, BlowNode, string?> check,
        BlowValidationSeverity severity = BlowValidationSeverity.Error)
    {
        ArgumentNullException.ThrowIfNull(code);
        ArgumentNullException.ThrowIfNull(check);
        return new DelegateRule(context => context.Graph.Edges
            .Where(e => context.Graph.ContainsNode(e.Source) && context.Graph.ContainsNode(e.Target))
            .Select(e => (Edge: e, Message: check(e, context.Graph.GetNode(e.Source)!, context.Graph.GetNode(e.Target)!)))
            .Where(r => r.Message is not null)
            .Select(r => new BlowValidationIssue(code, severity, r.Message!) { EdgeId = r.Edge.Id }));
    }

    private sealed class DelegateRule(Func<BlowValidationContext, IEnumerable<BlowValidationIssue>> rule) : IBlowValidationRule
    {
        public IEnumerable<BlowValidationIssue> Validate(BlowValidationContext context) => rule(context);
    }
}
