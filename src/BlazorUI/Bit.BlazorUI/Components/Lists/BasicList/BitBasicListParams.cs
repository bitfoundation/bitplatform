namespace Bit.BlazorUI;

/// <summary>
/// The parameters for <see cref="BitBasicList{TItem}"/> component.
/// </summary>
/// <remarks>
/// The item source (Items and ItemsProvider), the Loading state, the templates and the event callbacks are
/// left out on purpose: they belong to a single list rather than to a group of them.
/// <br />
/// None of the parameters here depends on the type of the items, so the class is not generic: one instance
/// reaches every list under the <see cref="BitParams"/> it is given to, whatever the type of its items.
/// </remarks>
public class BitBasicListParams : BitComponentBaseParams, IBitComponentParams
{
    /// <summary>
    /// Represents the parameter name used to identify the <see cref="BitBasicList{TItem}"/> cascading parameters within <see cref="BitParams"/>.
    /// </summary>
    /// <remarks>
    /// This constant is typically used when referencing or accessing the BitBasicList value in
    /// parameterized APIs or configuration settings. Using this constant helps ensure consistency and reduces the risk
    /// of typographical errors.
    /// </remarks>
    public const string ParamName = $"{nameof(BitParams)}.{nameof(BitBasicList<object>)}";



    public string Name => ParamName;



    /// <summary>
    /// Loads the next page as soon as the end of the loaded items scrolls into view, turning the LoadMore button into an infinite scrolling list. Only effective while <see cref="LoadMore"/> is enabled.
    /// </summary>
    public bool? AutoLoad { get; set; }

    /// <summary>
    /// How many pixels before the end of the loaded items the next page starts loading in <see cref="AutoLoad"/> mode.
    /// </summary>
    public int? AutoLoadThreshold { get; set; }

    /// <summary>
    /// Custom CSS classes for different parts of the list.
    /// </summary>
    public BitBasicListClassStyles? Classes { get; set; }

    /// <summary>
    /// Sets the height of the list to fit its content.
    /// </summary>
    public bool? FitHeight { get; set; }

    /// <summary>
    /// Sets the width and height of the list to fit its content.
    /// </summary>
    public bool? FitSize { get; set; }

    /// <summary>
    /// Sets the width of the list to fit its content.
    /// </summary>
    public bool? FitWidth { get; set; }

    /// <summary>
    /// Sets the height of the list to 100%.
    /// </summary>
    public bool? FullHeight { get; set; }

    /// <summary>
    /// Sets the width and height of the list to 100%.
    /// </summary>
    public bool? FullSize { get; set; }

    /// <summary>
    /// Sets the width of the list to 100%.
    /// </summary>
    public bool? FullWidth { get; set; }

    /// <summary>
    /// Lays the items of the list out in a row and scrolls it sideways instead of down. Ignored while <see cref="Virtualize"/> is enabled.
    /// </summary>
    public bool? Horizontal { get; set; }

    /// <summary>
    /// Size of each item in pixels, which the Virtualize mode calculates the scroll range and the number of rows to render from.
    /// </summary>
    public float? ItemSize { get; set; }

    /// <summary>
    /// The number of milliseconds the list waits before calling its items provider in Virtualize mode.
    /// </summary>
    public int? ItemsProviderDelay { get; set; }

    /// <summary>
    /// The text shown next to the default spinners and announced to screen readers while the list loads.
    /// </summary>
    public string? LoadingLabel { get; set; }

    /// <summary>
    /// Enables the LoadMore mode for the list.
    /// </summary>
    public bool? LoadMore { get; set; }

    /// <summary>
    /// The number of items to be loaded and rendered after the LoadMore button is clicked.
    /// </summary>
    public int? LoadMoreSize { get; set; }

    /// <summary>
    /// The custom text of the default LoadMore button.
    /// </summary>
    public string? LoadMoreText { get; set; }

    /// <summary>
    /// How many additional items are rendered before and after the visible region in Virtualize mode.
    /// </summary>
    public int? OverscanCount { get; set; }

    /// <summary>
    /// The role attribute of the element holding the rows of the list.
    /// </summary>
    public string? Role { get; set; }

    /// <summary>
    /// Custom CSS styles for different parts of the list.
    /// </summary>
    public BitBasicListClassStyles? Styles { get; set; }

    /// <summary>
    /// Enables virtualization in rendering the list.
    /// </summary>
    public bool? Virtualize { get; set; }



