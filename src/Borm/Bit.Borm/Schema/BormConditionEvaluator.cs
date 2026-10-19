using System.Collections;

namespace Bit.Borm;

/// <summary>
/// Compares values for <see cref="BormFieldCondition"/>. Handles loose, culture-invariant
/// comparison so conditions authored in JSON (strings) work against typed model values.
/// </summary>
public static class BormConditionEvaluator
{
    public static bool Compare(object? actual, BormConditionOperator op, object? expected) => op switch
    {
        BormConditionOperator.IsEmpty => IsEmpty(actual),
        BormConditionOperator.IsNotEmpty => !IsEmpty(actual),
        BormConditionOperator.IsTrue => AsBool(actual) == true,
        // An unanswered checkbox is not true, and for a yes/no question there is nothing else it can
        // be. Reading a missing value as neither true nor false hid a field under *both* branches of
        // the same question — which a dictionary-backed form met immediately, since an untouched
        // checkbox has no entry at all while a `bool` on a typed model already reads false.
        // A non-empty value that is not a boolean is still neither.
        BormConditionOperator.IsFalse => AsBool(actual) == false || IsEmpty(actual),
        BormConditionOperator.Equals => LooseEquals(actual, expected),
        BormConditionOperator.NotEquals => !LooseEquals(actual, expected),
        BormConditionOperator.GreaterThan => TryCompare(actual, expected, out var g) && g > 0,
        BormConditionOperator.GreaterThanOrEqual => TryCompare(actual, expected, out var ge) && ge >= 0,
        BormConditionOperator.LessThan => TryCompare(actual, expected, out var l) && l < 0,
        BormConditionOperator.LessThanOrEqual => TryCompare(actual, expected, out var le) && le <= 0,
        BormConditionOperator.Contains => Contains(actual, expected),
        BormConditionOperator.NotContains => !Contains(actual, expected),
        BormConditionOperator.In => In(actual, expected),
        BormConditionOperator.NotIn => !In(actual, expected),
        BormConditionOperator.StartsWith => StartsOrEnds(actual, expected, start: true),
        BormConditionOperator.EndsWith => StartsOrEnds(actual, expected, start: false),
        BormConditionOperator.Matches => Matches(actual, expected),
        _ => false
    };

    private static bool IsEmpty(object? value) => value switch
    {
        null => true,
        string s => string.IsNullOrWhiteSpace(s),
        // A [Flags] multi-select with nothing ticked, the same empty set an empty List<TEnum> is.
        Enum e => BormEnumOptions.IsEmptyFlags(e),
        ICollection c => c.Count == 0,
        IEnumerable e and not string => !e.Cast<object?>().Any(),
        _ => false
    };

    private static bool? AsBool(object? value) => value switch
    {
        bool b => b,
        string s when bool.TryParse(s, out var b) => b,
        _ => null
    };

    private static bool LooseEquals(object? a, object? b)
    {
        if (a is null && b is null) return true;
        if (a is null || b is null) return false;
        if (a.Equals(b)) return true;

        // Numbers compare numerically (1 == 1.0 == "1"), everything else by invariant text. At least one
        // side has to actually be a number: two pieces of text are compared as text, so a zip code of
        // "01234" is not "1234" and a code of "007" is not in ["7"] merely because both parse.
        if ((BormNumber.IsNumber(a) || BormNumber.IsNumber(b))
            && BormNumber.TryToDouble(a, out var da) && BormNumber.TryToDouble(b, out var db))
            return da.Equals(db);

        // A typed value — a date, a time, an enum member — against the text a condition authored in
        // JSON carries: the text is read as that type, exactly as the ordering operators already did.
        // Compared as text instead, a DateTime spelled itself "01/01/2024 00:00:00" and never equalled
        // "2024-01-01", so Equals was false on the very date GreaterThanOrEqual and LessThanOrEqual
        // both accepted.
        if (SameValueOnceTyped(a, b) || SameValueOnceTyped(b, a)) return true;

        return string.Equals(
            BormValueConverter.ToInvariantString(a),
            BormValueConverter.ToInvariantString(b),
            StringComparison.OrdinalIgnoreCase);
    }

