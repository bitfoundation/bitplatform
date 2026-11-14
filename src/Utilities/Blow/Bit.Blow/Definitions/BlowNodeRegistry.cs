namespace Bit.Blow;

/// <summary>
/// The node kinds an editor knows (<see cref="BlowNodeDefinition"/>): a catalog to search, a factory for new nodes, a
/// validation rule for the ones in a graph, the handlers a <see cref="BlowRunner"/> runs them with, and the upgrade of
/// kinds that have been replaced.
/// </summary>
/// <remarks>
/// Definitions can be added and removed while the editor runs - a plugin loading - and <see cref="Changed"/> says so.
/// Each change replaces the registry's lists rather than editing them, so reading it from any thread while another
/// registers - a run looking up handlers - always sees one whole version. Registering from several threads at once is
/// serialized.
/// </remarks>
public sealed class BlowNodeRegistry
{
    private readonly object _gate = new();
    private volatile State _state = new([], new Dictionary<string, BlowNodeDefinition>(StringComparer.Ordinal));

    private sealed record State(List<BlowNodeDefinition> Definitions, Dictionary<string, BlowNodeDefinition> ByType);

    private List<BlowNodeDefinition> List => _state.Definitions;

    private Dictionary<string, BlowNodeDefinition> Map => _state.ByType;

    /// <summary>Raised after a definition is registered or removed.</summary>
    public event Action<BlowNodeRegistry>? Changed;

    /// <summary>Every definition, in the order registered.</summary>
    public IReadOnlyList<BlowNodeDefinition> Definitions => List;

    /// <summary>The categories of the definitions a catalog offers, in the order they first appear.</summary>
    public IReadOnlyList<string> Categories => [.. List
        .Where(d => !d.Hidden && d.Deprecated is null && !string.IsNullOrWhiteSpace(d.Category))
        .Select(d => d.Category!)
        .Distinct(StringComparer.Ordinal)];

