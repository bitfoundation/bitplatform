namespace Boilerplate.LandingPage.Client.Components.Pages.PeriodTracker;

public partial class PeriodTrackerCard
{
    private static readonly (string X, string Y, string Class)[] dots = [.. Enumerable.Range(0, 30).Select(Dot)];

    [AutoInject] private IStringLocalizer<LandingStrings> L = default!;
    [AutoInject] private IStringLocalizer<PeriodTrackerStrings> P = default!;

    private static (string X, string Y, string Class) Dot(int index)
    {
        var angle = index * 12 * Math.PI / 180;
        var kind = index switch
        {
            < 5 => "is-period",
            25 => "is-today",
            >= 27 and <= 29 => "is-window",
            > 25 => "is-future",
            _ => "is-past"
        };

        return ((100 + 84 * Math.Sin(angle)).ToString("0.##", CultureInfo.InvariantCulture), (100 - 84 * Math.Cos(angle)).ToString("0.##", CultureInfo.InvariantCulture), kind);
    }
}
