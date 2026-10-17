using System.Text.Json.Serialization;

namespace Bit.Blex;

/// <summary>
/// An immutable, normalized collection: an ordered list of ids plus an id-keyed entity map.
/// Designed to be stored as a single <c>[BlexStateAttribute]</c> field. Every mutation returns a new instance,
/// so change detection (and therefore notifications and time-travel) works correctly. Create and
/// mutate instances through an <see cref="BlexEntityAdapter{TEntity, TKey}"/>.
/// </summary>
/// <typeparam name="TEntity">The stored entity type.</typeparam>
/// <typeparam name="TKey">The id type (must be non-null and JSON-serializable as a dictionary key).</typeparam>
public sealed class BlexEntityState<TEntity, TKey>
    where TKey : notnull
{
    /// <summary>Creates a normalized state from an ordered id list and matching entity map.</summary>
    /// <remarks>
    /// This is also the JSON constructor, so it is the boundary where untrusted data arrives. A
    /// payload that is missing (or nulls out) either half degrades to an empty collection, and one
    /// whose two halves disagree -- a dangling id, a duplicate id, an entity no id lists -- is
    /// reconciled here rather than producing an instance that throws on first enumeration.
    /// Reconciliation drops ids with no entity, de-duplicates repeated ids, and appends entities the
    /// id list forgot (in map order) so nothing is silently lost.
    /// </remarks>
    [JsonConstructor]
    public BlexEntityState(IReadOnlyList<TKey> ids, IReadOnlyDictionary<TKey, TEntity> entities)
    {
        ids ??= EmptyIds;
        entities ??= EmptyEntities;

        // Copied rather than aliased. The caller keeps a reference to whatever they passed, and a
        // `Dictionary<,>` handed in here and mutated afterwards would change this instance in place
        // -- silently breaking both the immutability this type promises and the reference-based
        // change detection that depends on it. The JSON path already hands over freshly built
        // collections, so the copy only costs on a boundary that was already O(n).
        var map = entities.Count == 0
            ? EmptyEntities
            : new System.Collections.ObjectModel.ReadOnlyDictionary<TKey, TEntity>(CopyOf(entities));

        Ids = NeedsReconciliation(ids, map) ? Reconcile(ids, map) : CopyOf(ids);
        Entities = map;
    }

    private static Dictionary<TKey, TEntity> CopyOf(IReadOnlyDictionary<TKey, TEntity> source)
    {
        var copy = new Dictionary<TKey, TEntity>(source.Count);
        foreach (var pair in source)
            copy[pair.Key] = pair.Value;
        return copy;
    }

    private static IReadOnlyList<TKey> CopyOf(IReadOnlyList<TKey> source)
        => source.Count == 0 ? EmptyIds : new List<TKey>(source);

    /// <summary>
    /// Creates a state whose halves are known to agree, skipping the reconciliation scan. Every
    /// <see cref="BlexEntityAdapter{TEntity, TKey}"/> operation maintains that invariant by
    /// construction, so the adapter must not pay an O(n) verification on each call.
    /// </summary>
    internal BlexEntityState(IReadOnlyList<TKey> ids, IReadOnlyDictionary<TKey, TEntity> entities, bool consistent)
    {
        _ = consistent;
        Ids = ids;
        Entities = entities;
    }

    /// <summary>Whether the id list and the entity map disagree (dangling id, duplicate id, or an unlisted entity).</summary>
    private static bool NeedsReconciliation(IReadOnlyList<TKey> ids, IReadOnlyDictionary<TKey, TEntity> entities)
    {
        // Equal counts plus "every id resolves" implies a bijection: duplicates in the id list
        // would force some entity to go unlisted, which the count check already rejects.
        if (ids.Count != entities.Count)
            return true;

        for (var i = 0; i < ids.Count; i++)
        {
            if (!entities.ContainsKey(ids[i]))
                return true;
        }

        return false;
    }

    private static IReadOnlyList<TKey> Reconcile(IReadOnlyList<TKey> ids, IReadOnlyDictionary<TKey, TEntity> entities)
    {
        var reconciled = new List<TKey>(entities.Count);
        var seen = new HashSet<TKey>();
        for (var i = 0; i < ids.Count; i++)
        {
            var id = ids[i];
            if (entities.ContainsKey(id) && seen.Add(id))
                reconciled.Add(id);
        }

        // Entities the id list never mentioned would otherwise be unreachable through Ids/All while
        // still counting towards Entities; appending them keeps the two halves in step.
        if (reconciled.Count != entities.Count)
        {
            foreach (var key in entities.Keys)
            {
                if (seen.Add(key))
                    reconciled.Add(key);
            }
        }

        return reconciled;
    }

    private static readonly IReadOnlyList<TKey> EmptyIds = Array.Empty<TKey>();

    private static readonly IReadOnlyDictionary<TKey, TEntity> EmptyEntities =
        new System.Collections.ObjectModel.ReadOnlyDictionary<TKey, TEntity>(new Dictionary<TKey, TEntity>());

    /// <summary>The ids in insertion/display order.</summary>
    public IReadOnlyList<TKey> Ids { get; }

    /// <summary>The id-to-entity map.</summary>
    public IReadOnlyDictionary<TKey, TEntity> Entities { get; }

    /// <summary>An empty state. Shared and defensively read-only.</summary>
    public static BlexEntityState<TEntity, TKey> Empty { get; } = new(EmptyIds, EmptyEntities, consistent: true);

    /// <summary>Number of stored entities.</summary>
    [JsonIgnore]
    public int Count => Ids.Count;

    /// <summary>Entities in id order.</summary>
    /// <remarks>
    /// Lazily enumerated: the sequence is walked on each iteration rather than materialized, so
    /// cache it (<c>ToList()</c>) when a render pass reads it more than once. Every adapter
    /// operation keeps <see cref="Ids"/> and <see cref="Entities"/> in step, and a payload whose
    /// halves disagree is reconciled by the constructor, so this never throws.
    /// </remarks>
    [JsonIgnore]
    public IEnumerable<TEntity> All
    {
        get
        {
            foreach (var id in Ids)
                yield return Entities[id];
        }
    }

    /// <summary>The entity with the given id.</summary>
    /// <exception cref="KeyNotFoundException">No entity has that id.</exception>
    /// <remarks>
    /// For the cases where absence is expected, use <see cref="TryGet"/> or <see cref="Find"/>.
    /// </remarks>
    public TEntity this[TKey id] => Entities[id];

    /// <summary>Whether an entity with the given id exists.</summary>
    public bool Contains(TKey id) => Entities.ContainsKey(id);

    /// <summary>Returns the entity for an id, or <c>default</c> if absent.</summary>
    public TEntity? Find(TKey id) => Entities.TryGetValue(id, out var e) ? e : default;

    /// <summary>
    /// Looks an entity up by id, reporting whether it was there.
    /// </summary>
    /// <remarks>
    /// The unambiguous counterpart to <see cref="Find"/>: for a struct entity (or any type whose
    /// default is a legitimate value) <c>Find</c> cannot distinguish "absent" from "present and
    /// equal to default", and <c>Contains</c> followed by <c>Find</c> hashes the key twice.
    /// </remarks>
    public bool TryGet(TKey id, out TEntity entity) => Entities.TryGetValue(id, out entity!);
}
