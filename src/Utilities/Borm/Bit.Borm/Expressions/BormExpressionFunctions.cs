using System.Collections.Concurrent;

namespace Bit.Borm;

/// <summary>One function an expression may call.</summary>
internal sealed class BormExpressionFunction
{
    public required string Name { get; init; }
    public int MinArgs { get; init; }
    public int MaxArgs { get; init; } = int.MaxValue;

    /// <summary>Called with every argument already evaluated.</summary>
    public Func<IReadOnlyList<object?>, object?>? Body { get; init; }

    /// <summary>For a function that decides which arguments to evaluate at all — <c>iif</c>.</summary>
    public Func<IReadOnlyList<BormExpressionNode>, IBormDataReader, object?>? Lazy { get; init; }

    /// <summary>A check run when the expression is parsed; returns the problem, or null.</summary>
    public Func<IReadOnlyList<BormExpressionNode>, string?>? Check { get; init; }

    public bool BuiltIn { get; init; }
}

/// <summary>
/// The functions a <see cref="BormExpression"/> may call: the built-in set below, and any the
/// application registers with <see cref="Register"/>.
/// </summary>
/// <remarks>
/// <para>Built in:</para>
/// <list type="table">
/// <item><term>Logic</term><description><c>iif(condition, then, else)</c>, <c>coalesce(a, b, …)</c>, <c>isEmpty(x)</c>, <c>isNotEmpty(x)</c></description></item>
/// <item><term>Text</term><description><c>len(x)</c> (also <c>length</c>; counts a list's items), <c>lower</c>, <c>upper</c>, <c>trim</c>, <c>concat(…)</c>, <c>substring(text, start, length?)</c>, <c>contains(haystack, needle)</c>, <c>startsWith</c>, <c>endsWith</c>, <c>matches(text, 'pattern')</c></description></item>
/// <item><term>Numbers</term><description><c>abs</c>, <c>round(x, digits?)</c> (halves away from zero), <c>floor</c>, <c>ceil</c>, <c>number(x)</c>, <c>text(x)</c></description></item>
/// <item><term>Aggregates</term><description><c>sum</c>, <c>avg</c>, <c>min</c>, <c>max</c>, <c>count</c> — each takes values, lists, or both, and skips what has not been answered</description></item>
/// <item><term>Dates</term><description><c>today()</c>, <c>now()</c>, <c>year(d)</c>, <c>month(d)</c>, <c>day(d)</c>, <c>age(birthDate)</c>, <c>dateDiff(from, to, unit?)</c> (<c>'days'</c>, <c>'weeks'</c>, <c>'months'</c>, <c>'years'</c>, <c>'hours'</c>, <c>'minutes'</c>), <c>addDays</c>, <c>addMonths</c>, <c>addYears</c></description></item>
/// </list>
/// <para>
/// <c>matches</c> only takes its pattern as a literal written into the expression, never one read from
/// a field: a pattern a user can type is a pattern a user can make backtrack forever, and the match is
/// time-limited regardless.
/// </para>
/// </remarks>
public static class BormExpressionFunctions
{
    private static readonly ConcurrentDictionary<string, BormExpressionFunction> Functions = new(StringComparer.OrdinalIgnoreCase);

    static BormExpressionFunctions()
    {
        AddBuiltIns();
    }

    /// <summary>
    /// What <c>today()</c> and <c>now()</c> read the clock from — the system clock unless replaced (in a
    /// test, say, with a fixed one). The local time of whatever process evaluates the expression: on
    /// Blazor Server that is the server's zone, not the user's.
    /// </summary>
    public static TimeProvider TimeProvider { get; set; } = TimeProvider.System;

    /// <summary>The names of every function an expression may call, built in and registered.</summary>
    public static IReadOnlyCollection<string> Names => Functions.Keys.Order(StringComparer.OrdinalIgnoreCase).ToList();

    /// <summary>Whether an expression may call <paramref name="name"/>.</summary>
    public static bool IsDefined(string name) => Functions.ContainsKey(name);

