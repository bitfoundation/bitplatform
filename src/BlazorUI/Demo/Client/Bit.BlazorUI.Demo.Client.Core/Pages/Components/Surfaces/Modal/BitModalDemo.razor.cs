namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Surfaces.Modal;

public partial class BitModalDemo
{
    private readonly List<ComponentParameter> componentParameters =
    [
        new()
        {
            Name = "AutoToggleScroll",
            Type = "bool",
            DefaultValue = "true",
            Description = "Enables the auto scrollbar toggle behavior of the Modal.",
        },
        new()
        {
            Name = "AbsolutePosition",
            Type = "bool",
            DefaultValue = "false",
            Description = "When true, the Modal will be positioned absolute instead of fixed.",
        },
        new()
        {
            Name = "ChildContent",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "The content of Modal, It can be Any custom tag or a text.",
        },
        new()
        {
            Name = "Classes",
            Type = "BitModalClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS classes for different parts of the BitModal component.",
            Href = "#modal-class-styles",
            LinkType = LinkType.Link
        },
        new()
        {
            Name = "DragElementSelector",
            Type = "string?",
            DefaultValue = "null",
            Description = "The CSS selector of the drag element. by default the Modal container is the drag element.",
        },
        new()
        {
            Name = "IsAlert",
            Type = "bool?",
            DefaultValue = "null",
            Description = "Determines the ARIA role of the dialog (alertdialog/dialog). If this is set, it will override the ARIA role determined by IsBlocking and IsModeless.",
        },
        new()
        {
            Name = "IsBlocking",
            Type = "bool",
            DefaultValue = "false",
            Description = "Whether the dialog can be light dismissed by clicking outside the dialog (on the overlay).",
        },
        new()
        {
            Name = "IsDraggable",
            Type = "bool",
            DefaultValue = "false",
            Description = "Whether the Modal can be dragged around.",
        },
        new()
        {
            Name = "IsModeless",
            Type = "bool",
            DefaultValue = "false",
            Description = "Whether the dialog should be modeless (e.g. not dismiss when focusing/clicking outside of the dialog). if true: IsBlocking is ignored, there will be no overlay.",
        },
        new()
        {
            Name = "IsOpen",
            Type = "bool",
            DefaultValue = "false",
            Description = "Whether the dialog is displayed.",
        },
        new()
        {
            Name = "OnDismiss",
            Type = "EventCallback<MouseEventArgs>",
            Description = "A callback function for when the Modal is dismissed.",
        },
        new()
        {
            Name = "Position",
            Type = "BitModalPosition",
            LinkType = LinkType.Link,
            Href = "#component-position-enum",
            DefaultValue = "BitModalPosition.Center",
            Description = "Position of the modal on the screen.",
        },
        new()
        {
            Name = "ScrollerSelector",
            Type = "string",
            DefaultValue = "body",
            Description = "Set the element selector for which the Modal disables its scroll if applicable.",
        },
        new()
        {
            Name = "Styles",
            Type = "BitModalClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS styles for different parts of the BitModal component.",
            Href = "#modal-class-styles",
            LinkType = LinkType.Link
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
            Id = "modal-class-styles",
            Title = "BitModalClassStyles",
            Parameters =
            [
               new()
               {
                   Name = "Root",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the root element of the BitModal."
               },
               new()
               {
                   Name = "Container",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the main container of the BitModal."
               },
               new()
               {
                   Name = "Overlay",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the overlay of the BitModal."
               },
               new()
               {
                   Name = "Content",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the content of the BitModal."
               },
               new()
               {
                   Name = "ScrollContent",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the scroll content of the BitModal."
               }
            ]
        }
    ];

    private readonly List<ComponentSubEnum> componentSubEnums =
    [
        new()
        {
            Id = "component-position-enum",
            Name = "BitModalPosition",
            Description = "",
            Items =
            [
                new() { Name = "Center", Value = "0" },
                new() { Name = "TopLeft", Value = "1" },
                new() { Name = "TopCenter", Value = "2" },
                new() { Name = "TopRight", Value = "3" },
                new() { Name = "CenterLeft", Value = "4" },
                new() { Name = "CenterRight", Value = "5" },
                new() { Name = "BottomLeft", Value = "6" },
                new() { Name = "BottomCenter", Value = "7" },
                new() { Name = "BottomRight", Value = "8" }
            ]
        }
    ];



