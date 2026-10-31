using Bit.Blow;

namespace Boilerplate.Workbench.Components.Pages;

public partial class FlowsPage
{
    private BlowCanvas? canvas;
    private int presentIndex = -1;
    private bool isConfirmingDelete;
    private string? editingLinkId;
    private string? labelDraft;
    private readonly List<BlowNode> nodes = [];
    private readonly List<BlowEdge> edges = [];
    private readonly Dictionary<string, string> sources = [];
    private readonly Dictionary<string, int> numbers = [];
    private readonly BlowFitViewOptions fitOptions = new() { Padding = 0.08, MaxZoom = 1 };

    [AutoInject] private WorkbenchReviewSession reviewSession = default!;
    [AutoInject] private WorkbenchReviewAttachments attachments = default!;
    [AutoInject] private WorkbenchSnapshots snapshots = default!;
    [AutoInject] private BitThemeManager bitThemeManager = default!;

    [Parameter] public string? FlowId { get; set; }

    private WorkbenchFlow? Flow => FlowId is null ? null : reviewSession.Flows.FirstOrDefault(f => f.Id == FlowId);

    private string? CurrentScreenId => Flow is { } flow && presentIndex >= 0 && presentIndex < flow.Screens.Length ? flow.Screens[presentIndex].Id : null;

    protected override void OnInitialized()
    {
        reviewSession.Changed += OnReviewChanged;
    }

    protected override async Task OnParametersSetAsync()
    {
        presentIndex = -1;
        editingLinkId = null;
        isConfirmingDelete = false;

        await Rebuild();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender is false)
            return;

        await reviewSession.Open(Localizer[nameof(WorkbenchStrings.Reviewer)]);
        await Rebuild();

