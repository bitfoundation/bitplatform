namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Utilities.PullToRefresh;

public partial class BitPullToRefreshDemo
{
    private readonly List<ComponentParameter> componentParameters =
    [
        new()
        {
            Name = "Anchor",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "The anchor element that the pull to refresh component adheres to (alias of ChildContent).",
        },
        new()
        {
            Name = "ChildContent",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "The anchor element that the pull to refresh component adheres to.",
        },
        new()
        {
            Name = "Classes",
            Type = "BitPullToRefreshClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS classes for different parts of the BitPullToRefresh.",
            LinkType = LinkType.Link,
            Href = "#class-styles",
        },
        new()
        {
            Name = "Color",
            Type = "BitColor?",
            DefaultValue = "null",
            Description = "The general color of the pull indicator. It colors the glyph inside the indicator's disc, which the pull, the refresh and the complete states all draw.",
            LinkType = LinkType.Link,
            Href = "#color-enum",
        },
        new()
        {
            Name = "Complete",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "The custom template to replace the default checkmark svg shown while the complete state is visible.",
        },
        new()
        {
            Name = "CompleteDelay",
            Type = "int",
            DefaultValue = "0",
            Description = "The duration in milliseconds to keep the complete indicator visible after a successful refresh before snapping back (0 disables the complete state).",
        },
        new()
        {
            Name = "CompleteLabel",
            Type = "string",
            DefaultValue = "Refresh complete",
            Description = "The text that gets announced to screen readers while the complete state is visible after a successful refresh.",
        },
        new()
        {
            Name = "CustomColor",
            Type = "string?",
            DefaultValue = "null",
            Description = "The custom css color of the pull indicator. It only applies while Color is left unset.",
        },
        new()
        {
            Name = "Factor",
            Type = "decimal",
            DefaultValue = "1.5",
            Description = "The factor to balance the pull height out. The pull-down distance gets divided by it, so higher values make the pull feel heavier. Values below 0.1 are treated as 0.1.",
        },
        new()
        {
            Name = "FullWidth",
            Type = "bool",
            DefaultValue = "false",
            Description = "Whether the component takes the whole width of its container instead of shrink-wrapping its anchor.",
        },
        new()
        {
            Name = "Loading",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "The custom loading template to replace the default loading svg. It is what the indicator shows while the pull is under way and while the refresh is running, so it covers every state that Release and Complete do not take over.",
        },
        new()
        {
            Name = "Margin",
            Type = "int",
            DefaultValue = "30",
            Description = "The value in pixel to add to the top of pull element as a margin for the pull height.",
        },
        new()
        {
            Name = "MaxPull",
            Type = "int",
            DefaultValue = "0",
            Description = "The furthest the pull can travel, in pixels, past which it stops following the finger; 0 stops it at Trigger. The indicator holds its full size over that stretch, and only the strip keeps growing. It is measured on the same damped scale as Trigger.",
        },
        new()
        {
            Name = "OnRefresh",
            Type = "EventCallback",
            DefaultValue = "",
            Description = "The callback for when the trigger condition of the pull-down happens.",
        },
        new()
        {
            Name = "OnPullStart",
            Type = "EventCallback<BitPullToRefreshPullStartArgs>",
            DefaultValue = "",
            Description = "The callback for the starting of the pull-down.",
            LinkType = LinkType.Link,
            Href = "#pull-start-args"
        },
        new()
        {
            Name = "OnPullMove",
            Type = "EventCallback<decimal>",
            DefaultValue = "",
            Description = "The callback for when the pull-down is in progress, reporting the pull height in pixels, which is capped at Trigger - or at MaxPull where the pull is allowed past it. The reports are coalesced to at most one per frame and never repeat a whole pixel.",
        },
        new()
        {
            Name = "OnPullEnd",
            Type = "EventCallback<decimal>",
            DefaultValue = "",
            Description = "The callback for the ending of the pull-down.",
        },
        new()
        {
            Name = "OnPullCancel",
            Type = "EventCallback<decimal>",
            DefaultValue = "",
            Description = "The callback for when the pull-down gets canceled before release, providing the last pull height.",
        },
        new()
        {
            Name = "RefreshingLabel",
            Type = "string",
            DefaultValue = "Refreshing",
            Description = "The text that gets announced to screen readers while the refresh is in progress.",
        },
        new()
        {
            Name = "Release",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "The custom template to replace the default svg while the pull has passed the trigger and releasing starts the refresh.",
        },
        new()
        {
            Name = "ReleaseLabel",
            Type = "string",
            DefaultValue = "Release to refresh",
            Description = "The text that gets announced to screen readers while the pull has passed the trigger and releasing starts the refresh. An empty string leaves the release state unannounced.",
        },
        new()
        {
            Name = "ScrollerElement",
            Type = "ElementReference?",
            DefaultValue = "null",
            Description = "The element that is the scroller in the anchor to control the behavior of the pull to refresh.",
        },
        new()
        {
            Name = "ScrollerSelector",
            Type = "string?",
            DefaultValue = "null",
            Description = "The CSS selector of the element that is the scroller in the anchor to control the behavior of the pull to refresh. It is looked up inside the anchor first and in the document afterwards; left unset, the first element of the anchor is taken as the scroller.",
        },
        new()
        {
            Name = "Styles",
            Type = "BitPullToRefreshClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS styles for different parts of the BitPullToRefresh.",
            LinkType = LinkType.Link,
            Href = "#class-styles",
        },
        new()
        {
            Name = "Threshold",
            Type = "int",
            DefaultValue = "0",
            Description = "The dead-zone distance in pixel that the pull-down must travel before the pull to refresh process starts and the indicator appears.",
        },
        new()
        {
            Name = "Trigger",
            Type = "int",
            DefaultValue = "80",
            Description = "The pulling height in pixel that triggers the refresh. It is also the distance the indicator grows to its full size over. Values below 1 are treated as 1.",
        }
    ];

