namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Layouts.Footer;

public partial class BitFooterDemo
{
    private readonly string example1RazorCode = @"
<BitFooter>I'm a Footer</BitFooter>

<BitFooter Height=""80"">I'm a Footer with a fixed 80px height</BitFooter>

<BitFooter IsEnabled=""false"">I'm a disabled Footer</BitFooter>";

    private readonly string example2RazorCode = @"
<BitFooter Variant=""BitVariant.Fill"" Color=""BitColor.Info"">Fill</BitFooter>

<BitFooter Variant=""BitVariant.Outline"" Color=""BitColor.Info"">Outline</BitFooter>

<BitFooter Variant=""BitVariant.Text"" Color=""BitColor.Info"">Text</BitFooter>";

    private readonly string example3RazorCode = @"
<BitFooter Bordered>Bordered</BitFooter>

<BitFooter Elevated>Elevated</BitFooter>";

    private readonly string example4RazorCode = @"
<BitFooter Alignment=""BitAlignment.Start"" Bordered>
    <BitTag Text=""Start"" />
    <BitTag Text=""of"" />
    <BitTag Text=""the line"" />
</BitFooter>

<BitFooter Alignment=""BitAlignment.Center"" Bordered>
    <BitTag Text=""Center"" />
    <BitTag Text=""of"" />
    <BitTag Text=""the line"" />
</BitFooter>

<BitFooter Alignment=""BitAlignment.End"" Bordered>
    <BitTag Text=""End"" />
    <BitTag Text=""of"" />
    <BitTag Text=""the line"" />
</BitFooter>

<BitFooter Alignment=""BitAlignment.SpaceBetween"" Bordered>
    <BitTag Text=""Space"" />
    <BitTag Text=""between"" />
    <BitTag Text=""the items"" />
</BitFooter>

<BitFooter Alignment=""BitAlignment.SpaceAround"" Bordered>
    <BitTag Text=""Space"" />
    <BitTag Text=""around"" />
    <BitTag Text=""the items"" />
</BitFooter>

<BitFooter Alignment=""BitAlignment.SpaceEvenly"" Bordered>
    <BitTag Text=""Space"" />
    <BitTag Text=""evenly"" />
    <BitTag Text=""around the items"" />
</BitFooter>


<BitFooter Height=""72"" Bordered VerticalAlign=""BitAlignment.Start"">
    <BitTag Text=""Top of the footer"" />
</BitFooter>

<BitFooter Height=""72"" Bordered VerticalAlign=""BitAlignment.End"">
    <BitTag Text=""Bottom of the footer"" />
</BitFooter>";

    private readonly string example5RazorCode = @"
<style>
    .wrap-demo {
        max-width: 24rem;
    }
</style>


<BitFooter Bordered Gap=""1rem"">
    <BitTag Text=""A"" />
    <BitTag Text=""1rem"" />
    <BitTag Text=""gap"" />
</BitFooter>

<div class=""wrap-demo"">
    <BitFooter Bordered Wrap Gap=""0.25rem 0.5rem"" Alignment=""BitAlignment.Center"">
        @for (var i = 1; i <= 8; i++)
        {
            <BitTag Text=""@($""Wrapped {i}"")"" />
        }
    </BitFooter>
</div>

<BitFooter Bordered MaxWidth=""20rem"" Alignment=""BitAlignment.SpaceBetween"">
    <BitTag Text=""Centered"" />
    <BitTag Text=""20rem column"" />
</BitFooter>

<BitFooter Bordered NoGutter>
    <BitProgress Percent=""60"" />
</BitFooter>";

    private readonly string example6RazorCode = @"
<style>
    .scroll-demo {
        height: 10rem;
        overflow: auto;
        border: 1px solid gray;
    }

    .scroll-demo-row {
        padding: 0.5rem 1rem;
    }

    .fixed-demo {
        height: 10rem;
        overflow: hidden;
        position: relative;
        border: 1px solid gray;
        /* Scopes the fixed footer to this box for the sake of the demo.
           Drop it and the footer anchors to the bottom of the page. */
        transform: translateZ(0);
    }

