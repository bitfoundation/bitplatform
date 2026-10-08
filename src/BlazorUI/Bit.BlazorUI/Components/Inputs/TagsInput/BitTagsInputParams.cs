namespace Bit.BlazorUI;

/// <summary>
/// The parameters for <see cref="BitTagsInput"/> component.
/// </summary>
/// <remarks>
/// What a <see cref="BitParams"/> carries down is the configuration of a tags input - its look, its rules and
/// its wording - and never its value. The parameters that belong to the one field holding them (<c>Value</c>,
/// <c>DefaultValue</c>, <c>Name</c>, <c>Required</c>, <c>ReadOnly</c>), the templates and the event callbacks
/// stay on the component itself, since a list of tags, the form field it is posted as and the handler that
/// watches it are never shared between two fields.
/// </remarks>
public class BitTagsInputParams : BitComponentBaseParams, IBitComponentParams
{
    /// <summary>
    /// Represents the parameter name used to identify the <see cref="BitTagsInput"/> cascading parameters within <see cref="BitParams"/>.
    /// </summary>
    /// <remarks>
    /// This constant is typically used when referencing or accessing the BitTagsInput value in
    /// parameterized APIs or configuration settings. Using this constant helps ensure consistency and reduces the risk
    /// of typographical errors.
    /// </remarks>
    public const string ParamName = $"{nameof(BitParams)}.{nameof(BitTagsInput)}";



    public string Name => ParamName;



    /// <summary>
    /// The format of the message announced by screen readers when a tag is added, where {0} is the tag.
    /// </summary>
    public string? AddedAnnouncementFormat { get; set; }

    /// <summary>
    /// The format of the message announced by screen readers when several tags are added at once, where {0}
    /// is how many of them there were.
    /// </summary>
    public string? AddedManyAnnouncementFormat { get; set; }

    /// <summary>
    /// Lets a tag be moved within the list, by dragging it onto the position it should take or with Alt and
    /// the arrow keys.
    /// </summary>
    public bool? AllowReorder { get; set; }

    /// <summary>
    /// Sets the autocomplete html attribute of the input element.
    /// </summary>
    public string? AutoComplete { get; set; }

    /// <summary>
    /// Whether the input should receive focus on first render.
    /// </summary>
    public bool? AutoFocus { get; set; }

    /// <summary>
    /// Turns the Backspace pressed on an empty input from a removal into a correction: the last tag is taken
    /// off the list and its text is put back into the input.
    /// </summary>
    public bool? BackspaceEditsLastTag { get; set; }

    /// <summary>
    /// Lets the Enter pressed on an empty input through, so that it reaches the form around the field.
    /// </summary>
    public bool? CancelConfirmKeysOnEmpty { get; set; }

    /// <summary>
    /// A predicate deciding which tags the user is allowed to take off the list. A tag it turns down is
    /// drawn without a dismiss button, ignores the Delete and Backspace keys and stays behind when the
    /// field is cleared.
    /// </summary>
    public Func<string, bool>? CanRemoveTag { get; set; }

    /// <summary>
    /// Custom CSS classes for different parts of the component.
    /// </summary>
    public BitTagsInputClassStyles? Classes { get; set; }

    /// <summary>
    /// Accessible label of the clear button.
    /// </summary>
    public string? ClearButtonAriaLabel { get; set; }

    /// <summary>
    /// The icon of the clear button, from an external icon library.
    /// </summary>
    public BitIconInfo? ClearButtonIcon { get; set; }

    /// <summary>
    /// The name of the icon of the clear button, from the built-in Fluent UI icons.
    /// </summary>
    public string? ClearButtonIconName { get; set; }

    /// <summary>
    /// The tooltip of the clear button.
    /// </summary>
    public string? ClearButtonTitle { get; set; }

    /// <summary>
    /// The format of the message announced by screen readers when every tag is removed at once, where {0} is
    /// how many of them there were.
    /// </summary>
    public string? ClearedAnnouncementFormat { get; set; }

    /// <summary>
    /// Throws away whatever text is still sitting in the input when the field loses the focus.
    /// </summary>
    public bool? ClearOnBlur { get; set; }

    /// <summary>
    /// The color role the tags, the border and the focus ring of the field carry.
    /// </summary>
    public BitColor? Color { get; set; }

    /// <summary>
    /// The string comparison that decides whether a tag is a duplicate of one that is already in the list.
    /// </summary>
    public StringComparison? Comparison { get; set; }

    /// <summary>
    /// How long, in milliseconds, the field waits for the typing to stop before raising OnInput.
    /// </summary>
    public int? DebounceTime { get; set; }

