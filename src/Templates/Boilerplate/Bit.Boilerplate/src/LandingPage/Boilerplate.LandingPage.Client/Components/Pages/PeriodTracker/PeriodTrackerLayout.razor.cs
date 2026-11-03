namespace Boilerplate.LandingPage.Client.Components.Pages.PeriodTracker;

public partial class PeriodTrackerLayout
{
    [AutoInject] private IStringLocalizer<LandingStrings> L = default!;
    [AutoInject] private IStringLocalizer<PeriodTrackerStrings> P = default!;

    private LandingLink[] links = [];

    protected override void OnParametersSet()
    {
        links =
        [
            new(P[nameof(PeriodTrackerStrings.NavPromise)], Link("#promise")),
            new(P[nameof(PeriodTrackerStrings.NavHow)], Link("#how")),
            new(P[nameof(PeriodTrackerStrings.NavBeta)], Link("#download")),
            new(P[nameof(PeriodTrackerStrings.NavFaq)], Link("#faq")),
            new(L[nameof(LandingStrings.About)], LandingCultures.Localize($"{PeriodTrackerSite.Path}/about", CultureInfo.CurrentUICulture))
        ];

        base.OnParametersSet();
    }

    private static string Link(string hash) => $"{LandingCultures.Localize(PeriodTrackerSite.Path, CultureInfo.CurrentUICulture)}{hash}";
}
