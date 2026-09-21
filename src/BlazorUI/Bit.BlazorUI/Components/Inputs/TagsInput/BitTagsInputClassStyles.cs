namespace Bit.BlazorUI;

public class BitTagsInputClassStyles
{
    /// <summary>
    /// Custom CSS classes/styles for the root element of the BitTagsInput.
    /// </summary>
    public string? Root { get; set; }

    /// <summary>
    /// Custom CSS classes/styles carried by the root element while the input holds the focus. A tag reached
    /// with the arrow keys is the field's focus rather than the input's, so it lights the field's own ring
    /// (through :focus-within) without adding this one.
    /// </summary>
    public string? Focused { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the label of the BitTagsInput.
    /// </summary>
    public string? Label { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the input container of the BitTagsInput.
    /// </summary>
    public string? InputContainer { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the prefix of the BitTagsInput.
    /// </summary>
    public string? Prefix { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the suffix of the BitTagsInput.
    /// </summary>
    public string? Suffix { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the element that wraps the rendered tags.
    /// </summary>
    public string? TagsContainer { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for each tag element.
    /// </summary>
    public string? Tag { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the tag element that currently has the keyboard focus.
    /// </summary>
    public string? FocusedTag { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for a tag the CanRemoveTag predicate holds in place, which carries no
    /// dismiss button of its own.
    /// </summary>
    public string? FixedTag { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the tag text.
    /// </summary>
    public string? TagText { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the dismiss button of each tag.
    /// </summary>
    public string? DismissButton { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the dismiss icon of each tag.
    /// </summary>
    public string? DismissIcon { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the chip that stands for the tags MaxDisplayedTags folded away,
    /// which unfolds the list and folds it back.
    /// </summary>
    public string? ToggleButton { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the text input element.
    /// </summary>
    public string? Input { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the little input that replaces a tag while it is being edited in place.
    /// </summary>
    public string? EditInput { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the counter of the BitTagsInput.
    /// </summary>
    public string? Counter { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the spinner IsLoading draws at the end of the field.
    /// </summary>
    public string? Spinner { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the clear button of the BitTagsInput.
    /// </summary>
    public string? ClearButton { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the clear icon of the BitTagsInput.
    /// </summary>
    public string? ClearIcon { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the description (helper text) of the BitTagsInput.
    /// </summary>
    public string? Description { get; set; }
}
