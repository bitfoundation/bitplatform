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
            Description = "The throttle time in milliseconds to apply a delay between periodic calls to raise the events (default is 10)."
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



    private bool isTriggeredBasic;
    BitSwipeTrapEventArgs? swipeTrapEventArgsBasic;
    BitSwipeTrapTriggerArgs? swipeTrapTriggerArgsBasic;
    private void HandleOnStartBasic(BitSwipeTrapEventArgs args)
    {
        swipeTrapEventArgsBasic = args;
    }
    private void HandleOnMoveBasic(BitSwipeTrapEventArgs args)
    {
        swipeTrapEventArgsBasic = args;
    }
    private void HandleOnEndBasic(BitSwipeTrapEventArgs args)
    {
        swipeTrapEventArgsBasic = args;
    }
    private void HandleOnTriggerBasic(BitSwipeTrapTriggerArgs args)
    {
        isTriggeredBasic = true;
        swipeTrapTriggerArgsBasic = args;
        _ = Task.Delay(3000).ContinueWith(_ =>
        {
            isTriggeredBasic = false;
            swipeTrapEventArgsBasic = null;
            swipeTrapTriggerArgsBasic = null;
            StateHasChanged();
        });
    }


    private decimal diffXPanel;
    private bool isPanelOpen;
    private void OpenPanel()
    {
        isPanelOpen = true;
    }
    private void ClosePanel()
    {
        isPanelOpen = false;
    }
    private void HandleOnMovePanel(BitSwipeTrapEventArgs args)
    {
        diffXPanel = args.DiffX;
    }
    private void HandleOnEndPanel(BitSwipeTrapEventArgs args)
    {
        diffXPanel = 0;
    }
    private void HandleOnTriggerPanel(BitSwipeTrapTriggerArgs args)
    {
        if (args.Direction == BitSwipeDirection.Left)
        {
            diffXPanel = 0;
            ClosePanel();
        }
    }
    private string GetPanelStyle()
    {
        return diffXPanel < 0 ? $"transform: translateX({diffXPanel}px)" : "";
    }


    private decimal diffXList;
    private void HandleOnMoveList(BitSwipeTrapEventArgs args)
    {
        diffXList = args.DiffX;
    }
    private void HandleOnEndList(BitSwipeTrapEventArgs args)
    {
        diffXList = 0;
    }
    private void HandleOnTriggerList(BitSwipeTrapTriggerArgs args)
    {
        if (args.Direction == BitSwipeDirection.Left)
        {
            diffXList = 0;
            ClosePanel();
        }
    }



    private readonly string example1RazorCode = @"
