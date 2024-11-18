﻿namespace Bit.BlazorUI;

/// <summary>
/// Holds state to represent pagination in a <see cref="BitDataGrid{TGridItem}"/>.
/// </summary>
public class BitDataGridPaginationState
{
    /// <summary>
    /// Gets the current zero-based page index. To set it, call <see cref="SetCurrentPageIndexAsync(int)" />.
    /// </summary>
    public int CurrentPageIndex { get; private set; }

    /// <summary>
    /// Gets or sets the number of items on each page.
    /// </summary>
    public int ItemsPerPage { get; set; } = 10;

    /// <summary>
    /// Gets the zero-based index of the last page, if known. The value will be null until <see cref="TotalItemCount"/> is known.
    /// </summary>
    public int? LastPageIndex => (TotalItemCount - 1) / ItemsPerPage;

    /// <summary>
    /// Gets the total number of items across all pages, if known. The value will be null until an
    /// associated <see cref="BitDataGrid{TGridItem}"/> assigns a value after loading data.
    /// </summary>
    public int? TotalItemCount { get; private set; }

    /// <summary>
    /// An event that is raised when the total item count has changed.
    /// </summary>
    public event EventHandler<int?>? TotalItemCountChanged;

    internal EventCallbackSubscribable<BitDataGridPaginationState> CurrentPageItemsChanged { get; } = new();
    internal EventCallbackSubscribable<BitDataGridPaginationState> TotalItemCountChangedSubscribable { get; } = new();

    /// <inheritdoc />
    public override int GetHashCode()
        => HashCode.Combine(ItemsPerPage, CurrentPageIndex, TotalItemCount);

    /// <summary>
    /// Sets the current page index, and notifies any associated <see cref="BitDataGrid{TGridItem}"/>
    /// to fetch and render updated data.
    /// </summary>
    /// <param name="pageIndex">The new, zero-based page index.</param>
    /// <returns>A <see cref="Task"/> representing the completion of the operation.</returns>
    public Task SetCurrentPageIndexAsync(int pageIndex)
    {
        CurrentPageIndex = pageIndex;
        return CurrentPageItemsChanged.InvokeCallbacksAsync(this);
    }

    // Can be internal because this only needs to be called by BitDataGrid itself, not any custom pagination UI components.
    internal Task SetTotalItemCountAsync(int totalItemCount)
    {
        if (totalItemCount == TotalItemCount)
        {
            return Task.CompletedTask;
        }

        TotalItemCount = totalItemCount;

        if (CurrentPageIndex > 0 && CurrentPageIndex > LastPageIndex)
        {
            // If the number of items has reduced such that the current page index is no longer valid, move
            // automatically to the final valid page index and trigger a further data load.
            return SetCurrentPageIndexAsync(LastPageIndex.Value);
        }
        else
        {
            // Under normal circumstances, we just want any associated pagination UI to update
            TotalItemCountChanged?.Invoke(this, TotalItemCount);
            return TotalItemCountChangedSubscribable.InvokeCallbacksAsync(this);
        }
    }
}
