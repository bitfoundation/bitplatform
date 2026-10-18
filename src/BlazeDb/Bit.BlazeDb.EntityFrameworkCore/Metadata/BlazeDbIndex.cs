namespace Bit.BlazeDb.EntityFrameworkCore.Metadata;

/// <summary>
/// One secondary index, described in the terms the translator reasons about: which properties it
/// covers, whether it can answer ranges as well as equality, and the type of its key - a tuple
/// when the index is compound, which is what a lookup against it has to be given. <c>DefaultComparer</c>
/// says whether it compares keys the way the type does (see
/// <see cref="BlazeDbIndexDefinition{TRow}.HasDefaultComparer"/>): one with a comparer of its own
/// still finds every row a C# <c>==</c> would - equal values are equal under any comparer - but may
/// find more, and its order is not the operators' order.
/// </summary>
internal sealed record BlazeDbIndex(
    string Name, IReadOnlyList<string> Members, bool Ordered, Type KeyType, object Definition, bool DefaultComparer)
{
    /// <summary>
    /// Whether what the index returns for a comparison is exactly what the C# operator would select,
    /// so the comparison can be dropped from the filter. That takes the default comparer, and a key
    /// with no floating-point part: <c>==</c> and <c>&lt;</c> are false for NaN, while the default
    /// comparers file NaN as equal to itself and below every other value, so an index hands back
    /// NaN rows a comparison would not.
    /// </summary>
    public bool Exact { get; } = DefaultComparer && !HasFloatingPoint(KeyType);

    /// <summary>Whether <paramref name="type"/> - or, for a compound key, any of its elements - is a floating-point type.</summary>
    public static bool HasFloatingPoint(Type type)
    {
        type = Nullable.GetUnderlyingType(type) ?? type;
        if (type == typeof(double) || type == typeof(float) || type == typeof(Half))
        {
            return true;
        }
        return type.IsGenericType &&
               type.FullName?.StartsWith("System.ValueTuple`", StringComparison.Ordinal) == true &&
               type.GetGenericArguments().Any(HasFloatingPoint);
    }
}
