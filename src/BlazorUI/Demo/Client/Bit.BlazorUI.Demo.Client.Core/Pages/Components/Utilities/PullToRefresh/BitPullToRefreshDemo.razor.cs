﻿namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Utilities.PullToRefresh;

public partial class BitPullToRefreshDemo
{
    private readonly List<ComponentParameter> componentParameters =
    [
        new()
        {
            Name = "AnchorElement",
            Type = "ElementReference?",
            DefaultValue = "null",
            Description = "The element reference of the anchor element that the pull to refresh adheres to.",
        },
        new()
        {
            Name = "AnchorSelector",
            Type = "string?",
            DefaultValue = "null",
            Description = "The CSS selector of the anchor element that the pull to refresh adheres to.",
        },
        new()
        {
            Name = "ChildContent",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "The content of the pull to refresh element",
        },
        new()
        {
            Name = "Factor",
            Type = "decimal",
            DefaultValue = "2",
            Description = "The factor to balance the pull height out.",
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
            Description = "The callback for when the threshold of the pull-down happens.",
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
            Name = "Threshold",
            Type = "int",
            DefaultValue = "10",
            Description = "The threshold in pixel for pulling height that starts the pull to refresh process.",
        },
        new()
        {
            Name = "Trigger",
            Type = "int",
            DefaultValue = "80",
            Description = "The pulling height in pixel that triggers the refresh.",
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
    ];



    private bool isRefreshedBasic;
    private async Task HandleOnRefreshBasic()
    {
        await Task.Delay(2000);
        isRefreshedBasic = true;
        _ = Task.Delay(1000).ContinueWith(_ => { isRefreshedBasic = false; InvokeAsync(StateHasChanged); });
    }

    private bool isRefreshedCustom;
    private async Task HandleOnRefreshCustom()
    {
        await Task.Delay(2000);
        isRefreshedCustom = true;
        _ = Task.Delay(1000).ContinueWith(_ => { isRefreshedCustom = false; InvokeAsync(StateHasChanged); });
    }

    private bool isRefreshed1;
    private bool isRefreshed2;
    private async Task HandleOnRefresh1()
    {
        await Task.Delay(2000);
        isRefreshed1 = true;
        _ = Task.Delay(1000).ContinueWith(_ => { isRefreshed1 = false; InvokeAsync(StateHasChanged); });
    }
    private async Task HandleOnRefresh2()
    {
        await Task.Delay(2000);
        isRefreshed2 = true;
        _ = Task.Delay(1000).ContinueWith(_ => { isRefreshed2 = false; InvokeAsync(StateHasChanged); });
    }



    private readonly string example1RazorCode = @"
<style>
    .anchor {
        width: 150px;
        padding: 4px;
        cursor: grab;
        height: 300px;
        overflow: auto;
        user-select: none;
        border: 1px gray solid;
    }
</style>

<div style=""display:flex; gap:1rem;"">
    <div>
        <div>isRefreshed? [@(isRefreshedBasic ? ""Yes"" : ""No"")]</div>
        <br />
        <div class=""anchor anchor-basic"">
            @for (int i = 1; i <= 50; i++)
            {
                <div>@(i.ToString().PadLeft(2, '0')). Item</div>
            }
        </div>
    </div>
</div>

<BitPullToRefresh AnchorSelector="".anchor-basic"" OnRefresh=""HandleOnRefreshBasic"" />";
    private readonly string example1CsharpCode = @"
private bool isRefreshedBasic;
private async Task HandleOnRefreshBasic()
{
    await Task.Delay(2000);
    isRefreshedBasic = true;
    _ = Task.Delay(1000).ContinueWith(_ => { isRefreshedBasic = false; InvokeAsync(StateHasChanged); });
}";

    private readonly string example2RazorCode = @"
<style>
    .anchor {
        width: 150px;
        padding: 4px;
        cursor: grab;
        height: 300px;
        overflow: auto;
        user-select: none;
        border: 1px gray solid;
    }
</style>

<div style=""display:flex; gap:1rem;"">
    <div>
        <div>isRefreshed? [@(isRefreshedCustom ? ""Yes"" : ""No"")]</div>
        <br />
        <div class=""anchor anchor-custom"">
            @for (int i = 1; i <= 50; i++)
            {
                <div>@(i.ToString().PadLeft(2, '0')). Item</div>
            }
        </div>
    </div>
</div>

<BitPullToRefresh AnchorSelector="".anchor-custom"" OnRefresh=""HandleOnRefreshCustom"">
    <svg viewBox=""0 0 490 490"" fill=""currentColor"">
        <path d=""M112.156,97.111c72.3-65.4,180.5-66.4,253.8-6.7l-58.1,2.2c-7.5,0.3-13.3,6.5-13,14c0.3,7.3,6.3,13,13.5,13 c0.2,0,0.3,0,0.5,0l89.2-3.3c7.3-0.3,13-6.2,13-13.5v-1c0-0.2,0-0.3,0-0.5v-0.1l0,0l-3.3-88.2c-0.3-7.5-6.6-13.3-14-13 c-7.5,0.3-13.3,6.5-13,14l2.1,55.3c-36.3-29.7-81-46.9-128.8-49.3c-59.2-3-116.1,17.3-160,57.1c-60.4,54.7-86,137.9-66.8,217.1 c1.5,6.2,7,10.3,13.1,10.3c1.1,0,2.1-0.1,3.2-0.4c7.2-1.8,11.7-9.1,9.9-16.3C36.656,218.211,59.056,145.111,112.156,97.111z"">
        </path>
        <path d=""M462.456,195.511c-1.8-7.2-9.1-11.7-16.3-9.9c-7.2,1.8-11.7,9.1-9.9,16.3c16.9,69.6-5.6,142.7-58.7,190.7 c-37.3,33.7-84.1,50.3-130.7,50.3c-44.5,0-88.9-15.1-124.7-44.9l58.8-5.3c7.4-0.7,12.9-7.2,12.2-14.7s-7.2-12.9-14.7-12.2l-88.9,8 c-7.4,0.7-12.9,7.2-12.2,14.7l8,88.9c0.6,7,6.5,12.3,13.4,12.3c0.4,0,0.8,0,1.2-0.1c7.4-0.7,12.9-7.2,12.2-14.7l-4.8-54.1 c36.3,29.4,80.8,46.5,128.3,48.9c3.8,0.2,7.6,0.3,11.3,0.3c55.1,0,107.5-20.2,148.7-57.4 C456.056,357.911,481.656,274.811,462.456,195.511z"">
        </path>
    </svg>
</BitPullToRefresh>";
    private readonly string example2CsharpCode = @"
private bool isRefreshedCustom;
private async Task HandleOnRefreshCustom()
{
    await Task.Delay(2000);
    isRefreshedCustom = true;
    _ = Task.Delay(1000).ContinueWith(_ => { isRefreshedCustom = false; InvokeAsync(StateHasChanged); });
}";

    private readonly string example3RazorCode = @"
<style>
    .anchor {
        width: 150px;
        padding: 4px;
        cursor: grab;
        height: 300px;
        overflow: auto;
        user-select: none;
        border: 1px gray solid;
    }
</style>

<div style=""display:flex; gap:1rem;"">
    <div>
        <div>isRefreshed? [@(isRefreshed1 ? ""Yes"" : ""No"")]</div>
        <br />
        <div class=""anchor anchor1"">
            @for (int i = 1; i <= 50; i++)
            {
                <div>@(i.ToString().PadLeft(2, '0')). Item</div>
            }
        </div>
    </div>

    <div>
        <div>isRefreshed? [@(isRefreshed2 ? ""Yes"" : ""No"")]</div>
        <br />
        <div class=""anchor anchor2"">
            @for (int i = 51; i <= 100; i++)
            {
                <div>@(i.ToString().PadLeft(2, '0')). Item</div>
            }
        </div>
    </div>
</div>

<BitPullToRefresh AnchorSelector="".anchor1"" OnRefresh=""HandleOnRefresh1"" />
<BitPullToRefresh AnchorSelector="".anchor2"" OnRefresh=""HandleOnRefresh2"" />";
    private readonly string example3CsharpCode = @"
private bool isRefreshed1;
private bool isRefreshed2;
private async Task HandleOnRefresh1()
{
    await Task.Delay(2000);
    isRefreshed1 = true;
    _ = Task.Delay(1000).ContinueWith(_ => { isRefreshed1 = false; InvokeAsync(StateHasChanged); });
}
private async Task HandleOnRefresh2()
{
    await Task.Delay(2000);
    isRefreshed2 = true;
    _ = Task.Delay(1000).ContinueWith(_ => { isRefreshed2 = false; InvokeAsync(StateHasChanged); });
}";

}
