using System.Collections;
using System.Globalization;

namespace Bit.Borm;

/// <summary>
/// The value semantics of <see cref="BormExpression"/>: what counts as true, how numbers, dates and
/// text combine, and the difference between a missing value and an unknown answer.
/// </summary>
/// <remarks>
/// <para>
/// Two kinds of "nothing" are kept apart. A field with no answer reads as <c>null</c>, and in a
/// yes/no position null is no — an unticked box is not ticked, which is what
/// <see cref="BormConditionOperator.IsFalse"/> has always said. A comparison that cannot be made
/// because an operand is missing is <see cref="Unknown"/> instead, and that one follows SQL's
/// three-valued logic: <c>unknown and true</c> is still unknown, and a validation rule that comes out
/// unknown passes rather than complaining about a value nobody has entered yet.
/// </para>
/// <para>
/// Numbers are carried as <see cref="decimal"/> wherever they fit, so <c>0.1 + 0.2</c> is
/// <c>0.3</c> and a line total in money is exact; a value beyond decimal's range falls back to
/// <see cref="double"/>. Text is never read in the ambient culture.
/// </para>
/// </remarks>
internal static class BormExpressionValue
{
    /// <summary>The answer to a comparison that could not be made. Never stored and never returned publicly.</summary>
    public static readonly object Unknown = new UnknownValue();

    private sealed class UnknownValue
    {
        public override string ToString() => "unknown";
    }

    /// <summary>Turns <see cref="Unknown"/> into the null the outside world sees.</summary>
    public static object? Collapse(object? value) => ReferenceEquals(value, Unknown) ? null : value;

    /// <summary>
    /// Reads a value as a truth value: true, false, or null for <see cref="Unknown"/>. A missing value
    /// is false; so are an empty text, an empty list, zero and the text <c>"false"</c>.
    /// </summary>
    public static bool? ToBoolean(object? value) => value switch
    {
        _ when ReferenceEquals(value, Unknown) => null,
        null => false,
        bool b => b,
        string s when bool.TryParse(s.Trim(), out var parsed) => parsed,
        string s => !string.IsNullOrWhiteSpace(s),
        _ when BormNumber.IsNumber(value) => BormNumber.TryToDouble(value, out var d) && d != 0,
        _ => !BormConditionEvaluator.IsEmpty(value)
    };

    /// <summary>Boxes a three-valued truth value back into an expression value.</summary>
    public static object FromBoolean(bool? value) => value is { } b ? b : Unknown;

    // ---------------------------------------------------------------- numbers

