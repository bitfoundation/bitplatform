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
            Name = "Factor",
            Type = "decimal",
            DefaultValue = "1.5",
            Description = "The factor to balance the pull height out. The pull-down distance gets divided by it, so higher values make the pull feel heavier.",
        },
        new()
        {
            Name = "Loading",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "The custom loading template to replace the default loading svg.",
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
            Description = "The callback for when the pull-down is in progress.",
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
            Description = "The CSS selector of the element that is the scroller in the anchor to control the behavior of the pull to refresh.",
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
            Description = "The pulling height in pixel that triggers the refresh.",
        }
    ];

    private readonly List<ComponentParameter> componentPublicMembers =
    [
        new()
        {
            Name = "RefreshAsync",
            Type = "Task",
            Description = "Starts the refresh process programmatically, showing the loading indicator and invoking the OnRefresh callback. It has no effect while the component is disabled, a refresh is already in progress or the complete state is visible.",
        },
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

    private (int, int)[] classItems = GenerateRandomNumbers(51, 101);
    private async Task HandleOnRefreshClass()
    {
        await Task.Delay(2000);
        classItems = GenerateRandomNumbers(51, 101);
        _ = Task.Delay(1000).ContinueWith(_ => InvokeAsync(StateHasChanged));
    }


    private static (int, int)[] GenerateRandomNumbers(int min, int max)
    {
        var random = new Random();
        return Enumerable.Range(min, max - min).Select(i => (i, random.Next(min, max))).ToArray();
    }
}
