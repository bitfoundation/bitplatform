namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Extras.ResourceTimeline;

public partial class BitResourceTimelineDemo
{
    private readonly string example1RazorCode = """
<style>
    .timeline {
        height: 22rem;
    }
</style>

<div class="timeline">
    <BitResourceTimeline Config="basicConfig" />
</div>
""";
    private readonly string example1CsharpCode = """
private readonly BitResourceTimelineConfig basicConfig = CreateBasicConfig();

private static BitResourceTimelineConfig CreateBasicConfig()
{
    var today = new DateTimeOffset(DateTime.Today);

    return new()
    {
        StartDate = today,
        EndDate = today.AddDays(3),
        Resources =
        [
            new() { Id = "A1", Name = "Gate A1" },
            new() { Id = "A2", Name = "Gate A2" },
            new() { Id = "A3", Name = "Gate A3" },
            new() { Id = "A4", Name = "Gate A4" },
        ],
        Allocations =
        [
            new() { Id = "f1", ResourceId = "A1", StartTime = today.AddHours(1), EndTime = today.AddHours(3.5) },
            new() { Id = "f2", ResourceId = "A1", StartTime = today.AddHours(6), EndTime = today.AddHours(8) },
            new() { Id = "f3", ResourceId = "A1", StartTime = today.AddHours(13), EndTime = today.AddHours(16) },
            new() { Id = "f4", ResourceId = "A2", StartTime = today.AddHours(2), EndTime = today.AddHours(4) },
            new() { Id = "f5", ResourceId = "A2", StartTime = today.AddHours(9.5), EndTime = today.AddHours(12) },
            new() { Id = "f6", ResourceId = "A3", StartTime = today.AddHours(4), EndTime = today.AddHours(9) },
            new() { Id = "f7", ResourceId = "A3", StartTime = today.AddHours(17), EndTime = today.AddHours(20) },
            new() { Id = "f8", ResourceId = "A4", StartTime = today.AddHours(7), EndTime = today.AddHours(10) },
            new() { Id = "f9", ResourceId = "A4", StartTime = today.AddHours(20), EndTime = today.AddHours(27) },
            new() { Id = "f10", ResourceId = "A2", StartTime = today.AddDays(1).AddHours(8), EndTime = today.AddDays(1).AddHours(11) },
        ],
    };
}
""";

    private readonly string example2RazorCode = """
<style>
    .timeline {
        height: 22rem;
    }
</style>

<div class="timeline">
    <BitResourceTimeline Config="hierarchyConfig" />
</div>
""";
    private readonly string example2CsharpCode = """
private readonly BitResourceTimelineConfig hierarchyConfig = CreateHierarchyConfig();

private static BitResourceTimelineConfig CreateHierarchyConfig()
{
    var today = BitResourceTimelineDemoData.Today;

    List<BitResourceTimelineResource> resources =
    [
        new() { Id = "TA", Name = "Terminal A" },
        new() { Id = "A1", Name = "Gate A1", ParentId = "TA" },
        new() { Id = "A2", Name = "Gate A2", ParentId = "TA" },
        new() { Id = "AR", Name = "Remote stands", ParentId = "TA" },
        new() { Id = "R1", Name = "Stand R1", ParentId = "AR" },
        new() { Id = "R2", Name = "Stand R2", ParentId = "AR" },
        new() { Id = "TB", Name = "Terminal B", Collapsed = true },
        new() { Id = "B1", Name = "Gate B1", ParentId = "TB" },
        new() { Id = "B2", Name = "Gate B2", ParentId = "TB" },
    ];

    return new()
    {
        StartDate = today,
        EndDate = today.AddDays(3),
        Resources = resources,
        Allocations =
        [
            // The flights of the gates and the stands; the group rows get none from the helper.
            .. BitResourceTimelineDemoData.Flights(resources, today, today.AddDays(3)),
            // A group row can still hold bars of its own.
            new() { Id = "TA-cleaning", ResourceId = "TA", StartTime = today.AddHours(1), EndTime = today.AddHours(4) },
            new() { Id = "TA-inspection", ResourceId = "TA", StartTime = today.AddHours(13), EndTime = today.AddHours(15) },
        ],
    };
}
""";
    private readonly DemoCodeFile[] example2CodeFiles =
    [
        new("BitResourceTimelineDemoData.cs", demoDataCode),
    ];

    private readonly string example3RazorCode = """
<style>
    .timeline {
        height: 22rem;
    }
</style>

<div class="timeline">
    <BitResourceTimeline Config="appearanceConfig" />
</div>
""";
    private readonly string example3CsharpCode = """
private readonly BitResourceTimelineConfig appearanceConfig = CreateAppearanceConfig();

private static BitResourceTimelineConfig CreateAppearanceConfig()
{
    var today = BitResourceTimelineDemoData.Today;

    return new()
    {
        StartDate = today,
        EndDate = today.AddDays(1),
        Resources = BitResourceTimelineDemoData.Gates(4),
        Allocations =
        [
            new()
            {
                Id = "LH441",
                ResourceId = "A1",
                StartTime = today.AddHours(2),
                EndTime = today.AddHours(6),
                Color = "#2f9e44",
                Height = 10,
                TextAbove = "LH441",
                TextBelow = "Frankfurt",
                TextStart = "02:00",
                TextEnd = "06:00",
            },
            new()
            {
                Id = "BA117",
                ResourceId = "A1",
                StartTime = today.AddHours(11),
                EndTime = today.AddHours(15),
                Color = "#1971c2",
                Height = 6,
                TextAbove = "BA117 (hover me)",
                Tooltip = "BA117 to London\nBoarding at 14:20, gate closes at 14:45",
            },
            new()
            {
                Id = "AF1380",
                ResourceId = "A2",
                StartTime = today.AddHours(4),
                EndTime = today.AddHours(8),
                Color = "#e8590c",
                Height = 10,
                TextAbove = "AF1380 (delayed)",
                StartBar = new() { Duration = TimeSpan.FromMinutes(50), Color = "#ffa8a8" },
                EndBar = new() { Duration = TimeSpan.FromMinutes(30), Color = "#fa5252" },
            },
            new()
            {
                Id = "KL1002",
                ResourceId = "A3",
                StartTime = today.AddHours(3),
                EndTime = today.AddHours(8),
                Color = "#9c36b5",
                Height = 8,
                Icons =
                [
                    new() { Source = BitResourceTimelineDemoData.WarningIcon, Position = BitResourceTimelineBarIconPosition.Start },
                    new() { Source = BitResourceTimelineDemoData.CheckIcon, Position = BitResourceTimelineBarIconPosition.End },
                    new() { Source = BitResourceTimelineDemoData.StarIcon, Position = BitResourceTimelineBarIconPosition.Above },
                    new() { Source = BitResourceTimelineDemoData.StarIcon, Position = BitResourceTimelineBarIconPosition.Above },
                ],
            },
            new()
            {
                Id = "EK47",
                ResourceId = "A4",
                StartTime = today.AddHours(2),
                EndTime = today.AddHours(9),
                Color = "#0c8599",
                Height = 22,
                Icons =
                [
                    new() { Source = BitResourceTimelineDemoData.PlaneIcon, Position = BitResourceTimelineBarIconPosition.Start, Inside = true },
                    new() { Source = BitResourceTimelineDemoData.LockIcon, Position = BitResourceTimelineBarIconPosition.Center },
                    new() { Source = BitResourceTimelineDemoData.CheckIcon, Position = BitResourceTimelineBarIconPosition.End, Inside = true, Size = 14 },
                ],
            },
        ],
    };
}
""";
    private readonly DemoCodeFile[] example3CodeFiles =
    [
        new("BitResourceTimelineDemoData.cs", demoDataCode),
    ];

