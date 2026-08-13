namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Layouts.Grid;

public partial class BitGridDemo
{
    private readonly string example1RazorCode = @"
<style>
    .grid-item {
        padding: 8px;
        min-height: 56px;
        font-size: 0.75rem;
        text-align: center;
        border-radius: 2px;
        align-content: center;
        border: 1px solid gray;
    }
</style>


<BitGrid Columns=""4"">
    @for (int i = 0; i < 8; i++)
    {
        var item = i + 1;

        <BitGridItem Class=""grid-item"">
            Item @item
        </BitGridItem>
    }
</BitGrid>

<BitGrid Span=""4"">
    @for (int i = 0; i < 6; i++)
    {
        var item = i + 1;

        <BitGridItem Class=""grid-item"">
            Item @item
        </BitGridItem>
    }
</BitGrid>";

    private readonly string example2RazorCode = @"
<BitGrid Columns=""12"">
    <BitGridItem Class=""grid-item"" ColumnSpan=""12"">Span 12</BitGridItem>

    <BitGridItem Class=""grid-item"" ColumnSpan=""6"">Span 6</BitGridItem>
    <BitGridItem Class=""grid-item"" ColumnSpan=""6"">Span 6</BitGridItem>

    <BitGridItem Class=""grid-item"" ColumnSpan=""4"">Span 4</BitGridItem>
    <BitGridItem Class=""grid-item"" ColumnSpan=""4"">Span 4</BitGridItem>
    <BitGridItem Class=""grid-item"" ColumnSpan=""4"">Span 4</BitGridItem>

    <BitGridItem Class=""grid-item"" ColumnSpan=""8"">Span 8</BitGridItem>
    <BitGridItem Class=""grid-item"" ColumnSpan=""4"">Span 4</BitGridItem>

    <BitGridItem Class=""grid-item"" ColumnSpan=""3"">Span 3</BitGridItem>
    <BitGridItem Class=""grid-item"" ColumnSpan=""3"">Span 3</BitGridItem>
    <BitGridItem Class=""grid-item"" ColumnSpan=""3"">Span 3</BitGridItem>
    <BitGridItem Class=""grid-item"" ColumnSpan=""3"">Span 3</BitGridItem>
</BitGrid>";

    private readonly string example3RazorCode = @"
<BitGrid Columns=""12"">
    <BitGridItem Class=""grid-item"" Auto>Auto</BitGridItem>
    <BitGridItem Class=""grid-item"" Grow>Grow (takes the rest of the row)</BitGridItem>
    <BitGridItem Class=""grid-item"" Auto>Auto</BitGridItem>
</BitGrid>

<BitGrid Columns=""12"">
    <BitGridItem Class=""grid-item"" ColumnSpan=""4"">Span 4</BitGridItem>
    <BitGridItem Class=""grid-item"" Grow>Grow</BitGridItem>
    <BitGridItem Class=""grid-item"" Grow>Grow</BitGridItem>
</BitGrid>


<BitGrid Columns=""3"" Spacing=""0.5rem"">
    @for (int i = 0; i < 5; i++)
    {
        var item = i + 1;

        <BitGridItem Class=""grid-item"">Item @item</BitGridItem>
    }
</BitGrid>

<BitGrid Columns=""3"" Grow Spacing=""0.5rem"">
    @for (int i = 0; i < 5; i++)
    {
        var item = i + 1;

        <BitGridItem Class=""grid-item"">Item @item</BitGridItem>
    }
</BitGrid>


<BitGrid Columns=""12"" Spacing=""0.5rem"">
    <BitGridItem Class=""grid-item"" ColumnSpan=""12"" AutoMd=""true"">Span 12, then Auto from Md</BitGridItem>
    <BitGridItem Class=""grid-item"" ColumnSpan=""12"" GrowMd=""true"">Span 12, then Grow from Md</BitGridItem>
    <BitGridItem Class=""grid-item"" ColumnSpan=""12"" AutoMd=""true"">Span 12, then Auto from Md</BitGridItem>
</BitGrid>

<BitGrid Columns=""12"" Spacing=""0.5rem"">
    <BitGridItem Class=""grid-item"" Auto AutoMd=""false"" Md=""3"">Auto, then Span 3 from Md</BitGridItem>
    <BitGridItem Class=""grid-item"" Grow GrowMd=""false"" Md=""9"">Grow, then Span 9 from Md</BitGridItem>
</BitGrid>";

