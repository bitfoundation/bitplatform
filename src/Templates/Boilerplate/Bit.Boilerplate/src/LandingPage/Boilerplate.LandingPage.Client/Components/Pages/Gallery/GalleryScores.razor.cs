namespace Boilerplate.LandingPage.Client.Components.Pages.Gallery;

public partial class GalleryScores
{
    private const string TestedAddress = "landingpage.bitplatform.dev";
    private const string RunItYourself = "https://pagespeed.web.dev/report?url=landingpage.bitplatform.dev&utm_source=search_console&form_factor=mobile&hl=en";

    private static readonly DateOnly testedOn = new(2026, 11, 4);

    [Parameter] public string? Class { get; set; }

    private string[] Labels() =>
    [
        Localizer[nameof(LandingStrings.GalleryScorePerformance)],
        Localizer[nameof(LandingStrings.GalleryScoreAccessibility)],
        Localizer[nameof(LandingStrings.GalleryScoreBestPractices)],
        Localizer[nameof(LandingStrings.GalleryScoreSeo)]
    ];

    private string TestedText() => Localizer[nameof(LandingStrings.GalleryScoreTested), testedOn.ToString("d MMMM yyyy", CurrentCulture)];
}
