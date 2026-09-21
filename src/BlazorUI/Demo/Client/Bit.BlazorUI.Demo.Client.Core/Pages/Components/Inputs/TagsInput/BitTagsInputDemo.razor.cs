using System.Text.RegularExpressions;

namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Inputs.TagsInput;

public partial class BitTagsInputDemo
{
    private readonly List<ComponentParameter> componentParameters =
    [
        new()
        {
            Name = "AllowReorder",
            Type = "bool",
            DefaultValue = "false",
            Description = "Lets a tag be moved within the list, either by dragging it onto the position it should take or from the keyboard: Alt with the arrow keys walks the focused tag one position at a time, and Alt with Home or End sends it to either end.",
        },
        new()
        {
            Name = "AddedAnnouncementFormat",
            Type = "string?",
            DefaultValue = "null",
            Description = "The format of the message announced by screen readers when a tag is added, where {0} is the tag. The default is \"{0} added.\". An empty string keeps the addition from being announced.",
        },
        new()
        {
            Name = "AddedManyAnnouncementFormat",
            Type = "string?",
            DefaultValue = "null",
            Description = "The format of the message announced by screen readers when several tags are added at once (a pasted list, most of the time), where {0} is how many of them there were. The default is \"{0} tags added.\". An empty string keeps the addition from being announced. A single tag is always announced with AddedAnnouncementFormat instead.",
        },
        new()
        {
            Name = "AutoComplete",
            Type = "string?",
            DefaultValue = "off",
            Description = "Sets the autocomplete html attribute of the input element. It is off by default, since the browser's own autofill would otherwise be offered over the suggestion list of the field - and what it saved for a single line text box is the last tag that was typed rather than the list the field holds.",
        },
        new()
        {
            Name = "AutoFocus",
            Type = "bool",
            DefaultValue = "false",
            Description = "Whether the input should receive focus on first render.",
        },
        new()
        {
            Name = "BackspaceEditsLastTag",
            Type = "bool",
            DefaultValue = "false",
            Description = "Turns the Backspace pressed on an empty input from a removal into a correction: the last tag is taken off the list and its text is put back into the input, ready to be fixed and confirmed again. NoBackspaceRemove still wins over it, the key doing nothing at all then.",
        },
        new()
        {
            Name = "CancelConfirmKeysOnEmpty",
            Type = "bool",
            DefaultValue = "false",
            Description = "When true, pressing Enter while the input is empty does not suppress the event, allowing it to propagate (e.g., to submit a parent form).",
        },
        new()
        {
            Name = "CanRemoveTag",
            Type = "Func<string, bool>?",
            DefaultValue = "null",
            Description = "A predicate deciding which tags the user is allowed to take off the list. A tag it turns down is drawn without a dismiss button, ignores the Delete and Backspace keys, is left alone by the Backspace pressed on the empty input, and stays behind when the field is cleared. It is still editable and still movable. RemoveTagAsync and RemoveTagAtAsync name a tag outright and are not held back by it.",
        },
        new()
        {
            Name = "Classes",
            Type = "BitTagsInputClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS classes for different parts of the tags input.",
            LinkType = LinkType.Link,
            Href = "#tagsinput-class-styles",
        },
        new()
        {
            Name = "ClearButtonAriaLabel",
            Type = "string?",
            DefaultValue = "null",
            Description = "Accessible label of the clear button, for the benefit of screen readers and of localization. The default is \"Clear all tags\".",
        },
        new()
        {
            Name = "ClearButtonIcon",
            Type = "BitIconInfo?",
            DefaultValue = "null",
            Description = "Gets or sets the icon of the clear button using custom CSS classes for external icon libraries. Takes precedence over ClearButtonIconName when both are set.",
            LinkType = LinkType.Link,
            Href = "#bit-icon-info",
        },
        new()
        {
            Name = "ClearButtonIconName",
            Type = "string?",
            DefaultValue = "Clear",
            Description = "Gets or sets the name of the icon of the clear button from the built-in Fluent UI icons.",
        },
        new()
        {
            Name = "ClearButtonTitle",
            Type = "string?",
            DefaultValue = "null",
            Description = "The tooltip of the clear button, which is what the pointer reads rather than the screen reader. It falls back to the ClearButtonAriaLabel and then to \"Clear all tags\".",
        },
        new()
        {
            Name = "ClearedAnnouncementFormat",
            Type = "string?",
            DefaultValue = "null",
            Description = "The format of the message announced by screen readers when every tag is removed at once, where {0} is how many of them there were. The default is \"{0} tags removed.\". An empty string keeps the clearing from being announced.",
        },
        new()
        {
            Name = "ClearOnBlur",
            Type = "bool",
            DefaultValue = "false",
            Description = "Throws away whatever text is still sitting in the input when the field loses the focus. It runs after the text has had its chance to become a tag, so on its own it only takes away what was refused; paired with NoAddOnBlur it makes leaving the field cancel what was being typed.",
        },
        new()
        {
            Name = "Color",
            Type = "BitColor?",
            DefaultValue = "null",
            Description = "The color role of the tags input (Primary by default). It is carried by the tags themselves, the way a BitTag carries it, and by the border and the focus ring of the focused field. How much of it the tags are painted with is decided by the TagVariant.",
            LinkType = LinkType.Link,
            Href = "#color-enum",
        },
        new()
        {
            Name = "Comparison",
            Type = "StringComparison",
            DefaultValue = "StringComparison.Ordinal",
            Description = "The string comparison used to tell one tag from another, which is what decides whether a tag is a duplicate of one that is already in the list.",
        },
        new()
        {
            Name = "DebounceTime",
            Type = "int",
            DefaultValue = "0",
            Description = "How long, in milliseconds, the field waits for the typing to stop before raising OnInput, which turns a suggestion list fetched from a server into one request per word rather than one per keystroke. Only the callback waits: the typed text is tracked as it is typed, and an emptying the component itself caused is reported at once, taking a pending callback down with it. 0 means no wait.",
        },
        new()
        {
            Name = "Description",
            Type = "string?",
            DefaultValue = "null",
            Description = "A hint rendered under the field, referenced by the input through its aria-describedby attribute so it is announced along with the field.",
        },
        new()
        {
            Name = "DescriptionTemplate",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "A custom template rendered in place of the Description, referenced by the input through its aria-describedby attribute just the same.",
        },
        new()
        {
            Name = "DismissAriaLabelFormat",
            Type = "string?",
            DefaultValue = "null",
            Description = "The format of the accessible label of the dismiss button of each tag, where {0} is the tag. The default is \"Remove {0}\".",
        },
        new()
        {
            Name = "DismissIcon",
            Type = "BitIconInfo?",
            DefaultValue = "null",
            Description = "Gets or sets the icon for the dismiss button using custom CSS classes for external icon libraries. Takes precedence over DismissIconName when both are set.",
            LinkType = LinkType.Link,
            Href = "#bit-icon-info",
        },
        new()
        {
            Name = "DismissIconName",
            Type = "string?",
            DefaultValue = "Cancel",
            Description = "Gets or sets the name of the icon for the dismiss button from the built-in Fluent UI icons.",
        },
        new()
        {
            Name = "DismissTitle",
            Type = "string?",
            DefaultValue = "null",
            Description = "The title (tooltip) of the dismiss button of each tag. The default is \"Remove\".",
        },
        new()
        {
            Name = "Duplicates",
            Type = "bool",
            DefaultValue = "false",
            Description = "Whether duplicate tags are allowed. Which tags count as duplicates of one another is decided by the Comparison.",
        },
        new()
        {
            Name = "EditableTags",
            Type = "bool",
            DefaultValue = "false",
            Description = "Lets a tag be corrected in place: double clicking a tag (or pressing Enter or F2 on the focused one) turns it into a little input, Enter commits the new text and Escape puts the old one back. Committing an empty text removes the tag.",
        },
        new()
        {
            Name = "EditAriaLabelFormat",
            Type = "string?",
            DefaultValue = "null",
            Description = "The format of the accessible label of the little input that replaces a tag while it is being edited in place, where {0} is the tag. The default is \"Edit {0}\".",
        },
        new()
        {
            Name = "EditedAnnouncementFormat",
            Type = "string?",
            DefaultValue = "null",
            Description = "The format of the message announced by screen readers when a tag is edited, where {0} is the tag as it now reads. The default is \"{0} updated.\". An empty string keeps the edit from being announced.",
        },
        new()
        {
            Name = "EnterKeyHint",
            Type = "BitEnterKeyHint?",
            DefaultValue = "null",
            Description = "Sets the enterkeyhint html attribute of the input element, which decides the label a virtual keyboard draws on its return key. The key confirms a tag here, so Done and Next are the ones that describe it on a phone.",
            LinkType = LinkType.Link,
            Href = "#enter-key-hint-enum",
        },
        new()
        {
            Name = "GetTagClass",
            Type = "Func<string, string?>?",
            DefaultValue = "null",
            Description = "A function returning extra CSS classes for a single tag, which is what tells one chip apart from the next. It is called for every drawn tag on every render, so it should be a lookup rather than a computation, and the classes are added to those the Classes give every chip alike.",
        },
        new()
        {
            Name = "GetTagStyle",
            Type = "Func<string, string?>?",
            DefaultValue = "null",
            Description = "A function returning extra inline CSS styles for a single tag, the counterpart of GetTagClass. It is appended after the Tag and FocusedTag of the Styles, so it wins over both.",
        },
        new()
        {
            Name = "InputMode",
            Type = "BitInputMode?",
            DefaultValue = "null",
            Description = "Sets the inputmode html attribute of the input element, which decides the virtual keyboard a phone opens over the field. It changes nothing about what the field accepts - that is what Pattern and Validator are for - only about which keys the user is given to type it with.",
            LinkType = LinkType.Link,
            Href = "#input-mode-enum",
        },
        new()
        {
            Name = "IsLoading",
            Type = "bool",
            DefaultValue = "false",
            Description = "Draws a spinner at the end of the field, for the wait the field itself is the cause of: the suggestions being fetched for what is being typed, the tag being checked against a server. It is an indeterminate progressbar rather than a decoration, and it changes nothing about what the field accepts.",
        },
        new()
        {
            Name = "InvalidAnnouncementFormat",
            Type = "string?",
            DefaultValue = "null",
            Description = "The format of the message announced by screen readers when a tag is rejected, where {0} is the tag. The default is \"{0} was not added.\". An empty string keeps the rejection from being announced.",
        },
        new()
        {
            Name = "Label",
            Type = "string?",
            DefaultValue = "null",
            Description = "The label displayed above the input.",
        },
        new()
        {
            Name = "LabelTemplate",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "A custom template for the label.",
        },
        new()
        {
            Name = "LessTagsText",
            Type = "string?",
            DefaultValue = "null",
            Description = "The label of the chip that folds the tags back once MaxDisplayedTags unfolded them. The default is \"Show less\".",
        },
        new()
        {
            Name = "LoadingAriaLabel",
            Type = "string?",
            DefaultValue = "null",
            Description = "The accessible name of the spinner IsLoading draws, which is the whole of what a screen reader has to go on. The default is \"Loading\".",
        },
        new()
        {
            Name = "MaxDisplayedTags",
            Type = "int",
            DefaultValue = "0",
            Description = "The number of tags drawn before the rest of them are folded away behind a chip that says how many are left, which unfolds the list and folds it back. Only how much of the value is drawn changes, never the value itself. 0 means all of them.",
        },
        new()
        {
            Name = "MaxLength",
            Type = "int",
            DefaultValue = "0",
            Description = "The maximum number of characters allowed for each individual tag. Text beyond it is truncated rather than rejected. 0 means no limit.",
        },
        new()
        {
            Name = "MaxSuggestions",
            Type = "int",
            DefaultValue = "0",
            Description = "The number of values the suggestion list is allowed to offer at once. Beyond this ceiling only the values that hold what is being typed are offered, and only as many of them as it allows, which is what keeps a catalogue of thousands from being written into the page in full on every keystroke. 0 means all of them.",
        },
        new()
        {
            Name = "MaxTags",
            Type = "int",
            DefaultValue = "0",
            Description = "The maximum number of tags allowed. Once it is reached, further tags are rejected with the MaxTags reason. 0 means no limit.",
        },
        new()
        {
            Name = "MinLength",
            Type = "int",
            DefaultValue = "0",
            Description = "The minimum number of characters a tag has to hold to be accepted. Shorter ones are rejected with the MinLength reason. 0 means no limit.",
        },
        new()
        {
            Name = "MoreTagsFormat",
            Type = "string?",
            DefaultValue = "null",
            Description = "The format of the label of the chip that stands for the tags MaxDisplayedTags folded away, where {0} is how many of them there are. The default is \"+{0}\".",
        },
        new()
        {
            Name = "MoreTagsAriaLabelFormat",
            Type = "string?",
            DefaultValue = "null",
            Description = "The format of the accessible label of that same chip, where {0} is how many tags are folded away. The default is \"Show {0} more tags\", since \"+3\" read out on its own says nothing about what pressing it would do.",
        },
        new()
        {
            Name = "MovedAnnouncementFormat",
            Type = "string?",
            DefaultValue = "null",
            Description = "The format of the message announced by screen readers when a tag is moved with AllowReorder, where {0} is the tag, {1} its new one based position and {2} the number of tags. The default is \"{0} moved to position {1} of {2}.\".",
        },
        new()
        {
            Name = "NoAddOnBlur",
            Type = "bool",
            DefaultValue = "false",
            Description = "Stops the text left in the input from being committed as a tag when the field loses the focus.",
        },
        new()
        {
            Name = "NoAddOnTab",
            Type = "bool",
            DefaultValue = "false",
            Description = "Stops the Tab key from committing the text left in the input, leaving it to do nothing but move the focus.",
        },
        new()
        {
            Name = "NoBackspaceRemove",
            Type = "bool",
            DefaultValue = "false",
            Description = "Stops the Backspace key from removing the last tag when the input is empty.",
        },
        new()
        {
            Name = "NoBorder",
            Type = "bool",
            DefaultValue = "false",
            Description = "Removes the default border of the tags input.",
        },
        new()
        {
            Name = "NoClearOnEscape",
            Type = "bool",
            DefaultValue = "false",
            Description = "Leaves the Escape key alone, so that it takes back neither the text being typed nor the tags the clear button would empty - which hands the key back to the modal or the callout the field sits in.",
        },
        new()
        {
            Name = "NoInvalidHighlight",
            Type = "bool",
            DefaultValue = "false",
            Description = "Stops the field from marking a tag it refused. The mark is what makes a rejection visible to everyone rather than only to a screen reader: the field wears its invalid color until the user types again, and a tag refused as a duplicate marks the one already in the list that it collided with.",
        },
        new()
        {
            Name = "NoTrim",
            Type = "bool",
            DefaultValue = "false",
            Description = "Keeps the leading and trailing whitespace of a tag instead of trimming it away.",
        },
        new()
        {
            Name = "OnAdd",
            Type = "EventCallback<IReadOnlyList<string>>",
            Description = "Callback for when one or more tags are added. Receives the list of all newly added tags.",
        },
        new()
        {
            Name = "OnBeforeAdd",
            Type = "EventCallback<BitTagsInputBeforeArgs>",
            Description = "Callback invoked before a tag is added. Set args.Cancel = true to cancel the add.",
            LinkType = LinkType.Link,
            Href = "#before-args",
        },
        new()
        {
            Name = "OnBeforeRemove",
            Type = "EventCallback<BitTagsInputBeforeArgs>",
            Description = "Callback invoked before a tag is removed. Set args.Cancel = true to cancel the remove.",
            LinkType = LinkType.Link,
            Href = "#before-args",
        },
        new()
        {
            Name = "OnBeforeClear",
            Type = "EventCallback<BitTagsInputClearArgs>",
            Description = "Callback invoked before every tag is removed at once, by the clear button, the Escape key or the Clear method, carrying the whole list that is about to go. Set args.Cancel = true to leave it as it is.",
            LinkType = LinkType.Link,
            Href = "#clear-args",
        },
        new()
        {
            Name = "OnClear",
            Type = "EventCallback<IReadOnlyList<string>>",
            Description = "Callback for when every tag is removed at once, by the clear button or by the Clear method. It receives the tags that were removed.",
        },
        new()
        {
            Name = "OnEdit",
            Type = "EventCallback<BitTagsInputEditArgs>",
            Description = "Callback invoked when an inline edit of a tag is about to be committed, carrying both the old and the new text. Set args.Cancel = true to leave the tag as it was.",
            LinkType = LinkType.Link,
            Href = "#edit-args",
        },
        new()
        {
            Name = "OnFocusIn",
            Type = "EventCallback<FocusEventArgs>",
            Description = "Callback for when the input receives focus.",
        },
        new()
        {
            Name = "OnFocusOut",
            Type = "EventCallback<FocusEventArgs>",
            Description = "Callback for when the input loses focus.",
        },
        new()
        {
            Name = "OnInput",
            Type = "EventCallback<string>",
            Description = "Callback for when the text of the input changes, which is what an external suggestion list is driven by.",
        },
        new()
        {
            Name = "OnInvalid",
            Type = "EventCallback<BitTagsInputInvalidArgs>",
            Description = "Callback for when a tag is rejected, carrying the tag along with the rule that rejected it.",
            LinkType = LinkType.Link,
            Href = "#invalid-args",
        },
        new()
        {
            Name = "OnKeyDown",
            Type = "EventCallback<KeyboardEventArgs>",
            Description = "Callback for when a key is pressed down on the input. It is invoked for every key, including the ones the component handles itself.",
        },
        new()
        {
            Name = "OnRemove",
            Type = "EventCallback<string>",
            Description = "Callback for when a tag is removed.",
        },
        new()
        {
            Name = "OnReorder",
            Type = "EventCallback<BitTagsInputReorderArgs>",
            Description = "Callback for when a tag is moved within the list with AllowReorder, carrying the tag along with the positions it left and took.",
            LinkType = LinkType.Link,
            Href = "#reorder-args",
        },
        new()
        {
            Name = "OnTagClick",
            Type = "EventCallback&lt;string&gt;",
            DefaultValue = "",
            Description = "Callback for when a tag is clicked, carrying the tag. It changes nothing about what the click already does, the tag still taking the focus; the dismiss button is not a click on the tag, and neither is the second click of the double click that opens the inline edit.",
        },
        new()
        {
            Name = "OnTagExists",
            Type = "EventCallback<string>",
            Description = "Callback fired when a duplicate tag is attempted and Duplicates is false.",
        },
        new()
        {
            Name = "Pattern",
            Type = "string?",
            DefaultValue = "null",
            Description = "A regular expression that every tag has to match to be accepted. An unusable expression is ignored rather than breaking the input.",
        },
        new()
        {
            Name = "PickedUpAnnouncementFormat",
            Type = "string?",
            DefaultValue = "null",
            Description = "The format of the message announced by screen readers when a tag is picked up with its reorder handle, where {0} is the tag. The default names the tag and says how to put it down. An empty string keeps the pick up from being announced.",
        },
        new()
        {
            Name = "Placeholder",
            Type = "string?",
            DefaultValue = "null",
            Description = "The placeholder text of the input, shown while there is no tag in the list.",
        },
        new()
        {
            Name = "Prefix",
            Type = "string?",
            DefaultValue = "null",
            Description = "A short text drawn at the start of the field, in front of the tags, which is not part of the value. Since it never reaches the value, the label of the field has to say what it means on its own for a screen reader.",
        },
        new()
        {
            Name = "PrefixTemplate",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "A custom template drawn in place of the Prefix.",
        },
        new()
        {
            Name = "PutBackAnnouncementFormat",
            Type = "string?",
            DefaultValue = "null",
            Description = "The format of the message announced by screen readers when a picked up tag is put back where it came from, where {0} is the tag. The default is \"{0} put back.\".",
        },
        new()
        {
            Name = "RemovedAnnouncementFormat",
            Type = "string?",
            DefaultValue = "null",
            Description = "The format of the message announced by screen readers when a tag is removed, where {0} is the tag. The default is \"{0} removed.\". An empty string keeps the removal from being announced.",
        },
        new()
        {
            Name = "ReorderAriaLabelFormat",
            Type = "string?",
            DefaultValue = "null",
            Description = "The format of the accessible label of the reorder handle of each tag, where {0} is the tag. The default is \"Move {0}\".",
        },
        new()
        {
            Name = "ReorderDropAriaLabelFormat",
            Type = "string?",
            DefaultValue = "null",
            Description = "The format of the accessible label the other reorder handles take while one tag is picked up, where {0} is the tag being carried. The default is \"Move {0} here\".",
        },
        new()
        {
            Name = "ReorderIcon",
            Type = "BitIconInfo?",
            DefaultValue = "null",
            Description = "The icon of the reorder handle, as custom CSS classes for external icon libraries. It takes precedence over ReorderIconName.",
        },
        new()
        {
            Name = "ReorderIconName",
            Type = "string?",
            DefaultValue = "null",
            Description = "The name of the icon of the reorder handle from the built-in Fluent UI icons. It defaults to GripperBarVertical.",
        },
        new()
        {
            Name = "ReorderTitle",
            Type = "string?",
            DefaultValue = "null",
            Description = "The title (tooltip) of the reorder handle of each tag. The default is \"Move\".",
        },
        new()
        {
            Name = "RestrictToSuggestions",
            Type = "bool",
            DefaultValue = "false",
            Description = "Turns the Suggestions into the whole of what the field accepts: a tag that is not one of them is rejected with the NotSuggested reason, which is what a datalist on its own cannot do since it suggests rather than restricts.",
        },
        new()
        {
            Name = "Separators",
            Type = "IEnumerable<string>?",
            DefaultValue = "null",
            Description = "The character(s) that turn the typed text into a tag on top of the Enter key, which is the only one there is by default. The very same separators split a pasted list into a tag each, and a pasted text holding line breaks is joined over the first of them before it is split.",
        },
        new()
        {
            Name = "ShowClearButton",
            Type = "bool",
            DefaultValue = "false",
            Description = "Whether to render a button that removes every tag at once. It stays out of the tab order, the Escape key being its keyboard equivalent.",
        },
        new()
        {
            Name = "ShowCounter",
            Type = "bool",
            DefaultValue = "false",
            Description = "Whether to render the number of tags under the field, next to the Description, as a plain count or as \"count / MaxTags\" when there is a ceiling to reach.",
        },
        new()
        {
            Name = "Size",
            Type = "BitSize?",
            DefaultValue = "null",
            Description = "The size of the tags input.",
            LinkType = LinkType.Link,
            Href = "#size-enum",
        },
        new()
        {
            Name = "SpellCheck",
            Type = "bool?",
            DefaultValue = "false",
            Description = "Sets the spellcheck html attribute of the input element. It is off by default, a tag being a value rather than a sentence - an identifier or a hashtag underlined in red says only that the dictionary has not heard of it.",
        },
        new()
        {
            Name = "Suggestions",
            Type = "IEnumerable<string>?",
            DefaultValue = "null",
            Description = "The values offered to the user while typing, through the suggestion list the browser itself renders for a datalist. Picking one fills the input; the usual Enter (or a separator) turns it into a tag, so every validation rule still applies. Values already in the list are left out unless Duplicates allows them back in, and all of them are once the MaxTags ceiling is reached. See RestrictToSuggestions to make them the only accepted values.",
        },
        new()
        {
            Name = "Styles",
            Type = "BitTagsInputClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS styles for different parts of the tags input.",
            LinkType = LinkType.Link,
            Href = "#tagsinput-class-styles",
        },
        new()
        {
            Name = "Suffix",
            Type = "string?",
            DefaultValue = "null",
            Description = "A short text drawn at the end of the field, after everything else, which is not part of the value. Since it never reaches the value, the label of the field has to say what it means on its own for a screen reader.",
        },
        new()
        {
            Name = "SuffixTemplate",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "A custom template drawn in place of the Suffix.",
        },
        new()
        {
            Name = "TagTemplate",
            Type = "RenderFragment<string>?",
            DefaultValue = "null",
            Description = "A custom template for rendering each tag.",
        },
        new()
        {
            Name = "TagAriaDescription",
            Type = "string?",
            DefaultValue = "null",
            Description = "The sentence announced after each tag, telling what the keyboard can do with the one that has just been reached. It defaults to a sentence built from what the component was actually given (the inline edit, the reordering), and is left out entirely when neither is on. An empty string keeps it from being rendered at all.",
        },
        new()
        {
            Name = "TagsAriaLabel",
            Type = "string?",
            DefaultValue = "null",
            Description = "The accessible name of the list the tags form, which is what a screen reader announces before walking through them. The default is \"Tags\".",
        },
        new()
        {
            Name = "TagsPlaceholder",
            Type = "string?",
            DefaultValue = "null",
            Description = "The placeholder text of the input shown once there is at least one tag in the list, where the Placeholder would otherwise be replaced by nothing at all.",
        },
        new()
        {
            Name = "TagVariant",
            Type = "BitVariant?",
            DefaultValue = "null",
            Description = "How much of the Color the tags are painted with: Fill (the default) fills each chip with it, the way a BitTag is filled, Outline leaves the chip unfilled and draws the color as its rule and its text, and Text keeps only the text in it. It is independent of the Variant, which is about the frame of the field rather than the tags in it.",
            LinkType = LinkType.Link,
            Href = "#variant-enum",
        },
        new()
        {
            Name = "ThrottleTime",
            Type = "int",
            DefaultValue = "0",
            Description = "How long, in milliseconds, OnInput waits between two raises while the typing goes on, for a suggestion list that should keep up with the word rather than only answer once it is finished. DebounceTime wins over it when both are set. 0 means no limit.",
        },
        new()
        {
            Name = "Transformer",
            Type = "Func<string, string>?",
            DefaultValue = "null",
            Description = "A function applied to the text of a tag before anything else is done with it, which normalizes the tags of a list that has to stay consistent. It runs before every validation rule.",
        },
        new()
        {
            Name = "Validator",
            Type = "Func<string, bool>?",
            DefaultValue = "null",
            Description = "A predicate every tag has to satisfy to be accepted, for the rules a regular expression cannot express. Returning false rejects the tag with the Validator reason.",
        },
        new()
        {
            Name = "Variant",
            Type = "BitVariant?",
            DefaultValue = "null",
            Description = "The visual variant of the field: an outline (the default), a filled surface, or only an underline.",
            LinkType = LinkType.Link,
            Href = "#variant-enum",
        },
    ];

