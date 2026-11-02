namespace Boilerplate.Workbench.Infrastructure.Services;

/// <summary>Places the screens of a flow on its board.</summary>
public static class WorkbenchFlowLayout
{
    private const int ColumnGap = 260;

    private const int RowGap = 200;

    private const int CaptionHeight = 160;

    /// <summary>The space a screen takes on the board: its framed device and the caption under it.</summary>
    public static (int Width, int Height) SizeOf(WorkbenchScenario scenario)
    {
        var (width, height) = DeviceFrame.GetFrameSize(scenario.DevicePreset, scenario.Rotated, showFrame: true);

        return (width, height + CaptionHeight);
    }

    /// <summary>Where a screen added to a flow goes: right of the rightmost screen, at its height.</summary>
    public static (double X, double Y) Next(WorkbenchFlow flow)
    {
        if (flow.Screens.Length == 0)
            return (0, 0);

        var last = flow.Screens.MaxBy(screen => screen.X + SizeOf(screen.Scenario).Width)!;

        return (last.X + SizeOf(last.Scenario).Width + ColumnGap, last.Y);
    }

    /// <summary>
    /// Lays the screens out in columns by how many arrows lead to them from a screen no arrow points at, top to bottom in a column.
    /// </summary>
    public static Dictionary<string, (double X, double Y)> Arrange(IReadOnlyList<(string Id, WorkbenchScenario Scenario)> screens, IReadOnlyList<(string From, string To)> links)
    {
        var depth = new Dictionary<string, int>();
        var targets = links.Select(link => link.To).ToHashSet();

        // Breadth-first from each screen no arrow points at; a screen still unreached, as on a loop, starts a walk of its own.
        foreach (var (start, _) in screens.Where(screen => targets.Contains(screen.Id) is false).Concat(screens))
        {
            if (depth.ContainsKey(start))
                continue;

            depth[start] = 0;
            var queue = new Queue<string>([start]);

            while (queue.TryDequeue(out var current))
            {
                foreach (var (_, to) in links.Where(link => link.From == current))
                {
                    if (depth.ContainsKey(to) || screens.Any(screen => screen.Id == to) is false)
                        continue;

                    depth[to] = depth[current] + 1;
                    queue.Enqueue(to);
                }
            }
        }

        var positions = new Dictionary<string, (double X, double Y)>();
        double x = 0;

        foreach (var column in screens.GroupBy(screen => depth[screen.Id]).OrderBy(column => column.Key))
        {
            double y = 0;

            foreach (var (id, scenario) in column)
            {
                positions[id] = (x, y);
                y += SizeOf(scenario).Height + RowGap;
            }

            x += column.Max(screen => SizeOf(screen.Scenario).Width) + ColumnGap;
        }

        return positions;
    }
}
