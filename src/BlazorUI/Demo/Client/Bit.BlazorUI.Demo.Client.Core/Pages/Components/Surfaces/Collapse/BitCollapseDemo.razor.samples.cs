namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Surfaces.Collapse;

public partial class BitCollapseDemo
{
    private readonly string example1RazorCode = @"
<BitToggleButton OnText=""Collapse"" OffText=""Expand"" @bind-IsChecked=""expanded"" />
<BitCollapse Expanded=""expanded"">
    Lorem ipsum dolor sit amet, consectetur adipiscing elit. Duis a elit vel lacus tincidunt dignissim. Phasellus mollis mauris orci, eget fermentum diam porta eu. Integer a consequat sapien, pellentesque aliquam velit. Nullam quis ligula vitae nisi accumsan auctor. Ut faucibus nulla a est commodo, vel sagittis neque tristique. In nec urna hendrerit, iaculis turpis sed, dictum elit. Sed id sagittis nunc, vitae ornare elit. Sed consequat condimentum massa, non euismod magna gravida vitae. Donec rhoncus suscipit blandit. Nunc ultrices vulputate nisl. Duis lobortis tristique nunc, id egestas ligula condimentum quis. Integer elementum tempor cursus. Phasellus vestibulum neque non laoreet faucibus. Nunc eu congue urna, in dapibus justo.
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
    Lorem ipsum dolor sit amet, consectetur adipiscing elit. Duis a elit vel lacus tincidunt dignissim. Phasellus mollis mauris orci, eget fermentum diam porta eu. Integer a consequat sapien, pellentesque aliquam velit. Nullam quis ligula vitae nisi accumsan auctor. Ut faucibus nulla a est commodo, vel sagittis neque tristique. In nec urna hendrerit, iaculis turpis sed, dictum elit. Sed id sagittis nunc, vitae ornare elit. Sed consequat condimentum massa, non euismod magna gravida vitae. Donec rhoncus suscipit blandit. Nunc ultrices vulputate nisl. Duis lobortis tristique nunc, id egestas ligula condimentum quis. Integer elementum tempor cursus. Phasellus vestibulum neque non laoreet faucibus. Nunc eu congue urna, in dapibus justo.
</BitCollapse>

<BitToggleButton OnText=""Collapse"" OffText=""Expand"" @bind-IsChecked=""expandedClass"" />
<BitCollapse Expanded Classes=""@(new() { Expanded = ""custom-expanded"" })"">
    Lorem ipsum dolor sit amet, consectetur adipiscing elit. Duis a elit vel lacus tincidunt dignissim. Phasellus mollis mauris orci, eget fermentum diam porta eu. Integer a consequat sapien, pellentesque aliquam velit. Nullam quis ligula vitae nisi accumsan auctor. Ut faucibus nulla a est commodo, vel sagittis neque tristique. In nec urna hendrerit, iaculis turpis sed, dictum elit. Sed id sagittis nunc, vitae ornare elit. Sed consequat condimentum massa, non euismod magna gravida vitae. Donec rhoncus suscipit blandit. Nunc ultrices vulputate nisl. Duis lobortis tristique nunc, id egestas ligula condimentum quis. Integer elementum tempor cursus. Phasellus vestibulum neque non laoreet faucibus. Nunc eu congue urna, in dapibus justo.
</BitCollapse>";
    private readonly string example2CsharpCode = @"
private bool expandedClass = true;
private bool expandedStyle = true;";

    private readonly string example3RazorCode = @"
<BitToggleButton OnText=""بستن"" OffText=""باز کردن"" @bind-IsChecked=""expandedRtl"" />
<BitCollapse Expanded=""expandedRtl"" Dir=""BitDir.Rtl"">
    لورم ایپسوم متن ساختگی با تولید سادگی نامفهوم از صنعت چاپ و با استفاده از طراحان گرافیک است. چاپگرها و متون بلکه روزنامه و مجله در ستون و سطرآنچنان که لازم است و برای شرایط فعلی تکنولوژی مورد نیاز و کاربردهای متنوع با هدف بهبود ابزارهای کاربردی می باشد.
</BitCollapse>";
    private readonly string example3CsharpCode = @"
private bool expandedRtl = true;";
}
