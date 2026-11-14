namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Extras.PopConfirm;

public partial class BitPopConfirmDemo
{
    private readonly List<ComponentParameter> componentParameters =
    [
        new()
        {
            Name = "CancelText",
            Type = "string?",
            DefaultValue = "null",
            Description = "The text of the Cancel button. Default: Cancel.",
        },
        new()
        {
            Name = "ChildContent",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "The trigger the popup is anchored to and opened from, a button most of the time.",
        },
        new()
        {
            Name = "Classes",
            Type = "BitPopConfirmClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS classes for the different parts of the pop confirm.",
            LinkType = LinkType.Link,
            Href = "#class-styles",
        },
        new()
        {
            Name = "ConfirmColor",
            Type = "BitColor?",
            DefaultValue = "null",
            Description = "The color of the Confirm button, Error for an action that destroys something. Default: the color of a button left without one (Primary).",
            LinkType = LinkType.Link,
            Href = "#color-enum",
        },
        new()
        {
            Name = "ConfirmText",
            Type = "string?",
            DefaultValue = "null",
            Description = "The text of the Confirm button. Default: Ok.",
        },
        new()
        {
            Name = "IsOpen",
            Type = "bool",
            DefaultValue = "false",
            Description = "Whether the popup is open. Two-way bindable; opening or closing it through this parameter raises neither OnConfirm nor OnCancel.",
        },
        new()
        {
            Name = "Message",
            Type = "string?",
            DefaultValue = "null",
            Description = "The question the popup asks, or what it explains under the title. The popup is described by it, or named by it where there is no Title.",
        },
        new()
        {
            Name = "OnCancel",
            Type = "EventCallback",
            DefaultValue = "",
            Description = "The callback for when the question is answered with no: the Cancel button, Escape, or a click outside of the popup.",
        },
        new()
        {
            Name = "OnConfirm",
            Type = "EventCallback",
            DefaultValue = "",
            Description = "The callback for when the question is answered with yes, through the Confirm button. It is raised once the popup has closed.",
        },
        new()
        {
            Name = "Placement",
            Type = "BitPlacement?",
            DefaultValue = "null",
            Description = "The side of the trigger the popup is placed on: Top, Bottom, Start or End, or one of their corner variants (TopStart, BottomEnd, ...). By default it takes the side with the most room.",
            LinkType = LinkType.Link,
            Href = "#placement-enum",
        },
        new()
        {
            Name = "Styles",
            Type = "BitPopConfirmClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS styles for the different parts of the pop confirm.",
            LinkType = LinkType.Link,
            Href = "#class-styles",
        },
        new()
        {
            Name = "Title",
            Type = "string?",
            DefaultValue = "null",
            Description = "The title of the popup, which names it.",
        },
    ];

    private readonly List<ComponentSubClass> componentSubClasses =
    [
        new()
        {
            Id = "class-styles",
            Title = "BitPopConfirmClassStyles",
            Parameters =
            [
                new() { Name = "Root", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the root element of the BitPopConfirm, which holds the trigger." },
                new() { Name = "Callout", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the popup of the BitPopConfirm." },
                new() { Name = "Container", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the container of the content of the popup of the BitPopConfirm." },
                new() { Name = "Title", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the title of the BitPopConfirm." },
                new() { Name = "Message", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the message of the BitPopConfirm." },
                new() { Name = "Actions", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the container of the buttons of the BitPopConfirm." },
                new() { Name = "CancelButton", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the Cancel button of the BitPopConfirm." },
                new() { Name = "ConfirmButton", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the Confirm button of the BitPopConfirm." },
            ]
        },
    ];

    private readonly List<ComponentSubEnum> componentSubEnums =
    [
        DemoSharedEnums.BitColor(),
        DemoSharedEnums.BitPlacement(),
    ];

    private readonly List<ComponentCssVariable> componentCssVariables =
    [
        new() { Name = "--bit-PopConfirm-max-width", DefaultValue = "spacing(40)", Description = "Width the popup stops growing at, the message wrapping inside." },
        new() { Name = "--bit-PopConfirm-padding", DefaultValue = "spacing(1.5)", Description = "Inset of the content of the popup." },
        new() { Name = "--bit-PopConfirm-gap", DefaultValue = "spacing(1)", Description = "Room between the title, the message and the buttons." },
        new() { Name = "--bit-PopConfirm-title-color", DefaultValue = "var(--bit-clr-fg-pri)", Description = "Color of the title." },
        new() { Name = "--bit-PopConfirm-message-color", DefaultValue = "var(--bit-clr-fg-sec)", Description = "Color of the message." },
        new() { Name = "--bit-PopConfirm-actions-gap", DefaultValue = "spacing(1)", Description = "Room between the two buttons." },
    ];



    private string? basicAnswer;
    private bool isBoundOpen;
    private bool accountDeleted;
}
