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
            Name = "KeyboardTrigger",
            Type = "bool",
            DefaultValue = "false",
            Description = "Lets the arrow keys raise OnTrigger in their own direction while the trap itself has the focus. Makes the trap a tab stop, names the keys in aria-keyshortcuts and honors a Horizontal or Vertical OrientationLock. The event carries zero distances and a PointerType of \"keyboard\"."
        },
        new()
        {
            Name = "OnStart",
            Type = "EventCallback<BitSwipeTrapEventArgs>",
            DefaultValue = "",
            Description = "Raised when a swipe starts on the trap.",
            LinkType = LinkType.Link,
            Href = "#swipetrap-event-args",
        },
        new()
        {
            Name = "OnMove",
            Type = "EventCallback<BitSwipeTrapEventArgs>",
            DefaultValue = "",
            Description = "Raised while a swipe moves, at most once per Throttle milliseconds.",
            LinkType = LinkType.Link,
            Href = "#swipetrap-event-args",
        },
        new()
        {
            Name = "OnEnd",
            Type = "EventCallback<BitSwipeTrapEventArgs>",
            DefaultValue = "",
            Description = "Raised when a swipe is released, or canceled (IsCanceled) by the browser, by leaving the trap before it was trapped, or by Escape.",
            LinkType = LinkType.Link,
            Href = "#swipetrap-event-args",
        },
        new()
        {
            Name = "OnTrigger",
            Type = "EventCallback<BitSwipeTrapTriggerArgs>",
            DefaultValue = "",
            Description = "Raised on the release of a swipe that passed Trigger or was flicked faster than TriggerVelocity, and on an arrow key with KeyboardTrigger.",
            LinkType = LinkType.Link,
            Href = "#swipetrap-trigger-args",
        },
        new()
        {
            Name = "OrientationLock",
            Type = "BitSwipeOrientation?",
            DefaultValue = "null",
            Description = "Locks the trap to one axis. Horizontal and Vertical trap and report only that axis for the whole gesture, leaving the other to the browser (it reads zero); Auto locks to the axis the gesture moves along first.",
            LinkType = LinkType.Link,
            Href = "#swipe-orientation",
        },
        new()
        {
            Name = "SkipSelector",
            Type = "string?",
            DefaultValue = "null",
            Description = "A CSS selector of descendants on which a swipe never starts, such as inputs or nested sliders."
        },
        new()
        {
            Name = "Threshold",
            Type = "decimal?",
            DefaultValue = "null",
            Description = "The distance in pixels a gesture covers before the trap takes it over; it also decides the axis of a diagonal one. Defaults to 0."
        },
        new()
        {
            Name = "Throttle",
            Type = "int?",
            DefaultValue = "null",
            Description = "The least time in milliseconds between two OnMove events; the latest move of a window still arrives when it closes. Defaults to 0 (no throttling)."
        },
        new()
        {
            Name = "TouchOnly",
            Type = "bool",
            DefaultValue = "false",
            Description = "Ignores mouse swipes, trapping only touch and pen gestures."
        },
        new()
        {
            Name = "Trigger",
            Type = "decimal?",
            DefaultValue = "null",
            Description = "How far a swipe travels before its release triggers: a fraction of the trap's size per axis below 1, pixels from 1 up. Defaults to 0.25."
        },
        new()
        {
            Name = "TriggerVelocity",
            Type = "decimal?",
            DefaultValue = "null",
            Description = "The release velocity in px/ms that triggers a flick short of Trigger. Defaults to 0 (off)."
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
                    Description = "Whether the swipe was canceled (the browser took it over, it left the trap before being trapped, or Escape was pressed) rather than released. Only meaningful in OnEnd."
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
                    Description = "The type of the pointer that performed the swipe action: \"mouse\", \"touch\" or \"pen\" - or \"keyboard\" for an arrow key with KeyboardTrigger."
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

    private readonly List<ComponentCssVariable> componentCssVariables =
    [
        new()
        {
            Name = "--bit-SwipeTrap-cursor",
            DefaultValue = "inherit",
            Description = "Pointer cursor over the trap at rest, e.g. grab.",
        },
        new()
        {
            Name = "--bit-SwipeTrap-swiping-cursor",
            DefaultValue = "grabbing",
            Description = "Pointer cursor while a swipe is being trapped.",
        },
        new()
        {
            Name = "--bit-SwipeTrap-focus-color",
            DefaultValue = "--bit-clr-pri-focus",
            Description = "Focus ring color of a trap the keyboard can reach (KeyboardTrigger).",
        },
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
    private BitSwipeTrapEventArgs? swipeTrapEventArgsBasic;
    private BitSwipeTrapTriggerArgs? swipeTrapTriggerArgsBasic;
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
    private BitSwipeTrapTriggerArgs? triggerArgsFlick;
    private void HandleOnTriggerFractional(BitSwipeTrapTriggerArgs args)
    {
        triggerArgsFractional = args;
    }
    private void HandleOnTriggerAbsolute(BitSwipeTrapTriggerArgs args)
    {
        triggerArgsAbsolute = args;
    }
    private void HandleOnTriggerFlick(BitSwipeTrapTriggerArgs args)
    {
        triggerArgsFlick = args;
    }


    private decimal diffXHorizontalLock;
    private decimal diffYHorizontalLock;
    private decimal diffXVerticalLock;
    private decimal diffYVerticalLock;
    private decimal diffXAutoLock;
    private decimal diffYAutoLock;
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


    private decimal diffXTouchOnly;
    private decimal diffYTouchOnly;
    private decimal diffXSkip;
    private decimal diffYSkip;
    private int moveCountDisabled;
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
    private void HandleOnMoveDisabled(BitSwipeTrapEventArgs args)
    {
        moveCountDisabled++;
    }


    private string? cardAction;
    private string? cardPointerType;
    private string? cardLastEnd;
    private void HandleOnTriggerKeyboard(BitSwipeTrapTriggerArgs args)
    {
        cardAction = args.Direction == BitSwipeDirection.Right ? "Archived" : "Snoozed";
        cardPointerType = args.PointerType;
    }
    private void HandleOnEndKeyboard(BitSwipeTrapEventArgs args)
    {
        cardLastEnd = args.IsCanceled ? "canceled" : "released";
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
            await ConfirmDeleteList(index);
        }
    }
    private async Task ConfirmDeleteList(int index)
    {
        deletingIndex = index;
        listTcs = new();
        isListDialogOpen = true;
        await listTcs.Task;
        isListDialogOpen = false;
        diffXList[index] = 0;
        deletingIndex = -1;
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
    private void TogglePanelAdvanced(BitSwipeDirection side)
    {
        if (panelOpen == side)
        {
            ClosePanelAdvanced();
        }
        else
        {
            OpenPanelAdvanced(side);
        }
    }
    private string IsPanelOpenAdvanced(BitSwipeDirection side) => panelOpen == side ? "true" : "false";
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
        else if ((panelOpen.HasValue is false && direction == BitSwipeDirection.Right) || (panelOpen == BitSwipeDirection.Left && direction == BitSwipeDirection.Left))
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


    private BitSwipeTrapTriggerArgs? triggerArgsCascaded;
    private BitSwipeTrapTriggerArgs? triggerArgsCascadedOwn;
    private readonly BitSwipeTrapParams[] swipeTrapParams =
    [
        new() { OrientationLock = BitSwipeOrientation.Horizontal, Trigger = 60m, KeyboardTrigger = true }
    ];
    private void HandleOnTriggerCascaded(BitSwipeTrapTriggerArgs args)
    {
        triggerArgsCascaded = args;
    }
    private void HandleOnTriggerCascadedOwn(BitSwipeTrapTriggerArgs args)
    {
        triggerArgsCascadedOwn = args;
    }


    private string? rtlCardAction;
    private void HandleOnTriggerRtl(BitSwipeTrapTriggerArgs args)
    {
        // The directions are physical, so in a right-to-left layout the end of the line is on the left.
        rtlCardAction = args.Direction == BitSwipeDirection.Left ? "بایگانی شد" : "به تعویق افتاد";
    }
}