    private readonly string example1RazorCode = @"
<style>
    .modal-header {
        display: flex;
        align-items: center;
        font-size: 24px;
        font-weight: 600;
        border-top: 4px solid #0054C6;
        justify-content: space-between;
        padding: 12px 12px 14px 24px;
    }

    .modal-body {
        padding: 0 24px 24px;
        overflow-y: hidden;
        line-height: 20px;
        max-width: 960px;
    }
</style>

<BitButton OnClick=@(() => IsOpen = true)>Open Modal</BitButton>

<BitModal @bind-IsOpen=""IsOpen"">
    <div class=""modal-header"">
        <span>Lorem Ipsum</span>
        <BitButton Variant=""BitVariant.Text"" OnClick=@(() => IsOpen = false) IconName=""@BitIconName.ChromeClose"" Title=""Close"" />
    </div>
    <div class=""modal-body"">
        <p>
            Lorem ipsum dolor sit amet, consectetur adipiscing elit. Maecenas lorem nulla, malesuada ut sagittis sit
            amet, vulputate in leo. Maecenas vulputate congue sapien eu tincidunt. Etiam eu sem turpis. Fusce tempor
            sagittis nunc, ut interdum ipsum vestibulum non. Proin dolor elit, aliquam eget tincidunt non, vestibulum ut
            turpis. In hac habitasse platea dictumst. In a odio eget enim porttitor maximus. Aliquam nulla nibh,
            ullamcorper aliquam placerat eu, viverra et dui. Phasellus ex lectus, maximus in mollis ac, luctus vel eros.
            Vivamus ultrices, turpis sed malesuada gravida, eros ipsum venenatis elit, et volutpat eros dui et ante.
            Quisque ultricies mi nec leo ultricies mollis. Vivamus egestas volutpat lacinia. Quisque pharetra eleifend
            efficitur.
        </p>
        <p>
            Mauris at nunc eget lectus lobortis facilisis et eget magna. Vestibulum venenatis augue sapien, rhoncus
            faucibus magna semper eget. Proin rutrum libero sagittis sapien aliquet auctor. Suspendisse tristique a
            magna at facilisis. Duis rhoncus feugiat magna in rutrum. Suspendisse semper, dolor et vestibulum lacinia,
            nunc felis malesuada ex, nec hendrerit justo ex et massa. Quisque quis mollis nulla. Nam commodo est ornare,
            rhoncus odio eu, pharetra tellus. Nunc sed velit mi.
        </p>
        <p>
            Sed condimentum ultricies turpis convallis pharetra. Sed sagittis quam pharetra luctus porttitor. Cras vel
            consequat lectus. Sed nec fringilla urna, a aliquet libero. Aenean sed nisl purus. Vivamus vulputate felis
            et odio efficitur suscipit. Ut volutpat dictum lectus, ac rutrum massa accumsan at. Sed pharetra auctor
            finibus. In augue libero, commodo vitae nisi non, sagittis convallis ante. Phasellus malesuada eleifend
            mollis. Curabitur ultricies leo ac metus venenatis elementum.
        </p>
    </div>
</BitModal>";
    private readonly string example1CsharpCode = @"
private bool IsOpen = false;";

    private readonly string example2RazorCode = @"
<style>
    .modal-header {
        display: flex;
        align-items: center;
        font-size: 24px;
        font-weight: 600;
        border-top: 4px solid #0054C6;
        justify-content: space-between;
        padding: 12px 12px 14px 24px;
    }

