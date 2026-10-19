using System.Text.Json.Nodes;

namespace Bit.Blex;

/// <summary>
/// Context passed to middleware <em>before</em> an action mutates a store. Call <see cref="Cancel"/>
/// to veto the action: the mutation will not run and nothing is recorded.
/// </summary>
public sealed class BlexPreActionContext
{
    internal BlexPreActionContext(IBlexStore store, string actionName, IReadOnlyList<BlexActionArg>? args)
    {
        Store = store;
        ActionName = actionName;
        Args = args ?? [];
    }

    /// <summary>The store about to produce the action.</summary>
    public IBlexStore Store { get; }

    /// <summary>The action name (e.g. <c>"Increment"</c> or <c>"Set Count"</c>).</summary>
    public string ActionName { get; }

    /// <summary>
    /// The action's arguments (parameter name/value pairs). Useful for validation filters that
    /// veto based on the payload. Empty for parameterless actions.
    /// </summary>
    public IReadOnlyList<BlexActionArg> Args { get; }

    private string? _qualifiedName;

    /// <summary>Fully-qualified action label including the originating store.</summary>
    public string QualifiedName => _qualifiedName ??= $"{Store.Name}/{ActionName}";

    /// <summary>Whether a middleware has vetoed this action.</summary>
    public bool IsCancelled { get; private set; }

    /// <summary>Vetoes the action so its mutation does not run.</summary>
    public void Cancel() => IsCancelled = true;
}
