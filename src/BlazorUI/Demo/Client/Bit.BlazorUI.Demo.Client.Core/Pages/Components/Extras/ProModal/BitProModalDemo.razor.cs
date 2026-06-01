namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Extras.ProModal;

public partial class BitProModalDemo
{
    private readonly List<ComponentParameter> componentParameters =
    [
        new()
        {
            Name = "AbsolutePosition",
            Type = "bool",
            DefaultValue = "false",
            Description = "When true, the Modal will be positioned absolute instead of fixed.",
        },
        new()
        {
            Name = "AutoToggleScroll",
            Type = "bool",
            DefaultValue = "false",
            Description = "Enables the auto scrollbar toggle behavior of the Modal.",
        },
        new()
        {
            Name = "Blocking",
            Type = "bool",
            DefaultValue = "false",
            Description = "Whether the Modal can be light dismissed by clicking outside the Modal (on the overlay).",
        },
        new()
        {
            Name = "Body",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "The alias of the ChildContent.",
        },
        new()
        {
            Name = "ChildContent",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "The content of the Modal, it can be any custom tag or text.",
        },
        new()
        {
            Name = "Classes",
            Type = "BitProModalClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS classes for different parts of the BitProModal component.",
            Href = "#class-styles",
            LinkType = LinkType.Link,
        },
        new()
        {
            Name = "CloseIcon",
            Type = "BitIconInfo?",
            DefaultValue = "null",
            Description = "Gets or sets the icon to display in the close button using custom CSS classes for external icon libraries. Takes precedence over CloseIconName when both are set.",
            LinkType = LinkType.Link,
            Href = "#bit-icon-info",
        },
        new()
        {
            Name = "CloseIconName",
            Type = "string?",
            DefaultValue = "null",
            Description = "Gets or sets the name of the icon to display in the close button from the built-in Fluent UI icons.",
            LinkType = LinkType.Link,
            Href = "https://blazorui.bitplatform.dev/iconography",
        },
        new()
        {
            Name = "DragElementSelector",
            Type = "string?",
            DefaultValue = "null",
            Description = "The CSS selector of the drag element. by default it's the Modal container.",
        },
        new()
        {
            Name = "Draggable",
            Type = "bool",
            DefaultValue = "false",
            Description = "Whether the Modal can be dragged around.",
        },
        new()
        {
            Name = "Footer",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "The template used to render the footer section of the Modal.",
        },
        new()
        {
            Name = "FooterText",
            Type = "string?",
            DefaultValue = "null",
            Description = "The text of the footer section of the Modal.",
        },
        new()
        {
            Name = "FullHeight",
            Type = "bool",
            DefaultValue = "false",
            Description = "Makes the Modal height 100% of its parent container.",
        },
        new()
        {
            Name = "FullSize",
            Type = "bool",
            DefaultValue = "false",
            Description = "Makes the Modal width and height 100% of its parent container.",
        },
        new()
        {
            Name = "FullWidth",
            Type = "bool",
            DefaultValue = "false",
            Description = "Makes the Modal width 100% of its parent container.",
        },
        new()
        {
            Name = "Header",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "The template used to render the header section of the Modal.",
        },
        new()
        {
            Name = "HeaderText",
            Type = "string?",
            DefaultValue = "null",
            Description = "The text of the header section of the Modal.",
        },
        new()
        {
            Name = "IsAlert",
            Type = "bool?",
            DefaultValue = "null",
            Description = "Determines the ARIA role of the Modal (alertdialog/dialog). If this is set, it will override the ARIA role determined by Blocking and Modeless.",
        },
        new()
        {
            Name = "IsOpen",
            Type = "bool",
            DefaultValue = "false",
            Description = "Whether the Modal is displayed.",
        },
        new()
        {
            Name = "ModeFull",
            Type = "bool",
            DefaultValue = "false",
            Description = "Renders the overlay in full mode that gives it an opaque background.",
        },
        new()
        {
            Name = "Modeless",
            Type = "bool",
            DefaultValue = "false",
            Description = "Whether the Modal should be modeless. if true: Blocking is ignored, there will be no overlay.",
        },
        new()
        {
            Name = "NoBorder",
            Type = "bool",
            DefaultValue = "false",
            Description = "Removes the default top border of the Modal.",
        },
        new()
        {
            Name = "OnDismiss",
            Type = "EventCallback<MouseEventArgs>",
            Description = "A callback function for when the Modal is dismissed.",
        },
        new()
        {
            Name = "OnOpen",
            Type = "EventCallback",
            Description = "A callback function for when the Modal is opened.",
        },
        new()
        {
            Name = "OnOverlayClick",
            Type = "EventCallback<MouseEventArgs>",
            Description = "A callback function for when somewhere on the overlay element of the Modal is clicked.",
        },
        new()
        {
            Name = "Position",
            Type = "BitPosition?",
            DefaultValue = "null",
            Description = "Position of the Modal on the screen.",
            Href = "#position-enum",
            LinkType = LinkType.Link,
        },
        new()
        {
            Name = "ScrollerSelector",
            Type = "string?",
            DefaultValue = "null",
            Description = "Set the element selector for which the Modal disables its scroll if applicable.",
        },
        new()
        {
            Name = "ShowCloseButton",
            Type = "bool",
            DefaultValue = "false",
            Description = "Shows the close button of the Modal.",
        },
        new()
        {
            Name = "Styles",
            Type = "BitProModalClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS styles for different parts of the BitProModal component.",
            Href = "#class-styles",
            LinkType = LinkType.Link,
        }
    ];

