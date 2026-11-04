namespace Boilerplate.LandingPage.Client.Components.Pages.Fitness;

public static class FitnessSite
{
    public const string Path = "/fitness";

    public const string AppStoreUrl = "https://apps.apple.com/app/bit-adminpanel/id6450611349";

    public const string WebAppUrl = "https://adminpanel.bitplatform.dev/";

    public const string PressEmail = "press@pulsepath.example";

    public static LandingSite Info { get; } = new()
    {
        Key = "fitness",
        Path = Path,
        Name = "Pulsepath",
        AccentColor = "#C81E5B",
        Layout = typeof(FitnessLayout),
        NotFound = typeof(FitnessNotFound),
        Logo = typeof(FitnessLogo),
        Strings = typeof(FitnessStrings),
        AppleAppId = "6450611349",
        WebAppUrl = WebAppUrl,
        StatusUrl = "https://status.example.com/",
        Platforms =
        [
            new(LandingPlatform.iOS, LandingPlatformStatus.Live, AppStoreUrl),
            new(LandingPlatform.Web, LandingPlatformStatus.Live, WebAppUrl),
            new(LandingPlatform.Android, LandingPlatformStatus.ComingSoon)
        ],
        Company = new()
        {
            LegalName = "Pulsepath ApS",
            Address = "Vesterbrogade 41, 1620 Copenhagen V, Denmark",
            Email = "hello@pulsepath.example",
            Phone = "+45 32 55 01 87",
            RegistrationNumber = "CVR 41234567",
            VatId = "DK41234567",
            Representative = "Freja Holm"
        }
    };
}