    private readonly List<ComponentParameter> componentPublicMembers =
    [
        new()
        {
            Name = "IsRefreshing",
            Type = "bool",
            DefaultValue = "false",
            Description = "Whether a refresh is currently running - the pull was released past the trigger, or RefreshAsync was called, and the OnRefresh callback has not returned yet.",
        },
        new()
        {
            Name = "PullProgress",
            Type = "decimal",
            DefaultValue = "0",
            Description = "How far the current pull has come as a fraction of Trigger: 0 while nothing is being pulled, and 1 once releasing would start a refresh. It reads 1 for the whole of a refresh.",
        },
        new()
        {
            Name = "RefreshAsync",
            Type = "Task",
            Description = "Starts the refresh process programmatically, showing the loading indicator and invoking the OnRefresh callback. It has no effect while the component is disabled, a refresh is already in progress or the complete state is visible.",
        },
    ];

    private readonly List<ComponentSubEnum> componentSubEnums =
    [
        new()
        {
            Id = "color-enum",
            Name = "BitColor",
            Description = "Defines the general colors available in the bit BlazorUI.",
            Items =
            [
                new()
                {
                    Name= "Primary",
                    Description="Info Primary general color.",
                    Value="0",
                },
                new()
                {
                    Name= "Secondary",
                    Description="Secondary general color.",
                    Value="1",
                },
                new()
                {
                    Name= "Tertiary",
                    Description="Tertiary general color.",
                    Value="2",
                },
                new()
                {
                    Name= "Info",
                    Description="Info general color.",
                    Value="3",
                },
                new()
                {
                    Name= "Success",
                    Description="Success general color.",
                    Value="4",
                },
                new()
                {
                    Name= "Warning",
                    Description="Warning general color.",
                    Value="5",
                },
                new()
                {
                    Name= "SevereWarning",
                    Description="SevereWarning general color.",
                    Value="6",
                },
                new()
                {
                    Name= "Error",
                    Description="Error general color.",
                    Value="7",
                },
                new()
                {
                    Name= "PrimaryBackground",
                    Description="Primary background color.",
                    Value="8",
                },
                new()
                {
                    Name= "SecondaryBackground",
                    Description="Secondary background color.",
                    Value="9",
                },
                new()
                {
                    Name= "TertiaryBackground",
                    Description="Tertiary background color.",
                    Value="10",
                },
                new()
                {
                    Name= "PrimaryForeground",
                    Description="Primary foreground color.",
                    Value="11",
                },
                new()
                {
                    Name= "SecondaryForeground",
                    Description="Secondary foreground color.",
                    Value="12",
                },
                new()
                {
                    Name= "TertiaryForeground",
                    Description="Tertiary foreground color.",
                    Value="13",
                },
                new()
                {
                    Name= "PrimaryBorder",
                    Description="Primary border color.",
                    Value="14",
                },
                new()
                {
                    Name= "SecondaryBorder",
                    Description="Secondary border color.",
                    Value="15",
                },
                new()
                {
                    Name= "TertiaryBorder",
                    Description="Tertiary border color.",
                    Value="16",
                }
            ]
        }
    ];

