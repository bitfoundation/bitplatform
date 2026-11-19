namespace Bit.Bheadless;

/// <summary>What a <see cref="BitBhCalendar"/> selects.</summary>
public enum BitBhCalendarSelectionMode
{
    /// <summary>One date (<c>@bind-Value</c>).</summary>
    Single,

    /// <summary>A range of dates (<c>@bind-Range</c>): the first press sets one end, the second the other.</summary>
    Range,

    /// <summary>Any number of dates (<c>@bind-Values</c>): each press toggles a date.</summary>
    Multiple,
}