    private readonly string example4RazorCode = """
<style>
    .timeline {
        height: 22rem;
    }
</style>

<div class="timeline">
    <BitResourceTimeline Config="sectionsConfig" />
</div>
""";
    private readonly string example4CsharpCode = """
private readonly BitResourceTimelineConfig sectionsConfig = CreateSectionsConfig();

private static BitResourceTimelineConfig CreateSectionsConfig()
{
    var today = BitResourceTimelineDemoData.Today;

    return new()
    {
        StartDate = today,
        EndDate = today.AddDays(1),
        Resources = BitResourceTimelineDemoData.Gates(3),
        Allocations =
        [
            new()
            {
                // Bounded by times: deboarding until 03:00, boarding from 06:30.
                Id = "LH441",
                ResourceId = "A1",
                StartTime = today.AddHours(2),
                EndTime = today.AddHours(8),
                Color = "#1971c2",
                Height = 18,
                Sections =
                [
                    new() { EndTime = today.AddHours(3), BackgroundColor = "#fab005", ForegroundColor = "#212529", Text = "Deboard" },
                    new()
                    {
                        StartTime = today.AddHours(3),
                        EndTime = today.AddHours(6.5),
                        ForegroundColor = "#ffffff",
                        Text = "LH441",
                        Icon = BitResourceTimelineDemoData.PlaneIcon,
                        IconSize = 14,
                    },
                    new()
                    {
                        StartTime = today.AddHours(6.5),
                        BackgroundColor = "#d0ebff",
                        BorderColor = "#1971c2",
                        ForegroundColor = "#1971c2",
                        Text = "Board",
                        TextPosition = BitResourceTimelineBarSectionPosition.End,
                    },
                ],
            },
            new()
            {
                // Bounded by a percentage: the part of the job that has run.
                Id = "job",
                ResourceId = "A2",
                StartTime = today.AddHours(1),
                EndTime = today.AddHours(9),
                Color = "#ced4da",
                Height = 18,
                Sections =
                [
                    new()
                    {
                        EndPercent = 40,
                        BackgroundColor = "#2f9e44",
                        ForegroundColor = "#ffffff",
                        Text = "40% done",
                        TextPosition = BitResourceTimelineBarSectionPosition.Start,
                        Icon = BitResourceTimelineDemoData.CheckIcon,
                        IconPosition = BitResourceTimelineBarSectionPosition.End,
                    },
                ],
            },
            new()
            {
                // Percentages travel and stretch with the bar when it is edited.
                Id = "BA117",
                ResourceId = "A3",
                StartTime = today.AddHours(3),
                EndTime = today.AddHours(10),
                Color = "#9c36b5",
                Height = 18,
                Sections =
                [
                    new() { EndPercent = 20, BackgroundColor = "#e599f7", ForegroundColor = "#212529", Text = "Prep" },
                    new()
                    {
                        StartPercent = 20,
                        EndPercent = 85,
                        ForegroundColor = "#ffffff",
                        Text = "BA117",
                        Icon = BitResourceTimelineDemoData.StarIcon,
                        IconPosition = BitResourceTimelineBarSectionPosition.Center,
                    },
                    new()
                    {
                        StartPercent = 85,
                        BackgroundColor = "#f8f0fc",
                        BorderColor = "#9c36b5",
                        ForegroundColor = "#9c36b5",
                        Text = "Wrap",
                        TextPosition = BitResourceTimelineBarSectionPosition.End,
                    },
                ],
            },
        ],
    };
}
""";
    private readonly DemoCodeFile[] example4CodeFiles =
    [
        new("BitResourceTimelineDemoData.cs", demoDataCode),
    ];

    private readonly string example5RazorCode = """
<style>
    .timeline {
        height: 22rem;
    }

    /* The HTML renderer fills a bar with a background-color, so a hatch painted through ClassName draws over it. */
    .departed {
        background-image: repeating-linear-gradient(135deg, rgba(255, 255, 255, 0.45) 0 3px, transparent 3px 7px);
    }
</style>

<BitChoiceGroup Label="Renderer" Horizontal
                TItem="BitChoiceGroupOption<BitResourceTimelineRendererType>" TValue="BitResourceTimelineRendererType"
                @bind-Value="renderer" @bind-Value:after="() => rendererOptions = new() { Renderer = renderer }">
    <BitChoiceGroupOption Text="Canvas" Value="BitResourceTimelineRendererType.Canvas" />
    <BitChoiceGroupOption Text="Svg" Value="BitResourceTimelineRendererType.Svg" />
    <BitChoiceGroupOption Text="Html" Value="BitResourceTimelineRendererType.Html" />
</BitChoiceGroup>

<div class="timeline">
    <BitResourceTimeline Config="rendererConfig" Options="rendererOptions" />
</div>
""";
    private readonly string example5CsharpCode = """
private readonly BitResourceTimelineConfig rendererConfig = CreateRendererConfig();
private BitResourceTimelineRendererType renderer = BitResourceTimelineRendererType.Canvas;
private BitResourceTimelineOptions rendererOptions = new() { Renderer = BitResourceTimelineRendererType.Canvas };

private static BitResourceTimelineConfig CreateRendererConfig()
{
    var config = BitResourceTimelineDemoData.Airport(BitResourceTimelineDemoData.Gates(6), fromDay: 0, toDay: 3, labeled: true);

    // The HTML renderer puts ClassName on the element of the bar: the stylesheet hatches the flights already gone.
    foreach (var flight in config.Allocations.Where(f => f.EndTime < DateTimeOffset.Now))
    {
        flight.ClassName = "departed";
    }

    return config;
}
""";
    private readonly DemoCodeFile[] example5CodeFiles =
    [
        new("BitResourceTimelineDemoData.cs", demoDataCode),
    ];

