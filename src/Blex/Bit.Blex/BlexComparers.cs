namespace Bit.Blex;

/// <summary>
/// Equality comparers for selector subscriptions whose projection is a <em>collection</em>.
/// </summary>
/// <remarks>
/// <para>
/// Selector subscriptions compare with <see cref="EqualityComparer{T}.Default"/>, which for a
/// <c>List&lt;T&gt;</c>, an array or a LINQ query is reference equality -- and a projection like
/// <c>() =&gt; store.Todos.All.Where(t =&gt; !t.Done)</c> hands back a brand-new object every time it
/// runs. The subscription therefore fires on every action, which is precisely what selecting a
/// projection was meant to avoid. These comparers look at the contents instead, the way zustand's
/// <c>shallow</c> does.
/// </para>
/// <para>
/// They compare elements with <see cref="EqualityComparer{T}.Default"/> unless given another
/// comparer, so records and value types compare structurally while mutable classes still compare by
/// reference -- which is the right answer for a normalized store whose entities are replaced rather
/// than edited in place.
/// </para>
/// </remarks>
/// <example><code>
/// // re-renders only when the set of pending todos actually differs
/// Subscribe(Store, () => Store.Todos.All.Where(t => !t.Done), BlexComparers.Sequence&lt;Todo&gt;());
/// </code></example>
public static class BlexComparers
{
    /// <summary>
    /// Compares two sequences element by element, in order (LINQ's <c>SequenceEqual</c> as a
    /// comparer). Two <c>null</c> sequences are equal; a <c>null</c> and an empty one are not.
    /// </summary>
    /// <param name="elementComparer">Element equality; defaults to <see cref="EqualityComparer{T}.Default"/>.</param>
    public static IEqualityComparer<IEnumerable<T>?> Sequence<T>(IEqualityComparer<T>? elementComparer = null)
        => elementComparer is null ? SequenceComparer<T>.Default : new SequenceComparer<T>(elementComparer);

    /// <summary>
    /// Compares two sequences as sets: same elements, order and repetition ignored. Use it for a
    /// projection whose order is incidental (a selection of ids, a set of active filters) so a
    /// re-ordered but otherwise identical result does not count as a change.
    /// </summary>
    /// <param name="elementComparer">Element equality; defaults to <see cref="EqualityComparer{T}.Default"/>.</param>
    public static IEqualityComparer<IEnumerable<T>?> Set<T>(IEqualityComparer<T>? elementComparer = null)
        => elementComparer is null ? SetComparer<T>.Default : new SetComparer<T>(elementComparer);

    private sealed class SequenceComparer<T>(IEqualityComparer<T> elements) : IEqualityComparer<IEnumerable<T>?>
    {
        public static readonly SequenceComparer<T> Default = new(EqualityComparer<T>.Default);

        public bool Equals(IEnumerable<T>? x, IEnumerable<T>? y)
        {
            if (ReferenceEquals(x, y))
                return true;
            if (x is null || y is null)
                return false;

            // Skip the walk when both sides already know their size and disagree.
            if (x is IReadOnlyCollection<T> left && y is IReadOnlyCollection<T> right && left.Count != right.Count)
                return false;

            return x.SequenceEqual(y, elements);
        }

        public int GetHashCode(IEnumerable<T>? obj)
        {
            if (obj is null)
                return 0;

            var hash = new HashCode();
            foreach (var item in obj)
                hash.Add(item is null ? 0 : elements.GetHashCode(item));

            return hash.ToHashCode();
        }
    }

    private sealed class SetComparer<T>(IEqualityComparer<T> elements) : IEqualityComparer<IEnumerable<T>?>
    {
        public static readonly SetComparer<T> Default = new(EqualityComparer<T>.Default);

        public bool Equals(IEnumerable<T>? x, IEnumerable<T>? y)
        {
            if (ReferenceEquals(x, y))
                return true;
            if (x is null || y is null)
                return false;

            var left = new HashSet<T>(x, elements);
            return left.SetEquals(y);
        }

        public int GetHashCode(IEnumerable<T>? obj)
        {
            if (obj is null)
                return 0;

            // Order-independent, so it must stay commutative: XOR rather than a sequential combine.
            var hash = 0;
            foreach (var item in obj)
                hash ^= item is null ? 0 : elements.GetHashCode(item);

            return hash;
        }
    }
}
