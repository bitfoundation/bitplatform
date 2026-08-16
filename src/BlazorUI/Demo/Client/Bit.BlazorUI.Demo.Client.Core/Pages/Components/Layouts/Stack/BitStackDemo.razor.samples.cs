namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Layouts.Stack;

public partial class BitStackDemo
{
    private readonly string example1RazorCode = @"
<style>
    .stack {
        background: #71afe5;
    }

    .item {
        color: white;
        padding: 0.5rem;
        white-space: nowrap;
        background-color: #0078d4;
    }
</style>


<BitStack Class=""stack"">
    <div class=""item"">Item 1</div>
    <div class=""item"">Item 2</div>
    <div class=""item"">Item 3</div>
</BitStack>

<BitStack Horizontal Class=""stack"">
    <div class=""item"">Item 1</div>
    <div class=""item"">Item 2</div>
    <div class=""item"">Item 3</div>
</BitStack>

<BitStack Reversed Class=""stack"">
    <div class=""item"">Item 1</div>
    <div class=""item"">Item 2</div>
    <div class=""item"">Item 3</div>
</BitStack>

<BitStack Horizontal Reversed Class=""stack"">
    <div class=""item"">Item 1</div>
    <div class=""item"">Item 2</div>
    <div class=""item"">Item 3</div>
</BitStack>";

    private readonly string example2RazorCode = @"
<BitSlider Label=""Gap between items"" Max=""5"" ValueFormat=""0.0 rem"" Step=""0.1"" @bind-Value=""@gap"" />

<BitStack Gap=""@($""{gap}rem"")"" Class=""stack"">
    <div class=""item"">Item 1</div>
    <div class=""item"">Item 2</div>
    <div class=""item"">Item 3</div>
</BitStack>

<BitStack Horizontal Wrap HorizontalGap=""2rem"" VerticalGap=""0.25rem"" Class=""stack"">
    @for (int i = 1; i <= 12; i++)
    {
        <div class=""item"">Item @i</div>
    }
</BitStack>";
    private readonly string example2CsharpCode = @"
private double gap = 1;
";

    private readonly string example3RazorCode = @"
<BitStack Horizontal Wrap Gap=""2rem"">
    <BitToggle @bind-Value=""isHorizontal"" Text=""Horizontal"" />
    <BitToggle @bind-Value=""isReversed"" Text=""Reversed"" />
</BitStack>

<BitChoiceGroup @bind-Value=""direction""
                Horizontal
                Label=""Direction""
                TItem=""BitChoiceGroupOption<BitDir>"" TValue=""BitDir"">
    <BitChoiceGroupOption Text=""LTR"" Value=""BitDir.Ltr"" />
    <BitChoiceGroupOption Text=""RTL"" Value=""BitDir.Rtl"" />
    <BitChoiceGroupOption Text=""Auto"" Value=""BitDir.Auto"" />
</BitChoiceGroup>

<BitChoiceGroup @bind-Value=""horizontalAlign""
                Horizontal
                Label=""Horizontal Align""
                TItem=""BitChoiceGroupOption<BitAlignment>"" TValue=""BitAlignment"">
    <BitChoiceGroupOption Text=""Start"" Value=""BitAlignment.Start"" />
    <BitChoiceGroupOption Text=""Center"" Value=""BitAlignment.Center"" />
    <BitChoiceGroupOption Text=""End"" Value=""BitAlignment.End"" />
    <BitChoiceGroupOption Text=""SpaceBetween"" Value=""BitAlignment.SpaceBetween"" />
    <BitChoiceGroupOption Text=""SpaceAround"" Value=""BitAlignment.SpaceAround"" />
    <BitChoiceGroupOption Text=""SpaceEvenly"" Value=""BitAlignment.SpaceEvenly"" />
    <BitChoiceGroupOption Text=""Baseline"" Value=""BitAlignment.Baseline"" />
    <BitChoiceGroupOption Text=""Stretch"" Value=""BitAlignment.Stretch"" />
</BitChoiceGroup>

<BitChoiceGroup @bind-Value=""verticalAlign""
                Horizontal
                Label=""Vertical Align""
                TItem=""BitChoiceGroupOption<BitAlignment>"" TValue=""BitAlignment"">
    <BitChoiceGroupOption Text=""Start"" Value=""BitAlignment.Start"" />
    <BitChoiceGroupOption Text=""Center"" Value=""BitAlignment.Center"" />
    <BitChoiceGroupOption Text=""End"" Value=""BitAlignment.End"" />
    <BitChoiceGroupOption Text=""SpaceBetween"" Value=""BitAlignment.SpaceBetween"" />
    <BitChoiceGroupOption Text=""SpaceAround"" Value=""BitAlignment.SpaceAround"" />
    <BitChoiceGroupOption Text=""SpaceEvenly"" Value=""BitAlignment.SpaceEvenly"" />
    <BitChoiceGroupOption Text=""Baseline"" Value=""BitAlignment.Baseline"" />
    <BitChoiceGroupOption Text=""Stretch"" Value=""BitAlignment.Stretch"" />
</BitChoiceGroup>

<BitStack Dir=""direction""
          Class=""stack""
          Reversed=""isReversed""
          Horizontal=""isHorizontal""
          VerticalAlign=""verticalAlign""
          HorizontalAlign=""horizontalAlign""
          Style=""height:15rem"">
    <div class=""item"">Item 1</div>
    <div class=""item"">Item 2</div>
    <div class=""item"">Item 3</div>
</BitStack>";
    private readonly string example3CsharpCode = @"
private bool isReversed;
private bool isHorizontal;
private BitDir direction;
private BitAlignment verticalAlign;
private BitAlignment horizontalAlign;
";

