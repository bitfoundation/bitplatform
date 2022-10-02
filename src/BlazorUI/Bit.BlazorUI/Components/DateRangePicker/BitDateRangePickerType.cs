namespace Bit.BlazorUI;
public class BitDateRangePickerType
{
    public DateTimeOffset? StartDate { get; set; } = DateTimeOffset.Now.AddDays(-3);
    public DateTimeOffset? EndDate { get; set; } = DateTimeOffset.Now.AddDays(5);
}