    private readonly List<ComponentCssVariable> componentCssVariables =
    [
        new()
        {
            Name = "--bit-TagsInput-font-size",
            DefaultValue = "Per Size, from the type ramp",
            Description = "Text size of the field. The helper text and the counter are derived from it, so one value resizes the whole component.",
        },
        new()
        {
            Name = "--bit-TagsInput-color",
            DefaultValue = "--bit-clr-fg-pri",
            Description = "Color of the text being typed into the input.",
        },
        new()
        {
            Name = "--bit-TagsInput-placeholder-color",
            DefaultValue = "--bit-clr-fg-ter",
            Description = "Color of the Placeholder and the TagsPlaceholder.",
        },
        new()
        {
            Name = "--bit-TagsInput-label-color",
            DefaultValue = "--bit-clr-fg-pri",
            Description = "Color of the label above the field.",
        },
        new()
        {
            Name = "--bit-TagsInput-required-color",
            DefaultValue = "--bit-clr-req",
            Description = "Color of the asterisk a Required label carries.",
        },
        new()
        {
            Name = "--bit-TagsInput-description-color",
            DefaultValue = "--bit-clr-fg-sec",
            Description = "Color of the helper text under the field. The error state overrides it with the invalid color.",
        },
        new()
        {
            Name = "--bit-TagsInput-counter-color",
            DefaultValue = "--bit-clr-fg-sec",
            Description = "Color of the tag counter ShowCounter draws.",
        },
        new()
        {
            Name = "--bit-TagsInput-affix-color",
            DefaultValue = "--bit-clr-fg-sec",
            Description = "Color of the Prefix and the Suffix.",
        },
        new()
        {
            Name = "--bit-TagsInput-background",
            DefaultValue = "Per Variant",
            Description = "Fill of the field: the page surface in Outline, the secondary surface in Fill, transparent in Text.",
        },
        new()
        {
            Name = "--bit-TagsInput-border-color",
            DefaultValue = "Per Variant",
            Description = "Rule around the field at rest.",
        },
        new()
        {
            Name = "--bit-TagsInput-hover-border-color",
            DefaultValue = "Per Variant",
            Description = "Rule around the hovered field (pointer devices only).",
        },
        new()
        {
            Name = "--bit-TagsInput-border-width",
            DefaultValue = "--bit-shp-brd-width",
            Description = "Thickness of that rule, and of the underline the Text variant keeps.",
        },
        new()
        {
            Name = "--bit-TagsInput-radius",
            DefaultValue = "--bit-shp-radius-control",
            Description = "Corner of the field and of its focus ring. The Text variant squares it off.",
        },
        new()
        {
            Name = "--bit-TagsInput-min-height",
            DefaultValue = "Per Size, --bit-siz-ctrl-*",
            Description = "Smallest height of the field, which is what lines an empty tags input up with the text fields and pickers beside it. It is a floor: the field still grows with every line of chips that wraps into it.",
        },
        new()
        {
            Name = "--bit-TagsInput-padding",
            DefaultValue = "Per Size",
            Description = "Inset between the field and the chips, the input and the affixes inside it.",
        },
        new()
        {
            Name = "--bit-TagsInput-gap",
            DefaultValue = "Per Size",
            Description = "Room between the chips, the input and the affixes, on both axes.",
        },
        new()
        {
            Name = "--bit-TagsInput-focus-color",
            DefaultValue = "The Color role's focus color",
            Description = "Color of the focus ring the field wears while anything inside it holds the focus.",
        },
        new()
        {
            Name = "--bit-TagsInput-focus-border-color",
            DefaultValue = "The Color role's main color",
            Description = "Rule around the focused field, and the focus ring of the clear button.",
        },
        new()
        {
            Name = "--bit-TagsInput-invalid-color",
            DefaultValue = "--bit-clr-err",
            Description = "Rule and helper text of a field failing validation and its focus ring, and the mark a refused tag leaves on the field and on the chip it collided with.",
        },
        new()
        {
            Name = "--bit-TagsInput-disabled-color",
            DefaultValue = "--bit-clr-fg-dis",
            Description = "Text of a disabled field, of its chips, its label, its helper text and its affixes.",
        },
        new()
        {
            Name = "--bit-TagsInput-disabled-background",
            DefaultValue = "--bit-clr-bg-dis",
            Description = "Fill of a disabled field and of the chips in it.",
        },
        new()
        {
            Name = "--bit-TagsInput-disabled-border-color",
            DefaultValue = "--bit-clr-brd-dis",
            Description = "Rule of a disabled field and of the chips in it.",
        },
        new()
        {
            Name = "--bit-TagsInput-tag-color",
            DefaultValue = "Per TagVariant, from the Color role",
            Description = "Text of a chip, and of the chip that folds the tags away.",
        },
        new()
        {
            Name = "--bit-TagsInput-tag-background",
            DefaultValue = "Per TagVariant, from the Color role",
            Description = "Fill of a chip. Setting it is how a field paints its chips apart from its accent.",
        },
        new()
        {
            Name = "--bit-TagsInput-tag-border-color",
            DefaultValue = "Per TagVariant, from the Color role",
            Description = "Rule of a chip, drawn in every tag variant and transparent where it is not painted, so switching variants never moves the text in a chip.",
        },
        new()
        {
            Name = "--bit-TagsInput-tag-border-width",
            DefaultValue = "--bit-shp-brd-width",
            Description = "Thickness of that rule.",
        },
        new()
        {
            Name = "--bit-TagsInput-tag-radius",
            DefaultValue = "--bit-shp-radius-chip",
            Description = "Corner of a chip. A pill takes 999px, a square 0.",
        },
        new()
        {
            Name = "--bit-TagsInput-tag-padding",
            DefaultValue = "Per Size",
            Description = "Inset of a chip. The block half is 0 on purpose: the height is set by the minimum height below, so the text stays centered whatever the chip holds.",
        },
        new()
        {
            Name = "--bit-TagsInput-tag-gap",
            DefaultValue = "0.1875rem",
            Description = "Room between a chip's text and its dismiss button.",
        },
        new()
        {
            Name = "--bit-TagsInput-tag-font-size",
            DefaultValue = "Per Size, from the type ramp",
            Description = "Text size of a chip, which is a step under the field's own at the Medium and Large sizes.",
        },
        new()
        {
            Name = "--bit-TagsInput-tag-min-height",
            DefaultValue = "Per Size",
            Description = "Smallest height of a chip, of the input and of the affixes, so a chip holding an icon or a template is exactly as tall as the one beside it.",
        },
        new()
        {
            Name = "--bit-TagsInput-tag-max-width",
            DefaultValue = "100%",
            Description = "Widest a chip grows before its text is cut off with an ellipsis. Set it to keep one long value from taking a whole line of the field.",
        },
        new()
        {
            Name = "--bit-TagsInput-tag-focus-color",
            DefaultValue = "Per TagVariant",
            Description = "The inset ring of the focused chip, of the focused dismiss button and of the chip a dragged one would land on.",
        },
        new()
        {
            Name = "--bit-TagsInput-tag-dragging-opacity",
            DefaultValue = "0.4",
            Description = "Alpha of the chip being dragged, which is what reads as a gap waiting to be filled rather than as a chip in two places at once.",
        },
        new()
        {
            Name = "--bit-TagsInput-dismiss-icon-size",
            DefaultValue = "1em",
            Description = "Glyph of a chip's dismiss button, relative to the chip's own text by default so it scales with the chip rather than with the field.",
        },
        new()
        {
            Name = "--bit-TagsInput-reorder-icon-size",
            DefaultValue = "1em of the chip's text",
            Description = "The glyph of the reorder handle AllowReorder draws on each chip.",
        },
        new()
        {
            Name = "--bit-TagsInput-icon-size",
            DefaultValue = "Per Size, --bit-siz-icon-*",
            Description = "Glyph of the clear button, and the diameter of the spinner, at the end of the field.",
        },
        new()
        {
            Name = "--bit-TagsInput-spinner-color",
            DefaultValue = "The Color role's main color",
            Description = "The turning arc of the spinner IsLoading draws.",
        },
        new()
        {
            Name = "--bit-TagsInput-spinner-track-color",
            DefaultValue = "--bit-clr-brd-sec",
            Description = "The track that arc turns in.",
        },
        new()
        {
            Name = "--bit-TagsInput-clear-color",
            DefaultValue = "--bit-clr-fg-sec",
            Description = "The clear button at rest.",
        },
        new()
        {
            Name = "--bit-TagsInput-clear-hover-color",
            DefaultValue = "--bit-clr-fg-pri",
            Description = "The clear button under the pointer.",
        },
        new()
        {
            Name = "--bit-TagsInput-toggle-hover-color",
            DefaultValue = "Per TagVariant",
            Description = "Text of the hovered chip that folds and unfolds the tags MaxDisplayedTags put away.",
        },
        new()
        {
            Name = "--bit-TagsInput-toggle-hover-background",
            DefaultValue = "Per TagVariant",
            Description = "Fill and rule of that same chip while it is hovered.",
        },
    ];

