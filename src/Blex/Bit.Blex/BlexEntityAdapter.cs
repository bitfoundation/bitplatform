using System.Text.Json.Serialization;

namespace Bit.Blex;

/// <summary>
/// Generates CRUD operations and an initial state for a normalized <see cref="BlexEntityState{TEntity, TKey}"/>.
/// Holds the id selector (not serialized), so create one per entity type, typically as a static field
/// on the store. Mirrors Redux Toolkit's <c>createEntityAdapter</c>.
/// </summary>
public sealed class BlexEntityAdapter<TEntity, TKey>
    where TKey : notnull
{
    private readonly Func<TEntity, TKey> _selectId;
    private readonly IComparer<TEntity>? _sortComparer;

    /// <summary>Creates an adapter that derives an entity's id via <paramref name="selectId"/>.</summary>
    public BlexEntityAdapter(Func<TEntity, TKey> selectId)
        : this(selectId, null)
    {
    }

    /// <summary>
    /// Creates an adapter that keeps <see cref="BlexEntityState{TEntity, TKey}.Ids"/> sorted by
    /// <paramref name="sortComparer"/> after every operation (like Redux Toolkit's
    /// <c>sortComparer</c>). Without a comparer, insertion order is preserved.
    /// </summary>
    public BlexEntityAdapter(Func<TEntity, TKey> selectId, IComparer<TEntity>? sortComparer)
    {
        ArgumentNullException.ThrowIfNull(selectId);
        _selectId = selectId;
        _sortComparer = sortComparer;
    }

    /// <summary>
    /// The id of an entity, as this adapter derives it. Exposed (like Redux Toolkit's
    /// <c>adapter.selectId</c>) so callers can key entities the same way the adapter does without
    /// duplicating the selector -- the difference between <c>Adapter.SelectId(todo)</c> and a
    /// second, silently divergent <c>t =&gt; t.Id</c> at every call site.
    /// </summary>
    public TKey SelectId(TEntity entity) => _selectId(entity);

    /// <summary>
    /// The comparer keeping <see cref="BlexEntityState{TEntity, TKey}.Ids"/> ordered, or
    /// <c>null</c> when the adapter preserves insertion order.
    /// </summary>
    public IComparer<TEntity>? SortComparer => _sortComparer;

    /// <summary>Builds the resulting state, re-sorting ids when a sort comparer is configured.</summary>
    private BlexEntityState<TEntity, TKey> Build(List<TKey> ids, Dictionary<TKey, TEntity> map)
    {
        if (_sortComparer is not null)
            ids = ids.OrderBy(id => map[id], _sortComparer).ToList(); // stable sort

        // The ids/map pair is built here and always agrees, so skip the constructor's
        // reconciliation scan (that boundary check exists for untrusted JSON payloads).
        return new BlexEntityState<TEntity, TKey>(ids, map, consistent: true);
    }

    /// <summary>
    /// Builds the next state, but hands back <paramref name="state"/> unchanged when the result
    /// would be equivalent to it. State fields compare by reference, so returning an
    /// equivalent-but-new instance would raise a change notification and record a time-travel
    /// action for a mutation that never happened.
    /// </summary>
    private BlexEntityState<TEntity, TKey> BuildOrSame(
        BlexEntityState<TEntity, TKey> state,
        List<TKey> ids,
        Dictionary<TKey, TEntity> map)
    {
        var next = Build(ids, map);
        return Equivalent(state, next) ? state : next;
    }

    /// <summary>Whether two states hold the same entities, in the same order, with equal values.</summary>
    private static bool Equivalent(BlexEntityState<TEntity, TKey> left, BlexEntityState<TEntity, TKey> right)
    {
        if (ReferenceEquals(left, right))
            return true;
        if (left.Ids.Count != right.Ids.Count)
            return false;

        var keys = EqualityComparer<TKey>.Default;
        var values = EqualityComparer<TEntity>.Default;
        for (var i = 0; i < left.Ids.Count; i++)
        {
            var id = left.Ids[i];
            if (!keys.Equals(id, right.Ids[i]))
                return false;
            if (!right.Entities.TryGetValue(id, out var candidate)
                || !values.Equals(left.Entities[id], candidate))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>The empty initial state for this entity type.</summary>
    public BlexEntityState<TEntity, TKey> GetInitialState() => BlexEntityState<TEntity, TKey>.Empty;

    /// <summary>Initial state seeded with the supplied entities.</summary>
    public BlexEntityState<TEntity, TKey> GetInitialState(IEnumerable<TEntity> entities)
        => SetAll(BlexEntityState<TEntity, TKey>.Empty, entities);

    /// <summary>Adds an entity. No-op if its id already exists.</summary>
    public BlexEntityState<TEntity, TKey> AddOne(BlexEntityState<TEntity, TKey> state, TEntity entity)
    {
        var id = _selectId(entity);
        if (state.Entities.ContainsKey(id))
            return state;

        var ids = new List<TKey>(state.Ids) { id };
        var map = ToDictionary(state);
        map[id] = entity;
        return Build(ids, map);
    }

    /// <summary>Adds many entities, ignoring any whose id already exists.</summary>
    public BlexEntityState<TEntity, TKey> AddMany(BlexEntityState<TEntity, TKey> state, IEnumerable<TEntity> entities)
    {
        ArgumentNullException.ThrowIfNull(entities);
        var ids = new List<TKey>(state.Ids);
        var map = ToDictionary(state);
        var changed = false;
        foreach (var entity in entities)
        {
            var id = _selectId(entity);
            if (map.ContainsKey(id))
                continue;
            ids.Add(id);
            map[id] = entity;
            changed = true;
        }

        // A no-op must return the *same* instance: state fields compare by reference, so handing
        // back an equivalent-but-new instance would raise a change notification and record a
        // time-travel action for a mutation that never happened.
        return changed ? Build(ids, map) : state;
    }

    /// <summary>
    /// Adds or replaces an entity (matched by id). Replacing an entity with an equal one is a
    /// no-op and returns the same state instance.
    /// </summary>
    public BlexEntityState<TEntity, TKey> UpsertOne(BlexEntityState<TEntity, TKey> state, TEntity entity)
    {
        var id = _selectId(entity);
        var existed = state.Entities.TryGetValue(id, out var existing);

        // Re-upserting an equal entity changes nothing; keep the instance so it raises no
        // notification and records no action.
        if (existed && EqualityComparer<TEntity>.Default.Equals(existing, entity))
            return state;

        var map = ToDictionary(state);
        map[id] = entity;
        var ids = existed ? new List<TKey>(state.Ids) : new List<TKey>(state.Ids) { id };
        return Build(ids, map);
    }

    /// <summary>
    /// Adds or replaces many entities (matched by id). Entries equal to what is already stored
    /// are skipped, so an upsert that changes nothing returns the same state instance.
    /// </summary>
    public BlexEntityState<TEntity, TKey> UpsertMany(BlexEntityState<TEntity, TKey> state, IEnumerable<TEntity> entities)
    {
        ArgumentNullException.ThrowIfNull(entities);
        var ids = new List<TKey>(state.Ids);
        var map = ToDictionary(state);
        var changed = false;
        foreach (var entity in entities)
        {
            var id = _selectId(entity);
            if (map.TryGetValue(id, out var existing))
            {
                if (EqualityComparer<TEntity>.Default.Equals(existing, entity))
                    continue;
            }
            else
            {
                ids.Add(id);
            }

            map[id] = entity;
            changed = true;
        }

        // A no-op must return the *same* instance (see AddMany).
        return changed ? Build(ids, map) : state;
    }

    /// <summary>
    /// Replaces the entity with this id, but only if it is already present -- the counterpart to
    /// <see cref="AddOne"/>, mirroring Redux Toolkit's <c>setOne</c>/<c>setMany</c> pair.
    /// Unknown ids and equal replacements are no-ops.
    /// </summary>
    public BlexEntityState<TEntity, TKey> SetOne(BlexEntityState<TEntity, TKey> state, TEntity entity)
    {
        var id = _selectId(entity);
        if (!state.Entities.TryGetValue(id, out var existing)
            || EqualityComparer<TEntity>.Default.Equals(existing, entity))
        {
            return state;
        }

        var map = ToDictionary(state);
        map[id] = entity;
        return Build(new List<TKey>(state.Ids), map);
    }

    /// <summary>
    /// Replaces each entity that is already present, ignoring ids that are not.
    /// Returns the same state instance when nothing changed.
    /// </summary>
    public BlexEntityState<TEntity, TKey> SetMany(BlexEntityState<TEntity, TKey> state, IEnumerable<TEntity> entities)
    {
        ArgumentNullException.ThrowIfNull(entities);
        var map = ToDictionary(state);
        var changed = false;
        foreach (var entity in entities)
        {
            var id = _selectId(entity);
            if (!map.TryGetValue(id, out var existing)
                || EqualityComparer<TEntity>.Default.Equals(existing, entity))
            {
                continue;
            }

            map[id] = entity;
            changed = true;
        }

        return changed ? Build(new List<TKey>(state.Ids), map) : state;
    }

    /// <summary>Applies an update function to one entity, matched by id. No-op if absent.</summary>
    public BlexEntityState<TEntity, TKey> UpdateOne(BlexEntityState<TEntity, TKey> state, TKey id, Func<TEntity, TEntity> update)
    {
        if (!state.Entities.TryGetValue(id, out var existing))
            return state;

        var updated = update(existing);
        var newId = _selectId(updated);

        // An updater that returns an equal entity (a record with the same values, or the very same
        // instance) is a no-op; returning the same state keeps it from raising a notification and
        // recording a time-travel action. This is what makes Map/UpdateMany over unchanged
        // entities free.
        if (EqualityComparer<TKey>.Default.Equals(newId, id)
            && EqualityComparer<TEntity>.Default.Equals(existing, updated))
        {
            return state;
        }

        var map = ToDictionary(state);

        if (EqualityComparer<TKey>.Default.Equals(newId, id))
        {
            map[id] = updated;
            // The id list is never mutated after construction, so it can be shared when order
            // is insertion-based; a sort comparer may need to re-position the updated entity.
            return _sortComparer is null
                ? new BlexEntityState<TEntity, TKey>(state.Ids, map, consistent: true)
                : Build(new List<TKey>(state.Ids), map);
        }

        // Id changed.
        map.Remove(id);
        var ids = new List<TKey>(state.Ids);
        if (map.ContainsKey(newId))
        {
            // The new id already belongs to another entity: overwrite it and drop the old
            // slot (a positional swap would duplicate the id in the ordered list).
            map[newId] = updated;
            ids.Remove(id);
        }
        else
        {
            map[newId] = updated;
            ids[ids.IndexOf(id)] = newId;
        }

        return Build(ids, map);
    }

    /// <summary>Applies an update function to each entity matched by id. Missing ids are ignored.</summary>
    /// <remarks>
    /// Applied in one pass over a single copy of the state. Delegating to <see cref="UpdateOne"/>
    /// per id would copy the whole entity map (and re-sort it) once per id, making a bulk update
    /// over <c>n</c> entities quadratic -- exactly the operation most likely to run over a large
    /// collection. The observable semantics are identical, including the sequential ones: an update
    /// that changes an entity's id is visible to the ids that follow it.
    /// </remarks>
    public BlexEntityState<TEntity, TKey> UpdateMany(BlexEntityState<TEntity, TKey> state, IEnumerable<TKey> ids, Func<TEntity, TEntity> update)
    {
        ArgumentNullException.ThrowIfNull(ids);
        ArgumentNullException.ThrowIfNull(update);

        var keys = EqualityComparer<TKey>.Default;
        var values = EqualityComparer<TEntity>.Default;
        Dictionary<TKey, TEntity>? map = null;
        List<TKey>? order = null;

        foreach (var id in ids)
        {
            // Read through the working copy once it exists, so a later id sees earlier updates.
            var lookup = (IReadOnlyDictionary<TKey, TEntity>?)map ?? state.Entities;
            if (!lookup.TryGetValue(id, out var existing))
                continue;

            var updated = update(existing);
            var newId = _selectId(updated);
            var sameId = keys.Equals(newId, id);

            // An updater that returns an equal entity changes nothing; not materializing a copy for
            // it is what makes Map over an unchanged collection free.
            if (sameId && values.Equals(existing, updated))
                continue;

            if (map is null)
            {
                map = ToDictionary(state);
                order = new List<TKey>(state.Ids);
            }

            if (sameId)
            {
                map[id] = updated;
                continue;
            }

            map.Remove(id);
            if (map.ContainsKey(newId))
            {
                // The new id already belongs to another entity: overwrite it and drop the old slot
                // (a positional swap would duplicate the id in the ordered list).
                map[newId] = updated;
                order!.Remove(id);
            }
            else
            {
                map[newId] = updated;
                order![order.IndexOf(id)] = newId;
            }
        }

        // Nothing matched, or every update was a no-op: same instance, no notification, no action.
        return map is null ? state : Build(order!, map);
    }

    /// <summary>Applies a transform to every entity (like Redux Toolkit's <c>map</c>).</summary>
    public BlexEntityState<TEntity, TKey> Map(BlexEntityState<TEntity, TKey> state, Func<TEntity, TEntity> transform)
    {
        ArgumentNullException.ThrowIfNull(transform);
        return UpdateMany(state, new List<TKey>(state.Ids), transform);
    }

    /// <summary>Removes the entity with the given id. No-op if absent.</summary>
    public BlexEntityState<TEntity, TKey> RemoveOne(BlexEntityState<TEntity, TKey> state, TKey id)
    {
        if (!state.Entities.ContainsKey(id))
            return state;

        var map = ToDictionary(state);
        map.Remove(id);
        var ids = new List<TKey>(state.Ids);
        ids.Remove(id);
        return Build(ids, map);
    }

    /// <summary>Removes the entities with the given ids. Ids that are not present are ignored.</summary>
    public BlexEntityState<TEntity, TKey> RemoveMany(BlexEntityState<TEntity, TKey> state, IEnumerable<TKey> ids)
    {
        ArgumentNullException.ThrowIfNull(ids);
        var toRemove = new HashSet<TKey>(ids);
        var map = ToDictionary(state);
        var removed = 0;
        foreach (var id in toRemove)
        {
            if (map.Remove(id))
                removed++;
        }

        if (removed == 0)
            return state;

        var remaining = new List<TKey>(state.Ids.Count - removed);
        foreach (var id in state.Ids)
        {
            if (!toRemove.Contains(id))
                remaining.Add(id);
        }

        // Removal preserves the relative order of what is left, so a sorted state stays sorted
        // and there is nothing for Build's comparer pass to do.
        return new BlexEntityState<TEntity, TKey>(remaining, map, consistent: true);
    }

    /// <summary>Removes every entity.</summary>
    public BlexEntityState<TEntity, TKey> RemoveAll(BlexEntityState<TEntity, TKey> state)
        => state.Count == 0 ? state : BlexEntityState<TEntity, TKey>.Empty;

    /// <summary>
    /// Replaces the entire collection with the supplied entities. Returns the same state instance
    /// when the replacement is equivalent to what is already stored (same ids, same order, equal
    /// values) -- reseeding from an unchanged server response records no action.
    /// </summary>
    public BlexEntityState<TEntity, TKey> SetAll(BlexEntityState<TEntity, TKey> state, IEnumerable<TEntity> entities)
    {
        ArgumentNullException.ThrowIfNull(entities);
        var ids = new List<TKey>();
        var map = new Dictionary<TKey, TEntity>();
        foreach (var entity in entities)
        {
            var id = _selectId(entity);
            if (!map.ContainsKey(id))
                ids.Add(id);
            map[id] = entity;
        }

        // Clearing an already-empty collection changes nothing; keep the instance so it does not
        // register as a mutation.
        if (ids.Count == 0 && state.Count == 0)
            return state;

        return BuildOrSame(state, ids, map);
    }

    private static Dictionary<TKey, TEntity> ToDictionary(BlexEntityState<TEntity, TKey> state)
        => new(state.Entities);
}