    /// <summary>
    /// Updates the properties of the specified <see cref="BitBasicList{TItem}"/> instance with any values that have been set on
    /// this object, if those properties have not already been set on the <see cref="BitBasicList{TItem}"/>.
    /// </summary>
    /// <remarks>
    /// Only properties that have a value set and have not already been set on the <paramref name="bitBasicList"/> will be updated.
    /// This method does not overwrite existing values on <paramref name="bitBasicList"/>.
    /// </remarks>
    /// <typeparam name="TItem">The type of the items of the list.</typeparam>
    /// <param name="bitBasicList">
    /// The <see cref="BitBasicList{TItem}"/> instance whose properties will be updated. Cannot be null.
    /// </param>
    public void UpdateParameters<TItem>(BitBasicList<TItem> bitBasicList)
    {
        if (bitBasicList is null) return;

        UpdateBaseParameters(bitBasicList);

        if (AutoLoad.HasValue)
        {
            bitBasicList.TakeFromCascade(nameof(AutoLoad), AutoLoad.Value, static b => b.AutoLoad, static (b, v) => b.AutoLoad = v);
        }

        if (AutoLoadThreshold.HasValue)
        {
            bitBasicList.TakeFromCascade(nameof(AutoLoadThreshold), AutoLoadThreshold.Value, static b => b.AutoLoadThreshold, static (b, v) => b.AutoLoadThreshold = v);
        }

        if (Classes is not null)
        {
            bitBasicList.TakeFromCascade(nameof(Classes), Classes, static b => b.Classes, static (b, v) => b.Classes = v);
        }

        if (FitHeight.HasValue)
        {
            bitBasicList.TakeFromCascade(nameof(FitHeight), FitHeight.Value, static b => b.FitHeight, static (b, v) => b.FitHeight = v);
        }

        if (FitSize.HasValue)
        {
            bitBasicList.TakeFromCascade(nameof(FitSize), FitSize.Value, static b => b.FitSize, static (b, v) => b.FitSize = v);
        }

        if (FitWidth.HasValue)
        {
            bitBasicList.TakeFromCascade(nameof(FitWidth), FitWidth.Value, static b => b.FitWidth, static (b, v) => b.FitWidth = v);
        }

        if (FullHeight.HasValue)
        {
            bitBasicList.TakeFromCascade(nameof(FullHeight), FullHeight.Value, static b => b.FullHeight, static (b, v) => b.FullHeight = v);
        }

        if (FullSize.HasValue)
        {
            bitBasicList.TakeFromCascade(nameof(FullSize), FullSize.Value, static b => b.FullSize, static (b, v) => b.FullSize = v);
        }

        if (FullWidth.HasValue)
        {
            bitBasicList.TakeFromCascade(nameof(FullWidth), FullWidth.Value, static b => b.FullWidth, static (b, v) => b.FullWidth = v);
        }

        if (Horizontal.HasValue)
        {
            bitBasicList.TakeFromCascade(nameof(Horizontal), Horizontal.Value, static b => b.Horizontal, static (b, v) => b.Horizontal = v);
        }

        if (ItemSize.HasValue)
        {
            bitBasicList.TakeFromCascade(nameof(ItemSize), ItemSize.Value, static b => b.ItemSize, static (b, v) => b.ItemSize = v);
        }

        if (ItemsProviderDelay.HasValue)
        {
            bitBasicList.TakeFromCascade(nameof(ItemsProviderDelay), ItemsProviderDelay.Value, static b => b.ItemsProviderDelay, static (b, v) => b.ItemsProviderDelay = v);
        }

        if (LoadingLabel.HasValue())
        {
            bitBasicList.TakeFromCascade(nameof(LoadingLabel), LoadingLabel, static b => b.LoadingLabel, static (b, v) => b.LoadingLabel = v);
        }

        if (LoadMore.HasValue)
        {
            bitBasicList.TakeFromCascade(nameof(LoadMore), LoadMore.Value, static b => b.LoadMore, static (b, v) => b.LoadMore = v);
        }

        if (LoadMoreSize.HasValue)
        {
            bitBasicList.TakeFromCascade(nameof(LoadMoreSize), LoadMoreSize.Value, static b => b.LoadMoreSize, static (b, v) => b.LoadMoreSize = v);
        }

        if (LoadMoreText.HasValue())
        {
            bitBasicList.TakeFromCascade(nameof(LoadMoreText), LoadMoreText, static b => b.LoadMoreText, static (b, v) => b.LoadMoreText = v);
        }

        if (OverscanCount.HasValue)
        {
            bitBasicList.TakeFromCascade(nameof(OverscanCount), OverscanCount.Value, static b => b.OverscanCount, static (b, v) => b.OverscanCount = v);
        }

        if (Role.HasValue())
        {
            bitBasicList.TakeFromCascade(nameof(Role), Role, static b => b.Role, static (b, v) => b.Role = v);
        }

        if (Styles is not null)
        {
            bitBasicList.TakeFromCascade(nameof(Styles), Styles, static b => b.Styles, static (b, v) => b.Styles = v);
        }

        if (Virtualize.HasValue)
        {
            bitBasicList.TakeFromCascade(nameof(Virtualize), Virtualize.Value, static b => b.Virtualize, static (b, v) => b.Virtualize = v);
        }
    }
}
