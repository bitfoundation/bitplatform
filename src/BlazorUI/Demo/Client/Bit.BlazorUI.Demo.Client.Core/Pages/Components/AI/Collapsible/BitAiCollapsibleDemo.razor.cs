namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.AI.Collapsible;

public partial class BitAiCollapsibleDemo
{
    private readonly List<ComponentParameter> componentParameters =
    [
        new()
        {
            Name = "ChildContent",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "The body of the panel. It is rendered only while the panel is open: closing the panel unmounts it, so a component inside starts again from its initial state the next time the panel opens.",
        },
        new()
        {
            Name = "Classes",
            Type = "BitAiCollapsibleClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS classes for different parts of the collapsible.",
            LinkType = LinkType.Link,
            Href = "#class-styles",
        },
        new()
        {
            Name = "HeaderTemplate",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "The custom content of the header, which replaces the Title text beside the chevron. It is rendered inside the toggle button, so it is the accessible name of the toggle: keep interactive content out of it (use SuffixTemplate).",
        },
        new()
        {
            Name = "Open",
            Type = "bool",
            DefaultValue = "false",
            Description = "Whether the panel is open. The panel also opens and closes itself when the user toggles it, and reports that through OpenChanged, so it can be bound with @bind-Open.",
        },
        new()
        {
            Name = "OpenChanged",
            Type = "EventCallback<bool>",
            DefaultValue = "",
            Description = "The callback raised with the new state when the user opens or closes the panel.",
        },
        new()
        {
            Name = "Styles",
            Type = "BitAiCollapsibleClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS styles for different parts of the collapsible.",
            LinkType = LinkType.Link,
            Href = "#class-styles",
        },
        new()
        {
            Name = "SuffixTemplate",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "The content shown at the end of the header, beside the toggle rather than inside it, such as a status chip. It is not part of the accessible name of the toggle.",
        },
        new()
        {
            Name = "Title",
            Type = "string?",
            DefaultValue = "null",
            Description = "The text of the header, used when no HeaderTemplate is given.",
        },
    ];

    private readonly List<ComponentSubClass> componentSubClasses =
    [
        new()
        {
            Id = "class-styles",
            Title = "BitAiCollapsibleClassStyles",
            Parameters =
            [
                new()
                {
                    Name = "Root",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the root element of the BitAiCollapsible.",
                },
                new()
                {
                    Name = "Accordion",
                    Type = "BitAccordionClassStyles?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the parts of the BitAccordion that draws the panel of the BitAiCollapsible (its header, title, expander icon, content and the rest).",
                    LinkType = LinkType.Link,
                    Href = "/components/accordion/#accordion-class-styles",
                },
            ]
        },
    ];



    private bool isOpen;
    private int toggleCount;

    private readonly BitAiCollapsibleParams[] collapsibleParams =
    [
        new() { Styles = new() { Accordion = new() { Title = "color: var(--bit-clr-pri); font-weight: 600;" } } }
    ];
}
