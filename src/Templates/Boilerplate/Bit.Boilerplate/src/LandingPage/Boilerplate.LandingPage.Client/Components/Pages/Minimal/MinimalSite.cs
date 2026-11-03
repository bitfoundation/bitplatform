namespace Boilerplate.LandingPage.Client.Components.Pages.Minimal;

public static class MinimalSite
{
    public const string Path = "/minimal";

    public static LandingSite Info { get; } = new()
    {
        Key = "minimal",
        Path = Path,
        Name = "Notch",
        AccentColor = "#141414",
        Layout = typeof(MinimalLayout),
        NotFound = typeof(MinimalNotFound),
        Card = typeof(MinimalCard),
        Platforms =
        [
            new(LandingPlatform.iOS, LandingPlatformStatus.ComingSoon),
            new(LandingPlatform.Android, LandingPlatformStatus.ComingSoon)
        ],
        Company = new()
        {
            LegalName = "Notch Journal Oy",
            Address = "Tallberginkatu 1 C, 00180 Helsinki, Finland",
            Email = "hello@notch.example",
            RegistrationNumber = "Business ID 3312845-6",
            VatId = "FI33128456",
            Representative = "Aino Laaksonen"
        }
    };
}