    private static bool TryCompare(object? a, object? b, out int result)
    {
        result = 0;
        if (a is null || b is null) return false;

        if (BormNumber.TryToDouble(a, out var da) && BormNumber.TryToDouble(b, out var db))
        {
            result = da.CompareTo(db);
            return true;
        }

        // Dates and other IComparable values: coerce the expected operand to the actual value's type
        // so a condition authored as the string "2024-01-01" still compares against a DateTime.
        if (a is IComparable ca)
        {
            if (a.GetType() == b.GetType())
            {
                result = ca.CompareTo(b);
                return true;
            }
            if (BormValueConverter.TryCoerce(b, a.GetType(), out var coerced) && coerced is not null)
            {
                result = ca.CompareTo(coerced);
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Whether <paramref name="other"/>, read as the type of <paramref name="typed"/>, is the same value.
    /// Only a typed (non-text) value sets the type, so two strings keep their case-insensitive text
    /// comparison and a string is never forced into another string's culture-sensitive ordering.
    /// </summary>
    private static bool SameValueOnceTyped(object typed, object other)
    {
        if (typed is string || typed.GetType() == other.GetType()) return false;
        // A bool already compares by its text ("True" == "true"), and reading arbitrary text as a bool
        // makes everything that is not "true" equal to false.
        if (typed is bool || typed is not IComparable comparable) return false;

        // Blank text is no value of any type, and asking whether it is one is wasted work on a path
        // every render takes. Between two non-text values only an enum member and its number are worth
        // reconciling; anything else (a date against a number) is simply not equal — and converting it
        // would only throw to say so.
        if (other is string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return false;
        }
        else if (typed is not Enum)
        {
            return false;
        }

        return BormValueConverter.TryCoerce(other, typed.GetType(), out var coerced)
               && coerced is not null
               && comparable.CompareTo(coerced) == 0;
    }

    private static bool Contains(object? actual, object? expected)
    {
        if (expected is null) return false;

        // A [Flags] value is a set of members; "contains Write" asks whether that flag is set. It is not
        // a collection to enumerate or text to search, so it used to answer false whatever was ticked.
        if (actual is Enum flags && flags.GetType().IsDefined(typeof(FlagsAttribute), inherit: false))
        {
            if (!BormValueConverter.TryCoerce(expected, flags.GetType(), out var flag) || flag is not Enum member)
                return false;
            // The zero member is the empty set, which every value "contains" by HasFlag's arithmetic.
            return BormEnumOptions.IsEmptyFlags(member) ? BormEnumOptions.IsEmptyFlags(flags) : flags.HasFlag(member);
        }

        if (actual is string s)
            return s.Contains(BormValueConverter.ToInvariantString(expected), StringComparison.OrdinalIgnoreCase);

        if (actual is IEnumerable e and not string)
            return e.Cast<object?>().Any(x => LooseEquals(x, expected));

        return false;
    }

    private static bool In(object? actual, object? expected)
    {
        if (expected is IEnumerable e and not string)
            return e.Cast<object?>().Any(x => LooseEquals(actual, x));
        return LooseEquals(actual, expected);
    }

    private static bool StartsOrEnds(object? actual, object? expected, bool start)
    {
        if (actual is null || expected is null) return false;
        var haystack = BormValueConverter.ToInvariantString(actual);
        var needle = BormValueConverter.ToInvariantString(expected);
        return start
            ? haystack.StartsWith(needle, StringComparison.OrdinalIgnoreCase)
            : haystack.EndsWith(needle, StringComparison.OrdinalIgnoreCase);
    }

    private static bool Matches(object? actual, object? expected)
    {
        if (actual is null || expected is null) return false;
        // Patterns can come from untrusted JSON: compiled with a timeout, and a pattern that neither
        // compiles nor completes simply does not match rather than taking the render down.
        var regex = BormRegex.Create(BormValueConverter.ToInvariantString(expected));
        if (regex is null) return false;
        return BormRegex.IsMatch(regex, BormValueConverter.ToInvariantString(actual)) == true;
    }
}