    private readonly List<ComponentSubClass> componentSubClasses =
    [
        new()
        {
            Id = "class-styles",
            Title = "BitProModalClassStyles",
            Parameters =
            [
               new()
               {
                   Name = "Root",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the root element of the BitProModal."
               },
               new()
               {
                   Name = "Overlay",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the overlay of the BitProModal."
               },
               new()
               {
                   Name = "Content",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the content of the BitProModal."
               },
               new()
               {
                   Name = "HeaderContainer",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the header container of the BitProModal."
               },
               new()
               {
                   Name = "Header",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the header of the BitProModal."
               },
               new()
               {
                   Name = "CloseButton",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the close button of the BitProModal."
               },
               new()
               {
                   Name = "CloseIcon",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the close icon of the BitProModal."
               },
               new()
               {
                   Name = "Body",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the body of the BitProModal."
               },
               new()
               {
                   Name = "Footer",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the footer of the BitProModal."
               }
            ]
        },
        new()
        {
            Id = "bit-icon-info",
            Title = "BitIconInfo",
            Parameters =
            [
               new()
               {
                   Name = "Name",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Gets or sets the name of the icon."
               },
               new()
               {
                   Name = "BaseClass",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Gets or sets the base CSS class for the icon. For built-in Fluent UI icons, this defaults to \"bit-icon\". For external icon libraries like FontAwesome, you might set this to \"fa\" or leave empty."
               },
               new()
               {
                   Name = "Prefix",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Gets or sets the CSS class prefix used before the icon name. For built-in Fluent UI icons, this defaults to \"bit-icon--\". For external icon libraries, you might set this to \"fa-\" or leave empty."
               },
            ]
        }
    ];

    private readonly List<ComponentSubEnum> componentSubEnums =
    [
        new()
        {
            Id = "position-enum",
            Name = "BitPosition",
            Description = "",
            Items =
            [
                new() { Name = "TopLeft", Value = "0" },
                new() { Name = "TopCenter", Value = "1" },
                new() { Name = "TopRight", Value = "2" },
                new() { Name = "TopStart", Value = "3" },
                new() { Name = "TopEnd", Value = "4" },
                new() { Name = "CenterLeft", Value = "5" },
                new() { Name = "Center", Value = "6" },
                new() { Name = "CenterRight", Value = "7" },
                new() { Name = "CenterStart", Value = "8" },
                new() { Name = "CenterEnd", Value = "9" },
                new() { Name = "BottomLeft", Value = "10" },
                new() { Name = "BottomCenter", Value = "11" },
                new() { Name = "BottomRight", Value = "12" },
                new() { Name = "BottomStart", Value = "13" },
                new() { Name = "BottomEnd", Value = "14" },
            ]
        }
    ];



    private bool isBasicProModalOpen;

    private bool isProModalWithHeaderTextOpen;
    private bool isProModalWithHeaderOpen;
    private bool isProModalWithFooterTextOpen;
    private bool isProModalWithFooterOpen;

    private bool isBlockingProModalOpen;
    private bool isModelessProModalOpen;
    private bool isModeFullProModalOpen;
    private bool isAutoToggleScrollProModalOpen;
    private bool isNoBorderProModalOpen;
    private BitProModal bitProModalRef = default!;

    private bool isOpenFullSize;
    private bool isFullSize;
    private bool isOpenFullWidth;
    private bool isFullWidth;
    private bool isOpenFullHeight;
    private bool isFullHeight;