    .modal-body {
        padding: 0 24px 24px;
        overflow-y: hidden;
        line-height: 20px;
        max-width: 960px;
    }
</style>

<BitButton OnClick=@(() => IsOpen1 = true)>Open Modal (IsBlocking = true)</BitButton>
<BitButton OnClick=@(() => IsOpen2 = true)>Open Modal (AutoToggleScroll = false)</BitButton>

<BitModal @bind-IsOpen=""IsOpen1"" IsBlocking=""true"">
    <div class=""modal-header"">
        <span>IsBlocking = true</span>
        <BitButton Variant=""BitVariant.Text"" OnClick=@(()=> IsOpen1 = false) IconName=""@BitIconName.ChromeClose"" Title=""Close"" />
    </div>
    <div class=""modal-body"">
        <p>
            Lorem ipsum dolor sit amet, consectetur adipiscing elit. Maecenas lorem nulla, malesuada ut sagittis sit
            amet, vulputate in leo. Maecenas vulputate congue sapien eu tincidunt. Etiam eu sem turpis. Fusce tempor
            sagittis nunc, ut interdum ipsum vestibulum non. Proin dolor elit, aliquam eget tincidunt non, vestibulum ut
            turpis. In hac habitasse platea dictumst. In a odio eget enim porttitor maximus. Aliquam nulla nibh,
            ullamcorper aliquam placerat eu, viverra et dui. Phasellus ex lectus, maximus in mollis ac, luctus vel eros.
            Vivamus ultrices, turpis sed malesuada gravida, eros ipsum venenatis elit, et volutpat eros dui et ante.
            Quisque ultricies mi nec leo ultricies mollis. Vivamus egestas volutpat lacinia. Quisque pharetra eleifend
            efficitur.
        </p>
    </div>
</BitModal>

<BitModal @bind-IsOpen=""IsOpen2"" AutoToggleScroll=""false"">
    <div class=""modal-header"">
        <span>AutoToggleScroll = false</span>
        <BitButton Variant=""BitVariant.Text"" OnClick=@(()=> IsOpen2 = false) IconName=""@BitIconName.ChromeClose"" Title=""Close"" />
    </div>
    <div class=""modal-body"">
        <p>
            Lorem ipsum dolor sit amet, consectetur adipiscing elit. Maecenas lorem nulla, malesuada ut sagittis sit
            amet, vulputate in leo. Maecenas vulputate congue sapien eu tincidunt. Etiam eu sem turpis. Fusce tempor
            sagittis nunc, ut interdum ipsum vestibulum non. Proin dolor elit, aliquam eget tincidunt non, vestibulum ut
            turpis. In hac habitasse platea dictumst. In a odio eget enim porttitor maximus. Aliquam nulla nibh,
            ullamcorper aliquam placerat eu, viverra et dui. Phasellus ex lectus, maximus in mollis ac, luctus vel eros.
            Vivamus ultrices, turpis sed malesuada gravida, eros ipsum venenatis elit, et volutpat eros dui et ante.
            Quisque ultricies mi nec leo ultricies mollis. Vivamus egestas volutpat lacinia. Quisque pharetra eleifend
            efficitur.
        </p>
    </div>
</BitModal>";
    private readonly string example2CsharpCode = @"
private bool IsOpen1 = false;
private bool IsOpen2 = false;";

    private readonly string example3RazorCode = @"
<style>
    .relative-container {
        margin-top: 1rem;
        position: relative;
        width: 100%;
        height: 400px;
        border: 2px lightgreen solid;
        background-color: #eee;
        overflow: auto;
    }

    .modal-header {
        display: flex;
        align-items: center;
        font-size: 24px;
        font-weight: 600;
        border-top: 4px solid #0054C6;
        justify-content: space-between;
        padding: 12px 12px 14px 24px;
    }

