namespace Boilerplate.LandingPage.Client.Components.Pages.Restaurant;

public sealed record RestaurantCafe(string Key, string Address, int Queue, TimeOnly WeekdayOpen, TimeOnly WeekdayClose, TimeOnly WeekendOpen, TimeOnly WeekendClose)
{
    public string NameKey => $"Cafe{Key}";

    public string NoteKey => $"Cafe{Key}Note";
}

public sealed record RestaurantDrink(string Key, decimal Price, int Minutes, bool HasSizes, string DefaultMilk)
{
    public string NameKey => $"Drink{Key}";
}

public static class RestaurantCafes
{
    public const decimal LargeSize = 6;

    public const decimal OatMilk = 5;

    public const decimal ExtraShot = 8;

    public static RestaurantCafe[] All { get; } =
    [
        new("Norrebro", "Elmegade 14, 2200 København N", 3, new(7, 0), new(18, 0), new(8, 0), new(17, 0)),
        new("Vesterbro", "Værnedamsvej 9, 1819 Frederiksberg C", 2, new(7, 30), new(18, 0), new(8, 30), new(17, 0)),
        new("Christianshavn", "Overgaden neden Vandet 27, 1414 København K", 1, new(7, 0), new(17, 0), new(9, 0), new(16, 0))
    ];

    public static RestaurantDrink[] Drinks { get; } =
    [
        new("FlatWhite", 42, 4, false, "Whole"),
        new("Cortado", 38, 3, false, "Whole"),
        new("Latte", 45, 4, true, "Whole"),
        new("Filter", 35, 2, true, "None"),
        new("Chai", 46, 4, true, "Oat")
    ];

    public static DateTime CopenhagenNow()
    {
        var utc = DateTime.UtcNow;
        var summerStart = LastSunday(utc.Year, 3).AddHours(1);
        var summerEnd = LastSunday(utc.Year, 10).AddHours(1);

        return DateTime.SpecifyKind(utc.AddHours(utc >= summerStart && utc < summerEnd ? 2 : 1), DateTimeKind.Unspecified);
    }

    private static DateTime LastSunday(int year, int month)
    {
        var last = new DateTime(year, month, DateTime.DaysInMonth(year, month), 0, 0, 0, DateTimeKind.Utc);

        return last.AddDays(-(int)last.DayOfWeek);
    }
}