    private bool isOpenAbsolutePosition;
    private bool isOpenScrollerSelector;

    private bool isOpenPosition;
    private BitPosition position;
    private void OpenModalInPosition(BitPosition positionValue)
    {
        isOpenPosition = true;
        position = positionValue;
    }

    private bool isOpenDraggable;
    private bool isOpenDraggableSelector;

    private bool isOnOpenProModalOpen;
    private bool isOnDismissProModalOpen;
    private BitTextField onOpenTextFieldRef = default!;
    private BitTextField onDismissTextFieldRef = default!;

    private bool isOpenStyle;
    private bool isOpenClass;
    private bool isOpenStyles;
    private bool isOpenClasses;

    private bool isExternalIconProModalOpen;

    private bool isOpenRtl;



    private readonly string example1RazorCode = @"
<BitButton OnClick=""() => isBasicProModalOpen = true"">Open ProModal</BitButton>
<BitProModal @bind-IsOpen=""isBasicProModalOpen"">
    <div style=""padding:1rem; max-width:40rem"">
        Lorem ipsum dolor sit amet, consectetur adipiscing elit.
    </div>
</BitProModal>";
    private readonly string example1CsharpCode = @"
private bool isBasicProModalOpen;";

    private readonly string example2RazorCode = @"
<BitButton OnClick=""() => isProModalWithHeaderTextOpen = true"">Open ProModal with HeaderText</BitButton>
<BitProModal @bind-IsOpen=""isProModalWithHeaderTextOpen"" HeaderText=""BitProModal with HeaderText"">
    <div style=""padding:1rem; max-width:40rem"">
        Lorem ipsum dolor sit amet, consectetur adipiscing elit.
    </div>
</BitProModal>

<BitButton OnClick=""() => isProModalWithHeaderOpen = true"">Open ProModal with Header</BitButton>
<BitProModal @bind-IsOpen=""isProModalWithHeaderOpen"">
    <Header>
        <div>
            <div style=""margin-bottom:4px"">BitProModal with Header</div>
            <BitSearchBox Placeholder=""Search here..."" />
        </div>
    </Header>
    <Body>
        <div style=""padding:1rem; max-width:40rem"">
            Lorem ipsum dolor sit amet, consectetur adipiscing elit.
        </div>
    </Body>
</BitProModal>


<BitButton OnClick=""() => isProModalWithFooterTextOpen = true"">Open ProModal with FooterText</BitButton>
<BitProModal @bind-IsOpen=""isProModalWithFooterTextOpen"" FooterText=""BitProModal with FooterText"">
    <div style=""padding:1rem; max-width:40rem"">
        Lorem ipsum dolor sit amet, consectetur adipiscing elit.
    </div>
</BitProModal>

<BitButton OnClick=""() => isProModalWithFooterOpen = true"">Open ProModal with Footer</BitButton>
<BitProModal @bind-IsOpen=""isProModalWithFooterOpen"">
    <Body>
        <div style=""padding:1rem; max-width:40rem"">
            Lorem ipsum dolor sit amet, consectetur adipiscing elit.
        </div>
    </Body>
    <Footer>
        <h3 style=""margin-bottom:4px"">BitProModal with Footer</h3>
        <BitButton OnClick=""() => isProModalWithFooterOpen = false"">Save</BitButton>
        <BitButton OnClick=""() => isProModalWithFooterOpen = false"" Variant=""BitVariant.Outline"">Close</BitButton>
    </Footer>
</BitProModal>";
    private readonly string example2CsharpCode = @"
private bool isProModalWithHeaderTextOpen;
private bool isProModalWithHeaderOpen;
private bool isProModalWithFooterTextOpen;
private bool isProModalWithFooterOpen;";

