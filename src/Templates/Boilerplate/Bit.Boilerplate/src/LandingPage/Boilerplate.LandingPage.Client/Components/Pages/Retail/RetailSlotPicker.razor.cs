namespace Boilerplate.LandingPage.Client.Components.Pages.Retail;

public partial class RetailSlotPicker
{
    private const int FirstSlot = 10;
    private const int LastSlot = 20;

    [AutoInject] private IStringLocalizer<RetailStrings> R = default!;

    private DateTime now;
    private string? shop = RetailShops.All[0].Key;
    private int day;
    private int? slot;
    private bool booked;
    private BitDropdownItem<string>[] shopItems = [];
    private BitChoiceGroupItem<int>[] dayItems = [];
    private BitChoiceGroupItem<int?>[] slotItems = [];

    private RetailShop SelectedShop => RetailShops.All.FirstOrDefault(s => s.Key == shop, RetailShops.All[0]);

    private int ShopIndex => Array.IndexOf(RetailShops.All, SelectedShop);

    private string Summary => slot is { } hour
        ? R[nameof(RetailStrings.DemoSlotSummary), DayName(day), Time(hour), Time(hour + 1)]
        : R[nameof(RetailStrings.DemoPickSlot)];

    private int Code => 1000 + (ShopIndex * 1847 + day * 431 + (slot ?? 0) * 97) % 9000;

    protected override void OnInitialized()
    {
        now = RetailShops.DublinNow();

        base.OnInitialized();
    }

    protected override void OnParametersSet()
    {
        shopItems = [.. RetailShops.All.Select(s => new BitDropdownItem<string> { Value = s.Key, Text = R[s.NameKey] })];

        if (HasFreeSlot(0) is false && day is 0)
        {
            day = 1;
        }

        Refresh();

        base.OnParametersSet();
    }

    private void SelectShop(string? value)
    {
        shop = value;
        Refresh();
    }

    private void SelectDay(int value)
    {
        day = value;
        Refresh();
    }

    private void Refresh()
    {
        dayItems = [.. Enumerable.Range(0, 3).Select(i => new BitChoiceGroupItem<int> { Value = i, Text = DayName(i), IsDisabled = HasFreeSlot(i) is false })];

        slotItems = [.. Enumerable.Range(FirstSlot, LastSlot - FirstSlot + 1).Select(hour => new BitChoiceGroupItem<int?>
        {
            Value = hour,
            Text = Time(hour),
            IsDisabled = IsOpen(day, hour) is false,
            AriaLabel = IsFull(hour) ? $"{Time(hour)}, {R[nameof(RetailStrings.DemoFull)]}" : null
        })];

        if (slot is not { } current || IsOpen(day, current) is false)
        {
            slot = slotItems.FirstOrDefault(i => i.IsDisabled is false)?.Value;
        }
    }

    private bool HasFreeSlot(int dayIndex) => Enumerable.Range(FirstSlot, LastSlot - FirstSlot + 1).Any(hour => IsOpen(dayIndex, hour));

    private bool IsOpen(int dayIndex, int hour) => IsFull(dayIndex, hour) is false && now.Date.AddDays(dayIndex).AddHours(hour) >= now.AddHours(2);

    private bool IsFull(int? hour) => hour is { } value && IsFull(day, value);

    // A fixed pattern rather than Random, so the prerendered page and the running app agree on the full slots
    private bool IsFull(int dayIndex, int hour) => (ShopIndex * 7 + dayIndex * 3 + hour * 5) % 9 is 0;

    private string DayName(int dayIndex) => dayIndex switch
    {
        0 => R[nameof(RetailStrings.DemoToday)],
        1 => R[nameof(RetailStrings.DemoTomorrow)],
        _ => CurrentCulture.DateTimeFormat.GetDayName(now.Date.AddDays(dayIndex).DayOfWeek)
    };

    private static string Time(int hour) => new TimeOnly(hour % 24, 0).ToString("HH:mm", CurrentCulture);

    private string Money(decimal amount) => R[nameof(RetailStrings.Money), amount.ToString("0.00", CurrentCulture)];

    private void Book() => booked = slot is not null;
}
