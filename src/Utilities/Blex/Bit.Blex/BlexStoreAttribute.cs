namespace Bit.Blex;

/// <summary>
/// Marks a partial class as a Blex state store. The source generator will emit reactive
/// properties for <see cref="BlexStateAttribute"/> fields, memoized <see cref="BlexComputedAttribute"/>
/// accessors, named action wrappers, JSON snapshot support and the <see cref="BlexStoreBase"/> base type.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class BlexStoreAttribute : Attribute
{
    /// <summary>Optional display name used in DevTools. Defaults to the class name.</summary>
    public string? Name { get; set; }

    /// <summary>
    /// When <c>true</c>, the store opts in to automatic persistence: its state is saved through the
    /// registered <see cref="IBlexStorage"/> after every action and rehydrated on startup.
    /// Has no effect unless a persistence provider is wired up (see <c>AddBitBlexPersistence</c>).
    /// Individual fields can opt out with <c>[BlexStateAttribute(Persist = false)]</c>.
    /// </summary>
    public bool Persist { get; set; }

    /// <summary>
    /// The version of this store's saved state, for a store marked <see cref="Persist"/>. When greater
    /// than zero it is used for this store instead of <see cref="BlexPersistenceOptions.Version"/>: the
    /// store's state is saved in a version envelope of its own, and state saved under another version goes
    /// to <see cref="BlexPersistenceOptions.Migrate"/> with the store's name and the saved version (<c>0</c>
    /// for state saved before the store had a version, whatever the app's version was), or is discarded
    /// when there is no migration. Raise it when the store's saved state no longer fits, and
    /// the other stores keep theirs. Defaults to <c>0</c>: the store follows
    /// <see cref="BlexPersistenceOptions.Version"/>. Cannot be negative (<c>BLEX024</c>), and has no
    /// effect without <see cref="Persist"/> (<c>BLEX025</c>).
    /// </summary>
    public int Version { get; set; }

    /// <summary>
    /// When <c>true</c>, the store's state can only change through its own actions: the generated
    /// state properties get <c>private</c> setters, so assigning one from outside the store is a
    /// compile-time error, and an assignment the store's own code makes outside any action throws at
    /// runtime once the store is registered. Defaults to <c>false</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The guarantee this buys is the one a Flux store is supposed to give: every state change has a
    /// name, passes the filters, reaches middleware and lands in DevTools as a deliberate action -- never
    /// as an anonymous <c>"Set X"</c> from whichever component happened to hold the store.
    /// </para>
    /// <para>
    /// Blazor two-way binding needs a setter, so bind such a store's state through an action instead:
    /// <c>&lt;input @bind:get="Store.Query" @bind:set="Store.SetQuery" /&gt;</c>.
    /// </para>
    /// </remarks>
    public bool EnforceActions { get; set; }

    /// <summary>
    /// When <c>true</c>, a synchronous action that throws leaves no trace: the store's state fields are
    /// put back to what they held when the action started, nothing is notified, recorded, persisted or
    /// added to the undo history, and the exception continues to the caller (and the action-error hooks)
    /// as usual. Defaults to <c>false</c>: like any C# method, a throwing action keeps what it changed
    /// before the throw -- and, since it did change state, is recorded.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Every action level is its own transaction: an action whose caller catches its exception and carries
    /// on is still undone, while the caller's own changes stay. The snapshot is a shallow copy of the
    /// <c>[BlexStateAttribute]</c> fields, so it undoes assignments -- which is how state changes in a Blex
    /// store -- not mutations made inside an object a field refers to.
    /// </para>
    /// <para>
    /// Only this store is rolled back: an action of another store that ran inside the failed one keeps its
    /// effect. Asynchronous actions and effects are not rolled back -- the UI has already shown what they
    /// changed before their first <c>await</c>, and other changes may have landed since.
    /// </para>
    /// </remarks>
    public bool RollbackOnError { get; set; }
}
