namespace Bit.BlazorUI;

/// <summary>
/// The parameters for <see cref="BitPagination"/> component.
/// </summary>
/// <remarks>
/// The data a pagination pages through (<see cref="BitPagination.Count"/>, <see cref="BitPagination.TotalItems"/>,
/// <see cref="BitPagination.PageSize"/> and the selected page) and its callbacks belong to each instance, so they are
/// not carried here. What is carried is how a pagination looks, behaves and speaks: which makes a single
/// <see cref="BitParams"/> the place to localize every pagination of an app at once.
/// </remarks>
public class BitPaginationParams : BitComponentBaseParams, IBitComponentParams
{
    /// <summary>
    /// Represents the parameter name used to identify the <see cref="BitPagination"/> cascading parameters within <see cref="BitParams"/>.
    /// </summary>
    /// <remarks>
    /// This constant is typically used when referencing or accessing the BitPagination value in
    /// parameterized APIs or configuration settings. Using this constant helps ensure consistency and reduces the risk
    /// of typographical errors.
    /// </remarks>
    public const string ParamName = $"{nameof(BitParams)}.{nameof(BitPagination)}";



    public string Name => ParamName;



    /// <summary>
    /// The horizontal alignment of the pagination inside the room it is given, which stretches it across that room.
    /// </summary>
    public BitAlignment? Alignment { get; set; }

    /// <summary>
    /// The number of items at the start and end of the pagination.
    /// </summary>
    public int? BoundaryCount { get; set; }

    /// <summary>
    /// Custom CSS classes for different parts of the pagination.
    /// </summary>
    public BitPaginationClassStyles? Classes { get; set; }

    /// <summary>
    /// Turns every ellipsis into a control that jumps into the middle of the pages it collapses.
    /// </summary>
    public bool? ClickableEllipsis { get; set; }

    /// <summary>
    /// The general color of the pagination.
    /// </summary>
    public BitColor? Color { get; set; }

    /// <summary>
    /// The accessible label of the item standing in for the pages an ellipsis collapses.
    /// </summary>
    public string? EllipsisAriaLabel { get; set; }

    /// <summary>
    /// The text of the ellipsis standing in for the pages that are collapsed out of the range.
    /// </summary>
    public string? EllipsisText { get; set; }

    /// <summary>
    /// The accessible label of the first button.
    /// </summary>
    public string? FirstButtonAriaLabel { get; set; }

    /// <summary>
    /// The icon for the first button using custom CSS classes for external icon libraries.
    /// </summary>
    public BitIconInfo? FirstButtonIcon { get; set; }

    /// <summary>
    /// The built-in icon name for the first button.
    /// </summary>
    public string? FirstButtonIconName { get; set; }

    /// <summary>
    /// The text rendered beside the icon of the first button.
    /// </summary>
    public string? FirstButtonText { get; set; }

    /// <summary>
    /// Provides the text of the summary while TotalItems is set, from the first and the last items of the selected
    /// page and the total number of items.
    /// </summary>
    public Func<int, int, int, string>? GetItemsSummary { get; set; }

    /// <summary>
    /// Provides the accessible label of a page button, from its one-based number and whether it is the selected one.
    /// </summary>
    public Func<int, bool, string>? GetPageAriaLabel { get; set; }

    /// <summary>
    /// Provides the address a page control points at, from its one-based number, which turns every control of the
    /// pagination into a link instead of a button.
    /// </summary>
    public Func<int, string?>? GetPageHref { get; set; }

    /// <summary>
    /// Provides the text of the summary, from the selected page and the total number of pages.
    /// </summary>
    public Func<int, int, string>? GetSummary { get; set; }

    /// <summary>
    /// The accessible label of the go to page input.
    /// </summary>
    public string? GoToPageAriaLabel { get; set; }

    /// <summary>
    /// The text rendered ahead of the go to page input. An empty text leaves the input on its own.
    /// </summary>
    public string? GoToPageText { get; set; }

    /// <summary>
    /// Renders nothing at all while there is a single page to navigate.
    /// </summary>
    public bool? HideOnSinglePage { get; set; }

