using System.Globalization;

namespace Bit.Bheadless;

/// <summary>
/// An inclusive range of dates: the value of a range <see cref="BitBhCalendar"/> (<c>@bind-Range</c>).
/// <see cref="Start"/> is never after <see cref="End"/>: the constructor swaps them when they are given the other way round.
/// A one-day range has the same start and end.
/// </summary>
public sealed record BitBhDateRange
{
    /// <summary>Creates a range; the earlier of the two dates becomes <see cref="Start"/>.</summary>
    public BitBhDateRange(DateOnly start, DateOnly end)
    {
        if (end < start) (start, end) = (end, start);
        Start = start;
        End = end;
    }

    /// <summary>The first date of the range.</summary>
    public DateOnly Start { get; }

    /// <summary>The last date of the range (included).</summary>
    public DateOnly End { get; }

    /// <summary>The number of days in the range, both ends included.</summary>
    public int Days => End.DayNumber - Start.DayNumber + 1;

    /// <summary>Whether <paramref name="date"/> is in the range (both ends included).</summary>
    public bool Contains(DateOnly date) => date >= Start && date <= End;

    // ── What can be picked: the same rules for the calendar and the range picker ──

    /// <summary>The longest stretch of days walked one by one for an unavailable date: what a person could pick by hand.</summary>
    internal const int MaxCheckedDays = 3700;

    /// <summary>Whether <paramref name="date"/> can be picked: within <paramref name="min"/> / <paramref name="max"/> and not <paramref name="unavailable"/>.</summary>
    internal static bool IsSelectable(DateOnly date, DateOnly? min, DateOnly? max, Func<DateOnly, bool>? unavailable)
        => (min is not { } low || date >= low) && (max is not { } high || date <= high) && !(unavailable?.Invoke(date) ?? false);

    /// <summary>
    /// Whether the range can be picked: both ends can (<see cref="IsSelectable(DateOnly, DateOnly?, DateOnly?, Func{DateOnly, bool}?)"/>)
    /// and, unless <paramref name="allowsNonContiguous"/>, it spans no unavailable date.
    /// </summary>
    internal bool IsSelectable(DateOnly? min, DateOnly? max, Func<DateOnly, bool>? unavailable, bool allowsNonContiguous)
        => IsSelectable(Start, min, max, unavailable) && IsSelectable(End, min, max, unavailable)
           && (allowsNonContiguous || !SpansUnavailable(unavailable));

    /// <summary>
    /// Whether a date of the range is <paramref name="unavailable"/>. A range longer than <see cref="MaxCheckedDays"/> is
    /// not walked day by day: false.
    /// </summary>
    internal bool SpansUnavailable(Func<DateOnly, bool>? unavailable)
    {
        if (unavailable is null || Days > MaxCheckedDays) return false;
        for (var day = Start.DayNumber; day <= End.DayNumber; day++)
            if (unavailable(DateOnly.FromDayNumber(day))) return true;
        return false;
    }

    /// <summary>Deconstructs the range into its start and end.</summary>
    public void Deconstruct(out DateOnly start, out DateOnly end)
    {
        start = Start;
        end = End;
    }

    /// <summary>The range as an ISO 8601 interval: <c>2026-11-08/2026-11-12</c> (what a form posts).</summary>
    public override string ToString()
        => string.Create(CultureInfo.InvariantCulture, $"{Start:yyyy-MM-dd}/{End:yyyy-MM-dd}");
}