    private readonly List<ComponentSubClass> componentSubClasses =
    [
        new()
        {
            Id = "before-args",
            Title = "BitTagsInputBeforeArgs",
            Description = "Arguments passed to the OnBeforeAdd and OnBeforeRemove callbacks.",
            Parameters =
            [
                new()
                {
                    Name = "Tag",
                    Type = "string",
                    DefaultValue = "string.Empty",
                    Description = "The tag text being added or removed.",
                },
                new()
                {
                    Name = "Cancel",
                    Type = "bool",
                    DefaultValue = "false",
                    Description = "Set to true to cancel the add or remove operation.",
                },
            ]
        },
        new()
        {
            Id = "invalid-args",
            Title = "BitTagsInputInvalidArgs",
            Description = "Arguments passed to the OnInvalid callback, describing the tag that was rejected along with the rule that rejected it.",
            Parameters =
            [
                new()
                {
                    Name = "Tag",
                    Type = "string",
                    DefaultValue = "string.Empty",
                    Description = "The tag text that was rejected, after the trimming and the transformation were applied to it.",
                },
                new()
                {
                    Name = "Reason",
                    Type = "BitTagsInputInvalidReason",
                    DefaultValue = "BitTagsInputInvalidReason.None",
                    Description = "The rule that rejected the tag.",
                    LinkType = LinkType.Link,
                    Href = "#invalid-reason-enum",
                },
            ]
        },
        new()
        {
            Id = "edit-args",
            Title = "BitTagsInputEditArgs",
            Description = "Arguments passed to the OnEdit callback, describing an inline edit of a tag that is about to be committed.",
            Parameters =
            [
                new()
                {
                    Name = "Tag",
                    Type = "string",
                    DefaultValue = "string.Empty",
                    Description = "The tag as it stands in the list, before the edit.",
                },
                new()
                {
                    Name = "NewTag",
                    Type = "string",
                    DefaultValue = "string.Empty",
                    Description = "The text the tag is about to become, after the trimming and the transformation were applied to it.",
                },
                new()
                {
                    Name = "Cancel",
                    Type = "bool",
                    DefaultValue = "false",
                    Description = "Set to true to cancel the edit, leaving the tag as it was.",
                },
            ]
        },
        new()
        {
            Id = "clear-args",
            Title = "BitTagsInputClearArgs",
            Description = "Arguments passed to the OnBeforeClear callback, describing the whole list that is about to be emptied.",
            Parameters =
            [
                new()
                {
                    Name = "Tags",
                    Type = "IReadOnlyList<string>",
                    DefaultValue = "[]",
                    Description = "The tags that are about to be removed.",
                },
                new()
                {
                    Name = "Cancel",
                    Type = "bool",
                    DefaultValue = "false",
                    Description = "Set to true to cancel the clear, leaving every tag in the list.",
                },
            ]
        },
        new()
        {
            Id = "reorder-args",
            Title = "BitTagsInputReorderArgs",
            Description = "Arguments passed to the OnReorder callback, describing a tag that was moved within the list.",
            Parameters =
            [
                new()
                {
                    Name = "Tag",
                    Type = "string",
                    DefaultValue = "string.Empty",
                    Description = "The tag that was moved.",
                },
                new()
                {
                    Name = "OldIndex",
                    Type = "int",
                    DefaultValue = "0",
                    Description = "The zero based position the tag was moved from.",
                },
                new()
                {
                    Name = "NewIndex",
                    Type = "int",
                    DefaultValue = "0",
                    Description = "The zero based position the tag was moved to.",
                },
            ]
        },
        new()
        {
            Id = "tagsinput-class-styles",
            Title = "BitTagsInputClassStyles",
            Description = "",
            Parameters =
            [
                new()
                {
                    Name = "Root",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the root element of the tags input.",
                },
                new()
                {
                    Name = "Focused",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the focus state of the tags input.",
                },
                new()
                {
                    Name = "Label",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the label of the tags input.",
                },
                new()
                {
                    Name = "InputContainer",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the input container of the tags input.",
                },
                new()
                {
                    Name = "Prefix",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the prefix of the tags input.",
                },
                new()
                {
                    Name = "Suffix",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the suffix of the tags input.",
                },
                new()
                {
                    Name = "TagsContainer",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the element that wraps the rendered tags.",
                },
                new()
                {
                    Name = "Tag",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for each tag element.",
                },
                new()
                {
                    Name = "FocusedTag",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the tag element that currently has the keyboard focus.",
                },
                new()
                {
                    Name = "FixedTag",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for a tag the CanRemoveTag predicate holds in place, which carries no dismiss button of its own.",
                },
                new()
                {
                    Name = "PickedUpTag",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the tag that has been picked up with its reorder handle and is waiting to be put down.",
                },
                new()
                {
                    Name = "DuplicateTag",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the tag a refused duplicate collided with, which is marked until the user types again.",
                },
                new()
                {
                    Name = "TagText",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the tag text.",
                },
                new()
                {
                    Name = "ReorderButton",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the reorder handle AllowReorder draws on each tag.",
                },
                new()
                {
                    Name = "ReorderIcon",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the icon of that reorder handle.",
                },
                new()
                {
                    Name = "DismissButton",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the dismiss button of each tag.",
                },
                new()
                {
                    Name = "DismissIcon",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the dismiss icon of each tag.",
                },
                new()
                {
                    Name = "ToggleButton",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the chip that stands for the tags MaxDisplayedTags folded away, which unfolds the list and folds it back.",
                },
                new()
                {
                    Name = "Input",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the text input element.",
                },
                new()
                {
                    Name = "EditInput",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the little input that replaces a tag while it is being edited in place.",
                },
                new()
                {
                    Name = "Counter",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the counter of the tags input.",
                },
                new()
                {
                    Name = "Spinner",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the spinner IsLoading draws at the end of the field.",
                },
                new()
                {
                    Name = "ClearButton",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the clear button of the tags input.",
                },
                new()
                {
                    Name = "ClearIcon",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the clear icon of the tags input.",
                },
                new()
                {
                    Name = "Description",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the description (helper text) of the tags input.",
                },
            ]
        },
        new()
        {
            Id = "bit-icon-info",
            Title = "BitIconInfo",
            Parameters =
            [
               new()
               {
                   Name = "Name",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Gets or sets the name of the icon."
               },
               new()
               {
                   Name = "BaseClass",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Gets or sets the base CSS class for the icon. For built-in Fluent UI icons, this defaults to \"bit-icon\". For external icon libraries like FontAwesome, you might set this to \"fa\" or leave empty."
               },
               new()
               {
                   Name = "Prefix",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Gets or sets the CSS class prefix used before the icon name. For built-in Fluent UI icons, this defaults to \"bit-icon--\". For external icon libraries, you might set this to \"fa-\" or leave empty."
               },
            ]
        }
    ];