    private readonly string example4RazorCode = @"
<BitGrid Columns=""12"">
    <BitGridItem Class=""grid-item"" ColumnSpan=""4"">Span 4</BitGridItem>
    <BitGridItem Class=""grid-item"" ColumnSpan=""4"" Offset=""4"">Span 4, Offset 4</BitGridItem>

    <BitGridItem Class=""grid-item"" ColumnSpan=""6"" Offset=""3"">Span 6, Offset 3 (centered)</BitGridItem>

    <BitGridItem Class=""grid-item"" ColumnSpan=""3"" Offset=""1"">Span 3, Offset 1</BitGridItem>
    <BitGridItem Class=""grid-item"" ColumnSpan=""3"" Offset=""1"">Span 3, Offset 1</BitGridItem>
    <BitGridItem Class=""grid-item"" ColumnSpan=""3"" Offset=""1"">Span 3, Offset 1</BitGridItem>
</BitGrid>

<BitGrid Columns=""12"">
    <BitGridItem Class=""grid-item"" ColumnSpan=""3"">Span 3</BitGridItem>
    <BitGridItem Class=""grid-item"" ColumnSpan=""3"" AutoOffset>Span 3, AutoOffset</BitGridItem>
</BitGrid>

<BitGrid Columns=""12"">
    <BitGridItem Class=""grid-item"" Auto>An auto sized heading</BitGridItem>
    <BitGridItem Class=""grid-item"" Auto AutoOffset>Auto, AutoOffset</BitGridItem>
</BitGrid>

<BitGrid Columns=""12"" Spacing=""0.5rem"">
    <BitGridItem Class=""grid-item"" ColumnSpan=""12"" Md=""4"">A heading</BitGridItem>
    <BitGridItem Class=""grid-item"" ColumnSpan=""12"" Md=""3"" AutoOffsetMd=""true"">AutoOffset from Md only</BitGridItem>
    <BitGridItem Class=""grid-item"" ColumnSpan=""12"" Md=""4"" OffsetMd=""2"">Offset 2 from Md only</BitGridItem>
</BitGrid>

<BitGrid Columns=""12"" Reversed>
    <BitGridItem Class=""grid-item"" ColumnSpan=""3"">Span 3</BitGridItem>
    <BitGridItem Class=""grid-item"" ColumnSpan=""3"" Offset=""2"">Span 3, Offset 2, reversed</BitGridItem>
</BitGrid>";

    private readonly string example5RazorCode = @"
<BitGrid Columns=""4"">
    <BitGridItem Class=""grid-item"" Order=""3"">1st written, Order 3</BitGridItem>
    <BitGridItem Class=""grid-item"" Order=""1"">2nd written, Order 1</BitGridItem>
    <BitGridItem Class=""grid-item"" Order=""-1"">3rd written, Order -1</BitGridItem>
    <BitGridItem Class=""grid-item"" Order=""2"">4th written, Order 2</BitGridItem>
</BitGrid>";

