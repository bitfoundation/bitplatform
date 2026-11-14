using System.Diagnostics.CodeAnalysis;

namespace Bit.BlazorUI;

/// <summary>
/// The words the components fall back on when the parameter that would say them is left unset: the names of
/// icon-only buttons, the busy and error announcements, and the default text of a few labels and messages.
/// </summary>
/// <remarks>
/// <para>
/// Kept in one replaceable object so that a page in another language translates them once, instead of finding and
/// setting the parameter behind each of them on every component. Subclass it, override the members to translate
/// (each one left alone keeps its English), and set <see cref="Current"/> to an instance of the subclass - directly,
/// or through <see cref="BitBlazorUIOptions.Strings"/> when calling <c>AddBitBlazorUIServices</c>.
/// </para>
/// <para>
/// A parameter set on a component, or cascaded to it by its params (a <see cref="BitParams"/>), still wins over these:
/// they are only what the component says when nothing else is given. <see cref="Current"/> is process-wide, so every
/// circuit of a Blazor Server app shares it; an app serving several languages at once overrides the members to read
/// the culture of the moment (<see cref="System.Globalization.CultureInfo.CurrentUICulture"/>, an
/// <c>IStringLocalizer</c>) rather than return fixed text. The members ending in <c>Format</c> are composite format
/// strings whose <c>{0}</c> is filled in by the component, just as the parameters of the same name are.
/// </para>
/// </remarks>
public class BitStrings
{
    private static BitStrings? _current;



    /// <summary>The default English strings.</summary>
    public static BitStrings Default { get; } = new();

    /// <summary>
    /// The strings every component falls back on. Defaults to <see cref="Default"/>; setting it to <see langword="null"/>
    /// puts the default back.
    /// </summary>
    [AllowNull]
    public static BitStrings Current
    {
        get => _current ?? Default;
        set => _current = value;
    }



    // ===== Shared by several components =====

    /// <summary>
    /// What a screen reader announces while a field is busy: the fallback of <see cref="BitTextField.LoadingAriaLabel"/>,
    /// <see cref="BitTagsInput.LoadingAriaLabel"/> and <see cref="BitNumberField{TValue}.LoadingAriaLabel"/>.
    /// </summary>
    public virtual string Loading => "Loading";

    /// <summary>
    /// The visible text of a list that is still loading its items: the fallback of
    /// <see cref="BitDropdown{TItem, TValue}.LoadingText"/>.
    /// </summary>
    public virtual string LoadingText => "Loading...";

    /// <summary>
    /// What a component with no items at all says: the fallback of every <c>EmptyText</c> - that of
    /// <see cref="BitDropdown{TItem, TValue}.EmptyText"/>, <see cref="BitBasicList{TItem}.EmptyText"/> and the
    /// components of the other packages (the lists, the chart, the notification center, the data grid).
    /// </summary>
    public virtual string NoItemsFound => "No items found";

    /// <summary>
    /// What a component says when a search or a filter matched none of its items: the fallback of every
    /// <c>NoResultsText</c> - that of <see cref="BitDropdown{TItem, TValue}.NoResultsText"/>,
    /// <see cref="BitSearchBox.NoResultsText"/> and the pickers and the nav panel of the other packages.
    /// </summary>
    public virtual string NoResultsFound => "No results found";

    /// <summary>
    /// What the live region of an input says of an <c>ErrorMessageTemplate</c> when its <c>ErrorMessageAriaLabel</c> is
    /// not set, the template having no text of its own to announce (<see cref="BitTextField"/>, <see cref="BitSearchBox"/>,
    /// <see cref="BitNumberField{TValue}"/>, <see cref="BitDropdown{TItem, TValue}"/>, <see cref="BitDatePicker"/>,
    /// <see cref="BitTimePicker"/> and <see cref="BitToggle"/>).
    /// </summary>
    public virtual string InvalidInput => "Invalid input";

    /// <summary>
    /// The name of a button that closes a surface: the fallback of <see cref="BitPanel.CloseButtonTitle"/>,
    /// <see cref="BitDialog.CloseButtonTitle"/>, <see cref="BitModal.CloseButtonTitle"/>,
    /// <see cref="BitSnackBar.DismissAriaLabel"/> and <see cref="BitDropdown{TItem, TValue}.ResponsiveCloseButtonAriaLabel"/>.
    /// </summary>
    public virtual string Close => "Close";

