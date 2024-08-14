namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Surfaces.Callout;

public partial class BitCalloutDemo
{
    private readonly List<ComponentParameter> componentParameters =
    [
        new()
        {
            Name = "Anchor",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "The content of the anchor element of the callout.",
        },
        new()
        {
            Name = "AnchorEl",
            Type = "Func<ElementReference>?",
            DefaultValue = "null",
            Description = "The setter function for element reference to the external anchor element."
        },
        new()
        {
            Name = "AnchorId",
            Type = "string?",
            DefaultValue = "null",
            Description = "The id of the external anchor element."
        },
        new()
        {
            Name = "ChildContent",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "The content of the callout."
        },
        new()
        {
            Name = "Classes",
            Type = "BitCalloutClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS classes for different parts of the callout.",
            LinkType = LinkType.Link,
            Href = "#class-styles"
        },
        new()
        {
            Name = "Content",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "Alias for ChildContent."
        },
        new()
        {
            Name = "Direction",
            Type = "BitDropDirection?",
            DefaultValue = "null",
            Description = "Determines the allowed directions in which the callout should decide to be opened.",
            LinkType = LinkType.Link,
            Href = "#drop-direction-enum"
        },
        new()
        {
            Name = "IsOpen",
            Type = "bool",
            DefaultValue = "false",
            Description = "Determines the opening state of the callout."
        },
        new()
        {
            Name = "OnToggle",
            Type = "EventCallback<bool>",
            DefaultValue = "",
            Description = "The callback that is called when the callout opens or closes."
        },
        new()
        {
            Name = "ResponsiveMode",
            Type = "BitResponsiveMode?",
            DefaultValue = "null",
            Description = "Configures the responsive mode of the callout for the small screens.",
            LinkType = LinkType.Link,
            Href = "#responsive-mode-enum"
        },
        new()
        {
            Name = "Styles",
            Type = "BitCalloutClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS styles for different parts of the callout.",
            LinkType = LinkType.Link,
            Href = "#class-styles"
        },
    ];

    private readonly List<ComponentSubClass> componentSubClasses =
    [
        new()
        {
            Id = "class-styles",
            Title = "BitCalloutClassStyles",
            Parameters =
            [
                new()
                {
                    Name = "Root",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the root element of the BitCallout."
                },
                new()
                {
                    Name = "AnchorContainer",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the anchor container element of the BitCallout."
                },
                new()
                {
                    Name = "Opened",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the opened callout state of the BitCallout."
                },
                new()
                {
                    Name = "Content",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the content of the BitCallout."
                },
                new()
                {
                    Name = "Overlay",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the overlay of the BitCallout."
                },
            ]
        }
    ];

    private readonly List<ComponentSubEnum> componentSubEnums =
    [
        new()
        {
            Id = "drop-direction-enum",
            Name = "BitDropDirection",
            Description = "",
            Items =
            [
                new()
                {
                    Name = "All",
                    Value = "0",
                    Description = "The direction determined automatically based on the available spaces in all directions."
                },
                new()
                {
                    Name = "TopAndBottom",
                    Value = "1",
                    Description = "The direction determined automatically based on the available spaces in only top and bottom directions."
                },
            ]
        },
        new()
        {
            Id = "responsive-mode-enum",
            Name = "BitResponsiveMode",
            Description = "",
            Items =
            [
                new()
                {
                    Name = "None",
                    Value = "0",
                    Description = "Disables the responsive mode."
                },
                new()
                {
                    Name = "Panel",
                    Value = "1",
                    Description = "Enables the panel responsive mode."
                },
                new()
                {
                    Name = "Top",
                    Value = "2",
                    Description = "Enables the top responsive mode."
                },
            ]
        }
    ];

    private readonly List<ComponentParameter> componentPublicMembers =
    [
        new()
        {
            Name = "Toggle",
            Type = "Task",
            Description = "Toggles the callout to open/close it.",
        }
    ];



    private ElementReference anchorEl = default!;
    private BitCallout callout1 = default!;
    private BitCallout callout2 = default!;

    private bool isOpen;



    private readonly string example1RazorCode = @"
<BitCallout>
    <Anchor>
        <BitButton>Click me</BitButton>
    </Anchor>
    <Content>
        <div style=""width: fit-content; padding: 1rem; border:1px solid gray;"">
            This is the callout content.
        </div>
    </Content>
</BitCallout>";

    private readonly string example2RazorCode = @"
<BitButton Id=""anchor_id"" OnClick=""() => callout1.Toggle()"">AnchorId</BitButton>
<BitCallout AnchorId=""anchor_id"" @ref=""callout1"">
    <div style=""padding:1rem; border:1px solid gray;"">
        <BitCalendar />
    </div>
</BitCallout>


<button @ref=""anchorEl"" @onclick=""() => callout2.Toggle()"">AnchorEl</button>
<BitCallout AnchorEl=""() => anchorEl"" @ref=""callout2"">
    <div style=""padding:1rem; border:1px solid gray;"">
        <BitCalendar />
    </div>
</BitCallout>";
    private readonly string example2CsharpCode = @"
private ElementReference anchorEl;
private BitCallout callout1;
private BitCallout callout2;";

    private readonly string example3RazorCode = @"
<BitButton OnClick=""() => isOpen = true"">Open callout</BitButton>

<BitCallout @bind-IsOpen=""isOpen"">
    <Anchor>
        <button>Anchor</button>
    </Anchor>
    <Content>
        <div style=""width: fit-content; padding: 1rem; border:1px solid gray;"">
            This is the callout content.
        </div>
    </Content>
</BitCallout>";
    private readonly string example3CsharpCode = @"
private bool isOpen;";
}