    /// <summary>
    /// A hint rendered under the field, referenced by the input through its aria-describedby attribute.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// The format of the accessible label of the dismiss button of each tag, where {0} is the tag.
    /// </summary>
    public string? DismissAriaLabelFormat { get; set; }

    /// <summary>
    /// The icon of the dismiss button of each tag, from an external icon library.
    /// </summary>
    public BitIconInfo? DismissIcon { get; set; }

    /// <summary>
    /// The name of the icon of the dismiss button of each tag, from the built-in Fluent UI icons.
    /// </summary>
    public string? DismissIconName { get; set; }

    /// <summary>
    /// The title (tooltip) of the dismiss button of each tag.
    /// </summary>
    public string? DismissTitle { get; set; }

    /// <summary>
    /// Whether duplicate tags are allowed.
    /// </summary>
    public bool? Duplicates { get; set; }

    /// <summary>
    /// The format of the accessible label of the little input that replaces a tag while it is being edited in
    /// place, where {0} is the tag.
    /// </summary>
    public string? EditAriaLabelFormat { get; set; }

    /// <summary>
    /// Lets a tag be corrected in place with a double click, or with the Enter or F2 key.
    /// </summary>
    public bool? EditableTags { get; set; }

    /// <summary>
    /// The format of the message announced by screen readers when a tag is edited, where {0} is the tag as it
    /// now reads.
    /// </summary>
    public string? EditedAnnouncementFormat { get; set; }

    /// <summary>
    /// The sentence announced after each tag CanRemoveTag holds in place.
    /// </summary>
    public string? FixedTagAriaDescription { get; set; }

    /// <summary>
    /// Sets the enterkeyhint html attribute of the input element.
    /// </summary>
    public BitEnterKeyHint? EnterKeyHint { get; set; }

    /// <summary>
    /// The sentence that says why a tag was refused, for wording of your own and for localization.
    /// </summary>
    public Func<BitTagsInputInvalidArgs, string?>? GetInvalidMessage { get; set; }

    /// <summary>
    /// How a tag is called wherever the component names it: the accessible name of its chip and of its
    /// buttons, and the announcements it takes part in. It defaults to the tag itself.
    /// </summary>
    public Func<string, string?>? GetTagName { get; set; }

    /// <summary>
    /// A function returning extra CSS classes for a single tag.
    /// </summary>
    public Func<string, string?>? GetTagClass { get; set; }

    /// <summary>
    /// A function returning extra inline CSS styles for a single tag.
    /// </summary>
    public Func<string, string?>? GetTagStyle { get; set; }

    /// <summary>
    /// Sets the inputmode html attribute of the input element.
    /// </summary>
    public BitInputMode? InputMode { get; set; }

    /// <summary>
    /// Draws a spinner at the end of the field, for the wait the field itself is the cause of.
    /// </summary>
    public bool? IsLoading { get; set; }

    /// <summary>
    /// The format of the message announced by screen readers when a tag is rejected, where {0} is the tag.
    /// </summary>
    public string? InvalidAnnouncementFormat { get; set; }

    /// <summary>
    /// The label displayed above the input.
    /// </summary>
    public string? Label { get; set; }

    /// <summary>
    /// The label of the chip that folds the tags back once MaxDisplayedTags unfolded them.
    /// </summary>
    public string? LessTagsText { get; set; }

    /// <summary>
    /// The accessible name of the spinner IsLoading draws.
    /// </summary>
    public string? LoadingAriaLabel { get; set; }

    /// <summary>
    /// The number of tags drawn before the rest of them are folded away behind a chip. 0 means all of them.
    /// </summary>
    public int? MaxDisplayedTags { get; set; }

    /// <summary>
    /// The maximum number of characters allowed for each individual tag. 0 means no limit.
    /// </summary>
    public int? MaxLength { get; set; }

    /// <summary>
    /// The number of values the suggestion list is allowed to offer at once. 0 means all of them.
    /// </summary>
    public int? MaxSuggestions { get; set; }

    /// <summary>
    /// The maximum number of tags allowed. 0 means no limit.
    /// </summary>
    public int? MaxTags { get; set; }

    /// <summary>
    /// The minimum number of characters a tag has to hold to be accepted. 0 means no limit.
    /// </summary>
    public int? MinLength { get; set; }

    /// <summary>
    /// The format of the accessible label of the chip standing for the folded tags, where {0} is how many of
    /// them are folded away.
    /// </summary>
    public string? MoreTagsAriaLabelFormat { get; set; }

    /// <summary>
    /// The format of the label of the chip standing for the folded tags, where {0} is how many of them there
    /// are.
    /// </summary>
    public string? MoreTagsFormat { get; set; }

