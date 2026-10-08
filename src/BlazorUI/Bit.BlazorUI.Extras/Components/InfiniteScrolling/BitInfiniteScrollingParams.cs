namespace Bit.BlazorUI;

/// <summary>
/// The parameters for <see cref="BitInfiniteScrolling{TItem}"/> component.
/// </summary>
/// <remarks>
/// What belongs here is what the lists of an application agree on: the texts of their states (which is where a
/// localized app sets them once), their templates, how they load (Manual, AutoLoadLimit, PageSize, the observer options) and how
/// they are styled. The provider, the item template, the keys and the names of the items, the events and the layout of one list (Horizontal,
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
    /// The number of the pages loaded automatically before the list switches to the Load more button.
    /// </summary>
    public int? AutoLoadLimit { get; set; }

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
    /// The message to render when the items provider throws and no ErrorTemplate is provided. With an ErrorTemplate it is what screen readers are told instead.
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

        if (AutoLoadLimit.HasValue)
        {
            bitInfiniteScrolling.TakeFromCascade(nameof(AutoLoadLimit), AutoLoadLimit.Value, static i => i.AutoLoadLimit, static (i, v) => i.AutoLoadLimit = v);
        }

        if (Classes is not null)
        {
            bitInfiniteScrolling.TakeFromCascade(nameof(Classes), Classes, static i => i.Classes, static (i, v) => i.Classes = v);
        }

        if (EmptyMessage is not null)
        {
            bitInfiniteScrolling.TakeFromCascade(nameof(EmptyMessage), EmptyMessage, static i => i.EmptyMessage, static (i, v) => i.EmptyMessage = v);
        }

        if (EmptyTemplate is not null)
        {
            bitInfiniteScrolling.TakeFromCascade(nameof(EmptyTemplate), EmptyTemplate, static i => i.EmptyTemplate, static (i, v) => i.EmptyTemplate = v);
        }

        if (EndMessage is not null)
        {
            bitInfiniteScrolling.TakeFromCascade(nameof(EndMessage), EndMessage, static i => i.EndMessage, static (i, v) => i.EndMessage = v);
        }

        if (EndTemplate is not null)
        {
            bitInfiniteScrolling.TakeFromCascade(nameof(EndTemplate), EndTemplate, static i => i.EndTemplate, static (i, v) => i.EndTemplate = v);
        }

        if (ErrorMessage is not null)
        {
            bitInfiniteScrolling.TakeFromCascade(nameof(ErrorMessage), ErrorMessage, static i => i.ErrorMessage, static (i, v) => i.ErrorMessage = v);
        }

        if (ErrorTemplate is not null)
        {
            bitInfiniteScrolling.TakeFromCascade(nameof(ErrorTemplate), ErrorTemplate, static i => i.ErrorTemplate, static (i, v) => i.ErrorTemplate = v);
        }

        if (Feed.HasValue)
        {
            bitInfiniteScrolling.TakeFromCascade(nameof(Feed), Feed.Value, static i => i.Feed, static (i, v) => i.Feed = v);
        }

        if (LastElementClass is not null)
        {
            bitInfiniteScrolling.TakeFromCascade(nameof(LastElementClass), LastElementClass, static i => i.LastElementClass, static (i, v) => i.LastElementClass = v);
        }

        if (LastElementHeight is not null)
        {
            bitInfiniteScrolling.TakeFromCascade(nameof(LastElementHeight), LastElementHeight, static i => i.LastElementHeight, static (i, v) => i.LastElementHeight = v);
        }

        if (LastElementStyle is not null)
        {
            bitInfiniteScrolling.TakeFromCascade(nameof(LastElementStyle), LastElementStyle, static i => i.LastElementStyle, static (i, v) => i.LastElementStyle = v);
        }

        if (LastElementWidth is not null)
        {
            bitInfiniteScrolling.TakeFromCascade(nameof(LastElementWidth), LastElementWidth, static i => i.LastElementWidth, static (i, v) => i.LastElementWidth = v);
        }

        if (LoadedMessage is not null)
        {
            bitInfiniteScrolling.TakeFromCascade(nameof(LoadedMessage), LoadedMessage, static i => i.LoadedMessage, static (i, v) => i.LoadedMessage = v);
        }

        if (LoadingMessage is not null)
        {
            bitInfiniteScrolling.TakeFromCascade(nameof(LoadingMessage), LoadingMessage, static i => i.LoadingMessage, static (i, v) => i.LoadingMessage = v);
        }

        if (LoadingTemplate is not null)
        {
            bitInfiniteScrolling.TakeFromCascade(nameof(LoadingTemplate), LoadingTemplate, static i => i.LoadingTemplate, static (i, v) => i.LoadingTemplate = v);
        }

        if (LoadMoreTemplate is not null)
        {
            bitInfiniteScrolling.TakeFromCascade(nameof(LoadMoreTemplate), LoadMoreTemplate, static i => i.LoadMoreTemplate, static (i, v) => i.LoadMoreTemplate = v);
        }

        if (LoadMoreText is not null)
        {
            bitInfiniteScrolling.TakeFromCascade(nameof(LoadMoreText), LoadMoreText, static i => i.LoadMoreText, static (i, v) => i.LoadMoreText = v);
        }

        if (Manual.HasValue)
        {
            bitInfiniteScrolling.TakeFromCascade(nameof(Manual), Manual.Value, static i => i.Manual, static (i, v) => i.Manual = v);
        }

        if (MaxItems.HasValue)
        {
            bitInfiniteScrolling.TakeFromCascade(nameof(MaxItems), MaxItems.Value, static i => i.MaxItems, static (i, v) => i.MaxItems = v);
        }

        if (PageSize.HasValue)
        {
            bitInfiniteScrolling.TakeFromCascade(nameof(PageSize), PageSize.Value, static i => i.PageSize, static (i, v) => i.PageSize = v);
        }

        if (Preload.HasValue)
        {
            bitInfiniteScrolling.TakeFromCascade(nameof(Preload), Preload.Value, static i => i.Preload, static (i, v) => i.Preload = v);
        }

        if (RetryText is not null)
        {
            bitInfiniteScrolling.TakeFromCascade(nameof(RetryText), RetryText, static i => i.RetryText, static (i, v) => i.RetryText = v);
        }

        if (RootMargin is not null)
        {
            bitInfiniteScrolling.TakeFromCascade(nameof(RootMargin), RootMargin, static i => i.RootMargin, static (i, v) => i.RootMargin = v);
        }

        if (Styles is not null)
        {
            bitInfiniteScrolling.TakeFromCascade(nameof(Styles), Styles, static i => i.Styles, static (i, v) => i.Styles = v);
        }

        if (Threshold.HasValue)
        {
            bitInfiniteScrolling.TakeFromCascade(nameof(Threshold), Threshold.Value, static i => i.Threshold, static (i, v) => i.Threshold = v);
        }
    }
}