    private readonly string example6RazorCode = """
<style>
    .toolbar {
        gap: 0.5rem;
        display: flex;
        flex-wrap: wrap;
        align-items: center;
    }

    .timeline {
        height: 22rem;
    }

    .readout {
        color: gray;
        margin-top: 0.5rem;
    }
</style>

<div class="toolbar">
    <BitButton Variant="BitVariant.Outline" OnClick="() => navigationRef.PanByDaysAsync(-7)">« Week</BitButton>
    <BitButton Variant="BitVariant.Outline" OnClick="() => navigationRef.PanByDaysAsync(-1)">‹ Day</BitButton>
    <BitButton OnClick="() => navigationRef.GoToTodayAsync()">Today</BitButton>
    <BitButton Variant="BitVariant.Outline" OnClick="() => navigationRef.PanByDaysAsync(1)">Day ›</BitButton>
    <BitButton Variant="BitVariant.Outline" OnClick="() => navigationRef.PanByDaysAsync(7)">Week »</BitButton>
    <BitToggle @bind-Value="panToDayStart" @bind-Value:after="() => navigationOptions = CreateNavigationOptions(panToDayStart)" Label="PanToDayStart" Inline />
</div>
<br />
<div class="toolbar">
    <BitButton Variant="BitVariant.Outline" OnClick="() => navigationRef.ZoomOutAsync()">Zoom out</BitButton>
    <BitButton Variant="BitVariant.Outline" OnClick="() => navigationRef.ZoomInAsync()">Zoom in</BitButton>
    <BitButton Variant="BitVariant.Outline" OnClick="() => navigationRef.ZoomToDaysAsync(1)">1 day</BitButton>
    <BitButton Variant="BitVariant.Outline" OnClick="() => navigationRef.ZoomToDaysAsync(3)">3 days</BitButton>
    <BitButton Variant="BitVariant.Outline" OnClick="() => navigationRef.ZoomToDaysAsync(7)">7 days</BitButton>
    <BitButton Variant="BitVariant.Outline" OnClick="() => navigationRef.SetPixelsPerHourAsync(120)">120 px/h</BitButton>
    <BitButton Variant="BitVariant.Outline" OnClick="() => navigationRef.ResetZoomAsync()">Reset zoom</BitButton>
    <BitButton Variant="BitVariant.Outline" OnClick="() => navigationRef.ScrollToTimeAsync(new DateTimeOffset(DateTime.Today.AddDays(1).AddHours(12)))">Tomorrow noon</BitButton>
</div>
<br />
<div class="timeline">
    <BitResourceTimeline @ref="navigationRef"
                         Config="navigationConfig"
                         Options="navigationOptions"
                         OnViewChanged="view => navigationView = view" />
</div>
<div class="readout">@NavigationText</div>
""";
    private readonly string example6CsharpCode = """
private BitResourceTimeline navigationRef = default!;
private readonly BitResourceTimelineConfig navigationConfig = BitResourceTimelineDemoData.Airport(BitResourceTimelineDemoData.Gates(6), fromDay: -14, toDay: 14, labeled: true);
private bool panToDayStart;
private BitResourceTimelineOptions navigationOptions = CreateNavigationOptions(false);
private BitResourceTimelineView? navigationView;

private static BitResourceTimelineOptions CreateNavigationOptions(bool panToDayStart) => new()
{
    AutoScrollToNow = true,
    MinPixelsPerHour = 2,
    MaxPixelsPerHour = 240,
    PanToDayStart = panToDayStart,
};

private string NavigationText => navigationView is null
    ? "Scroll or zoom the timeline"
    : $"{navigationView.Start.ToLocalTime():ddd d MMM HH:mm} - {navigationView.End.ToLocalTime():ddd d MMM HH:mm} · {navigationView.PixelsPerHour:0.#} px/h";
""";
    private readonly DemoCodeFile[] example6CodeFiles =
    [
        new("BitResourceTimelineDemoData.cs", demoDataCode),
    ];

    private readonly string example7RazorCode = """
<style>
    .toolbar {
        gap: 0.5rem;
        display: flex;
        flex-wrap: wrap;
        align-items: center;
    }

    .timeline {
        height: 22rem;
    }

    .readout {
        color: gray;
        margin-top: 0.5rem;
    }
</style>

<div class="toolbar">
    <BitButton Variant="BitVariant.Outline" OnClick="SelectNextFlights">Select the next 3 flights</BitButton>
    <BitButton Variant="BitVariant.Outline" OnClick="ScrollToSelection">Scroll to the selection</BitButton>
    <BitButton Variant="BitVariant.Outline" OnClick='() => selectionRef.ScrollToResourceAsync("A12")'>Scroll to Gate A12</BitButton>
    <BitButton Variant="BitVariant.Outline" OnClick="() => selectionRef.ClearSelectionAsync()">Clear the selection</BitButton>
</div>
<br />
<div class="timeline">
    <BitResourceTimeline @ref="selectionRef"
                         Config="selectionConfig"
                         Options="selectionOptions"
                         OnSelectionChanged="bars => selectedBars = bars"
                         OnClick="args => clickText = DescribePointer(args)"
                         OnDoubleClick="args => doubleClickText = DescribePointer(args)"
                         OnContextMenu="args => contextMenuText = DescribePointer(args)" />
</div>
<div class="readout">Selected: @(selectedBars.Length == 0 ? "nothing" : string.Join(", ", selectedBars.Select(b => b.TextAbove)))</div>
<div class="readout">Click: @clickText</div>
<div class="readout">Double-click: @doubleClickText</div>
<div class="readout">Right-click: @contextMenuText</div>
""";
    private readonly string example7CsharpCode = """
private BitResourceTimeline selectionRef = default!;
private readonly BitResourceTimelineConfig selectionConfig = BitResourceTimelineDemoData.Airport(BitResourceTimelineDemoData.Gates(12), labeled: true);
private readonly BitResourceTimelineOptions selectionOptions = new() { AutoScrollToNow = true };
private BitResourceTimelineAllocation[] selectedBars = [];
private string? clickText;
private string? doubleClickText;
private string? contextMenuText;

private async Task SelectNextFlights()
{
    var now = DateTimeOffset.Now;
    var next = selectionConfig.Allocations.Where(f => f.StartTime > now).OrderBy(f => f.StartTime).Take(3).Select(f => f.Id).ToArray();

    await selectionRef.SelectAsync(next);
}

private async Task ScrollToSelection()
{
    var selected = await selectionRef.GetSelectedBarsAsync();

    if (selected.Count > 0)
    {
        await selectionRef.ScrollToAllocationAsync(selected[0].Id);
    }
}

private static string DescribePointer(BitResourceTimelinePointerArgs args)
{
    List<string> parts = [args.Area.ToString()];

    if (args.Resource is not null) parts.Add(args.Resource.Name);
    if (args.Time is { } time) parts.Add(time.ToLocalTime().ToString("ddd HH:mm"));
    if (args.Allocation is not null) parts.Add($"flight {args.Allocation.TextAbove}");

    return string.Join(" · ", parts);
}
""";
    private readonly DemoCodeFile[] example7CodeFiles =
    [
        new("BitResourceTimelineDemoData.cs", demoDataCode),
    ];

