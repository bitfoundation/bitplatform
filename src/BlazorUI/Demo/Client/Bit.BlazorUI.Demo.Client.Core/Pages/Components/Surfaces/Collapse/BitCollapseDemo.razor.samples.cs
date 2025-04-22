namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Surfaces.Collapse;

public partial class BitCollapseDemo
{
    private readonly string example1RazorCode = @"
<BitToggleButton OnText=""Collapse"" OffText=""Expand"" @bind-IsChecked=""expanded"" />
<BitCollapse Expanded=""expanded"">
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
</BitCollapse>";
    private readonly string example1CsharpCode = @"
private bool expanded = true;";

    private readonly string example2RazorCode = @"
<style>
    .custom-expanded {
        padding: 10px;
        background-color: #808080;
        border: 1px solid #0054C6;
    }
</style>

<BitToggleButton OnText=""Collapse"" OffText=""Expand"" @bind-IsChecked=""expandedStyle"" />
<BitCollapse Expanded Styles=""@(new() { Expanded = ""padding:10px;background-color:#333;border: 1px solid #ff0000;"" })"">
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
</BitCollapse>

<BitToggleButton OnText=""Collapse"" OffText=""Expand"" @bind-IsChecked=""expandedClass"" />
<BitCollapse Expanded Classes=""@(new() { Expanded = ""custom-expanded"" })"">
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
</BitCollapse>";
    private readonly string example2CsharpCode = @"
private bool expandedClass = true;
private bool expandedStyle = true;";

    private readonly string example3RazorCode = @"
<BitToggleButton OnText=""بستن"" OffText=""باز کردن"" @bind-IsChecked=""expandedRtl"" />
<BitCollapse Expanded=""expandedRtl"" Dir=""BitDir.Rtl"">
    لورم ایپسوم متن ساختگی با تولید سادگی نامفهوم از صنعت چاپ و با استفاده از طراحان گرافیک است.
    چاپگرها و متون بلکه روزنامه و مجله در ستون و سطرآنچنان که لازم است
    و برای شرایط فعلی تکنولوژی مورد نیاز و کاربردهای متنوع با هدف بهبود ابزارهای کاربردی می باشد
</BitCollapse>";
    private readonly string example3CsharpCode = @"
private bool expandedRtl = true;";
}