    /// <summary>
    /// The name of a button that removes an item: the fallback of <see cref="BitFileInput.RemoveButtonTitle"/>,
    /// <see cref="BitFileUpload.RemoveButtonTitle"/>, <see cref="BitTagsInput.DismissTitle"/> and
    /// <see cref="BitPivot.DismissTitle"/>.
    /// </summary>
    public virtual string Remove => "Remove";

    /// <summary>
    /// The name of a button that removes a named item, <c>{0}</c> being its name: the fallback of
    /// <see cref="BitTagsInput.DismissAriaLabelFormat"/>, <see cref="BitPivot.DismissAriaLabelFormat"/> and
    /// <see cref="BitTag.DismissLabelFormat"/>.
    /// </summary>
    public virtual string RemoveFormat => "Remove {0}";

    /// <summary>
    /// The name of the dismiss button of a <see cref="BitTag"/> that has no text to name it by.
    /// </summary>
    public virtual string Dismiss => "Dismiss";

    /// <summary>
    /// The name of a button that adds an item: the fallback of <see cref="BitPivot.AddTitle"/>,
    /// <see cref="BitPivot.AddAriaLabel"/> and <see cref="BitDropdown{TItem, TValue}.ComboBoxAddButtonAriaLabel"/>.
    /// </summary>
    public virtual string Add => "Add";

    /// <summary>
    /// The name of a button that clears the text typed into a field: the fallback of
    /// <see cref="BitTextField.ClearButtonAriaLabel"/> and <see cref="BitDropdown{TItem, TValue}.SearchBoxClearButtonAriaLabel"/>.
    /// </summary>
    public virtual string ClearText => "Clear text";

    /// <summary>
    /// The text of the link that skips to the main content: the fallback of <see cref="BitHeader.SkipLinkText"/> and
    /// <see cref="BitLayout.SkipLinkText"/>.
    /// </summary>
    public virtual string SkipToMainContent => "Skip to main content";



    // ===== BitMenuButton =====

    /// <summary>
    /// The name of the chevron button of a split menu button: the fallback of <see cref="BitMenuButton{TItem}.ChevronDownAriaLabel"/>.
    /// </summary>
    public virtual string MoreOptions => "More options";



    // ===== BitNumberField =====

    /// <summary>
    /// The name of the increment button: the fallback of <see cref="BitNumberField{TValue}.IncrementAriaLabel"/>.
    /// </summary>
    public virtual string IncreaseValue => "Increase value";

    /// <summary>
    /// The name of the decrement button: the fallback of <see cref="BitNumberField{TValue}.DecrementAriaLabel"/>.
    /// </summary>
    public virtual string DecreaseValue => "Decrease value";

    /// <summary>
    /// The name of the clear button: the fallback of <see cref="BitNumberField{TValue}.ClearButtonAriaLabel"/>.
    /// </summary>
    public virtual string ClearValue => "Clear value";



    // ===== BitTextField =====

    /// <summary>
    /// The name of the reveal-password button: the fallback of <see cref="BitTextField.RevealPasswordAriaLabel"/>.
    /// </summary>
    public virtual string RevealPassword => "Reveal password";

    /// <summary>
    /// What a screen reader announces once the clear button emptied the field: the fallback of
    /// <see cref="BitTextField.ClearedAnnouncement"/>.
    /// </summary>
    public virtual string Cleared => "Cleared";



    // ===== BitDropdown =====

    /// <summary>
    /// The name of the clear button: the fallback of <see cref="BitDropdown{TItem, TValue}.ClearButtonAriaLabel"/>.
    /// </summary>
    public virtual string ClearSelection => "Clear selection";

    /// <summary>
    /// The name of the search box: the fallback of <see cref="BitDropdown{TItem, TValue}.SearchBoxAriaLabel"/>.
    /// </summary>
    public virtual string SearchText => "Search text";

    /// <summary>
    /// The text of the select-all option: the fallback of <see cref="BitDropdown{TItem, TValue}.SelectAllText"/>.
    /// </summary>
    public virtual string SelectAll => "Select all";




    // ===== BitSearchBox =====

    /// <summary>
    /// What a screen reader announces while the suggestions load: the fallback of <see cref="BitSearchBox.LoadingText"/>.
    /// </summary>
    public virtual string LoadingSuggestions => "Loading suggestions.";