    /// <summary>
    /// The format of the message announced by screen readers when a tag is moved, where {0} is the tag, {1}
    /// its new one based position and {2} the number of tags.
    /// </summary>
    public string? MovedAnnouncementFormat { get; set; }

    /// <summary>
    /// Stops the text left in the input from being committed as a tag when the field loses the focus.
    /// </summary>
    public bool? NoAddOnBlur { get; set; }

    /// <summary>
    /// Stops the Tab key from committing the text left in the input.
    /// </summary>
    public bool? NoAddOnTab { get; set; }

    /// <summary>
    /// Stops the Backspace key from removing the last tag when the input is empty.
    /// </summary>
    public bool? NoBackspaceRemove { get; set; }

    /// <summary>
    /// Whether the input should have no border.
    /// </summary>
    public bool? NoBorder { get; set; }

    /// <summary>
    /// Leaves the Escape key alone, so that it empties neither the input nor the list of tags.
    /// </summary>
    public bool? NoClearOnEscape { get; set; }

    /// <summary>
    /// Stops the field from marking a tag it refused, and the tag a refused duplicate collided with.
    /// </summary>
    public bool? NoInvalidHighlight { get; set; }

    /// <summary>
    /// Keeps the leading and trailing whitespace of a tag instead of trimming it away.
    /// </summary>
    public bool? NoTrim { get; set; }

    /// <summary>
    /// A regular expression that every tag has to match to be accepted.
    /// </summary>
    public string? Pattern { get; set; }

    /// <summary>
    /// The format of the message announced when a tag is picked up with its reorder handle, where {0} is
    /// the tag.
    /// </summary>
    public string? PickedUpAnnouncementFormat { get; set; }

    /// <summary>
    /// The placeholder text of the input, shown while there is no tag in the list.
    /// </summary>
    public string? Placeholder { get; set; }

    /// <summary>
    /// A short text drawn at the start of the field, which is not part of the value.
    /// </summary>
    public string? Prefix { get; set; }

    /// <summary>
    /// The format of the message announced when a picked up tag is put back, where {0} is the tag.
    /// </summary>
    public string? PutBackAnnouncementFormat { get; set; }

    /// <summary>
    /// The format of the message announced by screen readers when a tag is removed, where {0} is the tag.
    /// </summary>
    public string? RemovedAnnouncementFormat { get; set; }

    /// <summary>
    /// The format of the accessible label of the reorder handle of each tag, where {0} is the tag.
    /// </summary>
    public string? ReorderAriaLabelFormat { get; set; }

    /// <summary>
    /// The format of the accessible label the other reorder handles take while a tag is picked up, where
    /// {0} is the tag being carried.
    /// </summary>
    public string? ReorderDropAriaLabelFormat { get; set; }

    /// <summary>
    /// The icon of the reorder handle, as custom CSS classes for external icon libraries.
    /// </summary>
    public BitIconInfo? ReorderIcon { get; set; }

    /// <summary>
    /// The name of the icon of the reorder handle from the built-in Fluent UI icons.
    /// </summary>
    public string? ReorderIconName { get; set; }

    /// <summary>
    /// The title (tooltip) of the reorder handle of each tag.
    /// </summary>
    public string? ReorderTitle { get; set; }

    /// <summary>
    /// Turns the Suggestions into the whole of what the field accepts.
    /// </summary>
    public bool? RestrictToSuggestions { get; set; }

    /// <summary>
    /// The character(s) that turn the typed text into a tag on top of the Enter key, and that split a pasted
    /// list into a tag each.
    /// </summary>
    public IEnumerable<string>? Separators { get; set; }

    /// <summary>
    /// Whether to render a button that removes every tag at once.
    /// </summary>
    public bool? ShowClearButton { get; set; }

    /// <summary>
    /// Whether to render the number of tags under the field.
    /// </summary>
    public bool? ShowCounter { get; set; }

    /// <summary>
    /// Draws the sentence saying why the last tag was refused under the field, where the description
    /// otherwise stands.
    /// </summary>
    public bool? ShowInvalidMessage { get; set; }

    /// <summary>
    /// The size of the tags input.
    /// </summary>
    public BitSize? Size { get; set; }

    /// <summary>
    /// Sets the spellcheck html attribute of the input element.
    /// </summary>
    public bool? SpellCheck { get; set; }

    /// <summary>
    /// Custom CSS styles for different parts of the component.
    /// </summary>
    public BitTagsInputClassStyles? Styles { get; set; }

    /// <summary>
    /// A short text drawn at the end of the field, which is not part of the value.
    /// </summary>
    public string? Suffix { get; set; }