    private readonly string example4RazorCode = @"
<style>
    .box {
        color: white;
        display: flex;
        width: 3.5rem;
        height: 3.5rem;
        align-items: center;
        justify-content: center;
        background-color: #0078d4;
    }
</style>


<BitSlider Label=""Stack height"" Min=""10"" Max=""20"" Step=""0.1"" ValueFormat=""0.0 rem"" @bind-Value=""@stackHeight"" />

<BitChoiceGroup @bind-Value=""alignContent""
                Horizontal
                Label=""Align Content""
                TItem=""BitChoiceGroupOption<BitAlignment>"" TValue=""BitAlignment"">
    <BitChoiceGroupOption Text=""Start"" Value=""BitAlignment.Start"" />
    <BitChoiceGroupOption Text=""Center"" Value=""BitAlignment.Center"" />
    <BitChoiceGroupOption Text=""End"" Value=""BitAlignment.End"" />
    <BitChoiceGroupOption Text=""SpaceBetween"" Value=""BitAlignment.SpaceBetween"" />
    <BitChoiceGroupOption Text=""SpaceEvenly"" Value=""BitAlignment.SpaceEvenly"" />
    <BitChoiceGroupOption Text=""Stretch"" Value=""BitAlignment.Stretch"" />
</BitChoiceGroup>

<BitToggle @bind-Value=""isWrapReversed"" Text=""Wrap reverse"" />

<BitStack Wrap=""@(isWrapReversed is false)""
          WrapReverse=""isWrapReversed""
          AlignContent=""alignContent""
          Class=""stack""
          Style=""@($""height:{stackHeight}rem"")"">
    @for (int i = 1; i <= 20; i++)
    {
        <div class=""box"">@i</div>
    }
</BitStack>";
    private readonly string example4CsharpCode = @"
private double stackHeight = 15;
private bool isWrapReversed;
private BitAlignment alignContent;
";

    private readonly string example5RazorCode = @"
<BitStack HorizontalMd Class=""stack"">
    <div class=""item"">Item 1</div>
    <div class=""item"">Item 2</div>
    <div class=""item"">Item 3</div>
</BitStack>

<BitStack Horizontal HorizontalLg=""false"" Class=""stack"">
    <div class=""item"">Item 1</div>
    <div class=""item"">Item 2</div>
    <div class=""item"">Item 3</div>
</BitStack>";