    private readonly string example6RazorCode = @"
<BitChoiceGroup @bind-Value=""horizontalAlign""
                Horizontal
                Label=""HorizontalAlign""
                TItem=""BitChoiceGroupOption<BitAlignment>"" TValue=""BitAlignment"">
    <BitChoiceGroupOption Text=""Start"" Value=""BitAlignment.Start"" />
    <BitChoiceGroupOption Text=""Center"" Value=""BitAlignment.Center"" />
    <BitChoiceGroupOption Text=""End"" Value=""BitAlignment.End"" />
    <BitChoiceGroupOption Text=""SpaceBetween"" Value=""BitAlignment.SpaceBetween"" />
    <BitChoiceGroupOption Text=""SpaceAround"" Value=""BitAlignment.SpaceAround"" />
    <BitChoiceGroupOption Text=""SpaceEvenly"" Value=""BitAlignment.SpaceEvenly"" />
</BitChoiceGroup>

<BitGrid Columns=""12"" HorizontalAlign=""horizontalAlign"">
    @for (int i = 0; i < 4; i++)
    {
        var item = i + 1;

        <BitGridItem Class=""grid-item"" ColumnSpan=""2"">
            Item @item
        </BitGridItem>
    }
</BitGrid>


<BitChoiceGroup @bind-Value=""verticalAlign""
                Horizontal
                Label=""VerticalAlign""
                TItem=""BitChoiceGroupOption<BitAlignment>"" TValue=""BitAlignment"">
    <BitChoiceGroupOption Text=""Start"" Value=""BitAlignment.Start"" />
    <BitChoiceGroupOption Text=""Center"" Value=""BitAlignment.Center"" />
    <BitChoiceGroupOption Text=""End"" Value=""BitAlignment.End"" />
    <BitChoiceGroupOption Text=""Baseline"" Value=""BitAlignment.Baseline"" />
    <BitChoiceGroupOption Text=""Stretch"" Value=""BitAlignment.Stretch"" />
</BitChoiceGroup>

<BitGrid Columns=""4"" VerticalAlign=""verticalAlign"">
    <BitGridItem Class=""grid-item tall"">A tall item</BitGridItem>
    <BitGridItem Class=""grid-item"">Short</BitGridItem>
    <BitGridItem Class=""grid-item medium"">A medium item</BitGridItem>
    <BitGridItem Class=""grid-item"">Short</BitGridItem>
</BitGrid>


<BitGrid Columns=""4"" VerticalAlign=""BitAlignment.Start"">
    <BitGridItem Class=""grid-item tall"">A tall item</BitGridItem>
    <BitGridItem Class=""grid-item"">Start (from the grid)</BitGridItem>
    <BitGridItem Class=""grid-item"" AlignSelf=""BitAlignment.Center"">AlignSelf Center</BitGridItem>
    <BitGridItem Class=""grid-item"" AlignSelf=""BitAlignment.End"">AlignSelf End</BitGridItem>
</BitGrid>

<BitGrid Columns=""12"" Alignment=""BitAlignment.Center"">
    <BitGridItem Class=""grid-item tall"" ColumnSpan=""3"">A tall item</BitGridItem>
    <BitGridItem Class=""grid-item"" ColumnSpan=""3"">Alignment Center</BitGridItem>
</BitGrid>

<BitGrid Columns=""12"" Alignment=""BitAlignment.Center"" HorizontalAlign=""BitAlignment.End"">
    <BitGridItem Class=""grid-item tall"" ColumnSpan=""3"">A tall item</BitGridItem>
    <BitGridItem Class=""grid-item"" ColumnSpan=""3"">HorizontalAlign End</BitGridItem>
</BitGrid>


<style>
    .tall-grid {
        height: 320px;
    }
</style>

<BitChoiceGroup @bind-Value=""alignContent""
                Horizontal
                Label=""AlignContent""
                TItem=""BitChoiceGroupOption<BitAlignment>"" TValue=""BitAlignment"">
    <BitChoiceGroupOption Text=""Start"" Value=""BitAlignment.Start"" />
    <BitChoiceGroupOption Text=""Center"" Value=""BitAlignment.Center"" />
    <BitChoiceGroupOption Text=""End"" Value=""BitAlignment.End"" />
    <BitChoiceGroupOption Text=""SpaceBetween"" Value=""BitAlignment.SpaceBetween"" />
    <BitChoiceGroupOption Text=""SpaceAround"" Value=""BitAlignment.SpaceAround"" />
    <BitChoiceGroupOption Text=""SpaceEvenly"" Value=""BitAlignment.SpaceEvenly"" />
    <BitChoiceGroupOption Text=""Stretch"" Value=""BitAlignment.Stretch"" />
</BitChoiceGroup>

<BitGrid Columns=""3"" AlignContent=""alignContent"" Class=""tall-grid"">
    @for (int i = 0; i < 6; i++)
    {
        var item = i + 1;

        <BitGridItem Class=""grid-item"">Item @item</BitGridItem>
    }
</BitGrid>";
    private readonly string example6CsharpCode = @"
private BitAlignment horizontalAlign = BitAlignment.Start;
private BitAlignment verticalAlign = BitAlignment.Start;
private BitAlignment alignContent = BitAlignment.Start;
";

