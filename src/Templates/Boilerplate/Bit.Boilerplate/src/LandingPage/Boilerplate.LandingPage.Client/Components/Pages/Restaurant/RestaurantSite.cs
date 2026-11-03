namespace Boilerplate.LandingPage.Client.Components.Pages.Restaurant;

public static class RestaurantSite
{
    public const string Path = "/restaurant";

    public const string PreOrderUrl = "https://apps.apple.com/app/bit-adminpanel/id6450611349";

    public static DateTimeOffset LaunchDate { get; } = new(2027, 1, 15, 8, 0, 0, TimeSpan.FromHours(1));

    public static LandingSite Info { get; } = new()
    {
        Key = "restaurant",
        Path = Path,
        Name = "Little Copper",
        AccentColor = "#1F44B8",
        Layout = typeof(RestaurantLayout),
        NotFound = typeof(RestaurantNotFound),
        Card = typeof(RestaurantCard),
        Platforms =
        [
            new(LandingPlatform.iOS, LandingPlatformStatus.PreOrder, PreOrderUrl),
            new(LandingPlatform.Android, LandingPlatformStatus.ComingSoon),
            new(LandingPlatform.Web, LandingPlatformStatus.ComingSoon)
        ],
        Company = new()
        {
            LegalName = "Little Copper ApS",
            Address = "Elmegade 14, 2200 København N, Denmark",
            Email = "hej@littlecopper.example",
            Phone = "+45 31 55 01 23",
            RegistrationNumber = "CVR 38291745",
            VatId = "DK38291745",
            Representative = "Ida Mørch"
        }
    };
}
