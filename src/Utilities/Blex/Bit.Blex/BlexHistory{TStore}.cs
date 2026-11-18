namespace Bit.Blex;

/// <summary>
/// Undo/redo for the stores of one type only -- an editor's undo stack that never reverts the session,
/// the theme or a cart. Every instance of <typeparamref name="TStore"/> is covered, scoped instances
/// included. Register it with <c>AddBitBlexHistory&lt;TStore&gt;()</c> and inject
/// <c>BlexHistory&lt;TStore&gt;</c>; the registration starts recording as soon as it is resolved.
/// </summary>
/// <typeparam name="TStore">The store type whose actions are recorded and restored.</typeparam>
/// <example><code>
/// builder.Services.AddBitBlexHistory&lt;EditorStore&gt;();
///
/// @inject BlexHistory&lt;EditorStore&gt; History
/// &lt;button @onclick="History.Undo" disabled="@(!History.CanUndo)"&gt;Undo&lt;/button&gt;
/// </code></example>
public sealed class BlexHistory<TStore> : BlexHistory
    where TStore : class, IBlexStore
{
    /// <summary>Creates a history covering every <typeparamref name="TStore"/> registered with <paramref name="manager"/>.</summary>
    /// <param name="manager">The manager whose actions are recorded.</param>
    /// <param name="maxEntries">Maximum number of undo entries retained; older entries are discarded.</param>
    /// <param name="filter">Optional predicate deciding which actions get their own undo step.</param>
    /// <param name="groupBy">Optional key selector that merges consecutive actions into one step.</param>
    public BlexHistory(
        BlexManager manager,
        int maxEntries = 100,
        Func<BlexActionContext, bool>? filter = null,
        Func<BlexActionContext, string?>? groupBy = null)
        : base(manager, static store => store is TStore, maxEntries, filter, groupBy)
    {
    }
}
