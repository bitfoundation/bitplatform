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
            Name = "OrientationLock",
            Type = "BitSwipeOrientation?",
            DefaultValue = "null",
            Description = "Specifies the orientation lock in which the swipe trap allows to trap the swipe actions.",
            LinkType = LinkType.Link,
            Href = "#swipe-orientation",
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
            Description = "The swiping point (fraction of element's width or an absolute value) to trigger and call the OnTrigger event (default is 0.25m)."
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
            Id = "swipe-orientation",
            Name = "BitSwipeOrientation",
            Description = "The lock orientation of the swipe trap component.",
            Items =
            [
                new()
                {
                    Name = "None",
                    Value = "0",
                    Description = "Not orientation lock for swipe trap."
                },
                new()
                {
                    Name = "Horizontal",
                    Value = "1",
                    Description = "Horizontal orientation lock of trapping the swipe action."
                },
                new()
                {
                    Name = "Vertical",
                    Value = "2",
                    Description = "Vertical orientation lock of trapping the swipe action."
                },
            ]
        },
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


    private int deletingIndex = -1;
    private bool isListDialogOpen;
    private TaskCompletionSource listTcs;
    private List<int> itemsList = Enumerable.Range(0, 10).ToList();
    private decimal[] diffXList = Enumerable.Repeat(0m, 10).ToArray();
    private void HandleOnMoveList(BitSwipeTrapEventArgs args, int index)
    {
        diffXList[index] = args.DiffX;
    }
    private void HandleOnEndList(BitSwipeTrapEventArgs args, int index)
    {
        if (diffXList[index] < 60)
        {
            diffXList[index] = 0;
        }
    }
    private async Task HandleOnTriggerList(BitSwipeTrapTriggerArgs args, int index)
    {
        if (args.Direction == BitSwipeDirection.Right)
        {
            deletingIndex = index;
            listTcs = new();
            isListDialogOpen = true;
            await listTcs.Task;
            isListDialogOpen = false;
            diffXList[index] = 0;
            deletingIndex = -1;
        }
    }
    private string GetRowStyle(int index)
    {
        var x = Math.Min(diffXList[index], 60);
        return x > 0 ? $"transform: translateX({x}px)" : "";
    }
    private void HandleOnOkList()
    {
        if (deletingIndex != -1)
        {
            itemsList.Remove(deletingIndex);
        }
        listTcs.SetResult();
    }
    private void HandleOnCancelList()
    {
        listTcs.SetResult();
    }
    private void ResetList()
    {
        itemsList = Enumerable.Range(0, 10).ToList();
    }


    private decimal? diffXPanelAdvanced;
    private BitSwipeDirection? direction;
    private BitSwipeDirection? panelOpen;
    private void OpenPanelAdvanced(BitSwipeDirection swipeDirection)
    {
        if (panelOpen == swipeDirection) return;

        direction = null;
        panelOpen = swipeDirection;
        diffXPanelAdvanced = 0;
    }
    private void ClosePanelAdvanced()
    {
        panelOpen = null;
        diffXPanelAdvanced = null;
    }
    private void HandleOnMovePanelAdvanced(BitSwipeTrapEventArgs args)
    {
        diffXPanelAdvanced = args.DiffX;

        if (Math.Abs(args.DiffX) > 2 || Math.Abs(args.DiffY) > 2)
        {
            direction = Math.Abs(args.DiffX) > Math.Abs(args.DiffY)
            ? args.DiffX > 0 ? BitSwipeDirection.Right : BitSwipeDirection.Left
            : args.DiffY > 0 ? BitSwipeDirection.Bottom : BitSwipeDirection.Top;
        }
        else
        {
            direction = null;
        }
    }
    private void HandleOnEndPanelAdvanced(BitSwipeTrapEventArgs args)
    {
        if (panelOpen.HasValue)
        {
            diffXPanelAdvanced = 0;
        }
        else
        {
            diffXPanelAdvanced = null;
        }
    }
    private void HandleOnTriggerPanelAdvanced(BitSwipeTrapTriggerArgs args)
    {
        if (args.Direction == BitSwipeDirection.Left)
        {
            if (panelOpen.HasValue is false || panelOpen == BitSwipeDirection.Right)
            {
                OpenPanelAdvanced(BitSwipeDirection.Right);
            }
            else if (panelOpen == BitSwipeDirection.Left)
            {
                ClosePanelAdvanced();
            }
        }
        else if (args.Direction == BitSwipeDirection.Right)
        {
            if (panelOpen.HasValue is false || panelOpen == BitSwipeDirection.Left)
            {
                OpenPanelAdvanced(BitSwipeDirection.Left);
            }
            else if (panelOpen == BitSwipeDirection.Right)
            {
                ClosePanelAdvanced();
            }
        }
    }
    private string GetLeftPanelAdvancedStyle()
    {
        if (panelOpen == BitSwipeDirection.Left && direction != BitSwipeDirection.Left)
        {
            return "transform: translateX(0px)";
        }
        else if((panelOpen.HasValue is false && direction == BitSwipeDirection.Right) || (panelOpen == BitSwipeDirection.Left && direction == BitSwipeDirection.Left))
        {
            return diffXPanelAdvanced switch
            {
                0 or > 200 => "transform: translateX(0px)",
                < 0 and < 200 => $"transform: translateX({diffXPanelAdvanced}px)",
                > 0 => $"transform: translateX(calc(-100% + {diffXPanelAdvanced}px))",
                _ => string.Empty
            };
        }

        return string.Empty;
    }
    private string GetRightPanelAdvancedStyle()
    {
        if (panelOpen == BitSwipeDirection.Right && direction != BitSwipeDirection.Right)
        {
            return "transform: translateX(0px)";
        }
        else if ((panelOpen.HasValue is false && direction == BitSwipeDirection.Left) || (panelOpen == BitSwipeDirection.Right && direction == BitSwipeDirection.Right))
        {
            return diffXPanelAdvanced switch
            {
                0 or < -200 => "transform: translateX(0px)",
                > 0 => $"transform: translateX({diffXPanelAdvanced}px)",
                < 0 => $"transform: translateX(calc(100% - {(-1 * diffXPanelAdvanced)}px))",
                _ => string.Empty
            };
        }

        return string.Empty;
    }
}

