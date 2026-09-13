namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Layouts.Header;

public partial class BitHeaderDemo
{
    private readonly string example1RazorCode = @"
<BitHeader>I'm a Header</BitHeader>

<BitHeader Height=""80"">I'm a Header with a fixed 80px height</BitHeader>

<BitHeader IsEnabled=""false"">I'm a disabled Header</BitHeader>";

    private readonly string example2RazorCode = @"
<BitHeader Variant=""BitVariant.Fill"" Color=""BitColor.Info"">Fill</BitHeader>

<BitHeader Variant=""BitVariant.Outline"" Color=""BitColor.Info"">Outline</BitHeader>

<BitHeader Variant=""BitVariant.Text"" Color=""BitColor.Info"">Text</BitHeader>";

    private readonly string example3RazorCode = @"
<BitHeader Alignment=""BitAlignment.Start"" Bordered>
    <BitTag Text=""Start"" />
    <BitTag Text=""of"" />
    <BitTag Text=""the line"" />
</BitHeader>

<BitHeader Alignment=""BitAlignment.Center"" Bordered>
    <BitTag Text=""Center"" />
    <BitTag Text=""of"" />
    <BitTag Text=""the line"" />
</BitHeader>

<BitHeader Alignment=""BitAlignment.End"" Bordered>
    <BitTag Text=""End"" />
    <BitTag Text=""of"" />
    <BitTag Text=""the line"" />
</BitHeader>

<BitHeader Alignment=""BitAlignment.SpaceBetween"" Bordered>
    <BitTag Text=""Space"" />
    <BitTag Text=""between"" />
    <BitTag Text=""the items"" />
</BitHeader>

<BitHeader Alignment=""BitAlignment.SpaceAround"" Bordered>
    <BitTag Text=""Space"" />
    <BitTag Text=""around"" />
    <BitTag Text=""the items"" />
</BitHeader>

<BitHeader Alignment=""BitAlignment.SpaceEvenly"" Bordered>
    <BitTag Text=""Space"" />
    <BitTag Text=""evenly"" />
    <BitTag Text=""around the items"" />
</BitHeader>


<BitHeader Height=""72"" Bordered VerticalAlign=""BitAlignment.Start"">
    <BitTag Text=""Top of the header"" />
</BitHeader>

<BitHeader Height=""72"" Bordered VerticalAlign=""BitAlignment.Center"">
    <BitTag Text=""Middle of the header"" />
</BitHeader>

<BitHeader Height=""72"" Bordered VerticalAlign=""BitAlignment.End"">
    <BitTag Text=""Bottom of the header"" />
</BitHeader>";

    private readonly string example4RazorCode = @"
<style>
    .wrap-demo {
        max-width: 24rem;
    }
</style>


<div class=""wrap-demo"">
    <BitHeader Bordered Gap=""0.25rem 0.5rem"">
        @for (var i = 1; i <= 8; i++)
        {
            <BitTag Text=""@($""No wrap {i}"")"" />
        }
    </BitHeader>
</div>

<div class=""wrap-demo"">
    <BitHeader Bordered Wrap Gap=""0.25rem 0.5rem"" Alignment=""BitAlignment.Center"">
        @for (var i = 1; i <= 8; i++)
        {
            <BitTag Text=""@($""Wrapped {i}"")"" />
        }
    </BitHeader>
</div>";

    private readonly string example5RazorCode = @"
<BitHeader Bordered>Bordered</BitHeader>

<BitHeader Elevated>Elevated</BitHeader>

<BitHeader Bordered Elevated Color=""BitColor.Tertiary"" Variant=""BitVariant.Outline"">Bordered & Elevated</BitHeader>";

    private readonly string example6RazorCode = @"
<BitHeader Bordered>With the default gutter</BitHeader>

<BitHeader Bordered NoGutter>
    <BitProgress Percent=""60"" />
</BitHeader>";

    private readonly string example7RazorCode = @"
<BitHeader Bordered>
    <BitTag Text=""No"" />
    <BitTag Text=""gap"" />
    <BitTag Text=""at all"" />
</BitHeader>

<BitHeader Bordered Gap=""0.5rem"">
    <BitTag Text=""A"" />
    <BitTag Text=""0.5rem"" />
    <BitTag Text=""gap"" />
</BitHeader>

<BitHeader Bordered Gap=""2rem"" Alignment=""BitAlignment.Center"">
    <BitTag Text=""A"" />
    <BitTag Text=""2rem"" />
    <BitTag Text=""gap"" />
</BitHeader>";

