using System.Globalization;

namespace Bit.BlazeDb.EntityFrameworkCore.Query;

/// <summary>
/// Brings a value read out of a LINQ predicate to the type an index (or the primary key) is keyed
/// on. C# widens the operands of a comparison before it is compared - <c>row.Kind == Kind.A</c> is
/// really <c>(int)row.Kind == 0</c>, and <c>row.Age &gt; 5</c> on a <c>byte</c> property compares
/// two <c>int</c>s - so the boxed value in the tree is often not of the property's type. The engine
/// unboxes the key inside its generic index store, and unboxing an <c>int</c> as an enum or a
/// <c>byte</c> throws; this converts first, and refuses when a lossless conversion is not possible.
/// </summary>
internal static class BlazeDbKeyCoercion
{
    public static bool TryCoerce(object? value, Type targetType, out object result)
    {
        result = null!;
        if (value is null)
        {
            // There is nothing to convert. Whether a null can be looked up depends on the index it
            // is aimed at - its key type has to be able to hold one - which is the selector's call.
            return false;
        }

        var target = Nullable.GetUnderlyingType(targetType) ?? targetType;
        var source = value.GetType();
        if (source == target)
        {
            result = value;
            return true;
        }

        try
        {
            if (target.IsEnum)
            {
                // Go through the enum's underlying type with the same round-trip check as any other
                // integer, so a value outside it is refused rather than truncated onto a real member.
                if (source.IsEnum || !IsIntegral(source) ||
                    !TryCoerce(value, Enum.GetUnderlyingType(target), out var underlying))
                {
                    return false;
                }
                result = Enum.ToObject(target, underlying);
                return true;
            }

            if (source.IsEnum && IsIntegral(target))
            {
                result = Convert.ChangeType(value, target, CultureInfo.InvariantCulture);
                return true;
            }

            if (IsNumeric(source) && IsNumeric(target))
            {
                var converted = Convert.ChangeType(value, target, CultureInfo.InvariantCulture);
                // A conversion that does not round-trip changed the value (5.5 to 5, 300 to 44),
                // which would make the lookup answer a different question than the predicate asks.
                var roundTrip = Convert.ChangeType(converted, source, CultureInfo.InvariantCulture);
                if (!Equals(roundTrip, value))
                {
                    return false;
                }
                result = converted;
                return true;
            }
        }
        catch (Exception ex) when (ex is InvalidCastException or FormatException or OverflowException)
        {
            return false;
        }

        // Anything else (a string against a Guid property, a DateTimeOffset against a DateTime)
        // is a comparison the compiler would only have accepted through an operator the engine
        // does not model; the predicate stays a residual filter and remains correct.
        if (target.IsInstanceOfType(value))
        {
            result = value;
            return true;
        }
        return false;
    }

    private static bool IsIntegral(Type type) =>
        type == typeof(byte) || type == typeof(sbyte) || type == typeof(short) || type == typeof(ushort) ||
        type == typeof(int) || type == typeof(uint) || type == typeof(long) || type == typeof(ulong) ||
        type == typeof(char);

    private static bool IsNumeric(Type type) =>
        IsIntegral(type) || type == typeof(float) || type == typeof(double) || type == typeof(decimal);
}
