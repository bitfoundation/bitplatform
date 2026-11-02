namespace Boilerplate.Workbench.Components.Pages.Showcases.PosDashboard;

/// <summary>
/// The card around every widget: a header with its icon, title and subtitle, and a body that shows the skeleton, the
/// content, the empty state or the error with Retry. In edit mode it adds a drag handle and a menu to resize, move or hide.
/// </summary>
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

    /// <summary>The empty state's icon; the widget's own icon when not set.</summary>
    [Parameter] public string? EmptyIconName { get; set; }

    [Parameter] public EventCallback OnRetry { get; set; }

    /// <summary>Shown at the end of the header outside edit mode, where the edit menu goes in edit mode.</summary>
    [Parameter] public RenderFragment? HeaderContent { get; set; }

    /// <summary>The loading placeholder, shaped like the content; generic shimmer lines when not set.</summary>
    [Parameter] public RenderFragment? Skeleton { get; set; }

    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>Rebuilds the edit menu, so its checked size follows the layout and its move arrows the reading direction.</summary>
    protected override void OnParametersSet()
    {
        menuItems =
        [
            SizeItem(PosWidgetSize.Small, nameof(WorkbenchStrings.PosSizeSmall)),
            SizeItem(PosWidgetSize.Medium, nameof(WorkbenchStrings.PosSizeMedium)),
            SizeItem(PosWidgetSize.Large, nameof(WorkbenchStrings.PosSizeLarge)),
            new() { IsSeparator = true },
            new() { Key = "earlier", Text = Localizer[nameof(WorkbenchStrings.PosMoveEarlier)], IconName = CurrentDir is BitDir.Rtl ? BitIconName.ChevronRight : BitIconName.ChevronLeft },
            new() { Key = "later", Text = Localizer[nameof(WorkbenchStrings.PosMoveLater)], IconName = CurrentDir is BitDir.Rtl ? BitIconName.ChevronLeft : BitIconName.ChevronRight },
            new() { IsSeparator = true },
            new() { Key = "hide", Text = Localizer[nameof(WorkbenchStrings.PosHideWidget)], IconName = BitIconName.Hide3 }
        ];
    }

    /// <summary>Runs the edit menu's command: a size, a move or hiding the widget.</summary>
    private async Task OnMenuClick(BitMenuButtonItem? item)
    {
        switch (item?.Key)
        {
            case "earlier":
                await Dashboard.Move(Layout, -1);
                break;
            case "later":
                await Dashboard.Move(Layout, 1);
                break;
            case "hide":
                Dashboard.Hide(Layout);
                break;
            default:
                if (Enum.TryParse<PosWidgetSize>(item?.Key, out var size))
                {
                    Dashboard.Resize(Layout, size);
                }
                break;
        }
    }

    /// <summary>A checkable menu item that gives the widget this size.</summary>
    private BitMenuButtonItem SizeItem(PosWidgetSize size, string textKey) => new()
    {
        Key = size.ToString(),
        Text = Localizer[textKey],
        IsChecked = Layout.Size == size,
        Checkable = true
    };
}