    private readonly string example8RazorCode = """
<style>
    .toolbar {
        gap: 0.5rem;
        display: flex;
        flex-wrap: wrap;
        align-items: center;
    }

    .timeline {
        height: 22rem;
    }

    .log {
        color: gray;
        margin-top: 0.5rem;
        min-height: 6.5rem;
    }
</style>

<div class="toolbar">
    <BitToggle @bind-Value="allowResourceChange" @bind-Value:after="() => editingOptions = CreateEditingOptions(allowResourceChange)" Label="AllowResourceChange" Inline />
</div>
<br />
<div class="timeline">
    <BitResourceTimeline Config="editingConfig"
                         Options="editingOptions"
                         OnAllocationChanging="HandleAllocationChanging"
                         OnAllocationsChanging="HandleAllocationsChanging"
                         OnAllocationChanged="HandleAllocationChanged"
                         OnAllocationCreating="HandleAllocationCreating"
                         OnAllocationsCopying="HandleAllocationsCopying"
                         OnAllocationsDeleting="HandleAllocationsDeleting" />
</div>
<div class="log">
    @foreach (var line in editLog)
    {
        <div>@line</div>
    }
</div>
""";
    private readonly string example8CsharpCode = """
private readonly BitResourceTimelineConfig editingConfig = CreateEditingConfig();
private bool allowResourceChange = true;
private BitResourceTimelineOptions editingOptions = CreateEditingOptions(true);
private readonly List<string> editLog = [];
private int createdCount;

private static BitResourceTimelineConfig CreateEditingConfig()
{
    var config = BitResourceTimelineDemoData.Airport(BitResourceTimelineDemoData.Gates(6), labeled: true);
    var now = DateTimeOffset.Now;

    // A flight at its gate right now cannot be moved: Locked keeps it selectable but not editable.
    foreach (var flight in config.Allocations.Where(f => f.StartTime <= now && f.EndTime > now))
    {
        flight.Locked = true;
        flight.Color = "#868e96";
        flight.TextBelow = "at the gate";
    }

    return config;
}

private static BitResourceTimelineOptions CreateEditingOptions(bool allowResourceChange) => new()
{
    AutoScrollToNow = true,
    Editable = true,
    EditSnapMinutes = 15,
    AllowResourceChange = allowResourceChange,
    EmptyDragAction = BitResourceTimelineEmptyDragAction.Create,
    AllowDelete = true,
};

private Task<bool> HandleAllocationChanging(BitResourceTimelineAllocationChange change) => Task.FromResult(AcceptEdit([change]));

private Task<bool> HandleAllocationsChanging(IReadOnlyList<BitResourceTimelineAllocationChange> changes) => Task.FromResult(AcceptEdit(changes));

// The past is history: an edit that would start a flight before now is refused, and the bars snap back.
private bool AcceptEdit(IReadOnlyList<BitResourceTimelineAllocationChange> changes)
{
    var refused = changes.FirstOrDefault(c => c.Allocation.StartTime < DateTimeOffset.Now);

    if (refused is null) return true;

    Log($"Refused: {refused.Allocation.TextAbove} would start in the past");
    return false;
}

private void HandleAllocationChanged(BitResourceTimelineAllocation flight)
{
    Log($"{flight.TextAbove}: Gate {flight.ResourceId}, {flight.StartTime.ToLocalTime():ddd HH:mm} - {flight.EndTime.ToLocalTime():HH:mm}");
}

private Task<BitResourceTimelineAllocation?> HandleAllocationCreating(BitResourceTimelineCreateRequest request)
{
    // The timeline never invents ids: the app builds the allocation (or returns null to cancel).
    var flight = new BitResourceTimelineAllocation
    {
        Id = $"new-{++createdCount}",
        ResourceId = request.ResourceId,
        StartTime = request.StartTime,
        EndTime = request.EndTime,
        Color = "#2f9e44",
        TextAbove = $"NEW{createdCount}",
    };

    editingConfig.Allocations.Add(flight);
    Log($"Created {flight.TextAbove} at Gate {flight.ResourceId}");

    return Task.FromResult<BitResourceTimelineAllocation?>(flight);
}

private Task<IReadOnlyList<BitResourceTimelineAllocation>?> HandleAllocationsCopying(BitResourceTimelineCopyRequest request)
{
    List<BitResourceTimelineAllocation> copies =
    [
        .. request.Allocations.Select(source => new BitResourceTimelineAllocation
        {
            Id = $"copy-{++createdCount}",
            ResourceId = source.ResourceId,
            StartTime = source.StartTime + request.Offset,
            EndTime = source.EndTime + request.Offset,
            Color = "#2f9e44",
            TextAbove = $"{source.TextAbove} copy",
        })
    ];

    editingConfig.Allocations.AddRange(copies);
    Log($"Pasted {copies.Count}");

    return Task.FromResult<IReadOnlyList<BitResourceTimelineAllocation>?>(copies);
}

private Task<bool> HandleAllocationsDeleting(IReadOnlyList<string> ids)
{
    if (editingConfig.Allocations.Any(f => f.Locked && ids.Contains(f.Id)))
    {
        Log("Refused: a flight at its gate cannot be deleted");
        return Task.FromResult(false);
    }

    editingConfig.Allocations.RemoveAll(f => ids.Contains(f.Id));
    Log($"Deleted {ids.Count}");

    return Task.FromResult(true);
}

private void Log(string line)
{
    editLog.Insert(0, line);
    if (editLog.Count > 5) editLog.RemoveAt(5);

    StateHasChanged();
}
""";
    private readonly DemoCodeFile[] example8CodeFiles =
    [
        new("BitResourceTimelineDemoData.cs", demoDataCode),
    ];

    private readonly string example9RazorCode = """
<style>
    .toolbar {
        gap: 0.5rem;
        display: flex;
        flex-wrap: wrap;
        align-items: center;
    }

    .timeline {
        height: 22rem;
    }
</style>

<BitChoiceGroup Label="MaxStackLanes" Horizontal
                TItem="BitChoiceGroupOption<int>" TValue="int"
                @bind-Value="maxStackLanes" @bind-Value:after="UpdateOverlapOptions">
    <BitChoiceGroupOption Text="Unlimited" Value="0" />
    <BitChoiceGroupOption Text="3" Value="3" />
    <BitChoiceGroupOption Text="2" Value="2" />
</BitChoiceGroup>
<br />
<div class="toolbar">
    <BitToggle @bind-Value="stackLabelClearance" @bind-Value:after="UpdateOverlapOptions" Label="StackLabelClearance" Inline />
    <BitToggle @bind-Value="stackOnLabelCollision" @bind-Value:after="UpdateOverlapOptions" Label="StackOnLabelCollision" Inline />
    <BitToggle @bind-Value="allowOverlap" @bind-Value:after="UpdateOverlapOptions" Label="AllowOverlap" Inline />
</div>
<br />
<div class="timeline">
    <BitResourceTimeline Config="overlapConfig" Options="overlapOptions" />
</div>
""";
    private readonly string example9CsharpCode = """
private readonly BitResourceTimelineConfig overlapConfig = CreateOverlapConfig();
private int maxStackLanes;
private bool stackLabelClearance = true;
private bool stackOnLabelCollision = true;
private bool allowOverlap = true;
private BitResourceTimelineOptions overlapOptions = CreateOverlapOptions(0, true, true, true);

private void UpdateOverlapOptions() => overlapOptions = CreateOverlapOptions(maxStackLanes, stackLabelClearance, stackOnLabelCollision, allowOverlap);

private static BitResourceTimelineOptions CreateOverlapOptions(int maxStackLanes, bool stackLabelClearance, bool stackOnLabelCollision, bool allowOverlap) => new()
{
    Editable = true,
    BarHeight = 8,
    BarMargin = 4,
    MaxStackLanes = maxStackLanes,
    StackLabelClearance = stackLabelClearance,
    StackOnLabelCollision = stackOnLabelCollision,
    AllowOverlap = allowOverlap,
};

private static BitResourceTimelineConfig CreateOverlapConfig()
{
    var today = BitResourceTimelineDemoData.Today;

    BitResourceTimelineAllocation Flight(string gate, string id, double from, double to, string? below = null, bool times = false) => new()
    {
        Id = id,
        ResourceId = gate,
        StartTime = today.AddHours(from),
        EndTime = today.AddHours(to),
        TextAbove = id,
        TextBelow = below,
        TextStart = times ? today.AddHours(from).ToString("HH:mm", CultureInfo.InvariantCulture) : null,
        TextEnd = times ? today.AddHours(to).ToString("HH:mm", CultureInfo.InvariantCulture) : null,
    };

    return new()
    {
        StartDate = today,
        EndDate = today.AddDays(1),
        Resources = BitResourceTimelineDemoData.Gates(4),
        Allocations =
        [
            // Five at once: five lanes, or MaxStackLanes and a +N marker.
            Flight("A1", "LH400", 2, 7),
            Flight("A1", "LH402", 3, 8),
            Flight("A1", "LH404", 3.5, 6),
            Flight("A1", "LH406", 4, 9),
            Flight("A1", "LH408", 5, 7.5),
            // A quarter of an hour apart: their times collide, though they do not overlap.
            Flight("A2", "BA200", 3, 6, times: true),
            Flight("A2", "BA202", 6.25, 9, times: true),
            // Overlapping, with labels above and below.
            Flight("A3", "AF300", 2, 7, below: "Paris"),
            Flight("A3", "AF302", 4, 9, below: "Nice"),
            // Drop a bar on this one to try AllowOverlap.
            Flight("A4", "KL500", 3, 6),
        ],
    };
}
""";
    private readonly DemoCodeFile[] example9CodeFiles =
    [
        new("BitResourceTimelineDemoData.cs", demoDataCode),
    ];