    private readonly string example3RazorCode = @"
<BitButton OnClick=""() => bitProModalRef.Open()"">Open ProModal with ShowCloseButton</BitButton>
<BitProModal @ref=""bitProModalRef"" HeaderText=""ShowCloseButton"" ShowCloseButton>
    <div style=""padding:1rem; max-width:40rem"">Lorem ipsum...</div>
</BitProModal>

<BitButton OnClick=""() => isBlockingProModalOpen = true"">Open ProModal with Blocking</BitButton>
<BitProModal @bind-IsOpen=""isBlockingProModalOpen"" HeaderText=""Blocking"" ShowCloseButton Blocking>
    <div style=""padding:1rem; max-width:40rem"">Lorem ipsum...</div>
</BitProModal>

<BitButton OnClick=""() => isModelessProModalOpen = !isModelessProModalOpen"">Toggle ProModal with Modeless</BitButton>
<BitProModal @bind-IsOpen=""isModelessProModalOpen"" HeaderText=""Modeless"" ShowCloseButton Modeless>
    <div style=""padding:1rem; max-width:40rem"">Lorem ipsum...</div>
</BitProModal>

<BitButton OnClick=""() => isModeFullProModalOpen = true"">Open ProModal with ModeFull</BitButton>
<BitProModal @bind-IsOpen=""isModeFullProModalOpen"" HeaderText=""ModeFull"" ShowCloseButton ModeFull>
    <div style=""padding:1rem; max-width:40rem"">Lorem ipsum...</div>
</BitProModal>

<BitButton OnClick=""() => isAutoToggleScrollProModalOpen = true"">Open ProModal with AutoToggleScroll</BitButton>
<BitProModal @bind-IsOpen=""isAutoToggleScrollProModalOpen"" HeaderText=""AutoToggleScroll"" ShowCloseButton AutoToggleScroll>
    <div style=""padding:1rem; max-width:40rem"">Lorem ipsum...</div>
</BitProModal>

<BitButton OnClick=""() => isNoBorderProModalOpen = true"">Open ProModal with NoBorder</BitButton>
<BitProModal @bind-IsOpen=""isNoBorderProModalOpen"" HeaderText=""NoBorder"" ShowCloseButton NoBorder>
    <div style=""padding:1rem; max-width:40rem"">Lorem ipsum...</div>
</BitProModal>";
    private readonly string example3CsharpCode = @"
private bool isBlockingProModalOpen;
private bool isModelessProModalOpen;
private bool isModeFullProModalOpen;
private bool isAutoToggleScrollProModalOpen;
private bool isNoBorderProModalOpen;
private BitProModal bitProModalRef = default!;";

    private readonly string example4RazorCode = @"
<BitButton OnClick=""() => isOpenFullSize = true"">Open ProModal with FullSize</BitButton>
<BitProModal @bind-IsOpen=""isOpenFullSize"" FullSize=""isFullSize"" HeaderText=""FullSize ProModal"" ShowCloseButton>
    <div style=""padding:1rem; max-width:40rem"">
        Lorem ipsum...
        <hr />
        <BitToggleButton @bind-IsChecked=""isFullSize"" OnText=""Restore"" OffText=""FullSize"" />
    </div>
</BitProModal>

<BitButton OnClick=""() => isOpenFullWidth = true"">Open ProModal with FullWidth</BitButton>
<BitProModal @bind-IsOpen=""isOpenFullWidth"" FullWidth=""isFullWidth"" HeaderText=""FullWidth ProModal"" ShowCloseButton>
    <div style=""padding:1rem"">
        Lorem ipsum...
        <hr />
        <BitToggleButton @bind-IsChecked=""isFullWidth"" OnText=""Restore"" OffText=""FullWidth"" />
    </div>
</BitProModal>

<BitButton OnClick=""() => isOpenFullHeight = true"">Open ProModal with FullHeight</BitButton>
<BitProModal @bind-IsOpen=""isOpenFullHeight"" FullHeight=""isFullHeight"" HeaderText=""FullHeight ProModal"" ShowCloseButton>
    <div style=""padding:1rem; max-width:40rem"">
        Lorem ipsum...
        <hr />
        <BitToggleButton @bind-IsChecked=""isFullHeight"" OnText=""Restore"" OffText=""FullHeight"" />
    </div>
</BitProModal>";
    private readonly string example4CsharpCode = @"
private bool isOpenFullSize;
private bool isFullSize;
private bool isOpenFullWidth;
private bool isFullWidth;
private bool isOpenFullHeight;
private bool isFullHeight;";

    private readonly string example5RazorCode = @"
<style>
    .relative-container {
        width: 100%;
        height: 400px;
        overflow: auto;
        margin-top: 1rem;
        position: relative;
        border: 2px lightgreen solid;
    }
</style>


<BitButton OnClick=""() => isOpenAbsolutePosition = true"">Open ProModal</BitButton>

<BitButton OnClick=""() => isOpenScrollerSelector = true"">Open ProModal (AutoToggleScroll & ScrollerSelector)</BitButton>

<div class=""relative-container"" id=""modal-scroller"">
    <BitProModal @bind-IsOpen=""isOpenAbsolutePosition"" HeaderText=""AbsolutePosition"" ShowCloseButton AbsolutePosition>
        <div style=""padding:1rem; max-width:40rem"">Lorem ipsum...</div>
    </BitProModal>