    /// <summary>
    /// The values offered to the user while typing, through the browser's own suggestion list.
    /// </summary>
    public IEnumerable<string>? Suggestions { get; set; }

    /// <summary>
    /// The sentence announced after each tag, telling what the keyboard can do with it.
    /// </summary>
    public string? TagAriaDescription { get; set; }

    /// <summary>
    /// The format of the sentence describing the input, where {0} is how many tags the list holds and {1}
    /// the MaxTags ceiling.
    /// </summary>
    public string? TagCountAriaDescriptionFormat { get; set; }

    /// <summary>
    /// The accessible name of the list the tags form.
    /// </summary>
    public string? TagsAriaLabel { get; set; }

    /// <summary>
    /// The placeholder text of the input shown once there is at least one tag in the list.
    /// </summary>
    public string? TagsPlaceholder { get; set; }

    /// <summary>
    /// How much of the Color the tags are painted with.
    /// </summary>
    public BitVariant? TagVariant { get; set; }

    /// <summary>
    /// How long, in milliseconds, OnInput waits between two raises while the typing goes on.
    /// </summary>
    public int? ThrottleTime { get; set; }

    /// <summary>
    /// A function applied to the text of a tag before anything else is done with it.
    /// </summary>
    public Func<string, string>? Transformer { get; set; }

    /// <summary>
    /// A predicate every tag has to satisfy to be accepted.
    /// </summary>
    public Func<string, bool>? Validator { get; set; }

    /// <summary>
    /// The visual variant of the field.
    /// </summary>
    public BitVariant? Variant { get; set; }



