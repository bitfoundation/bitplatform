namespace Bit.Bortable.Demo.Client.Pages;

/// <summary>A tree node used by the nested sortable demo.</summary>
public sealed class NestedItem
{
    public required string Name { get; set; }
    public List<NestedItem> Children { get; set; } = new();
}