    <BitProModal @bind-IsOpen=""isOpenScrollerSelector""
                 HeaderText=""AbsolutePosition with AutoToggleScroll and ScrollerSelector""
                 ShowCloseButton
                 AbsolutePosition AutoToggleScroll ScrollerSelector=""#modal-scroller"">
        <div style=""padding:1rem; max-width:40rem"">Lorem ipsum...</div>
    </BitProModal>
</div>";
    private readonly string example5CsharpCode = @"
private bool isOpenAbsolutePosition;
private bool isOpenScrollerSelector;";

    private readonly string example6RazorCode = @"
<BitButton Class=""position-button"" OnClick=""() => OpenModalInPosition(BitPosition.TopLeft)"">Top Left</BitButton>
<BitButton Class=""position-button"" OnClick=""() => OpenModalInPosition(BitPosition.TopCenter)"">Top Center</BitButton>
<BitButton Class=""position-button"" OnClick=""() => OpenModalInPosition(BitPosition.TopRight)"">Top Right</BitButton>

<BitButton Class=""position-button"" OnClick=""() => OpenModalInPosition(BitPosition.CenterLeft)"">Center Left</BitButton>
<BitButton Class=""position-button"" OnClick=""() => OpenModalInPosition(BitPosition.Center)"">Center</BitButton>
<BitButton Class=""position-button"" OnClick=""() => OpenModalInPosition(BitPosition.CenterRight)"">Center Right</BitButton>

<BitButton Class=""position-button"" OnClick=""() => OpenModalInPosition(BitPosition.BottomLeft)"">Bottom Left</BitButton>
<BitButton Class=""position-button"" OnClick=""() => OpenModalInPosition(BitPosition.BottomCenter)"">Bottom Center</BitButton>
<BitButton Class=""position-button"" OnClick=""() => OpenModalInPosition(BitPosition.BottomRight)"">Bottom Right</BitButton>

<BitProModal @bind-IsOpen=""isOpenPosition"" Position=""position"" ShowCloseButton>
    <Header>Position: @position</Header>
    <Body>
        <div style=""padding:1rem; max-width:40rem"">Lorem ipsum...</div>
    </Body>
</BitProModal>";
    private readonly string example6CsharpCode = @"
private bool isOpenPosition;
private BitPosition position;

private void OpenModalInPosition(BitPosition positionValue)
{
    isOpenPosition = true;
    position = positionValue;
}";

    private readonly string example7RazorCode = @"
<BitButton OnClick=""() => isOpenDraggable = true"">Open ProModal</BitButton>
<BitProModal @bind-IsOpen=""isOpenDraggable"" Draggable HeaderText=""Draggable"" ShowCloseButton>
    <div style=""padding:1rem; max-width:40rem"">Lorem ipsum...</div>
</BitProModal>


<BitButton OnClick=""() => isOpenDraggableSelector = true"">Open ProModal</BitButton>
<BitProModal @bind-IsOpen=""isOpenDraggableSelector"" Draggable DragElementSelector=""#modal-drag-element"" ShowCloseButton>
    <div style=""padding:1rem; max-width:40rem"">
        <h3 id=""modal-drag-element"" style=""color:white; background:brown; padding:1rem"">
            Draggable with DragElementSelector
        </h3>
        Lorem ipsum...
    </div>
</BitProModal>";
    private readonly string example7CsharpCode = @"
private bool isOpenDraggable;
private bool isOpenDraggableSelector;";

    private readonly string example8RazorCode = @"
<BitButton OnClick=""() => isOnOpenProModalOpen = true"">Open OnOpen ProModal</BitButton>
<BitProModal @bind-IsOpen=""isOnOpenProModalOpen"" HeaderText=""OnOpen"" ShowCloseButton OnOpen=""() => onOpenTextFieldRef.FocusAsync()"">
    <div style=""padding:1rem; max-width:40rem"">
        The following text field will be focused on open:
        <br /><br />
        <BitTextField @ref=""onOpenTextFieldRef"" />
    </div>
</BitProModal>


<BitButton OnClick=""() => isOnDismissProModalOpen = true"">Open OnDismiss ProModal</BitButton>
<BitTextField @ref=""onDismissTextFieldRef"" Placeholder=""This will be focused on dismiss..."" />
<BitProModal @bind-IsOpen=""isOnDismissProModalOpen"" HeaderText=""OnDismiss"" ShowCloseButton OnDismiss=""() => onDismissTextFieldRef.FocusAsync()"">
    <div style=""padding:1rem; max-width:40rem"">Lorem ipsum...</div>
</BitProModal>";
    private readonly string example8CsharpCode = @"
private bool isOnOpenProModalOpen;
private bool isOnDismissProModalOpen;
private BitTextField onOpenTextFieldRef = default!;
private BitTextField onDismissTextFieldRef = default!;";