    /// <summary>
    /// The accessible label of the last button.
    /// </summary>
    public string? LastButtonAriaLabel { get; set; }

    /// <summary>
    /// The icon for the last button using custom CSS classes for external icon libraries.
    /// </summary>
    public BitIconInfo? LastButtonIcon { get; set; }

    /// <summary>
    /// The built-in icon name for the last button.
    /// </summary>
    public string? LastButtonIconName { get; set; }

    /// <summary>
    /// The text rendered beside the icon of the last button.
    /// </summary>
    public string? LastButtonText { get; set; }

    /// <summary>
    /// Wraps the next and previous buttons around the ends of the range.
    /// </summary>
    public bool? Loop { get; set; }

    /// <summary>
    /// The number of items to render in the middle of the pagination.
    /// </summary>
    public int? MiddleCount { get; set; }

    /// <summary>
    /// The accessible label of the next button.
    /// </summary>
    public string? NextButtonAriaLabel { get; set; }

    /// <summary>
    /// The icon for the next button using custom CSS classes for external icon libraries.
    /// </summary>
    public BitIconInfo? NextButtonIcon { get; set; }

    /// <summary>
    /// The built-in icon name for the next button.
    /// </summary>
    public string? NextButtonIconName { get; set; }

    /// <summary>
    /// The text rendered beside the icon of the next button.
    /// </summary>
    public string? NextButtonText { get; set; }

    /// <summary>
    /// The accessible label of the page size selector.
    /// </summary>
    public string? PageSizeAriaLabel { get; set; }

    /// <summary>
    /// The page sizes the page size selector offers. The first one is the page size a pagination falls back to when its <see cref="BitPagination.PageSize"/> is not positive.
    /// </summary>
    public IEnumerable<int>? PageSizeOptions { get; set; }

    /// <summary>
    /// The text rendered ahead of the page size selector. An empty text leaves the selector on its own.
    /// </summary>
    public string? PageSizeText { get; set; }

    /// <summary>
    /// The accessible label of the previous button.
    /// </summary>
    public string? PreviousButtonAriaLabel { get; set; }

    /// <summary>
    /// The icon for the previous button using custom CSS classes for external icon libraries.
    /// </summary>
    public BitIconInfo? PreviousButtonIcon { get; set; }

    /// <summary>
    /// The built-in icon name for the previous button.
    /// </summary>
    public string? PreviousButtonIconName { get; set; }

    /// <summary>
    /// The text rendered beside the icon of the previous button.
    /// </summary>
    public string? PreviousButtonText { get; set; }

    /// <summary>
    /// Renders the buttons of the pagination with fully rounded (pill) corners.
    /// </summary>
    public bool? Rounded { get; set; }

    /// <summary>
    /// Determines whether to show the first button.
    /// </summary>
    public bool? ShowFirstButton { get; set; }

    /// <summary>
    /// Shows an input that jumps straight to the page number typed into it.
    /// </summary>
    public bool? ShowGoToPage { get; set; }

    /// <summary>
    /// Determines whether to show the last button.
    /// </summary>
    public bool? ShowLastButton { get; set; }

    /// <summary>
    /// Determines whether to show the next button.
    /// </summary>
    public bool? ShowNextButton { get; set; }

    /// <summary>
    /// Determines whether to show the numeric page buttons.
    /// </summary>
    public bool? ShowPageButtons { get; set; }

    /// <summary>
    /// Shows a selector that picks how many items a page holds.
    /// </summary>
    public bool? ShowPageSizeSelector { get; set; }

    /// <summary>
    /// Determines whether to show the previous button.
    /// </summary>
    public bool? ShowPreviousButton { get; set; }

    /// <summary>
    /// Shows the position in the range ahead of the buttons of the pagination.
    /// </summary>
    public bool? ShowSummary { get; set; }

    /// <summary>
    /// The size of the buttons.
    /// </summary>
    public BitSize? Size { get; set; }

    /// <summary>
    /// Custom CSS styles for different parts of the pagination.
    /// </summary>
    public BitPaginationClassStyles? Styles { get; set; }

