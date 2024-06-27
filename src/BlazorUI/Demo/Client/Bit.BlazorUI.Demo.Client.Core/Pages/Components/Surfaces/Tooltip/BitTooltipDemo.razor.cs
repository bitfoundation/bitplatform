namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Surfaces.Tooltip;

public partial class BitTooltipDemo
{
    private readonly List<ComponentParameter> componentParameters =
    [
        new()
        {
            Name = "Anchor",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "Child content of component, the content that the Tooltip will apply to."
        },
        new()
        {
            Name = "ChildContent",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "Child content of component, the content that the Tooltip will apply to."
        },
        new()
        {
            Name = "Classes",
            Type = "BitTooltipClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS classes for different parts of the BitTooltip.",
            LinkType = LinkType.Link,
            Href = "#tooltip-class-styles"
        },
        new()
        {
            Name = "DefaultIsShown",
            Type = "bool",
            DefaultValue = "false",
            Description = "Default value of the IsShown."
        },
        new()
        {
            Name = "HideArrow",
            Type = "bool",
            DefaultValue = "false",
            Description = "Hides the arrow of tooltip."
        },
        new()
        {
            Name = "HideDelay",
            Type = "int",
            DefaultValue = "0",
            Description = "Delay (in milliseconds) before hiding the tooltip."
        },
        new()
        {
            Name = "IsShown",
            Type = "bool",
            DefaultValue = "false",
            Description = "The visibility state of the tooltip."
        },
        new()
        {
            Name = "IsShownChanged",
            Type = "EventCallback<bool>",
            DefaultValue = "",
            Description = ""
        },
        new()
        {
            Name = "Position",
            Type = "BitTooltipPosition",
            DefaultValue = "BitTooltipPosition.Top",
            Description = "The position of tooltip around its anchor.",
            LinkType = LinkType.Link,
            Href = "#tooltip-position-enum"
        },
        new()
        {
            Name = "Template",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "The content you want inside the tooltip."
        },
        new()
        {
            Name = "Text",
            Type = "string?",
            DefaultValue = "null",
            Description = "The text of tooltip to show."
        },
        new()
        {
            Name = "ShowOnClick",
            Type = "bool",
            DefaultValue = "false",
            Description = "Determines shows tooltip on click."
        },
        new()
        {
            Name = "ShowDelay",
            Type = "int",
            DefaultValue = "0",
            Description = "Delay (in milliseconds) before showing the tooltip."
        },
        new()
        {
            Name = "ShowOnFocus",
            Type = "bool",
            DefaultValue = "false",
            Description = "Determines shows tooltip on focus."
        },
        new()
        {
            Name = "ShowOnHover",
            Type = "bool",
            DefaultValue = "true",
            Description = "Determines shows tooltip on hover."
        },
        new()
        {
            Name = "Styles",
            Type = "BitTooltipClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS styles for different parts of the BitTooltip.",
            LinkType = LinkType.Link,
            Href = "#tooltip-class-styles"
        }
    ];

    private readonly List<ComponentSubEnum> componentSubEnums =
    [
        new()
        {
            Id = "tooltip-position-enum",
            Name = "BitTooltipPosition",
            Description = "",
            Items =
            [
                new()
                {
                    Name = "Top",
                    Value = "0",
                    Description = "The position of tooltip top of its anchor"
                },
                new()
                {
                    Name = "TopLeft",
                    Value = "1",
                    Description = "The position of tooltip top left of its anchor"
                },
                new()
                {
                    Name = "TopRight",
                    Value = "2",
                    Description = "The position of tooltip top right of its anchor"
                },
                new()
                {
                    Name = "RightTop",
                    Value = "3",
                    Description = "The position of tooltip right top of its anchor"
                },
                new()
                {
                    Name = "Right",
                    Value = "4",
                    Description = "The position of tooltip right of its anchor"
                },
                new()
                {
                    Name = "RightBottom",
                    Value = "5",
                    Description = "The position of tooltip right bottom of its anchor"
                },
                new()
                {
                    Name = "BottomRight",
                    Value = "6",
                    Description = "The position of tooltip bottom right of its anchor"
                },
                new()
                {
                    Name = "Bottom",
                    Value = "7",
                    Description = "The position of tooltip bottom of its anchor"
                },
                new()
                {
                    Name = "BottomLeft",
                    Value = "8",
                    Description = "The position of tooltip bottom left of its anchor"
                },
                new()
                {
                    Name = "LeftBottom",
                    Value = "9",
                    Description = "The position of tooltip left bottom of its anchor"
                },
                new()
                {
                    Name = "Left",
                    Value = "10",
                    Description = "The position of tooltip left of its anchor"
                },
                new()
                {
                    Name = "LeftTop",
                    Value = "11",
                    Description = "The position of tooltip left top of its anchor"
                }
            ]
        }
    ];

    private readonly List<ComponentSubClass> componentSubClasses =
    [
        new()
        {
            Id = "tooltip-class-styles",
            Title = "BitTooltipClassStyles",
            Parameters =
            [
               new()
               {
                   Name = "Root",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the root element of the BitTooltip."
               },
               new()
               {
                   Name = "TooltipWrapper",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the tooltip wrapper of the BitTooltip."
               },
               new()
               {
                   Name = "Tooltip",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the tooltip of the BitTooltip."
               },
               new()
               {
                   Name = "Arrow",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the arrow of the BitTooltip."
               }
            ]
        }
    ];



    private BitTooltipPosition tooltipPosition;
    private List<BitDropdownItem<BitTooltipPosition>> tooltipPositionList = Enum.GetValues(typeof(BitTooltipPosition))
        .Cast<BitTooltipPosition>()
        .Select(enumValue => new BitDropdownItem<BitTooltipPosition>
        {
            Value = enumValue,
            Text = enumValue.ToString()
        })
        .ToList();

