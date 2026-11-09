namespace Boilerplate.Workbench.Components.Pages.Showcases.Theme.Sections;

/// <summary>
/// The orders table the Extras section shows BitDataGrid with: number, customer, date, total and status, every column
/// sortable, in the state its parameters ask for.
/// </summary>
public partial class ExtrasOrdersGrid
{
    private const string TotalColumnId = "total";

    private BitDataGrid<ExtrasOrder>? grid;

    [Parameter, EditorRequired] public IEnumerable<ExtrasOrder> Items { get; set; } = [];

    [Parameter] public bool Loading { get; set; }

    [Parameter] public BitSelectionMode SelectionMode { get; set; } = BitSelectionMode.None;

    [Parameter] public IReadOnlyList<ExtrasOrder>? SelectedItems { get; set; }

    /// <summary>Sorts the grid by its total, highest first, once it is on the screen, so its header shows the sort.</summary>
    [Parameter] public bool SortedByTotal { get; set; }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender is false || SortedByTotal is false || grid is null)
            return;

        await grid.SortByAsync(TotalColumnId, BitDataGridSortDirection.Descending);
    }

    private string StatusText(ExtrasOrderStatus status) => Localizer[status switch
    {
        ExtrasOrderStatus.Paid => nameof(WorkbenchStrings.PosOrderStatusPaid),
        ExtrasOrderStatus.Pending => nameof(WorkbenchStrings.PosOrderStatusPending),
        _ => nameof(WorkbenchStrings.PosOrderStatusRefunded)
    }];

    private static BitColor StatusColor(ExtrasOrderStatus status) => status switch
    {
        ExtrasOrderStatus.Paid => BitColor.Success,
        ExtrasOrderStatus.Pending => BitColor.Warning,
        _ => BitColor.Error
    };
}
