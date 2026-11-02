using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Bit.BlazeDb.EntityFrameworkCore.Storage;

/// <summary>
/// Value comparers for the sequence types a row can hold - <c>T[]</c> (<c>byte[]</c> among them),
/// <c>List&lt;T&gt;</c> or an interface it implements, and <c>HashSet&lt;T&gt;</c> or a set interface it
/// implements. They compare element by element (as sets, for a set) and snapshot a copy, so the change
/// tracker's original value is not the very object the application then changes in place, and a
/// changed element is seen as a modification.
/// </summary>
internal static class BlazeDbSequenceComparer
{
    /// <summary>A comparer for <paramref name="clrType"/>, or null when it is not a sequence type this covers.</summary>
    public static ValueComparer? TryCreate(Type clrType)
    {
        if (clrType == typeof(string))
        {
            return null;
        }

        if (clrType.IsGenericType && clrType.GetGenericArguments() is [var member] &&
            clrType.IsAssignableFrom(typeof(HashSet<>).MakeGenericType(member)) &&
            !clrType.IsAssignableFrom(typeof(List<>).MakeGenericType(member)))
        {
            return (ValueComparer)Activator.CreateInstance(typeof(SetComparer<,>).MakeGenericType(clrType, member))!;
        }

        Type? element;
        if (clrType.IsArray)
        {
            element = clrType.GetArrayRank() == 1 ? clrType.GetElementType() : null;
        }
        else
        {
            element = clrType.IsGenericType && clrType.GetGenericArguments() is [var argument] &&
                      clrType.IsAssignableFrom(typeof(List<>).MakeGenericType(argument))
                ? argument
                : null;
        }
        if (element is null)
        {
            return null;
        }

        return (ValueComparer)Activator.CreateInstance(typeof(ElementwiseComparer<,>).MakeGenericType(clrType, element))!;
    }

    private sealed class ElementwiseComparer<TSequence, TElement> : ValueComparer<TSequence>
        where TSequence : class, IEnumerable<TElement>
    {
        public ElementwiseComparer()
            : base(
                (left, right) => AreEqual(left, right),
                sequence => HashOf(sequence),
                sequence => Copy(sequence))
        {
        }

        private static bool AreEqual(TSequence? left, TSequence? right) =>
            ReferenceEquals(left, right) || (left is not null && right is not null && left.SequenceEqual(right));

        private static int HashOf(TSequence? sequence)
        {
            var hash = new HashCode();
            if (sequence is not null)
            {
                foreach (var element in sequence)
                {
                    hash.Add(element);
                }
            }
            return hash.ToHashCode();
        }

        // An array type gets an array back; everything else this covers is List<T> or an interface
        // List<T> implements, so a list is a copy of the same type the property declares.
        private static TSequence Copy(TSequence? sequence) =>
            sequence is null
                ? null!
                : sequence is TElement[] array
                    ? (TSequence)(object)array.ToArray()
                    : (TSequence)(object)new List<TElement>(sequence);
    }

    private sealed class SetComparer<TSet, TElement> : ValueComparer<TSet>
        where TSet : class, IEnumerable<TElement>
    {
        public SetComparer()
            : base(
                (left, right) => AreEqual(left, right),
                set => HashOf(set),
                set => Copy(set))
        {
        }

        private static bool AreEqual(TSet? left, TSet? right) =>
            ReferenceEquals(left, right) || (left is not null && right is not null && new HashSet<TElement>(left).SetEquals(right));

        // Order-independent, as set equality is.
        private static int HashOf(TSet? set)
        {
            var hash = 0;
            if (set is not null)
            {
                foreach (var element in set)
                {
                    hash ^= element?.GetHashCode() ?? 0;
                }
            }
            return hash;
        }

        private static TSet Copy(TSet? set) => set is null ? null! : (TSet)(object)new HashSet<TElement>(set);
    }
}