    /// <summary>
    /// Updates the properties of the specified <see cref="BitTagsInput"/> instance with any values that have been set on
    /// this object, if those properties have not already been set on the <see cref="BitTagsInput"/>.
    /// </summary>
    /// <remarks>
    /// Only properties that have a value set and have not already been set on the <paramref name="bitTagsInput"/> will be updated.
    /// This method does not overwrite existing values on <paramref name="bitTagsInput"/>.
    /// </remarks>
    /// <param name="bitTagsInput">
    /// The <see cref="BitTagsInput"/> instance whose properties will be updated. Cannot be null.
    /// </param>
    public void UpdateParameters(BitTagsInput bitTagsInput)
    {
        if (bitTagsInput is null) return;

        UpdateBaseParameters(bitTagsInput);

        if (AddedAnnouncementFormat is not null)
        {
            bitTagsInput.TakeFromCascade(nameof(AddedAnnouncementFormat), AddedAnnouncementFormat, static t => t.AddedAnnouncementFormat, static (t, v) => t.AddedAnnouncementFormat = v);
        }

        if (AddedManyAnnouncementFormat is not null)
        {
            bitTagsInput.TakeFromCascade(nameof(AddedManyAnnouncementFormat), AddedManyAnnouncementFormat, static t => t.AddedManyAnnouncementFormat, static (t, v) => t.AddedManyAnnouncementFormat = v);
        }

        if (AllowReorder.HasValue)
        {
            bitTagsInput.TakeFromCascade(nameof(AllowReorder), AllowReorder.Value, static t => t.AllowReorder, static (t, v) => t.AllowReorder = v);
        }

        if (AutoComplete.HasValue())
        {
            bitTagsInput.TakeFromCascade(nameof(AutoComplete), AutoComplete, static t => t.AutoComplete, static (t, v) => t.AutoComplete = v);
        }

        if (AutoFocus.HasValue)
        {
            bitTagsInput.TakeFromCascade(nameof(AutoFocus), AutoFocus.Value, static t => t.AutoFocus, static (t, v) => t.AutoFocus = v);
        }

        if (BackspaceEditsLastTag.HasValue)
        {
            bitTagsInput.TakeFromCascade(nameof(BackspaceEditsLastTag), BackspaceEditsLastTag.Value, static t => t.BackspaceEditsLastTag, static (t, v) => t.BackspaceEditsLastTag = v);
        }

        if (CancelConfirmKeysOnEmpty.HasValue)
        {
            bitTagsInput.TakeFromCascade(nameof(CancelConfirmKeysOnEmpty), CancelConfirmKeysOnEmpty.Value, static t => t.CancelConfirmKeysOnEmpty, static (t, v) => t.CancelConfirmKeysOnEmpty = v);
        }

        if (CanRemoveTag is not null)
        {
            bitTagsInput.TakeFromCascade(nameof(CanRemoveTag), CanRemoveTag, static t => t.CanRemoveTag, static (t, v) => t.CanRemoveTag = v);
        }

        if (Classes is not null)
        {
            bitTagsInput.TakeFromCascade(nameof(Classes), Classes, static t => t.Classes, static (t, v) => t.Classes = v);
        }

        if (ClearButtonAriaLabel.HasValue())
        {
            bitTagsInput.TakeFromCascade(nameof(ClearButtonAriaLabel), ClearButtonAriaLabel, static t => t.ClearButtonAriaLabel, static (t, v) => t.ClearButtonAriaLabel = v);
        }

        if (ClearButtonIcon is not null)
        {
            bitTagsInput.TakeFromCascade(nameof(ClearButtonIcon), ClearButtonIcon, static t => t.ClearButtonIcon, static (t, v) => t.ClearButtonIcon = v);
        }

        if (ClearButtonIconName.HasValue())
        {
            bitTagsInput.TakeFromCascade(nameof(ClearButtonIconName), ClearButtonIconName, static t => t.ClearButtonIconName, static (t, v) => t.ClearButtonIconName = v);
        }

        if (ClearButtonTitle.HasValue())
        {
            bitTagsInput.TakeFromCascade(nameof(ClearButtonTitle), ClearButtonTitle, static t => t.ClearButtonTitle, static (t, v) => t.ClearButtonTitle = v);
        }

        if (ClearedAnnouncementFormat is not null)
        {
            bitTagsInput.TakeFromCascade(nameof(ClearedAnnouncementFormat), ClearedAnnouncementFormat, static t => t.ClearedAnnouncementFormat, static (t, v) => t.ClearedAnnouncementFormat = v);
        }

        if (ClearOnBlur.HasValue)
        {
            bitTagsInput.TakeFromCascade(nameof(ClearOnBlur), ClearOnBlur.Value, static t => t.ClearOnBlur, static (t, v) => t.ClearOnBlur = v);
        }

        if (Color.HasValue)
        {
            bitTagsInput.TakeFromCascade(nameof(Color), Color.Value, static t => t.Color, static (t, v) => t.Color = v);
        }

        if (Comparison.HasValue)
        {
            bitTagsInput.TakeFromCascade(nameof(Comparison), Comparison.Value, static t => t.Comparison, static (t, v) => t.Comparison = v);
        }

        if (DebounceTime.HasValue)
        {
            bitTagsInput.TakeFromCascade(nameof(DebounceTime), DebounceTime.Value, static t => t.DebounceTime, static (t, v) => t.DebounceTime = v);
        }

        if (Description.HasValue())
        {
            bitTagsInput.TakeFromCascade(nameof(Description), Description, static t => t.Description, static (t, v) => t.Description = v);
        }

        if (DismissAriaLabelFormat.HasValue())
        {
            bitTagsInput.TakeFromCascade(nameof(DismissAriaLabelFormat), DismissAriaLabelFormat, static t => t.DismissAriaLabelFormat, static (t, v) => t.DismissAriaLabelFormat = v);
        }

        if (DismissIcon is not null)
        {
            bitTagsInput.TakeFromCascade(nameof(DismissIcon), DismissIcon, static t => t.DismissIcon, static (t, v) => t.DismissIcon = v);
        }

        if (DismissIconName.HasValue())
        {
            bitTagsInput.TakeFromCascade(nameof(DismissIconName), DismissIconName, static t => t.DismissIconName, static (t, v) => t.DismissIconName = v);
        }

        if (DismissTitle.HasValue())
        {
            bitTagsInput.TakeFromCascade(nameof(DismissTitle), DismissTitle, static t => t.DismissTitle, static (t, v) => t.DismissTitle = v);
        }

        if (Duplicates.HasValue)
        {
            bitTagsInput.TakeFromCascade(nameof(Duplicates), Duplicates.Value, static t => t.Duplicates, static (t, v) => t.Duplicates = v);
        }

        if (EditAriaLabelFormat.HasValue())
        {
            bitTagsInput.TakeFromCascade(nameof(EditAriaLabelFormat), EditAriaLabelFormat, static t => t.EditAriaLabelFormat, static (t, v) => t.EditAriaLabelFormat = v);
        }

        if (EditableTags.HasValue)
        {
            bitTagsInput.TakeFromCascade(nameof(EditableTags), EditableTags.Value, static t => t.EditableTags, static (t, v) => t.EditableTags = v);
        }

        if (EditedAnnouncementFormat is not null)
        {
            bitTagsInput.TakeFromCascade(nameof(EditedAnnouncementFormat), EditedAnnouncementFormat, static t => t.EditedAnnouncementFormat, static (t, v) => t.EditedAnnouncementFormat = v);
        }

        if (FixedTagAriaDescription is not null)
        {
            bitTagsInput.TakeFromCascade(nameof(FixedTagAriaDescription), FixedTagAriaDescription, static t => t.FixedTagAriaDescription, static (t, v) => t.FixedTagAriaDescription = v);
        }

        if (EnterKeyHint.HasValue)
        {
            bitTagsInput.TakeFromCascade(nameof(EnterKeyHint), EnterKeyHint.Value, static t => t.EnterKeyHint, static (t, v) => t.EnterKeyHint = v);
        }

        if (GetInvalidMessage is not null)
        {
            bitTagsInput.TakeFromCascade(nameof(GetInvalidMessage), GetInvalidMessage, static t => t.GetInvalidMessage, static (t, v) => t.GetInvalidMessage = v);
        }

        if (GetTagName is not null)
        {
            bitTagsInput.TakeFromCascade(nameof(GetTagName), GetTagName, static t => t.GetTagName, static (t, v) => t.GetTagName = v);
        }

        if (GetTagClass is not null)
        {
            bitTagsInput.TakeFromCascade(nameof(GetTagClass), GetTagClass, static t => t.GetTagClass, static (t, v) => t.GetTagClass = v);
        }

        if (GetTagStyle is not null)
        {
            bitTagsInput.TakeFromCascade(nameof(GetTagStyle), GetTagStyle, static t => t.GetTagStyle, static (t, v) => t.GetTagStyle = v);
        }

        if (InputMode.HasValue)
        {
            bitTagsInput.TakeFromCascade(nameof(InputMode), InputMode.Value, static t => t.InputMode, static (t, v) => t.InputMode = v);
        }

        if (InvalidAnnouncementFormat is not null)
        {
            bitTagsInput.TakeFromCascade(nameof(InvalidAnnouncementFormat), InvalidAnnouncementFormat, static t => t.InvalidAnnouncementFormat, static (t, v) => t.InvalidAnnouncementFormat = v);
        }

        if (IsLoading.HasValue)
        {
            bitTagsInput.TakeFromCascade(nameof(IsLoading), IsLoading.Value, static t => t.IsLoading, static (t, v) => t.IsLoading = v);
        }

        if (Label.HasValue())
        {
            bitTagsInput.TakeFromCascade(nameof(Label), Label, static t => t.Label, static (t, v) => t.Label = v);
        }

        if (LessTagsText.HasValue())
        {
            bitTagsInput.TakeFromCascade(nameof(LessTagsText), LessTagsText, static t => t.LessTagsText, static (t, v) => t.LessTagsText = v);
        }

        if (LoadingAriaLabel.HasValue())
        {
            bitTagsInput.TakeFromCascade(nameof(LoadingAriaLabel), LoadingAriaLabel, static t => t.LoadingAriaLabel, static (t, v) => t.LoadingAriaLabel = v);
        }

        if (MaxDisplayedTags.HasValue)
        {
            bitTagsInput.TakeFromCascade(nameof(MaxDisplayedTags), MaxDisplayedTags.Value, static t => t.MaxDisplayedTags, static (t, v) => t.MaxDisplayedTags = v);
        }

        if (MaxLength.HasValue)
        {
            bitTagsInput.TakeFromCascade(nameof(MaxLength), MaxLength.Value, static t => t.MaxLength, static (t, v) => t.MaxLength = v);
        }

        if (MaxSuggestions.HasValue)
        {
            bitTagsInput.TakeFromCascade(nameof(MaxSuggestions), MaxSuggestions.Value, static t => t.MaxSuggestions, static (t, v) => t.MaxSuggestions = v);
        }

        if (MaxTags.HasValue)
        {
            bitTagsInput.TakeFromCascade(nameof(MaxTags), MaxTags.Value, static t => t.MaxTags, static (t, v) => t.MaxTags = v);
        }

        if (MinLength.HasValue)
        {
            bitTagsInput.TakeFromCascade(nameof(MinLength), MinLength.Value, static t => t.MinLength, static (t, v) => t.MinLength = v);
        }

        if (MoreTagsAriaLabelFormat.HasValue())
        {
            bitTagsInput.TakeFromCascade(nameof(MoreTagsAriaLabelFormat), MoreTagsAriaLabelFormat, static t => t.MoreTagsAriaLabelFormat, static (t, v) => t.MoreTagsAriaLabelFormat = v);
        }

        if (MoreTagsFormat.HasValue())
        {
            bitTagsInput.TakeFromCascade(nameof(MoreTagsFormat), MoreTagsFormat, static t => t.MoreTagsFormat, static (t, v) => t.MoreTagsFormat = v);
        }

        if (MovedAnnouncementFormat is not null)
        {
            bitTagsInput.TakeFromCascade(nameof(MovedAnnouncementFormat), MovedAnnouncementFormat, static t => t.MovedAnnouncementFormat, static (t, v) => t.MovedAnnouncementFormat = v);
        }

        if (NoAddOnBlur.HasValue)
        {
            bitTagsInput.TakeFromCascade(nameof(NoAddOnBlur), NoAddOnBlur.Value, static t => t.NoAddOnBlur, static (t, v) => t.NoAddOnBlur = v);
        }

        if (NoAddOnTab.HasValue)
        {
            bitTagsInput.TakeFromCascade(nameof(NoAddOnTab), NoAddOnTab.Value, static t => t.NoAddOnTab, static (t, v) => t.NoAddOnTab = v);
        }

        if (NoBackspaceRemove.HasValue)
        {
            bitTagsInput.TakeFromCascade(nameof(NoBackspaceRemove), NoBackspaceRemove.Value, static t => t.NoBackspaceRemove, static (t, v) => t.NoBackspaceRemove = v);
        }

        if (NoBorder.HasValue)
        {
            bitTagsInput.TakeFromCascade(nameof(NoBorder), NoBorder.Value, static t => t.NoBorder, static (t, v) => t.NoBorder = v);
        }

        if (NoClearOnEscape.HasValue)
        {
            bitTagsInput.TakeFromCascade(nameof(NoClearOnEscape), NoClearOnEscape.Value, static t => t.NoClearOnEscape, static (t, v) => t.NoClearOnEscape = v);
        }

        if (NoInvalidHighlight.HasValue)
        {
            bitTagsInput.TakeFromCascade(nameof(NoInvalidHighlight), NoInvalidHighlight.Value, static t => t.NoInvalidHighlight, static (t, v) => t.NoInvalidHighlight = v);
        }

        if (NoTrim.HasValue)
        {
            bitTagsInput.TakeFromCascade(nameof(NoTrim), NoTrim.Value, static t => t.NoTrim, static (t, v) => t.NoTrim = v);
        }

        if (Pattern.HasValue())
        {
            bitTagsInput.TakeFromCascade(nameof(Pattern), Pattern, static t => t.Pattern, static (t, v) => t.Pattern = v);
        }

        if (PickedUpAnnouncementFormat is not null)
        {
            bitTagsInput.TakeFromCascade(nameof(PickedUpAnnouncementFormat), PickedUpAnnouncementFormat, static t => t.PickedUpAnnouncementFormat, static (t, v) => t.PickedUpAnnouncementFormat = v);
        }

        if (Placeholder.HasValue())
        {
            bitTagsInput.TakeFromCascade(nameof(Placeholder), Placeholder, static t => t.Placeholder, static (t, v) => t.Placeholder = v);
        }

        if (Prefix.HasValue())
        {
            bitTagsInput.TakeFromCascade(nameof(Prefix), Prefix, static t => t.Prefix, static (t, v) => t.Prefix = v);
        }

        if (RemovedAnnouncementFormat is not null)
        {
            bitTagsInput.TakeFromCascade(nameof(RemovedAnnouncementFormat), RemovedAnnouncementFormat, static t => t.RemovedAnnouncementFormat, static (t, v) => t.RemovedAnnouncementFormat = v);
        }

        if (PutBackAnnouncementFormat is not null)
        {
            bitTagsInput.TakeFromCascade(nameof(PutBackAnnouncementFormat), PutBackAnnouncementFormat, static t => t.PutBackAnnouncementFormat, static (t, v) => t.PutBackAnnouncementFormat = v);
        }

        if (ReorderAriaLabelFormat.HasValue())
        {
            bitTagsInput.TakeFromCascade(nameof(ReorderAriaLabelFormat), ReorderAriaLabelFormat, static t => t.ReorderAriaLabelFormat, static (t, v) => t.ReorderAriaLabelFormat = v);
        }

        if (ReorderDropAriaLabelFormat.HasValue())
        {
            bitTagsInput.TakeFromCascade(nameof(ReorderDropAriaLabelFormat), ReorderDropAriaLabelFormat, static t => t.ReorderDropAriaLabelFormat, static (t, v) => t.ReorderDropAriaLabelFormat = v);
        }

        if (ReorderIcon is not null)
        {
            bitTagsInput.TakeFromCascade(nameof(ReorderIcon), ReorderIcon, static t => t.ReorderIcon, static (t, v) => t.ReorderIcon = v);
        }

        if (ReorderIconName.HasValue())
        {
            bitTagsInput.TakeFromCascade(nameof(ReorderIconName), ReorderIconName, static t => t.ReorderIconName, static (t, v) => t.ReorderIconName = v);
        }

        if (ReorderTitle.HasValue())
        {
            bitTagsInput.TakeFromCascade(nameof(ReorderTitle), ReorderTitle, static t => t.ReorderTitle, static (t, v) => t.ReorderTitle = v);
        }

        if (RestrictToSuggestions.HasValue)
        {
            bitTagsInput.TakeFromCascade(nameof(RestrictToSuggestions), RestrictToSuggestions.Value, static t => t.RestrictToSuggestions, static (t, v) => t.RestrictToSuggestions = v);
        }

        if (Separators is not null)
        {
            bitTagsInput.TakeFromCascade(nameof(Separators), Separators, static t => t.Separators, static (t, v) => t.Separators = v);
        }

        if (ShowClearButton.HasValue)
        {
            bitTagsInput.TakeFromCascade(nameof(ShowClearButton), ShowClearButton.Value, static t => t.ShowClearButton, static (t, v) => t.ShowClearButton = v);
        }

        if (ShowCounter.HasValue)
        {
            bitTagsInput.TakeFromCascade(nameof(ShowCounter), ShowCounter.Value, static t => t.ShowCounter, static (t, v) => t.ShowCounter = v);
        }

        if (ShowInvalidMessage.HasValue)
        {
            bitTagsInput.TakeFromCascade(nameof(ShowInvalidMessage), ShowInvalidMessage.Value, static t => t.ShowInvalidMessage, static (t, v) => t.ShowInvalidMessage = v);
        }

        if (Size.HasValue)
        {
            bitTagsInput.TakeFromCascade(nameof(Size), Size.Value, static t => t.Size, static (t, v) => t.Size = v);
        }

        if (SpellCheck.HasValue)
        {
            bitTagsInput.TakeFromCascade(nameof(SpellCheck), SpellCheck.Value, static t => t.SpellCheck, static (t, v) => t.SpellCheck = v);
        }

        if (Styles is not null)
        {
            bitTagsInput.TakeFromCascade(nameof(Styles), Styles, static t => t.Styles, static (t, v) => t.Styles = v);
        }

        if (Suffix.HasValue())
        {
            bitTagsInput.TakeFromCascade(nameof(Suffix), Suffix, static t => t.Suffix, static (t, v) => t.Suffix = v);
        }

        if (Suggestions is not null)
        {
            bitTagsInput.TakeFromCascade(nameof(Suggestions), Suggestions, static t => t.Suggestions, static (t, v) => t.Suggestions = v);
        }

        if (TagAriaDescription is not null)
        {
            bitTagsInput.TakeFromCascade(nameof(TagAriaDescription), TagAriaDescription, static t => t.TagAriaDescription, static (t, v) => t.TagAriaDescription = v);
        }

        if (TagCountAriaDescriptionFormat is not null)
        {
            bitTagsInput.TakeFromCascade(nameof(TagCountAriaDescriptionFormat), TagCountAriaDescriptionFormat, static t => t.TagCountAriaDescriptionFormat, static (t, v) => t.TagCountAriaDescriptionFormat = v);
        }

        if (TagsAriaLabel.HasValue())
        {
            bitTagsInput.TakeFromCascade(nameof(TagsAriaLabel), TagsAriaLabel, static t => t.TagsAriaLabel, static (t, v) => t.TagsAriaLabel = v);
        }

        if (TagsPlaceholder.HasValue())
        {
            bitTagsInput.TakeFromCascade(nameof(TagsPlaceholder), TagsPlaceholder, static t => t.TagsPlaceholder, static (t, v) => t.TagsPlaceholder = v);
        }

        if (TagVariant.HasValue)
        {
            bitTagsInput.TakeFromCascade(nameof(TagVariant), TagVariant.Value, static t => t.TagVariant, static (t, v) => t.TagVariant = v);
        }

        if (ThrottleTime.HasValue)
        {
            bitTagsInput.TakeFromCascade(nameof(ThrottleTime), ThrottleTime.Value, static t => t.ThrottleTime, static (t, v) => t.ThrottleTime = v);
        }

        if (Transformer is not null)
        {
            bitTagsInput.TakeFromCascade(nameof(Transformer), Transformer, static t => t.Transformer, static (t, v) => t.Transformer = v);
        }

        if (Validator is not null)
        {
            bitTagsInput.TakeFromCascade(nameof(Validator), Validator, static t => t.Validator, static (t, v) => t.Validator = v);
        }

        if (Variant.HasValue)
        {
            bitTagsInput.TakeFromCascade(nameof(Variant), Variant.Value, static t => t.Variant, static (t, v) => t.Variant = v);
        }
    }
}
