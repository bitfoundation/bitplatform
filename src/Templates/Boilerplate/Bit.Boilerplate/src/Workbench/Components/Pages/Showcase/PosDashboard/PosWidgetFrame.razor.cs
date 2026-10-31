namespace Boilerplate.Workbench.Components.Pages.Showcase.PosDashboard;

public partial class PosWidgetFrame
{
    private List<BitMenuButtonItem> menuItems = [];

    [CascadingParameter] public PosDashboardPage Dashboard { get; set; } = default!;

    [Parameter] public PosWidgetDefinition Definition { get; set; } = default!;

    [Parameter] public PosWidgetLayout Layout { get; set; } = default!;

    [Parameter] public PosWidgetStatus Status { get; set; }

    [Parameter] public string? Subtitle { get; set; }

    [Parameter] public string EmptyTitleKey { get; set; } = nameof(WorkbenchStrings.PosNoDataYet);

    [Parameter] public string? EmptyDescriptionKey { get; set; }

    [Parameter] public string? EmptyIconName { get; set; }

    [Parameter] public EventCallback OnRetry { get; set; }

    [Parameter] public RenderFragment? HeaderContent { get; set; }

    [Parameter] public RenderFragment? Skeleton { get; set; }

    [Parameter] public RenderFragment? ChildContent { get; set; }

    protected override void OnParametersSet()
    {
        menuItems =
        [
            SizeItem(PosWidgetSize.Small, nameof(WorkbenchStrings.PosSizeSmall)),
            SizeItem(PosWidgetSize.Medium, nameof(WorkbenchStrings.PosSizeMedium)),
            SizeItem(PosWidgetSize.Large, nameof(WorkbenchStrings.PosSizeLarge)),
            new() { IsSeparator = true },
            new() { Key = "earlier", Text = Localizer[nameof(WorkbenchStrings.PosMoveEarlier)], IconName = CurrentDir is BitDir.Rtl ? BitIconName.ChevronRight : BitIconName.ChevronLeft, OnClick = item => _ = Dashboard.Move(Layout, -1) },
            new() { Key = "later", Text = Localizer[nameof(WorkbenchStrings.PosMoveLater)], IconName = CurrentDir is BitDir.Rtl ? BitIconName.ChevronLeft : BitIconName.ChevronRight, OnClick = item => _ = Dashboard.Move(Layout, 1) },
            new() { IsSeparator = true },
            new() { Key = "hide", Text = Localizer[nameof(WorkbenchStrings.PosHideWidget)], IconName = BitIconName.Hide3, OnClick = _ => Dashboard.Hide(Layout) }
        ];
    }

    private BitMenuButtonItem SizeItem(PosWidgetSize size, string textKey) => new()
    {
        Key = size.ToString(),
        Text = Localizer[textKey],
        IsChecked = Layout.Size == size,
        Checkable = true,
        OnClick = _ => Dashboard.Resize(Layout, size)
    };
}
