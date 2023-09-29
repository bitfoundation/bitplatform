namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.ScrollablePane;

public partial class BitScrollablePaneDemo
{
    private readonly List<ComponentParameter> componentParameters = new()
    {
        new()
        {
            Name = "ChildContent",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "The content of ScrollablePane, It can be Any custom tag or a text.",
        },
        new()
        {
            Name = "OnScroll",
            Type = "EventCallback",
            Description = "Callback for when the ScrollablePane scrolled.",
        },
        new()
        {
            Name = "ScrollbarVisibility",
            Type = "BitScrollbarVisibility",
            DefaultValue= "BitScrollbarVisibility.Auto",
            Description = "Controls the visibility of scrollbars in the ScrollablePane.",
            Href = "#scrollbar-visibility-enum",
            LinkType = LinkType.Link,
        },
        new()
        {
            Name = "ScrollContainerFocus",
            Type = "bool",
            DefaultValue= "false",
            Description = "Makes the scrollable container focusable, to aid with keyboard-only scrolling Should only be set to true if the scrollable region will not contain any other focusable items.",
        }
    };

    private readonly List<ComponentSubEnum> componentSubEnums = new()
    {
        new()
        {
            Id = "scrollbar-visibility-enum",
            Name = "ScrollbarVisibility",
            Description = "",
            Items = new List<ComponentEnumItem>()
            {
                new() 
                {
                    Name = "Auto",
                    Value = "0",
                    Description = "Scrollbars are displayed automatically when needed based on the content size, and hidden when not needed."
                },
                new() 
                { 
                    Name = "Hidden",
                    Value = "1",
                    Description = "Scrollbars are always hidden, even if the content overflows the visible area."
                },
                new() 
                { 
                    Name = "Scroll",
                    Value = "2",
                    Description = "Scrollbars are always visible, allowing users to scroll through the content even if it doesn't overflow the visible area."
                }
            }
        }
    };



    private double itemsCount = 25;
    private BitScrollbarVisibility scrollbarVisibility;



    private readonly string example1RazorCode = @"
<style>
    .vertical-scroll-item {
        height: 2.75rem;
        margin: 0.5rem 0.5rem;
        padding: 0.5rem 1.25rem;
        background-color: #f2f2f2;
    }

    .horizontal-scroll-item {
        width: 4rem;
        height: 2.75rem;
        margin: 0.5rem 0.5rem;
        padding: 0.5rem 1.25rem;
        background-color: #f2f2f2;
    }

    .custom-scroll-container {
        height: 5rem;
        display: flex;
    }
</style>

<BitScrollablePane Style=""height: 19rem;"">
    @for (int i = 0; i < 25; i++)
    {
        var index = i;
        <div class=""vertical-scroll-item"">@index</div>
    }
</BitScrollablePane>

<BitScrollablePane Class=""custom-scroll-container"">
    @for (int i = 0; i < 25; i++)
    {
        var index = i;
        <div class=""horizontal-scroll-item"">@index</div>
    }
</BitScrollablePane>";

    private readonly string example2RazorCode = @"
<style>
    .vertical-scroll-item {
        height: 2.75rem;
        margin: 0.5rem 0.5rem;
        padding: 0.5rem 1.25rem;
        background-color: #f2f2f2;
    }
</style>
                    
<BitScrollablePane @bind-ScrollbarVisibility=""@scrollbarVisibility"" Style=""height: 19rem;"">
    @for (int i = 0; i < itemsCount; i++)
    {
        var index = i;
        <div class=""vertical-scroll-item"">@index</div>
    }
</BitScrollablePane>

<BitSpinButton Min=""0"" @bind-Value=""@itemsCount"" Label=""Items count"" Style=""max-width: 19rem"" />
                    
<BitChoiceGroup @bind-Value=""scrollbarVisibility"" Label=""Scrollbar visibility"" TItem=""BitChoiceGroupOption<BitScrollbarVisibility>"" TValue=""BitScrollbarVisibility"">
    <BitChoiceGroupOption Text=""Auto"" Value=""BitScrollbarVisibility.Auto"" />
    <BitChoiceGroupOption Text=""Hidden"" Value=""BitScrollbarVisibility.Hidden"" />
    <BitChoiceGroupOption Text=""Scroll"" Value=""BitScrollbarVisibility.Scroll"" />
</BitChoiceGroup>";
    private readonly string example2CsharpCode = @"
private double itemsCount = 25;
private BitScrollbarVisibility scrollbarVisibility;
";
}