    private readonly string example10RazorCode = """
<style>
    .timeline {
        height: 22rem;
    }
</style>

<BitChoiceGroup Label="Surface" Horizontal
                TItem="BitChoiceGroupOption<string>" TValue="string"
                @bind-Value="surfacePreset" @bind-Value:after="UpdateSurfaceOptions">
    <BitChoiceGroupOption Text="None" Value="@("none")" />
    <BitChoiceGroupOption Text="Days" Value="@("days")" />
    <BitChoiceGroupOption Text="Shifts" Value="@("shifts")" />
    <BitChoiceGroupOption Text="Rows" Value="@("rows")" />
    <BitChoiceGroupOption Text="Checker" Value="@("checker")" />
    <BitChoiceGroupOption Text="Bands" Value="@("bands")" />
</BitChoiceGroup>
<br />
<BitToggle @bind-Value="axisLines" @bind-Value:after="UpdateSurfaceOptions" Label="AxisLines" Inline />
<br />
<div class="timeline">
    <BitResourceTimeline Config="surfaceConfig" Options="surfaceOptions" />
</div>
""";
    private readonly string example10CsharpCode = """
private readonly BitResourceTimelineConfig surfaceConfig = CreateSurfaceConfig();
private string? surfacePreset = "days";
private bool axisLines;
private BitResourceTimelineOptions surfaceOptions = CreateSurfaceOptions("days", false);

private void UpdateSurfaceOptions() => surfaceOptions = CreateSurfaceOptions(surfacePreset, axisLines);

private static BitResourceTimelineConfig CreateSurfaceConfig()
{
    var config = BitResourceTimelineDemoData.Airport(BitResourceTimelineDemoData.Gates(6), labeled: true);

    // A resource paints its own row and line, over any pattern of the surface.
    config.Resources[2].Name = "Gate A3 (closed)";
    config.Resources[2].Background = "rgba(250, 82, 82, 0.15)";
    config.Allocations.RemoveAll(f => f.ResourceId == "A3");
    config.Resources[4].LineColor = "#e8590c";

    return config;
}

private static BitResourceTimelineOptions CreateSurfaceOptions(string? preset, bool axisLines)
{
    var today = BitResourceTimelineDemoData.Today;

    return new()
    {
        AutoScrollToNow = true,
        // The weekends, and the hours outside 09:00 - 17:00, are washed over whatever the surface paints.
        NonWorkingDays = [0, 6],
        WorkingHoursStart = 9 * 60,
        WorkingHoursEnd = 17 * 60,
        // A new surface replaces the previous one as a whole, and an empty one clears it.
        Surface = preset switch
        {
            "days" => new()
            {
                Columns = new() { Colors = ["rgba(25, 113, 194, 0.08)", "transparent"] },
                ShadeTimeAxis = true,
            },
            "shifts" => new()
            {
                Columns = new()
                {
                    Unit = BitResourceTimelineSurfaceUnit.Hour,
                    Span = 8,
                    Align = BitResourceTimelineStripeAlign.Repeat,
                    Colors = ["rgba(250, 176, 5, 0.12)", "rgba(47, 158, 68, 0.1)", "rgba(25, 113, 194, 0.1)"],
                },
                ShadeTimeAxis = true,
            },
            "rows" => new()
            {
                Rows = new() { Colors = ["rgba(134, 142, 150, 0.12)", "transparent"] },
                ResourceColors = new() { ["A6"] = "rgba(156, 54, 181, 0.15)" },
                ShadeResourceAxis = true,
            },
            "checker" => new()
            {
                Columns = new() { Colors = ["rgba(25, 113, 194, 0.1)", "transparent"] },
                Combine = BitResourceTimelineSurfaceCombine.Checker,
            },
            "bands" => new()
            {
                Bands =
                [
                    new() { Start = today.AddHours(12), End = today.AddHours(14), Color = "rgba(250, 82, 82, 0.15)" },
                    new() { Start = today.AddHours(6), End = today.AddHours(10), Color = "rgba(250, 176, 5, 0.3)", ResourceId = "A2" },
                ],
            },
            _ => new(),
        },
        AxisLines = axisLines
            ? new()
            {
                BoundariesOnly = true,
                Vertical = new() { Colors = ["#1971c2"] },
                Horizontal = new() { Colors = ["rgba(134, 142, 150, 0.6)"] },
            }
            : new(),
    };
}
""";
    private readonly DemoCodeFile[] example10CodeFiles =
    [
        new("BitResourceTimelineDemoData.cs", demoDataCode),
    ];

