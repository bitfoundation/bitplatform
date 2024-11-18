namespace Bit.BlazorUI;

/// <summary>
/// A component that provides a user interface for <see cref="BitDataGridPaginationState"/>.
/// </summary>
public partial class BitDataGridPaginator : IDisposable
{
    private readonly EventCallbackSubscriber<BitDataGridPaginationState> _totalItemCountChanged;

    /// <summary>
    /// The title of the go to first page button.
    /// </summary>
    [Parameter] public string GoToFirstButtonTitle { get; set; } = "Go to first page";

    /// <summary>
    /// The title of the go to previous page button.
    /// </summary>
    [Parameter] public string GoToPrevButtonTitle { get; set; } = "Go to previous page";

    /// <summary>
    /// The title of the go to next page button.
    /// </summary>
    [Parameter] public string GoToNextButtonTitle { get; set; } = "Go to next page";

    /// <summary>
    /// The title of the go to last page button.
    /// </summary>
    [Parameter] public string GoToLastButtonTitle { get; set; } = "Go to last page";

    /// <summary>
    /// Optionally supplies a format for rendering the page count summary.
    /// </summary>
    [Parameter] public Func<BitDataGridPaginationState, string>? SummaryFormat { get; set; }

    /// <summary>
    /// Optionally supplies a template for rendering the page count summary.
    /// </summary>
    [Parameter] public RenderFragment<BitDataGridPaginationState>? SummaryTemplate { get; set; }

    /// <summary>
    /// The optional custom format for the main text of the paginator in the middle of it.
    /// </summary>
    [Parameter] public Func<BitDataGridPaginationState, string>? TextFormat { get; set; }

    /// <summary>
    /// The optional custom template for the main text of the paginator in the middle of it.
    /// </summary>
    [Parameter] public RenderFragment<BitDataGridPaginationState>? TextTemplate { get; set; }

    /// <summary>
    /// Specifies the associated <see cref="BitDataGridPaginationState"/>. This parameter is required.
    /// </summary>
    [Parameter, EditorRequired] public BitDataGridPaginationState Value { get; set; } = default!;

    /// <summary>
    /// Constructs an instance of <see cref="BitDataGridPaginator" />.
    /// </summary>
    public BitDataGridPaginator()
    {
        // The "total item count" handler doesn't need to do anything except cause this component to re-render
        _totalItemCountChanged = new(new EventCallback<BitDataGridPaginationState>(this, null));
    }

    private Task GoFirstAsync() => GoToPageAsync(0);
    private Task GoPreviousAsync() => GoToPageAsync(Value.CurrentPageIndex - 1);
    private Task GoNextAsync() => GoToPageAsync(Value.CurrentPageIndex + 1);
    private Task GoLastAsync() => GoToPageAsync(Value.LastPageIndex.GetValueOrDefault(0));

    private bool CanGoBack => Value.CurrentPageIndex > 0;
    private bool CanGoForwards => Value.CurrentPageIndex < Value.LastPageIndex;

    private Task GoToPageAsync(int pageIndex)
        => Value.SetCurrentPageIndexAsync(pageIndex);

    /// <inheritdoc />
    protected override void OnParametersSet()
        => _totalItemCountChanged.SubscribeOrMove(Value.TotalItemCountChangedSubscribable);

    /// <inheritdoc />
    public void Dispose()
        => _totalItemCountChanged.Dispose();
}
