namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Navs.Stepper;

public partial class BitStepperDemo
{
    [CascadingParameter(Name = nameof(RenderForMcpClient))] public bool RenderForMcpClient { get; set; }

    private readonly List<ComponentParameter> componentParameters =
    [
        new()
        {
            Name = "ChildContent",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "The content of the BitStepper, that are BitStepperOption components.",
            LinkType = LinkType.Link,
            Href = "#stepper-option",
        },
        new()
        {
            Name = "Classes",
            Type = "BitStepperClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS classes for the different parts of the stepper.",
            LinkType = LinkType.Link,
            Href = "#class-styles",
        },
        new()
        {
            Name = "CompletedIcon",
            Type = "BitIconInfo?",
            DefaultValue = "null",
            Description = "The icon shown in the indicator of a completed step, using custom CSS classes for external icon libraries. Takes precedence over CompletedIconName.",
        },
        new()
        {
            Name = "CompletedIconName",
            Type = "string?",
            DefaultValue = "null",
            Description = "The name of the icon shown in the indicator of a completed step, from the built-in Fluent UI icons. Defaults to \"CheckMark\".",
        },
        new()
        {
            Name = "CurrentIndex",
            Type = "int",
            DefaultValue = "0",
            Description = "The index of the current step. The steps before it are completed and the ones after it are upcoming; an index past the last step shows every step completed, and a negative one shows none started. Two-way bindable.",
        },
        new()
        {
            Name = "ErrorIcon",
            Type = "BitIconInfo?",
            DefaultValue = "null",
            Description = "The icon shown in the indicator of a step in error, using custom CSS classes for external icon libraries. Takes precedence over ErrorIconName.",
        },
        new()
        {
            Name = "ErrorIconName",
            Type = "string?",
            DefaultValue = "null",
            Description = "The name of the icon shown in the indicator of a step in error, from the built-in Fluent UI icons. Defaults to \"ErrorBadge\".",
        },
        new()
        {
            Name = "Items",
            Type = "IEnumerable<TItem>",
            DefaultValue = "[]",
            Description = "The steps of the stepper.",
            LinkType = LinkType.Link,
            Href = "#stepper-item",
        },
        new()
        {
            Name = "Linear",
            Type = "bool",
            DefaultValue = "false",
            Description = "Keeps the user from skipping ahead: only the steps before the current one (and the current one) can be clicked, so a step is reached only by the page moving CurrentIndex forward.",
        },
        new()
        {
            Name = "NameSelectors",
            Type = "BitStepperNameSelectors<TItem>?",
            DefaultValue = "null",
            Description = "The names (or selectors) of the members the steps are read from, for items of a custom class.",
            LinkType = LinkType.Link,
            Href = "#name-selectors",
        },
        new()
        {
            Name = "OnChange",
            Type = "EventCallback<int>",
            DefaultValue = "",
            Description = "The callback for when the user goes to another step, with its index. It runs even when CurrentIndex is bound one way and could not be written back.",
        },
        new()
        {
            Name = "OnItemClick",
            Type = "EventCallback<TItem>",
            DefaultValue = "",
            Description = "The callback for when a step is clicked, the current one included, as long as it can be gone to.",
        },
        new()
        {
            Name = "Options",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "Alias of ChildContent: the steps declared as BitStepperOption components.",
            LinkType = LinkType.Link,
            Href = "#stepper-option",
        },
        new()
        {
            Name = "ReadOnly",
            Type = "bool",
            DefaultValue = "false",
            Description = "Only shows where the process is: no step can be clicked.",
        },
        new()
        {
            Name = "Styles",
            Type = "BitStepperClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS styles for the different parts of the stepper.",
            LinkType = LinkType.Link,
            Href = "#class-styles",
        },
        new()
        {
            Name = "Vertical",
            Type = "bool",
            DefaultValue = "false",
            Description = "Lays the steps out from top to bottom instead of from start to end.",
        },
    ];