    private readonly string example8RazorCode = @"
<BitHeader Bordered Color=""BitColor.SecondaryBackground"" Alignment=""BitAlignment.SpaceBetween"">
    <BitTag Text=""Full width content"" />
    <BitTag Text=""Edge to edge"" />
</BitHeader>

<BitHeader Bordered MaxWidth=""24rem"" Color=""BitColor.SecondaryBackground"" Alignment=""BitAlignment.SpaceBetween"">
    <BitTag Text=""Capped at 24rem"" />
    <BitTag Text=""And centered"" />
</BitHeader>";

    private readonly string example9RazorCode = @"
<style>
    .scroll-demo {
        height: 10rem;
        overflow: auto;
        border: 1px solid gray;
    }

    .scroll-demo-row {
        padding: 0.5rem 1rem;
    }

    /* A fixed or absolute header overlaps the content it covers, so the first row keeps its own
       distance instead of hiding underneath the bar. */
    .scroll-demo-row--offset {
        margin-block-start: 3rem;
    }

    /* A transform makes this box the containing block of any fixed descendant, which scopes the
       fixed header of the example to the box instead of the top of the page. */
    .fixed-demo {
        height: 10rem;
        overflow: hidden;
        position: relative;
        transform: translateZ(0);
        border: 1px solid gray;
    }

    /* An absolute header only asks for an ancestor that is positioned, which any plain relative box is. */
    .absolute-demo {
        height: 10rem;
        position: relative;
        border: 1px solid gray;
    }
</style>


<div class=""scroll-demo"">
    <BitHeader Sticky Bordered Color=""BitColor.SecondaryBackground"">I'm a sticky Header</BitHeader>
    @for (var i = 1; i <= 12; i++)
    {
        <div class=""scroll-demo-row"">Row @i</div>
    }
</div>

<div class=""fixed-demo"">
    <BitHeader Fixed Elevated Color=""BitColor.Primary"">I'm a fixed Header</BitHeader>
    <div class=""scroll-demo-row scroll-demo-row--offset"">The fixed header covers the top of its page.</div>
</div>

<div class=""absolute-demo"">
    <BitHeader Absolute Bordered Color=""BitColor.TertiaryBackground"">I'm an absolute Header</BitHeader>
    <div class=""scroll-demo-row scroll-demo-row--offset"">The absolute header covers the top of its container.</div>
</div>";

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


<div>Scroll inside the box to hide and reveal the header: <b>@(isHeaderRevealed ? ""revealed"" : ""hidden"")</b></div>

<div class=""scroll-demo"">
    <BitHeader Sticky Reveal Elevated
               Color=""BitColor.Primary""
               Alignment=""BitAlignment.Center""
               OnRevealChanged=""v => isHeaderRevealed = v"">
        I hide myself while you scroll down
    </BitHeader>
    @for (var i = 1; i <= 12; i++)
    {
        <div class=""scroll-demo-row"">Row @i</div>
    }
</div>


<div class=""scroll-demo"">
    <BitHeader Sticky Reveal Elevated
               RevealOffset=""100""
               Color=""BitColor.Tertiary""
               Alignment=""BitAlignment.Center"">
        I stay until you scroll past 100px
    </BitHeader>
    @for (var i = 1; i <= 12; i++)
    {
        <div class=""scroll-demo-row"">Row @i</div>
    }
</div>";
    private readonly string example10CsharpCode = @"
private bool isHeaderRevealed = true;";

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


<div class=""scroll-demo"">
    <BitHeader Sticky ElevateOnScroll Color=""BitColor.PrimaryBackground"">I gain my shadow once you scroll</BitHeader>
    @for (var i = 1; i <= 12; i++)
    {
        <div class=""scroll-demo-row"">Row @i</div>
    }
</div>


<div class=""scroll-demo"">
    <BitHeader Sticky ElevateOnScroll
               ElevateOffset=""40""
               Color=""BitColor.PrimaryBackground""
               Height=""@(isHeaderScrolled ? 48 : 80)""
               OnScrolledChanged=""v => isHeaderScrolled = v"">
        <BitText Typography=""@(isHeaderScrolled ? BitTypography.Subtitle2 : BitTypography.H5)"">
            @(isHeaderScrolled ? ""My App"" : ""My Awesome Application"")
        </BitText>
    </BitHeader>
    @for (var i = 1; i <= 12; i++)
    {
        <div class=""scroll-demo-row"">Row @i</div>
    }
</div>";
    private readonly string example11CsharpCode = @"
private bool isHeaderScrolled;";

    private readonly string example12RazorCode = @"
<style>
    .split-demo {
        border: 1px solid gray;
    }