    private readonly string example6RazorCode = @"
<BitStack Horizontal HorizontalAlign=""BitAlignment.SpaceAround"" Class=""stack"">
    <div class=""item"">Item 1</div>
    <BitStack>
        <div class=""item"">Item 2-1</div>
        <div class=""item"">Item 2-2</div>
        <div class=""item"">Item 2-3</div>
    </BitStack>
    <BitStack Horizontal>
        <div class=""item"">Item 3-1</div>
        <div class=""item"">Item 3-2</div>
        <div class=""item"">Item 3-3</div>
    </BitStack>
</BitStack>";

    private readonly string example7RazorCode = @"
<BitStack FillContent Class=""stack"" Style=""width:20rem"">
    <div class=""item"">Item 1</div>
    <div class=""item"">Item 2</div>
    <div class=""item"">Item 3</div>
</BitStack>

<BitStack Horizontal GrowContent Class=""stack"">
    <div class=""item"">Item 1</div>
    <div class=""item"">A longer item 2</div>
    <div class=""item"">Item 3</div>
</BitStack>

<BitStack Horizontal FillContent GrowContent Class=""stack"" Style=""height:6rem"">
    <div class=""item"">Item 1</div>
    <div class=""item"">Item 2</div>
    <div class=""item"">Item 3</div>
</BitStack>";

    private readonly string example8RazorCode = @"
<style>
    .host {
        padding: 0.5rem;
        overflow: hidden;
        box-sizing: border-box;
        border: 1px dashed gray;
    }
</style>


<BitStack Class=""stack"" AutoHeight>
    <div class=""item"">Default</div>
</BitStack>

<BitStack Class=""stack"" AutoHeight AutoWidth>
    <div class=""item"">AutoWidth</div>
</BitStack>

<BitStack Horizontal Class=""host"" VerticalAlign=""BitAlignment.Stretch"" Style=""height:8rem"">
    <BitStack Class=""stack"" AutoWidth AutoHeight>
        <div class=""item"">AutoHeight</div>
    </BitStack>
    <BitStack Class=""stack"" AutoWidth FitHeight>
        <div class=""item"">FitHeight</div>
    </BitStack>
    <BitStack Class=""stack"" FitSize>
        <div class=""item"">FitSize</div>
    </BitStack>
</BitStack>";

    private readonly string example9RazorCode = @"
<style>
    .resizable {
        resize: horizontal;
        overflow: auto;
        max-width: 100%;
        min-width: 12rem;
    }

    /* A flex child refuses to shrink below its own content until it is told it may. */
    .squeezable {
        min-width: 0;
        overflow: hidden;
    }
</style>


<BitStack Horizontal Gap=""0.5rem"" Class=""host"" AutoHeight>
    <BitStack Grow=""3"" Class=""item"" AutoSize Alignment=""BitAlignment.Center"">Grow 3</BitStack>
    <BitStack Grow=""2"" Class=""item"" AutoSize Alignment=""BitAlignment.Center"">Grow 2</BitStack>
    <BitStack Grows Class=""item"" AutoSize Alignment=""BitAlignment.Center"">Grows</BitStack>
</BitStack>

<div class=""resizable"">
    <BitStack Horizontal Gap=""0.5rem"" Class=""host"" AutoHeight>
        <BitStack Class=""item squeezable"" AutoSize>Shrinks away</BitStack>
        <BitStack Class=""item"" AutoSize NoShrink>Never shrinks</BitStack>
        <BitStack Class=""item squeezable"" AutoSize>Shrinks away</BitStack>
    </BitStack>
</div>

<BitStack Horizontal Gap=""0.5rem"" Class=""host"" VerticalAlign=""BitAlignment.Center"" Style=""height:8rem"">
    <BitStack Class=""item"" AutoSize Self=""BitAlignment.Start"">Self Start</BitStack>
    <BitStack Class=""item"" AutoSize>Centered</BitStack>
    <BitStack Class=""item"" AutoSize Self=""BitAlignment.End"">Self End</BitStack>
    <BitStack Class=""item"" AutoSize Order=""-1"">Order -1 (written last)</BitStack>
</BitStack>";

