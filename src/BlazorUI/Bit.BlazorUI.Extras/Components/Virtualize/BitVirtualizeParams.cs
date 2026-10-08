namespace Bit.BlazorUI;

/// <summary>
/// The parameters for <see cref="BitVirtualize{TItem}"/> component.
/// </summary>
/// <remarks>
/// What belongs here is what the lists of an application agree on: how their items are sized, spaced and measured, how
/// much they render ahead, the roles they expose, the templates of their loading, empty and placeholder states, and how they
/// are styled. The data, the templates typed over the items, the keys, the item attributes, the sticky predicate, the events and the layout
/// of one list (Horizontal, Lanes, MinLaneSize, Reversed, AlignToEnd, InitialIndex, ScrollerSelector, its header and footer) are left out on purpose:
/// they are what makes one list the one it is.
/// <br />
/// None of the parameters here depends on the type of the items, so the class is not generic: one instance
/// reaches every list under the <see cref="BitParams"/> it is given to, whatever it lists.
/// </remarks>
public class BitVirtualizeParams : BitComponentBaseParams, IBitComponentParams
{
    /// <summary>
    /// Represents the parameter name used to identify the <see cref="BitVirtualize{TItem}"/> cascading parameters within <see cref="BitParams"/>.
    /// </summary>
    /// <remarks>
    /// This constant is typically used when referencing or accessing the BitVirtualize value in
    /// parameterized APIs or configuration settings. Using this constant helps ensure consistency and reduces the risk
    /// of typographical errors.
    /// </remarks>
    public const string ParamName = $"{nameof(BitParams)}.{nameof(BitVirtualize<object>)}";



    public string Name => ParamName;



    /// <summary>
    /// Custom CSS classes for different parts of the virtualize.
    /// </summary>
    public BitVirtualizeClassStyles? Classes { get; set; }

    /// <summary>
    /// Enables dynamic item sizing in which each rendered item gets measured in the browser.
    /// </summary>
    public bool? Dynamic { get; set; }

    /// <summary>
    /// The custom template to render when there is no item available.
    /// </summary>
    public RenderFragment? EmptyTemplate { get; set; }

    /// <summary>
    /// The assumed size in pixels of the items that have not been measured yet in dynamic mode.
    /// </summary>
    public float? EstimatedItemSize { get; set; }

    /// <summary>
    /// The space in pixels between consecutive items along the scroll axis, and between the lanes of a grid.
    /// </summary>
    public float? Gap { get; set; }

    /// <summary>
    /// The ARIA role of each item element.
    /// </summary>
    public string? ItemRole { get; set; }

    /// <summary>
    /// The size in pixels of each item (each row of items with Lanes) along the scroll axis when the Dynamic mode is off.
    /// </summary>
    public float? ItemSize { get; set; }

    /// <summary>
    /// The custom template to render until the component has performed its first load.
    /// </summary>
    public RenderFragment? LoadingTemplate { get; set; }

    /// <summary>
    /// The number of extra items to render on each side of the visible window for smoother scrolling.
    /// </summary>
    public int? OverscanCount { get; set; }

    /// <summary>
    /// The custom template to render an item whose data has not been loaded yet in provider mode.
    /// </summary>
    public RenderFragment<BitVirtualizePlaceholderContext>? PlaceholderTemplate { get; set; }

    /// <summary>
    /// The number of items away from an edge the visible window must be before OnEndReached/OnStartReached fire.
    /// </summary>
    public int? ReachedThreshold { get; set; }

    /// <summary>
    /// The ARIA role of the root element.
    /// </summary>
    public string? Role { get; set; }

    /// <summary>
    /// Custom CSS styles for different parts of the virtualize.
    /// </summary>
    public BitVirtualizeClassStyles? Styles { get; set; }



    /// <summary>
    /// Updates the properties of the specified <see cref="BitVirtualize{TItem}"/> instance with any values that have been set on
    /// this object, if those properties have not already been set on the <see cref="BitVirtualize{TItem}"/> itself.
    /// </summary>
    /// <remarks>
    /// Only properties that have a value set and have not already been set on the <paramref name="bitVirtualize"/> will be updated.
    /// This method does not overwrite existing values on <paramref name="bitVirtualize"/>.
    /// </remarks>
    /// <param name="bitVirtualize">
    /// The <see cref="BitVirtualize{TItem}"/> instance whose properties will be updated. Cannot be null.
    /// </param>
    public void UpdateParameters<TItem>(BitVirtualize<TItem> bitVirtualize)
    {
        if (bitVirtualize is null) return;

        UpdateBaseParameters(bitVirtualize);

        if (Classes is not null)
        {
            bitVirtualize.TakeFromCascade(nameof(Classes), Classes, static l => l.Classes, static (l, v) => l.Classes = v);
        }

        if (Dynamic.HasValue)
        {
            bitVirtualize.TakeFromCascade(nameof(Dynamic), Dynamic.Value, static l => l.Dynamic, static (l, v) => l.Dynamic = v);
        }

        if (EmptyTemplate is not null)
        {
            bitVirtualize.TakeFromCascade(nameof(EmptyTemplate), EmptyTemplate, static l => l.EmptyTemplate, static (l, v) => l.EmptyTemplate = v);
        }

        if (EstimatedItemSize.HasValue)
        {
            bitVirtualize.TakeFromCascade(nameof(EstimatedItemSize), EstimatedItemSize.Value, static l => l.EstimatedItemSize, static (l, v) => l.EstimatedItemSize = v);
        }

        if (Gap.HasValue)
        {
            bitVirtualize.TakeFromCascade(nameof(Gap), Gap.Value, static l => l.Gap, static (l, v) => l.Gap = v);
        }

        if (ItemRole is not null)
        {
            bitVirtualize.TakeFromCascade(nameof(ItemRole), ItemRole, static l => l.ItemRole, static (l, v) => l.ItemRole = v);
        }

        if (ItemSize.HasValue)
        {
            bitVirtualize.TakeFromCascade(nameof(ItemSize), ItemSize.Value, static l => l.ItemSize, static (l, v) => l.ItemSize = v);
        }

        if (LoadingTemplate is not null)
        {
            bitVirtualize.TakeFromCascade(nameof(LoadingTemplate), LoadingTemplate, static l => l.LoadingTemplate, static (l, v) => l.LoadingTemplate = v);
        }

        if (OverscanCount.HasValue)
        {
            bitVirtualize.TakeFromCascade(nameof(OverscanCount), OverscanCount.Value, static l => l.OverscanCount, static (l, v) => l.OverscanCount = v);
        }

        if (PlaceholderTemplate is not null)
        {
            bitVirtualize.TakeFromCascade(nameof(PlaceholderTemplate), PlaceholderTemplate, static l => l.PlaceholderTemplate, static (l, v) => l.PlaceholderTemplate = v);
        }

        if (ReachedThreshold.HasValue)
        {
            bitVirtualize.TakeFromCascade(nameof(ReachedThreshold), ReachedThreshold.Value, static l => l.ReachedThreshold, static (l, v) => l.ReachedThreshold = v);
        }

        if (Role is not null)
        {
            bitVirtualize.TakeFromCascade(nameof(Role), Role, static l => l.Role, static (l, v) => l.Role = v);
        }

        if (Styles is not null)
        {
            bitVirtualize.TakeFromCascade(nameof(Styles), Styles, static l => l.Styles, static (l, v) => l.Styles = v);
        }
    }
}