    /* The pane, not the outer box, is what carries the overflow - and what the header is pointed at. */
    .split-demo-pane {
        height: 10rem;
        overflow: auto;
    }

    .scroll-demo-row {
        padding: 0.5rem 1rem;
    }
</style>


<div class=""split-demo"">
    <BitHeader Sticky ElevateOnScroll Bordered
               ScrollTarget=""#header-scroll-pane""
               Color=""BitColor.PrimaryBackground"">
        I react to the pane below me
    </BitHeader>
    <div id=""header-scroll-pane"" class=""split-demo-pane"">
        @for (var i = 1; i <= 12; i++)
        {
            <div class=""scroll-demo-row"">Row @i</div>
        }
    </div>
</div>";

    private readonly string example13RazorCode = @"
<style>
    /* A transform makes this box the containing block of any fixed descendant, which scopes the
       fixed header of the example to the box instead of the top of the page. */
    .fixed-demo {
        height: 10rem;
        overflow: hidden;
        position: relative;
        transform: translateZ(0);
        border: 1px solid gray;
    }

    .scroll-demo-row {
        padding: 0.5rem 1rem;
    }

    /* The fixed header overlaps the content it covers, so the row below keeps its own distance. */
    .scroll-demo-row--offset {
        margin-block-start: 3rem;
    }
</style>


<div class=""fixed-demo"">
    <BitHeader Fixed Elevated
               Gap=""0.5rem""
               Color=""BitColor.Primary""
               Hidden=""@isImmersiveMode""
               Alignment=""BitAlignment.SpaceBetween""
               AriaLabel=""Reader actions"">
        <span>Chapter 3</span>
        <BitButton Size=""BitSize.Small"" Variant=""BitVariant.Text"" Color=""BitColor.PrimaryBackground"">Contents</BitButton>
    </BitHeader>
    <div class=""scroll-demo-row scroll-demo-row--offset"">
        <BitToggleButton @bind-IsChecked=""isImmersiveMode"" OnText=""Leave immersive mode"" OffText=""Enter immersive mode"" />
    </div>
</div>";
    private readonly string example13CsharpCode = @"
private bool isImmersiveMode;";

    private readonly string example14RazorCode = @"
<style>
    /* The content scrolls behind the pinned header, which is what makes the frosted glass visible. */
    .translucent-demo {
        height: 10rem;
        overflow: auto;
        border: 1px solid gray;
    }

    .scroll-demo-row {
        padding: 0.5rem 1rem;
    }
</style>


<div class=""translucent-demo"">
    <BitHeader Sticky Translucent Bordered Color=""BitColor.PrimaryBackground"">I'm a translucent Header</BitHeader>
    @for (var i = 1; i <= 10; i++)
    {
        <div class=""scroll-demo-row"">Content behind the header - row @i</div>
    }
</div>";

    private readonly string example15RazorCode = @"
<style>
    .scroll-demo {
        height: 10rem;
        overflow: auto;
        border: 1px solid gray;
    }

    .scroll-demo-row {
        padding: 0.5rem 1rem;
    }

    /* The outline is what makes the jump of the skip link visible, since the target itself is only
       focusable programmatically. */
    .skip-target:focus {
        outline: 2px dashed dodgerblue;
    }
</style>


<BitHeader AriaLabel=""Editor actions"" Alignment=""BitAlignment.End"" Gap=""0.5rem"" Bordered>
    <BitButton Variant=""BitVariant.Text"">Discard</BitButton>
    <BitButton>Save</BitButton>
</BitHeader>


<BitHeader SkipLinkHref=""#header-skip-target"" Gap=""0.5rem"" Bordered>
    <BitButton Variant=""BitVariant.Text"" IconName=""@BitIconName.GlobalNavButton"" Title=""Open navigation"" />
    <BitText Typography=""BitTypography.Subtitle2"">My Awesome App</BitText>
</BitHeader>

<div id=""header-skip-target"" tabindex=""-1"" class=""skip-target"">The skip link lands here.</div>


<div class=""scroll-demo"">
    <BitHeader Sticky Elevated Color=""BitColor.Error"" AriaLabel=""Without scroll padding"">
        Without ScrollPadding - the focus lands under me
    </BitHeader>
    @for (var i = 1; i <= 12; i++)
    {
        <div class=""scroll-demo-row""><BitButton Variant=""BitVariant.Text"">Button @i</BitButton></div>
    }
</div>

<div class=""scroll-demo"">
    <BitHeader Sticky Elevated ScrollPadding Color=""BitColor.Success"" AriaLabel=""With scroll padding"">
        With ScrollPadding - the focus stops below me
    </BitHeader>
    @for (var i = 1; i <= 12; i++)
    {
        <div class=""scroll-demo-row""><BitButton Variant=""BitVariant.Text"">Button @i</BitButton></div>
    }
</div>";