        StateHasChanged();
    }

    private async Task Rebuild()
    {
        if (Flow is not { } flow)
        {
            nodes.Clear();
            edges.Clear();
            return;
        }

        foreach (var screen in flow.Screens.Where(s => sources.ContainsKey(s.Id) is false))
        {
            var state = screen.HasState ? await attachments.ReadState($"{flow.Id}{screen.Id}") : null;
            var snapshotId = snapshots.Add(state ?? "{}");

            sources[screen.Id] = WorkbenchUrls.Preview(screen.PageKey, screen.Scenario with { Backdrop = null, Caption = null }, snapshotId: snapshotId);
        }

        numbers.Clear();

        for (var index = 0; index < flow.Screens.Length; index++)
        {
            numbers[flow.Screens[index].Id] = index + 1;
        }

        var existing = nodes.ToDictionary(n => n.Id);

        nodes.RemoveAll(n => numbers.ContainsKey(n.Id) is false);

        foreach (var screen in flow.Screens)
        {
            if (existing.TryGetValue(screen.Id, out var node))
            {
                node.Data = screen;

                if (node.Dragging is false)
                {
                    node.Position = new(screen.X, screen.Y);
                }

                continue;
            }

            nodes.Add(new()
            {
                Id = screen.Id,
                Type = "screen",
                Data = screen,
                Position = new(screen.X, screen.Y),
                Draggable = reviewSession.CanWrite,
                Connectable = reviewSession.CanWrite,
                Deletable = reviewSession.CanWrite,
                AriaLabel = TitleOf(screen.PageKey)
            });
        }

        edges.Clear();
        edges.AddRange(flow.Links.Where(link => numbers.ContainsKey(link.From) && numbers.ContainsKey(link.To)).Select(link => new BlowEdge
        {
            Id = link.Id,
            Source = link.From,
            Target = link.To,
            SourceHandle = link.Spot is null ? "out" : link.Id,
            TargetHandle = "in",
            Label = link.Label,
            StrokeWidth = 4,
            MarkerWidth = 22,
            MarkerHeight = 22,
            LabelStyle = "font-size:30px;font-weight:600;fill:var(--bit-clr-fg-pri)",
            LabelBgStyle = "fill:var(--bit-clr-bg-pri);stroke:var(--bit-clr-brd-sec)",
            LabelBgPaddingX = 16,
            LabelBgPaddingY = 8,
            LabelBgBorderRadius = 14,
            Deletable = reviewSession.CanWrite,
            Reconnectable = false
        }));
    }

    private async Task Present(int index)
    {
        if (Flow is not { Screens.Length: > 0 } flow)
            return;

        presentIndex = Math.Clamp(index, 0, flow.Screens.Length - 1);

        if (canvas is not null && nodes.FirstOrDefault(n => n.Id == flow.Screens[presentIndex].Id) is { } node)
        {
            await canvas.FitViewAsync(new BlowFitViewOptions { Nodes = [node], Padding = 0.1, MaxZoom = 1, Duration = 450 });
        }
    }

    private Task TogglePresent() => presentIndex >= 0 ? StopPresenting() : Present(0);

    private async Task StopPresenting()
    {
        presentIndex = -1;

        await ShowAll();
    }

    private async Task ShowAll()
    {
        if (canvas is not null)
        {
            await canvas.FitViewAsync(new BlowFitViewOptions { Padding = 0.08, MaxZoom = 1, Duration = 450 });
        }
    }

    private void MoveScreen(BlowNode node)
    {
        if (FlowId is not null)
        {
            reviewSession.MoveScreen(FlowId, node.Id, node.Position.X, node.Position.Y);
        }
    }

    private void Connect(BlowConnection connection)
    {
        if (FlowId is null || connection.Source is not { } from || connection.Target is not { } to || from == to)
            return;

        reviewSession.AddLink(FlowId, from, to);
    }

    private void RemoveScreens(IReadOnlyList<BlowNode> removed)
    {
        foreach (var node in removed)
        {
            reviewSession.RemoveScreen(FlowId!, node.Id);
        }
    }

    private void RemoveLinks(IReadOnlyList<BlowEdge> removed)
    {
        foreach (var edge in removed)
        {
            reviewSession.RemoveLink(FlowId!, edge.Id);
        }
    }

    private void EditLabel(BlowEdgeMouseEvent args)
    {
        if (reviewSession.CanWrite is false || Flow?.Links.FirstOrDefault(l => l.Id == args.Edge.Id) is not { } link)
            return;

        editingLinkId = link.Id;
        labelDraft = link.Label;
    }

    private Task PresentScreen(BlowNodeMouseEvent args)
    {
        return Flow is { } flow && Array.FindIndex(flow.Screens, s => s.Id == args.Node.Id) is var index and >= 0
            ? Present(index)
            : Task.CompletedTask;
    }

    private void RemoveScreen(string screenId)
    {
        if (FlowId is not null)
        {
            reviewSession.RemoveScreen(FlowId, screenId);
        }
    }

    private void DeleteLink()
    {
        if (FlowId is not null && editingLinkId is not null)
        {
            reviewSession.RemoveLink(FlowId, editingLinkId);
        }

        editingLinkId = null;
    }

    private void SaveLabel()
    {
        if (FlowId is not null && editingLinkId is not null)
        {
            reviewSession.SetLinkLabel(FlowId, editingLinkId, labelDraft);
        }

        editingLinkId = null;
    }

    private void DeleteFlow()
    {
        if (FlowId is null)
            return;

        if (isConfirmingDelete is false)
        {
            isConfirmingDelete = true;
            return;
        }

        reviewSession.DeleteFlow(FlowId);
        NavigationManager.NavigateTo(WorkbenchUrls.Flows);
    }

    private async Task ToggleShellTheme()
    {
        await bitThemeManager.ToggleDarkLightAsync();
    }

    private string TitleOf(string pageKey) => WorkbenchPageCatalog.Find(pageKey) is { } page ? Localizer[page.TitleKey].Value : pageKey;

    private static string SpotStyle(WorkbenchFlowSpot spot)
    {
        return FormattableString.Invariant($"left:{spot.X * 100:0.##}%;top:{spot.Y * 100:0.##}%;width:{spot.Width * 100:0.##}%;height:{spot.Height * 100:0.##}%");
    }

    private static string SpotHandleStyle(WorkbenchFlowSpot spot)
    {
        return FormattableString.Invariant($"left:{(spot.X + spot.Width) * 100:0.##}%;right:auto;top:{(spot.Y + spot.Height / 2) * 100:0.##}%");
    }

    private void OnReviewChanged()
    {
        InvokeAsync(async () =>
        {
            await Rebuild();
            StateHasChanged();
        });
    }

    public override async ValueTask DisposeAsync()
    {
        reviewSession.Changed -= OnReviewChanged;

        await base.DisposeAsync();
    }
}