    private readonly string example11RazorCode = """
<style>
    .toolbar {
        gap: 0.5rem;
        display: flex;
        flex-wrap: wrap;
        align-items: center;
    }

    .timeline {
        height: 22rem;
    }
</style>

<BitChoiceGroup Label="TimeZone" Horizontal
                TItem="BitChoiceGroupOption<string>" TValue="string"
                @bind-Value="timeZone" @bind-Value:after="UpdateZoneOptions">
    <BitChoiceGroupOption Text="Viewer's" Value="@("")" />
    <BitChoiceGroupOption Text="UTC" Value="@("UTC")" />
    <BitChoiceGroupOption Text="New York" Value="@("America/New_York")" />
    <BitChoiceGroupOption Text="Kolkata" Value="@("Asia/Kolkata")" />
    <BitChoiceGroupOption Text="Tokyo" Value="@("Asia/Tokyo")" />
</BitChoiceGroup>
<br />
<BitChoiceGroup Label="Locale" Horizontal
                TItem="BitChoiceGroupOption<string>" TValue="string"
                @bind-Value="locale" @bind-Value:after="UpdateZoneOptions">
    <BitChoiceGroupOption Text="Viewer's" Value="@("")" />
    <BitChoiceGroupOption Text="en-US" Value="@("en-US")" />
    <BitChoiceGroupOption Text="de-DE" Value="@("de-DE")" />
    <BitChoiceGroupOption Text="ja-JP" Value="@("ja-JP")" />
</BitChoiceGroup>
<br />
<BitChoiceGroup Label="FirstDayOfWeek" Horizontal
                TItem="BitChoiceGroupOption<DayOfWeek>" TValue="DayOfWeek"
                @bind-Value="firstDayOfWeek" @bind-Value:after="UpdateZoneOptions">
    <BitChoiceGroupOption Text="Sunday" Value="DayOfWeek.Sunday" />
    <BitChoiceGroupOption Text="Monday" Value="DayOfWeek.Monday" />
    <BitChoiceGroupOption Text="Saturday" Value="DayOfWeek.Saturday" />
</BitChoiceGroup>
<br />
<div class="toolbar">
    <BitToggle @bind-Value="showUtcTime" @bind-Value:after="UpdateZoneOptions" Label="ShowUtcTime" Inline />
    <BitToggle @bind-Value="hour12" @bind-Value:after="UpdateZoneOptions" Label="Hour12" Inline />
    <BitButton Variant="BitVariant.Outline" OnClick="() => zoneRef.ZoomToDaysAsync(1)">1 day</BitButton>
    <BitButton Variant="BitVariant.Outline" OnClick="() => zoneRef.ZoomToDaysAsync(7)">7 days</BitButton>
</div>
<br />
<div class="timeline">
    <BitResourceTimeline @ref="zoneRef" Config="zoneConfig" Options="zoneOptions" />
</div>
""";
    private readonly string example11CsharpCode = """
private BitResourceTimeline zoneRef = default!;
private readonly BitResourceTimelineConfig zoneConfig = BitResourceTimelineDemoData.Airport(BitResourceTimelineDemoData.Gates(5), fromDay: -21, toDay: 21, labeled: true);
private string? timeZone = "";
private string? locale = "";
private DayOfWeek firstDayOfWeek = DayOfWeek.Monday;
private bool showUtcTime;
private bool hour12;
private BitResourceTimelineOptions zoneOptions = CreateZoneOptions("", "", DayOfWeek.Monday, false, false);

private void UpdateZoneOptions() => zoneOptions = CreateZoneOptions(timeZone, locale, firstDayOfWeek, showUtcTime, hour12);

private static BitResourceTimelineOptions CreateZoneOptions(string? timeZone, string? locale, DayOfWeek firstDayOfWeek, bool showUtcTime, bool hour12) => new()
{
    AutoScrollToNow = true,
    // An empty string is the viewer's own zone and locale: a null option would keep the one applied before.
    TimeZone = timeZone,
    Locale = locale,
    FirstDayOfWeek = firstDayOfWeek,
    ShowUtcTime = showUtcTime,
    // The UTC row shares the band under the day labels, so the axis grows with it.
    TimeAxisHeight = showUtcTime ? 80 : 60,
    Hour12 = hour12,
    NonWorkingDays = [0, 6],
    Surface = new()
    {
        Columns = new() { Unit = BitResourceTimelineSurfaceUnit.Week, Colors = ["rgba(25, 113, 194, 0.1)", "transparent"] },
        ShadeTimeAxis = true,
    },
};
""";
    private readonly DemoCodeFile[] example11CodeFiles =
    [
        new("BitResourceTimelineDemoData.cs", demoDataCode),
    ];

    private readonly string example12RazorCode = """
<style>
    .toolbar {
        gap: 0.5rem;
        display: flex;
        flex-wrap: wrap;
        align-items: center;
    }

    .timeline {
        height: 22rem;
    }

    .corner {
        height: 100%;
        display: flex;
        padding: 0 0.5rem;
        font-size: 0.75rem;
        flex-direction: column;
        justify-content: center;
        box-sizing: border-box;
    }

    .gate {
        gap: 0.5rem;
        width: 100%;
        display: flex;
        align-items: center;
        justify-content: space-between;
    }

    .gate-badge {
        color: white;
        padding: 0 0.5rem;
        font-size: 0.75rem;
        border-radius: 1rem;
        background-color: dodgerblue;
    }

    .flight-tip {
        display: flex;
        padding: 0.5rem 0.75rem;
        font-size: 0.8125rem;
        border-radius: 0.5rem;
        flex-direction: column;
        background-color: white;
        border: 1px solid lightgray;
        box-shadow: 0 2px 8px rgba(0, 0, 0, 0.2);
    }
</style>

<div class="toolbar">
    <BitButton Variant="BitVariant.Outline" OnClick="() => templatesConfig = CreateTemplatesConfig()">Reload</BitButton>
</div>
<br />
<div class="timeline">
    <BitResourceTimeline Config="templatesConfig" Options="templatesOptions" LoadingMinDurationMs="800">
        <TopStartContent>
            <div class="corner">
                <b>Gates</b>
                <span>flights in 2 weeks</span>
            </div>
        </TopStartContent>
        <ResourceTemplate Context="row">
            @if (row.HasChildren)
            {
                <b>@row.Resource.Name</b>
            }
            else
            {
                <div class="gate">
                    <span>@row.Resource.Name</span>
                    <span class="gate-badge">@FlightsOf(row.Resource.Id)</span>
                </div>
            }
        </ResourceTemplate>
        <TooltipTemplate Context="flight">
            <div class="flight-tip">
                <b>@flight.TextAbove</b>
                <span>Gate @flight.ResourceId</span>
                <span>@flight.StartTime.ToLocalTime().ToString("ddd HH:mm") - @flight.EndTime.ToLocalTime().ToString("HH:mm")</span>
            </div>
        </TooltipTemplate>
        <LoadingContent>
            <BitBarsLoading Size="BitSize.Small" Label="Loading the flights..." />
        </LoadingContent>
    </BitResourceTimeline>
</div>
""";
    private readonly string example12CsharpCode = """
private BitResourceTimelineConfig templatesConfig = CreateTemplatesConfig();
private readonly BitResourceTimelineOptions templatesOptions = new() { AutoScrollToNow = true, PreserveScrollOnReload = true };

private static BitResourceTimelineConfig CreateTemplatesConfig() => BitResourceTimelineDemoData.Airport(BitResourceTimelineDemoData.Terminals(2, 4), labeled: true);

private int FlightsOf(string resourceId) => templatesConfig.Allocations.Count(f => f.ResourceId == resourceId);
""";
    private readonly DemoCodeFile[] example12CodeFiles =
    [
        new("BitResourceTimelineDemoData.cs", demoDataCode),
    ];

