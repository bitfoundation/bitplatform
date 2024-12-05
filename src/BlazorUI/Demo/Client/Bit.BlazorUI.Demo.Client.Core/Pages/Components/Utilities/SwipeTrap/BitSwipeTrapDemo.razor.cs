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
            LinkType = LinkType.Link,
            Href = "#swipetrap-event-args",
        },
        new()
        {
            Name = "OnMove",
            Type = "EventCallback<BitSwipeTrapEventArgs>",
            DefaultValue = "",
            Description = "The event callback for when the swipe action moves on the container of the swipe trap.",
            LinkType = LinkType.Link,
            Href = "#swipetrap-event-args",
        },
        new()
        {
            Name = "OnEnd",
            Type = "EventCallback<BitSwipeTrapEventArgs>",
            DefaultValue = "",
            Description = "The event callback for when the swipe action ends on the container of the swipe trap.",
            LinkType = LinkType.Link,
            Href = "#swipetrap-event-args",
        },
        new()
        {
            Name = "OnTrigger",
            Type = "EventCallback<BitSwipeTrapTriggerArgs>",
            DefaultValue = "",
            Description = "The event callback for when the swipe action triggers based on the Trigger constraint.",
            LinkType = LinkType.Link,
            Href = "#swipetrap-trigger-args",
        },
        new()
        {
            Name = "Threshold",
            Type = "decimal?",
            DefaultValue = "null",
            Description = "The threshold in pixel for swiping distance that starts the swipe process process which stops the default behavior."
        },
        new()
        {
            Name = "Throttle",
            Type = "int?",
            DefaultValue = "null",
            Description = "The throttle time in milliseconds to apply a delay between periodic calls to raise the events (default is 0)."
        },
        new()
        {
            Name = "Trigger",
            Type = "decimal?",
            DefaultValue = "null",
            Description = "The swiping point based on the width of the element (difference fraction) to trigger and call the OnTrigger event (default is 0.25m)."
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
                    Name = "DiffY",
                    Type = "decimal",
                    DefaultValue = "0",
                    Description = "The vertical difference of swipe action in pixels."
                },
            ]
        },
        new()
        {
            Id = "swipetrap-trigger-args",
            Title = "BitSwipeTrapTriggerArgs",
            Description = "The event arguments of the SwipeTrap trigger event.",
            Parameters =
            [
                new()
                {
                    Name = "Direction",
                    Type = "BitSwipeDirection",
                    DefaultValue = "",
                    Description = "The swipe direction in which the action triggered.",
                    LinkType = LinkType.Link,
                    Href = "#swipe-direction-enum"

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
                    Name = "DiffY",
                    Type = "decimal",
                    DefaultValue = "0",
                    Description = "The vertical difference of swipe action in pixels."
                },
            ]
        }
    ];

    private readonly List<ComponentSubEnum> componentSubEnums =
    [
        new()
        {
            Id = "swipe-direction-enum",
            Name = "BitSwipeDirection",
            Description = "The direction in which the swipe trap triggers.",
            Items =
            [
                new()
                {
                    Name = "Right",
                    Value = "0",
                    Description = "Swipe to right direction."
                },
                new()
                {
                    Name = "Left",
                    Value = "1",
                    Description = "Swipe to left direction."
                },
                new()
                {
                    Name = "Top",
                    Value = "2",
                    Description = "Swipe to top direction."
                },
                new()
                {
                    Name = "Bottom",
                    Value = "3",
                    Description = "Swipe to bottom direction."
                },
            ]
        }
    ];



    private bool isTriggered;
    BitSwipeTrapEventArgs? swipeTrapEventArgs;
    BitSwipeTrapTriggerArgs? swipeTrapTriggerArgs;

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

    private void HandleOnTrigger(BitSwipeTrapTriggerArgs args)
    {
        isTriggered = true;
        swipeTrapTriggerArgs = args;
        _ = Task.Delay(2000).ContinueWith(_ =>
        {
            isTriggered = false;
            swipeTrapEventArgs = null;
            swipeTrapTriggerArgs = null;
            InvokeAsync(StateHasChanged);
        });
    }



    private readonly string example1RazorCode = @"
<style>
    .trap-container {
        width: 100%;
        cursor: grab;
        height: 500px;
        display: flex;
        user-select: none;
        align-items: center;
        flex-direction: column;
        justify-content: center;
        border: 1px solid lightgray;
    }
</style>

<BitSwipeTrap Style=""width:100%""
              Throttle=""10""
              OnStart=""HandleOnStart""
              OnMove=""HandleOnMove""
              OnEnd=""HandleOnEnd""
              OnTrigger=""HandleOnTrigger"">
    <div class=""trap-container"">
        <div>StartX: @swipeTrapEventArgs?.StartX</div>
        <div>StartY: @swipeTrapEventArgs?.StartY</div>
        <div>DiffX: @swipeTrapEventArgs?.DiffX</div>
        <div>DiffY: @swipeTrapEventArgs?.DiffY</div>
        <div>---</div>
        <div>Triggered? @isTriggered</div>
        <div>Trigger direction: <b>@swipeTrapTriggerArgs?.Direction</b></div>
        <div>Trigger diffX: @swipeTrapTriggerArgs?.DiffX</div>
        <div>Trigger diffY: @swipeTrapTriggerArgs?.DiffY</div>
    </div>
</BitSwipeTrap>";
    private readonly string example1CsharpCode = @"
private bool isTriggered;
BitSwipeTrapEventArgs? swipeTrapEventArgs;
BitSwipeTrapTriggerArgs? swipeTrapTriggerArgs;

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

private void HandleOnTrigger(BitSwipeTrapTriggerArgs args)
{
    isTriggered = true;
    swipeTrapTriggerArgs = args;
    _ = Task.Delay(2000).ContinueWith(async t =>
    {
        await t;
        isTriggered = false;
        swipeTrapEventArgs = null;
        swipeTrapTriggerArgs = null;
        await InvokeAsync(StateHasChanged);
    });
}";
}