    private readonly List<ComponentSubEnum> componentSubEnums =
    [
        new()
        {
            Id = "invalid-reason-enum",
            Name = "BitTagsInputInvalidReason",
            Description = "The reason a tag was rejected, reported through the OnInvalid callback.",
            Items =
            [
                new()
                {
                    Name = "None",
                    Description = "No reason was given, which is what an uninitialized value stands for rather than an actual rule.",
                    Value = "0",
                },
                new()
                {
                    Name = "Duplicate",
                    Description = "The tag is already in the list and duplicates are not allowed.",
                    Value = "1",
                },
                new()
                {
                    Name = "MaxTags",
                    Description = "The list already holds the maximum number of tags.",
                    Value = "2",
                },
                new()
                {
                    Name = "MinLength",
                    Description = "The tag is shorter than the minimum length.",
                    Value = "3",
                },
                new()
                {
                    Name = "Pattern",
                    Description = "The tag does not match the required pattern.",
                    Value = "4",
                },
                new()
                {
                    Name = "Validator",
                    Description = "The tag was rejected by the custom validator.",
                    Value = "5",
                },
                new()
                {
                    Name = "NotSuggested",
                    Description = "The tag is not one of the suggestions, which RestrictToSuggestions made the only accepted values.",
                    Value = "6",
                },
            ]
        },
        new()
        {
            Id = "input-mode-enum",
            Name = "BitInputMode",
            Description = "Defines the inputmode html attribute, which is what lets a browser display an appropriate virtual keyboard.",
            Items =
            [
                new() { Name = "None", Description = "No virtual keyboard. For when the page implements its own keyboard input control.", Value = "0" },
                new() { Name = "Text", Description = "Standard input keyboard for the user's current locale.", Value = "1" },
                new() { Name = "Decimal", Description = "Fractional numeric input keyboard containing the digits and decimal separator for the user's locale.", Value = "2" },
                new() { Name = "Numeric", Description = "Numeric input keyboard, but only requires the digits 0–9.", Value = "3" },
                new() { Name = "Tel", Description = "A telephone keypad input, including the digits 0–9, the asterisk (*), and the pound (#) key.", Value = "4" },
                new() { Name = "Search", Description = "A virtual keyboard optimized for search input.", Value = "5" },
                new() { Name = "Email", Description = "A virtual keyboard optimized for entering email addresses.", Value = "6" },
                new() { Name = "Url", Description = "A keypad optimized for entering URLs.", Value = "7" },
            ]
        },
        new()
        {
            Id = "enter-key-hint-enum",
            Name = "BitEnterKeyHint",
            Description = "Tells the browser which action label (or icon) to present for the enter key of a virtual keyboard.",
            Items =
            [
                new() { Name = "Enter", Description = "Typically inserting a new line.", Value = "0" },
                new() { Name = "Done", Description = "Typically meaning there is nothing more to input and the input method editor will be closed.", Value = "1" },
                new() { Name = "Go", Description = "Typically meaning to take the user to the target of the text they typed.", Value = "2" },
                new() { Name = "Next", Description = "Typically taking the user to the next field that will accept text.", Value = "3" },
                new() { Name = "Previous", Description = "Typically taking the user to the previous field that will accept text.", Value = "4" },
                new() { Name = "Search", Description = "Typically taking the user to the results of searching for the text they have typed.", Value = "5" },
                new() { Name = "Send", Description = "Typically delivering the text to its target.", Value = "6" },
            ]
        },
        new()
        {
            Id = "variant-enum",
            Name = "BitVariant",
            Description = "",
            Items =
            [
                new()
                {
                    Name = "Fill",
                    Description = "Fill styled variant.",
                    Value = "0",
                },
                new()
                {
                    Name = "Outline",
                    Description = "Outline styled variant.",
                    Value = "1",
                },
                new()
                {
                    Name = "Text",
                    Description = "Text styled variant.",
                    Value = "2",
                },
            ]
        },
        new()
        {
            Id = "size-enum",
            Name = "BitSize",
            Description = "",
            Items =
            [
                new()
                {
                    Name = "Small",
                    Description = "The small size.",
                    Value = "0",
                },
                new()
                {
                    Name = "Medium",
                    Description = "The medium size.",
                    Value = "1",
                },
                new()
                {
                    Name = "Large",
                    Description = "The large size.",
                    Value = "2",
                },
            ]
        },
        new()
        {
            Id = "color-enum",
            Name = "BitColor",
            Description = "Defines the general colors available in the bit BlazorUI.",
            Items =
            [
                new() { Name = "Primary", Description = "Primary general color.", Value = "0" },
                new() { Name = "Secondary", Description = "Secondary general color.", Value = "1" },
                new() { Name = "Tertiary", Description = "Tertiary general color.", Value = "2" },
                new() { Name = "Info", Description = "Info general color.", Value = "3" },
                new() { Name = "Success", Description = "Success general color.", Value = "4" },
                new() { Name = "Warning", Description = "Warning general color.", Value = "5" },
                new() { Name = "SevereWarning", Description = "SevereWarning general color.", Value = "6" },
                new() { Name = "Error", Description = "Error general color.", Value = "7" },
                new() { Name = "PrimaryBackground", Description = "Primary background color.", Value = "8" },
                new() { Name = "SecondaryBackground", Description = "Secondary background color.", Value = "9" },
                new() { Name = "TertiaryBackground", Description = "Tertiary background color.", Value = "10" },
                new() { Name = "PrimaryForeground", Description = "Primary foreground color.", Value = "11" },
                new() { Name = "SecondaryForeground", Description = "Secondary foreground color.", Value = "12" },
                new() { Name = "TertiaryForeground", Description = "Tertiary foreground color.", Value = "13" },
                new() { Name = "PrimaryBorder", Description = "Primary border color.", Value = "14" },
                new() { Name = "SecondaryBorder", Description = "Secondary border color.", Value = "15" },
                new() { Name = "TertiaryBorder", Description = "Tertiary border color.", Value = "16" },
            ]
        },
    ];

