namespace Boilerplate.LandingPage.Client.Components.Pages.Hotel;

public partial class HotelStayPlanner
{
    [AutoInject] private IStringLocalizer<HotelStrings> H = default!;

    private DateTimeOffset today;
    private BitDateRangePickerValue? range;
    private string? room = HotelRooms.All[1].Key;
    private bool isChecked;
    private BitChoiceGroupItem<string>[] roomItems = [];

    private HotelRoom SelectedRoom => HotelRooms.All.FirstOrDefault(r => r.Key == room, HotelRooms.All[1]);

    private int Nights => range is { StartDate: { } start, EndDate: { } end } ? Math.Max(0, (end.Date - start.Date).Days) : 0;

    private decimal Total => range?.StartDate is { } start ? HotelRooms.Price(SelectedRoom, start.Date, Nights) : 0;

    private int RoomsLeft
    {
        get
        {
            var index = Array.IndexOf(HotelRooms.All, SelectedRoom);
            var seed = (range?.StartDate?.DayOfYear ?? 0) * 7 + Nights * 3 + index * 5;

            return 1 + seed % Math.Min(SelectedRoom.Count, 3);
        }
    }

    protected override void OnInitialized()
    {
        var now = DateTime.UtcNow.Date;
        var untilFriday = ((int)DayOfWeek.Friday - (int)now.DayOfWeek + 7) % 7;
        var arrival = now.AddDays(untilFriday is 0 ? 7 : untilFriday);

        today = new DateTimeOffset(now, TimeSpan.Zero);
        range = new() { StartDate = new DateTimeOffset(arrival, TimeSpan.Zero), EndDate = new DateTimeOffset(arrival.AddDays(3), TimeSpan.Zero) };

        base.OnInitialized();
    }

    protected override void OnParametersSet()
    {
        roomItems = [.. HotelRooms.All.Select(r => new BitChoiceGroupItem<string> { Value = r.Key, Text = H[r.NameKey] })];

        base.OnParametersSet();
    }

    private void SelectDates(BitDateRangePickerValue? value)
    {
        range = value;
        isChecked = false;
    }

    private void SelectRoom(string? value)
    {
        room = value;
        isChecked = false;
    }

    private string Money(decimal amount) => H[nameof(HotelStrings.Money), amount.ToString("N0", CurrentCulture)];
}