    private readonly string example13RazorCode = """
<style>
    .toolbar {
        gap: 0.5rem;
        display: flex;
        flex-wrap: wrap;
        align-items: center;
    }

    .timeline {
        height: 22rem;
    }

    .readout {
        color: gray;
        margin-top: 0.5rem;
    }
</style>

<div class="toolbar">
    <BitToggle @bind-Value="resourceAxisResizable" @bind-Value:after="() => UpdateColumnOptions(resourceAxisWidth)" Label="ResourceAxisResizable" Inline />
    <BitButton Variant="BitVariant.Outline" OnClick="() => UpdateColumnOptions(120)">120px</BitButton>
    <BitButton Variant="BitVariant.Outline" OnClick="() => UpdateColumnOptions(200)">200px</BitButton>
    <BitButton Variant="BitVariant.Outline" OnClick="() => UpdateColumnOptions(320)">320px</BitButton>
</div>
<br />
<div class="timeline">
    <BitResourceTimeline Config="columnConfig" Options="columnOptions" OnResourceAxisWidthChanged="UpdateColumnOptions" />
</div>
<div class="readout">ResourceAxisWidth: @resourceAxisWidth px</div>
""";
    private readonly string example13CsharpCode = """
private readonly BitResourceTimelineConfig columnConfig = CreateColumnConfig();
private int resourceAxisWidth = 200;
private bool resourceAxisResizable = true;
private BitResourceTimelineOptions columnOptions = CreateColumnOptions(200, true);

private static BitResourceTimelineConfig CreateColumnConfig()
{
    var config = BitResourceTimelineDemoData.Airport(BitResourceTimelineDemoData.Gates(6), labeled: true);

    foreach (var gate in config.Resources)
    {
        gate.Name = $"{gate.Name} - long-haul departures";
    }

    return config;
}

// The width a resize committed comes back through OnResourceAxisWidthChanged; passing it back keeps it.
private void UpdateColumnOptions(int width)
{
    resourceAxisWidth = width;
    columnOptions = CreateColumnOptions(width, resourceAxisResizable);
}

private static BitResourceTimelineOptions CreateColumnOptions(int width, bool resizable) => new()
{
    AutoScrollToNow = true,
    ResourceAxisWidth = width,
    ResourceAxisMinWidth = 100,
    ResourceAxisMaxWidth = 360,
    ResourceAxisResizable = resizable,
};
""";
    private readonly DemoCodeFile[] example13CodeFiles =
    [
        new("BitResourceTimelineDemoData.cs", demoDataCode),
    ];

    private readonly string example14RazorCode = """
<style>
    .toolbar {
        gap: 0.5rem;
        display: flex;
        flex-wrap: wrap;
        align-items: center;
    }

    .timeline {
        height: 22rem;
    }

    .readout {
        color: gray;
        margin-top: 0.5rem;
    }
</style>

<div class="toolbar">
    <BitButton Variant="BitVariant.Outline" OnClick="DelayFlights">Delay 500 flights</BitButton>
    <BitButton Variant="BitVariant.Outline" OnClick="CancelFlights">Cancel 500 flights</BitButton>
    <BitButton Variant="BitVariant.Outline" OnClick="ShiftAllFlights">Move every flight an hour later</BitButton>
</div>
<br />
<div class="timeline">
    <BitResourceTimeline @ref="largeRef" Config="LargeConfig" Options="largeOptions" LoadBatchSize="5000" />
</div>
<div class="readout">@LargeConfig.Allocations.Count.ToString("N0") flights on @LargeConfig.Resources.Count resources@(largeStatus is null ? null : $" · {largeStatus}")</div>
""";
    private readonly string example14CsharpCode = """
private BitResourceTimeline largeRef = default!;
private BitResourceTimelineConfig? largeConfig;
private readonly BitResourceTimelineOptions largeOptions = new() { AutoScrollToNow = true, MinPixelsPerHour = 1 };
private string? largeStatus;

// About 40,000 flights of 160 gates over five weeks, built the first time the example renders.
private BitResourceTimelineConfig LargeConfig => largeConfig ??= BitResourceTimelineDemoData.Airport(BitResourceTimelineDemoData.Terminals(8, 20), fromDay: -14, toDay: 21);

private async Task DelayFlights()
{
    var watch = Stopwatch.StartNew();
    var now = DateTimeOffset.Now;
    var delayed = LargeConfig.Allocations.Where(f => f.StartTime > now && f.EndBar is null).Take(500).ToList();

    foreach (var flight in delayed)
    {
        flight.Color = "#e8590c";
        flight.EndBar = new() { Duration = TimeSpan.FromMinutes(45), Color = "#fa5252" };
    }

    // Merged by id, without reloading the rest or losing the selection.
    await largeRef.UpsertAllocationsAsync(delayed);

    largeStatus = $"{delayed.Count} flights delayed in {watch.ElapsedMilliseconds} ms";
}

private async Task CancelFlights()
{
    var watch = Stopwatch.StartNew();
    var now = DateTimeOffset.Now;
    var cancelled = LargeConfig.Allocations.Where(f => f.StartTime > now).Take(500).Select(f => f.Id).ToHashSet();

    // RemoveAllocationsAsync leaves Config.Allocations alone, so the app keeps it in step itself.
    LargeConfig.Allocations.RemoveAll(f => cancelled.Contains(f.Id));
    await largeRef.RemoveAllocationsAsync([.. cancelled]);

    largeStatus = $"{cancelled.Count} flights cancelled in {watch.ElapsedMilliseconds} ms";
}

private async Task ShiftAllFlights()
{
    var watch = Stopwatch.StartNew();

    foreach (var flight in LargeConfig.Allocations)
    {
        flight.StartTime = flight.StartTime.AddHours(1);
        flight.EndTime = flight.EndTime.AddHours(1);
    }

    // The Config was changed in place and its reference is the same, so ReloadAsync sends it again.
    await largeRef.ReloadAsync();

    largeStatus = $"{LargeConfig.Allocations.Count:N0} flights moved an hour later in {watch.ElapsedMilliseconds} ms";
}
""";
    private readonly DemoCodeFile[] example14CodeFiles =
    [
        new("BitResourceTimelineDemoData.cs", demoDataCode),
    ];

    private readonly string example15RazorCode = """
<style>
    .timeline {
        height: 22rem;
    }

    .readout {
        color: gray;
        margin-top: 0.5rem;
    }
</style>

<div class="timeline">
    <BitResourceTimeline Config="windowedConfig" Options="windowedOptions" LoadAllocationsAsync="LoadWindowAsync" />
</div>
<div class="readout">@windowedStatus</div>
""";
    private readonly string example15CsharpCode = """
// 100 gates over eight months - some 170,000 flights, far too many to load up front. The Config holds the
// resources and the range alone; the bars come from LoadWindowAsync.
private readonly BitResourceTimelineConfig windowedConfig = new()
{
    Resources = BitResourceTimelineDemoData.Terminals(5, 20),
    StartDate = BitResourceTimelineDemoData.Today.AddDays(-120),
    EndDate = BitResourceTimelineDemoData.Today.AddDays(120),
    Allocations = [],
};
private readonly BitResourceTimelineOptions windowedOptions = new() { AutoScrollToNow = true, MinPixelsPerHour = 6 };
private int windowRequests;
private string windowedStatus = "Waiting for the first window";

private async Task<IReadOnlyList<BitResourceTimelineAllocation>> LoadWindowAsync(BitResourceTimelineWindow window)
{
    await Task.Delay(400); // simulate a query of the server

    var flights = BitResourceTimelineDemoData.Flights(windowedConfig.Resources, window.Start, window.End, labeled: true);

    windowRequests++;
    windowedStatus = $"Request {windowRequests}: {window.Start.ToLocalTime():d MMM HH:mm} - {window.End.ToLocalTime():d MMM HH:mm}, {flights.Count:N0} flights";
    StateHasChanged();

    return flights;
}
""";
    private readonly DemoCodeFile[] example15CodeFiles =
    [
        new("BitResourceTimelineDemoData.cs", demoDataCode),
    ];

