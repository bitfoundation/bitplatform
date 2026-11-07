namespace Boilerplate.Workbench.Components.Pages.LandingPages.Minimal;

/// <summary>The working demo in the hero's phone: one line for today, the week it fills and the same day in earlier years.</summary>
public partial class MinimalJournalDemo
{
    private const int MaxLength = 140;

    [AutoInject] private MinimalLandingStore store = default!;

    private readonly DateTime today = DateTime.Today;

    private IEnumerable<(DateTime Date, bool Written)> Week
    {
        get
        {
            var offset = ((int)today.DayOfWeek - (int)CultureInfo.CurrentUICulture.DateTimeFormat.FirstDayOfWeek + 7) % 7;
            var start = today.AddDays(-offset);

            return Enumerable.Range(0, 7).Select(i => start.AddDays(i)).Select(date => (date, date < today ? (date - start).Days != 2 : date == today && store.SavedLine is not null));
        }
    }

    protected override void OnInitialized()
    {
        Subscribe(store);

        base.OnInitialized();
    }

    private void Save()
    {
        if (string.IsNullOrWhiteSpace(store.Line))
            return;

        store.SaveLine(store.Line.Trim());
    }
}