    private readonly string example16RazorCode = @"
<BitHeader Gap=""1rem"">
    <BitButton Variant=""BitVariant.Text"" IconName=""@BitIconName.GlobalNavButton"" Title=""Open Navigation"" />
    <BitText Typography=""BitTypography.Caption1"">My Awesome App</BitText>
    <BitSpacer />
    <BitButton Variant=""BitVariant.Text"" IconName=""@BitIconName.Contact"" Title=""Sign in"" />
    <BitMenuButton TItem=""BitMenuButtonOption""
                   ChevronDownIconName=""@BitIconName.More""
                   Variant=""BitVariant.Text""
                   title=""See more""
                   Styles=""@(new() { OperatorButton = ""padding: 0.5rem;"" })"">
        <BitMenuButtonOption Text=""Settings"" IconName=""@BitIconName.Settings"" />
        <BitMenuButtonOption Text=""About"" IconName=""@BitIconName.Info"" />
        <BitMenuButtonOption Text=""Feedback"" IconName=""@BitIconName.Feedback"" />
    </BitMenuButton>
</BitHeader>";

    private readonly string example17RazorCode = @"
<BitHeader Color=""BitColor.Primary"">Primary</BitHeader>

<BitHeader Color=""BitColor.Secondary"">Secondary</BitHeader>

<BitHeader Color=""BitColor.Tertiary"">Tertiary</BitHeader>

<BitHeader Color=""BitColor.Info"">Info</BitHeader>

<BitHeader Color=""BitColor.Success"">Success</BitHeader>

<BitHeader Color=""BitColor.Warning"">Warning</BitHeader>

<BitHeader Color=""BitColor.SevereWarning"">SevereWarning</BitHeader>

<BitHeader Color=""BitColor.Error"">Error</BitHeader>


<BitHeader Color=""BitColor.PrimaryBackground"">PrimaryBackground</BitHeader>

<BitHeader Color=""BitColor.SecondaryBackground"">SecondaryBackground</BitHeader>

<BitHeader Color=""BitColor.TertiaryBackground"">TertiaryBackground</BitHeader>


<BitHeader Color=""BitColor.PrimaryForeground"">PrimaryForeground</BitHeader>

<BitHeader Color=""BitColor.SecondaryForeground"">SecondaryForeground</BitHeader>

<BitHeader Color=""BitColor.TertiaryForeground"">TertiaryForeground</BitHeader>


<BitHeader Color=""BitColor.PrimaryBorder"" Variant=""BitVariant.Outline"">PrimaryBorder</BitHeader>

<BitHeader Color=""BitColor.SecondaryBorder"" Variant=""BitVariant.Outline"">SecondaryBorder</BitHeader>

<BitHeader Color=""BitColor.TertiaryBorder"" Variant=""BitVariant.Outline"">TertiaryBorder</BitHeader>";

    private readonly string example18RazorCode = @"
<BitHeader Size=""BitSize.Small"" Bordered>Small</BitHeader>

<BitHeader Size=""BitSize.Medium"" Bordered>Medium</BitHeader>

<BitHeader Size=""BitSize.Large"" Bordered>Large</BitHeader>";

    private readonly string example19RazorCode = @"
<style>
    .custom-class {
        color: white;
        background: linear-gradient(90deg, #f7971e, #ffd200);
    }

    .custom-root {
        border-block-end: 2px solid blueviolet;
    }

    .custom-container {
        color: blueviolet;
        justify-content: flex-end;
    }
</style>


<BitHeader Style=""background: linear-gradient(90deg, #7e57c2, #26c6da); color: white;"">Styled Header</BitHeader>

<BitHeader Class=""custom-class"">Classed Header</BitHeader>


<BitHeader Styles=""@(new() { Root = ""border-block-end: 2px dashed tomato;"",
                             Container = ""justify-content: center; font-weight: bold; color: tomato;"" })"">
    Styles
</BitHeader>

<BitHeader Classes=""@(new() { Root = ""custom-root"", Container = ""custom-container"" })"">
    Classes
</BitHeader>";

    private readonly string example20RazorCode = @"
<BitHeader Dir=""BitDir.Rtl"" Alignment=""BitAlignment.SpaceBetween"" Bordered>
    <BitTag Text=""یک"" />
    <BitTag Text=""دو"" />
    <BitTag Text=""سه"" />
</BitHeader>";
}