    .absolute-demo {
        height: 10rem;
        position: relative;
        border: 1px solid gray;
    }
</style>


<div class=""scroll-demo"">
    @for (var i = 1; i <= 12; i++)
    {
        <div class=""scroll-demo-row"">Row @i</div>
    }
    <BitFooter Sticky Bordered Color=""BitColor.SecondaryBackground"">I'm a sticky Footer</BitFooter>
</div>

<div class=""fixed-demo"">
    <div class=""scroll-demo-row"">The fixed footer covers the bottom of its page.</div>
    <BitFooter Fixed Elevated Color=""BitColor.Primary"">I'm a fixed Footer</BitFooter>
</div>

<div class=""absolute-demo"">
    <div class=""scroll-demo-row"">The absolute footer covers the bottom of its container.</div>
    <BitFooter Absolute Bordered Color=""BitColor.TertiaryBackground"">I'm an absolute Footer</BitFooter>
</div>";

    private readonly string example7RazorCode = @"
<style>
    .scroll-demo {
        height: 10rem;
        overflow: auto;
        border: 1px solid gray;
    }

    .scroll-demo-row {
        padding: 0.5rem 1rem;
    }

    .fixed-demo {
        height: 10rem;
        overflow: hidden;
        position: relative;
        border: 1px solid gray;
        /* Scopes the fixed footer to this box for the sake of the demo. */
        transform: translateZ(0);
    }

    .shell-pane {
        inset: 0;
        overflow: auto;
        position: absolute;
    }
</style>


<div>Scroll inside the box: <b>@(isFooterRevealed ? ""revealed"" : ""hidden"")</b></div>

<div class=""scroll-demo"">
    @for (var i = 1; i <= 12; i++)
    {
        <div class=""scroll-demo-row"">Row @i</div>
    }
    <BitFooter Sticky Reveal Elevated
               Color=""BitColor.Primary""
               Alignment=""BitAlignment.Center""
               OnRevealChanged=""v => isFooterRevealed = v"">
        I hide myself while you scroll down
    </BitFooter>
</div>


<div class=""fixed-demo"">
    <div id=""footer-reveal-pane"" class=""shell-pane"">
        @for (var i = 1; i <= 12; i++)
        {
            <div class=""scroll-demo-row"">Row @i</div>
        }
    </div>
    <BitFooter Fixed Reveal Elevated
               RevealOffset=""100""
               ScrollTarget=""#footer-reveal-pane""
               Color=""BitColor.Tertiary""
               Alignment=""BitAlignment.Center"">
        I stay until the pane scrolls past 100px
    </BitFooter>
</div>";
    private readonly string example7CsharpCode = @"
private bool isFooterRevealed = true;";

    private readonly string example8RazorCode = @"
<style>
    .scroll-demo {
        height: 10rem;
        overflow: auto;
        border: 1px solid gray;
    }

    .scroll-demo-row {
        padding: 0.5rem 1rem;
    }
</style>


<div>Scroll to the end of the box: <b>@(isFooterOverlapping ? ""content below"" : ""at the end"")</b></div>

<div class=""scroll-demo"">
    @for (var i = 1; i <= 12; i++)
    {
        <div class=""scroll-demo-row"">Row @i</div>
    }
    <BitFooter Sticky ElevateOnScroll
               Color=""BitColor.PrimaryBackground""
               OnOverlapChanged=""v => isFooterOverlapping = v"">
        I lose my shadow at the end
    </BitFooter>
</div>";
    private readonly string example8CsharpCode = @"
private bool isFooterOverlapping;";

    private readonly string example9RazorCode = @"
<style>
    .fixed-demo {
        height: 10rem;
        overflow: hidden;
        position: relative;
        border: 1px solid gray;
        /* Scopes the fixed footer to this box for the sake of the demo.
           Drop it and the footer anchors to the bottom of the page. */
        transform: translateZ(0);
    }

