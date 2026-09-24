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

        if (AutoLoad.HasValue && bitBasicList.HasNotBeenSet(nameof(AutoLoad)))
        {
            bitBasicList.AutoLoad = AutoLoad.Value;
        }

        if (AutoLoadThreshold.HasValue && bitBasicList.HasNotBeenSet(nameof(AutoLoadThreshold)))
        {
            bitBasicList.AutoLoadThreshold = AutoLoadThreshold.Value;
        }

        if (Classes is not null && bitBasicList.HasNotBeenSet(nameof(Classes)))
        {
            bitBasicList.Classes = Classes;

            bitBasicList.ClassBuilder.Reset();
        }

        if (FitHeight.HasValue && bitBasicList.HasNotBeenSet(nameof(FitHeight)))
        {
            bitBasicList.FitHeight = FitHeight.Value;

            bitBasicList.StyleBuilder.Reset();
        }

        if (FitSize.HasValue && bitBasicList.HasNotBeenSet(nameof(FitSize)))
        {
            bitBasicList.FitSize = FitSize.Value;

            bitBasicList.StyleBuilder.Reset();
        }

        if (FitWidth.HasValue && bitBasicList.HasNotBeenSet(nameof(FitWidth)))
        {
            bitBasicList.FitWidth = FitWidth.Value;

            bitBasicList.StyleBuilder.Reset();
        }

        if (FullHeight.HasValue && bitBasicList.HasNotBeenSet(nameof(FullHeight)))
        {
            bitBasicList.FullHeight = FullHeight.Value;

            bitBasicList.StyleBuilder.Reset();
        }

        if (FullSize.HasValue && bitBasicList.HasNotBeenSet(nameof(FullSize)))
        {
            bitBasicList.FullSize = FullSize.Value;

            bitBasicList.StyleBuilder.Reset();
        }

        if (FullWidth.HasValue && bitBasicList.HasNotBeenSet(nameof(FullWidth)))
        {
            bitBasicList.FullWidth = FullWidth.Value;

            bitBasicList.StyleBuilder.Reset();
        }

        if (Horizontal.HasValue && bitBasicList.HasNotBeenSet(nameof(Horizontal)))
        {
            bitBasicList.Horizontal = Horizontal.Value;

            bitBasicList.ClassBuilder.Reset();
        }

        if (ItemSize.HasValue && bitBasicList.HasNotBeenSet(nameof(ItemSize)))
        {
            bitBasicList.ItemSize = ItemSize.Value;
        }

        if (ItemsProviderDelay.HasValue && bitBasicList.HasNotBeenSet(nameof(ItemsProviderDelay)))
        {
            bitBasicList.ItemsProviderDelay = ItemsProviderDelay.Value;
        }

        if (LoadingLabel.HasValue() && bitBasicList.HasNotBeenSet(nameof(LoadingLabel)))
        {
            bitBasicList.LoadingLabel = LoadingLabel;
        }

        if (LoadMore.HasValue && bitBasicList.HasNotBeenSet(nameof(LoadMore)))
        {
            bitBasicList.LoadMore = LoadMore.Value;
        }

        if (LoadMoreSize.HasValue && bitBasicList.HasNotBeenSet(nameof(LoadMoreSize)))
        {
            bitBasicList.LoadMoreSize = LoadMoreSize.Value;
        }

        if (LoadMoreText.HasValue() && bitBasicList.HasNotBeenSet(nameof(LoadMoreText)))
        {
            bitBasicList.LoadMoreText = LoadMoreText;
        }

        if (OverscanCount.HasValue && bitBasicList.HasNotBeenSet(nameof(OverscanCount)))
        {
            bitBasicList.OverscanCount = OverscanCount.Value;
        }

        if (Role.HasValue() && bitBasicList.HasNotBeenSet(nameof(Role)))
        {
            bitBasicList.Role = Role;
        }

        if (Styles is not null && bitBasicList.HasNotBeenSet(nameof(Styles)))
        {
            bitBasicList.Styles = Styles;

            bitBasicList.StyleBuilder.Reset();
        }

        if (Virtualize.HasValue && bitBasicList.HasNotBeenSet(nameof(Virtualize)))
        {
            bitBasicList.Virtualize = Virtualize.Value;

            bitBasicList.ClassBuilder.Reset();
        }
    }
}
