using Bit.Blow;
using Microsoft.AspNetCore.Components.Web;

namespace Boilerplate.Workbench.Components.Pages;

/// <summary>
/// /flows lists the flows of the review; /flows/{id} draws one as a Bit.Blow board of live screens joined by arrows,
/// which dev mode can rearrange, link and label, and anyone can present screen by screen. A sample's arrows open their
/// editor in dev mode too, and a change made there goes into the review's own copy of the sample, which then opens.
/// </summary>
public partial class FlowsPage
{
    private BlowCanvas? canvas;
    // The screen being presented, or -1.
    private int presentIndex = -1;
    private bool isConfirmingDelete;
    // The arrow whose label editor is open.
    private string? editingLinkId;
    // The arrow whose starting element is being picked on its first screen.
    private string? pickingLinkId;
    // The last pick found its screen not loaded yet; the label editor says so.
    private bool isScreenNotLoaded;
    // The review document couldn't be opened, so the board shows a message instead of loading for ever.
    private bool isOpenFailed;
    private string? labelDraft;
    // The arrow whose editor opens again once the copy of the sample a change was made on is shown.
    private string? linkToReopen;
    private readonly List<BlowNode> nodes = [];
    private readonly List<BlowEdge> edges = [];
    // Each screen's 1-based place in the flow, shown on its card.
    private readonly Dictionary<string, int> numbers = [];
    private readonly BlowFitViewOptions fitOptions = new() { Padding = 0.08, MaxZoom = 1 };

    [AutoInject] private WorkbenchReviewSession reviewSession = default!;
    [AutoInject] private BitThemeManager bitThemeManager = default!;
    [AutoInject] private ILogger<FlowsPage> logger = default!;

    [Parameter] public string? FlowId { get; set; }

    /// <summary>The arrow whose editor the board opens with, as the tour opens one.</summary>
    [SupplyParameterFromQuery(Name = WorkbenchUrls.ArrowQueryKey)] public string? ArrowId { get; set; }

    private WorkbenchFlow? Flow => FlowId is null ? null : reviewSession.AllFlows.FirstOrDefault(f => f.Id == FlowId);

    // Only dev mode may change a board, and nobody a sample's.
    private bool CanEdit => reviewSession.CanWrite && Flow is { IsSample: false };

    // A sample's arrows too, as a change to one goes into a copy of the sample.
    private bool CanEditArrows => reviewSession.CanWrite && Flow is not null;

    private WorkbenchFlowLink? EditingLink => editingLinkId is null ? null : Flow?.Links.FirstOrDefault(l => l.Id == editingLinkId);

    private WorkbenchFlowLink? PickingLink => pickingLinkId is null ? null : Flow?.Links.FirstOrDefault(l => l.Id == pickingLinkId);

    private string? CurrentScreenId => Flow is { } flow && presentIndex >= 0 && presentIndex < flow.Screens.Length ? flow.Screens[presentIndex].Id : null;

    private bool IsRtl => CurrentDir is BitDir.Rtl;

    protected override void OnInitialized()
    {
        reviewSession.Changed += OnReviewChanged;
    }

    protected override async Task OnParametersSetAsync()
    {
        // Another flow: whatever was being presented, edited or picked belonged to the previous one.
        presentIndex = -1;
        editingLinkId = null;
        isConfirmingDelete = false;
        isScreenNotLoaded = false;

        if (pickingLinkId is not null)
        {
            pickingLinkId = null;
            await CancelSpotPick();
        }

        Rebuild();
        OpenArrowEditor();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender is false)
            return;

        // The flows live in the review document, which opens only once the page is interactive.
        try
        {
            await reviewSession.Open(Localizer[nameof(WorkbenchStrings.Reviewer)]);
        }
        catch (Exception exception)
        {
            isOpenFailed = true;
            logger.LogError(exception, "The review document could not be opened for the flows page.");
        }

        if (CurrentCancellationToken.IsCancellationRequested)
            return;

        Rebuild();

        // The review's own flows are known only now.
        if (editingLinkId is null)
        {
            OpenArrowEditor();
        }

