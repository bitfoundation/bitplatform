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
    /// The page sizes the page size selector offers. The first one is the page size a pagination that is given none pages by.
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

        if (Alignment.HasValue && bitPagination.HasNotBeenSet(nameof(Alignment)))
        {
            bitPagination.Alignment = Alignment.Value;

            bitPagination.ClassBuilder.Reset();
            bitPagination.StyleBuilder.Reset();
        }

        if (BoundaryCount.HasValue && bitPagination.HasNotBeenSet(nameof(BoundaryCount)))
        {
            bitPagination.BoundaryCount = BoundaryCount.Value;
        }

        if (Classes is not null && bitPagination.HasNotBeenSet(nameof(Classes)))
        {
            bitPagination.Classes = Classes;

            bitPagination.ClassBuilder.Reset();
        }

        if (ClickableEllipsis.HasValue && bitPagination.HasNotBeenSet(nameof(ClickableEllipsis)))
        {
            bitPagination.ClickableEllipsis = ClickableEllipsis.Value;
        }

        if (Color.HasValue && bitPagination.HasNotBeenSet(nameof(Color)))
        {
            bitPagination.Color = Color.Value;

            bitPagination.ClassBuilder.Reset();
        }

        if (EllipsisAriaLabel is not null && bitPagination.HasNotBeenSet(nameof(EllipsisAriaLabel)))
        {
            bitPagination.EllipsisAriaLabel = EllipsisAriaLabel;
        }

        if (EllipsisText is not null && bitPagination.HasNotBeenSet(nameof(EllipsisText)))
        {
            bitPagination.EllipsisText = EllipsisText;
        }

        if (FirstButtonAriaLabel is not null && bitPagination.HasNotBeenSet(nameof(FirstButtonAriaLabel)))
        {
            bitPagination.FirstButtonAriaLabel = FirstButtonAriaLabel;
        }

        if (FirstButtonIcon is not null && bitPagination.HasNotBeenSet(nameof(FirstButtonIcon)))
        {
            bitPagination.FirstButtonIcon = FirstButtonIcon;
        }

        if (FirstButtonIconName.HasValue() && bitPagination.HasNotBeenSet(nameof(FirstButtonIconName)))
        {
            bitPagination.FirstButtonIconName = FirstButtonIconName;
        }

        if (FirstButtonText is not null && bitPagination.HasNotBeenSet(nameof(FirstButtonText)))
        {
            bitPagination.FirstButtonText = FirstButtonText;
        }

        if (GetPageAriaLabel is not null && bitPagination.HasNotBeenSet(nameof(GetPageAriaLabel)))
        {
            bitPagination.GetPageAriaLabel = GetPageAriaLabel;
        }

        if (GetPageHref is not null && bitPagination.HasNotBeenSet(nameof(GetPageHref)))
        {
            bitPagination.GetPageHref = GetPageHref;
        }

        if (GetSummary is not null && bitPagination.HasNotBeenSet(nameof(GetSummary)))
        {
            bitPagination.GetSummary = GetSummary;
        }

        if (GoToPageAriaLabel is not null && bitPagination.HasNotBeenSet(nameof(GoToPageAriaLabel)))
        {
            bitPagination.GoToPageAriaLabel = GoToPageAriaLabel;
        }

        if (GoToPageText is not null && bitPagination.HasNotBeenSet(nameof(GoToPageText)))
        {
            bitPagination.GoToPageText = GoToPageText;
        }

        if (HideOnSinglePage.HasValue && bitPagination.HasNotBeenSet(nameof(HideOnSinglePage)))
        {
            bitPagination.HideOnSinglePage = HideOnSinglePage.Value;
        }

        if (LastButtonAriaLabel is not null && bitPagination.HasNotBeenSet(nameof(LastButtonAriaLabel)))
        {
            bitPagination.LastButtonAriaLabel = LastButtonAriaLabel;
        }

        if (LastButtonIcon is not null && bitPagination.HasNotBeenSet(nameof(LastButtonIcon)))
        {
            bitPagination.LastButtonIcon = LastButtonIcon;
        }

        if (LastButtonIconName.HasValue() && bitPagination.HasNotBeenSet(nameof(LastButtonIconName)))
        {
            bitPagination.LastButtonIconName = LastButtonIconName;
        }

        if (LastButtonText is not null && bitPagination.HasNotBeenSet(nameof(LastButtonText)))
        {
            bitPagination.LastButtonText = LastButtonText;
        }

        if (Loop.HasValue && bitPagination.HasNotBeenSet(nameof(Loop)))
        {
            bitPagination.Loop = Loop.Value;
        }

        if (MiddleCount.HasValue && bitPagination.HasNotBeenSet(nameof(MiddleCount)))
        {
            bitPagination.MiddleCount = MiddleCount.Value;
        }

        if (NextButtonAriaLabel is not null && bitPagination.HasNotBeenSet(nameof(NextButtonAriaLabel)))
        {
            bitPagination.NextButtonAriaLabel = NextButtonAriaLabel;
        }

        if (NextButtonIcon is not null && bitPagination.HasNotBeenSet(nameof(NextButtonIcon)))
        {
            bitPagination.NextButtonIcon = NextButtonIcon;
        }

        if (NextButtonIconName.HasValue() && bitPagination.HasNotBeenSet(nameof(NextButtonIconName)))
        {
            bitPagination.NextButtonIconName = NextButtonIconName;
        }

        if (NextButtonText is not null && bitPagination.HasNotBeenSet(nameof(NextButtonText)))
        {
            bitPagination.NextButtonText = NextButtonText;
        }

        if (PageSizeAriaLabel is not null && bitPagination.HasNotBeenSet(nameof(PageSizeAriaLabel)))
        {
            bitPagination.PageSizeAriaLabel = PageSizeAriaLabel;
        }

        if (PageSizeOptions is not null && bitPagination.HasNotBeenSet(nameof(PageSizeOptions)))
        {
            bitPagination.PageSizeOptions = PageSizeOptions;
        }

        if (PageSizeText is not null && bitPagination.HasNotBeenSet(nameof(PageSizeText)))
        {
            bitPagination.PageSizeText = PageSizeText;
        }

        if (PreviousButtonAriaLabel is not null && bitPagination.HasNotBeenSet(nameof(PreviousButtonAriaLabel)))
        {
            bitPagination.PreviousButtonAriaLabel = PreviousButtonAriaLabel;
        }

        if (PreviousButtonIcon is not null && bitPagination.HasNotBeenSet(nameof(PreviousButtonIcon)))
        {
            bitPagination.PreviousButtonIcon = PreviousButtonIcon;
        }

        if (PreviousButtonIconName.HasValue() && bitPagination.HasNotBeenSet(nameof(PreviousButtonIconName)))
        {
            bitPagination.PreviousButtonIconName = PreviousButtonIconName;
        }

        if (PreviousButtonText is not null && bitPagination.HasNotBeenSet(nameof(PreviousButtonText)))
        {
            bitPagination.PreviousButtonText = PreviousButtonText;
        }

        if (Rounded.HasValue && bitPagination.HasNotBeenSet(nameof(Rounded)))
        {
            bitPagination.Rounded = Rounded.Value;

            bitPagination.ClassBuilder.Reset();
        }

        if (ShowFirstButton.HasValue && bitPagination.HasNotBeenSet(nameof(ShowFirstButton)))
        {
            bitPagination.ShowFirstButton = ShowFirstButton.Value;
        }

        if (ShowGoToPage.HasValue && bitPagination.HasNotBeenSet(nameof(ShowGoToPage)))
        {
            bitPagination.ShowGoToPage = ShowGoToPage.Value;
        }

        if (ShowLastButton.HasValue && bitPagination.HasNotBeenSet(nameof(ShowLastButton)))
        {
            bitPagination.ShowLastButton = ShowLastButton.Value;
        }

        if (ShowNextButton.HasValue && bitPagination.HasNotBeenSet(nameof(ShowNextButton)))
        {
            bitPagination.ShowNextButton = ShowNextButton.Value;
        }

        if (ShowPageButtons.HasValue && bitPagination.HasNotBeenSet(nameof(ShowPageButtons)))
        {
            bitPagination.ShowPageButtons = ShowPageButtons.Value;
        }

        if (ShowPageSizeSelector.HasValue && bitPagination.HasNotBeenSet(nameof(ShowPageSizeSelector)))
        {
            bitPagination.ShowPageSizeSelector = ShowPageSizeSelector.Value;
        }

        if (ShowPreviousButton.HasValue && bitPagination.HasNotBeenSet(nameof(ShowPreviousButton)))
        {
            bitPagination.ShowPreviousButton = ShowPreviousButton.Value;
        }

        if (ShowSummary.HasValue && bitPagination.HasNotBeenSet(nameof(ShowSummary)))
        {
            bitPagination.ShowSummary = ShowSummary.Value;
        }

        if (Size.HasValue && bitPagination.HasNotBeenSet(nameof(Size)))
        {
            bitPagination.Size = Size.Value;

            bitPagination.ClassBuilder.Reset();
        }

        if (Styles is not null && bitPagination.HasNotBeenSet(nameof(Styles)))
        {
            bitPagination.Styles = Styles;

            bitPagination.StyleBuilder.Reset();
        }

        if (Variant.HasValue && bitPagination.HasNotBeenSet(nameof(Variant)))
        {
            bitPagination.Variant = Variant.Value;

            bitPagination.ClassBuilder.Reset();
        }
    }
}