    /// <summary>
    /// What a screen reader announces when the suggestions failed to load: the fallback of
    /// <see cref="BitSearchBox.SuggestFailedText"/>.
    /// </summary>
    public virtual string SuggestionsFailed => "Suggestions could not be loaded.";

    /// <summary>
    /// What a screen reader announces while too few characters are typed for suggestions, <c>{0}</c> being how many
    /// are needed: the fallback of <see cref="BitSearchBox.MinSuggestTriggerCharsText"/>.
    /// </summary>
    public virtual string MinSuggestTriggerCharsFormat => "Type {0} or more characters for suggestions.";



    // ===== BitFileInput and BitFileUpload =====

    /// <summary>
    /// The text of the browse button: the fallback of <see cref="BitFileInput.Label"/>.
    /// </summary>
    public virtual string Browse => "Browse";

    /// <summary>
    /// The name of the list of selected files: the fallback of <see cref="BitFileInput.FileListAriaLabel"/>.
    /// </summary>
    public virtual string SelectedFiles => "Selected files";

    /// <summary>
    /// The message of a file larger than allowed: the fallback of <see cref="BitFileInput.MaxSizeErrorMessage"/>.
    /// </summary>
    public virtual string FileTooLarge => "The file size is larger than the max size";

    /// <summary>
    /// The message of a file smaller than allowed: the fallback of <see cref="BitFileInput.MinSizeErrorMessage"/>.
    /// </summary>
    public virtual string FileTooSmall => "The file size is smaller than the min size";

    /// <summary>
    /// The message of a file of a type not allowed: the fallback of <see cref="BitFileInput.NotAllowedExtensionErrorMessage"/>.
    /// </summary>
    public virtual string FileTypeNotAllowed => "The file type is not allowed";

    /// <summary>
    /// The message of a file selected twice: the fallback of <see cref="BitFileInput.DuplicateErrorMessage"/>.
    /// </summary>
    public virtual string FileAlreadySelected => "The file is already selected";

    /// <summary>
    /// The message of a file beyond the most allowed: the fallback of <see cref="BitFileInput.MaxCountErrorMessage"/>.
    /// </summary>
    public virtual string TooManyFiles => "The maximum number of files is exceeded";

    /// <summary>
    /// The message of a file that takes the total size past the most allowed: the fallback of
    /// <see cref="BitFileInput.MaxTotalSizeErrorMessage"/>.
    /// </summary>
    public virtual string FilesTooLarge => "The total size of the files is larger than the max total size";

    /// <summary>
    /// The name of the upload button of a file: the fallback of <see cref="BitFileUpload.UploadButtonTitle"/>.
    /// </summary>
    public virtual string Upload => "Upload";

    /// <summary>
    /// The name of the upload button of a file whose upload failed or was canceled: the fallback of
    /// <see cref="BitFileUpload.RetryButtonTitle"/> when <see cref="BitFileUpload.UploadButtonTitle"/> is not set either.
    /// </summary>
    public virtual string Retry => "Retry";

    /// <summary>
    /// The name of the pause button of a file: the fallback of <see cref="BitFileUpload.PauseButtonTitle"/>.
    /// </summary>
    public virtual string Pause => "Pause";

    /// <summary>
    /// The name of the cancel button of a file: the fallback of <see cref="BitFileUpload.CancelButtonTitle"/>.
    /// </summary>
    public virtual string Cancel => "Cancel";



    // ===== BitTagsInput =====

    /// <summary>
    /// The name of the list of tags: the fallback of <see cref="BitTagsInput.TagsAriaLabel"/>.
    /// </summary>
    public virtual string Tags => "Tags";

    /// <summary>
    /// The name of the clear button: the fallback of <see cref="BitTagsInput.ClearButtonAriaLabel"/>.
    /// </summary>
    public virtual string ClearAllTags => "Clear all tags";

    /// <summary>
    /// The tooltip of the reorder handle of a tag: the fallback of <see cref="BitTagsInput.ReorderTitle"/>.
    /// </summary>
    public virtual string Move => "Move";

    /// <summary>
    /// The name of the reorder handle of a tag, <c>{0}</c> being the tag: the fallback of
    /// <see cref="BitTagsInput.ReorderAriaLabelFormat"/>.
    /// </summary>
    public virtual string MoveFormat => "Move {0}";

