namespace Boilerplate.LandingPage.Client.Components.Pages.PeriodTracker;

public partial class PeriodTrackerNotFound
{
    private const int Missing = 17;

    private static readonly (string X, string Y, string Class)[] dots = [.. Enumerable.Range(0, 30).Where(index => index != Missing).Select(Dot)];

    private static readonly (string X, string Y, string Class) gap = Dot(Missing);

    [AutoInject] private IStringLocalizer<PeriodTrackerStrings> P = default!;

    private static (string X, string Y, string Class) Dot(int index)
    {
        var angle = index * 12 * Math.PI / 180;
        var kind = index switch
        {
            < 5 => "is-period",
            >= 26 => "is-window",
            _ => "is-past"
        };

        return ((150 + 112 * Math.Sin(angle)).ToString("0.##", CultureInfo.InvariantCulture), (150 - 112 * Math.Cos(angle)).ToString("0.##", CultureInfo.InvariantCulture), kind);
    }
}
