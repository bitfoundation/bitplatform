using System.Collections.Concurrent;

namespace Bit.BlazorUI;

/// <summary>
/// The default <see cref="IBitAiToolRegistry"/>: a thread-safe, case-insensitive name-to-tool map.
/// </summary>
/// <remarks>
/// Registered as a singleton by <c>AddBitBlazorUIAIServices</c>. Tools can be added at runtime, which is what
/// lets an application enable a capability only for certain users or conversations.
/// </remarks>
public sealed class BitAiToolRegistry : IBitAiToolRegistry
{
    private readonly ConcurrentDictionary<string, IBitAiTool> _tools = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Creates an empty registry.</summary>
    public BitAiToolRegistry() { }

    /// <summary>Creates a registry populated from a sequence.</summary>
    /// <param name="tools">The tools to register.</param>
    public BitAiToolRegistry(IEnumerable<IBitAiTool> tools)
    {
        ArgumentNullException.ThrowIfNull(tools);
        foreach (var tool in tools) Register(tool);
    }

    /// <inheritdoc />
    public IReadOnlyCollection<IBitAiTool> Tools => _tools.Values.ToArray();

    /// <inheritdoc />
    public IBitAiTool? Find(string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        return _tools.TryGetValue(name, out var tool) ? tool : null;
    }

    /// <inheritdoc />
    public void Register(IBitAiTool tool)
    {
        ArgumentNullException.ThrowIfNull(tool);
        _tools[tool.Definition.Name] = tool;
    }

    /// <inheritdoc />
    public bool Unregister(string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        return _tools.TryRemove(name, out _);
    }

    /// <summary>The definitions of every registered tool, which is what a request carries.</summary>
    /// <returns>The definitions.</returns>
    public IReadOnlyList<BitAiToolDefinition> GetDefinitions() =>
        _tools.Values.Select(t => t.Definition).ToArray();
}

/// <summary>
/// The default approval policy: approve destructive tools, and honour an explicit
/// <see cref="BitAiToolDefinition.RequiresApproval"/> either way.
/// </summary>
/// <remarks>
/// Replace it by registering your own <see cref="IBitAiToolApprovalPolicy"/> when the decision depends on
/// the arguments or the current user rather than only on the tool.
/// </remarks>
public sealed class BitAiDefaultToolApprovalPolicy : IBitAiToolApprovalPolicy
{
    private readonly HashSet<string> _alwaysApproved = new(StringComparer.OrdinalIgnoreCase);

    /// <inheritdoc />
    public ValueTask<bool> RequiresApprovalAsync(
        BitAiToolDefinition definition,
        BitAiToolInvocation invocation,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(definition);

        // Tools in one round are checked concurrently while the UI may be recording an "always".
        bool alwaysApproved;
        lock (_alwaysApproved) alwaysApproved = _alwaysApproved.Contains(definition.Name);

        if (alwaysApproved) return ValueTask.FromResult(false);
        if (definition.RequiresApproval is { } explicitChoice) return ValueTask.FromResult(explicitChoice);
        return ValueTask.FromResult(definition.Risk is BitAiToolRisk.Destructive);
    }

    /// <summary>
    /// Stops asking for a tool the user approved with "always".
    /// </summary>
    /// <param name="toolName">The tool to stop asking about.</param>
    public void RememberAlwaysApproved(string toolName)
    {
        ArgumentNullException.ThrowIfNull(toolName);
        lock (_alwaysApproved) _alwaysApproved.Add(toolName);
    }
}