    private readonly List<ComponentParameter> componentPublicMembers =
    [
        new()
        {
            Name = "InputElement",
            Type = "ElementReference",
            Description = "The ElementReference to the input element of the BitTagsInput.",
        },
        new()
        {
            Name = "FocusAsync",
            Type = "ValueTask",
            Description = "Gives focus to the input element of the BitTagsInput.",
        },
        new()
        {
            Name = "AddTagAsync",
            Type = "Task",
            Description = "Adds a tag through exactly the same pipeline as typing it does: the trimming, the Transformer, every validation rule and every callback included. It does nothing while the component is disabled or read-only.",
        },
        new()
        {
            Name = "AddTagsAsync",
            Type = "Task",
            Description = "Adds several tags at once, exactly as pasting a separated list of them does. The rejected ones are reported through OnInvalid while the rest are still added.",
        },
        new()
        {
            Name = "RemoveTagAsync",
            Type = "Task",
            Description = "Removes the first tag equal to the given one (per the Comparison), exactly as its dismiss button does.",
        },
        new()
        {
            Name = "RemoveTagAtAsync",
            Type = "Task",
            Description = "Removes the tag sitting at the given index.",
        },
        new()
        {
            Name = "MoveTagAsync",
            Type = "Task",
            Description = "Moves the tag sitting at the given index to another one, exactly as dragging it there or walking it with Alt and the arrow keys does, raising OnReorder along with it. Unlike the gestures, it does not require AllowReorder.",
        },
        new()
        {
            Name = "EditTagAsync",
            Type = "Task",
            Description = "Opens the inline edit of the tag sitting at the given index, exactly as double clicking it does. It requires EditableTags.",
        },
        new()
        {
            Name = "SetInputTextAsync",
            Type = "Task",
            Description = "Sets the text of the input, which is what fills the field from a suggestion list of your own driven by OnInput - and what empties it again once the pick has been turned into a tag. The MaxLength is applied to it and OnInput is raised with the text that was kept.",
        },
        new()
        {
            Name = "Clear",
            Type = "Task",
            Description = "Removes all tags along with the text left in the input, and raises OnClear with the tags that were removed.",
        }
    ];