    /// <summary>
    /// Reads a value as a number. Text is accepted when it is a plain invariant number ("12", "-3.5"),
    /// because a dictionary-backed text field holds what was typed; a boolean is not a number.
    /// </summary>
    public static bool TryNumber(object? value, out decimal number, out double fallback, out bool isDecimal)
    {
        fallback = 0;
        isDecimal = false;
        number = 0;

        if (value is string text)
        {
            if (decimal.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out number))
            {
                isDecimal = true;
                return true;
            }
            if (double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out fallback) && double.IsFinite(fallback))
                return true;
            return false;
        }

        if (!BormNumber.IsNumber(value)) return false;
        if (BormNumber.TryToDecimal(value, out number))
        {
            isDecimal = true;
            return true;
        }
        return BormNumber.TryToDouble(value, out fallback) && !double.IsNaN(fallback);
    }

    /// <summary>Applies an arithmetic operator to two numbers, or null when either is not one.</summary>
    public static object? Arithmetic(char op, object? left, object? right)
    {
        if (!TryNumber(left, out var lm, out var ld, out var ldec) || !TryNumber(right, out var rm, out var rd, out var rdec))
            return null;

        if (ldec && rdec)
        {
            try
            {
                switch (op)
                {
                    case '+': return lm + rm;
                    case '-': return lm - rm;
                    case '*': return lm * rm;
                    case '/': return rm == 0 ? null : lm / rm;
                    case '%': return rm == 0 ? null : lm % rm;
                }
            }
            catch (OverflowException)
            {
                // Beyond decimal's range: carried on in double below, which states it approximately.
            }
        }

        var a = ldec ? (double)lm : ld;
        var b = rdec ? (double)rm : rd;
        var result = op switch
        {
            '+' => a + b,
            '-' => a - b,
            '*' => a * b,
            '/' => b == 0 ? double.NaN : a / b,
            '%' => b == 0 ? double.NaN : a % b,
            _ => double.NaN
        };
        return double.IsFinite(result) ? result : null;
    }

    public static object? Negate(object? value)
    {
        if (!TryNumber(value, out var m, out var d, out var isDecimal)) return null;
        return isDecimal ? -m : -d;
    }

    /// <summary>Reads a value as a number to aggregate, or null when it is not one.</summary>
    public static object? AsNumber(object? value)
        => TryNumber(value, out var m, out var d, out var isDecimal) ? isDecimal ? m : d : null;

    // ---------------------------------------------------------------- dates

    /// <summary>
    /// Reads a value as a point in time. A <see cref="DateTimeOffset"/> is read on its own clock, the
    /// way its input shows it; text has to be ISO 8601 (<c>2024-05-01</c>, <c>2024-05-01T09:30</c>).
    /// </summary>
    public static bool TryDate(object? value, out DateTime date)
    {
        switch (value)
        {
            case DateTime dt: date = dt; return true;
            case DateOnly d: date = d.ToDateTime(TimeOnly.MinValue); return true;
            case DateTimeOffset dto: date = dto.DateTime; return true;
            case string s when s.Length >= 7 && char.IsDigit(s[0]):
                // A month (2024-05) or an ISO week (2024-W19) as a picker writes it reads as its first day.
                return BormPeriod.TryParseAny(s, out date)
                       || (s.Length >= 8 && DateTime.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.None, out date));
            default:
                date = default;
                return false;
        }
    }

    private static bool IsDate(object? value) => value is DateTime or DateOnly or DateTimeOffset;

    /// <summary>
    /// A date moved by a number of days, keeping the kind of date it was: a <see cref="DateOnly"/>
    /// stays a date, and a timestamp keeps its offset.
    /// </summary>
    public static object? AddDays(object? date, double days)
    {
        try
        {
            return date switch
            {
                // DateOnly spans 3,652,059 days; a larger whole number would wrap when cast to int.
                DateOnly d when double.IsInteger(days) && Math.Abs(days) <= 3_652_059 => d.AddDays((int)days),
                DateOnly d => d.ToDateTime(TimeOnly.MinValue).AddDays(days),
                DateTime dt => dt.AddDays(days),
                DateTimeOffset dto => dto.AddDays(days),
                _ => null
            };
        }
        catch (ArgumentOutOfRangeException)
        {
            return null;
        }
    }

    /// <summary>
    /// <c>+</c>: numbers add, a date and a number move the date by that many days, and anything
    /// involving text joins as text — with a missing part joining as nothing, so
    /// <c>{First} + ' ' + {Last}</c> still reads well with no last name.
    /// </summary>
    public static object? Add(object? left, object? right)
    {
        if (IsDate(left) && TryNumber(right, out _, out _, out _)) return AddDays(left, ToDouble(right));
        if (IsDate(right) && TryNumber(left, out _, out _, out _)) return AddDays(right, ToDouble(left));
        if (left is string || right is string) return ToText(left) + ToText(right);
        return Arithmetic('+', left, right);
    }

    /// <summary>
    /// A value as text: invariant numbers and dates, <c>true</c>/<c>false</c> in lower case as JSON
    /// writes them, and a missing value as nothing.
    /// </summary>
    public static string ToText(object? value) => value switch
    {
        bool b => b ? "true" : "false",
        _ => BormValueConverter.ToInvariantString(value)
    };

    /// <summary><c>-</c>: numbers subtract, a date minus a number moves it back, and two dates give the days between them.</summary>
    public static object? Subtract(object? left, object? right)
    {
        if (IsDate(left))
        {
            if (TryNumber(right, out _, out _, out _)) return AddDays(left, -ToDouble(right));
            if (TryDate(left, out var a) && TryDate(right, out var b))
                return BormNumber.TryToDecimalAtFifteenDigits((a - b).TotalDays, out var days) ? days : (a - b).TotalDays;
            return null;
        }
        return Arithmetic('-', left, right);
    }

    private static double ToDouble(object? value)
        => TryNumber(value, out var m, out var d, out var isDecimal) ? isDecimal ? (double)m : d : double.NaN;

    // ---------------------------------------------------------------- comparison

    /// <summary>
    /// An ordering comparison, judged exactly as <see cref="BormFieldCondition"/> judges one: numbers
    /// by value, dates against ISO text, everything else by its own ordering. Unknown when either side
    /// is missing or the two cannot be ordered against each other.
    /// </summary>
    public static object Compare(object? left, object? right, Func<int, bool> accept)
    {
        if (left is null || right is null) return Unknown;
        // A DateOnly field against today() (a DateTime), or either against ISO text: compared as the
        // points in time they are, whatever CLR type each arrived as.
        if ((IsDate(left) || IsDate(right)) && TryDate(left, out var a) && TryDate(right, out var b))
            return accept(a.CompareTo(b));
        return BormConditionEvaluator.TryCompare(left, right, out var order) ? accept(order) : Unknown;
    }

    /// <summary>
    /// <c>=</c>: <see cref="BormFieldCondition"/>'s loose equality — numbers by value, text without
    /// regard to case — with dates compared as points in time across their CLR types.
    /// </summary>
    public static bool Equal(object? left, object? right)
    {
        if ((IsDate(left) || IsDate(right)) && TryDate(left, out var a) && TryDate(right, out var b))
            return a == b;
        return BormConditionEvaluator.LooseEquals(left, right);
    }

    // ---------------------------------------------------------------- collections

    /// <summary>
    /// Flattens arguments for an aggregate: a list contributes its items, anything else itself, and
    /// missing values are dropped — <c>sum({A}, {B})</c> adds what has been answered.
    /// </summary>
    public static IEnumerable<object?> Flatten(IReadOnlyList<object?> args)
    {
        foreach (var arg in args)
        {
            if (arg is null || ReferenceEquals(arg, Unknown)) continue;
            if (arg is IEnumerable items and not string)
            {
                foreach (var item in items)
                    if (item is not null) yield return item;
            }
            else
            {
                yield return arg;
            }
        }
    }
}
