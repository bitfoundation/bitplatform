namespace Bit.Blow;

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
        => new BlowDelegateRule(context => StructureIssues(context, mode));

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
        => new BlowDelegateRule(context => PortIssues(context, typeCompatibility ?? BlowConnectionRules.AreTypesCompatible, mode));

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
                var count = graph.HandleCounts.At(n.Id, port.Type, port.Id, mode);
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
        => new BlowDelegateRule(context => BlowGraphAlgorithms.FindCycles(context.Graph)
            .SelectMany(cycle => cycle.Select(n => new BlowValidationIssue(BlowValidationCodes.Cycle, severity,
                $"Node '{n.Id}' is on a cycle: {string.Join(" -> ", cycle.Select(c => c.Id))}.") { NodeId = n.Id })));

    /// <summary>Reports every node with no edges at all.</summary>
    public static IBlowValidationRule NoIsolatedNodes(BlowValidationSeverity severity = BlowValidationSeverity.Warning)
        => new BlowDelegateRule(context => BlowGraphAlgorithms.GetIsolatedNodes(context.Graph)
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
        return new BlowDelegateRule(context =>
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
        return new BlowDelegateRule(context => context.Graph.Nodes
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
        return new BlowDelegateRule(context => context.Graph.Edges
            .Where(e => context.Graph.ContainsNode(e.Source) && context.Graph.ContainsNode(e.Target))
            .Select(e => (Edge: e, Message: check(e, context.Graph.GetNode(e.Source)!, context.Graph.GetNode(e.Target)!)))
            .Where(r => r.Message is not null)
            .Select(r => new BlowValidationIssue(code, severity, r.Message!) { EdgeId = r.Edge.Id }));
    }
}