    /// <summary>
    /// Makes <paramref name="body"/> callable from expressions as <paramref name="name"/> — a tax rate
    /// lookup, a checksum, a business rule the application owns.
    /// </summary>
    /// <remarks>
    /// This is the trust boundary: an expression from an untrusted document can call what is registered
    /// here and nothing else, so a registered function must be safe to call with any arguments. It is
    /// given every argument already evaluated (a missing answer as null) and should return null rather
    /// than throw for input it has no answer for. Register functions at startup, before any expression
    /// naming them is parsed; a built-in function cannot be replaced.
    /// </remarks>
    /// <exception cref="ArgumentException">The name is not an identifier, or names a built-in function.</exception>
    public static void Register(string name, Func<IReadOnlyList<object?>, object?> body, int minArgs = 0, int maxArgs = 16)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);
        ArgumentNullException.ThrowIfNull(body);
        if (!BormExpressionParser.IsIdentifier(name))
            throw new ArgumentException($"'{name}' is not a valid function name: use letters, digits and underscores, starting with a letter.", nameof(name));
        if (BormExpressionParser.IsKeyword(name))
            throw new ArgumentException($"'{name}' is a keyword of the expression language.", nameof(name));
        if (minArgs < 0 || maxArgs < minArgs)
            throw new ArgumentOutOfRangeException(nameof(maxArgs), "The argument counts must satisfy 0 <= minArgs <= maxArgs.");
        if (Functions.TryGetValue(name, out var existing) && existing.BuiltIn)
            throw new ArgumentException($"'{name}' is a built-in function and cannot be replaced.", nameof(name));

        Functions[name] = new BormExpressionFunction { Name = name, MinArgs = minArgs, MaxArgs = maxArgs, Body = body };
    }

    /// <summary>Removes a function registered with <see cref="Register"/>. Built-in functions stay.</summary>
    public static bool Unregister(string name)
        => Functions.TryGetValue(name, out var existing) && !existing.BuiltIn && Functions.TryRemove(name, out _);

    internal static BormExpressionFunction? Find(string name) => Functions.TryGetValue(name, out var f) ? f : null;

    private static void Add(string name, int min, int max, Func<IReadOnlyList<object?>, object?> body,
        Func<IReadOnlyList<BormExpressionNode>, string?>? check = null)
        => Functions[name] = new BormExpressionFunction
        {
            Name = name, MinArgs = min, MaxArgs = max, Body = body, Check = check, BuiltIn = true
        };

    private static void AddBuiltIns()
    {
        // ---- logic
        Functions["iif"] = new BormExpressionFunction
        {
            Name = "iif", MinArgs = 3, MaxArgs = 3, BuiltIn = true,
            Lazy = static (args, data) => BormExpressionValue.Collapse(
                BormExpressionValue.ToBoolean(args[0].Evaluate(data)) == true
                    ? args[1].Evaluate(data)
                    : args[2].Evaluate(data))
        };
        Add("coalesce", 1, int.MaxValue, static args =>
        {
            foreach (var arg in args)
                if (!BormConditionEvaluator.IsEmpty(arg)) return arg;
            return null;
        });
        Add("isEmpty", 1, 1, static args => BormConditionEvaluator.IsEmpty(args[0]));
        Add("isNotEmpty", 1, 1, static args => !BormConditionEvaluator.IsEmpty(args[0]));

        // ---- text
        Add("len", 1, 1, static args => Length(args[0]));
        Add("length", 1, 1, static args => Length(args[0]));
        Add("lower", 1, 1, static args => args[0] is null ? null : Text(args[0]).ToLowerInvariant());
        Add("upper", 1, 1, static args => args[0] is null ? null : Text(args[0]).ToUpperInvariant());
        Add("trim", 1, 1, static args => args[0] is null ? null : Text(args[0]).Trim());
        Add("concat", 1, int.MaxValue, static args => string.Concat(args.Select(Text)));
        Add("substring", 2, 3, static args =>
        {
            if (args[0] is null || !TryInt(args[1], out var start)) return null;
            var text = Text(args[0]);
            start = Math.Clamp(start, 0, text.Length);
            var length = text.Length - start;
            if (args.Count > 2 && (!TryInt(args[2], out length) || length < 0)) return null;
            return text.Substring(start, Math.Min(length, text.Length - start));
        });
        Add("contains", 2, 2, static args => BormConditionEvaluator.Contains(args[0], args[1]));
        Add("startsWith", 2, 2, static args => BormConditionEvaluator.StartsOrEnds(args[0], args[1], start: true));
        Add("endsWith", 2, 2, static args => BormConditionEvaluator.StartsOrEnds(args[0], args[1], start: false));
        Add("matches", 2, 2, static args =>
        {
            // Nothing given is not judged, as Pattern() does not judge it: a rule that answered false here put "must
            // look like this" under "This field is required." on an empty box. A condition reads unknown as false.
            if (args[0] is null or "") return BormExpressionValue.Unknown;
            var regex = BormRegex.Create(Text(args[1]));
            return BormRegex.IsMatch(regex, Text(args[0])) is { } matched ? matched : BormExpressionValue.Unknown;
        }, check: static args =>
            args[1] is not BormLiteralNode { Value: string pattern }
                ? "matches() takes its pattern as a quoted literal, not a value read from the form"
                : BormRegex.Create(pattern) is null ? $"'{pattern}' is not a valid regular expression" : null);

        // ---- numbers
        Add("abs", 1, 1, static args => Unary(args[0], Math.Abs, Math.Abs));
        Add("floor", 1, 1, static args => Unary(args[0], Math.Floor, Math.Floor));
        Add("ceil", 1, 1, static args => Unary(args[0], Math.Ceiling, Math.Ceiling));
        Add("round", 1, 2, static args =>
        {
            var digits = 0;
            if (args.Count > 1 && !TryInt(args[1], out digits)) return null;
            if (!BormExpressionValue.TryNumber(args[0], out var m, out var d, out var isDecimal)) return null;
            return isDecimal
                ? Math.Round(m, Math.Clamp(digits, 0, 28), MidpointRounding.AwayFromZero)
                : Math.Round(d, Math.Clamp(digits, 0, 15), MidpointRounding.AwayFromZero);
        });
        Add("number", 1, 1, static args => BormExpressionValue.AsNumber(args[0]));
        Add("text", 1, 1, static args => args[0] is null ? null : Text(args[0]));

        // ---- aggregates
        Add("sum", 1, int.MaxValue, static args =>
        {
            object? total = 0m;
            foreach (var item in BormExpressionValue.Flatten(args))
                if (BormExpressionValue.AsNumber(item) is { } n) total = BormExpressionValue.Arithmetic('+', total, n);
            return total;
        });
        Add("avg", 1, int.MaxValue, static args =>
        {
            object? total = 0m;
            var count = 0;
            foreach (var item in BormExpressionValue.Flatten(args))
            {
                if (BormExpressionValue.AsNumber(item) is not { } n) continue;
                total = BormExpressionValue.Arithmetic('+', total, n);
                count++;
            }
            return count == 0 ? null : BormExpressionValue.Arithmetic('/', total, count);
        });
        Add("min", 1, int.MaxValue, static args => Extreme(args, smallest: true));
        Add("max", 1, int.MaxValue, static args => Extreme(args, smallest: false));
        Add("count", 1, int.MaxValue, static args => BormExpressionValue.Flatten(args).Count(x => !BormConditionEvaluator.IsEmpty(x)));

        // ---- dates
        Add("today", 0, 0, static _ => TimeProvider.GetLocalNow().Date);
        Add("now", 0, 0, static _ => TimeProvider.GetLocalNow().DateTime);
        Add("year", 1, 1, static args => BormExpressionValue.TryDate(args[0], out var d) ? d.Year : null);
        Add("month", 1, 1, static args => BormExpressionValue.TryDate(args[0], out var d) ? d.Month : null);
        Add("day", 1, 1, static args => BormExpressionValue.TryDate(args[0], out var d) ? d.Day : null);
        Add("age", 1, 1, static args =>
        {
            if (!BormExpressionValue.TryDate(args[0], out var born)) return null;
            var today = TimeProvider.GetLocalNow().Date;
            var years = today.Year - born.Year;
            if (today.Month < born.Month || (today.Month == born.Month && today.Day < born.Day)) years--;
            return years;
        });
        Add("dateDiff", 2, 3, static args =>
        {
            if (!BormExpressionValue.TryDate(args[0], out var from) || !BormExpressionValue.TryDate(args[1], out var to))
                return null;
            var unit = args.Count > 2 ? Text(args[2]).Trim().ToLowerInvariant() : "days";
            return unit switch
            {
                "days" or "day" => (long)Math.Floor((to - from).TotalDays),
                "weeks" or "week" => (long)Math.Floor((to - from).TotalDays / 7),
                "hours" or "hour" => (long)Math.Floor((to - from).TotalHours),
                "minutes" or "minute" => (long)Math.Floor((to - from).TotalMinutes),
                "months" or "month" => WholeMonths(from, to),
                "years" or "year" => WholeMonths(from, to) / 12,
                _ => null
            };
        }, check: static args =>
            args.Count > 2 && args[2] is BormLiteralNode { Value: string unit }
            && unit.Trim().ToLowerInvariant() is not ("days" or "day" or "weeks" or "week" or "hours" or "hour"
                or "minutes" or "minute" or "months" or "month" or "years" or "year")
                ? $"'{unit}' is not a unit dateDiff() knows; use 'days', 'weeks', 'months', 'years', 'hours' or 'minutes'"
                : null);
        Add("addDays", 2, 2, static args => TryInt(args[1], out var n) ? BormExpressionValue.AddDays(args[0], n) : null);
        Add("addMonths", 2, 2, static args => AddMonths(args[0], args[1], 1));
        Add("addYears", 2, 2, static args => AddMonths(args[0], args[1], 12));
    }

    private static string Text(object? value) => BormExpressionValue.ToText(value);

    private static object Length(object? value) => value switch
    {
        null => 0,
        string s => s.Length,
        System.Collections.ICollection c => c.Count,
        System.Collections.IEnumerable e => e.Cast<object?>().Count(),
        _ => Text(value).Length
    };

    private static bool TryInt(object? value, out int result)
    {
        result = 0;
        if (!BormExpressionValue.TryNumber(value, out var m, out var d, out var isDecimal)) return false;
        var number = isDecimal ? (double)m : d;
        // Only a whole number counts: substring({x}, 1.5) is a mistake to report as no answer, not to round.
        if (isDecimal ? m != decimal.Truncate(m) : !double.IsInteger(d)) return false;
        if (number is < int.MinValue or > int.MaxValue) return false;
        result = (int)number;
        return true;
    }

    private static object? Unary(object? value, Func<decimal, decimal> exact, Func<double, double> approximate)
    {
        if (!BormExpressionValue.TryNumber(value, out var m, out var d, out var isDecimal)) return null;
        return isDecimal ? exact(m) : approximate(d);
    }

    /// <summary>The smallest or largest of the answers: numbers by value, dates by time.</summary>
    private static object? Extreme(IReadOnlyList<object?> args, bool smallest)
    {
        object? best = null;
        foreach (var item in BormExpressionValue.Flatten(args))
        {
            var candidate = BormExpressionValue.AsNumber(item) ?? (BormExpressionValue.TryDate(item, out _) ? item : null);
            if (candidate is null) continue;
            if (best is null) { best = candidate; continue; }
            if (BormExpressionValue.Compare(candidate, best, c => smallest ? c < 0 : c > 0) is true) best = candidate;
        }
        return best;
    }

    private static long WholeMonths(DateTime from, DateTime to)
    {
        var months = (to.Year - from.Year) * 12L + to.Month - from.Month;
        // A month is only whole once its day has come round: 31 Jan to 28 Feb is no month at all.
        if (months > 0 && to.Day < from.Day) months--;
        if (months < 0 && to.Day > from.Day) months++;
        return months;
    }

    private static object? AddMonths(object? date, object? count, int factor)
    {
        if (!TryInt(count, out var n)) return null;
        try
        {
            return date switch
            {
                DateOnly d => d.AddMonths(checked(n * factor)),
                DateTime dt => dt.AddMonths(checked(n * factor)),
                DateTimeOffset dto => dto.AddMonths(checked(n * factor)),
                _ => null
            };
        }
        catch (Exception ex) when (ex is ArgumentOutOfRangeException or OverflowException)
        {
            return null;
        }
    }
}
