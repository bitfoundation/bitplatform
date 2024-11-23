namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Utilities.PullToRefresh;

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
            DefaultValue = "80",
            Description = "The pulling height that triggers the refresh.",
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


    private bool isRefreshed;
    private async Task HandleOnRefresh()
    {
        isRefreshed = true;
        _ = Task.Delay(1000).ContinueWith(_ => { isRefreshed = false; InvokeAsync(StateHasChanged); });
    }



    private readonly string example1RazorCode = @"
<style>
    .anchor {
        width: 200px;
        height: 300px;
        overflow: auto;
        background: gray;
        margin-left: 50px;
    }
</style>

<div>isRefreshed? [@(isRefreshed ? ""Yes"" : ""No"")]</div>

<div class=""anchor"">
    @for (int i = 1; i < 51; i++)
    {
        <div>@(i). Item @(i)</div>
    }
</div>

<BitPullToRefresh Anchor="".anchor"" OnRefresh=""HandleOnRefresh"" />";
    private readonly string example1CsharpCode = @"
private bool isRefreshed;
private async Task HandleOnRefresh()
{
    isRefreshed = true;
}";
}
