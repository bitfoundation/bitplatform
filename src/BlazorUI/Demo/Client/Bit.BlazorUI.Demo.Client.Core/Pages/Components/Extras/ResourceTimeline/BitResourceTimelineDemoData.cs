using System.Globalization;

namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Extras.ResourceTimeline;

/// <summary>
/// The sample data of the BitResourceTimeline demo: the gates of an airport and the turnarounds of the flights parked
/// at them, laid out around today so the current time is always on the timeline. Every day of every gate is generated
/// from a seed of its own, so the same arguments always return the same flights - and a window asked for on demand
/// returns exactly the flights a full load would have had there.
/// </summary>
public static class BitResourceTimelineDemoData
{
    private static readonly string[] Airlines = ["LH", "BA", "AF", "KL", "EK", "QR", "TK", "IB"];

    public static readonly string WarningIcon = Svg("<svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 24 24'><path d='M12 2 1 21h22L12 2z' fill='#f59f00'/><rect x='11' y='9' width='2' height='6' fill='#fff'/><rect x='11' y='17' width='2' height='2' fill='#fff'/></svg>");
    public static readonly string CheckIcon = Svg("<svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 24 24'><circle cx='12' cy='12' r='11' fill='#2f9e44'/><path d='M6 12l4 4 8-8' stroke='#fff' stroke-width='2.5' fill='none' stroke-linecap='round' stroke-linejoin='round'/></svg>");
    public static readonly string StarIcon = Svg("<svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 24 24'><path d='M12 2l3 7h7l-5.5 4.5L18 21l-6-4-6 4 1.5-7.5L2 9h7z' fill='#fcc419'/></svg>");
    public static readonly string LockIcon = Svg("<svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 24 24'><rect x='5' y='10' width='14' height='11' rx='2' fill='#ffffff'/><path d='M8 10V7a4 4 0 0 1 8 0v3' stroke='#ffffff' stroke-width='2' fill='none'/></svg>");
    public static readonly string PlaneIcon = Svg("<svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 24 24'><path d='M21 16v-2l-8-5V3.5a1.5 1.5 0 0 0-3 0V9l-8 5v2l8-2.5V19l-2 1.5V22l3.5-1 3.5 1v-1.5L13 19v-5.5z' fill='#ffffff'/></svg>");

    /// <summary>The local midnight of today, which every example lays its bars out around.</summary>
    public static DateTimeOffset Today => new(DateTime.Today);

    /// <summary>
    /// The local midnight <paramref name="days"/> days from today, with the offset in force on that day - which a
    /// daylight saving change in between makes differ from the one <see cref="Today"/> carries.
    /// </summary>
    public static DateTimeOffset Day(int days) => new(DateTime.Today.AddDays(days));

    /// <summary>Gates named Gate A1, Gate A2, ... - optionally the children of a group row.</summary>
    public static List<BitResourceTimelineResource> Gates(int count, string terminal = "A", string? parentId = null) =>
        [.. Enumerable.Range(1, count).Select(n => new BitResourceTimelineResource { Id = $"{terminal}{n}", Name = $"Gate {terminal}{n}", ParentId = parentId })];

    /// <summary>Terminal A, B, ... as group rows, each holding <paramref name="gatesPerTerminal"/> gates.</summary>
    public static List<BitResourceTimelineResource> Terminals(int terminals, int gatesPerTerminal)
    {
        var resources = new List<BitResourceTimelineResource>();

        for (var t = 0; t < terminals; t++)
        {
            var terminal = ((char)('A' + t)).ToString();

            resources.Add(new() { Id = $"T{terminal}", Name = $"Terminal {terminal}" });
            resources.AddRange(Gates(gatesPerTerminal, terminal, $"T{terminal}"));
        }

        return resources;
    }

    /// <summary>
    /// A whole board: the resources, the days from <paramref name="fromDay"/> to <paramref name="toDay"/> around today,
    /// and the flights of every gate in between.
    /// </summary>
    public static BitResourceTimelineConfig Airport(List<BitResourceTimelineResource> resources, int fromDay = -7, int toDay = 7, bool labeled = false) => new()
    {
        Resources = resources,
        StartDate = Day(fromDay),
        EndDate = Day(toDay),
        Allocations = Flights(resources, Day(fromDay), Day(toDay), labeled),
    };

    /// <summary>
    /// The turnarounds overlapping <paramref name="from"/>..<paramref name="to"/> at every resource that is not a group
    /// row: 45 minutes to 3 hours each, 30 minutes to 2 hours apart. With <paramref name="labeled"/> each carries its
    /// flight number above it.
    /// </summary>
    public static List<BitResourceTimelineAllocation> Flights(IReadOnlyList<BitResourceTimelineResource> resources, DateTimeOffset from, DateTimeOffset to, bool labeled = false)
    {
        var groups = resources.Where(r => r.ParentId is not null).Select(r => r.ParentId).ToHashSet();
        // Counted in local calendar days rather than elapsed 24 hours, which a daylight saving change in between would
        // throw off by one: the day the window starts on, through the day it ends on unless it ends at its midnight.
        var localTo = to.LocalDateTime;
        var firstDay = (int)(from.LocalDateTime.Date - DateTime.Today).TotalDays;
        var lastDay = (int)(localTo.Date - DateTime.Today).TotalDays + (localTo.TimeOfDay > TimeSpan.Zero ? 1 : 0);
        var flights = new List<BitResourceTimelineAllocation>();

        for (var r = 0; r < resources.Count; r++)
        {
            var gate = resources[r];
            if (groups.Contains(gate.Id)) continue;

            for (var day = firstDay; day < lastDay; day++)
            {
                var dayStart = Day(day);
                var dayEnd = Day(day + 1);

                // Seeded by the gate and the calendar date, so a day reads the same whichever window asks for it, and
                // whenever it is asked for (a fetch after midnight still finds the flights the earlier ones loaded).
                var random = new Random(unchecked(r * 100_003 + dayStart.Year * 10_000 + dayStart.Month * 100 + dayStart.Day));
                var start = dayStart.AddMinutes(random.Next(0, 12) * 15);

                for (var n = 0; ; n++)
                {
                    var end = start.AddMinutes(random.Next(3, 13) * 15);
                    var flightNumber = $"{Airlines[random.Next(Airlines.Length)]}{random.Next(100, 1000)}";

                    if (end > dayEnd) break;

                    if (end > from && start < to)
                    {
                        flights.Add(new()
                        {
                            Id = $"{gate.Id}-{dayStart.ToString("yyyyMMdd", CultureInfo.InvariantCulture)}-{n}",
                            ResourceId = gate.Id,
                            StartTime = start,
                            EndTime = end,
                            TextAbove = labeled ? flightNumber : null,
                        });
                    }

                    start = end.AddMinutes(random.Next(2, 9) * 15);
                }
            }
        }

        return flights;
    }

    // An inline SVG as a data URI, so the icons need no image files.
    private static string Svg(string svg) => "data:image/svg+xml," + Uri.EscapeDataString(svg);
}
