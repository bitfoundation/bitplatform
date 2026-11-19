namespace Bit.Bheadless;

/// <summary>
/// The button that shows the previous month(s) of a <see cref="BitBhCalendar"/>, named "Previous month"
/// (<see cref="BitBhStrings.PreviousMonth"/>; an <c>aria-label</c> of your own wins).
/// </summary>
public class BitBhCalendarPrevious : BitBhCalendarNavButton
{
    private protected override bool Forward => false;
}
