namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Extras.ProModal;

public partial class BitProModalDemo
{
    [CascadingParameter(Name = BitAppShell.Container)]
    private ElementReference? appShellContainer { get; set; }

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
            Description = "When enabled, prevents the Modal from being light dismissed by clicking outside the Modal (on the overlay).",
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
            Name = "CloseButtonTitle",
            Type = "string",
            DefaultValue = "Close",
            Description = "The title (and aria-label) of the close button for accessibility and localization.",
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
            Name = "ScrollerElement",
            Type = "ElementReference?",
            DefaultValue = "null",
            Description = "Set the element reference for which the Modal disables its scroll if applicable.",
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
            Name = "SubtitleAriaId",
            Type = "string?",
            DefaultValue = "null",
            Description = "ARIA id for the subtitle of the Modal, if any.",
        },
        new()
        {
            Name = "Styles",
            Type = "BitProModalClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS styles for different parts of the BitProModal component.",
            Href = "#class-styles",
            LinkType = LinkType.Link,
        },
        new()
        {
            Name = "TitleAriaId",
            Type = "string?",
            DefaultValue = "null",
            Description = "ARIA id for the title of the Modal, if any.",
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
    <div style=""max-width:40rem"">
        In the beginning, there is silence a blank canvas yearning to be filled, a quiet space where creativity waits
        to awaken. These words are temporary, standing in place of ideas yet to come, a glimpse into the infinite
        possibilities that lie ahead. Think of this text as a bridge, connecting the empty spaces of now with the
        vibrant narratives of tomorrow. It whispers of the stories waiting to be told, of the thoughts yet to be
        shaped into meaning, and the emotions ready to resonate with every reader.
        <br />
        In this space, potential reigns supreme. It is a moment suspended in time, where imagination dances freely and
        each word has the power to transform into something extraordinary. Here lies the start of something new—an
        opportunity to craft, inspire, and create. Whether it's a tale of adventure, a reflection of truth, or an
        idea that sparks change, these lines are yours to fill, to shape, and to make uniquely yours. The journey
        begins here, in this quiet moment where everything is possible.
    </div>
</BitProModal>";
    private readonly string example1CsharpCode = @"
private bool isBasicProModalOpen;";

    private readonly string example2RazorCode = @"
<BitButton OnClick=""() => isProModalWithHeaderTextOpen = true"">Open ProModal with HeaderText</BitButton>

<BitProModal @bind-IsOpen=""isProModalWithHeaderTextOpen"" 
             HeaderText=""BitProModal with HeaderText"">
    <div style=""max-width:40rem"">
        Lorem ipsum dolor sit amet, consectetur adipiscing elit. Maecenas lorem nulla, malesuada ut sagittis sit
        amet, vulputate in leo. Maecenas vulputate congue sapien eu tincidunt. Etiam eu sem turpis.
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
        <div style=""max-width:40rem"">
            Lorem ipsum dolor sit amet, consectetur adipiscing elit. Maecenas lorem nulla, malesuada ut sagittis sit
            amet, vulputate in leo. Maecenas vulputate congue sapien eu tincidunt. Etiam eu sem turpis.
        </div>
    </Body>
</BitProModal>


<BitButton OnClick=""() => isProModalWithFooterTextOpen = true"">Open ProModal with FooterText</BitButton>

<BitProModal @bind-IsOpen=""isProModalWithFooterTextOpen"" 
             FooterText=""BitProModal with FooterText"">
    <div style=""max-width:40rem"">
        Lorem ipsum dolor sit amet, consectetur adipiscing elit. Maecenas lorem nulla, malesuada ut sagittis sit
        amet, vulputate in leo. Maecenas vulputate congue sapien eu tincidunt. Etiam eu sem turpis.
    </div>
</BitProModal>

<BitButton OnClick=""() => isProModalWithFooterOpen = true"">Open ProModal with Footer</BitButton>
<BitProModal @bind-IsOpen=""isProModalWithFooterOpen"">
    <Body>
        <div style=""max-width:40rem"">
            Lorem ipsum dolor sit amet, consectetur adipiscing elit. Maecenas lorem nulla, malesuada ut sagittis sit
            amet, vulputate in leo. Maecenas vulputate congue sapien eu tincidunt. Etiam eu sem turpis.
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

<BitProModal @ref=""bitProModalRef"" 
             ShowCloseButton
             HeaderText=""ShowCloseButton"">
    <div style=""max-width:40rem"">
        Lorem ipsum dolor sit amet, consectetur adipiscing elit. Maecenas lorem nulla, malesuada ut sagittis sit
        amet, vulputate in leo. Maecenas vulputate congue sapien eu tincidunt.
    </div>
</BitProModal>



<BitButton OnClick=""() => isBlockingProModalOpen = true"">Open ProModal with Blocking</BitButton>
<BitProModal @bind-IsOpen=""isBlockingProModalOpen""
             Blocking
             ShowCloseButton
             HeaderText=""Blocking"">
    <div style=""max-width:40rem"">
        Lorem ipsum dolor sit amet, consectetur adipiscing elit. Maecenas lorem nulla, malesuada ut sagittis sit
        amet, vulputate in leo. Maecenas vulputate congue sapien eu tincidunt.
    </div>
</BitProModal>


<BitButton OnClick=""() => isModelessProModalOpen = !isModelessProModalOpen"">Toggle ProModal with Modeless</BitButton>

<BitProModal @bind-IsOpen=""isModelessProModalOpen""
             Modeless
             ShowCloseButton
             HeaderText=""Modeless"">
    <div style=""max-width:40rem"">
        Lorem ipsum dolor sit amet, consectetur adipiscing elit. Maecenas lorem nulla, malesuada ut sagittis sit
        amet, vulputate in leo. Maecenas vulputate congue sapien eu tincidunt.
    </div>
</BitProModal>


<BitButton OnClick=""() => isModeFullProModalOpen = true"">Open ProModal with ModeFull</BitButton>

<BitProModal @bind-IsOpen=""isModeFullProModalOpen""
             ModeFull
             ShowCloseButton
             HeaderText=""ModeFull"">
    <div style=""max-width:40rem"">
        Lorem ipsum dolor sit amet, consectetur adipiscing elit. Maecenas lorem nulla, malesuada ut sagittis sit
        amet, vulputate in leo. Maecenas vulputate congue sapien eu tincidunt.
    </div>
</BitProModal>


<BitButton OnClick=""() => isAutoToggleScrollProModalOpen = true"">Open ProModal with AutoToggleScroll</BitButton>

<BitProModal @bind-IsOpen=""isAutoToggleScrollProModalOpen""
             ShowCloseButton
             AutoToggleScroll
             HeaderText=""AutoToggleScroll""
             ScrollerElement=""appShellContainer"">
    <div style=""max-width:40rem"">
        Lorem ipsum dolor sit amet, consectetur adipiscing elit. Maecenas lorem nulla, malesuada ut sagittis sit
        amet, vulputate in leo. Maecenas vulputate congue sapien eu tincidunt.
    </div>
</BitProModal>


<BitButton OnClick=""() => isNoBorderProModalOpen = true"">Open ProModal with NoBorder</BitButton>

<BitProModal @bind-IsOpen=""isNoBorderProModalOpen""
             NoBorder
             ShowCloseButton
             HeaderText=""NoBorder"">
    <div style=""max-width:40rem"">
        Lorem ipsum dolor sit amet, consectetur adipiscing elit. Maecenas lorem nulla, malesuada ut sagittis sit
        amet, vulputate in leo. Maecenas vulputate congue sapien eu tincidunt.
    </div>
</BitProModal>";
    private readonly string example3CsharpCode = @"
private bool isBlockingProModalOpen;
private bool isModelessProModalOpen;
private bool isModeFullProModalOpen;
private bool isAutoToggleScrollProModalOpen;
private bool isNoBorderProModalOpen;
private BitProModal bitProModalRef = default!;
private ElementReference appShellContainer = default!;";

    private readonly string example4RazorCode = @"
<BitButton OnClick=""() => isOpenFullSize = true"">Open ProModal with FullSize</BitButton>

<BitProModal @bind-IsOpen=""isOpenFullSize""
             ShowCloseButton
             FullSize=""isFullSize""
             HeaderText=""FullSize ProModal"">
    <div style=""max-width:40rem"">
        Lorem ipsum dolor sit amet, consectetur adipiscing elit. Maecenas lorem nulla, malesuada ut sagittis sit
        amet, vulputate in leo. Maecenas vulputate congue sapien eu tincidunt. Etiam eu sem turpis.
        <hr />
        <BitToggleButton @bind-IsChecked=""isFullSize"" OnText=""Restore"" OffText=""FullSize"" />
    </div>
</BitProModal>


<BitButton OnClick=""() => isOpenFullWidth = true"">Open ProModal with FullWidth</BitButton>

<BitProModal @bind-IsOpen=""isOpenFullWidth""
             ShowCloseButton
             FullWidth=""isFullWidth""
             HeaderText=""FullWidth ProModal"">
    <div style=""max-width:40rem"">
        Lorem ipsum dolor sit amet, consectetur adipiscing elit. Maecenas lorem nulla, malesuada ut sagittis sit
        amet, vulputate in leo. Maecenas vulputate congue sapien eu tincidunt. Etiam eu sem turpis.
        <hr />
        <BitToggleButton @bind-IsChecked=""isFullWidth"" OnText=""Restore"" OffText=""FullWidth"" />
    </div>
</BitProModal>


<BitButton OnClick=""() => isOpenFullHeight = true"">Open ProModal with FullHeight</BitButton>

<BitProModal @bind-IsOpen=""isOpenFullHeight""
             ShowCloseButton
             FullHeight=""isFullHeight""
             HeaderText=""FullHeight ProModal"">
    <div style=""max-width:40rem"">
        Lorem ipsum dolor sit amet, consectetur adipiscing elit. Maecenas lorem nulla, malesuada ut sagittis sit
        amet, vulputate in leo. Maecenas vulputate congue sapien eu tincidunt. Etiam eu sem turpis.
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
<BitButton OnClick=""() => isOpenAbsolutePosition = true"">Open ProModal</BitButton>
<BitButton OnClick=""() => isOpenScrollerSelector = true"">Open ProModal (AutoToggleScroll & ScrollerSelector)</BitButton>

<div class=""relative-container"" id=""modal-scroller"">
    <BitProModal @bind-IsOpen=""isOpenAbsolutePosition""
                 ShowCloseButton
                 AbsolutePosition
                 HeaderText=""AbsolutePosition"">
        <div style=""max-width:40rem"">
            Lorem ipsum dolor sit amet, consectetur adipiscing elit. Maecenas lorem nulla, malesuada ut sagittis sit
            amet, vulputate in leo.
        </div>
    </BitProModal>

    <BitProModal @bind-IsOpen=""isOpenScrollerSelector""
                 ShowCloseButton
                 AbsolutePosition
                 AutoToggleScroll
                 ScrollerSelector=""#modal-scroller""
                 HeaderText=""AbsolutePosition with AutoToggleScroll and ScrollerSelector"">
        <div style=""max-width:40rem"">
            Lorem ipsum dolor sit amet, consectetur adipiscing elit. Maecenas lorem nulla, malesuada ut sagittis sit
            amet, vulputate in leo.
        </div>
    </BitProModal>

    <div>
        Lorem ipsum dolor sit amet, consectetur adipiscing elit. Maecenas lorem nulla, malesuada ut sagittis sit
        amet, vulputate in leo. Maecenas vulputate congue sapien eu tincidunt. Etiam eu sem turpis. Fusce tempor
        sagittis nunc, ut interdum ipsum vestibulum non. Proin dolor elit, aliquam eget tincidunt non, vestibulum ut
        turpis. In hac habitasse platea dictumst. In a odio eget enim porttitor maximus. Aliquam nulla nibh,
        ullamcorper aliquam placerat eu, viverra et dui. Phasellus ex lectus, maximus in mollis ac, luctus vel eros.
        Vivamus ultrices, turpis sed malesuada gravida, eros ipsum venenatis elit, et volutpat eros dui et ante.
        Quisque ultricies mi nec leo ultricies mollis. Vivamus egestas volutpat lacinia. Quisque pharetra eleifend
        efficitur.
        Lorem ipsum dolor sit amet, consectetur adipiscing elit. Maecenas lorem nulla, malesuada ut sagittis sit
        amet, vulputate in leo. Maecenas vulputate congue sapien eu tincidunt. Etiam eu sem turpis. Fusce tempor
        sagittis nunc, ut interdum ipsum vestibulum non. Proin dolor elit, aliquam eget tincidunt non, vestibulum ut
        turpis. In hac habitasse platea dictumst. In a odio eget enim porttitor maximus. Aliquam nulla nibh,
        ullamcorper aliquam placerat eu, viverra et dui. Phasellus ex lectus, maximus in mollis ac, luctus vel eros.
        Lorem ipsum dolor sit amet, consectetur adipiscing elit. Maecenas lorem nulla, malesuada ut sagittis sit
        amet, vulputate in leo. Maecenas vulputate congue sapien eu tincidunt. Etiam eu sem turpis. Fusce tempor
        sagittis nunc, ut interdum ipsum vestibulum non. Proin dolor elit, aliquam eget tincidunt non, vestibulum ut
        turpis. In hac habitasse platea dictumst. In a odio eget enim porttitor maximus. Aliquam nulla nibh,
        ullamcorper aliquam placerat eu, viverra et dui. Phasellus ex lectus, maximus in mollis ac, luctus vel eros.
        Vivamus ultrices, turpis sed malesuada gravida, eros ipsum venenatis elit, et volutpat eros dui et ante.
        Quisque ultricies mi nec leo ultricies mollis. Vivamus egestas volutpat lacinia. Quisque pharetra eleifend
        efficitur.
        Lorem ipsum dolor sit amet, consectetur adipiscing elit. Maecenas lorem nulla, malesuada ut sagittis sit
        amet, vulputate in leo. Maecenas vulputate congue sapien eu tincidunt. Etiam eu sem turpis. Fusce tempor
        sagittis nunc, ut interdum ipsum vestibulum non. Proin dolor elit, aliquam eget tincidunt non, vestibulum ut
        turpis. In hac habitasse platea dictumst. In a odio eget enim porttitor maximus. Aliquam nulla nibh,
        ullamcorper aliquam placerat eu, viverra et dui. Phasellus ex lectus, maximus in mollis ac, luctus vel eros.
        Lorem ipsum dolor sit amet, consectetur adipiscing elit. Maecenas lorem nulla, malesuada ut sagittis sit
        amet, vulputate in leo. Maecenas vulputate congue sapien eu tincidunt. Etiam eu sem turpis. Fusce tempor
        sagittis nunc, ut interdum ipsum vestibulum non. Proin dolor elit, aliquam eget tincidunt non, vestibulum ut
        turpis. In hac habitasse platea dictumst. In a odio eget enim porttitor maximus. Aliquam nulla nibh,
        ullamcorper aliquam placerat eu, viverra et dui. Phasellus ex lectus, maximus in mollis ac, luctus vel eros.
        Vivamus ultrices, turpis sed malesuada gravida, eros ipsum venenatis elit, et volutpat eros dui et ante.
        Quisque ultricies mi nec leo ultricies mollis. Vivamus egestas volutpat lacinia. Quisque pharetra eleifend
        efficitur.
        Lorem ipsum dolor sit amet, consectetur adipiscing elit. Maecenas lorem nulla, malesuada ut sagittis sit
        amet, vulputate in leo. Maecenas vulputate congue sapien eu tincidunt. Etiam eu sem turpis. Fusce tempor
        sagittis nunc, ut interdum ipsum vestibulum non. Proin dolor elit, aliquam eget tincidunt non, vestibulum ut
        turpis. In hac habitasse platea dictumst. In a odio eget enim porttitor maximus. Aliquam nulla nibh,
        ullamcorper aliquam placerat eu, viverra et dui. Phasellus ex lectus, maximus in mollis ac, luctus vel eros.
    </div>
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
    <Header>
        Position: @position
    </Header>
    <Body>
        <div style=""max-width:40rem"">
            Lorem ipsum dolor sit amet, consectetur adipiscing elit. Maecenas lorem nulla, malesuada ut sagittis sit
            amet, vulputate in leo. Maecenas vulputate congue sapien eu tincidunt. Etiam eu sem turpis.
        </div>
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

<BitProModal @bind-IsOpen=""isOpenDraggable""
             Draggable
             ShowCloseButton
             HeaderText=""Draggable"">
    <div style=""max-width:40rem"">
        Lorem ipsum dolor sit amet, consectetur adipiscing elit. Maecenas lorem nulla, malesuada ut sagittis sit
        amet, vulputate in leo. Maecenas vulputate congue sapien eu tincidunt. Etiam eu sem turpis.
    </div>
</BitProModal>


<BitButton OnClick=""() => isOpenDraggableSelector = true"">Open ProModal</BitButton>

<BitProModal @bind-IsOpen=""isOpenDraggableSelector""
             Draggable
             ShowCloseButton
             DragElementSelector=""#modal-drag-element"">
    <div style=""max-width:40rem"">
        <h3 id=""modal-drag-element"" style=""color:white; background:brown; padding:1rem"">
            Draggable with DragElementSelector
        </h3>
        Lorem ipsum dolor sit amet, consectetur adipiscing elit. Maecenas lorem nulla, malesuada ut sagittis sit
        amet, vulputate in leo.
    </div>
</BitProModal>";
    private readonly string example7CsharpCode = @"
private bool isOpenDraggable;
private bool isOpenDraggableSelector;";

    private readonly string example8RazorCode = @"
<BitButton OnClick=""() => isOnOpenProModalOpen = true"">Open OnOpen ProModal</BitButton>

<BitProModal @bind-IsOpen=""isOnOpenProModalOpen""
             ShowCloseButton
             HeaderText=""OnOpen""
             OnOpen=""() => onOpenTextFieldRef.FocusAsync()"">
    <div style=""max-width:40rem"">
        The following text field will be focused on open:
        <br /><br />
        <BitTextField @ref=""onOpenTextFieldRef"" />
    </div>
</BitProModal>


<BitButton OnClick=""() => isOnDismissProModalOpen = true"">Open OnDismiss ProModal</BitButton>
<BitTextField @ref=""onDismissTextFieldRef"" Placeholder=""This will be focused on dismiss..."" />

<BitProModal @bind-IsOpen=""isOnDismissProModalOpen""
             ShowCloseButton
             HeaderText=""OnDismiss""
             OnDismiss=""() => onDismissTextFieldRef.FocusAsync()"">
    <div style=""max-width:40rem"">
        Lorem ipsum dolor sit amet, consectetur adipiscing elit. Maecenas lorem nulla, malesuada ut sagittis sit
        amet, vulputate in leo.
    </div>
</BitProModal>";
    private readonly string example8CsharpCode = @"
private bool isOnOpenProModalOpen;
private bool isOnDismissProModalOpen;
private BitTextField onOpenTextFieldRef = default!;
private BitTextField onDismissTextFieldRef = default!;";

    private readonly string example9RazorCode = @"
<BitButton OnClick=""() => isOpenStyle = true"">Open styled ProModal</BitButton>

<BitProModal @bind-IsOpen=""isOpenStyle""
             ShowCloseButton
             HeaderText=""Style""
             Style=""box-shadow:inset 0 0 1.5rem 1.5rem palevioletred;"">
    <div style=""max-width:40rem"">
        BitProModal with custom style.
    </div>
</BitProModal>


<BitButton OnClick=""() => isOpenClass = true"">Open classed ProModal</BitButton>

<BitProModal @bind-IsOpen=""isOpenClass""
             ShowCloseButton
             HeaderText=""Class""
             Class=""custom-class"">
    <div style=""max-width:40rem"">
        BitProModal with custom class.
    </div>
</BitProModal>


<BitButton OnClick=""() => isOpenStyles = true"">Open ProModal Styles</BitButton>

<BitProModal @bind-IsOpen=""isOpenStyles""
             ShowCloseButton
             HeaderText=""Styles""
             Styles=""@(new()
             {
                 Overlay = ""background-color:#4776f433;"",
                 Content = ""box-shadow: 0 0 1rem tomato;""
             })"">
    <div style=""max-width:40rem"">
        BitProModal with <b>Styles</b> to customize its elements.
    </div>
</BitProModal>


<BitButton OnClick=""() => isOpenClasses = true"">Open ProModal Classes</BitButton>

<BitProModal @bind-IsOpen=""isOpenClasses""
             ShowCloseButton
             HeaderText=""Classes""
             FooterText=""This is a footer text!""
             Classes=""@(new()
             {
                 Root = ""custom-root"",
                 Overlay = ""custom-overlay"",
                 Content = ""custom-content"",
                 HeaderContainer = ""custom-header-container"",
                 Header = ""custom-header"",
                 Body = ""custom-body"",
                 Footer = ""custom-footer""
             })"">
    <div style=""max-width:40rem"">
        BitProModal with <b>Classes</b> to customize its elements.
    </div>
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
    <div style=""max-width:40rem"">
        Lorem ipsum dolor sit amet, consectetur adipiscing elit. Maecenas lorem nulla, malesuada ut sagittis sit
        amet, vulputate in leo.
    </div>
</BitProModal>";
    private readonly string example10CsharpCode = @"
private bool isExternalIconProModalOpen;";

    private readonly string example11RazorCode = @"
<BitButton Dir=""BitDir.Rtl"" OnClick=""() => isOpenRtl = true"">باز کردن مُدال</BitButton>

<BitProModal @bind-IsOpen=""isOpenRtl""
             ShowCloseButton
             Dir=""BitDir.Rtl""
             HeaderText=""مدال راست به چپ"">
    <div style=""max-width:40rem"">
        لورم ایپسوم متن ساختگی با تولید سادگی نامفهوم از صنعت چاپ و با استفاده از طراحان گرافیک است.
        چاپگرها و متون بلکه روزنامه و مجله در ستون و سطرآنچنان که لازم است و برای شرایط فعلی تکنولوژی مورد نیاز و کاربردهای متنوع با هدف بهبود ابزارهای کاربردی می باشد.
    </div>
</BitProModal>";
    private readonly string example11CsharpCode = @"
private bool isOpenRtl;";

}
