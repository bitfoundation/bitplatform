namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Notifications.EmptyState;

public partial class BitEmptyStateDemo
{
    private readonly List<ComponentParameter> componentParameters =
    [
        new()
        {
            Name = "ChildContent",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "The actions under the text of the empty state, most of the time the button that adds the first item.",
        },
        new()
        {
            Name = "Classes",
            Type = "BitEmptyStateClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS classes for the different parts of the empty state.",
            LinkType = LinkType.Link,
            Href = "#class-styles",
        },
        new()
        {
            Name = "Description",
            Type = "string?",
            DefaultValue = "null",
            Description = "The line under the title, on why there is nothing to show or what to do about it.",
        },
        new()
        {
            Name = "DescriptionTemplate",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "The custom content of the line under the title, which takes the place of Description.",
        },
        new()
        {
            Name = "HeadingLevel",
            Type = "int?",
            DefaultValue = "null",
            Description = "Makes the title a heading of this level (1 to 6) in the outline of the page, for an empty state that stands for the whole of a page or a section. By default the title is no heading, as it is not in a list.",
        },
        new()
        {
            Name = "Icon",
            Type = "BitIconInfo?",
            DefaultValue = "null",
            Description = "The icon above the title, provided as custom CSS classes of an external icon library. Takes precedence over IconName when both are set.",
            LinkType = LinkType.Link,
            Href = "#bit-icon-info",
        },
        new()
        {
            Name = "IconName",
            Type = "string?",
            DefaultValue = "null",
            Description = "The name of the icon above the title, from the built-in Fluent UI icons. By default there is no icon.",
            LinkType = LinkType.Link,
            Href = "https://blazorui.bitplatform.dev/iconography",
        },
        new()
        {
            Name = "IconTemplate",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "The custom content above the title (an illustration, most of the time), which takes the place of the icon.",
        },
        new()
        {
            Name = "Styles",
            Type = "BitEmptyStateClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS styles for the different parts of the empty state.",
            LinkType = LinkType.Link,
            Href = "#class-styles",
        },
        new()
        {
            Name = "Title",
            Type = "string?",
            DefaultValue = "null",
            Description = "The title of the empty state. Defaults to BitStrings.NoItemsFound (\"No items found\").",
        },
        new()
        {
            Name = "TitleTemplate",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "The custom content of the title, which takes the place of Title.",
        },
    ];

    private readonly List<ComponentSubClass> componentSubClasses =
    [
        new()
        {
            Id = "class-styles",
            Title = "BitEmptyStateClassStyles",
            Parameters =
            [
                new() { Name = "Root", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the root element of the BitEmptyState." },
                new() { Name = "Icon", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the icon of the BitEmptyState, or the element its IconTemplate is rendered in." },
                new() { Name = "Title", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the title of the BitEmptyState." },
                new() { Name = "Description", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the line under the title of the BitEmptyState." },
                new() { Name = "Content", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the element the actions (ChildContent) of the BitEmptyState are rendered in." },
            ]
        },
        new()
        {
            Id = "bit-icon-info",
            Title = "BitIconInfo",
            Parameters =
            [
                new() { Name = "Name", Type = "string?", DefaultValue = "null", Description = "Gets or sets the name of the icon." },
                new() { Name = "BaseClass", Type = "string?", DefaultValue = "null", Description = "Gets or sets the base CSS class for the icon. For built-in Fluent UI icons, this defaults to \"bit-icon\". For external icon libraries like FontAwesome, you might set this to \"fa\" or leave empty." },
                new() { Name = "Prefix", Type = "string?", DefaultValue = "null", Description = "Gets or sets the CSS class prefix used before the icon name. For built-in Fluent UI icons, this defaults to \"bit-icon--\". For external icon libraries, you might set this to \"fa-\" or leave empty." },
            ]
        },
    ];

    private readonly List<ComponentCssVariable> componentCssVariables =
    [
        new() { Name = "--bit-EmptyState-gap", DefaultValue = "spacing(1)", Description = "Room between the icon, the title, the line under it and the actions." },
        new() { Name = "--bit-EmptyState-padding", DefaultValue = "spacing(4) spacing(2)", Description = "Inset of the empty state." },
        new() { Name = "--bit-EmptyState-align", DefaultValue = "center", Description = "Where the parts sit across the empty state: center, start or end. The text is aligned the same way." },
        new() { Name = "--bit-EmptyState-icon-size", DefaultValue = "--bit-tpg-fs-4xl", Description = "Size of the icon." },
        new() { Name = "--bit-EmptyState-icon-color", DefaultValue = "--bit-clr-fg-sec", Description = "Color of the icon." },
        new() { Name = "--bit-EmptyState-title-color", DefaultValue = "--bit-clr-fg-pri", Description = "Color of the title." },
        new() { Name = "--bit-EmptyState-title-font-size", DefaultValue = "--bit-tpg-fs-lg", Description = "Size of the title." },
        new() { Name = "--bit-EmptyState-title-font-weight", DefaultValue = "--bit-tpg-fw-semibold", Description = "Weight of the title." },
        new() { Name = "--bit-EmptyState-description-color", DefaultValue = "--bit-clr-fg-sec", Description = "Color of the line under the title." },
        new() { Name = "--bit-EmptyState-description-font-size", DefaultValue = "--bit-tpg-fs-sm", Description = "Size of the line under the title." },
        new() { Name = "--bit-EmptyState-content-gap", DefaultValue = "spacing(1)", Description = "Room between two actions." },
    ];



    private readonly List<string> tasks = [];

    private void AddTask() => tasks.Add($"Task {tasks.Count + 1}");

    private readonly List<string> people = [];

    private void AddPerson() => people.Add($"Person {people.Count + 1}");
}