<style>
    .basic-container {
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

<BitSwipeTrap Throttle=""10""
              OnStart=""HandleOnStartBasic""
              OnMove=""HandleOnMoveBasic""
              OnEnd=""HandleOnEndBasic""
              OnTrigger=""HandleOnTriggerBasic"">
    <div class=""basic-container"">
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
private bool isTriggeredBasic;
BitSwipeTrapEventArgs? swipeTrapEventArgsBasic;
BitSwipeTrapTriggerArgs? swipeTrapTriggerArgsBasic;
private void HandleOnStartBasic(BitSwipeTrapEventArgs args)
{
    swipeTrapEventArgsBasic = args;
}
private void HandleOnMoveBasic(BitSwipeTrapEventArgs args)
{
    swipeTrapEventArgsBasic = args;
}
private void HandleOnEndBasic(BitSwipeTrapEventArgs args)
{
    swipeTrapEventArgsBasic = args;
}
private void HandleOnTriggerBasic(BitSwipeTrapTriggerArgs args)
{
    isTriggeredBasic = true;
    swipeTrapTriggerArgsBasic = args;
    _ = Task.Delay(2000).ContinueWith(_ =>
    {
        isTriggeredBasic = false;
        swipeTrapEventArgsBasic = null;
        swipeTrapTriggerArgsBasic = null;
        InvokeAsync(StateHasChanged);
    });
}";

    private readonly string example2RazorCode = @"
<style>
    .panel-container {
        width: 100%;
        height: 300px;
        overflow: hidden;
        user-select: none;
        position: relative;
        border: 1px solid lightgray;
    }

    .panel-container button {
        padding: 0.5rem;
    }

    .panel-container .panel {
        left: 0;
        color: black;
        width: 200px;
        cursor: grab;
        inset-block: 0;
        position: absolute;
        background-color: lightgray;
        transform: translateX(-100%);
    }

    .panel-container .panel.open {
        transform: translateX(0);
    }

    .panel-container .panel-trap {
        gap: 1rem;
        height: 100%;
        display: flex;
        flex-direction: column;
        background-color: gray;
    }
</style>

<div class=""panel-container"">
    <button @onclick=""OpenPanel"">
        Open
    </button>
    <div class=""panel@(isPanelOpen ? "" open"": """")"" style=""@GetPanelStyle()"">
        <button @onclick=""ClosePanel"" style=""position:absolute;top:0;right:0"">
            Close
        </button>
        <BitSwipeTrap Style=""width:100%;height:100%""
                      OnMove=""HandleOnMovePanel""
                      OnEnd=""HandleOnEndPanel""
                      OnTrigger=""HandleOnTriggerPanel"">
            <div class=""panel-trap"">
                <h3>Title</h3>
                <div>Item1</div>
                <div>Item2</div>
                <div>Item3</div>
            </div>
        </BitSwipeTrap>
    </div>
</div>";
    private readonly string example2CsharpCode = @"
private decimal diffXPanel;
private bool isPanelOpen;
private void OpenPanel()
{
    isPanelOpen = true;
}
private void ClosePanel()
{
    isPanelOpen = false;
}
private void HandleOnMovePanel(BitSwipeTrapEventArgs args)
{
    diffXPanel = args.DiffX;
}
private void HandleOnEndPanel(BitSwipeTrapEventArgs args)
{
    diffXPanel = 0;
}
private void HandleOnTriggerPanel(BitSwipeTrapTriggerArgs args)
{
    if (args.Direction == BitSwipeDirection.Left)
    {
        diffXPanel = 0;
        ClosePanel();
    }
}
private string GetPanelStyle()
{
    return diffXPanel < 0 ? $""transform: translateX({diffXPanel}px)"" : """";
}";

    private readonly string example3RazorCode = @"
<style>
    .basic-container {
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
              OnStart=""HandleOnStartBasic""
              OnMove=""HandleOnMoveBasic""
              OnEnd=""HandleOnEndBasic""
              OnTrigger=""HandleOnTriggerBasic"">
    <div class=""basic-container"">
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
    private readonly string example3CsharpCode = @"
private bool isTriggeredBasic;
BitSwipeTrapEventArgs? swipeTrapEventArgsBasic;
BitSwipeTrapTriggerArgs? swipeTrapTriggerArgsBasic;
private void HandleOnStartBasic(BitSwipeTrapEventArgs args)
{
    swipeTrapEventArgsBasic = args;
}
private void HandleOnMoveBasic(BitSwipeTrapEventArgs args)
{
    swipeTrapEventArgsBasic = args;
}
private void HandleOnEndBasic(BitSwipeTrapEventArgs args)
{
    swipeTrapEventArgsBasic = args;
}
private void HandleOnTriggerBasic(BitSwipeTrapTriggerArgs args)
{
    isTriggeredBasic = true;
    swipeTrapTriggerArgsBasic = args;
    _ = Task.Delay(2000).ContinueWith(_ =>
    {
        isTriggeredBasic = false;
        swipeTrapEventArgsBasic = null;
        swipeTrapTriggerArgsBasic = null;
        InvokeAsync(StateHasChanged);
    });
}";
}

