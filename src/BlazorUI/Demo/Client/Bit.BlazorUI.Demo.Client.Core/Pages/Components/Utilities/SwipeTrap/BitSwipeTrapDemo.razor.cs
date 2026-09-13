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
            Description = "The event callback for when the swipe action triggers based on the Trigger or TriggerVelocity constraints.",
            LinkType = LinkType.Link,
            Href = "#swipetrap-trigger-args",
        },
        new()
        {
            Name = "OrientationLock",
            Type = "BitSwipeOrientation?",
            DefaultValue = "null",
            Description = "Specifies the orientation lock in which the swipe trap allows to trap the swipe actions. A Horizontal or Vertical lock is fixed for the whole gesture, whichever direction it starts in: the locked axis is the only one trapped and the only one reported, while the other axis keeps its default browser behavior (via a matching touch-action) and always reports zero. Auto instead locks to the first axis the gesture moves along.",
            LinkType = LinkType.Link,
            Href = "#swipe-orientation",
        },
        new()
        {
            Name = "SkipSelector",
            Type = "string?",
            DefaultValue = "null",
            Description = "A CSS selector of descendant elements on which starting a swipe is ignored (e.g. inputs or nested interactive elements)."
        },
        new()
        {
            Name = "Threshold",
            Type = "decimal?",
            DefaultValue = "null",
            Description = "The distance in pixels a gesture must cover before the swipe trap takes it over and stops the default behavior. It is also what resolves the axis a diagonal gesture is moving along (default is 0)."
        },
        new()
        {
            Name = "Throttle",
            Type = "int?",
            DefaultValue = "null",
            Description = "The throttle time in milliseconds to apply a delay between periodic calls to raise the OnMove event (default is 0, meaning no throttling)."
        },
        new()
        {
            Name = "TouchOnly",
            Type = "bool",
            DefaultValue = "false",
            Description = "Ignores mouse swipes, trapping only touch (and pen) gestures."
        },
        new()
        {
            Name = "Trigger",
            Type = "decimal?",
            DefaultValue = "null",
            Description = "The swiping point to trigger and call the OnTrigger event: either a fraction of the element's width/height (values less than 1) or an absolute value in pixels (default is 0.25m)."
        },
        new()
        {
            Name = "TriggerVelocity",
            Type = "decimal?",
            DefaultValue = "null",
            Description = "The swiping velocity in pixels per millisecond that triggers and calls the OnTrigger event on release (a flick), even if the swiping distance has not reached the Trigger point (default is 0, meaning disabled)."
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
                    Description = "The horizontal start point of the swipe action in pixels, relative to the viewport."
                },
                new()
                {
                    Name = "StartY",
                    Type = "decimal",
                    DefaultValue = "0",
                    Description = "The vertical start point of the swipe action in pixels, relative to the viewport."
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
                new()
                {
                    Name = "VelocityX",
                    Type = "decimal",
                    DefaultValue = "0",
                    Description = "The horizontal velocity of the swipe action in pixels per millisecond."
                },
                new()
                {
                    Name = "VelocityY",
                    Type = "decimal",
                    DefaultValue = "0",
                    Description = "The vertical velocity of the swipe action in pixels per millisecond."
                },
                new()
                {
                    Name = "PointerType",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "The type of the pointer that performed the swipe action: \"mouse\", \"touch\" or \"pen\"."
                },
                new()
                {
                    Name = "IsCanceled",
                    Type = "bool",
                    DefaultValue = "false",
                    Description = "Whether the swipe action ended by being canceled (e.g. the browser took the gesture over) instead of a normal release. Only meaningful in the OnEnd event."
                },
                new()
                {
                    Name = "Duration",
                    Type = "decimal",
                    DefaultValue = "0",
                    Description = "The elapsed time of the swipe action in milliseconds, measured from the moment it started."
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
                new()
                {
                    Name = "VelocityX",
                    Type = "decimal",
                    DefaultValue = "0",
                    Description = "The horizontal velocity of the swipe action in pixels per millisecond."
                },
                new()
                {
                    Name = "VelocityY",
                    Type = "decimal",
                    DefaultValue = "0",
                    Description = "The vertical velocity of the swipe action in pixels per millisecond."
                },
                new()
                {
                    Name = "PointerType",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "The type of the pointer that performed the swipe action: \"mouse\", \"touch\" or \"pen\"."
                },
                new()
                {
                    Name = "Duration",
                    Type = "decimal",
                    DefaultValue = "0",
                    Description = "The elapsed time of the swipe action in milliseconds, measured from the moment it started."
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
                    Description = "No orientation lock for the swipe trap."
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
                new()
                {
                    Name = "Auto",
                    Value = "3",
                    Description = "Locks the trap to the first orientation the gesture moves along, trapping that axis and zeroing the other."
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
        _ = Task.Delay(3000).ContinueWith(async _ =>
        {
            isTriggeredBasic = false;
            swipeTrapEventArgsBasic = null;
            swipeTrapTriggerArgsBasic = null;
            await InvokeAsync(StateHasChanged);
        });
    }


    private BitSwipeTrapTriggerArgs? triggerArgsFractional;
    private BitSwipeTrapTriggerArgs? triggerArgsAbsolute;
    private void HandleOnTriggerFractional(BitSwipeTrapTriggerArgs args)
    {
        triggerArgsFractional = args;
    }
    private void HandleOnTriggerAbsolute(BitSwipeTrapTriggerArgs args)
    {
        triggerArgsAbsolute = args;
    }


    private decimal diffXHorizontalLock;
    private decimal diffYHorizontalLock;
    private decimal diffXVerticalLock;
    private decimal diffYVerticalLock;
    private void HandleOnMoveHorizontalLock(BitSwipeTrapEventArgs args)
    {
        diffXHorizontalLock = args.DiffX;
        diffYHorizontalLock = args.DiffY;
    }
    private void HandleOnEndHorizontalLock(BitSwipeTrapEventArgs args)
    {
        diffXHorizontalLock = 0;
        diffYHorizontalLock = 0;
    }
    private void HandleOnMoveVerticalLock(BitSwipeTrapEventArgs args)
    {
        diffXVerticalLock = args.DiffX;
        diffYVerticalLock = args.DiffY;
    }
    private void HandleOnEndVerticalLock(BitSwipeTrapEventArgs args)
    {
        diffXVerticalLock = 0;
        diffYVerticalLock = 0;
    }
    private decimal diffXAutoLock;
    private decimal diffYAutoLock;
    private void HandleOnMoveAutoLock(BitSwipeTrapEventArgs args)
    {
        diffXAutoLock = args.DiffX;
        diffYAutoLock = args.DiffY;
    }
    private void HandleOnEndAutoLock(BitSwipeTrapEventArgs args)
    {
        diffXAutoLock = 0;
        diffYAutoLock = 0;
    }


    private decimal diffXThreshold;
    private decimal diffYThreshold;
    private int moveCountThrottle;
    private decimal diffXThrottle;
    private decimal diffYThrottle;
    private void HandleOnMoveThreshold(BitSwipeTrapEventArgs args)
    {
        diffXThreshold = args.DiffX;
        diffYThreshold = args.DiffY;
    }
    private void HandleOnEndThreshold(BitSwipeTrapEventArgs args)
    {
        diffXThreshold = 0;
        diffYThreshold = 0;
    }
    private void HandleOnMoveThrottle(BitSwipeTrapEventArgs args)
    {
        moveCountThrottle++;
        diffXThrottle = args.DiffX;
        diffYThrottle = args.DiffY;
    }
    private void HandleOnEndThrottle(BitSwipeTrapEventArgs args)
    {
        moveCountThrottle = 0;
        diffXThrottle = 0;
        diffYThrottle = 0;
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
    private TaskCompletionSource? listTcs;
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
        listTcs?.SetResult();
    }
    private void HandleOnCancelList()
    {
        listTcs?.SetResult();
    }
    private void ResetList()
    {
        itemsList = Enumerable.Range(0, 10).ToList();
    }


    private bool isFlicked;
    private BitSwipeTrapTriggerArgs? swipeTrapTriggerArgsFlick;
    private void HandleOnTriggerFlick(BitSwipeTrapTriggerArgs args)
    {
        isFlicked = true;
        swipeTrapTriggerArgsFlick = args;
        _ = Task.Delay(3000).ContinueWith(async _ =>
        {
            isFlicked = false;
            swipeTrapTriggerArgsFlick = null;
            await InvokeAsync(StateHasChanged);
        });
    }


    private decimal diffXTouchOnly;
    private decimal diffYTouchOnly;
    private decimal diffXSkip;
    private decimal diffYSkip;
    private void HandleOnMoveTouchOnly(BitSwipeTrapEventArgs args)
    {
        diffXTouchOnly = args.DiffX;
        diffYTouchOnly = args.DiffY;
    }
    private void HandleOnEndTouchOnly(BitSwipeTrapEventArgs args)
    {
        diffXTouchOnly = 0;
        diffYTouchOnly = 0;
    }
    private void HandleOnMoveSkip(BitSwipeTrapEventArgs args)
    {
        diffXSkip = args.DiffX;
        diffYSkip = args.DiffY;
    }
    private void HandleOnEndSkip(BitSwipeTrapEventArgs args)
    {
        diffXSkip = 0;
        diffYSkip = 0;
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
            direction = Math.Abs(args.DiffX) >= Math.Abs(args.DiffY)
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