    private ICollection<string>? maxTagsValue = ["blazor"];
    private string? maxTagsMessage;

    private readonly string[] frameworkSuggestions = ["blazor", "react", "vue", "angular", "svelte"];
    private readonly string[] countrySuggestions = ["Argentina", "Australia", "Austria", "Belgium", "Brazil",
                                                    "Canada", "Chile", "China", "Denmark", "Egypt", "Finland",
                                                    "France", "Germany", "Greece", "India", "Indonesia",
                                                    "Iran", "Ireland", "Italy", "Japan", "Mexico", "Morocco",
                                                    "Netherlands", "New Zealand", "Norway", "Poland", "Portugal",
                                                    "Spain", "Sweden", "Switzerland", "Turkey", "Ukraine"];
    private string? suggestionMessage;

    private bool asyncLoading;
    private int asyncRequestId;
    private string[] asyncSuggestions = [];

    private ICollection<string>? fixedTags = ["ada@example.com", "grace@example.com"];

    private const string emailPattern = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";
    private string? patternMessage;
    private string? validatorMessage;

    private string? duplicateMessage;
    private string? editMessage;
    private string? clearedMessage;
    private string? beforeClearMessage;
    private string? reorderMessage;

    private ICollection<string>? boundTags;
    private ICollection<string>? changedTags;

