namespace Boilerplate.LandingPage.Client.Components.Pages.Retail;

public sealed record RetailShop(string Key, string Address, string Phone, double Latitude, double Longitude, int Opened, RetailDayHours[] Week, string[] Services)
{
    public string NameKey => $"Shop{Key}";

    public string MapUrl
    {
        get
        {
            var box = FormattableString.Invariant($"{Longitude - .011:0.0000},{Latitude - .005:0.0000},{Longitude + .011:0.0000},{Latitude + .005:0.0000}");

            return FormattableString.Invariant($"https://www.openstreetmap.org/export/embed.html?bbox={box}&layer=mapnik&marker={Latitude:0.0000},{Longitude:0.0000}");
        }
    }

    public string DirectionsUrl => FormattableString.Invariant($"https://www.openstreetmap.org/directions?to={Latitude:0.0000},{Longitude:0.0000}");
}

public readonly record struct RetailDayHours(TimeOnly Open, TimeOnly Close);

public enum RetailShopState
{
    Open,
    ClosingSoon,
    OpensToday,
    OpensTomorrow
}

public readonly record struct RetailShopStatus(RetailShopState State, TimeOnly Time);

public static class RetailShops
{
    private static RetailDayHours[] Week(RetailDayHours weekday, RetailDayHours saturday, RetailDayHours sunday) => [sunday, weekday, weekday, weekday, weekday, weekday, saturday];

    private static RetailDayHours Hours(int open, int openMinute, int close, int closeMinute) => new(new TimeOnly(open, openMinute), new TimeOnly(close, closeMinute));

    public static RetailShop[] All { get; } =
    [
        new("Phibsborough", "41 Phibsborough Road, Dublin 7", "+353 1 555 0111", 53.3593, -6.2726, 1998,
            Week(Hours(7, 0, 22, 0), Hours(7, 0, 22, 0), Hours(8, 0, 21, 0)), [nameof(RetailStrings.ServiceBakery), nameof(RetailStrings.ServiceDeli)]),
        new("Stoneybatter", "18 Manor Street, Stoneybatter, Dublin 7", "+353 1 555 0122", 53.3524, -6.2862, 2004,
            Week(Hours(7, 30, 21, 30), Hours(7, 30, 21, 30), Hours(9, 0, 21, 0)), [nameof(RetailStrings.ServiceFish), nameof(RetailStrings.ServiceDeli)]),
        new("Drumcondra", "112 Drumcondra Road Lower, Dublin 9", "+353 1 555 0133", 53.3672, -6.2561, 2009,
            Week(Hours(7, 0, 23, 0), Hours(7, 0, 23, 0), Hours(7, 0, 23, 0)), [nameof(RetailStrings.ServiceParking), nameof(RetailStrings.ServiceFish)]),
        new("Glasnevin", "6 Botanic Road, Glasnevin, Dublin 9", "+353 1 555 0144", 53.3687, -6.2702, 2013,
            Week(Hours(8, 0, 21, 30), Hours(8, 0, 21, 30), Hours(9, 0, 21, 0)), [nameof(RetailStrings.ServiceFlowers), nameof(RetailStrings.ServiceDeli)]),
        new("Fairview", "57 Fairview Strand, Dublin 3", "+353 1 555 0155", 53.3628, -6.2398, 2018,
            Week(Hours(7, 0, 22, 0), Hours(7, 0, 22, 0), Hours(8, 0, 21, 0)), [nameof(RetailStrings.ServiceParking), nameof(RetailStrings.ServiceFish)]),
        new("Clontarf", "220 Clontarf Road, Dublin 3", "+353 1 555 0166", 53.3622, -6.2080, 2023,
            Week(Hours(7, 30, 21, 30), Hours(8, 0, 21, 30), Hours(9, 0, 21, 0)), [nameof(RetailStrings.ServiceParking), nameof(RetailStrings.ServiceFlowers)])
    ];

    public static DateTime DublinNow() => ToDublin(DateTimeOffset.UtcNow);

    public static DateTime ToDublin(DateTimeOffset moment)
    {
        var utc = moment.UtcDateTime;
        var summerStart = LastSunday(utc.Year, 3).AddHours(1);
        var summerEnd = LastSunday(utc.Year, 10).AddHours(1);

        return DateTime.SpecifyKind(utc >= summerStart && utc < summerEnd ? utc.AddHours(1) : utc, DateTimeKind.Unspecified);
    }

    public static RetailShopStatus StatusAt(RetailShop shop, DateTime local)
    {
        var today = shop.Week[(int)local.DayOfWeek];
        var now = TimeOnly.FromDateTime(local);

        if (now < today.Open)
            return new(RetailShopState.OpensToday, today.Open);

        if (now < today.Close)
            return new(today.Close - now <= TimeSpan.FromMinutes(30) ? RetailShopState.ClosingSoon : RetailShopState.Open, today.Close);

        return new(RetailShopState.OpensTomorrow, shop.Week[((int)local.DayOfWeek + 1) % 7].Open);
    }

    private static DateTime LastSunday(int year, int month)
    {
        var last = new DateTime(year, month, DateTime.DaysInMonth(year, month), 0, 0, 0, DateTimeKind.Utc);

        return last.AddDays(-(int)last.DayOfWeek);
    }
}
