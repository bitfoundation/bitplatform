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
    private readonly string example1CsharpCode = @"
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
