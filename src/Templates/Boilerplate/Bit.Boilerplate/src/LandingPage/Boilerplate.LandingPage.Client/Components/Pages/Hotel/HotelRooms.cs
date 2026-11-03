namespace Boilerplate.LandingPage.Client.Components.Pages.Hotel;

public sealed record HotelRoom(string Key, HotelSceneKind Scene, int Count, int Size, int Sleeps, decimal Rate)
{
    public string NameKey => $"Room{Key}Name";

    public string BodyKey => $"Room{Key}Body";

    public string BedKey => $"Room{Key}Bed";

    public string ViewKey => $"Room{Key}View";
}

public static class HotelRooms
{
    public const decimal MidSeason = 30;

    public const decimal HighSeason = 60;

    public static HotelRoom[] All { get; } =
    [
        new("Dune", HotelSceneKind.DuneRoom, 12, 19, 2, 165),
        new("Sea", HotelSceneKind.SeaRoom, 12, 24, 2, 215),
        new("Loft", HotelSceneKind.Loft, 6, 34, 4, 260),
        new("Suite", HotelSceneKind.Suite, 2, 48, 2, 380)
    ];

    public static decimal Season(DateTime night) => night.Month switch
    {
        >= 6 and <= 8 => HighSeason,
        4 or 5 or 9 or 10 => MidSeason,
        _ => 0
    };

    public static decimal Price(HotelRoom room, DateTime arrival, int nights) => Enumerable.Range(0, nights).Sum(night => room.Rate + Season(arrival.AddDays(night)));
}
