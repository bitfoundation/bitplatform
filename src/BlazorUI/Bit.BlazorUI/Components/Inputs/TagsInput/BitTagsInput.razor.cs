using System.Text.RegularExpressions;
using System.Diagnostics.CodeAnalysis;

namespace Bit.BlazorUI;

/// <summary>
/// A TagsInput (also known as a chips or token input) turns free text into a list of short values: the user
/// types a tag, confirms it with the Enter key (or with a separator character of your own), and it becomes a
/// removable chip sitting in front of the input. It binds to an <see cref="ICollection{T}"/> of strings, splits
/// pasted text over its separators, caps the number of tags and the length of each of them, rejects the ones
/// that fail a pattern or a validator of yours, normalizes them through a transformer, and reports every one of
/// those rejections. The chips form an accessible list that the arrow keys walk through, each of them removable
/// with the keyboard as well as with its dismiss button, correctable in place and movable within the list, with
/// a long list folded away behind a chip rather than grown into a wall. Every change can be watched or called
/// off before it happens, the whole component can be driven from code, and the whole thing takes part in an
/// EditForm like any other input.
/// </summary>
public partial class BitTagsInput : BitInputBase<ICollection<string>?>
{
    private static readonly string[] _emptySeparators = [];

    [Inject] private IJSRuntime _js { get; set; } = default!;

    private bool _hasFocus;
    private bool _syncInputValue;
    private string _inputText = string.Empty;
    private string _inputId = string.Empty;
    private string _labelId = string.Empty;
    private string _listId = string.Empty;
    private string _tagsId = string.Empty;
    private string _hintId = string.Empty;
    private string _descriptionId = string.Empty;
    private bool _tagsExpanded;
    private string? _separatorsJson;
    private string[] _separators = _emptySeparators;
    private Regex? _patternRegex;
    private string? _announcement;

    // A screen reader only re-reads a live region that actually changed, so the very same message said
    // twice in a row (the same tag rejected again, the same one added twice) would go unheard. The id
    // keys the element the message sits in, which makes every announcement replace an element rather
    // than only rewrite a text that may not have changed.
    private int _announcementId;

    // The tag that currently owns the single tab stop of the list (roving tabindex), and the one the
    // focus has to be moved to after the next render, once the element it belongs to exists again.
    private int _focusedTagIndex = -1;
    private int _pendingFocusTagIndex = -1;
    private bool _pendingFocusInput;
    private ElementReference[] _tagRefs = [];

    // The tag that is being edited in place, along with the text of the little input that replaces it.
    private int _editingTagIndex = -1;
    private string _editText = string.Empty;
    private bool _pendingFocusEdit;
    private ElementReference _editInputRef;

    // The tag being dragged and the one it is currently hovering over, which is what the pointer
    // equivalent of the Alt+arrow reordering is drawn and carried out with.
    private int _draggingTagIndex = -1;
    private int _dragOverTagIndex = -1;



    /// <summary>
    /// The accent color of the tags input, used for its focus ring and for the border of the focused field
    /// (Primary by default).
    /// </summary>
    [Parameter, ResetClassBuilder]
    public BitColor? Accent { get; set; }

    /// <summary>
    /// Lets a tag be moved within the list, either by dragging it onto the position it should take or
    /// from the keyboard: Alt with the arrow keys walks the focused tag one position at a time, and Alt
    /// with Home or End sends it to either end. The keyboard is not a fallback of the drag but its equal,
    /// which is what keeps the reordering usable where a drag is not (a touch screen, a screen reader),
    /// and the focus stays on the tag that moved so that several steps can be taken in a row.
    /// </summary>
    [Parameter] public bool AllowReorder { get; set; }

    /// <summary>
    /// The format of the message announced by screen readers when a tag is added, where {0} is the tag.
    /// The default is "{0} added.". Set it to an empty string to keep the addition from being announced.
    /// </summary>
    [Parameter] public string? AddedAnnouncementFormat { get; set; }

    /// <summary>
    /// The format of the message announced by screen readers when several tags are added at once (a
    /// pasted list, most of the time), where {0} is how many of them there were. The default is
    /// "{0} tags added." - a count rather than the tags themselves, since reading fifty names out is not
    /// a confirmation but a wall. Set it to an empty string to keep the addition from being announced.
    /// A single tag is always announced with <see cref="AddedAnnouncementFormat"/> instead.
    /// </summary>
    [Parameter] public string? AddedManyAnnouncementFormat { get; set; }

    /// <summary>
    /// Whether the input should receive focus on first render.
    /// </summary>
    [Parameter] public bool AutoFocus { get; set; }

    /// <summary>
    /// When set to true, pressing Enter (or a confirm key) while the input is empty will not be
    /// suppressed, allowing the event to propagate (e.g., to submit a form).
    /// </summary>
    [Parameter] public bool CancelConfirmKeysOnEmpty { get; set; }

    /// <summary>
    /// Custom CSS classes for different parts of the component.
    /// </summary>
    [Parameter] public BitTagsInputClassStyles? Classes { get; set; }

    /// <summary>
    /// The format of the message announced by screen readers when every tag is removed at once, where
    /// {0} is how many of them there were. The default is "{0} tags removed." - a count rather than the
    /// tags themselves, since reading fifty names out is not a confirmation but a wall. Set it to an
    /// empty string to keep the clearing from being announced.
    /// </summary>
    [Parameter] public string? ClearedAnnouncementFormat { get; set; }

    /// <summary>
    /// Accessible label of the clear button, for the benefit of screen readers and of localization.
    /// The default is "Clear all tags".
    /// </summary>
    [Parameter] public string? ClearButtonAriaLabel { get; set; }

    /// <summary>
    /// Gets or sets the icon of the clear button using custom CSS classes for external icon libraries.
    /// Takes precedence over <see cref="ClearButtonIconName"/> when both are set.
    /// </summary>
    [Parameter] public BitIconInfo? ClearButtonIcon { get; set; }

    /// <summary>
    /// Gets or sets the name of the icon of the clear button from the built-in Fluent UI icons.
    /// Defaults to Clear when not set.
    /// </summary>
    [Parameter] public string? ClearButtonIconName { get; set; }

    /// <summary>
    /// The tooltip of the clear button, which is what the pointer reads rather than the screen reader.
    /// It falls back to the <see cref="ClearButtonAriaLabel"/> and then to "Clear all tags".
    /// </summary>
    [Parameter] public string? ClearButtonTitle { get; set; }

    /// <summary>
    /// The string comparison used to tell one tag from another, which is what decides whether a tag is a
    /// duplicate of one that is already in the list. It defaults to <see cref="StringComparison.Ordinal"/>,
    /// so "Blazor" and "blazor" are two different tags; pass
    /// <see cref="StringComparison.OrdinalIgnoreCase"/> to treat them as one.
    /// </summary>
    [Parameter] public StringComparison Comparison { get; set; } = StringComparison.Ordinal;

    /// <summary>
    /// A hint rendered under the field, describing what is expected of it (the accepted format of a tag,
    /// how many of them are allowed). The input references it through its aria-describedby attribute, so
    /// it is announced along with the field rather than only being shown.
    /// </summary>
    [Parameter] public string? Description { get; set; }

    /// <summary>
    /// A custom template rendered in place of the <see cref="Description"/>, referenced by the input
    /// through its aria-describedby attribute just the same.
    /// </summary>
    [Parameter] public RenderFragment? DescriptionTemplate { get; set; }

    /// <summary>
    /// The format of the accessible label of the dismiss button of each tag, where {0} is the tag.
    /// The default is "Remove {0}".
    /// </summary>
    [Parameter] public string? DismissAriaLabelFormat { get; set; }

    /// <summary>
    /// Gets or sets the icon for the dismiss button using custom CSS classes for external icon libraries.
    /// Takes precedence over <see cref="DismissIconName"/> when both are set.
    /// </summary>
    [Parameter] public BitIconInfo? DismissIcon { get; set; }

    /// <summary>
    /// Gets or sets the name of the icon for the dismiss button from the built-in Fluent UI icons.
    /// Defaults to Cancel when not set.
    /// </summary>
    [Parameter] public string? DismissIconName { get; set; }

    /// <summary>
    /// The title (tooltip) of the dismiss button of each tag. The default is "Remove".
    /// </summary>
    [Parameter] public string? DismissTitle { get; set; }

