namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Inputs.ListBox;

public partial class BitListBoxDemo
{
    private readonly List<ComponentParameter> componentParameters =
    [
        new()
        {
            Name = "Classes",
            Type = "BitListBoxClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS classes for the different parts of the list box.",
            LinkType = LinkType.Link,
            Href = "#class-styles",
        },
        new()
        {
            Name = "DefaultValues",
            Type = "IEnumerable<TValue?>?",
            DefaultValue = "null",
            Description = "The values selected at first in multi select mode, when Values is not bound.",
        },
        new()
        {
            Name = "ErrorMessage",
            Type = "string?",
            DefaultValue = "null",
            Description = "A message saying what is wrong with the selection, shown under the list. Setting it marks the list box invalid.",
        },
        new()
        {
            Name = "Invalid",
            Type = "bool",
            DefaultValue = "false",
            Description = "Marks the list box invalid, with the same look and the same aria-invalid an ErrorMessage gives it.",
        },
        new()
        {
            Name = "Items",
            Type = "IEnumerable<TItem>",
            DefaultValue = "[]",
            Description = "The items of the list box.",
            LinkType = LinkType.Link,
            Href = "#listbox-item",
        },
        new()
        {
            Name = "Label",
            Type = "string?",
            DefaultValue = "null",
            Description = "The visible label of the list box, which is also what names it to a screen reader.",
        },
        new()
        {
            Name = "MaxHeight",
            Type = "string?",
            DefaultValue = "null",
            Description = "The height (any CSS length) past which the list scrolls.",
        },
        new()
        {
            Name = "MultiSelect",
            Type = "bool",
            DefaultValue = "false",
            Description = "Lets the user select several items, which are Values instead of Value.",
        },
        new()
        {
            Name = "OnDeselectItem",
            Type = "EventCallback<TItem>",
            DefaultValue = "",
            Description = "The callback for when an item is taken out of the selection in multi select mode.",
        },
        new()
        {
            Name = "OnSelectItem",
            Type = "EventCallback<TItem>",
            DefaultValue = "",
            Description = "The callback for when an item is selected (in multi select mode, added to the selection).",
        },
        new()
        {
            Name = "ShowValidationMessage",
            Type = "bool",
            DefaultValue = "false",
            Description = "Shows the validation messages of the cascading EditContext for the list box under it, where ErrorMessage goes, while there is no ErrorMessage of the app's own.",
        },
        new()
        {
            Name = "Styles",
            Type = "BitListBoxClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS styles for the different parts of the list box.",
            LinkType = LinkType.Link,
            Href = "#class-styles",
        },
        new()
        {
            Name = "Values",
            Type = "IEnumerable<TValue?>?",
            DefaultValue = "null",
            Description = "The values of the selected items in multi select mode. Two-way bindable.",
        },
        new()
        {
            Name = "ValuesExpression",
            Type = "Expression<Func<IEnumerable<TValue?>?>>?",
            DefaultValue = "null",
            Description = "The expression that identifies the bound Values, which ties the multi select mode to an EditContext. It is supplied by the compiler for a @bind-Values binding.",
        },
    ];

    private readonly List<ComponentParameter> componentPublicMembers =
    [
        new()
        {
            Name = "FocusAsync",
            Type = "() => ValueTask",
            DefaultValue = "",
            Description = "Moves the focus to the list box: to the item the keyboard is on, which is the selected one when it comes in.",
        },
    ];

    private readonly List<ComponentSubClass> componentSubClasses =
    [
        new()
        {
            Id = "listbox-item",
            Title = "BitListBoxItem<TValue>",
            Parameters =
            [
                new() { Name = "AriaLabel", Type = "string?", DefaultValue = "null", Description = "The aria-label of the item, which replaces its text and description as what a screen reader names it by." },
                new() { Name = "Class", Type = "string?", DefaultValue = "null", Description = "The custom CSS classes of the item." },
                new() { Name = "Description", Type = "string?", DefaultValue = "null", Description = "A second line of text under the text of the item." },
                new() { Name = "Icon", Type = "BitIconInfo?", DefaultValue = "null", Description = "The icon shown before the text of the item, using custom CSS classes for external icon libraries. Takes precedence over IconName." },
                new() { Name = "IconName", Type = "string?", DefaultValue = "null", Description = "The name of the icon shown before the text of the item, from the built-in Fluent UI icons." },
                new() { Name = "IsDisabled", Type = "bool", DefaultValue = "false", Description = "Whether the item is disabled: it cannot be selected, and the keyboard passes it by." },
                new() { Name = "Key", Type = "string?", DefaultValue = "null", Description = "A unique value to use as the key of the item." },
                new() { Name = "Style", Type = "string?", DefaultValue = "null", Description = "The custom CSS styles of the item." },
                new() { Name = "Text", Type = "string?", DefaultValue = "null", Description = "The text of the item, which typeahead matches too." },
                new() { Name = "Title", Type = "string?", DefaultValue = "null", Description = "The title (tooltip) of the item." },
                new() { Name = "Value", Type = "TValue?", DefaultValue = "null", Description = "The value of the item: what selecting it writes to the Value (or adds to the Values) of the list box." },
            ]
        },
        new()
        {
            Id = "class-styles",
            Title = "BitListBoxClassStyles",
            Parameters =
            [
                new() { Name = "Root", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the root element of the BitListBox." },
                new() { Name = "Label", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the label of the BitListBox." },
                new() { Name = "List", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the list (the listbox element) of the BitListBox." },
                new() { Name = "Item", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for each item of the BitListBox." },
                new() { Name = "SelectedItem", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for each selected item of the BitListBox." },
                new() { Name = "Check", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the check box of each item of a multi select BitListBox." },
                new() { Name = "Icon", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the icon of each item of the BitListBox." },
                new() { Name = "Text", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the text of each item of the BitListBox." },
                new() { Name = "Description", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the description of each item of the BitListBox." },
                new() { Name = "ErrorMessage", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the error message of the BitListBox." },
            ]
        },
    ];

    private readonly List<ComponentCssVariable> componentCssVariables =
    [
        new() { Name = "--bit-ListBox-selected-background", DefaultValue = "var(--bit-clr-bg-pri-active)", Description = "Background of a selected item." },
        new() { Name = "--bit-ListBox-selected-color", DefaultValue = "var(--bit-clr-pri)", Description = "Color of the mark of a selected item: the bar of a single select one, the filled box of a multi select one." },
    ];
}