    .modal-body {
        padding: 0 24px 24px;
        overflow-y: hidden;
        line-height: 20px;
        max-width: 960px;
    }
</style>

<BitButton OnClick=@(() => IsOpen3 = true)>Open Modal (AbsolutePosition = true)</BitButton>
<BitButton OnClick=@(() => IsOpen4 = true)>Open Modal (ScrollerSelector)</BitButton>

<div class=""relative-container"">
    <BitModal @bind-IsOpen=""IsOpen3"" AbsolutePosition=""true"" AutoToggleScroll=""false"" IsModeless=""true"">
        <div class=""modal-header"">
            <span>AbsolutePosition=true & IsModeless=true</span>
            <BitButton Variant=""BitVariant.Text"" OnClick=@(()=> IsOpen3 = false) IconName=""@BitIconName.ChromeClose"" Title=""Close"" />
        </div>
        <div class=""modal-body"">
            <p>
                Lorem ipsum dolor sit amet, consectetur adipiscing elit. Maecenas lorem nulla, malesuada ut sagittis sit
                amet, vulputate in leo. Maecenas vulputate congue sapien eu tincidunt. Etiam eu sem turpis. Fusce tempor
                sagittis nunc, ut interdum ipsum vestibulum non. Proin dolor elit, aliquam eget tincidunt non, vestibulum ut
                turpis. In hac habitasse platea dictumst. In a odio eget enim porttitor maximus. Aliquam nulla nibh,
                ullamcorper aliquam placerat eu, viverra et dui. Phasellus ex lectus, maximus in mollis ac, luctus vel eros.
                Vivamus ultrices, turpis sed malesuada gravida, eros ipsum venenatis elit, et volutpat eros dui et ante.
                Quisque ultricies mi nec leo ultricies mollis. Vivamus egestas volutpat lacinia. Quisque pharetra eleifend
                efficitur.
            </p>
        </div>
    </BitModal>
    <BitModal @bind-IsOpen=""IsOpen4"" AbsolutePosition=""true"" ScrollerSelector="".relative-container"">
        <div class=""modal-header"">
            <span>ScrollerSelector</span>
            <BitButton Variant=""BitVariant.Text"" OnClick=@(()=> IsOpen4 = false) IconName=""@BitIconName.ChromeClose"" Title=""Close"" />
        </div>
        <div class=""modal-body"">
            <p>
                Lorem ipsum dolor sit amet, consectetur adipiscing elit. Maecenas lorem nulla, malesuada ut sagittis sit
                amet, vulputate in leo. Maecenas vulputate congue sapien eu tincidunt. Etiam eu sem turpis. Fusce tempor
                sagittis nunc, ut interdum ipsum vestibulum non. Proin dolor elit, aliquam eget tincidunt non, vestibulum ut
                turpis. In hac habitasse platea dictumst. In a odio eget enim porttitor maximus. Aliquam nulla nibh,
                ullamcorper aliquam placerat eu, viverra et dui. Phasellus ex lectus, maximus in mollis ac, luctus vel eros.
                Vivamus ultrices, turpis sed malesuada gravida, eros ipsum venenatis elit, et volutpat eros dui et ante.
                Quisque ultricies mi nec leo ultricies mollis. Vivamus egestas volutpat lacinia. Quisque pharetra eleifend
                efficitur.
            </p>
        </div>
    </BitModal>

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
    Vivamus ultrices, turpis sed malesuada gravida, eros ipsum venenatis elit, et volutpat eros dui et ante.
    Quisque ultricies mi nec leo ultricies mollis. Vivamus egestas volutpat lacinia. Quisque pharetra eleifend
    efficitur.

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
    Vivamus ultrices, turpis sed malesuada gravida, eros ipsum venenatis elit, et volutpat eros dui et ante.
    Quisque ultricies mi nec leo ultricies mollis. Vivamus egestas volutpat lacinia. Quisque pharetra eleifend
    efficitur.
</div>";
    private readonly string example3CsharpCode = @"
private bool IsOpenInPosition = false;
private BitModalPosition position;

private void OpenModalInPosition(BitModalPosition positionValue)
{
    IsOpenInPosition = true;
    position = positionValue;
}";

    private readonly string example4RazorCode = @"
<style>
    .modal-header {
        display: flex;
        align-items: center;
        font-size: 24px;
        font-weight: 600;
        border-top: 4px solid #0054C6;
        justify-content: space-between;
        padding: 12px 12px 14px 24px;
    }