    private readonly string example7RazorCode = @"
<BitSlider Label=""VerticalSpacing"" Max=""5"" ValueFormat=""0.0 rem"" Step=""0.1"" @bind-Value=""@verticalSpacing"" />

<BitSlider Label=""HorizontalSpacing"" Max=""5"" ValueFormat=""0.0 rem"" Step=""0.1"" @bind-Value=""@horizontalSpacing"" />

<BitGrid Columns=""4""
         VerticalSpacing=""@($""{verticalSpacing}rem"")""
         HorizontalSpacing=""@($""{horizontalSpacing}rem"")"">
    @for (int i = 0; i < 12; i++)
    {
        var item = i + 1;

        <BitGridItem Class=""grid-item"">
            Item @item
        </BitGridItem>
    }
</BitGrid>

<BitGrid Columns=""4"" Spacing=""clamp(4px, 2vw, 24px)"">
    @for (int i = 0; i < 8; i++)
    {
        var item = i + 1;

        <BitGridItem Class=""grid-item"">Item @item</BitGridItem>
    }
</BitGrid>

<BitGrid Columns=""4"" Spacing=""0.25rem"" SpacingSm=""0.75rem"" SpacingLg=""1.5rem"">
    @for (int i = 0; i < 8; i++)
    {
        var item = i + 1;

        <BitGridItem Class=""grid-item"">Item @item</BitGridItem>
    }
</BitGrid>

<BitGrid Columns=""4"" Spacing=""0.5rem"" VerticalSpacingMd=""2rem"">
    @for (int i = 0; i < 8; i++)
    {
        var item = i + 1;

        <BitGridItem Class=""grid-item"">Item @item</BitGridItem>
    }
</BitGrid>";
    private readonly string example7CsharpCode = @"
private double verticalSpacing = 0.5;
private double horizontalSpacing = 0.5;
";

    private readonly string example8RazorCode = @"
<BitGrid Columns=""2"" ColumnsSm=""3"" ColumnsMd=""4"" ColumnsLg=""6"">
    @for (int i = 0; i < 12; i++)
    {
        var item = i + 1;

        <BitGridItem Class=""grid-item"">
            Item @item
        </BitGridItem>
    }
</BitGrid>


<BitGrid Columns=""12"">
    <BitGridItem Class=""grid-item"" ColumnSpan=""12"" Md=""8"">12 columns, then 8 from Md</BitGridItem>
    <BitGridItem Class=""grid-item"" ColumnSpan=""12"" Md=""4"">12 columns, then 4 from Md</BitGridItem>
    <BitGridItem Class=""grid-item"" ColumnSpan=""12"" Sm=""6"" Lg=""3"">12, then 6 from Sm, then 3 from Lg</BitGridItem>
    <BitGridItem Class=""grid-item"" ColumnSpan=""12"" Sm=""6"" Lg=""3"">12, then 6 from Sm, then 3 from Lg</BitGridItem>
    <BitGridItem Class=""grid-item"" ColumnSpan=""12"" Sm=""6"" Lg=""3"">12, then 6 from Sm, then 3 from Lg</BitGridItem>
    <BitGridItem Class=""grid-item"" ColumnSpan=""12"" Sm=""6"" Lg=""3"">12, then 6 from Sm, then 3 from Lg</BitGridItem>
</BitGrid>


<BitGrid Columns=""12"">
    <BitGridItem Class=""grid-item"" ColumnSpan=""12"" Md=""7"" OffsetMd=""1"" Order=""0"" OrderMd=""0"">Content (written first, painted second below Md)</BitGridItem>
    <BitGridItem Class=""grid-item"" ColumnSpan=""12"" Md=""4"" Order=""-1"" OrderMd=""1"">Sidebar (written second, painted first below Md)</BitGridItem>
</BitGrid>";

    private readonly string example9RazorCode = @"
<BitGrid Columns=""12"" Spacing=""0.5rem"">
    <BitGridItem Class=""grid-item"" ColumnSpan=""12"" Md=""8"">Always shown</BitGridItem>
    <BitGridItem Class=""grid-item"" ColumnSpan=""0"" Md=""4"">Hidden below Md</BitGridItem>
    <BitGridItem Class=""grid-item"" ColumnSpan=""12"" Md=""0"">Hidden from Md</BitGridItem>
</BitGrid>

<BitGrid Columns=""12"" Spacing=""0.5rem"">
    <BitGridItem Class=""grid-item"" ColumnSpan=""6"">Always shown</BitGridItem>
    <BitGridItem Class=""grid-item"" ColumnSpan=""6"" Sm=""0"" Md=""6"">Hidden between Sm and Md</BitGridItem>
</BitGrid>";

