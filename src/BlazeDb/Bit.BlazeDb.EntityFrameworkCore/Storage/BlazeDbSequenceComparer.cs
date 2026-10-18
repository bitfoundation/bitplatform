using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Bit.BlazeDb.EntityFrameworkCore.Storage;

/// <summary>
/// Value comparers for the sequence types a row can hold - <c>T[]</c> (<c>byte[]</c> among them) and
/// <c>List&lt;T&gt;</c> or an interface it implements. They compare element by element and snapshot
/// a copy, so the change tracker's original value is not the very object the application then
/// changes in place, and a changed element is seen as a modification.
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
}