    .modal-body {
        padding: 0 24px 24px;
        overflow-y: hidden;
        line-height: 20px;
        max-width: 960px;
    }
</style>

<BitButton OnClick=""() => OpenModalInPosition(BitModalPosition.TopLeft)"">Top Left</BitButton>
<BitButton OnClick=""() => OpenModalInPosition(BitModalPosition.TopRight)"">Top Right</BitButton>
<BitButton OnClick=""() => OpenModalInPosition(BitModalPosition.BottomLeft)"">Bottom Left</BitButton>
<BitButton OnClick=""() => OpenModalInPosition(BitModalPosition.BottomRight)"">Bottom Right</BitButton>

<BitModal @bind-IsOpen=""IsOpenInPosition"" Position=""position"">
    <div class=""modal-header"">
        <span>Modal positioning</span>
        <BitButton Variant=""BitVariant.Text"" OnClick=@(() => IsOpenInPosition = false) IconName=""@BitIconName.ChromeClose"" Title=""Close"" />
    </div>
    <div class=""modal-body"">
        BitModal with custom positioning. Lorem ipsum dolor sit amet, consectetur adipiscing elit.
    </div>
</BitModal>";
    private readonly string example4CsharpCode = @"
private bool IsOpenInPosition = false;
private BitModalPosition position;

private void OpenModalInPosition(BitModalPosition positionValue)
{
    IsOpenInPosition = true;
    position = positionValue;
}";

    private readonly string example5RazorCode = @"
<style>
    .modal-header {
        display: flex;
        align-items: center;
        font-size: 24px;
        font-weight: 600;
        border-top: 4px solid #0054C6;
        justify-content: space-between;
        padding: 12px 12px 14px 24px;
    }

    .modal-body {
        padding: 0 24px 24px;
        overflow-y: hidden;
        line-height: 20px;
        max-width: 960px;
    }
</style>

<BitToggle Label=""Is Draggable?"" @bind-Value=""IsDraggable"" />
<BitButton OnClick=""() => IsOpen5 = true"">Open Modal</BitButton>
<BitModal @bind-IsOpen=""IsOpen5"" IsDraggable=""IsDraggable"">
    <div class=""modal-header"">
        <span>Draggble Modal</span>
        <BitButton Variant=""BitVariant.Text"" OnClick=@(() => IsOpen5 = false) IconName=""@BitIconName.ChromeClose"" Title=""Close"" />
    </div>
    <div class=""modal-body"">
        <p>
            Lorem ipsum dolor sit amet, consectetur adipiscing elit. Maecenas lorem nulla, malesuada ut sagittis sit
            amet, vulputate in leo. Maecenas vulputate congue sapien eu tincidunt. Etiam eu sem turpis. Fusce tempor
            sagittis nunc, ut interdum ipsum vestibulum non. Proin dolor elit, aliquam eget tincidunt non, vestibulum ut
            turpis. In hac habitasse platea dictumst. In a odio eget enim porttitor maximus. Aliquam nulla nibh,
            ullamcorper aliquam placerat eu, viverra et dui. Phasellus ex lectus, maximus in mollis ac, luctus vel eros.
            Vivamus ultrices, turpis sed malesuada gravida, eros ipsum venenatis elit, et volutpat eros dui et ante.
            Quisque ultricies mi nec leo ultricies mollis. Vivamus egestas volutpat lacinia. Quisque pharetra eleifend
            efficitur.
        </p>
    </div>
</BitModal>

<BitButton OnClick=""() => IsOpen6 = true"">Open Modal</BitButton>
<BitModal @bind-IsOpen=""IsOpen6"" IsDraggable=""true"" DragElementSelector="".modal-header-drag"">
    <div class=""modal-header modal-header-drag"">
        <span>Draggble Modal with custom drag element</span>
        <BitButton Variant=""BitVariant.Text"" OnClick=@(() => IsOpen6 = false) IconName=""@BitIconName.ChromeClose"" Title=""Close"" />
    </div>
    <div class=""modal-body"">
        <p>
            Lorem ipsum dolor sit amet, consectetur adipiscing elit. Maecenas lorem nulla, malesuada ut sagittis sit
            amet, vulputate in leo. Maecenas vulputate congue sapien eu tincidunt. Etiam eu sem turpis. Fusce tempor
            sagittis nunc, ut interdum ipsum vestibulum non. Proin dolor elit, aliquam eget tincidunt non, vestibulum ut
            turpis. In hac habitasse platea dictumst. In a odio eget enim porttitor maximus. Aliquam nulla nibh,
            ullamcorper aliquam placerat eu, viverra et dui. Phasellus ex lectus, maximus in mollis ac, luctus vel eros.
            Vivamus ultrices, turpis sed malesuada gravida, eros ipsum venenatis elit, et volutpat eros dui et ante.
            Quisque ultricies mi nec leo ultricies mollis. Vivamus egestas volutpat lacinia. Quisque pharetra eleifend
            efficitur.
        </p>
    </div>
</BitModal>";
    private readonly string example5CsharpCode = @"
private bool IsDraggable = false;
private bool IsOpen5 = false;
private bool IsOpen6 = false;
";

