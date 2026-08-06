namespace Bit.BlazorUI;

/// <summary>
/// Arguments passed to <c>OnDetailToggle</c> when a row's master-detail content is expanded or collapsed.
/// </summary>
/// <typeparam name="TItem">The row item type.</typeparam>
public sealed class BitDataGridDetailEventArgs<TItem>
{
    /// <summary>The row whose detail content was toggled.</summary>
    public required TItem Item { get; init; }

    /// <summary>True when the detail content was expanded, false when it was collapsed.</summary>
    public required bool Expanded { get; init; }
}