    private bool isShown = true;
    private bool showOnClick = true;
    private bool showOnHover;
    private bool hideArrow;
    private double hideDelay = 800;



    private readonly string example1RazorCode = @"
<BitTooltip Text=""This is the tooltip text"">
    <BitButton ButtonStyle=""BitButtonStyle.Standard"">Hover over me</BitButton>
</BitTooltip>";

    private readonly string example2RazorCode = @"
<BitTooltip DefaultIsShown=""true"" Text=""Text"" Position=""BitTooltipPosition.Top"">
    <BitButton ButtonStyle=""BitButtonStyle.Standard"">Top</BitButton>
</BitTooltip>
<BitTooltip DefaultIsShown=""true"" Text=""Text"" Position=""BitTooltipPosition.Right"">
    <BitButton ButtonStyle=""BitButtonStyle.Standard"">Right</BitButton>
</BitTooltip>
<BitTooltip DefaultIsShown=""true"" Text=""Text"" Position=""BitTooltipPosition.Left"">
    <BitButton ButtonStyle=""BitButtonStyle.Standard"">Left</BitButton>
</BitTooltip>
<BitTooltip DefaultIsShown=""true"" Text=""Text"" Position=""BitTooltipPosition.Bottom"">
    <BitButton ButtonStyle=""BitButtonStyle.Standard"">Bottom</BitButton>
</BitTooltip>";

    private readonly string example3RazorCode = @"
<style>
    .custom-class .custom-content {
        gap: 0.5rem;
        padding: 0.5rem;
        color: blueviolet;
        border-radius: 1rem;
        display: inline-flex;
        box-shadow: aqua 0 0 0.5rem;
    }

    .custom-root {
        text-shadow: aqua 0 0 0.5rem;
    }

    .custom-tooltip {
        color: tomato;
        border: solid tomato;
        border-radius: 0.5rem;
    }

    .custom-arrow {
        border-right: solid tomato;
        border-bottom: solid tomato;
    }
</style>


<BitTooltip Text=""This is the tooltip text"" Class=""custom-class"">
    <div class=""custom-content"">
        <div>Item 1</div>
        <div>Item 2</div>
        <div>Item 3</div>
    </div>
</BitTooltip>


<BitTooltip Text=""This is the tooltip text"" Styles=""@(new() { Tooltip = ""box-shadow: aqua 0 0 0.5rem;"" })"">
    <BitButton ButtonStyle=""BitButtonStyle.Standard"">Hover over me</BitButton>
</BitTooltip>

<BitTooltip Text=""This is the tooltip text""
            Classes=""@(new() { Root = ""custom-root"",
                               Tooltip = ""custom-tooltip"",
                               Arrow = ""custom-arrow"" })"">
    <BitButton ButtonStyle=""BitButtonStyle.Standard"">Hover over me</BitButton>
</BitTooltip>";

    private readonly string example4RazorCode = @"
<BitTooltip>
    <Template>
        <ul style=""padding: 0.5rem; margin: 0;"">
            <li>1. One</li>
            <li>2. Two</li>
        </ul>
    </Template>
    <Anchor>
        <BitButton ButtonStyle=""BitButtonStyle.Standard"">Hover over me</BitButton>
    </Anchor>
</BitTooltip>";

    private readonly string example5RazorCode = @"
<BitTooltip Dir=""BitDir.Rtl"">
    <Template>
        <ul style=""padding: 0.5rem; margin: 0;"">
            <li>۱. یک</li>
            <li>۲. دو</li>
        </ul>
    </Template>
    <Anchor>
        <BitButton ButtonStyle=""BitButtonStyle.Standard"">نشانگر ماوس را روی من بیاورید</BitButton>
    </Anchor>
</BitTooltip>";

    private readonly string example6RazorCode = @"
<BitTooltip @bind-IsShown=""isShown""
            Text=""Text""
            HideArrow=""hideArrow""
            HideDelay=""(int)hideDelay""
            ShowOnClick=""showOnClick""
            ShowOnHover=""showOnHover""
            Position=""tooltipPosition"">
    <BitButton ButtonStyle=""BitButtonStyle.Standard"">Anchor</BitButton>
</BitTooltip>

<BitDropdown Label=""Tooltip positions"" Items=""tooltipPositionList"" @bind-Value=""tooltipPosition"" />
<BitSpinButton Label=""Hide delay"" @bind-Value=""hideDelay"" Mode=""BitSpinButtonMode.Inline"" />
<BitToggle @bind-Value=""isShown"" DefaultText=""Toggle tooltip state"" />
<BitToggle @bind-Value=""hideArrow"" DefaultText=""Toggle tooltip arrow"" />
<BitToggle @bind-Value=""showOnClick"" DefaultText=""Show tooltip on click"" />
<BitToggle @bind-Value=""showOnHover"" DefaultText=""Show tooltip on hover"" />";
    private readonly string example6CsharpCode = @"
private bool isShown = true;
private bool showOnClick = true;
private bool showOnHover;
private bool hideArrow;
private double hideDelay = 800;

private BitTooltipPosition tooltipPosition;

private List<BitDropdownItem<BitTooltipPosition>> tooltipPositionList = Enum.GetValues(typeof(BitTooltipPosition))
    .Cast<BitTooltipPosition>()
    .Select(enumValue => new BitDropdownItem<BitTooltipPosition>
    {
        Value = enumValue,
        Text = enumValue.ToString()
    })
    .ToList();";
}
