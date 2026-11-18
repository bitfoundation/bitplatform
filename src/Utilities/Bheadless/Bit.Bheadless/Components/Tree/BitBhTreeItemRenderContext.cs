namespace Bit.Bheadless;

/// <summary>Render-fragment context exposed by <see cref="BitBhTreeItem{TValue}"/>.</summary>
/// <typeparam name="TValue">The item value type.</typeparam>
public sealed record BitBhTreeItemRenderContext<TValue>
{
    /// <summary>The item's value.</summary>
    public required TValue Value { get; init; }

    /// <summary>Depth, starting at 1.</summary>
    public required int Level { get; init; }

    /// <summary>Whether the item has (or may lazily load) children.</summary>
    public required bool IsExpandable { get; init; }

    /// <summary>Whether the item is expanded.</summary>
    public required bool IsExpanded { get; init; }

    /// <summary>Whether the item is selected.</summary>
    public required bool IsSelected { get; init; }

    /// <summary>Checkbox state: true, false or null (mixed). Only meaningful with checkboxes.</summary>
    public required bool? Checked { get; init; }

    /// <summary>Whether the item has keyboard focus (tab stop).</summary>
    public required bool IsFocused { get; init; }

    /// <summary>Whether the item is disabled.</summary>
    public required bool Disabled { get; init; }

    /// <summary>Whether children are being loaded.</summary>
    public required bool IsLoading { get; init; }

    /// <summary>Toggles expansion.</summary>
    public required Func<Task> ToggleExpanded { get; init; }
}