    private string? typedText;
    private string? eventsLog;

    private BitTagsInput apiTagsInput = default!;

    private bool formSubmitted;
    private readonly ValidationTagsInputModel validationModel = new();

    private readonly List<IBitComponentParams> tagsInputParams =
    [
        new BitTagsInputParams
        {
            Size = BitSize.Small,
            Variant = BitVariant.Fill,
            TagVariant = BitVariant.Outline,
            Color = BitColor.Info,
            Separators = [","],
            ShowClearButton = true,
            Placeholder = "Add tag...",
            Transformer = t => t.ToLowerInvariant()
        }
    ];



    private void HandleMaxTagsInvalid(BitTagsInputInvalidArgs args)
    {
        maxTagsMessage = args.Reason == BitTagsInputInvalidReason.MaxTags
            ? $"'{args.Tag}' was refused: no more than 3 tags."
            : $"'{args.Tag}' was refused ({args.Reason}).";
    }

    private void HandlePatternInvalid(BitTagsInputInvalidArgs args)
    {
        patternMessage = $"'{args.Tag}' is not a valid email address.";
    }

    private void HandleSuggestionInvalid(BitTagsInputInvalidArgs args)
    {
        suggestionMessage = args.Reason == BitTagsInputInvalidReason.NotSuggested
            ? $"'{args.Tag}' is not one of the suggested values."
            : $"'{args.Tag}' was refused ({args.Reason}).";
    }

