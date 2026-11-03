namespace Boilerplate.LandingPage.Client.Components.Pages.Restaurant;

public partial class RestaurantOrder
{
    private const int StampsBefore = 3;

    [AutoInject] private IStringLocalizer<RestaurantStrings> C = default!;

    private DateTime now;
    private string? drink = RestaurantCafes.Drinks[0].Key;
    private string? size = "Small";
    private string? milk = RestaurantCafes.Drinks[0].DefaultMilk;
    private string? cafe = RestaurantCafes.All[0].Key;
    private bool extraShot;
    private bool placed;
    private BitChoiceGroupItem<string>[] drinkItems = [];
    private BitChoiceGroupItem<string>[] sizeItems = [];
    private BitChoiceGroupItem<string>[] milkItems = [];
    private BitChoiceGroupItem<string>[] cafeItems = [];

    private RestaurantDrink SelectedDrink => RestaurantCafes.Drinks.FirstOrDefault(d => d.Key == drink, RestaurantCafes.Drinks[0]);

    private RestaurantCafe SelectedCafe => RestaurantCafes.All.FirstOrDefault(c => c.Key == cafe, RestaurantCafes.All[0]);

    private bool IsFilter => SelectedDrink.Key is "Filter";

    private bool HasShot => extraShot && IsFilter is false;

    private decimal Total => SelectedDrink.Price
        + (size is "Regular" && SelectedDrink.HasSizes ? RestaurantCafes.LargeSize : 0)
        + (milk is "Oat" ? RestaurantCafes.OatMilk : 0)
        + (HasShot ? RestaurantCafes.ExtraShot : 0);

    private int Minutes => SelectedDrink.Minutes + SelectedCafe.Queue + (HasShot ? 1 : 0);

    private DateTime ReadyAt => Ready.Time;

    private (DateTime Time, bool AtOpening, bool Tomorrow) Ready
    {
        get
        {
            var (open, close) = Hours(now.Date);

            if (now < open)
                return (open.AddMinutes(Minutes), true, false);

            if (now >= close.AddMinutes(-15))
                return (Hours(now.Date.AddDays(1)).Open.AddMinutes(Minutes), true, true);

            return (now.AddMinutes(Minutes + 1).AddSeconds(-now.Second), false, false);
        }
    }

    private string ReadyText => Ready switch
    {
        { Tomorrow: true } => C[nameof(RestaurantStrings.DemoReadyTomorrow), Time(ReadyAt)],
        _ => C[nameof(RestaurantStrings.DemoReadyAt), Time(ReadyAt)]
    };

    private string ReadyNote => Ready switch
    {
        { Tomorrow: true } => C[nameof(RestaurantStrings.DemoClosedNow)],
        { AtOpening: true } => C[nameof(RestaurantStrings.DemoAtOpening)],
        _ => C[nameof(RestaurantStrings.DemoReadyIn), Minutes.ToString(CurrentCulture)]
    };

    private int Stamps => StampsBefore + (placed ? 1 : 0);

    private int OrderNumber => 20 + (now.Minute + Array.IndexOf(RestaurantCafes.All, SelectedCafe) * 7) % 70;

    private string Summary
    {
        get
        {
            var parts = new List<string> { C[SelectedDrink.NameKey] };

            if (SelectedDrink.HasSizes) parts.Add(C[$"Size{size}"]);
            if (milk is not "None") parts.Add(C[$"Milk{milk}"]);
            if (HasShot) parts.Add(C[nameof(RestaurantStrings.DemoShotShort)]);

            return string.Join(C[nameof(RestaurantStrings.ListSeparator)], parts);
        }
    }

    protected override void OnInitialized()
    {
        now = RestaurantCafes.CopenhagenNow();

        base.OnInitialized();
    }

    protected override void OnParametersSet()
    {
        drinkItems = [.. RestaurantCafes.Drinks.Select(d => new BitChoiceGroupItem<string> { Value = d.Key, Text = C[d.NameKey] })];
        milkItems = [.. new[] { "Whole", "Oat", "None" }.Select(m => new BitChoiceGroupItem<string> { Value = m, Text = C[$"Milk{m}"] })];
        cafeItems = [.. RestaurantCafes.All.Select(c => new BitChoiceGroupItem<string> { Value = c.Key, Text = C[c.NameKey] })];
        RefreshSizes();

        base.OnParametersSet();
    }

    private void SelectDrink(string? value)
    {
        drink = value;
        milk = SelectedDrink.DefaultMilk;
        now = RestaurantCafes.CopenhagenNow();
        RefreshSizes();
    }

    private void SelectCafe(string? value)
    {
        cafe = value;
        now = RestaurantCafes.CopenhagenNow();
    }

    private void RefreshSizes()
    {
        if (SelectedDrink.HasSizes is false)
        {
            size = "Small";
        }

        sizeItems =
        [
            new() { Value = "Small", Text = C[nameof(RestaurantStrings.SizeSmall)] },
            new() { Value = "Regular", Text = C[nameof(RestaurantStrings.SizeRegular)], IsDisabled = SelectedDrink.HasSizes is false }
        ];
    }

    private void Place()
    {
        now = RestaurantCafes.CopenhagenNow();
        placed = true;
    }

    private (DateTime Open, DateTime Close) Hours(DateTime day)
    {
        var weekend = day.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday;
        var cafe = SelectedCafe;

        return (day + (weekend ? cafe.WeekendOpen : cafe.WeekdayOpen).ToTimeSpan(), day + (weekend ? cafe.WeekendClose : cafe.WeekdayClose).ToTimeSpan());
    }

    private static string Time(DateTime time) => time.ToString("HH:mm", CurrentCulture);

    private string Money(decimal amount) => C[nameof(RestaurantStrings.Money), amount.ToString("0", CurrentCulture)];
}
