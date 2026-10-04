namespace Bit.BlazorUI;

/// <summary>
/// The parameters for <see cref="BitInfiniteScrolling{TItem}"/> component.
/// </summary>
/// <remarks>
/// What belongs here is what the lists of an application agree on: the texts of their states (which is where a
/// localized app sets them once), their templates, how they load (Manual, PageSize, the observer options) and how
/// they are styled. The provider, the item template, the keys, the events and the layout of one list (Horizontal,
/// Reversed, ScrollerSelector) are left out on purpose: they are what makes one list the one it is.
/// <br />
/// None of the parameters here depends on the type of the items, so the class is not generic: one instance
/// reaches every list under the <see cref="BitParams"/> it is given to, whatever it lists.
/// </remarks>
public class BitInfiniteScrollingParams : BitComponentBaseParams, IBitComponentParams
{
    /// <summary>
    /// Represents the parameter name used to identify the <see cref="BitInfiniteScrolling{TItem}"/> cascading parameters within <see cref="BitParams"/>.
    /// </summary>
    /// <remarks>
    /// This constant is typically used when referencing or accessing the BitInfiniteScrolling value in
    /// parameterized APIs or configuration settings. Using this constant helps ensure consistency and reduces the risk
    /// of typographical errors.
    /// </remarks>
    public const string ParamName = $"{nameof(BitParams)}.{nameof(BitInfiniteScrolling<object>)}";



    public string Name => ParamName;



    /// <summary>
    /// Custom CSS classes for different parts of the infinite scrolling.
    /// </summary>
    public BitInfiniteScrollingClassStyles? Classes { get; set; }

    /// <summary>
    /// The message to render when there is no item available and no EmptyTemplate is provided.
    /// </summary>
    public string? EmptyMessage { get; set; }

    /// <summary>
    /// The custom template to render when there is no item available.
    /// </summary>
    public RenderFragment? EmptyTemplate { get; set; }

    /// <summary>
    /// The message to render after the last page, when there is no more item to fetch.
    /// </summary>
    public string? EndMessage { get; set; }

    /// <summary>
    /// The custom template to render after the last page, when there is no more item to fetch.
    /// </summary>
    public RenderFragment? EndTemplate { get; set; }

    /// <summary>
    /// The message to render when the items provider throws and no ErrorTemplate is provided.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// The custom template to render when the items provider throws, receiving the thrown exception as its context.
    /// </summary>
    public RenderFragment<Exception>? ErrorTemplate { get; set; }

    /// <summary>
    /// Renders the list as an ARIA feed: each item becomes a focusable article, and Page Up / Page Down move between them.
    /// </summary>
    public bool? Feed { get; set; }

    /// <summary>
    /// The CSS class of the last element that triggers the loading.
    /// </summary>
    public string? LastElementClass { get; set; }

    /// <summary>
    /// The height of the last element that triggers the loading.
    /// </summary>
    public string? LastElementHeight { get; set; }

    /// <summary>
    /// The CSS style of the last element that triggers the loading.
    /// </summary>
    public string? LastElementStyle { get; set; }

    /// <summary>
    /// The width of the last element that triggers the loading.
    /// </summary>
    public string? LastElementWidth { get; set; }

    /// <summary>
    /// The message announced to screen readers after each page, formatted with the number of the items just loaded ({0}) and of all the loaded items ({1}).
    /// </summary>
    public string? LoadedMessage { get; set; }

    /// <summary>
    /// The message to render while loading the new items and no LoadingTemplate is provided.
    /// </summary>
    public string? LoadingMessage { get; set; }

    /// <summary>
    /// The custom template to render while loading the new items.
    /// </summary>
    public RenderFragment? LoadingTemplate { get; set; }

    /// <summary>
    /// The custom template of the button that loads the next page in the manual mode and retries a failed load.
    /// </summary>
    public RenderFragment? LoadMoreTemplate { get; set; }