    /// <summary>
    /// Whether duplicate tags are allowed. Which tags count as duplicates of one another is decided by
    /// the <see cref="Comparison"/>.
    /// </summary>
    [Parameter] public bool Duplicates { get; set; }

    /// <summary>
    /// Lets a tag be corrected in place instead of having to be removed and typed again: double clicking
    /// a tag (or pressing Enter or F2 on the focused one) turns it into a little input, Enter commits the
    /// new text and Escape puts the old one back. The text goes through the very same trimming,
    /// transformation and validation as a tag being added, committing an empty one removes the tag, and
    /// <see cref="OnEdit"/> can call the change off.
    /// </summary>
    [Parameter] public bool EditableTags { get; set; }

    /// <summary>
    /// The format of the accessible label of the little input that replaces a tag while it is being
    /// edited in place, where {0} is the tag. The default is "Edit {0}".
    /// </summary>
    [Parameter] public string? EditAriaLabelFormat { get; set; }

    /// <summary>
    /// The format of the message announced by screen readers when a tag is edited, where {0} is the tag
    /// as it now reads. The default is "{0} updated.". An empty string keeps the edit from being announced.
    /// </summary>
    [Parameter] public string? EditedAnnouncementFormat { get; set; }

    /// <summary>
    /// The format of the message announced by screen readers when a tag is rejected, where {0} is the tag.
    /// The default is "{0} was not added.". Set it to an empty string to keep the rejection from being announced.
    /// </summary>
    [Parameter] public string? InvalidAnnouncementFormat { get; set; }

    /// <summary>
    /// The label displayed above the input.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public string? Label { get; set; }

    /// <summary>
    /// A custom template for the label.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public RenderFragment? LabelTemplate { get; set; }

    /// <summary>
    /// The number of tags drawn before the rest of them are folded away behind a chip that says how
    /// many are left, which is what keeps a field holding dozens of them from growing into a wall of
    /// chips. The chip unfolds the list and folds it back, so nothing is ever out of reach; the value
    /// itself is untouched, only how much of it is drawn. 0 means all of them.
    /// </summary>
    [Parameter] public int MaxDisplayedTags { get; set; }

    /// <summary>
    /// The maximum number of characters allowed for each individual tag. Text beyond it is truncated
    /// rather than rejected, both while typing and while pasting. 0 means no limit.
    /// </summary>
    [Parameter] public int MaxLength { get; set; }

    /// <summary>
    /// The maximum number of tags allowed. Once it is reached, further tags are rejected with the
    /// <see cref="BitTagsInputInvalidReason.MaxTags"/> reason. 0 means no limit.
    /// </summary>
    [Parameter] public int MaxTags { get; set; }

    /// <summary>
    /// The minimum number of characters a tag has to hold to be accepted. Shorter ones are rejected with
    /// the <see cref="BitTagsInputInvalidReason.MinLength"/> reason. 0 means no limit.
    /// </summary>
    [Parameter] public int MinLength { get; set; }

    /// <summary>
    /// The label of the chip that folds the tags back once <see cref="MaxDisplayedTags"/> unfolded them.
    /// The default is "Show less".
    /// </summary>
    [Parameter] public string? LessTagsText { get; set; }

    /// <summary>
    /// The format of the label of the chip that stands for the tags <see cref="MaxDisplayedTags"/> folded
    /// away, where {0} is how many of them there are. The default is "+{0}".
    /// </summary>
    [Parameter] public string? MoreTagsFormat { get; set; }

    /// <summary>
    /// The format of the accessible label of that same chip, where {0} is how many tags are folded away.
    /// The default is "Show {0} more tags", since "+3" read out on its own says nothing about what
    /// pressing it would do.
    /// </summary>
    [Parameter] public string? MoreTagsAriaLabelFormat { get; set; }

    /// <summary>
    /// Stops the text left in the input from being committed as a tag when the field loses the focus,
    /// so that a tag is only ever added by a key the user pressed on purpose.
    /// </summary>
    [Parameter] public bool NoAddOnBlur { get; set; }

    /// <summary>
    /// Stops the Tab key from committing the text left in the input, leaving it to do nothing but move
    /// the focus to the next element, as it does everywhere else.
    /// </summary>
    [Parameter] public bool NoAddOnTab { get; set; }

    /// <summary>
    /// Stops the Backspace key from removing the last tag when the input is empty.
    /// </summary>
    [Parameter] public bool NoBackspaceRemove { get; set; }

    /// <summary>
    /// Whether the input should have no border.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public bool NoBorder { get; set; }

    /// <summary>
    /// Keeps the leading and trailing whitespace of a tag instead of trimming it away.
    /// </summary>
    [Parameter] public bool NoTrim { get; set; }

    /// <summary>
    /// Callback invoked before a tag is added. Set <c>args.Cancel = true</c> to cancel the add.
    /// </summary>
    [Parameter] public EventCallback<BitTagsInputBeforeArgs> OnBeforeAdd { get; set; }

    /// <summary>
    /// Callback invoked before a tag is removed. Set <c>args.Cancel = true</c> to cancel the remove.
    /// </summary>
    [Parameter] public EventCallback<BitTagsInputBeforeArgs> OnBeforeRemove { get; set; }

    /// <summary>
    /// Callback invoked before every tag is removed at once, by the clear button, the Escape key or the
    /// <see cref="Clear"/> method, carrying the whole list that is about to go. Set
    /// <c>args.Cancel = true</c> to leave it as it is, which is what a confirmation is asked from.
    /// </summary>
    [Parameter] public EventCallback<BitTagsInputClearArgs> OnBeforeClear { get; set; }

    /// <summary>
    /// Callback for when one or more tags are added. Receives the list of all newly added tags.
    /// </summary>
    [Parameter] public EventCallback<IReadOnlyList<string>> OnAdd { get; set; }

    /// <summary>
    /// Callback for when every tag is removed at once, by the clear button or by the
    /// <see cref="Clear"/> method. It receives the tags that were removed.
    /// </summary>
    [Parameter] public EventCallback<IReadOnlyList<string>> OnClear { get; set; }

    /// <summary>
    /// Callback fired when a duplicate tag entry is attempted (and <see cref="Duplicates"/> is false).
    /// </summary>
    [Parameter] public EventCallback<string> OnTagExists { get; set; }

    /// <summary>
    /// Callback invoked when an inline edit of a tag is about to be committed, carrying both the old and
    /// the new text. Set <c>args.Cancel = true</c> to leave the tag as it was.
    /// </summary>
    [Parameter] public EventCallback<BitTagsInputEditArgs> OnEdit { get; set; }

    /// <summary>
    /// Callback for when a tag is removed.
    /// </summary>
    [Parameter] public EventCallback<string> OnRemove { get; set; }

    /// <summary>
    /// Callback for when a tag is moved within the list with <see cref="AllowReorder"/>, carrying the
    /// tag along with the positions it left and took. The whole list is reported through the value as
    /// well; this is what tells which of the tags moved and where to, without diffing two lists.
    /// </summary>
    [Parameter] public EventCallback<BitTagsInputReorderArgs> OnReorder { get; set; }

    /// <summary>
    /// Callback for when the input receives focus.
    /// </summary>
    [Parameter] public EventCallback<FocusEventArgs> OnFocusIn { get; set; }

    /// <summary>
    /// Callback for when the input loses focus.
    /// </summary>
    [Parameter] public EventCallback<FocusEventArgs> OnFocusOut { get; set; }

    /// <summary>
    /// Callback for when the text of the input changes, which is what an external suggestion list is
    /// driven by. It receives the text the input holds after the separators and the
    /// <see cref="MaxLength"/> have been applied to it.
    /// </summary>
    [Parameter] public EventCallback<string> OnInput { get; set; }

    /// <summary>
    /// Callback for when a tag is rejected, carrying the tag along with the rule that rejected it: the
    /// duplicate check, the tag count, the length, the pattern or the validator. It is what turns a
    /// silent rejection into a visible one, a message or a shake of the field.
    /// </summary>
    [Parameter] public EventCallback<BitTagsInputInvalidArgs> OnInvalid { get; set; }

    /// <summary>
    /// Callback for when a key is pressed down on the input. It is invoked for every key, including the
    /// ones the component handles itself (Enter, Tab, Backspace and the separator characters).
    /// </summary>
    [Parameter] public EventCallback<KeyboardEventArgs> OnKeyDown { get; set; }