    private readonly string example10RazorCode = @"
<BitSlider Label=""MinItemWidth"" Min=""6"" Max=""24"" Step=""1"" ValueFormat=""0 rem"" @bind-Value=""@minItemWidth"" />

<BitGrid Spacing=""0.5rem"" MinItemWidth=""@($""{minItemWidth}rem"")"">
    @for (int i = 0; i < 8; i++)
    {
        var item = i + 1;

        <BitGridItem Class=""grid-item"">Item @item</BitGridItem>
    }
</BitGrid>

<BitGrid Spacing=""0.5rem"" MinItemWidth=""10rem"">
    <BitGridItem Class=""grid-item"" Auto>Auto</BitGridItem>
    <BitGridItem Class=""grid-item"">Fluid</BitGridItem>
    <BitGridItem Class=""grid-item"">Fluid</BitGridItem>
    <BitGridItem Class=""grid-item"">Fluid</BitGridItem>
</BitGrid>";
    private readonly string example10CsharpCode = @"
private double minItemWidth = 10;
";

    private readonly string example11RazorCode = @"
<style>
    .resizable {
        resize: horizontal;
        overflow: auto;
        max-width: 100%;
        min-width: 200px;
        padding: 0.5rem;
        border: 1px dashed gray;
    }
</style>


<div class=""resizable"">
    <div>Container: laid out by the width of this box</div>

    <BitGrid Container Columns=""12"" Spacing=""0.5rem"">
        @for (int i = 0; i < 4; i++)
        {
            var item = i + 1;

            <BitGridItem Class=""grid-item"" ColumnSpan=""12"" Sm=""6"" Md=""4"" Lg=""3"">Item @item</BitGridItem>
        }
    </BitGrid>
</div>

<div class=""resizable"">
    <div>The same grid without it: laid out by the width of the window</div>

    <BitGrid Columns=""12"" Spacing=""0.5rem"">
        @for (int i = 0; i < 4; i++)
        {
            var item = i + 1;

            <BitGridItem Class=""grid-item"" ColumnSpan=""12"" Sm=""6"" Md=""4"" Lg=""3"">Item @item</BitGridItem>
        }
    </BitGrid>
</div>";

    private readonly string example12RazorCode = @"
<BitGrid Columns=""4"">
    @for (int i = 0; i < 6; i++)
    {
        var item = i + 1;

        <BitGridItem Class=""grid-item"">Item @item</BitGridItem>
    }
</BitGrid>

<BitGrid Columns=""4"" NoWrap>
    @for (int i = 0; i < 6; i++)
    {
        var item = i + 1;

        <BitGridItem Class=""grid-item"">Item @item</BitGridItem>
    }
</BitGrid>

<BitGrid Columns=""4"" Reversed>
    @for (int i = 0; i < 6; i++)
    {
        var item = i + 1;

        <BitGridItem Class=""grid-item"">Item @item</BitGridItem>
    }
</BitGrid>";

    private readonly string example13RazorCode = @"
<BitGrid Columns=""12"">
    <BitGridItem Class=""grid-item"" ColumnSpan=""8"">
        <div>Outer item, span 8, holding a grid of its own</div>

        <BitGrid Columns=""3"" Spacing=""0.5rem"">
            <BitGridItem Class=""grid-item nested"">Nested 1</BitGridItem>
            <BitGridItem Class=""grid-item nested"">Nested 2</BitGridItem>
            <BitGridItem Class=""grid-item nested"">Nested 3</BitGridItem>
            <BitGridItem Class=""grid-item nested"" ColumnSpan=""2"">Nested span 2</BitGridItem>
            <BitGridItem Class=""grid-item nested"">Nested 5</BitGridItem>
        </BitGrid>
    </BitGridItem>
    <BitGridItem Class=""grid-item"" ColumnSpan=""4"">Outer item, span 4</BitGridItem>
</BitGrid>";

