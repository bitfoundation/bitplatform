namespace Boilerplate.LandingPage.Client.Components.Pages.Hotel;

public static class HotelSite
{
    public const string Path = "/hotel";

    public const double TripadvisorRating = 4.8;

    public const int TripadvisorReviewCount = 612;

    public const string TripadvisorUrl = "https://www.tripadvisor.com/Search?q=Alder%20%26%20Salt%20Amrum";

    public static LandingSite Info { get; } = new()
    {
        Key = "hotel",
        Path = Path,
        Name = "Alder & Salt",
        AccentColor = "#1F5E66",
        Layout = typeof(HotelLayout),
        NotFound = typeof(HotelNotFound),
        Card = typeof(HotelCard),
        AppleAppId = "6450611349",
        WebAppUrl = "https://adminpanel.bitplatform.dev/",
        Platforms =
        [
            new(LandingPlatform.iOS, LandingPlatformStatus.Live, "https://apps.apple.com/app/bit-adminpanel/id6450611349"),
            new(LandingPlatform.Android, LandingPlatformStatus.Live, "https://play.google.com/store/apps/details?id=com.bitplatform.AdminPanel.Template"),
            new(LandingPlatform.Web, LandingPlatformStatus.Live, "https://adminpanel.bitplatform.dev/")
        ],
        Company = new()
        {
            LegalName = "Alder & Salt Hotel GmbH",
            Address = "Strandwai 14, 25946 Norddorf auf Amrum, Germany",
            Email = "stay@alderandsalt.example",
            Phone = "+49 4682 555 0177",
            RegistrationNumber = "HRB 15277 FL",
            VatId = "DE318744526",
            Representative = "Freya Nielsen"
        }
    };
}