    /// <summary>
    /// The format of the message announced by screen readers when a tag is moved with
    /// <see cref="AllowReorder"/>, where {0} is the tag, {1} its new one based position and {2} the
    /// number of tags. The default is "{0} moved to position {1} of {2}.". An empty string keeps the
    /// move from being announced.
    /// </summary>
    [Parameter] public string? MovedAnnouncementFormat { get; set; }

    /// <summary>
    /// A regular expression that every tag has to match to be accepted. Tags that do not match are
    /// rejected with the <see cref="BitTagsInputInvalidReason.Pattern"/> reason. An unusable expression
    /// is ignored rather than breaking the input.
    /// </summary>
    [Parameter]
    [CallOnSet(nameof(OnSetPattern))]
    public string? Pattern { get; set; }

    /// <summary>
    /// The placeholder text of the input, shown while there is no tag in the list. Use
    /// <see cref="TagsPlaceholder"/> for the hint that should be shown once there are tags.
    /// </summary>
    [Parameter] public string? Placeholder { get; set; }

    /// <summary>
    /// A short text drawn at the start of the field, in front of the tags, which is not part of the
    /// value: the "to:" of a recipients field, the "#" of a hashtag one. Since it never reaches the
    /// value, the label of the field has to say what it means on its own for a screen reader.
    /// </summary>
    [Parameter] public string? Prefix { get; set; }

    /// <summary>
    /// A custom template drawn in place of the <see cref="Prefix"/>.
    /// </summary>
    [Parameter] public RenderFragment? PrefixTemplate { get; set; }

    /// <summary>
    /// The format of the message announced by screen readers when a tag is removed, where {0} is the tag.
    /// The default is "{0} removed.". Set it to an empty string to keep the removal from being announced.
    /// </summary>
    [Parameter] public string? RemovedAnnouncementFormat { get; set; }

    /// <summary>
    /// Turns the <see cref="Suggestions"/> from a convenience into the whole of what the field accepts:
    /// a tag that is not one of them is rejected with the
    /// <see cref="BitTagsInputInvalidReason.NotSuggested"/> reason, which is what a datalist on its own
    /// cannot do, since it suggests rather than restricts. The <see cref="Comparison"/> decides what
    /// counts as one of the suggestions, and no suggestions at all means nothing is accepted.
    /// </summary>
    [Parameter] public bool RestrictToSuggestions { get; set; }

    /// <summary>
    /// The character(s) that turn the typed text into a tag on top of the Enter key, which is the only
    /// one there is by default. Typing one of them commits whatever stands before it and the character
    /// itself never reaches the input, and the very same separators split a pasted list into a tag each.
    /// A pasted text holding line breaks is joined over the first separator before it is split, so a
    /// column copied out of a spreadsheet arrives as a row of tags rather than as a single run-on one.
    /// </summary>
    [Parameter]
    [CallOnSet(nameof(OnSetSeparators))]
    public IEnumerable<string>? Separators { get; set; }

    /// <summary>
    /// Whether to render a button that removes every tag at once. It is not rendered while the component
    /// is read-only, disabled or empty, and it stays out of the tab order, the Escape key pressed on the
    /// input being its keyboard equivalent.
    /// </summary>
    [Parameter] public bool ShowClearButton { get; set; }

    /// <summary>
    /// Whether to render the number of tags under the field, next to the <see cref="Description"/>,
    /// as a plain count or as "count / <see cref="MaxTags"/>" when there is a ceiling to reach. It is
    /// drawn rather than announced, the list of tags itself being what a screen reader counts.
    /// </summary>
    [Parameter] public bool ShowCounter { get; set; }

    /// <summary>
    /// The size of the tags input.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public BitSize? Size { get; set; }

    /// <summary>
    /// The values offered to the user while typing, through the suggestion list the browser itself
    /// renders for a datalist. Picking one fills the input with it, from where the usual Enter (or a
    /// separator) turns it into a tag, so every validation rule still applies to it. The values already
    /// in the list are left out of the suggestions unless <see cref="Duplicates"/> allows them back in,
    /// and so are all of them once the <see cref="MaxTags"/> ceiling leaves nothing to add. A datalist
    /// suggests rather than restricts; <see cref="RestrictToSuggestions"/> is what makes these values
    /// the only ones the field accepts.
    /// </summary>
    [Parameter] public IEnumerable<string>? Suggestions { get; set; }

    /// <summary>
    /// Custom CSS styles for different parts of the component.
    /// </summary>
    [Parameter] public BitTagsInputClassStyles? Styles { get; set; }

    /// <summary>
    /// A short text drawn at the end of the field, after everything else, which is not part of the
    /// value. Since it never reaches the value, the label of the field has to say what it means on its
    /// own for a screen reader.
    /// </summary>
    [Parameter] public string? Suffix { get; set; }

    /// <summary>
    /// A custom template drawn in place of the <see cref="Suffix"/>.
    /// </summary>
    [Parameter] public RenderFragment? SuffixTemplate { get; set; }

    /// <summary>
    /// The accessible name of the list the tags form, which is what a screen reader announces before
    /// walking through them ("Tags, list, 3 items"). The default is "Tags".
    /// </summary>
    [Parameter] public string? TagsAriaLabel { get; set; }

    /// <summary>
    /// The sentence announced after each tag, telling what the keyboard can do with the one that has
    /// just been reached - which is the only way those gestures are ever discovered, a chip looking
    /// like nothing but a word. It defaults to a sentence built from what the component was actually
    /// given (the inline edit, the reordering), and is left out entirely when neither is on, so that a
    /// plain list of chips is not read out with instructions it has no use for. An empty string keeps
    /// it from being rendered at all.
    /// </summary>
    [Parameter] public string? TagAriaDescription { get; set; }

    /// <summary>
    /// A custom template for rendering each tag.
    /// </summary>
    [Parameter] public RenderFragment<string>? TagTemplate { get; set; }

    /// <summary>
    /// The placeholder text of the input shown once there is at least one tag in the list, where the
    /// <see cref="Placeholder"/> would otherwise be replaced by nothing at all. It is what keeps the
    /// invitation to add another tag visible ("add another...").
    /// </summary>
    [Parameter] public string? TagsPlaceholder { get; set; }

    /// <summary>
    /// A function applied to the text of a tag before anything else is done with it, which is what
    /// normalizes the tags of a list that has to stay consistent: lower casing them, stripping a leading
    /// "#", collapsing the inner whitespace. It runs before the length, pattern, validator and duplicate
    /// checks, so all of them see the normalized text, and it is the normalized text that is added.
    /// An exception thrown out of it leaves the text untouched rather than breaking the input.
    /// </summary>
    [Parameter] public Func<string, string>? Transformer { get; set; }

    /// <summary>
    /// A predicate every tag has to satisfy to be accepted, for the rules a regular expression cannot
    /// express (a lookup in a list of allowed values, a checksum, a length that depends on the other
    /// tags). Returning false rejects the tag with the
    /// <see cref="BitTagsInputInvalidReason.Validator"/> reason.
    /// </summary>
    [Parameter] public Func<string, bool>? Validator { get; set; }

    /// <summary>
    /// The visual variant of the field, which decides how much of the frame around it is painted:
    /// an outline (the default), a filled surface, or only an underline.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public BitVariant? Variant { get; set; }



    /// <summary>
    /// Adds a tag to the list exactly as typing it and pressing Enter does: the trimming, the
    /// <see cref="Transformer"/>, every validation rule and the OnBeforeAdd/OnAdd/OnInvalid callbacks
    /// all included. It does nothing while the component is disabled or read-only.
    /// </summary>
    public Task AddTagAsync(string tag) => AddTagsAsync([tag]);

    /// <summary>
    /// Adds several tags at once, exactly as pasting a separated list of them does. Each of them goes
    /// through the very same normalization and validation, so the ones that are rejected are reported
    /// through <see cref="OnInvalid"/> while the rest are still added. It does nothing while the
    /// component is disabled or read-only.
    /// </summary>
    public Task AddTagsAsync(IEnumerable<string> tags)
    {
        ArgumentNullException.ThrowIfNull(tags);

        return InvokeAsync(async () =>
        {
            if (IsEnabled is false || ReadOnly) return;

            await TryAddTags([.. tags]);

            // Unlike the event handlers, this call does not arrive through the renderer, so nothing
            // re-renders the component on its own.
            StateHasChanged();
        });
    }

