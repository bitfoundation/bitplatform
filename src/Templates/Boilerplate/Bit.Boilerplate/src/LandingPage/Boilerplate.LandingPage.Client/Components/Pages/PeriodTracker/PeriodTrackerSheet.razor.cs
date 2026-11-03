namespace Boilerplate.LandingPage.Client.Components.Pages.PeriodTracker;

public partial class PeriodTrackerSheet
{
    private const int FirstYear = 2017;
    private const int Years = 7;
    private const double ColumnWidth = 52;

    private static readonly int[] cycleLengths = [28, 29, 27, 30, 28, 26, 29, 28, 31, 27, 28, 29, 30];
    private static readonly int[] periodLengths = [5, 4, 5, 6, 5];

    private static readonly (double X, double Y, double Width)[] periods = Build();

    [Parameter] public string? Label { get; set; }

    private static double ColumnX(int column) => 14 + column * (ColumnWidth + 8);

    private static double RowY(int row) => 60 + row * 20.5;

    private static (double X, double Y, double Width)[] Build()
    {
        var result = new List<(double, double, double)>();
        var start = new DateTime(FirstYear, 1, 9);
        var end = new DateTime(FirstYear + Years, 1, 1);

        for (var index = 0; start < end; index++)
        {
            var days = DateTime.DaysInMonth(start.Year, start.Month);
            var length = periodLengths[index % periodLengths.Length];
            var x = ColumnX(start.Year - FirstYear) + (start.Day - 1) * ColumnWidth / days;
            var width = Math.Min(length * ColumnWidth / days, ColumnX(start.Year - FirstYear) + ColumnWidth - x);

            result.Add((Math.Round(x, 2), RowY(start.Month - 1), Math.Round(Math.Max(width, 4), 2)));
            start = start.AddDays(cycleLengths[index % cycleLengths.Length]);
        }

        return [.. result];
    }

    private static string Invariant(double value) => value.ToString(CultureInfo.InvariantCulture);
}