    private readonly string example16RazorCode = """
<style>
    .timeline {
        height: 22rem;
    }

    .readout {
        color: gray;
        margin-top: 0.5rem;
    }
</style>

<BitToggle @bind-Value="isDisabled" Label="Disabled" Inline />
<br />
<div class="timeline">
    <BitResourceTimeline Config="disabledConfig"
                         Options="disabledOptions"
                         Disabled="isDisabled"
                         OnClick="() => disabledClicks++" />
</div>
<div class="readout">Clicks: @disabledClicks</div>
""";
    private readonly string example16CsharpCode = """
private readonly BitResourceTimelineConfig disabledConfig = BitResourceTimelineDemoData.Airport(BitResourceTimelineDemoData.Gates(5), labeled: true);
private readonly BitResourceTimelineOptions disabledOptions = new() { AutoScrollToNow = true, Editable = true };
private bool isDisabled = true;
private int disabledClicks;
""";
    private readonly DemoCodeFile[] example16CodeFiles =
    [
        new("BitResourceTimelineDemoData.cs", demoDataCode),
    ];

    private readonly string example17RazorCode = """
<style>
    .toolbar {
        gap: 0.5rem;
        display: flex;
        flex-wrap: wrap;
        align-items: center;
    }

    .timeline {
        height: 22rem;
    }
</style>

<div class="toolbar">
    <BitButton Variant="BitVariant.Outline" OnClick="() => textsConfig = BitResourceTimelineDemoData.Airport(BitResourceTimelineDemoData.Gates(5), labeled: true)">Neu laden</BitButton>
</div>
<br />
<div class="timeline">
    <BitResourceTimeline Config="textsConfig" Options="textsOptions" Texts="germanTexts" LoadingMinDurationMs="800" />
</div>
""";
    private readonly string example17CsharpCode = """
private BitResourceTimelineConfig textsConfig = BitResourceTimelineDemoData.Airport(BitResourceTimelineDemoData.Gates(5), labeled: true);
private readonly BitResourceTimelineOptions textsOptions = new() { AutoScrollToNow = true, Locale = "de-DE" };
private readonly BitResourceTimelineTexts germanTexts = new()
{
    AriaLabel = "Belegungsplan der Gates",
    RoleDescription = "Ressourcen-Zeitachse",
    Loading = "Wird geladen…",
    ExpandGroupFormat = "{0} aufklappen",
    CollapseGroupFormat = "{0} zuklappen",
    ResizeResourceColumn = "Breite der Ressourcenspalte ändern",
    BarLocked = "Balken ist gesperrt",
    SelectionCleared = "Auswahl aufgehoben",
    EmptyRowFormat = "{0}, keine Belegungen",
    FocusedBarFormat = "{0}: {1} bis {2}",
    FocusedLabeledBarFormat = "{0}: {1}, {2} bis {3}",
    SelectedBarFormat = "{0}, ausgewählt",
    ResizedFormat = "Größe geändert: {0} bis {1}",
    MovedFormat = "Verschoben: {0} bis {1}",
    MovedToResourceFormat = "Verschoben nach {0}: {1} bis {2}",
    EditRefused = "Änderung abgelehnt",
    EditRefusedOverlap = "Änderung abgelehnt: überschneidet eine andere Belegung",
    GroupCollapsed = "Gruppe zugeklappt",
    GroupExpanded = "Gruppe aufgeklappt",
    CopiedFormat = "{0} kopiert",
    PastedFormat = "{0} eingefügt",
    DeletedFormat = "{0} gelöscht",
    DeleteRefused = "Löschen abgelehnt",
    TimeRangeFormat = "{0} bis {1}",
    HiddenAllocationFormat = "{0} ausgeblendete Belegung",
    HiddenAllocationsFormat = "{0} ausgeblendete Belegungen",
    HiddenAllocationItemFormat = "{0} · {1}",
    MoreHiddenAllocationsFormat = "… und {0} weitere",
};
""";
    private readonly DemoCodeFile[] example17CodeFiles =
    [
        new("BitResourceTimelineDemoData.cs", demoDataCode),
    ];

    private readonly string example18RazorCode = """
<style>
    .small-timeline {
        height: 13rem;
    }

    .custom-class {
        border-radius: 0.5rem;
        box-shadow: 0 0 1rem rgba(232, 89, 12, 0.35);
    }

    .custom-root {
        overflow: hidden;
        border-radius: 0.5rem;
        border: 2px solid #2f9e44;
    }

    .custom-row {
        font-style: italic;
        box-shadow: inset 3px 0 0 #2f9e44;
        background-color: rgba(47, 158, 68, 0.1);
    }
</style>

<div>Style & Class (with CSS variables):</div><br />
<div class="small-timeline">
    <BitResourceTimeline Config="styleConfig"
                         Style="--bit-ResourceTimeline-bar-color: #e8590c; --bit-ResourceTimeline-now-color: #2f9e44; --bit-ResourceTimeline-axis-background: rgba(232, 89, 12, 0.08);"
                         Class="custom-class" />
</div>
<br /><br />
<div>Styles:</div><br />
<div class="small-timeline">
    <BitResourceTimeline Config="styleConfig"
                         Styles="@(new() { Root = "border: 2px solid #9c36b5; border-radius: 0.5rem; overflow: hidden;",
                                           Corner = "display: flex; align-items: center; justify-content: center; color: #9c36b5; font-weight: bold;" })">
        <TopStartContent>Gates</TopStartContent>
    </BitResourceTimeline>
</div>
<br /><br />
<div>Classes:</div><br />
<div class="small-timeline">
    <BitResourceTimeline Config="styleConfig"
                         Classes="@(new() { Root = "custom-root", ResourceRow = "custom-row" })">
        <ResourceTemplate Context="row">@row.Resource.Name</ResourceTemplate>
    </BitResourceTimeline>
</div>
<br /><br />
<div>Options.Colors:</div><br />
<div class="small-timeline">
    <BitResourceTimeline Config="styleConfig" Options="colorsOptions" />
</div>
""";
    private readonly string example18CsharpCode = """
private readonly BitResourceTimelineConfig styleConfig = BitResourceTimelineDemoData.Airport(BitResourceTimelineDemoData.Gates(3), fromDay: 0, toDay: 3, labeled: true);
private readonly BitResourceTimelineOptions colorsOptions = new()
{
    Colors = new()
    {
        Bar = "#9c36b5",
        BarSelected = "#5f3dc4",
        BarSelectedBorder = "#e8590c",
        Now = "#e8590c",
        Grid = "rgba(156, 54, 181, 0.2)",
    },
};
""";
    private readonly DemoCodeFile[] example18CodeFiles =
    [
        new("BitResourceTimelineDemoData.cs", demoDataCode),
    ];



    // The source of BitResourceTimelineDemoData.cs, the data helper every example but the first builds its Config with.
    private const string demoDataCode = """
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
""";
}