    /// <summary>
    /// The name of the reorder handle of a tag while another one is carried, <c>{0}</c> being the carried tag: the
    /// fallback of <see cref="BitTagsInput.ReorderDropAriaLabelFormat"/>.
    /// </summary>
    public virtual string MoveHereFormat => "Move {0} here";

    /// <summary>
    /// The name of the edit button of a tag, <c>{0}</c> being the tag: the fallback of
    /// <see cref="BitTagsInput.EditAriaLabelFormat"/>.
    /// </summary>
    public virtual string EditFormat => "Edit {0}";

    /// <summary>
    /// The text of the button that shows the hidden tags, <c>{0}</c> being how many: the fallback of
    /// <see cref="BitTagsInput.MoreTagsFormat"/>.
    /// </summary>
    public virtual string MoreTagsFormat => "+{0}";

    /// <summary>
    /// The name of the button that shows the hidden tags, <c>{0}</c> being how many: the fallback of
    /// <see cref="BitTagsInput.MoreTagsAriaLabelFormat"/>.
    /// </summary>
    public virtual string ShowMoreTagsFormat => "Show {0} more tags";

    /// <summary>
    /// The text of the button that hides the tags again: the fallback of <see cref="BitTagsInput.LessTagsText"/>.
    /// </summary>
    public virtual string ShowLess => "Show less";



    // ===== BitBreadcrumb, BitPagination and BitPivot =====

    /// <summary>
    /// The name of the navigation landmark of a breadcrumb: the fallback of <see cref="BitComponentBase.AriaLabel"/>
    /// on a <see cref="BitBreadcrumb{TItem}"/>.
    /// </summary>
    public virtual string Breadcrumb => "Breadcrumb";

    /// <summary>
    /// The name of the overflow button of a breadcrumb: the fallback of <see cref="BitBreadcrumb{TItem}.OverflowAriaLabel"/>.
    /// </summary>
    public virtual string MoreItems => "More items";

    /// <summary>
    /// The name of the navigation landmark of a pagination: the fallback of <see cref="BitComponentBase.AriaLabel"/>
    /// on a <see cref="BitPagination"/>.
    /// </summary>
    public virtual string Pagination => "Pagination";

    /// <summary>
    /// The name of a page button, <c>{0}</c> being the page number: the fallback of
    /// <see cref="BitPagination.GetPageAriaLabel"/>.
    /// </summary>
    public virtual string PageFormat => "Page {0}";

    /// <summary>
    /// The name of the overflow button of a pivot: the fallback of <see cref="BitPivot.OverflowAriaLabel"/>.
    /// </summary>
    public virtual string More => "More";

    /// <summary>
    /// The name of the scroll-back button of a pivot: the fallback of <see cref="BitPivot.PreviousAriaLabel"/>.
    /// </summary>
    public virtual string Previous => "Previous";

    /// <summary>
    /// The name of the scroll-forward button of a pivot: the fallback of <see cref="BitPivot.NextAriaLabel"/>.
    /// </summary>
    public virtual string Next => "Next";



    // ===== BitStepper =====

    /// <summary>
    /// What a screen reader is told after the text of a completed step of a <see cref="BitStepper{TItem}"/>.
    /// </summary>
    public virtual string StepCompleted => "Completed";

    /// <summary>
    /// What a screen reader is told after the text of a step of a <see cref="BitStepper{TItem}"/> that is in error.
    /// </summary>
    public virtual string StepError => "Error";



    // ===== BitSwiper =====

    /// <summary>
    /// The name of the previous button of a swiper: the fallback of <see cref="BitSwiper.PrevAriaLabel"/>.
    /// </summary>
    public virtual string PreviousSlide => "Previous slide";

    /// <summary>
    /// The name of the next button of a swiper: the fallback of <see cref="BitSwiper.NextAriaLabel"/>.
    /// </summary>
    public virtual string NextSlide => "Next slide";

    // ===== BitPanel =====

    /// <summary>
    /// The name of the grabber of a bottom sheet, which says the size it is at: the fallback of
    /// <see cref="BitPanel.GrabberAriaLabelFormat"/>. {0} is the snap point the sheet is at, counted from 1, and {1} how
    /// many there are.
    /// </summary>
    public virtual string PanelSizeFormat => "Resize, size {0} of {1}";
}
