namespace Boilerplate.LandingPage.Client.Components.Pages.PeriodTracker;

public partial class PeriodTrackerAboutPage
{
    private static readonly (string Title, string Body)[] promises =
    [
        (nameof(PeriodTrackerStrings.Promise1Title), nameof(PeriodTrackerStrings.Promise1Body)),
        (nameof(PeriodTrackerStrings.Promise2Title), nameof(PeriodTrackerStrings.Promise2Body)),
        (nameof(PeriodTrackerStrings.Promise3Title), nameof(PeriodTrackerStrings.Promise3Body)),
        (nameof(PeriodTrackerStrings.Promise4Title), nameof(PeriodTrackerStrings.Promise4Body)),
        (nameof(PeriodTrackerStrings.Promise5Title), nameof(PeriodTrackerStrings.Promise5Body))
    ];

    private static readonly (string Name, string Role, string Note)[] team =
    [
        (nameof(PeriodTrackerStrings.Team1Name), nameof(PeriodTrackerStrings.Team1Role), nameof(PeriodTrackerStrings.Team1Note)),
        (nameof(PeriodTrackerStrings.Team2Name), nameof(PeriodTrackerStrings.Team2Role), nameof(PeriodTrackerStrings.Team2Note)),
        (nameof(PeriodTrackerStrings.Team3Name), nameof(PeriodTrackerStrings.Team3Role), nameof(PeriodTrackerStrings.Team3Note)),
        (nameof(PeriodTrackerStrings.Team4Name), nameof(PeriodTrackerStrings.Team4Role), nameof(PeriodTrackerStrings.Team4Note))
    ];

    [AutoInject] private IStringLocalizer<LandingStrings> L = default!;
    [AutoInject] private IStringLocalizer<PeriodTrackerStrings> P = default!;

    private static (string X, string Y) DotPosition(int index)
    {
        var angle = (index * 97 + 20) * Math.PI / 180;

        return ((24 + 18 * Math.Sin(angle)).ToString("0.##", CultureInfo.InvariantCulture), (24 - 18 * Math.Cos(angle)).ToString("0.##", CultureInfo.InvariantCulture));
    }
}