    private readonly string example9RazorCode = @"
<style>
    .custom-class {
        border: 0.5rem solid tomato;
        background-color: darkgoldenrod;
    }

    .custom-root {
        border: 0.25rem solid #0054C6;
    }

    .custom-overlay {
        background-color: #ffbd5a66;
    }

    .custom-content {
        margin: 1rem;
        box-shadow: 0 0 10rem purple;
    }
</style>


<BitButton OnClick=""() => isOpenStyle = true"">Open styled ProModal</BitButton>
<BitProModal @bind-IsOpen=""isOpenStyle"" HeaderText=""Style"" ShowCloseButton Style=""box-shadow:inset 0 0 1.5rem 1.5rem palevioletred;"">
    <div style=""padding:1rem; max-width:40rem"">BitProModal with custom style.</div>
</BitProModal>

<BitButton OnClick=""() => isOpenClass = true"">Open classed ProModal</BitButton>
<BitProModal @bind-IsOpen=""isOpenClass"" HeaderText=""Class"" ShowCloseButton Class=""custom-class"">
    <div style=""padding:1rem; max-width:40rem"">BitProModal with custom class.</div>
</BitProModal>


<BitButton OnClick=""() => isOpenStyles = true"">Open ProModal Styles</BitButton>
<BitProModal @bind-IsOpen=""isOpenStyles""
             HeaderText=""Styles"" ShowCloseButton
             Styles=""@(new() { Overlay = ""background-color:#4776f433;"",
                               Content = ""box-shadow: 0 0 1rem tomato;"" })"">
    <div style=""padding:1rem; max-width:40rem"">BitProModal with Styles.</div>
</BitProModal>

<BitButton OnClick=""() => isOpenClasses = true"">Open ProModal Classes</BitButton>
<BitProModal @bind-IsOpen=""isOpenClasses""
             HeaderText=""Classes"" ShowCloseButton
             FooterText=""This is a footer text!""
             Classes=""@(new() { Root = ""custom-root"",
                                Overlay = ""custom-overlay"",
                                Content = ""custom-content"",
                                HeaderContainer = ""custom-header-container"",
                                Header = ""custom-header"",
                                Body = ""custom-body"",
                                Footer = ""custom-footer"" })"">
    <div style=""padding:1rem; max-width:40rem"">BitProModal with Classes.</div>
</BitProModal>";
    private readonly string example9CsharpCode = @"
private bool isOpenStyle;
private bool isOpenClass;
private bool isOpenStyles;
private bool isOpenClasses;";

    private readonly string example10RazorCode = @"
<link rel=""stylesheet"" href=""https://cdnjs.cloudflare.com/ajax/libs/font-awesome/7.0.1/css/all.min.css"" />
<BitButton OnClick=""() => isExternalIconProModalOpen = true"">Open ProModal</BitButton>
<BitProModal @bind-IsOpen=""isExternalIconProModalOpen""
             ShowCloseButton
             HeaderText=""External Close Icon""
             CloseIcon=""@BitIconInfo.Fa(""solid xmark"")"">
    <div style=""padding:1rem; max-width:40rem"">Lorem ipsum...</div>
</BitProModal>";
    private readonly string example10CsharpCode = @"
private bool isExternalIconProModalOpen;";

    private readonly string example11RazorCode = @"
<BitButton Dir=""BitDir.Rtl"" OnClick=""() => isOpenRtl = true"">باز کردن مُدال</BitButton>
<BitProModal Dir=""BitDir.Rtl"" @bind-IsOpen=""isOpenRtl"" HeaderText=""مدال راست به چپ"" ShowCloseButton>
    <div style=""padding:1rem; max-width:40rem"">
        لورم ایپسوم متن ساختگی با تولید سادگی نامفهوم از صنعت چاپ و با استفاده از طراحان گرافیک است.
    </div>
</BitProModal>";
    private readonly string example11CsharpCode = @"
private bool isOpenRtl;";
}