    /// <summary>
    /// The visual variant of the pagination.
    /// </summary>
    public BitVariant? Variant { get; set; }



    /// <summary>
    /// Updates the properties of the specified <see cref="BitPagination"/> instance with any values that have been set on
    /// this object, if those properties have not already been set on the <see cref="BitPagination"/>.
    /// </summary>
    /// <remarks>
    /// Only properties that have a value set and have not already been set on the <paramref name="bitPagination"/> will be updated.
    /// This method does not overwrite existing values on <paramref name="bitPagination"/>.
    /// <br />
    /// The texts are applied whenever they are not null, so an empty <see cref="GoToPageText"/> or
    /// <see cref="PageSizeText"/> drops the visible label the way it does when it is set on the pagination itself.
    /// </remarks>
    /// <param name="bitPagination">
    /// The <see cref="BitPagination"/> instance whose properties will be updated. Cannot be null.
    /// </param>
    public void UpdateParameters(BitPagination bitPagination)
    {
        if (bitPagination is null) return;

        UpdateBaseParameters(bitPagination);

        if (Alignment.HasValue)
        {
            bitPagination.TakeFromCascade(nameof(Alignment), Alignment.Value, static p => p.Alignment, static (p, v) => p.Alignment = v);
        }

        if (BoundaryCount.HasValue)
        {
            bitPagination.TakeFromCascade(nameof(BoundaryCount), BoundaryCount.Value, static p => p.BoundaryCount, static (p, v) => p.BoundaryCount = v);
        }

        if (Classes is not null)
        {
            bitPagination.TakeFromCascade(nameof(Classes), Classes, static p => p.Classes, static (p, v) => p.Classes = v);
        }

        if (ClickableEllipsis.HasValue)
        {
            bitPagination.TakeFromCascade(nameof(ClickableEllipsis), ClickableEllipsis.Value, static p => p.ClickableEllipsis, static (p, v) => p.ClickableEllipsis = v);
        }

        if (Color.HasValue)
        {
            bitPagination.TakeFromCascade(nameof(Color), Color.Value, static p => p.Color, static (p, v) => p.Color = v);
        }

        if (EllipsisAriaLabel is not null)
        {
            bitPagination.TakeFromCascade(nameof(EllipsisAriaLabel), EllipsisAriaLabel, static p => p.EllipsisAriaLabel, static (p, v) => p.EllipsisAriaLabel = v);
        }

        if (EllipsisText is not null)
        {
            bitPagination.TakeFromCascade(nameof(EllipsisText), EllipsisText, static p => p.EllipsisText, static (p, v) => p.EllipsisText = v);
        }

        if (FirstButtonAriaLabel is not null)
        {
            bitPagination.TakeFromCascade(nameof(FirstButtonAriaLabel), FirstButtonAriaLabel, static p => p.FirstButtonAriaLabel, static (p, v) => p.FirstButtonAriaLabel = v);
        }

        if (FirstButtonIcon is not null)
        {
            bitPagination.TakeFromCascade(nameof(FirstButtonIcon), FirstButtonIcon, static p => p.FirstButtonIcon, static (p, v) => p.FirstButtonIcon = v);
        }

        if (FirstButtonIconName.HasValue())
        {
            bitPagination.TakeFromCascade(nameof(FirstButtonIconName), FirstButtonIconName, static p => p.FirstButtonIconName, static (p, v) => p.FirstButtonIconName = v);
        }

        if (FirstButtonText is not null)
        {
            bitPagination.TakeFromCascade(nameof(FirstButtonText), FirstButtonText, static p => p.FirstButtonText, static (p, v) => p.FirstButtonText = v);
        }

        if (GetItemsSummary is not null)
        {
            bitPagination.TakeFromCascade(nameof(GetItemsSummary), GetItemsSummary, static p => p.GetItemsSummary, static (p, v) => p.GetItemsSummary = v);
        }

        if (GetPageAriaLabel is not null)
        {
            bitPagination.TakeFromCascade(nameof(GetPageAriaLabel), GetPageAriaLabel, static p => p.GetPageAriaLabel, static (p, v) => p.GetPageAriaLabel = v);
        }

        if (GetPageHref is not null)
        {
            bitPagination.TakeFromCascade(nameof(GetPageHref), GetPageHref, static p => p.GetPageHref, static (p, v) => p.GetPageHref = v);
        }

        if (GetSummary is not null)
        {
            bitPagination.TakeFromCascade(nameof(GetSummary), GetSummary, static p => p.GetSummary, static (p, v) => p.GetSummary = v);
        }

        if (GoToPageAriaLabel is not null)
        {
            bitPagination.TakeFromCascade(nameof(GoToPageAriaLabel), GoToPageAriaLabel, static p => p.GoToPageAriaLabel, static (p, v) => p.GoToPageAriaLabel = v);
        }

        if (GoToPageText is not null)
        {
            bitPagination.TakeFromCascade(nameof(GoToPageText), GoToPageText, static p => p.GoToPageText, static (p, v) => p.GoToPageText = v);
        }

        if (HideOnSinglePage.HasValue)
        {
            bitPagination.TakeFromCascade(nameof(HideOnSinglePage), HideOnSinglePage.Value, static p => p.HideOnSinglePage, static (p, v) => p.HideOnSinglePage = v);
        }

        if (LastButtonAriaLabel is not null)
        {
            bitPagination.TakeFromCascade(nameof(LastButtonAriaLabel), LastButtonAriaLabel, static p => p.LastButtonAriaLabel, static (p, v) => p.LastButtonAriaLabel = v);
        }

        if (LastButtonIcon is not null)
        {
            bitPagination.TakeFromCascade(nameof(LastButtonIcon), LastButtonIcon, static p => p.LastButtonIcon, static (p, v) => p.LastButtonIcon = v);
        }

        if (LastButtonIconName.HasValue())
        {
            bitPagination.TakeFromCascade(nameof(LastButtonIconName), LastButtonIconName, static p => p.LastButtonIconName, static (p, v) => p.LastButtonIconName = v);
        }

        if (LastButtonText is not null)
        {
            bitPagination.TakeFromCascade(nameof(LastButtonText), LastButtonText, static p => p.LastButtonText, static (p, v) => p.LastButtonText = v);
        }

        if (Loop.HasValue)
        {
            bitPagination.TakeFromCascade(nameof(Loop), Loop.Value, static p => p.Loop, static (p, v) => p.Loop = v);
        }

        if (MiddleCount.HasValue)
        {
            bitPagination.TakeFromCascade(nameof(MiddleCount), MiddleCount.Value, static p => p.MiddleCount, static (p, v) => p.MiddleCount = v);
        }

        if (NextButtonAriaLabel is not null)
        {
            bitPagination.TakeFromCascade(nameof(NextButtonAriaLabel), NextButtonAriaLabel, static p => p.NextButtonAriaLabel, static (p, v) => p.NextButtonAriaLabel = v);
        }

        if (NextButtonIcon is not null)
        {
            bitPagination.TakeFromCascade(nameof(NextButtonIcon), NextButtonIcon, static p => p.NextButtonIcon, static (p, v) => p.NextButtonIcon = v);
        }

        if (NextButtonIconName.HasValue())
        {
            bitPagination.TakeFromCascade(nameof(NextButtonIconName), NextButtonIconName, static p => p.NextButtonIconName, static (p, v) => p.NextButtonIconName = v);
        }

        if (NextButtonText is not null)
        {
            bitPagination.TakeFromCascade(nameof(NextButtonText), NextButtonText, static p => p.NextButtonText, static (p, v) => p.NextButtonText = v);
        }

        if (PageSizeAriaLabel is not null)
        {
            bitPagination.TakeFromCascade(nameof(PageSizeAriaLabel), PageSizeAriaLabel, static p => p.PageSizeAriaLabel, static (p, v) => p.PageSizeAriaLabel = v);
        }

        if (PageSizeOptions is not null)
        {
            bitPagination.TakeFromCascade(nameof(PageSizeOptions), PageSizeOptions, static p => p.PageSizeOptions, static (p, v) => p.PageSizeOptions = v);
        }

        if (PageSizeText is not null)
        {
            bitPagination.TakeFromCascade(nameof(PageSizeText), PageSizeText, static p => p.PageSizeText, static (p, v) => p.PageSizeText = v);
        }

        if (PreviousButtonAriaLabel is not null)
        {
            bitPagination.TakeFromCascade(nameof(PreviousButtonAriaLabel), PreviousButtonAriaLabel, static p => p.PreviousButtonAriaLabel, static (p, v) => p.PreviousButtonAriaLabel = v);
        }

        if (PreviousButtonIcon is not null)
        {
            bitPagination.TakeFromCascade(nameof(PreviousButtonIcon), PreviousButtonIcon, static p => p.PreviousButtonIcon, static (p, v) => p.PreviousButtonIcon = v);
        }

        if (PreviousButtonIconName.HasValue())
        {
            bitPagination.TakeFromCascade(nameof(PreviousButtonIconName), PreviousButtonIconName, static p => p.PreviousButtonIconName, static (p, v) => p.PreviousButtonIconName = v);
        }

        if (PreviousButtonText is not null)
        {
            bitPagination.TakeFromCascade(nameof(PreviousButtonText), PreviousButtonText, static p => p.PreviousButtonText, static (p, v) => p.PreviousButtonText = v);
        }

        if (Rounded.HasValue)
        {
            bitPagination.TakeFromCascade(nameof(Rounded), Rounded.Value, static p => p.Rounded, static (p, v) => p.Rounded = v);
        }

        if (ShowFirstButton.HasValue)
        {
            bitPagination.TakeFromCascade(nameof(ShowFirstButton), ShowFirstButton.Value, static p => p.ShowFirstButton, static (p, v) => p.ShowFirstButton = v);
        }

        if (ShowGoToPage.HasValue)
        {
            bitPagination.TakeFromCascade(nameof(ShowGoToPage), ShowGoToPage.Value, static p => p.ShowGoToPage, static (p, v) => p.ShowGoToPage = v);
        }

        if (ShowLastButton.HasValue)
        {
            bitPagination.TakeFromCascade(nameof(ShowLastButton), ShowLastButton.Value, static p => p.ShowLastButton, static (p, v) => p.ShowLastButton = v);
        }

        if (ShowNextButton.HasValue)
        {
            bitPagination.TakeFromCascade(nameof(ShowNextButton), ShowNextButton.Value, static p => p.ShowNextButton, static (p, v) => p.ShowNextButton = v);
        }

        if (ShowPageButtons.HasValue)
        {
            bitPagination.TakeFromCascade(nameof(ShowPageButtons), ShowPageButtons.Value, static p => p.ShowPageButtons, static (p, v) => p.ShowPageButtons = v);
        }

        if (ShowPageSizeSelector.HasValue)
        {
            bitPagination.TakeFromCascade(nameof(ShowPageSizeSelector), ShowPageSizeSelector.Value, static p => p.ShowPageSizeSelector, static (p, v) => p.ShowPageSizeSelector = v);
        }

        if (ShowPreviousButton.HasValue)
        {
            bitPagination.TakeFromCascade(nameof(ShowPreviousButton), ShowPreviousButton.Value, static p => p.ShowPreviousButton, static (p, v) => p.ShowPreviousButton = v);
        }

        if (ShowSummary.HasValue)
        {
            bitPagination.TakeFromCascade(nameof(ShowSummary), ShowSummary.Value, static p => p.ShowSummary, static (p, v) => p.ShowSummary = v);
        }

        if (Size.HasValue)
        {
            bitPagination.TakeFromCascade(nameof(Size), Size.Value, static p => p.Size, static (p, v) => p.Size = v);
        }

        if (Styles is not null)
        {
            bitPagination.TakeFromCascade(nameof(Styles), Styles, static p => p.Styles, static (p, v) => p.Styles = v);
        }

        if (Variant.HasValue)
        {
            bitPagination.TakeFromCascade(nameof(Variant), Variant.Value, static p => p.Variant, static (p, v) => p.Variant = v);
        }
    }
}