    /// <summary>
    /// Removes the first tag equal to <paramref name="tag"/> (per the <see cref="Comparison"/>), exactly
    /// as its dismiss button does. It does nothing while the component is disabled or read-only.
    /// </summary>
    public Task RemoveTagAsync(string tag)
    {
        return InvokeAsync(async () =>
        {
            if (IsEnabled is false || ReadOnly) return;

            var index = GetTags().FindIndex(t => string.Equals(t, tag, Comparison));
            if (index < 0) return;

            await RemoveTagAt(index);

            StateHasChanged();
        });
    }

    /// <summary>
    /// Removes the tag sitting at <paramref name="index"/>, doing nothing when there is none there or
    /// while the component is disabled or read-only.
    /// </summary>
    public Task RemoveTagAtAsync(int index)
    {
        return InvokeAsync(async () =>
        {
            if (IsEnabled is false || ReadOnly) return;

            await RemoveTagAt(index);

            StateHasChanged();
        });
    }

    /// <summary>
    /// Moves the tag sitting at <paramref name="from"/> to <paramref name="to"/>, exactly as dragging it
    /// there or walking it with Alt and the arrow keys does, raising <see cref="OnReorder"/> along with
    /// it. It does nothing when either position is outside the list, when the two are the same, or while
    /// the component is disabled or read-only. Unlike the gestures, it does not require
    /// <see cref="AllowReorder"/>: the parameter is what offers the reordering to the user, while this
    /// is the consumer reordering the list itself.
    /// </summary>
    public Task MoveTagAsync(int from, int to)
    {
        return InvokeAsync(async () =>
        {
            if (IsEnabled is false || ReadOnly) return;

            await MoveTag(from, to);

            StateHasChanged();
        });
    }

    /// <summary>
    /// Sets the text of the input, which is what fills the field from a suggestion list of your own
    /// driven by <see cref="OnInput"/> - and what empties it again once the pick has been turned into a
    /// tag. The <see cref="MaxLength"/> is applied to it, and <see cref="OnInput"/> is raised with the
    /// text that was kept, exactly as typing it would. It does nothing while the component is disabled
    /// or read-only.
    /// </summary>
    public Task SetInputTextAsync(string? text)
    {
        return InvokeAsync(async () =>
        {
            if (IsEnabled is false || ReadOnly) return;

            _inputText = text ?? string.Empty;

            if (MaxLength > 0 && _inputText.Length > MaxLength)
            {
                _inputText = _inputText[..MaxLength];
            }

            // The element already holds whatever the user typed, and the renderer only writes an
            // attribute it sees changing, so the new text is pushed into it by hand.
            _syncInputValue = true;

            StateHasChanged();

            await OnInput.InvokeAsync(_inputText);
        });
    }

    /// <summary>
    /// Opens the inline edit of the tag sitting at <paramref name="index"/>, exactly as double clicking
    /// it does. It does nothing when there is no tag there, when <see cref="EditableTags"/> is off, or
    /// while the component is disabled or read-only.
    /// </summary>
    public Task EditTagAsync(int index)
    {
        return InvokeAsync(() =>
        {
            StartEdit(index);

            StateHasChanged();
        });
    }

    /// <summary>
    /// Removes all tags along with the text left in the input, and raises <see cref="OnClear"/> with the
    /// tags that were removed. It does nothing while the component is disabled or read-only.
    /// </summary>
    public Task Clear() => InvokeAsync(async () =>
    {
        if (IsEnabled is false || ReadOnly) return;

        var removed = GetTags();

        if (OnBeforeClear.HasDelegate)
        {
            var args = new BitTagsInputClearArgs { Tags = removed };
            await OnBeforeClear.InvokeAsync(args);
            if (args.Cancel) return;
        }

        _inputText = string.Empty;
        _syncInputValue = true;
        _focusedTagIndex = -1;
        _tagsExpanded = false;
        _editingTagIndex = -1;
        _draggingTagIndex = -1;
        _dragOverTagIndex = -1;

        // A field that is already empty has nothing to report: setting the value again would otherwise
        // mark the form dirty and raise a change for a list that did not change.
        if (removed.Count > 0 || CurrentValue is not null)
        {
            await SetCurrentValueAsync(null);
        }

        StateHasChanged();

        if (removed.Count > 0)
        {
            Announce(ClearedAnnouncementFormat ?? "{0} tags removed.", removed.Count);

            await OnClear.InvokeAsync(removed);
        }
    });



    protected override string RootElementClass => "bit-tgi";

    protected override void RegisterCssClasses()
    {
        ClassBuilder.Register(() => Classes?.Root);

        ClassBuilder.Register(() => Variant switch
        {
            BitVariant.Fill => "bit-tgi-fil",
            BitVariant.Outline => "bit-tgi-otl",
            BitVariant.Text => "bit-tgi-txt",
            _ => "bit-tgi-otl"
        });

        ClassBuilder.Register(() => Accent switch
        {
            BitColor.Primary => "bit-tgi-pri",
            BitColor.Secondary => "bit-tgi-sec",
            BitColor.Tertiary => "bit-tgi-ter",
            BitColor.Info => "bit-tgi-inf",
            BitColor.Success => "bit-tgi-suc",
            BitColor.Warning => "bit-tgi-wrn",
            BitColor.SevereWarning => "bit-tgi-swr",
            BitColor.Error => "bit-tgi-err",
            BitColor.PrimaryBackground => "bit-tgi-pbg",
            BitColor.SecondaryBackground => "bit-tgi-sbg",
            BitColor.TertiaryBackground => "bit-tgi-tbg",
            BitColor.PrimaryForeground => "bit-tgi-pfg",
            BitColor.SecondaryForeground => "bit-tgi-sfg",
            BitColor.TertiaryForeground => "bit-tgi-tfg",
            BitColor.PrimaryBorder => "bit-tgi-pbr",
            BitColor.SecondaryBorder => "bit-tgi-sbr",
            BitColor.TertiaryBorder => "bit-tgi-tbr",
            _ => "bit-tgi-pri"
        });

        ClassBuilder.Register(() => Size switch
        {
            BitSize.Small => "bit-tgi-sm",
            BitSize.Medium => "bit-tgi-md",
            BitSize.Large => "bit-tgi-lg",
            _ => "bit-tgi-md"
        });

        ClassBuilder.Register(() => NoBorder ? "bit-tgi-nbd" : string.Empty);

        ClassBuilder.Register(() => ReadOnly ? "bit-tgi-rdl" : string.Empty);

        ClassBuilder.Register(() => IsEnabled && Required && (Label.HasValue() || LabelTemplate is not null) ? "bit-tgi-req" : string.Empty);

        ClassBuilder.Register(() => _hasFocus ? $"bit-tgi-fcs {Classes?.Focused}" : string.Empty);
    }

    protected override void RegisterCssStyles()
    {
        StyleBuilder.Register(() => Styles?.Root);
        StyleBuilder.Register(() => _hasFocus ? Styles?.Focused : string.Empty);
    }

    protected override async Task OnInitializedAsync()
    {
        _inputId = $"BitTagsInput-{UniqueId}-input";
        _labelId = $"BitTagsInput-{UniqueId}-label";
        _listId = $"BitTagsInput-{UniqueId}-list";
        _tagsId = $"BitTagsInput-{UniqueId}-tags";
        _hintId = $"BitTagsInput-{UniqueId}-hint";
        _descriptionId = $"BitTagsInput-{UniqueId}-description";

        SetDefaultValue();

        await base.OnInitializedAsync();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);

        if (firstRender)
        {
            try
            {
                await _js.BitTagsInputSetup(InputElement);

                // The chips answer to keys the browser acts on long before the .NET side is reached
                // (Home scrolls the page, Alt with an arrow leaves it altogether), which one listener
                // on the root holds back for all of them.
                await _js.BitTagsInputSetupTags(RootElement);
            }
            catch { } // JS is unavailable (e.g. a prerender or a disconnected circuit); the keys still work, only with their browser default side effects.

            if (AutoFocus && IsEnabled)
            {
                await InputElement.FocusAsync();
            }
        }

