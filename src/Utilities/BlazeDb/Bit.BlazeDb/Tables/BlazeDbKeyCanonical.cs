using System.Runtime.CompilerServices;

namespace Bit.BlazeDb;

/// <summary>
/// Puts a primary key in one canonical form before it is encoded, so two keys the table treats as equal are always
/// written as the same bytes.
/// <para>
/// A table matches keys with <see cref="EqualityComparer{T}.Default"/>, and for a few key types that equality ignores
/// part of what the encoding records: a <see cref="decimal"/>'s scale (<c>1.0m == 1.00m</c>), a <see cref="DateTime"/>'s
/// kind, a <see cref="DateTimeOffset"/>'s offset, and the sign of a floating-point zero or the payload of a NaN. A table
/// that is registered decodes keys and compares them that way, so it never notices. A table opened without its
/// descriptor (<see cref="BlazeDbOpaqueTable"/>) can only compare bytes, though: a delete written with <c>1.00m</c>
/// would miss the row stored under <c>1.0m</c>, and the row would come back once the descriptor did. Rows keep the key
/// exactly as the application set it; only the key bytes in front of them are canonical.
/// </para>
/// <para>
/// Keys written before this existed keep their bytes, so a table carried without its descriptor still matches those
/// by their original spelling; a registered table, and a checkpoint of it, matches them by value either way.
/// </para>
/// </summary>
internal static class BlazeDbKeyCanonical
{
    /// <summary><paramref name="key"/> in its canonical form; the same value for every type that needs none.</summary>
    public static TKey Of<TKey>(TKey key)
    {
        // Each branch folds away for every other TKey when the method is compiled for it, so the common key types
        // pay nothing.
        if (typeof(TKey) == typeof(decimal))
        {
            var value = Normalize(Unsafe.As<TKey, decimal>(ref key));
            return Unsafe.As<decimal, TKey>(ref value);
        }
        if (typeof(TKey) == typeof(DateTime))
        {
            var value = DateTime.SpecifyKind(Unsafe.As<TKey, DateTime>(ref key), DateTimeKind.Unspecified);
            return Unsafe.As<DateTime, TKey>(ref value);
        }
        if (typeof(TKey) == typeof(DateTimeOffset))
        {
            var value = new DateTimeOffset(Unsafe.As<TKey, DateTimeOffset>(ref key).UtcTicks, TimeSpan.Zero);
            return Unsafe.As<DateTimeOffset, TKey>(ref value);
        }
        if (typeof(TKey) == typeof(double))
        {
            var value = Unsafe.As<TKey, double>(ref key);
            value = double.IsNaN(value) ? double.NaN : value == 0 ? 0d : value;
            return Unsafe.As<double, TKey>(ref value);
        }
        if (typeof(TKey) == typeof(float))
        {
            var value = Unsafe.As<TKey, float>(ref key);
            value = float.IsNaN(value) ? float.NaN : value == 0 ? 0f : value;
            return Unsafe.As<float, TKey>(ref value);
        }
        if (typeof(TKey) == typeof(Half))
        {
            var value = Unsafe.As<TKey, Half>(ref key);
            value = Half.IsNaN(value) ? Half.NaN : value == Half.Zero ? Half.Zero : value;
            return Unsafe.As<Half, TKey>(ref value);
        }
        return key;
    }

    /// <summary>The decimal with no trailing zeros after the point, and zero without a sign: <c>1.50m</c> becomes <c>1.5m</c>.</summary>
    internal static decimal Normalize(decimal value)
    {
        if (value == 0m)
        {
            return 0m;
        }
        for (var scale = value.Scale; scale > 0; scale--)
        {
            var shorter = decimal.Round(value, scale - 1);
            if (shorter != value)
            {
                break;
            }
            value = shorter;
        }
        return value;
    }
}
