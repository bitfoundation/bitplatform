namespace Bit.BlazorUI;

/// <summary>
/// Arguments passed to the <see cref="BitDataGrid{T}.RowTemplate"/> render fragment.
/// </summary>
/// <typeparam name="T">The type of data represented by each row in the grid.</typeparam>
public class BitDataGridRowTemplateArgs<T>
{
    /// <summary>
    /// A render fragment that produces the original row markup (the default <c>&lt;tr&gt;</c> with all column cells).
    /// Render this in your custom template to include the default row, or omit it to replace entirely.
    /// </summary>
    public required RenderFragment OriginalRow { get; set; }

    /// <summary>
    /// The 1-based row index used for accessibility (e.g. aria-rowindex).
    /// </summary>
    public int RowIndex { get; set; }

    /// <summary>
    /// The data item for this row.
    /// </summary>
    public T RowItem { get; set; } = default!;
}
