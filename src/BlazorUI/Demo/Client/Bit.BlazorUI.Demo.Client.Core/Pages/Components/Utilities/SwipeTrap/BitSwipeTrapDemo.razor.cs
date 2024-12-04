namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Utilities.SwipeTrap;

public partial class BitSwipeTrapDemo
{
    private readonly List<ComponentParameter> componentParameters =
    [
        new()
        {
            Name = "ChildContent",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "The content of the swipe trap."
        },
        new()
        {
            Name = "OnStart",
            Type = "EventCallback<BitSwipeTrapEventArgs>",
            DefaultValue = "",
            Description = "The event callback for when the swipe action starts on the container of the swipe trap.",
            Href = "#swipetrap-event-args",
            LinkType = LinkType.Link,
        },
    ];

    private readonly List<ComponentSubClass> componentSubClasses =
    [
        new()
        {
            Id = "swipetrap-event-args",
            Title = "BitSwipeTrapEventArgs",
            Description = "The event arguments of the SwipeTrap events.",
            Parameters =
            [
                new()
                {
                    Name = "StartX",
                    Type = "decimal",
                    DefaultValue = "0",
                    Description = "The horizontal start point of the swipe action in pixels."
                },
                new()
                {
                    Name = "StartY",
                    Type = "decimal",
                    DefaultValue = "0",
                    Description = "The vertical start point of the swipe action in pixels."
                },
                new()
                {
                    Name = "DiffX",
                    Type = "decimal",
                    DefaultValue = "0",
                    Description = "The horizontal difference of swipe action in pixels."
                },
                new()
                {
                    Name = "DiffX",
                    Type = "decimal",
                    DefaultValue = "0",
                    Description = "The vertical difference of swipe action in pixels."
                },
            ]
        }
    ];



    private bool isTriggered;
    BitSwipeTrapEventArgs? swipeTrapEventArgs;

    private void HandleOnStart(BitSwipeTrapEventArgs args)
    {
        swipeTrapEventArgs = args;
    }
    private void HandleOnMove(BitSwipeTrapEventArgs args)
    {
        swipeTrapEventArgs = args;
    }
    private void HandleOnEnd(BitSwipeTrapEventArgs args)
    {
        swipeTrapEventArgs = args;
    }
    private void HandleOnTrigger()
    {
        isTriggered = true;
        _ = Task.Delay(2000).ContinueWith(async t =>
        {
            await t;
            isTriggered = false;
            await InvokeAsync(StateHasChanged);
        });
    }



    private readonly string example1RazorCode = @"
";
}

