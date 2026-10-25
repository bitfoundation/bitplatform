namespace Bit.Blex;

/// <summary>
/// Marks a backing field as a piece of reactive state. The generator emits a public property
/// (PascalCased, with the leading underscore removed) whose setter flows through the dispatch
/// pipeline so changes are tracked, notified and recorded for time-travel.
/// </summary>
/// <remarks>
/// Change detection uses <see cref="EqualityComparer{T}.Default"/>. For reference types such as
/// <c>List&lt;T&gt;</c> that do not override equality, this is reference equality, so mutating a
/// collection in place will not raise a notification. Assign a new instance (or use an immutable
/// collection) when updating collection or object state.
/// </remarks>
[AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = false)]
public sealed class BlexStateAttribute : Attribute
{
    /// <summary>
    /// Set to <c>false</c> to keep this field out of durable persistence in a store marked
    /// <c>[BlexStoreAttribute(Persist = true)]</c>: it is never written to storage, and a stored value
    /// for it (from before it was excluded) is ignored on hydration and on cross-tab sync. Defaults to
    /// <c>true</c>.
    /// </summary>
    /// <remarks>
    /// The declarative counterpart to <see cref="BlexPersistenceOptions.Partialize"/>, for state that
    /// belongs to one visit -- a search box's text, a cached response, a selection, a loading flag.
    /// The field is still part of the store's snapshot, so DevTools, undo/redo and the prerender
    /// handoff see it as usual.
    /// </remarks>
    /// <example><code>
    /// [BlexStore(Name = "search", Persist = true)]
    /// public partial class SearchStore
    /// {
    ///     [BlexState] private string[] _recent = [];
    ///     [BlexState(Persist = false)] private string _query = "";
    /// }
    /// </code></example>
    public bool Persist { get; set; } = true;
}