        StateHasChanged();
    }

    /// <summary>
    /// Opens the editor of the arrow the address names, or of the one a change on a sample was made on, now on its copy,
    /// keeping the label that was being written.
    /// </summary>
    private void OpenArrowEditor()
    {
        var keepsDraft = linkToReopen is not null;
        var linkId = linkToReopen ?? ArrowId;

        linkToReopen = null;

        if (linkId is null || CanEditArrows is false || Flow?.Links.FirstOrDefault(l => l.Id == linkId) is not { } link)
            return;

        editingLinkId = link.Id;
        labelDraft = keepsDraft ? labelDraft : link.Label;
    }

    /// <summary>
    /// Makes a change to the flow shown or, on a sample, to the review's own copy of it, which then opens; with an arrow
    /// given, that arrow's editor opens again on the copy.
    /// </summary>
    private async Task ChangeFlow(Action<string> change, string? reopenLinkId = null)
    {
        if (Flow is not { } flow)
            return;

        if (flow.IsSample is false)
        {
            change(flow.Id);
            return;
        }

        if (await reviewSession.CopySample(flow.Id) is not { } copyId)
            return;

        change(copyId);

        linkToReopen = reopenLinkId;
        NavigationManager.NavigateTo(WorkbenchUrls.Flow(copyId));
    }

    /// <summary>
    /// Brings the board's nodes and edges in line with the flow in the review document, keeping the node objects
    /// Blow already knows so their measured sizes survive and a node being dragged stays where the pointer is.
    /// </summary>
    private void Rebuild()
    {
        if (Flow is not { } flow)
        {
            nodes.Clear();
            edges.Clear();
            return;
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

                // A sample's copy keeps its screens' ids, and with them the sample's nodes, which couldn't be changed.
                node.Draggable = CanEdit;
                node.Connectable = CanEdit;
                node.Deletable = CanEdit;

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
                Draggable = CanEdit,
                Connectable = CanEdit,
                Deletable = CanEdit,
                AriaLabel = TitleOf(screen.PageKey)
            });
        }

        // An arrow with a marked start leaves from the hidden handle drawn at that spot, the others from the card's edge.
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
            Deletable = CanEdit,
            Reconnectable = false
        }));
    }

    /// <summary>Zooms the board to one screen and makes that screen's page usable.</summary>
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

    /// <summary>An arrow drawn between two screens on the board; its label editor opens at once.</summary>
    private void Connect(BlowConnection connection)
    {
        if (FlowId is null || connection.Source is not { } from || connection.Target is not { } to || from == to)
            return;

        editingLinkId = reviewSession.AddLink(FlowId, from, to);
        labelDraft = null;
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
        if (CanEditArrows is false || Flow?.Links.FirstOrDefault(l => l.Id == args.Edge.Id) is not { } link)
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

    private async Task DeleteLink()
    {
        if (editingLinkId is { } linkId)
        {
            await ChangeFlow(flowId => reviewSession.RemoveLink(flowId, linkId));
        }

        CloseLabelEditor();
    }

    /// <summary>
    /// Zooms to the arrow's first screen and waits for a click inside its page; the element clicked becomes the
    /// arrow's starting spot. Cancelling keeps the spot it had.
    /// </summary>
    private async Task PickSpot()
    {
        if (FlowId is null || EditingLink is not { } link)
            return;

        editingLinkId = null;
        pickingLinkId = link.Id;
        isScreenNotLoaded = false;

        if (canvas is not null && nodes.FirstOrDefault(n => n.Id == link.From) is { } node)
        {
            await canvas.FitViewAsync(new BlowFitViewOptions { Nodes = [node], Padding = 0.1, MaxZoom = 1, Duration = 300 });
        }

        StateHasChanged();

        WorkbenchFlowSpot? spot = null;

        try
        {
            // No timeout: the pick waits for the click however long it takes, until the page goes away.
            spot = await JSRuntime.InvokeAsync<WorkbenchFlowSpot?>("workbench.pickSpot", CurrentCancellationToken, [link.From]);
        }
        catch (Exception exception) when (exception is TaskCanceledException or JSDisconnectedException)
        {
            return;
        }
        catch (JSException)
        {
            // The screen's page hadn't loaded yet.
            isScreenNotLoaded = true;
        }

        // Another flow opened, or another pick started, while this one waited.
        if (pickingLinkId != link.Id)
            return;

        pickingLinkId = null;
        editingLinkId = link.Id;

        if (spot is not null)
        {
            await ChangeFlow(flowId => reviewSession.SetLinkSpot(flowId, link.Id, spot), reopenLinkId: link.Id);
        }

        await ShowAll();
    }

    /// <summary>Ends the pick with no spot, which brings back the label editor.</summary>
    private Task CancelPick() => CancelSpotPick();

    /// <summary>Takes the pick's listeners off the screen's page, which settles the pick with no spot.</summary>
    private async Task CancelSpotPick()
    {
        try
        {
            await JSRuntime.InvokeVoidAsync("workbench.cancelSpotPick");
        }
        catch (JSDisconnectedException)
        {
        }
    }

    /// <summary>Escape closes the label editor without saving.</summary>
    private void OnLabelEditorKeyDown(KeyboardEventArgs args)
    {
        if (args.Key is "Escape")
        {
            CloseLabelEditor();
        }
    }

    private void CloseLabelEditor()
    {
        editingLinkId = null;
        isScreenNotLoaded = false;
    }

    private async Task ClearSpot()
    {
        if (editingLinkId is { } linkId)
        {
            await ChangeFlow(flowId => reviewSession.SetLinkSpot(flowId, linkId, null), reopenLinkId: linkId);
        }
    }

    /// <summary>Saves a label that changed; one left as it was changes nothing, and so makes no copy of a sample.</summary>
    private async Task SaveLabel()
    {
        var label = string.IsNullOrWhiteSpace(labelDraft) ? null : labelDraft.Trim();

        if (EditingLink is { } link && label != link.Label)
        {
            await ChangeFlow(flowId => reviewSession.SetLinkLabel(flowId, link.Id, label));
        }

        CloseLabelEditor();
    }

    /// <summary>The first click asks for confirmation on the button itself, the second deletes.</summary>
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

    /// <summary>
    /// A screen's /preview address. Its state is read from the review by the screen's id, so the address never expires
    /// and is the same on every render, and the frame never reloads for a review change.
    /// </summary>
    private static string SourceOf(WorkbenchFlow flow, WorkbenchFlowScreen screen)
    {
        return WorkbenchUrls.Preview(screen.PageKey, screen.Scenario with { Backdrop = null, Caption = null }, snapshotId: $"{PreviewPage.SavedStatePrefix}{flow.Id}{screen.Id}");
    }

    /// <summary>The spot's outline over the screen; the spot is in fractions of the screen's viewport.</summary>
    private static string SpotStyle(WorkbenchFlowSpot spot)
    {
        return FormattableString.Invariant($"left:{spot.X * 100:0.##}%;top:{spot.Y * 100:0.##}%;width:{spot.Width * 100:0.##}%;height:{spot.Height * 100:0.##}%");
    }

    /// <summary>Puts the arrow's hidden source handle on the middle of the spot's right edge.</summary>
    private static string SpotHandleStyle(WorkbenchFlowSpot spot)
    {
        return FormattableString.Invariant($"left:{(spot.X + spot.Width) * 100:0.##}%;right:auto;top:{(spot.Y + spot.Height / 2) * 100:0.##}%");
    }

    /// <summary>Redraws the board for a change from anyone: this browser, another one, or the agent.</summary>
    private void OnReviewChanged()
    {
        InvokeAsync(() =>
        {
            Rebuild();
            StateHasChanged();
        });
    }

    public override async ValueTask DisposeAsync()
    {
        reviewSession.Changed -= OnReviewChanged;

        if (pickingLinkId is not null)
        {
            pickingLinkId = null;
            await CancelSpotPick();
        }

        await base.DisposeAsync();
    }
}
