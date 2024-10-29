namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Surfaces.Dialog;

public partial class BitDialogDemo
{
    private readonly List<ComponentParameter> componentParameters =
    [
        new()
        {
            Name = "AutoToggleScroll",
            Type = "bool",
            DefaultValue = "false",
            Description = "Enables the auto scrollbar toggle behavior of the Dialog."
        },
        new()
        {
            Name = "AbsolutePosition",
            Type = "bool",
            DefaultValue = "false",
            Description = "When true, the Dialog will be positioned absolute instead of fixed."
        },
        new()
        {
            Name = "Body",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "Alias for child content."
        },
        new()
        {
            Name = "CancelText",
            Type = "string?",
            DefaultValue = "Cancel",
            Description = "The text of the cancel button."
        },
        new()
        {
            Name = "ChildContent",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "The content of the Dialog, it can be any custom tag or text."
        },
        new()
        {
            Name = "Classes",
            Type = "BitDialogClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS classes for different parts of the BitDialog component.",
            LinkType = LinkType.Link,
            Href = "#class-styles",
        },
        new()
        {
            Name = "DragElementSelector",
            Type = "string?",
            DefaultValue = "null",
            Description = "The CSS selector of the drag element. By default, it's the Dialog container."
        },
        new()
        {
            Name = "FooterTemplate",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "Used to customize how the footer inside the Dialog is rendered."
        },
        new()
        {
            Name = "IsAlert",
            Type = "bool?",
            DefaultValue = "null",
            Description = "Determines the ARIA role of the Dialog (alertdialog/dialog). If this is set, it will override the ARIA role determined by IsBlocking and IsModeless."
        },
        new()
        {
            Name = "IsBlocking",
            Type = "bool",
            DefaultValue = "false",
            Description = "Whether the Dialog can be light dismissed by clicking outside the Dialog (on the overlay)."
        },
        new()
        {
            Name = "IsDraggable",
            Type = "bool",
            DefaultValue = "false",
            Description = "Whether the Dialog can be dragged around."
        },
        new()
        {
            Name = "IsModeless",
            Type = "bool",
            DefaultValue = "false",
            Description = "Whether the Dialog should be modeless (e.g. not dismiss when focusing/clicking outside of the Dialog). If true, IsBlocking is ignored, and there will be no overlay."
        },
        new()
        {
            Name = "IsOpen",
            Type = "bool",
            DefaultValue = "false",
            Description = "Whether the Dialog is displayed."
        },
        new()
        {
            Name = "IsOpenChanged",
            Type = "EventCallback<bool>",
            DefaultValue = "null",
            Description = "A callback function for when the Dialog is opened or closed."
        },
        new()
        {
            Name = "Message",
            Type = "string?",
            DefaultValue = "null",
            Description = "The message to display in the dialog."
        },
        new()
        {
            Name = "OkText",
            Type = "string?",
            DefaultValue = "Ok",
            Description = "The text of the ok button."
        },
        new()
        {
            Name = "OnCancel",
            Type = "EventCallback<MouseEventArgs>",
            DefaultValue = "null",
            Description = "A callback function for when the Cancel button is clicked."
        },
        new()
        {
            Name = "OnClose",
            Type = "EventCallback<MouseEventArgs>",
            DefaultValue = "null",
            Description = "A callback function for when the Close button is clicked."
        },
        new()
        {
            Name = "OnDismiss",
            Type = "EventCallback<MouseEventArgs>",
            DefaultValue = "null",
            Description = "A callback function for when the the dialog is dismissed (closed)."
        },
        new()
        {
            Name = "OnOk",
            Type = "EventCallback<MouseEventArgs>",
            DefaultValue = "null",
            Description = "A callback function for when the Ok button is clicked."
        },
        new()
        {
            Name = "Position",
            Type = "BitDialogPosition",
            DefaultValue = "BitDialogPosition.Center",
            Description = "Position of the Dialog on the screen.",
            LinkType = LinkType.Link,
            Href = "#component-position-enum",
        },
        new()
        {
            Name = "ScrollerSelector",
            Type = "string",
            DefaultValue = "body",
            Description = "Set the element selector for which the Dialog disables its scroll if applicable."
        },
        new()
        {
            Name = "ShowCancelButton",
            Type = "bool",
            DefaultValue = "true",
            Description = "Shows or hides the cancel button of the Dialog."
        },
        new()
        {
            Name = "ShowCloseButton",
            Type = "bool",
            DefaultValue = "true",
            Description = "Shows or hides the close button of the Dialog."
        },
        new()
        {
            Name = "ShowOkButton",
            Type = "bool",
            DefaultValue = "true",
            Description = "Shows or hides the ok button of the Dialog."
        },
        new()
        {
            Name = "Styles",
            Type = "BitDialogClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS styles for different parts of the BitDialog component.",
            LinkType = LinkType.Link,
            Href = "#class-styles",
        },
        new()
        {
            Name = "SubtitleAriaId",
            Type = "string?",
            DefaultValue = "null",
            Description = "ARIA id for the subtitle of the Dialog, if any."
        },
        new()
        {
            Name = "Title",
            Type = "string?",
            DefaultValue = "null",
            Description = "The title text to display at the top of the dialog."
        },
        new()
        {
            Name = "TitleAriaId",
            Type = "string?",
            DefaultValue = "null",
            Description = "ARIA id for the title of the Dialog, if any."
        }
    ];

