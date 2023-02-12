namespace Bit.BlazorUI;

public partial class BitBasicList<TItem>
{
    protected override bool UseVisual => false;


    /// <summary>
    /// Enables virtualization in rendering the list.
    /// </summary>
    [Parameter] public bool EnableVirtualization { get; set; } = false;

    /// <summary>
    /// Gets or sets the list of items to render.
    /// </summary>
    [Parameter] public ICollection<TItem> Items { get; set; } = Array.Empty<TItem>();

    /// <summary>
    /// Gets the size of each item in pixels. Defaults to 50px.
    /// </summary>
    [Parameter] public float ItemSize { get; set; } = 50f;

    /// <summary>
    /// Gets or sets a value that determines how many additional items will be rendered before and after the visible region.
    /// </summary>
    [Parameter] public int OverscanCount { get; set; } = 3;

    /// <summary>
    /// Gets or set the role attribute of the BasicList html element.
    /// </summary>
    [Parameter] public string Role { get; set; } = "list";

    /// <summary>
    /// Gets or sets the Template to render each row.
    /// </summary>
    [Parameter] public RenderFragment<TItem> RowTemplate { get; set; } = default!;


    protected override string RootElementClass => "bit-bsl";
}
