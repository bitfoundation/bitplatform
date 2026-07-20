namespace Bit.BlazorUI.Legacy;

/// <summary>
/// A component that provides a user interface for <see cref="BitDataGridLegacyPaginationState"/>.
/// </summary>
public partial class BitDataGridLegacyPaginator : IDisposable
{
    private readonly EventCallbackSubscriber<BitDataGridLegacyPaginationState> _totalItemCountChanged;

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
    [Parameter] public Func<BitDataGridLegacyPaginationState, string>? SummaryFormat { get; set; }

    /// <summary>
    /// Optionally supplies a template for rendering the page count summary.
    /// </summary>
    [Parameter] public RenderFragment<BitDataGridLegacyPaginationState>? SummaryTemplate { get; set; }

    /// <summary>
    /// The optional custom format for the main text of the paginator in the middle of it.
    /// </summary>
    [Parameter] public Func<BitDataGridLegacyPaginationState, string>? TextFormat { get; set; }

    /// <summary>
    /// The optional custom template for the main text of the paginator in the middle of it.
    /// </summary>
    [Parameter] public RenderFragment<BitDataGridLegacyPaginationState>? TextTemplate { get; set; }

    /// <summary>
    /// Specifies the associated <see cref="BitDataGridLegacyPaginationState"/>. This parameter is required.
    /// </summary>
    [Parameter, EditorRequired] public BitDataGridLegacyPaginationState Value { get; set; } = default!;

    /// <summary>
    /// Constructs an instance of <see cref="BitDataGridLegacyPaginator" />.
    /// </summary>
    public BitDataGridLegacyPaginator()
    {
        // The "total item count" handler doesn't need to do anything except cause this component to
        // re-render. Invoking this EventCallback already routes through the paginator's
        // IHandleEvent.HandleEventAsync (the receiver is `this`), which re-renders the component on its
        // own, so the callback body is intentionally empty - calling StateHasChanged() here as well
        // would queue a second, redundant render.
        _totalItemCountChanged = new(EventCallback.Factory.Create<BitDataGridLegacyPaginationState>(this, () => { }));
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