    private readonly string example14RazorCode = @"
<style>
    .plain-list {
        margin: 0;
        padding: 0;
        list-style: none;
    }
</style>


<BitGrid Columns=""4"" Element=""ul"" Class=""plain-list"" role=""list"">
    @foreach (var fruit in fruits)
    {
        <BitGridItem Class=""grid-item"" Element=""li"">@fruit</BitGridItem>
    }
</BitGrid>";
    private readonly string example14CsharpCode = @"
private readonly string[] fruits = [""Apple"", ""Banana"", ""Cherry"", ""Date"", ""Elderberry"", ""Fig"", ""Grape"", ""Honeydew""];
";

    private readonly string example15RazorCode = @"
<BitGrid Columns=""12"" Spacing=""0.5rem"" VerticalAlign=""BitAlignment.Center"">
    <BitGridItem Class=""grid-item"" Auto>Name</BitGridItem>
    <BitGridItem Class=""grid-item"" Grow>The field</BitGridItem>
    <BitGridItem Class=""grid-item"" Auto>Save</BitGridItem>
</BitGrid>

<BitGrid Columns=""12"" Spacing=""0.5rem"" VerticalAlign=""BitAlignment.Center"">
    <BitGridItem Class=""grid-item"" Auto>Email address</BitGridItem>
    <BitGridItem Class=""grid-item"" Grow>The field</BitGridItem>
    <BitGridItem Class=""grid-item"" Auto>Save</BitGridItem>
</BitGrid>


<BitGrid Columns=""12"" Spacing=""0.5rem"">
    <BitGridItem Class=""grid-item"" ColumnSpan=""12"" Order=""0"">Header</BitGridItem>
    <BitGridItem Class=""grid-item"" ColumnSpan=""0"" Md=""12"" Order=""1"">Breadcrumb (hidden below Md)</BitGridItem>
    <BitGridItem Class=""grid-item"" ColumnSpan=""12"" Md=""8"" Order=""3"" OrderMd=""2"">Content</BitGridItem>
    <BitGridItem Class=""grid-item"" ColumnSpan=""12"" Md=""4"" Order=""2"" OrderMd=""3"">Sidebar</BitGridItem>
    <BitGridItem Class=""grid-item"" ColumnSpan=""12"" Order=""4"">Footer</BitGridItem>
</BitGrid>


<BitGrid Columns=""12"" Spacing=""0.5rem"" NoWrap VerticalAlign=""BitAlignment.Center"">
    <BitGridItem Class=""grid-item"" Auto>Documents</BitGridItem>
    <BitGridItem Class=""grid-item"" Auto AutoOffset>Filter</BitGridItem>
    <BitGridItem Class=""grid-item"" Auto>New</BitGridItem>
</BitGrid>


<BitGrid Spacing=""0.5rem"" MinItemWidth=""11rem"">
    @for (int i = 0; i < 7; i++)
    {
        var item = i + 1;

        <BitGridItem Class=""grid-item"">Card @item</BitGridItem>
    }
</BitGrid>";

    private readonly string example16RazorCode = @"
<style>
    .custom-item {
        color: white;
        border-color: transparent;
        background: rebeccapurple;
    }
</style>


<BitGrid Columns=""4"" Style=""border: 1px solid var(--bit-clr-brd-pri); padding: 0.5rem;"">
    <BitGridItem Class=""grid-item custom-item"">Class</BitGridItem>
    <BitGridItem Class=""grid-item"" Style=""background: var(--bit-clr-inf); color: var(--bit-clr-inf-text);"">Style</BitGridItem>
    <BitGridItem Class=""grid-item"" ColumnSpan=""2"" Style=""background: var(--bit-clr-suc); color: var(--bit-clr-suc-text);"">Style, span 2</BitGridItem>
</BitGrid>";

    private readonly string example17RazorCode = @"
<BitGrid Dir=""BitDir.Rtl"" Columns=""12"">
    <BitGridItem Class=""grid-item"" ColumnSpan=""4"">راست‌چین ۴</BitGridItem>
    <BitGridItem Class=""grid-item"" ColumnSpan=""4"" Offset=""4"">راست‌چین ۴ با فاصله ۴</BitGridItem>
    <BitGridItem Class=""grid-item"" ColumnSpan=""6"" Offset=""3"">راست‌چین ۶ در وسط</BitGridItem>
</BitGrid>";
}
