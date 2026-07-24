namespace Bit.BlazorUI;

/// <summary>
/// Arguments passed to the <see cref="BitQuickGrid{T}.RowTemplate"/> render fragment.
/// </summary>
/// <typeparam name="T">The type of data represented by each row in the grid.</typeparam>
public class BitQuickGridRowTemplateArgs<T>
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

/// <summary>
/// Backward-compatible alias for <see cref="BitQuickGridRowTemplateArgs{T}"/>, kept so existing code that
/// stored, returned or accepted the old <c>BitDataGridRowTemplateArgs&lt;T&gt;</c> type keeps compiling. It
/// inherits the renamed type (so instances are accepted anywhere a <see cref="BitQuickGridRowTemplateArgs{T}"/>
/// is expected) and preserves the same public API surface.
/// </summary>
/// <typeparam name="T">The type of data represented by each row in the grid.</typeparam>
[Obsolete("BitDataGridRowTemplateArgs<T> has been renamed to BitQuickGridRowTemplateArgs<T>. Use BitQuickGridRowTemplateArgs<T> instead.")]
public class BitDataGridRowTemplateArgs<T> : BitQuickGridRowTemplateArgs<T>
{
}