    private readonly List<ComponentSubClass> componentSubClasses =
    [
        new()
        {
            Id = "stepper-item",
            Title = "BitStepperItem",
            Parameters =
            [
                new() { Name = "AriaLabel", Type = "string?", DefaultValue = "null", Description = "The aria-label of a step that can be clicked, which replaces its text, description and state as what a screen reader names it by." },
                new() { Name = "Class", Type = "string?", DefaultValue = "null", Description = "The custom CSS classes of the step." },
                new() { Name = "Description", Type = "string?", DefaultValue = "null", Description = "A second line of text under the text of the step." },
                new() { Name = "Icon", Type = "BitIconInfo?", DefaultValue = "null", Description = "The icon shown in the indicator of the step in place of its number, using custom CSS classes for external icon libraries. Takes precedence over IconName. A step in error shows the error icon instead, and a completed step the completed icon." },
                new() { Name = "IconName", Type = "string?", DefaultValue = "null", Description = "The name of the icon shown in the indicator of the step in place of its number, from the built-in Fluent UI icons." },
                new() { Name = "IsDisabled", Type = "bool", DefaultValue = "false", Description = "Whether the step is disabled: it cannot be gone to, and is drawn dimmed." },
                new() { Name = "IsError", Type = "bool", DefaultValue = "false", Description = "Whether the step is in error, shown with the error icon and color whatever the position of the step." },
                new() { Name = "Key", Type = "string?", DefaultValue = "null", Description = "A unique value to use as the key of the step." },
                new() { Name = "OnClick", Type = "Action<BitStepperItem>?", DefaultValue = "null", Description = "The click handler of the step, run when it is clicked and it can be gone to." },
                new() { Name = "Style", Type = "string?", DefaultValue = "null", Description = "The custom CSS styles of the step." },
                new() { Name = "Text", Type = "string?", DefaultValue = "null", Description = "The text of the step." },
                new() { Name = "Title", Type = "string?", DefaultValue = "null", Description = "The title (tooltip) of the step." },
            ]
        },
        new()
        {
            Id = "stepper-option",
            Title = "BitStepperOption",
            Parameters =
            [
                new() { Name = "AriaLabel", Type = "string?", DefaultValue = "null", Description = "The aria-label of a step that can be clicked, which replaces its text, description and state as what a screen reader names it by." },
                new() { Name = "Class", Type = "string?", DefaultValue = "null", Description = "The custom CSS classes of the step." },
                new() { Name = "Description", Type = "string?", DefaultValue = "null", Description = "A second line of text under the text of the step." },
                new() { Name = "Icon", Type = "BitIconInfo?", DefaultValue = "null", Description = "The icon shown in the indicator of the step in place of its number, using custom CSS classes for external icon libraries. Takes precedence over IconName." },
                new() { Name = "IconName", Type = "string?", DefaultValue = "null", Description = "The name of the icon shown in the indicator of the step in place of its number, from the built-in Fluent UI icons." },
                new() { Name = "IsDisabled", Type = "bool", DefaultValue = "false", Description = "Whether the step is disabled: it cannot be gone to, and is drawn dimmed." },
                new() { Name = "IsError", Type = "bool", DefaultValue = "false", Description = "Whether the step is in error, shown with the error icon and color whatever the position of the step." },
                new() { Name = "Key", Type = "string?", DefaultValue = "null", Description = "A unique value to use as the key of the step." },
                new() { Name = "OnClick", Type = "EventCallback<BitStepperOption>", DefaultValue = "", Description = "The click handler of the step, run when it is clicked and it can be gone to." },
                new() { Name = "Style", Type = "string?", DefaultValue = "null", Description = "The custom CSS styles of the step." },
                new() { Name = "Text", Type = "string?", DefaultValue = "null", Description = "The text of the step." },
                new() { Name = "Title", Type = "string?", DefaultValue = "null", Description = "The title (tooltip) of the step." },
            ]
        },
        new()
        {
            Id = "name-selectors",
            Title = "BitStepperNameSelectors",
            Parameters =
            [
                new() { Name = "AriaLabel", Type = "BitNameSelectorPair<TItem, string?>", DefaultValue = "new(nameof(BitStepperItem.AriaLabel))", Description = "The AriaLabel field name and selector of the custom input class.", LinkType = LinkType.Link, Href = "#name-selector-pair" },
                new() { Name = "Class", Type = "BitNameSelectorPair<TItem, string?>", DefaultValue = "new(nameof(BitStepperItem.Class))", Description = "The CSS Class field name and selector of the custom input class.", LinkType = LinkType.Link, Href = "#name-selector-pair" },
                new() { Name = "Description", Type = "BitNameSelectorPair<TItem, string?>", DefaultValue = "new(nameof(BitStepperItem.Description))", Description = "The Description field name and selector of the custom input class.", LinkType = LinkType.Link, Href = "#name-selector-pair" },
                new() { Name = "Icon", Type = "BitNameSelectorPair<TItem, BitIconInfo?>", DefaultValue = "new(nameof(BitStepperItem.Icon))", Description = "The Icon field name and selector of the custom input class.", LinkType = LinkType.Link, Href = "#name-selector-pair" },
                new() { Name = "IconName", Type = "BitNameSelectorPair<TItem, string?>", DefaultValue = "new(nameof(BitStepperItem.IconName))", Description = "The IconName field name and selector of the custom input class.", LinkType = LinkType.Link, Href = "#name-selector-pair" },
                new() { Name = "IsDisabled", Type = "BitNameSelectorPair<TItem, bool>", DefaultValue = "new(nameof(BitStepperItem.IsDisabled))", Description = "The IsDisabled field name and selector of the custom input class.", LinkType = LinkType.Link, Href = "#name-selector-pair" },
                new() { Name = "IsError", Type = "BitNameSelectorPair<TItem, bool>", DefaultValue = "new(nameof(BitStepperItem.IsError))", Description = "The IsError field name and selector of the custom input class.", LinkType = LinkType.Link, Href = "#name-selector-pair" },
                new() { Name = "Key", Type = "BitNameSelectorPair<TItem, string?>", DefaultValue = "new(nameof(BitStepperItem.Key))", Description = "The Key field name and selector of the custom input class.", LinkType = LinkType.Link, Href = "#name-selector-pair" },
                new() { Name = "OnClick", Type = "BitNameSelectorPair<TItem, Action<TItem>?>", DefaultValue = "new(nameof(BitStepperItem.OnClick))", Description = "The OnClick field name and selector of the custom input class.", LinkType = LinkType.Link, Href = "#name-selector-pair" },
                new() { Name = "Style", Type = "BitNameSelectorPair<TItem, string?>", DefaultValue = "new(nameof(BitStepperItem.Style))", Description = "The CSS Style field name and selector of the custom input class.", LinkType = LinkType.Link, Href = "#name-selector-pair" },
                new() { Name = "Text", Type = "BitNameSelectorPair<TItem, string?>", DefaultValue = "new(nameof(BitStepperItem.Text))", Description = "The Text field name and selector of the custom input class.", LinkType = LinkType.Link, Href = "#name-selector-pair" },
                new() { Name = "Title", Type = "BitNameSelectorPair<TItem, string?>", DefaultValue = "new(nameof(BitStepperItem.Title))", Description = "The Title field name and selector of the custom input class.", LinkType = LinkType.Link, Href = "#name-selector-pair" },
            ]
        },
        new()
        {
            Id = "name-selector-pair",
            Title = "BitNameSelectorPair",
            Parameters =
            [
                new() { Name = "Name", Type = "string", Description = "Custom class property name." },
                new() { Name = "Selector", Type = "Func<TItem, TProp?>?", Description = "Custom class property selector." },
            ]
        },
        new()
        {
            Id = "class-styles",
            Title = "BitStepperClassStyles",
            Parameters =
            [
                new() { Name = "Root", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the root element (the list of steps) of the BitStepper." },
                new() { Name = "Step", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for each step of the BitStepper." },
                new() { Name = "CompletedStep", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for each completed step of the BitStepper." },
                new() { Name = "CurrentStep", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the current step of the BitStepper." },
                new() { Name = "UpcomingStep", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for each upcoming step of the BitStepper." },
                new() { Name = "ErrorStep", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for each step of the BitStepper that is in error." },
                new() { Name = "Button", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the part of each step of the BitStepper that is clicked." },
                new() { Name = "Indicator", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the indicator (the circle with the number or icon) of each step of the BitStepper." },
                new() { Name = "Icon", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the icon in the indicator of each step of the BitStepper." },
                new() { Name = "Number", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the number in the indicator of each step of the BitStepper." },
                new() { Name = "Label", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the container of the text and description of each step of the BitStepper." },
                new() { Name = "Text", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the text of each step of the BitStepper." },
                new() { Name = "Description", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the description of each step of the BitStepper." },
                new() { Name = "Connector", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the line connecting each step of the BitStepper to the next one." },
            ]
        },
    ];

    private readonly List<ComponentCssVariable> componentCssVariables =
    [
        new() { Name = "--bit-Stepper-indicator-size", DefaultValue = "2rem", Description = "Diameter of the indicator of a step." },
        new() { Name = "--bit-Stepper-active-color", DefaultValue = "var(--bit-clr-pri)", Description = "Fill of the indicator of the completed and current steps, and of the line after a completed step." },
        new() { Name = "--bit-Stepper-active-text-color", DefaultValue = "var(--bit-clr-pri-text)", Description = "Number and icon color on the active fill." },
        new() { Name = "--bit-Stepper-error-color", DefaultValue = "var(--bit-clr-err)", Description = "Fill of the indicator, and color of the text, of a step in error." },
        new() { Name = "--bit-Stepper-connector-color", DefaultValue = "var(--bit-clr-brd-sec)", Description = "Color of the line between two steps." },
        new() { Name = "--bit-Stepper-connector-width", DefaultValue = "var(--bit-shp-brd-width-thick)", Description = "Thickness of the line between two steps." },
    ];
}