    private readonly string example10RazorCode = @"
<BitStack Horizontal Gap=""1rem"" AutoHeight>
    <BitStack Padding=""1rem"" Class=""stack"" AutoHeight>
        <div class=""item"">Padding 1rem</div>
    </BitStack>
    <BitStack Padding=""2rem 0.5rem"" Class=""stack"" AutoHeight>
        <div class=""item"">Padding 2rem 0.5rem</div>
    </BitStack>
</BitStack>";

    private readonly string example11RazorCode = @"
<BitStack Horizontal Element=""nav"" AriaLabel=""Main"" Class=""stack"" AutoHeight>
    <div class=""item"">Home</div>
    <div class=""item"">Products</div>
    <div class=""item"">About</div>
</BitStack>

<div>
    An inline stack
    <BitStack Inline Horizontal Gap=""0.25rem"" Class=""stack"">
        <div class=""item"">flows</div>
        <div class=""item"">with</div>
    </BitStack>
    the text around it.
</div>";

    private readonly string example12RazorCode = @"
<BitStack Horizontal Size=""BitSize.Small"" Class=""stack"" AutoHeight>
    <div class=""item"">Small</div>
    <div class=""item"">Small</div>
    <div class=""item"">Small</div>
</BitStack>

<BitStack Horizontal Size=""BitSize.Medium"" Class=""stack"" AutoHeight>
    <div class=""item"">Medium</div>
    <div class=""item"">Medium</div>
    <div class=""item"">Medium</div>
</BitStack>

<BitStack Horizontal Size=""BitSize.Large"" Class=""stack"" AutoHeight>
    <div class=""item"">Large</div>
    <div class=""item"">Large</div>
    <div class=""item"">Large</div>
</BitStack>";

    private readonly string example13RazorCode = @"
<style>
    .custom-stack {
        padding: 1rem;
        border: 2px solid #107c10;
        background: linear-gradient(90deg, #dff6dd, transparent);
    }

    .truncating {
        overflow: hidden;
    }

    .ellipsis {
        overflow: hidden;
        text-overflow: ellipsis;
    }
</style>


<BitStack Horizontal AutoHeight Style=""padding:1rem;border:2px dashed #d13438;background:#fdf2f2"">
    <div class=""item"">Style</div>
    <div class=""item"">on the stack</div>
</BitStack>

<BitStack Horizontal AutoHeight Class=""custom-stack"">
    <div class=""item"">Class</div>
    <div class=""item"">on the stack</div>
</BitStack>

<BitStack Horizontal Class=""host"" AutoHeight>
    <BitStack Class=""truncating"">
        <div class=""item ellipsis"">A very long label that has nowhere near enough room to be shown in full</div>
    </BitStack>
    <div class=""item"">Fixed</div>
</BitStack>

<BitStack Horizontal Class=""host"" AutoHeight>
    <BitStack Class=""truncating"" Style=""min-width:0"">
        <div class=""item ellipsis"">A very long label that has nowhere near enough room to be shown in full</div>
    </BitStack>
    <div class=""item"">Fixed</div>
</BitStack>";

    private readonly string example14RazorCode = @"
<BitStack Horizontal Dir=""BitDir.Rtl"" Class=""stack"">
    <div class=""item"">یک</div>
    <div class=""item"">دو</div>
    <div class=""item"">سه</div>
</BitStack>

<BitStack Horizontal Reversed Dir=""BitDir.Rtl"" Class=""stack"">
    <div class=""item"">یک</div>
    <div class=""item"">دو</div>
    <div class=""item"">سه</div>
</BitStack>

<BitStack Dir=""BitDir.Rtl"" HorizontalAlign=""BitAlignment.End"" Class=""stack"">
    <div class=""item"">یک</div>
    <div class=""item"">دو</div>
    <div class=""item"">سه</div>
</BitStack>";
}