    .scroll-demo-row {
        padding: 0.5rem 1rem;
    }
</style>


<div class=""fixed-demo"">
    <div class=""scroll-demo-row"">
        <BitToggleButton @bind-IsChecked=""isSelectionMode""
                         OnText=""Leave selection mode""
                         OffText=""Enter selection mode"" />
    </div>
    <BitFooter Fixed Elevated
               Gap=""0.5rem""
               Color=""BitColor.Primary""
               Hidden=""@(isSelectionMode is false)""
               Alignment=""BitAlignment.SpaceBetween""
               AriaLabel=""Selection actions"">
        <span>2 items selected</span>
        <BitButton Color=""BitColor.Error"" Size=""BitSize.Small"">Delete</BitButton>
    </BitFooter>
</div>";
    private readonly string example9CsharpCode = @"
private bool isSelectionMode;";

    private readonly string example10RazorCode = @"
<style>
    .scroll-demo {
        height: 10rem;
        overflow: auto;
        border: 1px solid gray;
    }

    .scroll-demo-row {
        padding: 0.5rem 1rem;
    }
</style>


<div class=""scroll-demo"">
    @for (var i = 1; i <= 6; i++)
    {
        <div class=""scroll-demo-row"">Content behind the footer - row @i</div>
    }
    <BitFooter Sticky Translucent Color=""BitColor.PrimaryBackground"">I'm a translucent Footer</BitFooter>
</div>";

    private readonly string example11RazorCode = @"
<style>
    .scroll-demo {
        height: 10rem;
        overflow: auto;
        border: 1px solid gray;
    }

    .scroll-demo-row {
        padding: 0.5rem 1rem;
    }
</style>


<BitFooter AriaLabel=""Editor actions"" Alignment=""BitAlignment.End"" Gap=""0.5rem"" Bordered>
    <BitButton Variant=""BitVariant.Text"">Discard</BitButton>
    <BitButton>Save</BitButton>
</BitFooter>


<div>Without ScrollPadding</div>
<div class=""scroll-demo"">
    @for (var i = 1; i <= 8; i++)
    {
        <div class=""scroll-demo-row""><BitButton Variant=""BitVariant.Text"" Size=""BitSize.Small"">Action @i</BitButton></div>
    }
    <BitFooter Sticky Elevated Height=""56"" Color=""BitColor.SecondaryBackground"">Covers the next action</BitFooter>
</div>

<div>With ScrollPadding</div>
<div class=""scroll-demo"">
    @for (var i = 1; i <= 8; i++)
    {
        <div class=""scroll-demo-row""><BitButton Variant=""BitVariant.Text"" Size=""BitSize.Small"">Action @i</BitButton></div>
    }
    <BitFooter Sticky Elevated ScrollPadding Height=""56"" Color=""BitColor.SecondaryBackground"">Keeps the next action visible</BitFooter>
</div>";

    private readonly string example12RazorCode = @"
<style>
    .footer-links {
        gap: 1rem;
        display: flex;
        flex-wrap: wrap;
    }
</style>


<BitFooter Bordered Wrap
           Gap=""0.5rem 1.5rem""
           MaxWidth=""60rem""
           Color=""BitColor.SecondaryBackground""
           Alignment=""BitAlignment.SpaceBetween"">
    <span>© 2026 bit platform. Made with <span role=""img"" aria-label=""love"">♥</span></span>
    <nav aria-label=""Legal"" class=""footer-links"">
        <BitLink Href=""https://bitplatform.dev"">Terms</BitLink>
        <BitLink Href=""https://bitplatform.dev"">Privacy</BitLink>
        <BitLink Href=""https://github.com/bitfoundation/bitplatform"" Target=""_blank"">GitHub</BitLink>
    </nav>
</BitFooter>";

    private readonly string example13RazorCode = @"
<BitParams Parameters=""@footerParams"">
    <BitFooter>Takes the color, variant and border from the cascade</BitFooter>

    <BitFooter Color=""BitColor.Error"">Its own color, the cascaded rest</BitFooter>
</BitParams>

<BitFooter>Outside the cascade, back to the defaults</BitFooter>";
    private readonly string example13CsharpCode = @"
private readonly BitFooterParams[] footerParams =
[
    new()
    {
        Bordered = true,
        Color = BitColor.Success,
        Variant = BitVariant.Outline,
    }
];";