    private readonly string example6RazorCode = @"
<style>
    .modal-header {
        display: flex;
        align-items: center;
        font-size: 24px;
        font-weight: 600;
        border-top: 4px solid #0054C6;
        justify-content: space-between;
        padding: 12px 12px 14px 24px;
    }

    .modal-body {
        padding: 0 24px 24px;
        overflow-y: hidden;
        line-height: 20px;
        max-width: 960px;
    }

    .custom-class {
        border: 0.5rem solid tomato;
        background-color: darkgoldenrod;
    }

    .custom-container {
        border: 0.25rem solid #0054C6;
    }

    .custom-overlay {
        background-color: #ffbd5a66;
    }

    .custom-content {
        margin: 1rem;
        box-shadow: 0 0 10rem purple;
        border-end-end-radius: 1rem;
        border-end-start-radius: 1rem;
    }
</style>


<BitButton OnClick=""() => IsOpen7 = true"">Open styled modal</BitButton>
<BitButton OnClick=""() => IsOpen8 = true"">Open classed modal</BitButton>
<BitButton OnClick=""() => IsOpen9 = true"">Open modal styles</BitButton>
<BitButton OnClick=""() => IsOpen10 = true"">Open modal classes</BitButton>

<BitModal @bind-IsOpen=""IsOpen7"" Style=""box-shadow: inset 0px 0px 1.5rem 1.5rem palevioletred;"">
    <div class=""modal-header"">
        <span>Styled modal</span>
        <BitButton Variant=""BitVariant.Text"" OnClick=""@(() => IsOpen7 = false)"" IconName=""@BitIconName.ChromeClose"" Title=""Close"" />
    </div>
    <div class=""modal-body"">
        <p>
            Lorem ipsum dolor sit amet, consectetur adipiscing elit. Maecenas lorem nulla, malesuada ut sagittis sit
            amet, vulputate in leo. Maecenas vulputate congue sapien eu tincidunt. Etiam eu sem turpis. Fusce tempor
            sagittis nunc, ut interdum ipsum vestibulum non. Proin dolor elit, aliquam eget tincidunt non, vestibulum ut
            turpis. In hac habitasse platea dictumst. In a odio eget enim porttitor maximus. Aliquam nulla nibh,
            ullamcorper aliquam placerat eu, viverra et dui. Phasellus ex lectus, maximus in mollis ac, luctus vel eros.
            Vivamus ultrices, turpis sed malesuada gravida, eros ipsum venenatis elit, et volutpat eros dui et ante.
            Quisque ultricies mi nec leo ultricies mollis. Vivamus egestas volutpat lacinia. Quisque pharetra eleifend
            efficitur.
        </p>
    </div>
</BitModal>

<BitModal @bind-IsOpen=""IsOpen8"" Class=""custom-class"">
    <div class=""modal-header"">
        <span>Classed modal</span>
        <BitButton Variant=""BitVariant.Text"" OnClick=""@(() => IsOpen8 = false)"" IconName=""@BitIconName.ChromeClose"" Title=""Close"" />
    </div>
    <div class=""modal-body"">
        <p>
            Lorem ipsum dolor sit amet, consectetur adipiscing elit. Maecenas lorem nulla, malesuada ut sagittis sit
            amet, vulputate in leo. Maecenas vulputate congue sapien eu tincidunt. Etiam eu sem turpis. Fusce tempor
            sagittis nunc, ut interdum ipsum vestibulum non. Proin dolor elit, aliquam eget tincidunt non, vestibulum ut
            turpis. In hac habitasse platea dictumst. In a odio eget enim porttitor maximus. Aliquam nulla nibh,
            ullamcorper aliquam placerat eu, viverra et dui. Phasellus ex lectus, maximus in mollis ac, luctus vel eros.
            Vivamus ultrices, turpis sed malesuada gravida, eros ipsum venenatis elit, et volutpat eros dui et ante.
            Quisque ultricies mi nec leo ultricies mollis. Vivamus egestas volutpat lacinia. Quisque pharetra eleifend
            efficitur.
        </p>
    </div>
</BitModal>

<BitModal @bind-IsOpen=""IsOpen9"" Styles=""@(new() { Overlay = ""background-color: #4776f433;"", Content = ""box-shadow: 0 0 1rem tomato;"" })"">
    <div class=""modal-header"">
        <span>Modal styles</span>
        <BitButton Variant=""BitVariant.Text"" OnClick=""@(() => IsOpen9 = false)"" IconName=""@BitIconName.ChromeClose"" Title=""Close"" />
    </div>
    <div class=""modal-body"">
        <p>
            Lorem ipsum dolor sit amet, consectetur adipiscing elit. Maecenas lorem nulla, malesuada ut sagittis sit
            amet, vulputate in leo. Maecenas vulputate congue sapien eu tincidunt. Etiam eu sem turpis. Fusce tempor
            sagittis nunc, ut interdum ipsum vestibulum non. Proin dolor elit, aliquam eget tincidunt non, vestibulum ut
            turpis. In hac habitasse platea dictumst. In a odio eget enim porttitor maximus. Aliquam nulla nibh,
            ullamcorper aliquam placerat eu, viverra et dui. Phasellus ex lectus, maximus in mollis ac, luctus vel eros.
            Vivamus ultrices, turpis sed malesuada gravida, eros ipsum venenatis elit, et volutpat eros dui et ante.
            Quisque ultricies mi nec leo ultricies mollis. Vivamus egestas volutpat lacinia. Quisque pharetra eleifend
            efficitur.
        </p>
    </div>
</BitModal>

<BitModal @bind-IsOpen=""IsOpen10"" Classes=""@(new() { Container = ""custom-container"", Overlay = ""custom-overlay"", Content = ""custom-content"" })"">
    <div class=""modal-header"">
        <span>Modal classes</span>
        <BitButton Variant=""BitVariant.Text"" OnClick=""@(() => IsOpen10 = false)"" IconName=""@BitIconName.ChromeClose"" Title=""Close"" />
    </div>
    <div class=""modal-body"">
        <p>
            Lorem ipsum dolor sit amet, consectetur adipiscing elit. Maecenas lorem nulla, malesuada ut sagittis sit
            amet, vulputate in leo. Maecenas vulputate congue sapien eu tincidunt. Etiam eu sem turpis. Fusce tempor
            sagittis nunc, ut interdum ipsum vestibulum non. Proin dolor elit, aliquam eget tincidunt non, vestibulum ut
            turpis. In hac habitasse platea dictumst. In a odio eget enim porttitor maximus. Aliquam nulla nibh,
            ullamcorper aliquam placerat eu, viverra et dui. Phasellus ex lectus, maximus in mollis ac, luctus vel eros.
            Vivamus ultrices, turpis sed malesuada gravida, eros ipsum venenatis elit, et volutpat eros dui et ante.
            Quisque ultricies mi nec leo ultricies mollis. Vivamus egestas volutpat lacinia. Quisque pharetra eleifend
            efficitur.
        </p>
    </div>
</BitModal>";
    private readonly string example6CsharpCode = @"
private bool IsOpen7 = false;
private bool IsOpen8 = false;
private bool IsOpen9 = false;
private bool IsOpen10 = false;
";

    private readonly string example7RazorCode = @"
<style>
    .modal-header {
        display: flex;
        align-items: center;
        font-size: 24px;
        font-weight: 600;
        border-top: 4px solid #0054C6;
        justify-content: space-between;
        padding: 12px 12px 14px 24px;
    }

    .modal-body {
        padding: 0 24px 24px;
        overflow-y: hidden;
        line-height: 20px;
        max-width: 960px;
    }
</style>

<BitButton Dir=""BitDir.Rtl"" OnClick=@(() => IsOpen11 = true)>باز کردن مُدال</BitButton>

<BitModal Dir=""BitDir.Rtl"" @bind-IsOpen=""IsOpen11"">
    <div class=""modal-header"">
        <span>لورم ایپسوم</span>
        <BitButton Variant=""BitVariant.Text"" OnClick=@(() => IsOpen11 = false) IconName=""@BitIconName.ChromeClose"" Title=""Close"" />
    </div>
    <div class=""modal-body"">
        <p>
            لورم ایپسوم متن ساختگی با تولید سادگی نامفهوم از صنعت چاپ و با استفاده از طراحان گرافیک است.
            چاپگرها و متون بلکه روزنامه و مجله در ستون و سطرآنچنان که لازم است و برای شرایط فعلی تکنولوژی مورد نیاز و کاربردهای متنوع با هدف بهبود ابزارهای کاربردی می باشد.
            کتابهای زیادی در شصت و سه درصد گذشته، حال و آینده شناخت فراوان جامعه و متخصصان را می طلبد تا با نرم افزارها شناخت بیشتری را برای طراحان رایانه ای علی الخصوص طراحان خلاقی و فرهنگ پیشرو در زبان فارسی ایجاد کرد.
            در این صورت می توان امید داشت که تمام و دشواری موجود در ارائه راهکارها و شرایط سخت تایپ به پایان رسد وزمان مورد نیاز شامل حروفچینی دستاوردهای اصلی و جوابگوی سوالات پیوسته اهل دنیای موجود طراحی اساسا مورد استفاده قرار گیرد.
        </p>
        <p>
            لورم ایپسوم متن ساختگی با تولید سادگی نامفهوم از صنعت چاپ و با استفاده از طراحان گرافیک است.
            چاپگرها و متون بلکه روزنامه و مجله در ستون و سطرآنچنان که لازم است و برای شرایط فعلی تکنولوژی مورد نیاز و کاربردهای متنوع با هدف بهبود ابزارهای کاربردی می باشد.
            کتابهای زیادی در شصت و سه درصد گذشته، حال و آینده شناخت فراوان جامعه و متخصصان را می طلبد تا با نرم افزارها شناخت بیشتری را برای طراحان رایانه ای علی الخصوص طراحان خلاقی و فرهنگ پیشرو در زبان فارسی ایجاد کرد.
            در این صورت می توان امید داشت که تمام و دشواری موجود در ارائه راهکارها و شرایط سخت تایپ به پایان رسد وزمان مورد نیاز شامل حروفچینی دستاوردهای اصلی و جوابگوی سوالات پیوسته اهل دنیای موجود طراحی اساسا مورد استفاده قرار گیرد.
        </p>
        <p>
            لورم ایپسوم متن ساختگی با تولید سادگی نامفهوم از صنعت چاپ و با استفاده از طراحان گرافیک است.
            چاپگرها و متون بلکه روزنامه و مجله در ستون و سطرآنچنان که لازم است و برای شرایط فعلی تکنولوژی مورد نیاز و کاربردهای متنوع با هدف بهبود ابزارهای کاربردی می باشد.
            کتابهای زیادی در شصت و سه درصد گذشته، حال و آینده شناخت فراوان جامعه و متخصصان را می طلبد تا با نرم افزارها شناخت بیشتری را برای طراحان رایانه ای علی الخصوص طراحان خلاقی و فرهنگ پیشرو در زبان فارسی ایجاد کرد.
            در این صورت می توان امید داشت که تمام و دشواری موجود در ارائه راهکارها و شرایط سخت تایپ به پایان رسد وزمان مورد نیاز شامل حروفچینی دستاوردهای اصلی و جوابگوی سوالات پیوسته اهل دنیای موجود طراحی اساسا مورد استفاده قرار گیرد.
        </p>
    </div>
</BitModal>";
    private readonly string example7CsharpCode = @"
private bool IsOpen11 = false;";
}
