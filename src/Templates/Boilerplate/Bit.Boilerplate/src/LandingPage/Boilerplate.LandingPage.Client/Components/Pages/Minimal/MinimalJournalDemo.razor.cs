namespace Boilerplate.LandingPage.Client.Components.Pages.Minimal;

public partial class MinimalJournalDemo
{
    private const int MaxLength = 140;

    [AutoInject] private IStringLocalizer<MinimalStrings> M = default!;

    private readonly DateTime today = DateTime.Today;
    private string? line;
    private string? saved;
    private (DateTime Date, bool Written)[] week = [];

    private int WeekCount => week.Count(day => day.Written);

    protected override void OnInitialized()
    {
        BuildWeek();

        base.OnInitialized();
    }

    private void Save()
    {
        if (string.IsNullOrWhiteSpace(line))
            return;

        saved = line.Trim();
        BuildWeek();
    }

    private void BuildWeek()
    {
        var offset = ((int)today.DayOfWeek - (int)CurrentCulture.DateTimeFormat.FirstDayOfWeek + 7) % 7;
        var start = today.AddDays(-offset);

        week = [.. Enumerable.Range(0, 7).Select(i => start.AddDays(i)).Select(date => (date, date < today ? (date - start).Days != 2 : date == today && saved is not null))];
    }
}