        // The input is rendered with value="@_inputText", so Blazor only writes it to the DOM when it
        // differs from what it rendered the previous time. A keystroke the component refused to keep -
        // a character beyond the MaxLength, or the part of a paste that was turned into tags - leaves
        // _inputText unchanged while the browser has already put the typed text into the element, and
        // that divergence is what this pushes back.
        if (_syncInputValue)
        {
            _syncInputValue = false;

            try
            {
                await _js.BitUtilsSetProperty(InputElement, "value", _inputText);
            }
            catch { }
        }

        // The three requests are exclusive by construction (each of the members that arms one clears the
        // other two), and all of them are cleared here whichever one is honored, so that a request left
        // over from a render that never happened cannot steal the focus later on.
        var focusEdit = _pendingFocusEdit;
        var focusInput = _pendingFocusInput;
        var focusTagIndex = _pendingFocusTagIndex;

        _pendingFocusEdit = false;
        _pendingFocusInput = false;
        _pendingFocusTagIndex = -1;

        try
        {
            if (focusEdit)
            {
                // The little input needs the very same Enter and IME handling as the main one: without
                // it, confirming a correction inside an EditForm submits the form, and the Enter that
                // picks a candidate out of an input method window commits the tag half way through a word.
                try
                {
                    await _js.BitTagsInputSetup(_editInputRef, isEdit: true);
                }
                catch { }

                await _editInputRef.FocusAsync();

                // The whole text is selected so that the correction can simply be typed over, which is
                // what an inline rename does everywhere else.
                await _js.BitUtilsSelectText(_editInputRef);
            }
            else if (focusInput)
            {
                await InputElement.FocusAsync();
            }
            else if (focusTagIndex >= 0 && focusTagIndex < _tagRefs.Length)
            {
                await _tagRefs[focusTagIndex].FocusAsync();
            }
        }
        catch { } // the element is gone or JS is unavailable; the focus simply stays where it is.
    }

    protected override bool TryParseValueFromString(string? value, out ICollection<string>? result, [NotNullWhen(false)] out string? parsingErrorMessage)
    {
        // The string form of the value is the tags joined by the first separator (a comma when none is
        // configured), so parsing it back has to split on the very same one.
        var separator = _separators.Length > 0 ? _separators[0] : ",";

        result = value?.Split(separator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
        parsingErrorMessage = null;
        return true;
    }

    protected override string? FormatValueAsString(ICollection<string>? value)
    {
        var separator = _separators.Length > 0 ? _separators[0] : ",";

        return value is not null ? string.Join(separator, value) : null;
    }



    private void OnSetSeparators()
    {
        // Only the empty string is dropped, not the whitespace: a single space is a perfectly ordinary
        // separator (a field of words), and so is a tab or a comma followed by one.
        _separators = Separators is null ? _emptySeparators : [.. Separators.Where(s => string.IsNullOrEmpty(s) is false)];

        // Hand-written rather than serialized: the payload is a flat array of strings, and the reflection
        // based serializer would otherwise drag a trimming and an AOT warning into the whole library.
        _separatorsJson = _separators.Length > 0
            ? $"[{string.Join(',', _separators.Select(EncodeJsonString))}]"
            : null;
    }

    /// <summary>
    /// Renders a separator as a JSON string literal for the data attribute the script side reads.
    /// </summary>
    private static string EncodeJsonString(string value)
    {
        var sb = new System.Text.StringBuilder(value.Length + 2);

        sb.Append('"');

        foreach (var c in value)
        {
            switch (c)
            {
                case '"': sb.Append("\\\""); break;
                case '\\': sb.Append("\\\\"); break;
                case '\b': sb.Append("\\b"); break;
                case '\f': sb.Append("\\f"); break;
                case '\n': sb.Append("\\n"); break;
                case '\r': sb.Append("\\r"); break;
                case '\t': sb.Append("\\t"); break;
                default:
                    // Control characters have no literal form of their own in JSON, and everything
                    // above them is left as is so that a non-Latin separator survives untouched.
                    if (c < ' ')
                    {
                        sb.Append("\\u").Append(((int)c).ToString("x4", System.Globalization.CultureInfo.InvariantCulture));
                    }
                    else
                    {
                        sb.Append(c);
                    }
                    break;
            }
        }

        sb.Append('"');

        return sb.ToString();
    }

    private void OnSetPattern()
    {
        if (Pattern.HasNoValue())
        {
            _patternRegex = null;
            return;
        }

        try
        {
            // The timeout keeps a pathological expression from hanging the render loop of the whole
            // circuit; a tag whose match times out is treated as one that does not match.
            _patternRegex = new Regex(Pattern!, RegexOptions.None, TimeSpan.FromSeconds(1));
        }
        catch
        {
            _patternRegex = null;
        }
    }

    private List<string> GetTags() => CurrentValue is null ? [] : [.. CurrentValue];

    /// <summary>
    /// The values the datalist offers: the <see cref="Suggestions"/> minus the ones that are already in
    /// the list, since offering a value that would only be refused as a duplicate is a dead end - and
    /// none at all once the ceiling is reached, where every one of them would be refused.
    /// </summary>
    private List<string> GetSuggestions()
    {
        if (Suggestions is null) return [];

        if (MaxTags > 0 && (CurrentValue?.Count ?? 0) >= MaxTags) return [];

        var suggestions = Suggestions.Where(s => s.HasValue());

        if (Duplicates is false && CurrentValue is not null && CurrentValue.Count > 0)
        {
            // A set rather than a scan of the list per suggestion: both of them are drawn on every
            // render, and the product of the two is what a long list of either would otherwise cost.
            var tags = new HashSet<string>(GetTags(), StringComparer.FromComparison(Comparison));

            suggestions = suggestions.Where(s => tags.Contains(s) is false);
        }

        return [.. suggestions];
    }

    private bool IsRtl => Dir == BitDir.Rtl;

    /// <summary>
    /// How many of the tags are actually drawn: all of them unless <see cref="MaxDisplayedTags"/> folded
    /// the rest away and the chip standing for them has not been used to unfold the list.
    /// </summary>
    private int GetDisplayedTagCount()
    {
        var count = CurrentValue?.Count ?? 0;

        if (MaxDisplayedTags <= 0 || _tagsExpanded || count <= MaxDisplayedTags) return count;

        return MaxDisplayedTags;
    }

    /// <summary>
    /// The sentence announced after each tag: the one the consumer gave, or one built from the gestures
    /// the component was actually given, since instructions for something that is off would be noise.
    /// </summary>
    private string? GetTagAriaDescription()
    {
        if (TagAriaDescription is not null) return TagAriaDescription.HasValue() ? TagAriaDescription : null;

        if (IsEnabled is false || ReadOnly) return null;

        return (EditableTags, AllowReorder) switch
        {
            (true, true) => "Press Enter to edit, Delete to remove, or Alt with the arrow keys to move.",
            (true, false) => "Press Enter to edit, or Delete to remove.",
            (false, true) => "Press Alt with the arrow keys to move, or Delete to remove.",
            _ => null
        };
    }

    private string GetMoreTagsText(int hidden)
    {
        return Format(MoreTagsFormat ?? "+{0}", hidden.ToString(System.Globalization.CultureInfo.CurrentCulture));
    }

    private string GetMoreTagsAriaLabel(int hidden)
    {
        return Format(MoreTagsAriaLabelFormat ?? "Show {0} more tags", hidden.ToString(System.Globalization.CultureInfo.CurrentCulture));
    }

    private string? GetPlaceholder()
    {
        return CurrentValue is null || CurrentValue.Count == 0 ? Placeholder : TagsPlaceholder;
    }

    private string GetDismissAriaLabel(string tag)
    {
        return Format(DismissAriaLabelFormat ?? "Remove {0}", tag);
    }

    private string GetEditAriaLabel(string tag)
    {
        return Format(EditAriaLabelFormat ?? "Edit {0}", tag);
    }

    /// <summary>
    /// Formats a label of the component, falling back to the tag alone when a format string of the
    /// consumer holds a placeholder that is not filled, so that a mistake in it cannot break the input.
    /// </summary>
    private static string Format(string format, string tag)
    {
        try
        {
            return string.Format(System.Globalization.CultureInfo.CurrentCulture, format, tag);
        }
        catch (FormatException)
        {
            return tag;
        }
    }

    private string? GetTagStyle(bool focused)
    {
        if (Styles is null) return null;

        if (focused is false || Styles.FocusedTag.HasNoValue()) return Styles.Tag;

        if (Styles.Tag.HasNoValue()) return Styles.FocusedTag;

        // The two are joined with a semicolon rather than with a space, since a style that omits its
        // trailing one would otherwise swallow the declaration appended after it.
        return $"{Styles.Tag!.TrimEnd().TrimEnd(';')};{Styles.FocusedTag}";
    }

    private string GetTagClass(int index, bool focused)
    {
        var custom = Classes?.Tag;
        var focusedClass = focused ? Classes?.FocusedTag : null;

        // The tag being dragged is faded out and the one it hovers over is marked, which is what tells
        // the pointer where the chip would land before it is let go of.
        var dragClass = _draggingTagIndex < 0
            ? null
            : index == _draggingTagIndex
                ? "bit-tgi-tag-drg"
                : index == _dragOverTagIndex ? "bit-tgi-tag-dro" : null;

        return $"bit-tgi-tag {custom} {focusedClass} {dragClass}".TrimEnd();
    }

    private string GetTagTabIndex(int index, int count)
    {
        if (IsEnabled is false) return "-1";

        // Roving tabindex: the list of tags is a single stop of the tab order, and the arrow keys move
        // between them from there. Without it every dismiss button would be a stop of its own, which
        // puts as many Tab presses between the user and the input as there are tags.
        var active = _focusedTagIndex >= 0 && _focusedTagIndex < count ? _focusedTagIndex : 0;

        return index == active ? "0" : "-1";
    }

    private void Announce(string? format, params object?[] args)
    {
        if (format.HasNoValue()) return;

        try
        {
            _announcement = string.Format(System.Globalization.CultureInfo.CurrentCulture, format!, args);
            _announcementId++;
        }
        catch (FormatException)
        {
            // A format string of the consumer holding a placeholder the component does not fill must
            // not be able to break the input; the announcement is dropped instead.
        }
    }

    private async Task ReportInvalid(string tag, BitTagsInputInvalidReason reason)
    {
        Announce(InvalidAnnouncementFormat ?? "{0} was not added.", tag);

        if (reason == BitTagsInputInvalidReason.Duplicate)
        {
            await OnTagExists.InvokeAsync(tag);
        }

        await OnInvalid.InvokeAsync(new() { Tag = tag, Reason = reason });
    }

    /// <summary>
    /// Applies the trimming, the <see cref="Transformer"/> and the <see cref="MaxLength"/> to the raw
    /// text, producing the tag that the validation rules and the list itself will see.
    /// </summary>
    private string NormalizeTag(string? raw)
    {
        // A null reaches this from the public API and from a collection holding one; it is the same
        // thing as an empty entry, which the callers drop rather than add.
        var text = raw is null ? string.Empty : NoTrim ? raw : raw.Trim();

        if (Transformer is not null)
        {
            try
            {
                text = Transformer(text) ?? string.Empty;
            }
            catch { } // a transformer of the consumer must not be able to break the input.

            if (NoTrim is false)
            {
                text = text.Trim();
            }
        }

        if (MaxLength > 0 && text.Length > MaxLength)
        {
            text = text[..MaxLength];
        }

        // A field whose only accepted values are the suggestions stores the suggestion rather than the
        // spelling it happened to be typed with: with a case insensitive Comparison, "BLAZOR" is the
        // very same value as "blazor", and a list holding both spellings of one value is a list nothing
        // downstream can group by.
        if (RestrictToSuggestions && text.Length > 0 && Suggestions is not null)
        {
            foreach (var suggestion in Suggestions)
            {
                if (suggestion is null) continue;

                if (string.Equals(suggestion, text, Comparison) is false) continue;

                text = suggestion;
                break;
            }
        }

        return text;
    }

    /// <summary>
    /// Runs every rule a tag has to satisfy. <paramref name="editingIndex"/> is the position of the tag
    /// being edited in place, which changes two of them: the number of tags does not grow, so the
    /// ceiling cannot be reached, and the tag being replaced is not a duplicate of itself.
    /// </summary>
    private bool IsValidTag(string text, List<string> current, out BitTagsInputInvalidReason reason, int editingIndex = -1)
    {
        reason = default;

        if (editingIndex < 0 && MaxTags > 0 && current.Count >= MaxTags)
        {
            reason = BitTagsInputInvalidReason.MaxTags;
            return false;
        }

        if (MinLength > 0 && text.Length < MinLength)
        {
            reason = BitTagsInputInvalidReason.MinLength;
            return false;
        }

        if (_patternRegex is not null)
        {
            var matched = false;
            try
            {
                matched = _patternRegex.IsMatch(text);
            }
            catch (RegexMatchTimeoutException) { }

            if (matched is false)
            {
                reason = BitTagsInputInvalidReason.Pattern;
                return false;
            }
        }

        if (RestrictToSuggestions)
        {
            var suggested = Suggestions is not null && Suggestions.Any(s => string.Equals(s, text, Comparison));

            if (suggested is false)
            {
                reason = BitTagsInputInvalidReason.NotSuggested;
                return false;
            }
        }

        if (Validator is not null)
        {
            var valid = false;
            try
            {
                valid = Validator(text);
            }
            catch { } // a validator of the consumer must not be able to break the input.

            if (valid is false)
            {
                reason = BitTagsInputInvalidReason.Validator;
                return false;
            }
        }

        if (Duplicates is false)
        {
            for (var i = 0; i < current.Count; i++)
            {
                if (i == editingIndex) continue;

                if (string.Equals(current[i], text, Comparison) is false) continue;

                reason = BitTagsInputInvalidReason.Duplicate;
                return false;
            }
        }

        return true;
    }



    private async Task HandleContainerClick()
    {
        // A read-only field is still focused, exactly as a read-only input is: the caret is what the
        // tags are read and copied out of, and refusing it would only make the field unreachable.
        if (IsEnabled is false) return;

        await InputElement.FocusAsync();
    }

    private async Task HandleOnFocusIn(FocusEventArgs e)
    {
        if (IsEnabled is false) return;

        _hasFocus = true;
        _focusedTagIndex = -1;
        ClassBuilder.Reset();
        StyleBuilder.Reset();
        await OnFocusIn.InvokeAsync(e);
    }

    private async Task HandleOnFocusOut(FocusEventArgs e)
    {
        if (IsEnabled is false) return;

        _hasFocus = false;
        ClassBuilder.Reset();
        StyleBuilder.Reset();

        if (NoAddOnBlur is false && ReadOnly is false)
        {
            await TryAddTag();
        }

        await OnFocusOut.InvokeAsync(e);
    }

    private async Task HandleOnInput(ChangeEventArgs e)
    {
        if (IsEnabled is false || ReadOnly) return;

        var text = e.Value?.ToString() ?? string.Empty;

        if (_separators.Length > 0 && _separators.Any(s => text.Contains(s, StringComparison.Ordinal)))
        {
            // Text arriving with a separator inside it is a finished list rather than something still
            // being typed: while typing, a single character separator never reaches the input at all
            // (the script side holds the keystroke back and the key handler commits the tag instead),
            // so what lands here is a paste, an autofill or a multi character separator. Every piece of
            // it becomes a tag and the input is emptied, which is what turns a comma separated list
            // copied out of a spreadsheet into a row of tags in one go.
            var splitOptions = NoTrim
                ? StringSplitOptions.RemoveEmptyEntries
                : StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries;

            var pieces = text.Split(_separators, splitOptions);

            _inputText = string.Empty;

            var added = await TryAddTags(pieces);

            // Nothing of the batch made it in (a single rejected entry typed with its separator, most
            // of the time), so the text is handed back to the user to correct rather than being lost -
            // stripped of the separators, which is what stopped it from being a single tag.
            if (added == 0 && pieces.Length == 1)
            {
                _inputText = pieces[0];
            }

            _syncInputValue = true;
        }
        else
        {
            _inputText = text;
        }

        if (MaxLength > 0 && _inputText.Length > MaxLength)
        {
            _inputText = _inputText[..MaxLength];
            _syncInputValue = true;
        }

        await OnInput.InvokeAsync(_inputText);
    }

    private async Task HandleOnKeyDown(KeyboardEventArgs e)
    {
        if (IsEnabled is false) return;

        await OnKeyDown.InvokeAsync(e);

        if (ReadOnly) return;

        if (e.Key == "Enter")
        {
            // JS capture-phase listener already called preventDefault() as needed;
            // just process the tag addition here.
            await TryAddTag();
        }
        else if (e.Key == "Backspace" && _inputText.Length == 0)
        {
            if (NoBackspaceRemove) return;

            await RemoveLastTag();
        }
        else if (e.Key == "Tab" && e.ShiftKey is false && NoAddOnTab is false && _inputText.Length > 0)
        {
            // JS already prevented focus move in capture phase; add the tag.
            await TryAddTag();
        }
        else if (e.Key == "Escape" && _inputText.Length > 0)
        {
            // Escape takes back what is being typed before it takes anything else: throwing a whole
            // list of tags away over a half typed word is not an undo but a loss.
            _inputText = string.Empty;
            _syncInputValue = true;

            await OnInput.InvokeAsync(_inputText);
        }
        else if (e.Key == "Escape" && ShowClearButton && CurrentValue?.Count > 0)
        {
            // The clear button is deliberately kept out of the tab order, so Escape is its keyboard
            // equivalent, exactly as it is in BitSearchBox and BitNumberField - once there is nothing
            // left in the input for it to take back.
            await Clear();
        }
        else if ((e.Key == (IsRtl ? "ArrowRight" : "ArrowLeft")) && _inputText.Length == 0 && CurrentValue?.Count > 0)
        {
            // Walking backwards out of an empty input lands on the last tag that is drawn, from where
            // the arrow keys keep moving between the tags.
            FocusTag(GetDisplayedTagCount() - 1);
        }
        else if (_separators.Length > 0 && e.Key.Length == 1 && _separators.Any(s => s == e.Key))
        {
            // JS already prevented the separator char from being typed in capture phase;
            // add the current input text as a tag.
            await TryAddTag();
        }
    }

    /// <summary>
    /// The chip standing for the tags folded away unfolds the list and folds it back, which is what
    /// keeps a tag that is not drawn from being a tag that cannot be reached.
    /// </summary>
    private void HandleOnToggleTags()
    {
        _tagsExpanded = _tagsExpanded is false;

        // Folding the list back would otherwise leave the roving tab stop on a tag that is no longer
        // drawn, which is a list nothing in it can be tabbed to.
        if (_tagsExpanded is false && _focusedTagIndex >= GetDisplayedTagCount())
        {
            _focusedTagIndex = -1;
        }
    }

    private void HandleOnTagFocusIn(int index)
    {
        if (IsEnabled is false) return;

        _focusedTagIndex = index;
    }

    /// <summary>
    /// A click on a tag gives it the focus, so that the arrow keys carry on from the tag the user
    /// pointed at. Not every engine focuses an element that is only focusable through its tabindex on
    /// its own, so it is asked for explicitly rather than being left to the browser.
    /// </summary>
    private void HandleOnTagClick(int index)
    {
        if (IsEnabled is false) return;

        // A click landing on another tag while one is being edited is ignored here: the edit input is
        // losing its focus at the very same moment, and HandleOnEditFocusOut is what commits it.
        if (_editingTagIndex >= 0 && _editingTagIndex != index) return;

        FocusTag(index);
    }

    /// <summary>
    /// The double click is the pointer gesture that opens the inline edit, matching what the Enter and
    /// F2 keys do on the focused tag.
    /// </summary>
    private void HandleOnTagDoubleClick(int index)
    {
        StartEdit(index);
    }

    private async Task HandleOnTagKeyDown(KeyboardEventArgs e, int index)
    {
        if (IsEnabled is false) return;

        var count = CurrentValue?.Count ?? 0;
        if (count == 0) return;

        // The navigation walks the tags that are drawn, while a move addresses the list as it really is:
        // sending a tag past the fold is a change to the value, not to where the focus may travel.
        var displayed = GetDisplayedTagCount();

        var previousKey = IsRtl ? "ArrowRight" : "ArrowLeft";
        var nextKey = IsRtl ? "ArrowLeft" : "ArrowRight";

        // Alt turns the navigation keys into the move commands of the reordering, which is the pointer
        // free equivalent of dragging a chip into place, so it is checked before them.
        if (e.AltKey)
        {
            if (AllowReorder is false || ReadOnly) return;

            if (e.Key == previousKey)
            {
                await MoveTag(index, index - 1);
            }
            else if (e.Key == nextKey)
            {
                await MoveTag(index, index + 1);
            }
            else if (e.Key == "Home")
            {
                await MoveTag(index, 0);
            }
            else if (e.Key == "End")
            {
                await MoveTag(index, count - 1);
            }

            return;
        }

        if (e.Key is "Enter" or "F2")
        {
            StartEdit(index);
            return;
        }

        if (e.Key == previousKey)
        {
            FocusTag(Math.Max(0, index - 1));
        }
        else if (e.Key == nextKey)
        {
            // Moving past the last tag lands back in the input, which is where the next tag is typed.
            if (index >= displayed - 1)
            {
                FocusInput();
            }
            else
            {
                FocusTag(index + 1);
            }
        }
        else if (e.Key == "Home")
        {
            FocusTag(0);
        }
        else if (e.Key == "End")
        {
            FocusTag(displayed - 1);
        }
        else if (e.Key is "Delete" or "Backspace")
        {
            if (ReadOnly) return;

            var removed = await RemoveTagAt(index);
            if (removed is false) return;

            var remaining = GetDisplayedTagCount();
            if (remaining == 0)
            {
                FocusInput();
            }
            else
            {
                // Delete keeps the focus at the same position (which now holds the next tag), Backspace
                // moves it back to the tag before the removed one, which is what each of them does in a
                // line of text.
                FocusTag(e.Key == "Delete" ? Math.Min(index, remaining - 1) : Math.Max(0, index - 1));
            }
        }
        else if (e.Key == "Escape")
        {
            FocusInput();
        }
    }

    private void FocusTag(int index)
    {
        _focusedTagIndex = index;
        _pendingFocusTagIndex = index;
        _pendingFocusInput = false;
        _pendingFocusEdit = false;
    }

    private void FocusInput()
    {
        _focusedTagIndex = -1;
        _pendingFocusTagIndex = -1;
        _pendingFocusInput = true;
        _pendingFocusEdit = false;
    }

    /// <summary>
    /// Moves the tag at <paramref name="from"/> to <paramref name="to"/>, keeping the focus on it so
    /// that several steps can be taken in a row, and doing nothing when the move would leave the list.
    /// </summary>
    private async Task MoveTag(int from, int to)
    {
        var list = GetTags();

        if (from < 0 || from >= list.Count) return;
        if (to < 0 || to >= list.Count || to == from) return;

        var tag = list[from];

        list.RemoveAt(from);
        list.Insert(to, tag);

        // A tag sent past the fold would otherwise vanish along with the focus that follows it, so the
        // list is unfolded to show where it landed.
        if (to >= GetDisplayedTagCount())
        {
            _tagsExpanded = true;
        }

        // The tag being corrected in place travels with the list, so the edit follows it rather than
        // being left pointing at whatever now stands where it was.
        if (_editingTagIndex == from)
        {
            _editingTagIndex = to;
        }
        else if (_editingTagIndex >= 0)
        {
            if (from < _editingTagIndex && to >= _editingTagIndex) _editingTagIndex--;
            else if (from > _editingTagIndex && to <= _editingTagIndex) _editingTagIndex++;
        }

        FocusTag(to);

        Announce(MovedAnnouncementFormat ?? "{0} moved to position {1} of {2}.", tag, to + 1, list.Count);

        await SetCurrentValueAsync(list);
        await OnReorder.InvokeAsync(new() { Tag = tag, OldIndex = from, NewIndex = to });
    }

    /// <summary>
    /// Whether the tag at <paramref name="index"/> can be picked up with the pointer, which the tag
    /// being corrected in place is not: its little input needs the drag to select text instead.
    /// </summary>
    private bool CanDragTag(int index)
    {
        return AllowReorder && IsEnabled && ReadOnly is false && _editingTagIndex != index;
    }

    private void HandleOnTagDragStart(int index)
    {
        if (CanDragTag(index) is false) return;

        _draggingTagIndex = index;
        _dragOverTagIndex = index;
    }

    /// <summary>
    /// The tag the pointer is currently over, which is the position the dragged one would take. It is
    /// only remembered rather than acted upon, so that a drag that is called off leaves the list alone.
    /// </summary>
    private void HandleOnTagDragEnter(int index)
    {
        if (_draggingTagIndex < 0) return;

        _dragOverTagIndex = index;
    }

    private void HandleOnTagDragEnd()
    {
        _draggingTagIndex = -1;
        _dragOverTagIndex = -1;
    }

    private async Task HandleOnTagDrop(int index)
    {
        var from = _draggingTagIndex;

        HandleOnTagDragEnd();

        if (from < 0 || from == index) return;
        if (AllowReorder is false || IsEnabled is false || ReadOnly) return;

        await MoveTag(from, index);
    }



    /// <summary>
    /// Turns the tag at <paramref name="index"/> into the little input that replaces it while it is
    /// being corrected in place.
    /// </summary>
    private void StartEdit(int index)
    {
        if (EditableTags is false || IsEnabled is false || ReadOnly) return;

        var list = GetTags();
        if (index < 0 || index >= list.Count) return;

        _editingTagIndex = index;
        _editText = list[index];
        _pendingFocusEdit = true;
        _focusedTagIndex = index;
        _pendingFocusTagIndex = -1;
        _pendingFocusInput = false;
    }

    private void HandleOnEditInput(ChangeEventArgs e)
    {
        _editText = e.Value?.ToString() ?? string.Empty;
    }

    private async Task HandleOnEditKeyDown(KeyboardEventArgs e)
    {
        if (e.Key == "Enter")
        {
            await CommitEdit();
        }
        else if (e.Key == "Escape")
        {
            CancelEdit();
        }
    }

    /// <summary>
    /// Leaving the little input commits what it holds, exactly as leaving the main input commits the
    /// text typed into it. The focus is left wherever it went: the commit is a consequence of the user
    /// having gone somewhere else, and pulling them back into the field would undo the very move that
    /// caused it.
    /// </summary>
    private Task HandleOnEditFocusOut() => CommitEdit(restoreFocus: false);

    private void CancelEdit()
    {
        if (_editingTagIndex < 0) return;

        var index = _editingTagIndex;

        _editingTagIndex = -1;
        _editText = string.Empty;

        FocusTag(index);
    }

    /// <summary>
    /// Commits the inline edit. <paramref name="restoreFocus"/> tells whether the focus belongs back on
    /// the tag: it does when the edit was confirmed from the keyboard, and it does not when the commit
    /// only happened because the focus went somewhere else of its own accord.
    /// </summary>
    private async Task CommitEdit(bool restoreFocus = true)
    {
        if (_editingTagIndex < 0) return;

        var index = _editingTagIndex;
        var list = GetTags();

        _editingTagIndex = -1;

        if (index >= list.Count) return;

        var original = list[index];
        var text = NormalizeTag(_editText);

        _editText = string.Empty;

        // An edit that empties the tag is how a tag is thrown away from the keyboard without reaching
        // for its dismiss button.
        if (text.Length == 0)
        {
            var removed = await RemoveTagAt(index);

            if (restoreFocus is false) return;

            if (removed && CurrentValue?.Count > 0)
            {
                FocusTag(Math.Min(index, CurrentValue.Count - 1));
            }
            else
            {
                FocusInput();
            }

            return;
        }

        if (string.Equals(text, original, StringComparison.Ordinal))
        {
            if (restoreFocus)
            {
                FocusTag(index);
            }
            return;
        }

        if (IsValidTag(text, list, out var reason, editingIndex: index) is false)
        {
            await ReportInvalid(text, reason);

            if (restoreFocus)
            {
                FocusTag(index);
            }
            return;
        }

        if (OnEdit.HasDelegate)
        {
            var args = new BitTagsInputEditArgs { Tag = original, NewTag = text };
            await OnEdit.InvokeAsync(args);
            if (args.Cancel)
            {
                if (restoreFocus)
                {
                    FocusTag(index);
                }
                return;
            }

            // The handler is allowed to correct the text on its way in, so what is stored is what it
            // left in NewTag rather than what was handed to it.
            text = args.NewTag;
        }

        list[index] = text;

        if (restoreFocus)
        {
            FocusTag(index);
        }
        else
        {
            // The tab stop of the list still belongs on the tag that was corrected, even though the
            // focus itself has gone elsewhere.
            _focusedTagIndex = index;
        }

        Announce(EditedAnnouncementFormat ?? "{0} updated.", text);

        await SetCurrentValueAsync(list);
    }

    private async Task TryAddTag()
    {
        if (_inputText.Length == 0) return;

        var text = NormalizeTag(_inputText);

        if (text.Length == 0)
        {
            _inputText = string.Empty;
            _syncInputValue = true;
            return;
        }

        var list = GetTags();

        if (IsValidTag(text, list, out var reason) is false)
        {
            await ReportInvalid(text, reason);
            return;
        }

        if (OnBeforeAdd.HasDelegate)
        {
            var args = new BitTagsInputBeforeArgs { Tag = text };
            await OnBeforeAdd.InvokeAsync(args);
            if (args.Cancel) return;
        }

        list.Add(text);

        _inputText = string.Empty;
        _syncInputValue = true;

        Announce(AddedAnnouncementFormat ?? "{0} added.", text);

        await SetCurrentValueAsync(list);
        await OnAdd.InvokeAsync([text]);
    }

    private async Task<int> TryAddTags(string[] tags)
    {
        var list = GetTags();
        var addedTags = new List<string>();

        foreach (var tag in tags)
        {
            var text = NormalizeTag(tag);
            if (text.Length == 0) continue;

            if (IsValidTag(text, list, out var reason) is false)
            {
                await ReportInvalid(text, reason);

                // Once the ceiling is reached nothing else of the batch can be added either, so the
                // rest of it is not walked (and not reported) one rejection at a time.
                if (reason == BitTagsInputInvalidReason.MaxTags) break;

                continue;
            }

            if (OnBeforeAdd.HasDelegate)
            {
                var args = new BitTagsInputBeforeArgs { Tag = text };
                await OnBeforeAdd.InvokeAsync(args);
                if (args.Cancel) continue;
            }

            list.Add(text);
            addedTags.Add(text);
        }

        if (addedTags.Count == 0) return 0;

        // One tag is named, a batch of them is counted: a pasted list of fifty read out name by name is
        // not a confirmation but a wall, and the tags themselves are in the list to be walked through.
        if (addedTags.Count == 1)
        {
            Announce(AddedAnnouncementFormat ?? "{0} added.", addedTags[0]);
        }
        else
        {
            Announce(AddedManyAnnouncementFormat ?? "{0} tags added.", addedTags.Count);
        }

        await SetCurrentValueAsync(list);
        await OnAdd.InvokeAsync(addedTags);

        return addedTags.Count;
    }

    private async Task HandleRemoveTag(int index)
    {
        if (IsEnabled is false || ReadOnly) return;

        var removed = await RemoveTagAt(index);

        // The dismiss button of the removed tag is gone along with it, so the focus would otherwise be
        // dropped on the document body and the user would lose their place entirely. A removal that was
        // called off leaves both the button and the focus exactly where they were.
        if (removed)
        {
            FocusInput();
        }
    }

    private async Task<bool> RemoveTagAt(int index)
    {
        var list = GetTags();

        if (index < 0 || index >= list.Count) return false;

        var tag = list[index];

        if (OnBeforeRemove.HasDelegate)
        {
            var args = new BitTagsInputBeforeArgs { Tag = tag };
            await OnBeforeRemove.InvokeAsync(args);
            if (args.Cancel) return false;
        }

        list.RemoveAt(index);

        if (_focusedTagIndex >= list.Count)
        {
            _focusedTagIndex = list.Count - 1;
        }

        // An edit open on the tag that has just been taken away has nothing left to commit, and one
        // open on a tag that has merely shifted has to follow it rather than the position it sat at.
        if (_editingTagIndex == index)
        {
            _editingTagIndex = -1;
            _editText = string.Empty;
        }
        else if (index < _editingTagIndex)
        {
            _editingTagIndex--;
        }

        Announce(RemovedAnnouncementFormat ?? "{0} removed.", tag);

        await SetCurrentValueAsync(list.Count > 0 ? list : null);
        await OnRemove.InvokeAsync(tag);

        return true;
    }

    private async Task RemoveLastTag()
    {
        var count = CurrentValue?.Count ?? 0;
        if (count == 0) return;

        await RemoveTagAt(count - 1);
    }
}
