using Boilerplate.LandingPage.Client.Components.Tour;

namespace Boilerplate.LandingPage.Client.Components.Pages.Gallery;

public partial class GalleryPage
{
    private const string All = "all";
    private const string Launched = "launched";
    private const string PreLaunch = "pre-launch";

    [AutoInject] private LandingTourLauncher tourLauncher = default!;
    [AutoInject] private IStringLocalizer<TourStrings> T = default!;
    [AutoInject] private IStringLocalizerFactory localizerFactory = default!;

    private string? filter = All;
    private BitChoiceGroupItem<string>[] filters = [];
    private string[] chatSuggestions = [];

    protected override void OnParametersSet()
    {
        filters =
        [
            new() { Value = All, Text = Localizer[nameof(LandingStrings.GalleryFilterAll)] },
            new() { Value = Launched, Text = Localizer[nameof(LandingStrings.GalleryFilterLaunched)] },
            new() { Value = PreLaunch, Text = Localizer[nameof(LandingStrings.GalleryFilterPreLaunch)] }
        ];

        chatSuggestions =
        [
            Localizer[nameof(LandingStrings.ChatGallerySuggestion1)],
            Localizer[nameof(LandingStrings.ChatGallerySuggestion2)],
            Localizer[nameof(LandingStrings.ChatGallerySuggestion3)]
        ];

        base.OnParametersSet();
    }

    private IEnumerable<LandingSite> Shown() => filter switch
    {
        Launched => LandingSites.All.Where(s => s.IsPreLaunch is false),
        PreLaunch => LandingSites.All.Where(s => s.IsPreLaunch),
        _ => LandingSites.All
    };

    private GallerySample[] Samples() => [.. Shown().Select(site => new GallerySample(site, localizerFactory.Create(site.Strings), Localizer[site.Mode switch
    {
        LandingLaunchMode.Launched => nameof(LandingStrings.GalleryModeLaunched),
        LandingLaunchMode.Staged => nameof(LandingStrings.GalleryModeStaged),
        _ => nameof(LandingStrings.GalleryModePreLaunch)
    }], LocalUrl(site.Path)))];
}
