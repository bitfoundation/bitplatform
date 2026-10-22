using Bit.BlazorUI.Demo.Client.Core.Services.AI;

namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.AI.ScrollToLatest;

public partial class BitAiScrollToLatestDemo
{
    private readonly List<ComponentParameter> componentParameters =
    [
        new()
        {
            Name = "Classes",
            Type = "BitAiScrollToLatestClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS classes for different parts of the scroll to latest.",
            LinkType = LinkType.Link,
            Href = "#class-styles",
        },
        new()
        {
            Name = "Label",
            Type = "string?",
            DefaultValue = "null",
            Description = "The text of the button, in place of the ScrollToLatest of the string table (\"Scroll to latest\").",
        },
        new()
        {
            Name = "OnClick",
            Type = "EventCallback",
            DefaultValue = "",
            Description = "The callback for when the button is pressed - typically the ScrollToLatestAsync of the BitAiMessageList it belongs to. Not raised while the button is disabled.",
        },
        new()
        {
            Name = "Styles",
            Type = "BitAiScrollToLatestClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS styles for different parts of the scroll to latest.",
            LinkType = LinkType.Link,
            Href = "#class-styles",
        },
        new()
        {
            Name = "Visible",
            Type = "bool",
            DefaultValue = "false",
            Description = "Shows the button - while the reader is away from the newest message, so it never sits over the transcript doing nothing. Left unset, nothing is rendered.",
        },
    ];

    private readonly List<ComponentCssVariable> componentCssVariables =
    [
        new() { Name = "--bit-AiScrollToLatest-offset", DefaultValue = "spacing(1)", Description = "Room between the button and the top edge of the positioned box it floats above." },
        new() { Name = "--bit-AiScrollToLatest-shadow", DefaultValue = "var(--bit-shd-popup)", Description = "Elevation of the button over the transcript." },
        new() { Name = "--bit-AiScrollToLatest-z-index", DefaultValue = "3", Description = "Stacking of the button over the transcript." },
    ];

    private readonly List<ComponentSubClass> componentSubClasses =
    [
        new()
        {
            Id = "class-styles",
            Title = "BitAiScrollToLatestClassStyles",
            Parameters =
            [
                new()
                {
                    Name = "Root",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the root element of the BitAiScrollToLatest."
                },
                new()
                {
                    Name = "Button",
                    Type = "BitButtonClassStyles?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the button of the BitAiScrollToLatest.",
                    LinkType = LinkType.Link,
                    Href = "/components/button#button-class-styles"
                }
            ]
        },
    ];



    private bool isVisible = true;
    private int clicks;

    private void HandleClick()
    {
        clicks++;
    }


    private BitAiMessageList? list;
    private bool isPinned = true;
    private readonly BitAiConversation conversation = AiDemoConversations.Long(20);

    private void HandlePinnedChange(bool pinned)
    {
        isPinned = pinned;
    }

    private async Task ScrollToLatest()
    {
        if (list is not null) await list.ScrollToLatestAsync();
    }


    private readonly BitAiScrollToLatestParams[] scrollParams = [new() { Label = "Back to the newest message" }];
}