    /// <summary>
    /// The text of the button that loads the next page in the manual mode.
    /// </summary>
    public string? LoadMoreText { get; set; }

    /// <summary>
    /// Replaces the automatic loading with an explicit button, so each page is fetched only when the user asks for it.
    /// </summary>
    public bool? Manual { get; set; }

    /// <summary>
    /// The maximum number of items to load.
    /// </summary>
    public int? MaxItems { get; set; }

    /// <summary>
    /// The number of the items to request in each page.
    /// </summary>
    public int? PageSize { get; set; }

    /// <summary>
    /// Pre-loads the data at the initialization of the component.
    /// </summary>
    public bool? Preload { get; set; }

    /// <summary>
    /// The text of the button that retries the failed load.
    /// </summary>
    public string? RetryText { get; set; }

    /// <summary>
    /// The rootMargin parameter of the IntersectionObserver.
    /// </summary>
    public string? RootMargin { get; set; }

    /// <summary>
    /// Custom CSS styles for different parts of the infinite scrolling.
    /// </summary>
    public BitInfiniteScrollingClassStyles? Styles { get; set; }

    /// <summary>
    /// The threshold parameter of the IntersectionObserver.
    /// </summary>
    public decimal? Threshold { get; set; }



    /// <summary>
    /// Updates the properties of the specified <see cref="BitInfiniteScrolling{TItem}"/> instance with any values that have been set on
    /// this object, if those properties have not already been set on the <see cref="BitInfiniteScrolling{TItem}"/> itself.
    /// </summary>
    /// <remarks>
    /// Only properties that have a value set and have not already been set on the <paramref name="bitInfiniteScrolling"/> will be updated.
    /// This method does not overwrite existing values on <paramref name="bitInfiniteScrolling"/>.
    /// </remarks>
    /// <param name="bitInfiniteScrolling">
    /// The <see cref="BitInfiniteScrolling{TItem}"/> instance whose properties will be updated. Cannot be null.
    /// </param>
    public void UpdateParameters<TItem>(BitInfiniteScrolling<TItem> bitInfiniteScrolling)
    {
        if (bitInfiniteScrolling is null) return;

        UpdateBaseParameters(bitInfiniteScrolling);

        // This runs on every render of every list under the BitParams, so the value that drives the class or the
        // style of the root is only assigned - and the builder only reset - when it differs from the one the list
        // already holds: an unchanged one would rebuild both strings on every render for nothing.
        if (Classes is not null && bitInfiniteScrolling.HasNotBeenSet(nameof(Classes)) && ReferenceEquals(bitInfiniteScrolling.Classes, Classes) is false)
        {
            bitInfiniteScrolling.Classes = Classes;

            bitInfiniteScrolling.ClassBuilder.Reset();
        }

        if (EmptyMessage is not null && bitInfiniteScrolling.HasNotBeenSet(nameof(EmptyMessage)))
        {
            bitInfiniteScrolling.EmptyMessage = EmptyMessage;
        }

        if (EmptyTemplate is not null && bitInfiniteScrolling.HasNotBeenSet(nameof(EmptyTemplate)))
        {
            bitInfiniteScrolling.EmptyTemplate = EmptyTemplate;
        }

        if (EndMessage is not null && bitInfiniteScrolling.HasNotBeenSet(nameof(EndMessage)))
        {
            bitInfiniteScrolling.EndMessage = EndMessage;
        }

        if (EndTemplate is not null && bitInfiniteScrolling.HasNotBeenSet(nameof(EndTemplate)))
        {
            bitInfiniteScrolling.EndTemplate = EndTemplate;
        }

        if (ErrorMessage is not null && bitInfiniteScrolling.HasNotBeenSet(nameof(ErrorMessage)))
        {
            bitInfiniteScrolling.ErrorMessage = ErrorMessage;
        }

        if (ErrorTemplate is not null && bitInfiniteScrolling.HasNotBeenSet(nameof(ErrorTemplate)))
        {
            bitInfiniteScrolling.ErrorTemplate = ErrorTemplate;
        }

        if (Feed.HasValue && bitInfiniteScrolling.HasNotBeenSet(nameof(Feed)))
        {
            bitInfiniteScrolling.Feed = Feed.Value;
        }

        if (LastElementClass is not null && bitInfiniteScrolling.HasNotBeenSet(nameof(LastElementClass)))
        {
            bitInfiniteScrolling.LastElementClass = LastElementClass;
        }

        if (LastElementHeight is not null && bitInfiniteScrolling.HasNotBeenSet(nameof(LastElementHeight)))
        {
            bitInfiniteScrolling.LastElementHeight = LastElementHeight;
        }

        if (LastElementStyle is not null && bitInfiniteScrolling.HasNotBeenSet(nameof(LastElementStyle)))
        {
            bitInfiniteScrolling.LastElementStyle = LastElementStyle;
        }

        if (LastElementWidth is not null && bitInfiniteScrolling.HasNotBeenSet(nameof(LastElementWidth)))
        {
            bitInfiniteScrolling.LastElementWidth = LastElementWidth;
        }

        if (LoadedMessage is not null && bitInfiniteScrolling.HasNotBeenSet(nameof(LoadedMessage)))
        {
            bitInfiniteScrolling.LoadedMessage = LoadedMessage;
        }

        if (LoadingMessage is not null && bitInfiniteScrolling.HasNotBeenSet(nameof(LoadingMessage)))
        {
            bitInfiniteScrolling.LoadingMessage = LoadingMessage;
        }

        if (LoadingTemplate is not null && bitInfiniteScrolling.HasNotBeenSet(nameof(LoadingTemplate)))
        {
            bitInfiniteScrolling.LoadingTemplate = LoadingTemplate;
        }

        if (LoadMoreTemplate is not null && bitInfiniteScrolling.HasNotBeenSet(nameof(LoadMoreTemplate)))
        {
            bitInfiniteScrolling.LoadMoreTemplate = LoadMoreTemplate;
        }

        if (LoadMoreText is not null && bitInfiniteScrolling.HasNotBeenSet(nameof(LoadMoreText)))
        {
            bitInfiniteScrolling.LoadMoreText = LoadMoreText;
        }

        if (Manual.HasValue && bitInfiniteScrolling.HasNotBeenSet(nameof(Manual)))
        {
            bitInfiniteScrolling.Manual = Manual.Value;
        }

        if (MaxItems.HasValue && bitInfiniteScrolling.HasNotBeenSet(nameof(MaxItems)))
        {
            bitInfiniteScrolling.MaxItems = MaxItems.Value;
        }

        if (PageSize.HasValue && bitInfiniteScrolling.HasNotBeenSet(nameof(PageSize)))
        {
            bitInfiniteScrolling.PageSize = PageSize.Value;
        }

        if (Preload.HasValue && bitInfiniteScrolling.HasNotBeenSet(nameof(Preload)))
        {
            bitInfiniteScrolling.Preload = Preload.Value;
        }

        if (RetryText is not null && bitInfiniteScrolling.HasNotBeenSet(nameof(RetryText)))
        {
            bitInfiniteScrolling.RetryText = RetryText;
        }

        if (RootMargin is not null && bitInfiniteScrolling.HasNotBeenSet(nameof(RootMargin)))
        {
            bitInfiniteScrolling.RootMargin = RootMargin;
        }

        if (Styles is not null && bitInfiniteScrolling.HasNotBeenSet(nameof(Styles)) && ReferenceEquals(bitInfiniteScrolling.Styles, Styles) is false)
        {
            bitInfiniteScrolling.Styles = Styles;

            bitInfiniteScrolling.StyleBuilder.Reset();
        }

        if (Threshold.HasValue && bitInfiniteScrolling.HasNotBeenSet(nameof(Threshold)))
        {
            bitInfiniteScrolling.Threshold = Threshold.Value;
        }
    }
}