    private readonly List<ComponentSubClass> componentSubClasses =
    [
        new()
        {
            Id = "class-styles",
            Title = "BitDialogClassStyles",
            Parameters =
            [
                new()
                {
                    Name = "Root",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the root element of the BitDialog."
                },
                new()
                {
                    Name = "Overlay",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the overlay of the BitDialog."
                },
                new()
                {
                    Name = "Container",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the container of the BitDialog."
                },
                new()
                {
                    Name = "Header",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the header of the BitDialog."
                },
                new()
                {
                    Name = "Body",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the body of the BitDialog."
                },
                new()
                {
                    Name = "Title",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the title of the BitDialog."
                },
                new()
                {
                    Name = "Message",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the message of the BitDialog."
                },
                new()
                {
                    Name = "ButtonsContainer",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the buttons container of the BitDialog."
                },
                new()
                {
                    Name = "Spinner",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the spinner of the ok button of the BitDialog."
                },
                new()
                {
                    Name = "OkButton",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the ok button of the BitDialog."
                },
                new()
                {
                    Name = "CancelButton",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the cancel button of the BitDialog."
                }
            ]
        }
    ];

    private readonly List<ComponentSubEnum> componentSubEnums =
    [
        new()
        {
            Id = "component-position-enum",
            Name = "BitDialogPosition",
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



    private bool IsOpen = false;
    private bool IsOpenEvent = false;

    private BitDialog dialogRef = default!;
    private BitDialog customDialogRef = default!;

    private bool IsOpen1 = false;

    private bool IsOpen2 = false;
    private string? optionValue;
    private bool IsOpen3 = false;

    private bool IsOpen4 = false;
    private bool IsOpen5 = false;

    private bool IsOpen6 = false;
    private bool IsOpen7 = false;

    private bool IsOpenInPosition = false;
    private BitDialogPosition position;

    private bool IsDraggable = false;
    private bool IsOpen8 = false;
    private bool IsOpen9 = false;

    private bool IsOpen10 = false;

    private void OpenDialogInPosition(BitDialogPosition positionValue)
    {
        IsOpenInPosition = true;
        position = positionValue;
    }



    private readonly string example1RazorCode = @"
<BitButton OnClick=""@(() => IsOpen = true)"">Open Dialog</BitButton>
<BitDialog @bind-IsOpen=""IsOpen"" Title=""Missing Subject"" Message=""Do you want to send this message without a subject?"" />";
    private readonly string example1CsharpCode = @"
private bool IsOpen = false;";

    private readonly string example2RazorCode = @"
<BitButton OnClick=""@(() => IsOpen1 = true)"">Open Dialog</BitButton>
<span>Result is: @dialogRef?.Result</span>

<BitDialog @ref=""@dialogRef""
           @bind-IsOpen=""@IsOpen1""
           Title=""Missing Subject""
           Message=""Do you want to send this message without a subject?"" />";
    private readonly string example2CsharpCode = @"
private bool IsOpen1;
private BitDialog dialogRef;
";

    private readonly string example3RazorCode = @"
<BitButton OnClick=""@(() => IsOpenEvent = true)"">Open Dialog</BitButton>
<BitDialog @bind-IsOpen=""IsOpenEvent""
           Title=""Missing Subject""
           Message=""Do you want to send this message without a subject?""
           OnOk=""async () => await Task.Delay(1000)"" />";
    private readonly string example3CsharpCode = @"
private bool IsOpenEvent = false;";

    private readonly string example4RazorCode = @"
<style>
    .dialog-title {
        display: flex;
        font-size: 24px;
        font-weight: 600;
        align-items: center;
        padding: 12px 12px 14px 24px;
        border-top: 4px solid #0054C6;
        justify-content: space-between;
    }

    .dialog-body {
        padding: 0 24px 24px;
        overflow-y: hidden;
        max-width: 960px;
    }

    .dialog-footer {
        display: flex;
        align-items: center;
        padding: 0 14px 14px;
        justify-content: flex-end;
    }
</style>


<BitButton OnClick=""@(() => IsOpen2 = true)"">Open Dialog</BitButton>
<div>Result is: @customDialogRef?.Result</div>
@if (customDialogRef?.Result == BitDialogResult.Ok)
{
    <div>Value is: @optionValue</div>
}

<BitDialog @ref=""customDialogRef"" @bind-IsOpen=""@IsOpen2"" ShowCloseButton=""false"">
    <div class=""dialog-title"">
        <span>All emails together</span>
    </div>
    <div class=""dialog-body"">
        <p>
            Your Inbox has changed. No longer does it include favorites, it is a singular destination for your emails.
        </p>
        <br />
        <BitChoiceGroup @bind-Value=""optionValue"" Label=""Basic Options"" TItem=""BitChoiceGroupOption<string?>"" TValue=""string?"">
            <BitChoiceGroupOption Text=""Option A"" Value=""@(""A"")"" />
            <BitChoiceGroupOption Text=""Option B"" Value=""@(""B"")"" />
            <BitChoiceGroupOption Text=""Option C"" Value=""@(""C"")"" />
        </BitChoiceGroup>
    </div>
</BitDialog>

<BitButton OnClick=""@(() => IsOpen3 = true)"">Open Dialog</BitButton>
<BitDialog @bind-IsOpen=""@IsOpen3"" ShowCloseButton=""false"">
    <Body>
        <div class=""dialog-title"">
            Delete all
        </div>
        <div class=""dialog-body"">
            +99 Emails will be deleted.
        </div>
    </Body>
    <FooterTemplate>
        <div class=""dialog-footer"">
            Are you sure?! there's no going back.
        </div>
    </FooterTemplate>
</BitDialog>";
    private readonly string example4CsharpCode = @"
private bool IsOpen2 = false;
private string? optionValue;
private bool IsOpen3 = false;
";

    private readonly string example5RazorCode = @"
<BitButton OnClick=""@(() => IsOpen4 = true)"">Open Dialog (IsBlocking = true)</BitButton>
<BitButton OnClick=""@(() => IsOpen5 = true)"">Open Dialog (AutoToggleScroll = false)</BitButton>

<BitDialog IsBlocking
           @bind-IsOpen=""IsOpen4""
           Title=""Missing Subject""
           Message=""Do you want to send this message without a subject?"" />

<BitDialog AutoToggleScroll=""false""
           @bind-IsOpen=""IsOpen5""
           Title=""Missing Subject""
           Message=""Do you want to send this message without a subject?"" />";
    private readonly string example5CsharpCode = @"
private bool IsOpen4 = false;
private bool IsOpen5 = false;";

    private readonly string example6RazorCode = @"
<style>
    .relative-container {
        width: 100%;
        height: 400px;
        overflow: auto;
        margin-top: 1rem;
        position: relative;
        background-color: #eee;
        border: 2px lightgreen solid;
    }
</style>

<BitButton OnClick=""@(() => IsOpen6 = true)"">Open Dialog (AbsolutePosition = true)</BitButton>
<BitButton OnClick=""@(() => IsOpen7 = true)"">Open Dialog (ScrollerSelector)</BitButton>

<div class=""relative-container"">

    <BitDialog AbsolutePosition IsModeless
               @bind-IsOpen=""IsOpen6""
               AutoToggleScroll=""false""
               Title=""Missing Subject""
               Message=""Do you want to send this message without a subject?"" />

    <BitDialog AbsolutePosition
               @bind-IsOpen=""IsOpen7""
               ScrollerSelector="".relative-container""
               Title=""Missing Subject""
               Message=""Do you want to send this message without a subject?"" />

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
    private readonly string example6CsharpCode = @"
private bool IsOpen6 = false;
private bool IsOpen7 = false;";

    private readonly string example7RazorCode = @"
<BitButton OnClick=""() => OpenDialogInPosition(BitDialogPosition.TopLeft)"">Top Left</BitButton>
<BitButton OnClick=""() => OpenDialogInPosition(BitDialogPosition.TopRight)"">Top Right</BitButton>
<BitButton OnClick=""() => OpenDialogInPosition(BitDialogPosition.BottomLeft)"">Bottom Left</BitButton>
<BitButton OnClick=""() => OpenDialogInPosition(BitDialogPosition.BottomRight)"">Bottom Right</BitButton>

<BitDialog @bind-IsOpen=""IsOpenInPosition""
           Position=""position""
           Title=""Missing Subject""
           Message=""Do you want to send this message without a subject?"" />";
    private readonly string example7CsharpCode = @"
private bool IsOpenInPosition = false;
private BitDialogPosition position;

private void OpenDialogInPosition(BitDialogPosition positionValue)
{
    IsOpenInPosition = true;
    position = positionValue;
}";

    private readonly string example8RazorCode = @"
<style>
    .dialog-title {
        display: flex;
        font-size: 24px;
        font-weight: 600;
        align-items: center;
        padding: 12px 12px 14px 24px;
        border-top: 4px solid #0054C6;
        justify-content: space-between;
    }

    .dialog-body {
        padding: 0 24px 24px;
        overflow-y: hidden;
        max-width: 960px;
    }
</style>

<BitToggle Label=""Is Draggable?"" @bind-Value=""IsDraggable"" />

<BitButton OnClick=""@(() => IsOpen8 = true)"">Open Dialog</BitButton>
<BitDialog @bind-IsOpen=""IsOpen8""
           IsDraggable=""IsDraggable""
           Title=""Draggable dialog""
           Message=""Do you want to send this message without a subject?"" />

<BitButton OnClick=""@(() => IsOpen9 = true)"">Open Dialog</BitButton>
<BitDialog IsDraggable @bind-IsOpen=""IsOpen9"" ShowCloseButton=""false"" DragElementSelector="".dialog-title-drag"">
    <div class=""dialog-title dialog-title-drag"">
        <span>Draggble Dialog with custom drag element</span>
        <BitButton Variant=""BitVariant.Text"" OnClick=@(() => IsOpen9 = false) IconName=""@BitIconName.ChromeClose"" Title=""Close"" />
    </div>
    <div class=""dialog-body"">
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
</BitDialog>";
    private readonly string example8CsharpCode = @"
private bool IsDraggable = false;
private bool IsOpen8 = false;
private bool IsOpen9 = false;
";

    private readonly string example9RazorCode = @"
<BitButton Dir=""BitDir.Rtl"" OnClick=""@(() => IsOpen10 = true)"">باز کردن پنجره پیام</BitButton>
<BitDialog @bind-IsOpen=""IsOpen10"" 
           Dir=""BitDir.Rtl""
           Title=""بدون موضوع""
           OkText=""تایید""
           CancelText=""انصراف""
           Message=""آیا می خواهید این پیام را بدون موضوع ارسال کنید؟"" />";
    private readonly string example9CsharpCode = @"
private bool IsOpen10 = false;";
}
