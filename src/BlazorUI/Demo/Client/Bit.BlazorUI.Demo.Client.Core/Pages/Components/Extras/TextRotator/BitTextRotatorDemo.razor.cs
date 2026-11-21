namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Extras.TextRotator;

public partial class BitTextRotatorDemo
{
    private readonly List<ComponentParameter> componentParameters =
    [
        new()
        {
            Name = "Classes",
            Type = "BitTextRotatorClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS classes for the different parts of the text rotator.",
            LinkType = LinkType.Link,
            Href = "#class-styles",
        },
        new()
        {
            Name = "Element",
            Type = "string?",
            DefaultValue = "null",
            Description = "The custom html element used for the root node. The default is \"span\", which fits inside a sentence, a heading or a button; a value that is not a name a tag can have falls back to it. A void element (such as \"br\" or \"img\") holds no content, so no item is rendered into one.",
        },
        new()
        {
            Name = "Interval",
            Type = "double",
            DefaultValue = "3000",
            Description = "The time each item is shown for before the next one comes in, in ms, counted from the moment it starts to come in. A value that is not a positive number falls back to the default.",
        },
        new()
        {
            Name = "Items",
            Type = "IEnumerable<string>?",
            DefaultValue = "null",
            Description = "The words or phrases to rotate through, in order. The first one is shown before the rotation starts, while prerendering and under reduced motion; the rotation starts over from it whenever the items change.",
        },
        new()
        {
            Name = "Paused",
            Type = "bool",
            DefaultValue = "false",
            Description = "Holds the rotation on the item that is shown. It carries on from that item, after a whole Interval, once this is turned off again. A disabled rotator does not rotate either.",
        },
        new()
        {
            Name = "Styles",
            Type = "BitTextRotatorClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS styles for the different parts of the text rotator.",
            LinkType = LinkType.Link,
            Href = "#class-styles",
        },
    ];

    private readonly List<ComponentSubClass> componentSubClasses =
    [
        new()
        {
            Id = "class-styles",
            Title = "BitTextRotatorClassStyles",
            Parameters =
            [
                new() { Name = "Root", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the root element of the BitTextRotator." },
                new() { Name = "Item", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for each item of the BitTextRotator." },
                new() { Name = "CurrentItem", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the item of the BitTextRotator that is shown." },
            ]
        },
    ];

    private readonly List<ComponentCssVariable> componentCssVariables =
    [
        new() { Name = "--bit-TextRotator-duration", DefaultValue = "var(--bit-mot-duration-long)", Description = "Length of the entrance of an item, and of the exit played beside it. It collapses under reduced motion along with the motion tokens." },
        new() { Name = "--bit-TextRotator-easing", DefaultValue = "var(--bit-mot-easing)", Description = "Curve of the entrance and the exit." },
        new() { Name = "--bit-TextRotator-distance", DefaultValue = "0.5em", Description = "How far an item travels: it comes in from below and goes out above, the other way round with a negative distance, and cross-fades in place with 0." },
        new() { Name = "--bit-TextRotator-disabled-opacity", DefaultValue = "var(--bit-opa-dis)", Description = "Opacity of a rotator whose Disabled is true." },
    ];



    private bool isPaused;

    private bool pauseAll;
    private BitTextRotatorParams[] rotatorParams => [new() { Interval = 1500, Paused = pauseAll }];
}
