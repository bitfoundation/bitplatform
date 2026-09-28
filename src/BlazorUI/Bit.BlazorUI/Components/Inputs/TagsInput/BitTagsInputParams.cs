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

        if (AddedAnnouncementFormat is not null && bitTagsInput.HasNotBeenSet(nameof(AddedAnnouncementFormat)))
        {
            bitTagsInput.AddedAnnouncementFormat = AddedAnnouncementFormat;
        }

        if (AddedManyAnnouncementFormat is not null && bitTagsInput.HasNotBeenSet(nameof(AddedManyAnnouncementFormat)))
        {
            bitTagsInput.AddedManyAnnouncementFormat = AddedManyAnnouncementFormat;
        }

        if (AllowReorder.HasValue && bitTagsInput.HasNotBeenSet(nameof(AllowReorder)))
        {
            bitTagsInput.AllowReorder = AllowReorder.Value;
        }

        if (AutoComplete.HasValue() && bitTagsInput.HasNotBeenSet(nameof(AutoComplete)))
        {
            bitTagsInput.AutoComplete = AutoComplete;
        }

        if (AutoFocus.HasValue && bitTagsInput.HasNotBeenSet(nameof(AutoFocus)))
        {
            bitTagsInput.AutoFocus = AutoFocus.Value;
        }

        if (BackspaceEditsLastTag.HasValue && bitTagsInput.HasNotBeenSet(nameof(BackspaceEditsLastTag)))
        {
            bitTagsInput.BackspaceEditsLastTag = BackspaceEditsLastTag.Value;
        }

        if (CancelConfirmKeysOnEmpty.HasValue && bitTagsInput.HasNotBeenSet(nameof(CancelConfirmKeysOnEmpty)))
        {
            bitTagsInput.CancelConfirmKeysOnEmpty = CancelConfirmKeysOnEmpty.Value;
        }

        if (CanRemoveTag is not null && bitTagsInput.HasNotBeenSet(nameof(CanRemoveTag)))
        {
            bitTagsInput.CanRemoveTag = CanRemoveTag;
        }

        if (Classes is not null && bitTagsInput.HasNotBeenSet(nameof(Classes)))
        {
            bitTagsInput.Classes = Classes;

            bitTagsInput.ClassBuilder.Reset();
        }

        if (ClearButtonAriaLabel.HasValue() && bitTagsInput.HasNotBeenSet(nameof(ClearButtonAriaLabel)))
        {
            bitTagsInput.ClearButtonAriaLabel = ClearButtonAriaLabel;
        }

        if (ClearButtonIcon is not null && bitTagsInput.HasNotBeenSet(nameof(ClearButtonIcon)))
        {
            bitTagsInput.ClearButtonIcon = ClearButtonIcon;
        }

        if (ClearButtonIconName.HasValue() && bitTagsInput.HasNotBeenSet(nameof(ClearButtonIconName)))
        {
            bitTagsInput.ClearButtonIconName = ClearButtonIconName;
        }

        if (ClearButtonTitle.HasValue() && bitTagsInput.HasNotBeenSet(nameof(ClearButtonTitle)))
        {
            bitTagsInput.ClearButtonTitle = ClearButtonTitle;
        }

        if (ClearedAnnouncementFormat is not null && bitTagsInput.HasNotBeenSet(nameof(ClearedAnnouncementFormat)))
        {
            bitTagsInput.ClearedAnnouncementFormat = ClearedAnnouncementFormat;
        }

        if (ClearOnBlur.HasValue && bitTagsInput.HasNotBeenSet(nameof(ClearOnBlur)))
        {
            bitTagsInput.ClearOnBlur = ClearOnBlur.Value;
        }

        if (Color.HasValue && bitTagsInput.HasNotBeenSet(nameof(Color)))
        {
            bitTagsInput.Color = Color.Value;

            bitTagsInput.ClassBuilder.Reset();
        }

        if (Comparison.HasValue && bitTagsInput.HasNotBeenSet(nameof(Comparison)))
        {
            bitTagsInput.Comparison = Comparison.Value;
        }

        if (DebounceTime.HasValue && bitTagsInput.HasNotBeenSet(nameof(DebounceTime)))
        {
            bitTagsInput.DebounceTime = DebounceTime.Value;
        }

        if (Description.HasValue() && bitTagsInput.HasNotBeenSet(nameof(Description)))
        {
            bitTagsInput.Description = Description;
        }

        if (DismissAriaLabelFormat.HasValue() && bitTagsInput.HasNotBeenSet(nameof(DismissAriaLabelFormat)))
        {
            bitTagsInput.DismissAriaLabelFormat = DismissAriaLabelFormat;
        }

        if (DismissIcon is not null && bitTagsInput.HasNotBeenSet(nameof(DismissIcon)))
        {
            bitTagsInput.DismissIcon = DismissIcon;
        }

        if (DismissIconName.HasValue() && bitTagsInput.HasNotBeenSet(nameof(DismissIconName)))
        {
            bitTagsInput.DismissIconName = DismissIconName;
        }

        if (DismissTitle.HasValue() && bitTagsInput.HasNotBeenSet(nameof(DismissTitle)))
        {
            bitTagsInput.DismissTitle = DismissTitle;
        }

        if (Duplicates.HasValue && bitTagsInput.HasNotBeenSet(nameof(Duplicates)))
        {
            bitTagsInput.Duplicates = Duplicates.Value;
        }

        if (EditAriaLabelFormat.HasValue() && bitTagsInput.HasNotBeenSet(nameof(EditAriaLabelFormat)))
        {
            bitTagsInput.EditAriaLabelFormat = EditAriaLabelFormat;
        }

        if (EditableTags.HasValue && bitTagsInput.HasNotBeenSet(nameof(EditableTags)))
        {
            bitTagsInput.EditableTags = EditableTags.Value;
        }

        if (EditedAnnouncementFormat is not null && bitTagsInput.HasNotBeenSet(nameof(EditedAnnouncementFormat)))
        {
            bitTagsInput.EditedAnnouncementFormat = EditedAnnouncementFormat;
        }

        if (EnterKeyHint.HasValue && bitTagsInput.HasNotBeenSet(nameof(EnterKeyHint)))
        {
            bitTagsInput.EnterKeyHint = EnterKeyHint.Value;

            bitTagsInput.OnSetEnterKeyHint();
        }

        if (GetInvalidMessage is not null && bitTagsInput.HasNotBeenSet(nameof(GetInvalidMessage)))
        {
            bitTagsInput.GetInvalidMessage = GetInvalidMessage;
        }

        if (GetTagName is not null && bitTagsInput.HasNotBeenSet(nameof(GetTagName)))
        {
            bitTagsInput.GetTagName = GetTagName;
        }

        if (GetTagClass is not null && bitTagsInput.HasNotBeenSet(nameof(GetTagClass)))
        {
            bitTagsInput.GetTagClass = GetTagClass;
        }

        if (GetTagStyle is not null && bitTagsInput.HasNotBeenSet(nameof(GetTagStyle)))
        {
            bitTagsInput.GetTagStyle = GetTagStyle;
        }

        if (InputMode.HasValue && bitTagsInput.HasNotBeenSet(nameof(InputMode)))
        {
            bitTagsInput.InputMode = InputMode.Value;

            bitTagsInput.OnSetInputMode();
        }

        if (InvalidAnnouncementFormat is not null && bitTagsInput.HasNotBeenSet(nameof(InvalidAnnouncementFormat)))
        {
            bitTagsInput.InvalidAnnouncementFormat = InvalidAnnouncementFormat;
        }

        if (IsLoading.HasValue && bitTagsInput.HasNotBeenSet(nameof(IsLoading)))
        {
            bitTagsInput.IsLoading = IsLoading.Value;
        }

        if (Label.HasValue() && bitTagsInput.HasNotBeenSet(nameof(Label)))
        {
            bitTagsInput.Label = Label;

            bitTagsInput.ClassBuilder.Reset();
        }

        if (LessTagsText.HasValue() && bitTagsInput.HasNotBeenSet(nameof(LessTagsText)))
        {
            bitTagsInput.LessTagsText = LessTagsText;
        }

        if (LoadingAriaLabel.HasValue() && bitTagsInput.HasNotBeenSet(nameof(LoadingAriaLabel)))
        {
            bitTagsInput.LoadingAriaLabel = LoadingAriaLabel;
        }

        if (MaxDisplayedTags.HasValue && bitTagsInput.HasNotBeenSet(nameof(MaxDisplayedTags)))
        {
            bitTagsInput.MaxDisplayedTags = MaxDisplayedTags.Value;
        }

        if (MaxLength.HasValue && bitTagsInput.HasNotBeenSet(nameof(MaxLength)))
        {
            bitTagsInput.MaxLength = MaxLength.Value;
        }

        if (MaxSuggestions.HasValue && bitTagsInput.HasNotBeenSet(nameof(MaxSuggestions)))
        {
            bitTagsInput.MaxSuggestions = MaxSuggestions.Value;
        }

        if (MaxTags.HasValue && bitTagsInput.HasNotBeenSet(nameof(MaxTags)))
        {
            bitTagsInput.MaxTags = MaxTags.Value;
        }

        if (MinLength.HasValue && bitTagsInput.HasNotBeenSet(nameof(MinLength)))
        {
            bitTagsInput.MinLength = MinLength.Value;
        }

        if (MoreTagsAriaLabelFormat.HasValue() && bitTagsInput.HasNotBeenSet(nameof(MoreTagsAriaLabelFormat)))
        {
            bitTagsInput.MoreTagsAriaLabelFormat = MoreTagsAriaLabelFormat;
        }

        if (MoreTagsFormat.HasValue() && bitTagsInput.HasNotBeenSet(nameof(MoreTagsFormat)))
        {
            bitTagsInput.MoreTagsFormat = MoreTagsFormat;
        }

        if (MovedAnnouncementFormat is not null && bitTagsInput.HasNotBeenSet(nameof(MovedAnnouncementFormat)))
        {
            bitTagsInput.MovedAnnouncementFormat = MovedAnnouncementFormat;
        }

        if (NoAddOnBlur.HasValue && bitTagsInput.HasNotBeenSet(nameof(NoAddOnBlur)))
        {
            bitTagsInput.NoAddOnBlur = NoAddOnBlur.Value;
        }

        if (NoAddOnTab.HasValue && bitTagsInput.HasNotBeenSet(nameof(NoAddOnTab)))
        {
            bitTagsInput.NoAddOnTab = NoAddOnTab.Value;
        }

        if (NoBackspaceRemove.HasValue && bitTagsInput.HasNotBeenSet(nameof(NoBackspaceRemove)))
        {
            bitTagsInput.NoBackspaceRemove = NoBackspaceRemove.Value;
        }

        if (NoBorder.HasValue && bitTagsInput.HasNotBeenSet(nameof(NoBorder)))
        {
            bitTagsInput.NoBorder = NoBorder.Value;

            bitTagsInput.ClassBuilder.Reset();
        }

        if (NoClearOnEscape.HasValue && bitTagsInput.HasNotBeenSet(nameof(NoClearOnEscape)))
        {
            bitTagsInput.NoClearOnEscape = NoClearOnEscape.Value;
        }

        if (NoInvalidHighlight.HasValue && bitTagsInput.HasNotBeenSet(nameof(NoInvalidHighlight)))
        {
            bitTagsInput.NoInvalidHighlight = NoInvalidHighlight.Value;
        }

        if (NoTrim.HasValue && bitTagsInput.HasNotBeenSet(nameof(NoTrim)))
        {
            bitTagsInput.NoTrim = NoTrim.Value;
        }

        if (Pattern.HasValue() && bitTagsInput.HasNotBeenSet(nameof(Pattern)))
        {
            bitTagsInput.Pattern = Pattern;

            bitTagsInput.OnSetPattern();
        }

        if (PickedUpAnnouncementFormat is not null && bitTagsInput.HasNotBeenSet(nameof(PickedUpAnnouncementFormat)))
        {
            bitTagsInput.PickedUpAnnouncementFormat = PickedUpAnnouncementFormat;
        }

        if (Placeholder.HasValue() && bitTagsInput.HasNotBeenSet(nameof(Placeholder)))
        {
            bitTagsInput.Placeholder = Placeholder;
        }

        if (Prefix.HasValue() && bitTagsInput.HasNotBeenSet(nameof(Prefix)))
        {
            bitTagsInput.Prefix = Prefix;
        }

        if (RemovedAnnouncementFormat is not null && bitTagsInput.HasNotBeenSet(nameof(RemovedAnnouncementFormat)))
        {
            bitTagsInput.RemovedAnnouncementFormat = RemovedAnnouncementFormat;
        }

        if (PutBackAnnouncementFormat is not null && bitTagsInput.HasNotBeenSet(nameof(PutBackAnnouncementFormat)))
        {
            bitTagsInput.PutBackAnnouncementFormat = PutBackAnnouncementFormat;
        }

        if (ReorderAriaLabelFormat.HasValue() && bitTagsInput.HasNotBeenSet(nameof(ReorderAriaLabelFormat)))
        {
            bitTagsInput.ReorderAriaLabelFormat = ReorderAriaLabelFormat;
        }

        if (ReorderDropAriaLabelFormat.HasValue() && bitTagsInput.HasNotBeenSet(nameof(ReorderDropAriaLabelFormat)))
        {
            bitTagsInput.ReorderDropAriaLabelFormat = ReorderDropAriaLabelFormat;
        }

        if (ReorderIcon is not null && bitTagsInput.HasNotBeenSet(nameof(ReorderIcon)))
        {
            bitTagsInput.ReorderIcon = ReorderIcon;
        }

        if (ReorderIconName.HasValue() && bitTagsInput.HasNotBeenSet(nameof(ReorderIconName)))
        {
            bitTagsInput.ReorderIconName = ReorderIconName;
        }

        if (ReorderTitle.HasValue() && bitTagsInput.HasNotBeenSet(nameof(ReorderTitle)))
        {
            bitTagsInput.ReorderTitle = ReorderTitle;
        }

        if (RestrictToSuggestions.HasValue && bitTagsInput.HasNotBeenSet(nameof(RestrictToSuggestions)))
        {
            bitTagsInput.RestrictToSuggestions = RestrictToSuggestions.Value;
        }

        if (Separators is not null && bitTagsInput.HasNotBeenSet(nameof(Separators)))
        {
            bitTagsInput.Separators = Separators;

            bitTagsInput.OnSetSeparators();
        }

        if (ShowClearButton.HasValue && bitTagsInput.HasNotBeenSet(nameof(ShowClearButton)))
        {
            bitTagsInput.ShowClearButton = ShowClearButton.Value;
        }

        if (ShowCounter.HasValue && bitTagsInput.HasNotBeenSet(nameof(ShowCounter)))
        {
            bitTagsInput.ShowCounter = ShowCounter.Value;
        }

        if (ShowInvalidMessage.HasValue && bitTagsInput.HasNotBeenSet(nameof(ShowInvalidMessage)))
        {
            bitTagsInput.ShowInvalidMessage = ShowInvalidMessage.Value;
        }

        if (Size.HasValue && bitTagsInput.HasNotBeenSet(nameof(Size)))
        {
            bitTagsInput.Size = Size.Value;

            bitTagsInput.ClassBuilder.Reset();
        }

        if (SpellCheck.HasValue && bitTagsInput.HasNotBeenSet(nameof(SpellCheck)))
        {
            bitTagsInput.SpellCheck = SpellCheck.Value;
        }

        if (Styles is not null && bitTagsInput.HasNotBeenSet(nameof(Styles)))
        {
            bitTagsInput.Styles = Styles;

            bitTagsInput.StyleBuilder.Reset();
        }

        if (Suffix.HasValue() && bitTagsInput.HasNotBeenSet(nameof(Suffix)))
        {
            bitTagsInput.Suffix = Suffix;
        }

        if (Suggestions is not null && bitTagsInput.HasNotBeenSet(nameof(Suggestions)))
        {
            bitTagsInput.Suggestions = Suggestions;
        }

        if (TagAriaDescription is not null && bitTagsInput.HasNotBeenSet(nameof(TagAriaDescription)))
        {
            bitTagsInput.TagAriaDescription = TagAriaDescription;
        }

        if (TagCountAriaDescriptionFormat is not null && bitTagsInput.HasNotBeenSet(nameof(TagCountAriaDescriptionFormat)))
        {
            bitTagsInput.TagCountAriaDescriptionFormat = TagCountAriaDescriptionFormat;
        }

        if (TagsAriaLabel.HasValue() && bitTagsInput.HasNotBeenSet(nameof(TagsAriaLabel)))
        {
            bitTagsInput.TagsAriaLabel = TagsAriaLabel;
        }

        if (TagsPlaceholder.HasValue() && bitTagsInput.HasNotBeenSet(nameof(TagsPlaceholder)))
        {
            bitTagsInput.TagsPlaceholder = TagsPlaceholder;
        }

        if (TagVariant.HasValue && bitTagsInput.HasNotBeenSet(nameof(TagVariant)))
        {
            bitTagsInput.TagVariant = TagVariant.Value;

            bitTagsInput.ClassBuilder.Reset();
        }

        if (ThrottleTime.HasValue && bitTagsInput.HasNotBeenSet(nameof(ThrottleTime)))
        {
            bitTagsInput.ThrottleTime = ThrottleTime.Value;
        }

        if (Transformer is not null && bitTagsInput.HasNotBeenSet(nameof(Transformer)))
        {
            bitTagsInput.Transformer = Transformer;
        }

        if (Validator is not null && bitTagsInput.HasNotBeenSet(nameof(Validator)))
        {
            bitTagsInput.Validator = Validator;
        }

        if (Variant.HasValue && bitTagsInput.HasNotBeenSet(nameof(Variant)))
        {
            bitTagsInput.Variant = Variant.Value;

            bitTagsInput.ClassBuilder.Reset();
        }
    }
}