    private readonly List<ComponentSubClass> componentSubClasses =
    [
        new()
        {
            Id = "pull-start-args",
            Title = "BitPullToRefreshPullStartArgs",
            Parameters =
            [
               new()
               {
                   Name = "Top",
                   Type = "decimal",
                   Description = "The top offset of the pull to refresh element in pixels.",
               },
               new()
               {
                   Name = "Left",
                   Type = "decimal",
                   Description = "The left offset of the pull to refresh element in pixels.",
               },
               new()
               {
                   Name = "Width",
                   Type = "decimal",
                   Description = "The width of the pull to refresh element in pixels.",
               },
            ]
        },
        new()
        {
            Id = "class-styles",
            Title = "BitPullToRefreshClassStyles",
            Parameters =
            [
               new()
               {
                   Name = "Root",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the root element of the PullToRefresh."
               },
               new()
               {
                   Name = "Loading",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the loading element."
               },
               new()
               {
                   Name = "SpinnerWrapper",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the spinner wrapper element."
               },
               new()
               {
                   Name = "SpinnerWrapperCanRelease",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the spinner wrapper element when the pull passed the trigger and releasing starts the refresh."
               },
               new()
               {
                   Name = "SpinnerWrapperRefreshing",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the spinner wrapper element in refreshing mode."
               },
               new()
               {
                   Name = "SpinnerWrapperComplete",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the spinner wrapper element while the complete state is visible after a successful refresh."
               },
               new()
               {
                   Name = "Spinner",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the spinner element."
               },
               new()
               {
                   Name = "SpinnerCanRelease",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the spinner element when the pull passed the trigger and releasing starts the refresh."
               },
               new()
               {
                   Name = "SpinnerRefreshing",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the spinner element in refreshing mode."
               },
               new()
               {
                   Name = "SpinnerComplete",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the spinner element while the complete state is visible after a successful refresh."
               },
            ]
        }
    ];



    private (int, int)[] basicItems = GenerateRandomNumbers(1, 51);
    private async Task HandleOnRefreshBasic()
    {
        await Task.Delay(2000);
        basicItems = GenerateRandomNumbers(1, 51);
        _ = Task.Delay(1000).ContinueWith(_ => InvokeAsync(StateHasChanged));
    }

    private (int, int)[] customItems = GenerateRandomNumbers(1, 51);
    private async Task HandleOnRefreshCustom()
    {
        await Task.Delay(2000);
        customItems = GenerateRandomNumbers(1, 51);
        _ = Task.Delay(1000).ContinueWith(_ => InvokeAsync(StateHasChanged));
    }

    private (int, int)[] multiItems1 = GenerateRandomNumbers(0, 50);
    private async Task HandleOnRefresh1()
    {
        await Task.Delay(2000);
        multiItems1 = GenerateRandomNumbers(1, 51);
        _ = Task.Delay(1000).ContinueWith(_ => InvokeAsync(StateHasChanged));
    }

    private (int, int)[] multiItems2 = GenerateRandomNumbers(51, 101);
    private async Task HandleOnRefresh2()
    {
        await Task.Delay(2000);
        multiItems2 = GenerateRandomNumbers(51, 101);
        _ = Task.Delay(1000).ContinueWith(_ => InvokeAsync(StateHasChanged));
    }

    private (int, int)[] advancedItems = GenerateRandomNumbers(1, 51);
    private async Task HandleOnRefreshAdvanced()
    {
        await Task.Delay(2000);
        advancedItems = GenerateRandomNumbers(1, 51);
        _ = Task.Delay(1000).ContinueWith(_ => InvokeAsync(StateHasChanged));
    }

    private bool isEnabled = true;
    private (int, int)[] disabledItems = GenerateRandomNumbers(1, 51);
    private async Task HandleOnRefreshDisabled()
    {
        await Task.Delay(2000);
        disabledItems = GenerateRandomNumbers(1, 51);
        _ = Task.Delay(1000).ContinueWith(_ => InvokeAsync(StateHasChanged));
    }

    private double trigger = 80;
    private double factor = 1.5;
    private double margin = 30;
    private double threshold = 0;
    private double maxPull = 0;
    private (int, int)[] behaviorItems = GenerateRandomNumbers(1, 51);
    private async Task HandleOnRefreshBehavior()
    {
        await Task.Delay(2000);
        behaviorItems = GenerateRandomNumbers(1, 51);
        _ = Task.Delay(1000).ContinueWith(_ => InvokeAsync(StateHasChanged));
    }