    private async Task HandleAsyncInput(string text)
    {
        // Only the answer to the last keystroke is kept: an earlier fetch coming back late would
        // otherwise replace a newer list and turn the spinner off over a wait that is still running.
        var id = ++asyncRequestId;

        if (string.IsNullOrEmpty(text))
        {
            asyncLoading = false;
            asyncSuggestions = [];
            return;
        }

        asyncLoading = true;
        StateHasChanged();

        await Task.Delay(500);

        if (id != asyncRequestId) return;

        asyncSuggestions = [.. countrySuggestions.Where(c => c.Contains(text, StringComparison.OrdinalIgnoreCase))];
        asyncLoading = false;
        StateHasChanged();
    }

    private static bool ValidateFramework(string tag)
    {
        return tag is "blazor" or "react" or "vue" or "angular";
    }

    private void HandleValidatorInvalid(BitTagsInputInvalidArgs args)
    {
        validatorMessage = $"'{args.Tag}' is not one of the known frameworks.";
    }

    private static string NormalizeHashtag(string tag)
    {
        return string.Concat(tag.TrimStart('#').Where(c => char.IsWhiteSpace(c) is false)).ToLowerInvariant();
    }

    private void HandleTagExists(string tag)
    {
        duplicateMessage = $"'{tag}' is already in the list.";
    }

    private void HandleEdit(BitTagsInputEditArgs args)
    {
        editMessage = $"'{args.Tag}' became '{args.NewTag}'.";
    }

    private void HandleClear(IReadOnlyList<string> tags)
    {
        clearedMessage = $"Cleared {tags.Count} tag(s).";
    }

    private void HandleBeforeClear(BitTagsInputClearArgs args)
    {
        if (args.Tags.Count > 2)
        {
            args.Cancel = true;
            beforeClearMessage = $"Clearing {args.Tags.Count} tags was refused. Remove a few of them first.";
        }
        else
        {
            beforeClearMessage = $"Cleared {args.Tags.Count} tag(s).";
        }
    }

    private static string? GetRecipientStyle(string tag)
    {
        return Regex.IsMatch(tag, emailPattern) ? null : "background: #fde7e9; color: #a4262c; border-color: #a4262c;";
    }

    private static string? GetPriorityClass(string tag) => tag.ToLowerInvariant() switch
    {
        "high" => "priority-high",
        "medium" => "priority-medium",
        "low" => "priority-low",
        _ => null
    };

    private void HandleReorder(BitTagsInputReorderArgs args)
    {
        reorderMessage = $"'{args.Tag}' moved from position {args.OldIndex + 1} to {args.NewIndex + 1}.";
    }

    private void HandleBeforeAdd(BitTagsInputBeforeArgs args)
    {
        if (args.Tag.Equals("block", StringComparison.OrdinalIgnoreCase))
        {
            args.Cancel = true;
            eventsLog = $"Adding '{args.Tag}' was cancelled by OnBeforeAdd.";
        }
    }

    private void HandleBeforeRemove(BitTagsInputBeforeArgs args)
    {
        eventsLog = $"Removing '{args.Tag}'...";
    }

    private void HandleAdd(IReadOnlyList<string> tags)
    {
        eventsLog = $"Added: {string.Join(", ", tags)}";
    }

    private void HandleRemove(string tag)
    {
        eventsLog = $"Removed: {tag}";
    }

    private void HandleInvalid(BitTagsInputInvalidArgs args)
    {
        eventsLog = $"Rejected '{args.Tag}' ({args.Reason})";
    }

    private void HandleTagClick(string tag)
    {
        eventsLog = $"Clicked: {tag}";
    }

    private Task ApiAddTag() => apiTagsInput.AddTagAsync("dotnet");

    private Task ApiAddTags() => apiTagsInput.AddTagsAsync(["web", "ui"]);

    private Task ApiRemoveTag() => apiTagsInput.RemoveTagAsync("dotnet");

    private Task ApiRemoveFirst() => apiTagsInput.RemoveTagAtAsync(0);

    private Task ApiMoveFirstToEnd() => apiTagsInput.MoveTagAsync(0, (apiTagsInput.Value?.Count ?? 1) - 1);

    private Task ApiEditFirst() => apiTagsInput.EditTagAsync(0);

    private Task ApiSetInputText() => apiTagsInput.SetInputTextAsync("razor");

    private Task ApiClear() => apiTagsInput.Clear();

    private async Task ApiFocus() => await apiTagsInput.FocusAsync();

    private void HandleValidSubmit() => formSubmitted = true;
}
