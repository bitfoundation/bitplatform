﻿using Bit.BlazorUI.Demo.Client.Core.Models;
using Bit.BlazorUI.Demo.Client.Core.Pages.Components.ComponentDemoBase;

namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Modal;

public partial class BitModalDemo
{
    private readonly List<ComponentParameter> componentParameters = new()
    {
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
            Name = "ClassStyles",
            Type = "BitModalClassStyles?",
            DefaultValue = "null",
            Href = "class-styles",
            LinkType = LinkType.Link,
            Description = "Custom CSS classes/styles for different parts of the BitModal component."
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
    };

    private readonly List<ComponentSubClass> componentSubClasses = new()
    {
        new()
        {
            Id = "class-styles",
            Title = "BitModalClassStyles",
            Parameters = new()
            {
               new()
               {
                   Name = "Container",
                   Type = "BitClassStylePair?",
                   Description = "Custom CSS classes/styles for the modal container.",
                   Href = "class-style-pair",
                   LinkType = LinkType.Link
               }
            }
        },
        new()
        {
            Id = "class-style-pair",
            Title = "BitClassStylePair",
            Parameters = new()
            {
               new()
               {
                   Name = "Class",
                   Type = "string?",
                   Description = "Custom CSS class."
               },
               new()
               {
                   Name = "Style",
                   Type = "string?",
                   Description = "Custom CSS style."
               }
            }
        }
    };

    private readonly List<ComponentSubEnum> componentSubEnums = new()
    {
        new()
        {
            Id = "component-position-enum",
            Name = "BitModalPosition",
            Description = "",
            Items = new List<ComponentEnumItem>()
            {
                new() { Name = "Center", Value = "0" },
                new() { Name = "TopLeft", Value = "1" },
                new() { Name = "TopCenter", Value = "2" },
                new() { Name = "TopRight", Value = "3" },
                new() { Name = "CenterLeft", Value = "4" },
                new() { Name = "CenterRight", Value = "5" },
                new() { Name = "BottomLeft", Value = "6" },
                new() { Name = "BottomCenter", Value = "7" },
                new() { Name = "BottomRight", Value = "8" }
            }
        }
    };



    private readonly string example1HTMLCode = @"
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
        <BitIconButton OnClick=@(() => IsOpen = false) IconName=""@BitIconName.ChromeClose"" Title=""Close"" />
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
    private readonly string example1CSharpCode = @"
private bool IsOpen = false;";

    private readonly string example2HTMLCode = @"
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
        <BitIconButton OnClick=@(()=> IsOpen1 = false) IconName=""@BitIconName.ChromeClose"" Title=""Close"" />
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
        <BitIconButton OnClick=@(()=> IsOpen2 = false) IconName=""@BitIconName.ChromeClose"" Title=""Close"" />
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
    private readonly string example2CSharpCode = @"
private bool IsOpen1 = false;
private bool IsOpen2 = false;";

    private readonly string example3HTMLCode = @"
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
            <BitIconButton OnClick=@(()=> IsOpen3 = false) IconName=""@BitIconName.ChromeClose"" Title=""Close"" />
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
            <BitIconButton OnClick=@(()=> IsOpen4 = false) IconName=""@BitIconName.ChromeClose"" Title=""Close"" />
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
    private readonly string example3CSharpCode = @"
private bool IsOpenInPosition = false;
private BitModalPosition position;

private void OpenModalInPosition(BitModalPosition positionValue)
{
    IsOpenInPosition = true;
    position = positionValue;
}";

    private readonly string example4HTMLCode = @"
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
        <BitIconButton OnClick=@(() => IsOpenInPosition = false) IconName=""@BitIconName.ChromeClose"" Title=""Close"" />
    </div>
    <div class=""modal-body"">
        BitModal with custom positioning. Lorem ipsum dolor sit amet, consectetur adipiscing elit.
    </div>
</BitModal>";
    private readonly string example4CSharpCode = @"
private bool IsOpenInPosition = false;
private BitModalPosition position;

private void OpenModalInPosition(BitModalPosition positionValue)
{
    IsOpenInPosition = true;
    position = positionValue;
}";

    private readonly string example5HTMLCode = @"
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
        <BitIconButton OnClick=@(() => IsOpen5 = false) IconName=""@BitIconName.ChromeClose"" Title=""Close"" />
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
        <BitIconButton OnClick=@(() => IsOpen6 = false) IconName=""@BitIconName.ChromeClose"" Title=""Close"" />
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
    private readonly string example5CSharpCode = @"
private bool IsDraggable = false;
private bool IsOpen5 = false;
private bool IsOpen6 = false;
";
}