    /// <summary>Adds a definition, or replaces the one of the same type in its place.</summary>
    public BlowNodeRegistry Register(BlowNodeDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);
        ArgumentException.ThrowIfNullOrEmpty(definition.Type);
        lock (_gate)
        {
            var state = _state;
            var definitions = new List<BlowNodeDefinition>(state.Definitions);
            var byType = new Dictionary<string, BlowNodeDefinition>(state.ByType, StringComparer.Ordinal);
            if (byType.TryGetValue(definition.Type, out var old)) definitions[definitions.IndexOf(old)] = definition;
            else definitions.Add(definition);
            byType[definition.Type] = definition;
            _state = new State(definitions, byType);
        }
        Changed?.Invoke(this);
        return this;
    }

    /// <summary>Adds several definitions.</summary>
    public BlowNodeRegistry Register(params IEnumerable<BlowNodeDefinition> definitions)
    {
        ArgumentNullException.ThrowIfNull(definitions);
        foreach (var definition in definitions) Register(definition);
        return this;
    }

    /// <summary>Removes the definition of a type. Nodes of the type stay; validation reports them as unknown.</summary>
    public bool Unregister(string type)
    {
        lock (_gate)
        {
            var state = _state;
            if (!state.ByType.TryGetValue(type, out var definition)) return false;
            var byType = new Dictionary<string, BlowNodeDefinition>(state.ByType, StringComparer.Ordinal);
            byType.Remove(type);
            _state = new State([.. state.Definitions.Where(d => !ReferenceEquals(d, definition))], byType);
        }
        Changed?.Invoke(this);
        return true;
    }

    /// <summary>The definition of a type, or null.</summary>
    public BlowNodeDefinition? Find(string? type) => type is null ? null : Map.GetValueOrDefault(type);

    /// <summary>The definition of a node's type, or null.</summary>
    public BlowNodeDefinition? Find(BlowNode node) => Find(node?.Type);

    /// <summary>True when the type is registered.</summary>
    public bool Contains(string type) => Map.ContainsKey(type);

    /// <summary>
    /// The definitions a catalog offers that match a query - by label, type, category, tags and description, ignoring
    /// case and accents, best matches first. An empty query lists them all. Hidden and deprecated kinds are left out.
    /// </summary>
    /// <param name="query">What was typed.</param>
    /// <param name="category">Only this category, or null for all.</param>
    /// <param name="filter">Any other condition: what fits where the node would go, what the user may add.</param>
    public IReadOnlyList<BlowNodeDefinition> Search(string? query, string? category = null, Func<BlowNodeDefinition, bool>? filter = null)
    {
        var offered = List.Where(d => !d.Hidden && d.Deprecated is null
                                              && (category is null || string.Equals(d.Category, category, StringComparison.Ordinal))
                                              && (filter is null || filter(d)));
        if (string.IsNullOrWhiteSpace(query)) return [.. offered];
        return BlowGraphSearch.Find(offered, query, Fields);
    }

    private static IEnumerable<string?> Fields(BlowNodeDefinition d)
    {
        yield return d.Label;
        yield return d.Type;
        foreach (var tag in d.Tags) yield return tag;
        yield return d.Category;
        yield return d.Description;
    }

    /// <summary>
    /// A new node of a registered type: its label, size, sides, ports (fixed and dynamic, each copied) and fresh data,
    /// then <see cref="BlowNodeDefinition.Configure"/>.
    /// </summary>
    /// <param name="type">The type.</param>
    /// <param name="position">Where it goes, in flow coordinates (relative to its parent, when it gets one).</param>
    /// <param name="id">Its id; a new unique one when null.</param>
    /// <exception cref="KeyNotFoundException">The type is not registered.</exception>
    public BlowNode Create(string type, BlowXYPosition position = default, string? id = null)
    {
        var definition = Find(type) ?? throw new KeyNotFoundException($"No node definition is registered for the type '{type}'.");
        var node = new BlowNode
        {
            Id = id ?? $"{type}-{Guid.NewGuid():N}",
            Type = type,
            Label = definition.DisplayName,
            Position = position,
            Width = definition.Width,
            Height = definition.Height,
            Data = definition.CreateData?.Invoke(),
        };
        if (definition.SourcePosition is { } source) node.SourcePosition = source;
        if (definition.TargetPosition is { } target) node.TargetPosition = target;
        node.Ports = PortsFor(definition, node);
        definition.Configure?.Invoke(node);
        return node;
    }

    /// <summary>
    /// Sets a node's ports from its definition again - after a change to its settings that changes its dynamic ports.
    /// Hands back the edges that end at a port the node no longer has: remove or rewire them, as the application
    /// sees fit. A node of an unregistered type is left as it is.
    /// </summary>
    /// <param name="node">The node.</param>
    /// <param name="edges">The graph's edges, to find those left without a port; null when you do not need them.</param>
    public IReadOnlyList<BlowEdge> RefreshPorts(BlowNode node, IEnumerable<BlowEdge>? edges = null)
    {
        ArgumentNullException.ThrowIfNull(node);
        if (Find(node) is not { } definition) return [];
        node.Ports = PortsFor(definition, node);
        // A node that declares no ports takes any handle, so no edge is left without one.
        if (edges is null || node.Ports is not { } ports) return [];

        return [.. edges.Where(e =>
            (e.Source == node.Id && !ports.Any(p => p.Matches(BlowHandleType.Source, e.SourceHandle))) ||
            (e.Target == node.Id && !ports.Any(p => p.Matches(BlowHandleType.Target, e.TargetHandle))))];
    }

    private static List<BlowPort>? PortsFor(BlowNodeDefinition definition, BlowNode node)
    {
        var ports = definition.Ports.Select(p => p.Clone()).ToList();
        if (definition.DynamicPorts?.Invoke(node) is { } dynamic) ports.AddRange(dynamic.Select(p => p.Clone()));

        // A port declared twice would draw two handles with one key; the first wins, as in BlowNode.FindPort.
        var unique = new List<BlowPort>(ports.Count);
        foreach (var port in ports)
        {
            if (!unique.Any(u => u.Type == port.Type && string.Equals(u.Id ?? "", port.Id ?? "", StringComparison.Ordinal))) unique.Add(port);
        }
        return unique.Count == 0 ? null : unique;
    }

    /// <summary>
    /// Turns nodes of deprecated kinds into the kinds that replace them: the type changes to
    /// <see cref="BlowNodeDefinition.ReplacedBy"/>, then the deprecated definition's <see cref="BlowNodeDefinition.Migrate"/>
    /// runs, then the node's ports are set from the new definition. Follows a chain of replacements; stops at a loop.
    /// Hands back the nodes changed.
    /// </summary>
    public IReadOnlyList<BlowNode> Upgrade(IEnumerable<BlowNode> nodes)
    {
        ArgumentNullException.ThrowIfNull(nodes);
        var changed = new List<BlowNode>();
        foreach (var node in nodes)
        {
            // The types the node has had: a replacement back to one of them is a loop, and the node stays where it is.
            var seen = new HashSet<string>(StringComparer.Ordinal) { node.Type };
            var upgraded = false;
            var before = node.Ports?.Select(p => (p.Type, p.Id ?? "")).ToHashSet() ?? [];
            while (Find(node) is { Deprecated: not null, ReplacedBy: { } next } old && Contains(next) && seen.Add(next))
            {
                node.Type = next;
                old.Migrate?.Invoke(node);
                upgraded = true;
            }
            if (!upgraded) continue;
            if (Find(node) is { } current)
            {
                // The new kind's ports, plus any the migration added; the deprecated kind's own ports go.
                var added = node.Ports?.Where(p => !before.Contains((p.Type, p.Id ?? ""))).ToList();
                node.Ports = MergePorts(added, PortsFor(current, node));
            }
            changed.Add(node);
        }
        return changed;
    }

    // The new definition's ports, with the ones the migration added kept after them.
    private static List<BlowPort>? MergePorts(List<BlowPort>? migrated, List<BlowPort>? defined)
    {
        if (migrated is null) return defined;
        var result = defined is null ? [] : new List<BlowPort>(defined);
        foreach (var port in migrated)
        {
            if (!result.Any(p => p.Type == port.Type && string.Equals(p.Id ?? "", port.Id ?? "", StringComparison.Ordinal))) result.Add(port);
        }
        return result.Count == 0 ? null : result;
    }

    /// <summary>
    /// A validation rule for <see cref="BlowGraphValidator"/>: nodes of unregistered types, of deprecated ones, and
    /// whatever each definition's own <see cref="BlowNodeDefinition.Validate"/> finds.
    /// </summary>
    /// <param name="unknownType">How serious a node of an unknown type is; null to not report them (group frames, notes).</param>
    public IBlowValidationRule ValidationRule(BlowValidationSeverity? unknownType = BlowValidationSeverity.Error)
        => new BlowDelegateRule(context => Validate(context, unknownType));

    private IEnumerable<BlowValidationIssue> Validate(BlowValidationContext context, BlowValidationSeverity? unknownType)
    {
        foreach (var node in context.Nodes)
        {
            context.CancellationToken.ThrowIfCancellationRequested();
            if (Find(node) is not { } definition)
            {
                if (unknownType is { } severity)
                {
                    yield return new BlowValidationIssue(BlowValidationCodes.UnknownNodeType, severity,
                        $"'{Name(node)}' is of the type '{node.Type}', which this editor does not know.") { NodeId = node.Id };
                }
                continue;
            }

            if (definition.Deprecated is { } why)
            {
                yield return new BlowValidationIssue(BlowValidationCodes.DeprecatedNodeType, BlowValidationSeverity.Warning,
                    $"'{Name(node)}' is a {definition.DisplayName}, which is deprecated: {why}") { NodeId = node.Id };
            }

            if (definition.Validate?.Invoke(node) is { } issues)
            {
                foreach (var issue in issues) yield return issue.NodeId is null && issue.EdgeId is null ? issue with { NodeId = node.Id } : issue;
            }
        }
    }

    /// <summary>The handler and run options of a node's definition, for <see cref="BlowRunner"/>.</summary>
    internal (BlowNodeHandler Handler, BlowNodeRunOptions? Options)? HandlerOf(BlowNode node)
        => Find(node) is { Run: { } run } definition ? (run, definition.RunOptions) : null;

    private static string Name(BlowNode node) => string.IsNullOrWhiteSpace(node.Label) ? node.Id : node.Label!;
}
