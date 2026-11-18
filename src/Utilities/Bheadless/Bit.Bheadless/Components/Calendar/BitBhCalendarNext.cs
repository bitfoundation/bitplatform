namespace Bit.Bheadless;

/// <summary>
/// The button that shows the next month(s) of a <see cref="BitBhCalendar"/>, named "Next month"
/// (<see cref="BitBhStrings.NextMonth"/>; an <c>aria-label</c> of your own wins).
/// </summary>
public class BitBhCalendarNext : BitBhCalendarNavButton
{
    private protected override bool Forward => true;
}
