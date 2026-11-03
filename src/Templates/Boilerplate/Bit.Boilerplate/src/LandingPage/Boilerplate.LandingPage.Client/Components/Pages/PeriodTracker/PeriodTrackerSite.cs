namespace Boilerplate.LandingPage.Client.Components.Pages.PeriodTracker;

public static class PeriodTrackerSite
{
    public const string Path = "/period-tracker";

    public const string BetaUrl = "https://apps.apple.com/app/bit-adminpanel/id6450611349";

    public static LandingSite Info { get; } = new()
    {
        Key = "period-tracker",
        Path = Path,
        Name = "Liora",
        AccentColor = "#7A2E67",
        Layout = typeof(PeriodTrackerLayout),
        NotFound = typeof(PeriodTrackerNotFound),
        Card = typeof(PeriodTrackerCard),
        Platforms =
        [
            new(LandingPlatform.iOS, LandingPlatformStatus.PreOrder, BetaUrl),
            new(LandingPlatform.Android, LandingPlatformStatus.ComingSoon)
        ],
        Company = new()
        {
            LegalName = "Liora Health AB",
            Address = "Götgatan 48, 118 26 Stockholm, Sweden",
            Email = "hi@liora.example",
            Phone = "+46 8 555 012 34",
            RegistrationNumber = "Org.nr 559412-3087",
            VatId = "SE559412308701",
            Representative = "Elin Sandberg"
        }
    };
}
