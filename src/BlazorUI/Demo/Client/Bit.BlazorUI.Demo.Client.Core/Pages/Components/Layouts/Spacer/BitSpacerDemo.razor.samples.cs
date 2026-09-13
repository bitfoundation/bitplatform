namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Layouts.Spacer;

public partial class BitSpacerDemo
{
    private readonly string example1RazorCode = @"
<style>
    .demo-row {
        width: 100%;
        display: flex;
        padding: 0.5rem;
        align-items: center;
        box-sizing: border-box;
        border: 1px solid gray;
    }

    .demo-box {
        color: white;
        white-space: nowrap;
        background: #0078d4;
        padding: 0.25rem 0.75rem;
    }
</style>


<div class=""demo-row"">
    <BitButton Variant=""BitVariant.Text"" IconName=""@BitIconName.GlobalNavButton"" />
    <BitSpacer />
    <BitText Typography=""BitTypography.H6"">Title</BitText>
    <BitSpacer />
    <BitButton Variant=""BitVariant.Text"" IconName=""@BitIconName.Contact"" />
</div>

<div class=""demo-row"">
    <div class=""demo-box"">Start</div>
    <BitSpacer />
    <div class=""demo-box"">End</div>
</div>";

    private readonly string example2RazorCode = @"
<div class=""demo-row"">
    <BitProgress Circular Indeterminate />
    <BitSpacer Width=""64"" />
    <BitProgress Circular Indeterminate />
    <BitSpacer Width=""64"" />
    <BitProgress Circular Indeterminate />
</div>

<div class=""demo-row"">
    <div class=""demo-box"">1rem</div>
    <BitSpacer Gap=""1rem"" />
    <div class=""demo-box"">4ch</div>
    <BitSpacer Gap=""4ch"" />
    <div class=""demo-box"">10%</div>
    <BitSpacer Gap=""10%"" />
    <div class=""demo-box"">end</div>
</div>";

    private readonly string example3RazorCode = @"
<style>
    .demo-col {
        height: 20rem;
        display: flex;
        padding: 0.5rem;
        flex-flow: column;
        align-items: flex-start;
        box-sizing: border-box;
        border: 1px solid gray;
    }
</style>


<div class=""demo-col"">
    <div class=""demo-box"">Height 8</div>
    <BitSpacer Height=""8"" />
    <div class=""demo-box"">Height 32</div>
    <BitSpacer Height=""32"" />
    <div class=""demo-box"">Vertical Gap 3rem</div>
    <BitSpacer Vertical Gap=""3rem"" />
    <div class=""demo-box"">Flexible</div>
    <BitSpacer />
    <div class=""demo-box"">Bottom</div>
</div>";

    private readonly string example4RazorCode = @"
<div class=""demo-row"">
    <div class=""demo-box"">A</div>
    <BitSpacer Grow=""1"" />
    <div class=""demo-box"">B</div>
    <BitSpacer Grow=""2"" />
    <div class=""demo-box"">C</div>
</div>

<div class=""demo-row"">
    <div class=""demo-box"">A</div>
    <BitSpacer Grow=""3"" />
    <div class=""demo-box"">B</div>
    <BitSpacer Grow=""1"" />
    <div class=""demo-box"">C</div>
</div>";

    private readonly string example5RazorCode = @"
<style>
    .demo-resizable {
        resize: horizontal;
        overflow: auto;
        max-width: 100%;
        min-width: 10rem;
    }
</style>


<div class=""demo-row demo-resizable"">
    <div class=""demo-box"">MinGap</div>
    <BitSpacer MinGap=""2rem"" />
    <div class=""demo-box"">2rem</div>
</div>

<div class=""demo-row demo-resizable"">
    <div class=""demo-box"">No</div>
    <BitSpacer />
    <div class=""demo-box"">MinGap</div>
</div>";

    private readonly string example6RazorCode = @"
<style>
    .demo-list {
        margin: 0;
        list-style: none;
    }
</style>


<ul class=""demo-row demo-list"">
    <li class=""demo-box"">Home</li>
    <li class=""demo-box"">Products</li>
    <BitSpacer Element=""li"" />
    <li class=""demo-box"">Sign in</li>
</ul>";

    private readonly string example7RazorCode = @"
<style>
    .demo-col-auto {
        height: auto;
    }
</style>


<div class=""demo-row"">
    <div class=""demo-box"">Small</div>
    <BitSpacer Size=""BitSize.Small"" />
    <div class=""demo-box"">Medium</div>
    <BitSpacer Size=""BitSize.Medium"" />
    <div class=""demo-box"">Large</div>
    <BitSpacer Size=""BitSize.Large"" />
    <div class=""demo-box"">End</div>
</div>

<div class=""demo-col demo-col-auto"">
    <div class=""demo-box"">Vertical Small</div>
    <BitSpacer Vertical Size=""BitSize.Small"" />
    <div class=""demo-box"">Vertical Large</div>
    <BitSpacer Vertical Size=""BitSize.Large"" />
    <div class=""demo-box"">End</div>
</div>";

    private readonly string example8RazorCode = @"
<style>
    .custom-class {
        min-width: 4rem;
        border: 1px dashed red;
        background: rgba(255, 0, 0, 0.15);
    }
</style>


<div class=""demo-row"">
    <div class=""demo-box"">Style</div>
    <BitSpacer Style=""min-width:4rem;background:tomato"" />
    <div class=""demo-box"">Class</div>
    <BitSpacer Class=""custom-class"" />
    <div class=""demo-box"">End</div>
</div>";

    private readonly string example9RazorCode = @"
<style>
    .visible-spacer {
        min-width: 1rem;
        background: tomato;
    }
</style>


<div class=""demo-row"" dir=""rtl"">
    <div class=""demo-box"">شروع</div>
    <BitSpacer Width=""64"" />
    <div class=""demo-box"">میانه</div>
    <BitSpacer />
    <div class=""demo-box"">پایان</div>
</div>

<div class=""demo-row"">
    <div class=""demo-box"">Start</div>
    <BitSpacer Width=""64"" Class=""visible-spacer"" />
    <div class=""demo-box"">Middle</div>
</div>

<div class=""demo-row"">
    <div class=""demo-box"">Start</div>
    <BitSpacer Width=""64"" Class=""visible-spacer"" Dir=""BitDir.Rtl"" />
    <div class=""demo-box"">Middle</div>
</div>";
}