    private readonly string example14RazorCode = @"
<BitFooter Color=""BitColor.Primary"">Primary</BitFooter>
<BitFooter Color=""BitColor.Secondary"">Secondary</BitFooter>
<BitFooter Color=""BitColor.Tertiary"">Tertiary</BitFooter>
<BitFooter Color=""BitColor.Info"">Info</BitFooter>
<BitFooter Color=""BitColor.Success"">Success</BitFooter>
<BitFooter Color=""BitColor.Warning"">Warning</BitFooter>
<BitFooter Color=""BitColor.SevereWarning"">SevereWarning</BitFooter>
<BitFooter Color=""BitColor.Error"">Error</BitFooter>

<BitFooter Color=""BitColor.PrimaryBackground"">PrimaryBackground</BitFooter>
<BitFooter Color=""BitColor.SecondaryBackground"">SecondaryBackground</BitFooter>
<BitFooter Color=""BitColor.TertiaryBackground"">TertiaryBackground</BitFooter>

<BitFooter Color=""BitColor.PrimaryForeground"">PrimaryForeground</BitFooter>
<BitFooter Color=""BitColor.SecondaryForeground"">SecondaryForeground</BitFooter>
<BitFooter Color=""BitColor.TertiaryForeground"">TertiaryForeground</BitFooter>

<BitFooter Color=""BitColor.PrimaryBorder"" Variant=""BitVariant.Outline"">PrimaryBorder</BitFooter>
<BitFooter Color=""BitColor.SecondaryBorder"" Variant=""BitVariant.Outline"">SecondaryBorder</BitFooter>
<BitFooter Color=""BitColor.TertiaryBorder"" Variant=""BitVariant.Outline"">TertiaryBorder</BitFooter>";

    private readonly string example15RazorCode = @"
<BitFooter Size=""BitSize.Small"" Bordered>Small</BitFooter>

<BitFooter Size=""BitSize.Medium"" Bordered>Medium</BitFooter>

<BitFooter Size=""BitSize.Large"" Bordered>Large</BitFooter>";

    private readonly string example16RazorCode = @"
<style>
    .custom-class {
        color: white;
        background: linear-gradient(90deg, #f7971e, #ffd200);
    }

    .custom-root {
        border-block-start: 2px solid blueviolet;
    }

    .custom-container {
        color: blueviolet;
        justify-content: flex-end;
    }
</style>


<BitFooter Style=""background: linear-gradient(90deg, #7e57c2, #26c6da); color: white;"">Styled Footer</BitFooter>

<BitFooter Class=""custom-class"">Classed Footer</BitFooter>


<BitFooter Styles=""@(new() { Root = ""border-block-start: 2px dashed tomato;"",
                             Container = ""justify-content: center; font-weight: bold; color: tomato;"" })"">
    Styles
</BitFooter>

<BitFooter Classes=""@(new() { Root = ""custom-root"", Container = ""custom-container"" })"">
    Classes
</BitFooter>


<BitFooter Style=""--bit-Footer-background: #1e293b; --bit-Footer-color: #e2e8f0; --bit-Footer-padding: 1.25rem 2rem; --bit-Footer-font-size: 0.875rem;"">
    Custom background, color, padding and font size
</BitFooter>

<div style=""--bit-Footer-radius: 12px; --bit-Footer-border-color: var(--bit-clr-pri); --bit-Footer-shadow: 0 -4px 16px rgb(0 0 0 / 0.2);"">
    <BitFooter Bordered Elevated>Rounded, with a primary divider and a custom shadow (set on the ancestor)</BitFooter>

    <BitFooter Variant=""BitVariant.Outline"">The ancestor re-skins this one too</BitFooter>
</div>";

    private readonly string example17RazorCode = @"
<BitFooter Dir=""BitDir.Rtl"" Alignment=""BitAlignment.SpaceBetween"" Bordered>
    <BitTag Text=""یک"" />
    <BitTag Text=""دو"" />
    <BitTag Text=""سه"" />
</BitFooter>";
}