    private BitPullToRefresh pullToRefreshRef = default!;
    private (int, int)[] programmaticItems = GenerateRandomNumbers(1, 51);
    private async Task RefreshProgrammatically()
    {
        await pullToRefreshRef.RefreshAsync();
    }
    private async Task HandleOnRefreshProgrammatic()
    {
        await Task.Delay(2000);
        programmaticItems = GenerateRandomNumbers(1, 51);
        _ = Task.Delay(1000).ContinueWith(_ => InvokeAsync(StateHasChanged));
    }

    private int refreshCount;
    private decimal pullMoveDiff;
    private decimal pullEndDiff;
    private decimal pullCancelDiff;
    private BitPullToRefreshPullStartArgs? pullStartArgs;
    private (int, int)[] eventsItems = GenerateRandomNumbers(1, 51);
    private void HandleOnPullStart(BitPullToRefreshPullStartArgs args)
    {
        pullStartArgs = args;
    }
    private void HandleOnPullMove(decimal diff)
    {
        pullMoveDiff = diff;
    }
    private void HandleOnPullEnd(decimal diff)
    {
        pullEndDiff = diff;
    }
    private void HandleOnPullCancel(decimal diff)
    {
        pullCancelDiff = diff;
    }
    private async Task HandleOnRefreshEvents()
    {
        refreshCount++;
        await Task.Delay(2000);
        eventsItems = GenerateRandomNumbers(1, 51);
        _ = Task.Delay(1000).ContinueWith(_ => InvokeAsync(StateHasChanged));
    }

    private (int, int)[] completeItems = GenerateRandomNumbers(1, 51);
    private async Task HandleOnRefreshComplete()
    {
        await Task.Delay(2000);
        completeItems = GenerateRandomNumbers(1, 51);
        _ = Task.Delay(1000).ContinueWith(_ => InvokeAsync(StateHasChanged));
    }

    private (int, int)[] completeCustomItems = GenerateRandomNumbers(51, 101);
    private async Task HandleOnRefreshCompleteCustom()
    {
        await Task.Delay(2000);
        completeCustomItems = GenerateRandomNumbers(51, 101);
        _ = Task.Delay(1000).ContinueWith(_ => InvokeAsync(StateHasChanged));
    }

    private (int, int)[] styleItems = GenerateRandomNumbers(1, 51);
    private async Task HandleOnRefreshStyle()
    {
        await Task.Delay(2000);
        styleItems = GenerateRandomNumbers(1, 51);
        _ = Task.Delay(1000).ContinueWith(_ => InvokeAsync(StateHasChanged));
    }

    private (int, int)[] releaseItems = GenerateRandomNumbers(1, 51);
    private async Task HandleOnRefreshRelease()
    {
        await Task.Delay(2000);
        releaseItems = GenerateRandomNumbers(1, 51);
        _ = Task.Delay(1000).ContinueWith(_ => InvokeAsync(StateHasChanged));
    }

    private (int, int)[] colorItems = GenerateRandomNumbers(1, 51);
    private async Task HandleOnRefreshColor()
    {
        await Task.Delay(2000);
        colorItems = GenerateRandomNumbers(1, 51);
        _ = Task.Delay(1000).ContinueWith(_ => InvokeAsync(StateHasChanged));
    }

    private (int, int)[] customColorItems = GenerateRandomNumbers(51, 101);
    private async Task HandleOnRefreshCustomColor()
    {
        await Task.Delay(2000);
        customColorItems = GenerateRandomNumbers(51, 101);
        _ = Task.Delay(1000).ContinueWith(_ => InvokeAsync(StateHasChanged));
    }

    private (int, int)[] classItems = GenerateRandomNumbers(51, 101);
    private async Task HandleOnRefreshClass()
    {
        await Task.Delay(2000);
        classItems = GenerateRandomNumbers(51, 101);
        _ = Task.Delay(1000).ContinueWith(_ => InvokeAsync(StateHasChanged));
    }

    private (int, int)[] rtlItems = GenerateRandomNumbers(1, 51);
    private async Task HandleOnRefreshRtl()
    {
        await Task.Delay(2000);
        rtlItems = GenerateRandomNumbers(1, 51);
        _ = Task.Delay(1000).ContinueWith(_ => InvokeAsync(StateHasChanged));
    }


    private static (int, int)[] GenerateRandomNumbers(int min, int max)
    {
        var random = new Random();
        return Enumerable.Range(min, max - min).Select(i => (i, random.Next(min, max))).ToArray();
    }
}
