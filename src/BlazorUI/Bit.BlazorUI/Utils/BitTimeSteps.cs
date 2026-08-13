namespace Bit.BlazorUI;

/// <summary>
/// The arithmetic the time pickers navigate the parts of a time of day with. A part is a value on a range
/// that wraps - the 24 hours of a day, the 60 minutes of an hour - and the step of a picker lays a grid
/// over that range which every value its controls produce sits on.
/// </summary>
/// <remarks>
/// The rest of what a picker allows - the allowed-value predicates, the bounds - filters the same range, so
/// all of it reaches these helpers as the one predicate they take: whatever it accepts is where the picker
/// can be moved to, and none of them has to know which constraint refused a value. A predicate that accepts
/// nothing leaves every one of them returning null, which the callers read as "leave the value alone".
/// </remarks>
internal static class BitTimeSteps
{
    /// <summary>
    /// The last time of day, which is where a bound above the end of a day is pulled back to.
    /// </summary>
    private static readonly TimeSpan EndOfDay = new(23, 59, 59);

    /// <summary>
    /// A bound pulled back into a single day. Every part of a picker works on a time of day, so a bound that
    /// falls outside of one has to be brought into it before anything is compared against it: without this
    /// the typed input would compare the whole span while the controls work on the parts of it, and the two
    /// would disagree about the same value.
    /// </summary>
    public static TimeSpan? ClampToDay(TimeSpan? time)
    {
        if (time.HasValue is false) return null;

        if (time.Value < TimeSpan.Zero) return TimeSpan.Zero;

        return time.Value > EndOfDay ? EndOfDay : time.Value;
    }

    /// <summary>
    /// The value brought back into the range, so a part that runs off one end of the clock face carries on
    /// from the other.
    /// </summary>
    public static int Wrap(int value, int range)
    {
        return ((value % range) + range) % range;
    }

    /// <summary>
    /// The step brought into a range it can lay a grid over: a step below one would leave the controls
    /// pressing without moving, and one of a whole range or more would leave a single value on the grid.
    /// </summary>
    private static int SafeStep(int step, int range)
    {
        if (step < 1) return 1;

        return step > range ? range : step;
    }

    /// <summary>
    /// Whether the value sits on the grid the step lays over the range.
    /// </summary>
    /// <param name="value">The part of the time being judged.</param>
    /// <param name="step">The step of the picker, which is the spacing of the grid.</param>
    /// <param name="anchor">
    /// Where the grid starts, which is the same part of the minimum time the application declared - so a
    /// picker whose range begins at 09:07 can still be set to 09:07 - and the top of the range without one.
    /// </param>
    /// <param name="range">The range the part wraps in: 24 for an hour of the day, 60 for a minute or a second.</param>
    public static bool IsOnGrid(int value, int step, int anchor, int range)
    {
        step = SafeStep(step, range);

        if (step == 1) return true;

        return Wrap(value - anchor, range) % step == 0;
    }

    /// <summary>
    /// The grid point nearest to the value, which is where a pointer that comes to rest between two of them
    /// settles. The grid is never empty, so this always lands somewhere.
    /// </summary>
    public static int SnapToGrid(int value, int step, int anchor, int range)
    {
        value = Wrap(value, range);

        return FindNearestAllowed(value, range, v => IsOnGrid(v, step, anchor, range)) ?? value;
    }

    /// <summary>
    /// The first value from the candidate on that the predicate accepts, walking the range in one direction
    /// only and wrapping around the end of it.
    /// </summary>
    public static int? FindAllowedFrom(int value, bool forward, int range, Func<int, bool> isAllowed)
    {
        for (var offset = 0; offset < range; offset++)
        {
            var candidate = Wrap(value + ((forward ? 1 : -1) * offset), range);

            if (isAllowed(candidate)) return candidate;
        }

        return null;
    }

    /// <summary>
    /// The value nearest to the given one that the predicate accepts. The distance is measured on the
    /// wrapping range - 23 sits next to 0 - preferring the exact value, then the closer of the two sides,
    /// the lower one on a tie.
    /// </summary>
    public static int? FindNearestAllowed(int value, int range, Func<int, bool> isAllowed)
    {
        for (var offset = 0; offset < range; offset++)
        {
            var lower = Wrap(value - offset, range);
            if (isAllowed(lower)) return lower;

            var upper = Wrap(value + offset, range);
            if (isAllowed(upper)) return upper;
        }

        return null;
    }

    /// <summary>
    /// The next value the predicate accepts in the given direction, which is where a press of a spin button
    /// or a turn of the dial lands.
    /// </summary>
    /// <remarks>
    /// The move is to the next accepted value rather than by a fixed amount, since the step is already part
    /// of what the predicate accepts: from a value on the grid that is a move of exactly one step, and from
    /// one off it - a time the application bound, or one typed by hand - it is the move back onto the grid.
    /// </remarks>
    public static int? StepToAllowed(int? current, bool forward, int range, Func<int, bool> isAllowed)
    {
        return FindAllowedFrom(Wrap(current.GetValueOrDefault() + (forward ? 1 : -1), range), forward, range, isAllowed);
    }

    /// <summary>
    /// Where a value typed into a part of a picker lands once the constraints have had their say: the
    /// nearest value they accept.
    /// </summary>
    /// <remarks>
    /// The exception is a move of exactly one, which is what the arrow keys of a number input make. Where
    /// the accepted values are sparser than that, the nearest one to where the arrow landed is the value it
    /// just left, which would leave the input stuck - so a move of one carries on in the direction it was
    /// going instead of sitting still.
    /// </remarks>
    public static int? FindAllowedNear(int candidate, int? current, int range, Func<int, bool> isAllowed)
    {
        var nearest = FindNearestAllowed(candidate, range, isAllowed);

        if (nearest.HasValue is false) return null;

        if (current.HasValue is false || candidate == current.Value || nearest.Value != current.Value) return nearest;

        var forward = Wrap(candidate - current.Value, range) == 1;
        var backward = Wrap(current.Value - candidate, range) == 1;

        if (forward is false && backward is false) return nearest;

        return FindAllowedFrom(candidate, forward, range, isAllowed) ?? nearest;
    }
}
