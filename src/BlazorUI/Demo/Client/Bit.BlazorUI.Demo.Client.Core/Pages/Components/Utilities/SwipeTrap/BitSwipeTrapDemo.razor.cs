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
        else if (args.Direction == BitSwipeDirection.Right)
        {
            OpenPanel();
        }
    }
    private string GetPanelStyle()
    {
        return diffXPanel switch
        {
            0 => string.Empty,
            < 0 => $"transform: translateX({diffXPanel}px)",
            > 0 => $"transform: translateX(calc(-100% + {diffXPanel}px))"
        };
    }


    private int deletingIndex = -1;
    private bool isListDialogOpen;
    private TaskCompletionSource listTcs;
    private List<int> itemsList = [.. Enumerable.Range(0, 13)];
    private decimal[] diffXList = [.. Enumerable.Repeat(0m, 13)];

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
        return x > 0 ? $"transform: translateX({x}px)" : string.Empty;
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
}

