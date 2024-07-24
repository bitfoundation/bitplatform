namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Buttons.MenuButton;

public partial class _BitMenuButtonOptionDemo
{
    private string? exampleChangedOption;
    private string? exampleClickedOption;

    private BitMenuButtonOption twoWaySelectedOption = default!;

    private BitMenuButtonOption optionA = default!;
    private BitMenuButtonOption optionB = default!;
    private BitMenuButtonOption optionC = default!;



    private readonly string example1RazorCode = @"
<BitMenuButton Text=""MenuButton"" TItem=""BitMenuButtonOption"">
    <BitMenuButtonOption Text=""Option A"" Key=""A"" />
    <BitMenuButtonOption Text=""Option B"" Key=""B"" IsEnabled=""false"" />
    <BitMenuButtonOption Text=""Option C"" Key=""C"" />
</BitMenuButton>";

    private readonly string example2RazorCode = @"
<BitMenuButton Text=""Split"" TItem=""BitMenuButtonOption"" Split>
    <BitMenuButtonOption Text=""Option A"" Key=""A"" />
    <BitMenuButtonOption Text=""Option B"" Key=""B"" IsEnabled=""false"" />
    <BitMenuButtonOption Text=""Option C"" Key=""C"" />
</BitMenuButton>";

    private readonly string example3RazorCode = @"
<BitMenuButton Text=""Fill"" TItem=""BitMenuButtonOption"" Variant=""BitVariant.Fill"">
    <BitMenuButtonOption Text=""Option A"" Key=""A"" />
    <BitMenuButtonOption Text=""Option B"" Key=""B"" IsEnabled=""false"" />
    <BitMenuButtonOption Text=""Option C"" Key=""C"" />
</BitMenuButton>
<BitMenuButton Text=""Outline"" TItem=""BitMenuButtonOption"" Variant=""BitVariant.Outline"">
    <BitMenuButtonOption Text=""Option A"" Key=""A"" />
    <BitMenuButtonOption Text=""Option B"" Key=""B"" IsEnabled=""false"" />
    <BitMenuButtonOption Text=""Option C"" Key=""C"" />
</BitMenuButton>
<BitMenuButton Text=""Text"" TItem=""BitMenuButtonOption"" Variant=""BitVariant.Text"">
    <BitMenuButtonOption Text=""Option A"" Key=""A"" />
    <BitMenuButtonOption Text=""Option B"" Key=""B"" IsEnabled=""false"" />
    <BitMenuButtonOption Text=""Option C"" Key=""C"" />
</BitMenuButton>

<BitMenuButton Text=""Fill"" TItem=""BitMenuButtonOption"" Variant=""BitVariant.Fill"" IsEnabled=""false"">
    <BitMenuButtonOption Text=""Option A"" Key=""A"" />
    <BitMenuButtonOption Text=""Option B"" Key=""B"" />
    <BitMenuButtonOption Text=""Option C"" Key=""C"" />
</BitMenuButton>
<BitMenuButton Text=""Outline"" TItem=""BitMenuButtonOption"" Variant=""BitVariant.Outline"" IsEnabled=""false"">
    <BitMenuButtonOption Text=""Option A"" Key=""A"" />
    <BitMenuButtonOption Text=""Option B"" Key=""B"" />
    <BitMenuButtonOption Text=""Option C"" Key=""C"" />
</BitMenuButton>
<BitMenuButton Text=""Text"" TItem=""BitMenuButtonOption"" Variant=""BitVariant.Text"" IsEnabled=""false"">
    <BitMenuButtonOption Text=""Option A"" Key=""A"" />
    <BitMenuButtonOption Text=""Option B"" Key=""B"" />
    <BitMenuButtonOption Text=""Option C"" Key=""C"" />
</BitMenuButton>


<BitMenuButton Text=""Fill"" TItem=""BitMenuButtonOption"" Variant=""BitVariant.Fill"" Split>
    <BitMenuButtonOption Text=""Option A"" Key=""A"" />
    <BitMenuButtonOption Text=""Option B"" Key=""B"" IsEnabled=""false"" />
    <BitMenuButtonOption Text=""Option C"" Key=""C"" />
</BitMenuButton>
<BitMenuButton Text=""Outline"" TItem=""BitMenuButtonOption"" Variant=""BitVariant.Outline"" Split>
    <BitMenuButtonOption Text=""Option A"" Key=""A"" />
    <BitMenuButtonOption Text=""Option B"" Key=""B"" IsEnabled=""false"" />
    <BitMenuButtonOption Text=""Option C"" Key=""C"" />
</BitMenuButton>
<BitMenuButton Text=""Text"" TItem=""BitMenuButtonOption"" Variant=""BitVariant.Text"" Split>
    <BitMenuButtonOption Text=""Option A"" Key=""A"" />
    <BitMenuButtonOption Text=""Option B"" Key=""B"" IsEnabled=""false"" />
    <BitMenuButtonOption Text=""Option C"" Key=""C"" />
</BitMenuButton>

<BitMenuButton Text=""Fill"" TItem=""BitMenuButtonOption"" Variant=""BitVariant.Fill"" IsEnabled=""false"" Split>
    <BitMenuButtonOption Text=""Option A"" Key=""A"" />
    <BitMenuButtonOption Text=""Option B"" Key=""B"" />
    <BitMenuButtonOption Text=""Option C"" Key=""C"" />
</BitMenuButton>
<BitMenuButton Text=""Outline"" TItem=""BitMenuButtonOption"" Variant=""BitVariant.Outline"" IsEnabled=""false"" Split>
    <BitMenuButtonOption Text=""Option A"" Key=""A"" />
    <BitMenuButtonOption Text=""Option B"" Key=""B"" />
    <BitMenuButtonOption Text=""Option C"" Key=""C"" />
</BitMenuButton>
<BitMenuButton Text=""Text"" TItem=""BitMenuButtonOption"" Variant=""BitVariant.Text"" IsEnabled=""false"" Split>
    <BitMenuButtonOption Text=""Option A"" Key=""A"" />
    <BitMenuButtonOption Text=""Option B"" Key=""B"" />
    <BitMenuButtonOption Text=""Option C"" Key=""C"" />
</BitMenuButton>";

    private readonly string example4RazorCode = @"
<BitMenuButton Text=""Primary"" TItem=""BitMenuButtonOption"" Variant=""BitVariant.Fill"" Color=""BitColor.Primary"">
    <BitMenuButtonOption Text=""Option A"" Key=""A"" />
    <BitMenuButtonOption Text=""Option B"" Key=""B"" IsEnabled=""false"" />
    <BitMenuButtonOption Text=""Option C"" Key=""C"" />
</BitMenuButton>
<BitMenuButton Text=""Primary"" TItem=""BitMenuButtonOption"" Variant=""BitVariant.Outline"" Color=""BitColor.Primary"">
    <BitMenuButtonOption Text=""Option A"" Key=""A"" />
    <BitMenuButtonOption Text=""Option B"" Key=""B"" IsEnabled=""false"" />
    <BitMenuButtonOption Text=""Option C"" Key=""C"" />
</BitMenuButton>
<BitMenuButton Text=""Primary"" TItem=""BitMenuButtonOption"" Variant=""BitVariant.Text"" Color=""BitColor.Primary"">
    <BitMenuButtonOption Text=""Option A"" Key=""A"" />
    <BitMenuButtonOption Text=""Option B"" Key=""B"" IsEnabled=""false"" />
    <BitMenuButtonOption Text=""Option C"" Key=""C"" />
</BitMenuButton>

<BitMenuButton Text=""Primary"" TItem=""BitMenuButtonOption"" Variant=""BitVariant.Fill"" Color=""BitColor.Primary"" Split>
    <BitMenuButtonOption Text=""Option A"" Key=""A"" />
    <BitMenuButtonOption Text=""Option B"" Key=""B"" IsEnabled=""false"" />
    <BitMenuButtonOption Text=""Option C"" Key=""C"" />
</BitMenuButton>
<BitMenuButton Text=""Primary"" TItem=""BitMenuButtonOption"" Variant=""BitVariant.Outline"" Color=""BitColor.Primary"" Split>
    <BitMenuButtonOption Text=""Option A"" Key=""A"" />
    <BitMenuButtonOption Text=""Option B"" Key=""B"" IsEnabled=""false"" />
    <BitMenuButtonOption Text=""Option C"" Key=""C"" />
</BitMenuButton>
<BitMenuButton Text=""Primary"" TItem=""BitMenuButtonOption"" Variant=""BitVariant.Text"" Color=""BitColor.Primary"" Split>
    <BitMenuButtonOption Text=""Option A"" Key=""A"" />
    <BitMenuButtonOption Text=""Option B"" Key=""B"" IsEnabled=""false"" />
    <BitMenuButtonOption Text=""Option C"" Key=""C"" />
</BitMenuButton>


<BitMenuButton Text=""Secondary"" TItem=""BitMenuButtonOption"" Variant=""BitVariant.Fill"" Color=""BitColor.Secondary"">
    <BitMenuButtonOption Text=""Option A"" Key=""A"" />
    <BitMenuButtonOption Text=""Option B"" Key=""B"" IsEnabled=""false"" />
    <BitMenuButtonOption Text=""Option C"" Key=""C"" />
</BitMenuButton>
<BitMenuButton Text=""Secondary"" TItem=""BitMenuButtonOption"" Variant=""BitVariant.Outline"" Color=""BitColor.Secondary"">
    <BitMenuButtonOption Text=""Option A"" Key=""A"" />
    <BitMenuButtonOption Text=""Option B"" Key=""B"" IsEnabled=""false"" />
    <BitMenuButtonOption Text=""Option C"" Key=""C"" />
</BitMenuButton>
<BitMenuButton Text=""Secondary"" TItem=""BitMenuButtonOption"" Variant=""BitVariant.Text"" Color=""BitColor.Secondary"">
    <BitMenuButtonOption Text=""Option A"" Key=""A"" />
    <BitMenuButtonOption Text=""Option B"" Key=""B"" IsEnabled=""false"" />
    <BitMenuButtonOption Text=""Option C"" Key=""C"" />
</BitMenuButton>

<BitMenuButton Text=""Secondary"" TItem=""BitMenuButtonOption"" Variant=""BitVariant.Fill"" Color=""BitColor.Secondary"" Split>
    <BitMenuButtonOption Text=""Option A"" Key=""A"" />
    <BitMenuButtonOption Text=""Option B"" Key=""B"" IsEnabled=""false"" />
    <BitMenuButtonOption Text=""Option C"" Key=""C"" />
</BitMenuButton>
<BitMenuButton Text=""Secondary"" TItem=""BitMenuButtonOption"" Variant=""BitVariant.Outline"" Color=""BitColor.Secondary"" Split>
    <BitMenuButtonOption Text=""Option A"" Key=""A"" />
    <BitMenuButtonOption Text=""Option B"" Key=""B"" IsEnabled=""false"" />
    <BitMenuButtonOption Text=""Option C"" Key=""C"" />
</BitMenuButton>
<BitMenuButton Text=""Secondary"" TItem=""BitMenuButtonOption"" Variant=""BitVariant.Text"" Color=""BitColor.Secondary"" Split>
    <BitMenuButtonOption Text=""Option A"" Key=""A"" />
    <BitMenuButtonOption Text=""Option B"" Key=""B"" IsEnabled=""false"" />
    <BitMenuButtonOption Text=""Option C"" Key=""C"" />
</BitMenuButton>


<BitMenuButton Text=""Tertiary"" TItem=""BitMenuButtonOption"" Variant=""BitVariant.Fill"" Color=""BitColor.Tertiary"">
    <BitMenuButtonOption Text=""Option A"" Key=""A"" />
    <BitMenuButtonOption Text=""Option B"" Key=""B"" IsEnabled=""false"" />
    <BitMenuButtonOption Text=""Option C"" Key=""C"" />
</BitMenuButton>
<BitMenuButton Text=""Tertiary"" TItem=""BitMenuButtonOption"" Variant=""BitVariant.Outline"" Color=""BitColor.Tertiary"">
    <BitMenuButtonOption Text=""Option A"" Key=""A"" />
    <BitMenuButtonOption Text=""Option B"" Key=""B"" IsEnabled=""false"" />
    <BitMenuButtonOption Text=""Option C"" Key=""C"" />
</BitMenuButton>
<BitMenuButton Text=""Tertiary"" TItem=""BitMenuButtonOption"" Variant=""BitVariant.Text"" Color=""BitColor.Tertiary"">
    <BitMenuButtonOption Text=""Option A"" Key=""A"" />
    <BitMenuButtonOption Text=""Option B"" Key=""B"" IsEnabled=""false"" />
    <BitMenuButtonOption Text=""Option C"" Key=""C"" />
</BitMenuButton>

<BitMenuButton Text=""Tertiary"" TItem=""BitMenuButtonOption"" Variant=""BitVariant.Fill"" Color=""BitColor.Tertiary"" Split>
    <BitMenuButtonOption Text=""Option A"" Key=""A"" />
    <BitMenuButtonOption Text=""Option B"" Key=""B"" IsEnabled=""false"" />
    <BitMenuButtonOption Text=""Option C"" Key=""C"" />
</BitMenuButton>
<BitMenuButton Text=""Tertiary"" TItem=""BitMenuButtonOption"" Variant=""BitVariant.Outline"" Color=""BitColor.Tertiary"" Split>
    <BitMenuButtonOption Text=""Option A"" Key=""A"" />
    <BitMenuButtonOption Text=""Option B"" Key=""B"" IsEnabled=""false"" />
    <BitMenuButtonOption Text=""Option C"" Key=""C"" />
</BitMenuButton>
<BitMenuButton Text=""Tertiary"" TItem=""BitMenuButtonOption"" Variant=""BitVariant.Text"" Color=""BitColor.Tertiary"" Split>
    <BitMenuButtonOption Text=""Option A"" Key=""A"" />
    <BitMenuButtonOption Text=""Option B"" Key=""B"" IsEnabled=""false"" />
    <BitMenuButtonOption Text=""Option C"" Key=""C"" />
</BitMenuButton>


<BitMenuButton Text=""Info"" TItem=""BitMenuButtonOption"" Variant=""BitVariant.Fill"" Color=""BitColor.Info"">
    <BitMenuButtonOption Text=""Option A"" Key=""A"" />
    <BitMenuButtonOption Text=""Option B"" Key=""B"" IsEnabled=""false"" />
    <BitMenuButtonOption Text=""Option C"" Key=""C"" />
</BitMenuButton>
<BitMenuButton Text=""Info"" TItem=""BitMenuButtonOption"" Variant=""BitVariant.Outline"" Color=""BitColor.Info"">
    <BitMenuButtonOption Text=""Option A"" Key=""A"" />
    <BitMenuButtonOption Text=""Option B"" Key=""B"" IsEnabled=""false"" />
    <BitMenuButtonOption Text=""Option C"" Key=""C"" />
</BitMenuButton>
<BitMenuButton Text=""Info"" TItem=""BitMenuButtonOption"" Variant=""BitVariant.Text"" Color=""BitColor.Info"">
    <BitMenuButtonOption Text=""Option A"" Key=""A"" />
    <BitMenuButtonOption Text=""Option B"" Key=""B"" IsEnabled=""false"" />
    <BitMenuButtonOption Text=""Option C"" Key=""C"" />
</BitMenuButton>

<BitMenuButton Text=""Info"" TItem=""BitMenuButtonOption"" Variant=""BitVariant.Fill"" Color=""BitColor.Info"" Split>
    <BitMenuButtonOption Text=""Option A"" Key=""A"" />
    <BitMenuButtonOption Text=""Option B"" Key=""B"" IsEnabled=""false"" />
    <BitMenuButtonOption Text=""Option C"" Key=""C"" />
</BitMenuButton>
<BitMenuButton Text=""Info"" TItem=""BitMenuButtonOption"" Variant=""BitVariant.Outline"" Color=""BitColor.Info"" Split>
    <BitMenuButtonOption Text=""Option A"" Key=""A"" />
    <BitMenuButtonOption Text=""Option B"" Key=""B"" IsEnabled=""false"" />
    <BitMenuButtonOption Text=""Option C"" Key=""C"" />
</BitMenuButton>
<BitMenuButton Text=""Info"" TItem=""BitMenuButtonOption"" Variant=""BitVariant.Text"" Color=""BitColor.Info"" Split>
    <BitMenuButtonOption Text=""Option A"" Key=""A"" />
    <BitMenuButtonOption Text=""Option B"" Key=""B"" IsEnabled=""false"" />
    <BitMenuButtonOption Text=""Option C"" Key=""C"" />
</BitMenuButton>


<BitMenuButton Text=""Success"" TItem=""BitMenuButtonOption"" Variant=""BitVariant.Fill"" Color=""BitColor.Success"">
    <BitMenuButtonOption Text=""Option A"" Key=""A"" />
    <BitMenuButtonOption Text=""Option B"" Key=""B"" IsEnabled=""false"" />
    <BitMenuButtonOption Text=""Option C"" Key=""C"" />
</BitMenuButton>
<BitMenuButton Text=""Success"" TItem=""BitMenuButtonOption"" Variant=""BitVariant.Outline"" Color=""BitColor.Success"">
    <BitMenuButtonOption Text=""Option A"" Key=""A"" />
    <BitMenuButtonOption Text=""Option B"" Key=""B"" IsEnabled=""false"" />
    <BitMenuButtonOption Text=""Option C"" Key=""C"" />
</BitMenuButton>
<BitMenuButton Text=""Success"" TItem=""BitMenuButtonOption"" Variant=""BitVariant.Text"" Color=""BitColor.Success"">
    <BitMenuButtonOption Text=""Option A"" Key=""A"" />
    <BitMenuButtonOption Text=""Option B"" Key=""B"" IsEnabled=""false"" />
    <BitMenuButtonOption Text=""Option C"" Key=""C"" />
</BitMenuButton>

<BitMenuButton Text=""Success"" TItem=""BitMenuButtonOption"" Variant=""BitVariant.Fill"" Color=""BitColor.Success"" Split>
    <BitMenuButtonOption Text=""Option A"" Key=""A"" />
    <BitMenuButtonOption Text=""Option B"" Key=""B"" IsEnabled=""false"" />
    <BitMenuButtonOption Text=""Option C"" Key=""C"" />
</BitMenuButton>
<BitMenuButton Text=""Success"" TItem=""BitMenuButtonOption"" Variant=""BitVariant.Outline"" Color=""BitColor.Success"" Split>
    <BitMenuButtonOption Text=""Option A"" Key=""A"" />
    <BitMenuButtonOption Text=""Option B"" Key=""B"" IsEnabled=""false"" />
    <BitMenuButtonOption Text=""Option C"" Key=""C"" />
</BitMenuButton>
<BitMenuButton Text=""Success"" TItem=""BitMenuButtonOption"" Variant=""BitVariant.Text"" Color=""BitColor.Success"" Split>
    <BitMenuButtonOption Text=""Option A"" Key=""A"" />
    <BitMenuButtonOption Text=""Option B"" Key=""B"" IsEnabled=""false"" />
    <BitMenuButtonOption Text=""Option C"" Key=""C"" />
</BitMenuButton>


<BitMenuButton Text=""Warning"" TItem=""BitMenuButtonOption"" Variant=""BitVariant.Fill"" Color=""BitColor.Warning"">
    <BitMenuButtonOption Text=""Option A"" Key=""A"" />
    <BitMenuButtonOption Text=""Option B"" Key=""B"" IsEnabled=""false"" />
    <BitMenuButtonOption Text=""Option C"" Key=""C"" />
</BitMenuButton>
<BitMenuButton Text=""Warning"" TItem=""BitMenuButtonOption"" Variant=""BitVariant.Outline"" Color=""BitColor.Warning"">
    <BitMenuButtonOption Text=""Option A"" Key=""A"" />
    <BitMenuButtonOption Text=""Option B"" Key=""B"" IsEnabled=""false"" />
    <BitMenuButtonOption Text=""Option C"" Key=""C"" />
</BitMenuButton>
<BitMenuButton Text=""Warning"" TItem=""BitMenuButtonOption"" Variant=""BitVariant.Text"" Color=""BitColor.Warning"">
    <BitMenuButtonOption Text=""Option A"" Key=""A"" />
    <BitMenuButtonOption Text=""Option B"" Key=""B"" IsEnabled=""false"" />
    <BitMenuButtonOption Text=""Option C"" Key=""C"" />
</BitMenuButton>

<BitMenuButton Text=""Warning"" TItem=""BitMenuButtonOption"" Variant=""BitVariant.Fill"" Color=""BitColor.Warning"" Split>
    <BitMenuButtonOption Text=""Option A"" Key=""A"" />
    <BitMenuButtonOption Text=""Option B"" Key=""B"" IsEnabled=""false"" />
    <BitMenuButtonOption Text=""Option C"" Key=""C"" />
</BitMenuButton>
<BitMenuButton Text=""Warning"" TItem=""BitMenuButtonOption"" Variant=""BitVariant.Outline"" Color=""BitColor.Warning"" Split>
    <BitMenuButtonOption Text=""Option A"" Key=""A"" />
    <BitMenuButtonOption Text=""Option B"" Key=""B"" IsEnabled=""false"" />
    <BitMenuButtonOption Text=""Option C"" Key=""C"" />
</BitMenuButton>
<BitMenuButton Text=""Warning"" TItem=""BitMenuButtonOption"" Variant=""BitVariant.Text"" Color=""BitColor.Warning"" Split>
    <BitMenuButtonOption Text=""Option A"" Key=""A"" />
    <BitMenuButtonOption Text=""Option B"" Key=""B"" IsEnabled=""false"" />
    <BitMenuButtonOption Text=""Option C"" Key=""C"" />
</BitMenuButton>


<BitMenuButton Text=""SevereWarning"" TItem=""BitMenuButtonOption"" Variant=""BitVariant.Fill"" Color=""BitColor.SevereWarning"">
    <BitMenuButtonOption Text=""Option A"" Key=""A"" />
    <BitMenuButtonOption Text=""Option B"" Key=""B"" IsEnabled=""false"" />
    <BitMenuButtonOption Text=""Option C"" Key=""C"" />
</BitMenuButton>
<BitMenuButton Text=""SevereWarning"" TItem=""BitMenuButtonOption"" Variant=""BitVariant.Outline"" Color=""BitColor.SevereWarning"">
    <BitMenuButtonOption Text=""Option A"" Key=""A"" />
    <BitMenuButtonOption Text=""Option B"" Key=""B"" IsEnabled=""false"" />
    <BitMenuButtonOption Text=""Option C"" Key=""C"" />
</BitMenuButton>
<BitMenuButton Text=""SevereWarning"" TItem=""BitMenuButtonOption"" Variant=""BitVariant.Text"" Color=""BitColor.SevereWarning"">
    <BitMenuButtonOption Text=""Option A"" Key=""A"" />
    <BitMenuButtonOption Text=""Option B"" Key=""B"" IsEnabled=""false"" />
    <BitMenuButtonOption Text=""Option C"" Key=""C"" />
</BitMenuButton>

<BitMenuButton Text=""SevereWarning"" TItem=""BitMenuButtonOption"" Variant=""BitVariant.Fill"" Color=""BitColor.SevereWarning"" Split>
    <BitMenuButtonOption Text=""Option A"" Key=""A"" />
    <BitMenuButtonOption Text=""Option B"" Key=""B"" IsEnabled=""false"" />
    <BitMenuButtonOption Text=""Option C"" Key=""C"" />
</BitMenuButton>
<BitMenuButton Text=""SevereWarning"" TItem=""BitMenuButtonOption"" Variant=""BitVariant.Outline"" Color=""BitColor.SevereWarning"" Split>
    <BitMenuButtonOption Text=""Option A"" Key=""A"" />
    <BitMenuButtonOption Text=""Option B"" Key=""B"" IsEnabled=""false"" />
    <BitMenuButtonOption Text=""Option C"" Key=""C"" />
</BitMenuButton>
<BitMenuButton Text=""SevereWarning"" TItem=""BitMenuButtonOption"" Variant=""BitVariant.Text"" Color=""BitColor.SevereWarning"" Split>
    <BitMenuButtonOption Text=""Option A"" Key=""A"" />
    <BitMenuButtonOption Text=""Option B"" Key=""B"" IsEnabled=""false"" />
    <BitMenuButtonOption Text=""Option C"" Key=""C"" />
</BitMenuButton>


<BitMenuButton Text=""Error"" TItem=""BitMenuButtonOption"" Variant=""BitVariant.Fill"" Color=""BitColor.Error"">
    <BitMenuButtonOption Text=""Option A"" Key=""A"" />
    <BitMenuButtonOption Text=""Option B"" Key=""B"" IsEnabled=""false"" />
    <BitMenuButtonOption Text=""Option C"" Key=""C"" />
</BitMenuButton>
<BitMenuButton Text=""Error"" TItem=""BitMenuButtonOption"" Variant=""BitVariant.Outline"" Color=""BitColor.Error"">
    <BitMenuButtonOption Text=""Option A"" Key=""A"" />
    <BitMenuButtonOption Text=""Option B"" Key=""B"" IsEnabled=""false"" />
    <BitMenuButtonOption Text=""Option C"" Key=""C"" />
</BitMenuButton>
<BitMenuButton Text=""Error"" TItem=""BitMenuButtonOption"" Variant=""BitVariant.Text"" Color=""BitColor.Error"">
    <BitMenuButtonOption Text=""Option A"" Key=""A"" />
    <BitMenuButtonOption Text=""Option B"" Key=""B"" IsEnabled=""false"" />
    <BitMenuButtonOption Text=""Option C"" Key=""C"" />
</BitMenuButton>

<BitMenuButton Text=""Error"" TItem=""BitMenuButtonOption"" Variant=""BitVariant.Fill"" Color=""BitColor.Error"" Split>
    <BitMenuButtonOption Text=""Option A"" Key=""A"" />
    <BitMenuButtonOption Text=""Option B"" Key=""B"" IsEnabled=""false"" />
    <BitMenuButtonOption Text=""Option C"" Key=""C"" />
</BitMenuButton>
<BitMenuButton Text=""Error"" TItem=""BitMenuButtonOption"" Variant=""BitVariant.Outline"" Color=""BitColor.Error"" Split>
    <BitMenuButtonOption Text=""Option A"" Key=""A"" />
    <BitMenuButtonOption Text=""Option B"" Key=""B"" IsEnabled=""false"" />
    <BitMenuButtonOption Text=""Option C"" Key=""C"" />
</BitMenuButton>
<BitMenuButton Text=""Error"" TItem=""BitMenuButtonOption"" Variant=""BitVariant.Text"" Color=""BitColor.Error"" Split>
    <BitMenuButtonOption Text=""Option A"" Key=""A"" />
    <BitMenuButtonOption Text=""Option B"" Key=""B"" IsEnabled=""false"" />
    <BitMenuButtonOption Text=""Option C"" Key=""C"" />
</BitMenuButton>";

    private readonly string example5RazorCode = @"
<BitMenuButton TItem=""BitMenuButtonOption"" Variant=""BitVariant.Fill"" Sticky>
    <BitMenuButtonOption Text=""Option A"" Key=""A"" />
    <BitMenuButtonOption Text=""Option B"" Key=""B"" IsEnabled=""false"" />
    <BitMenuButtonOption Text=""Option C"" Key=""C"" />
</BitMenuButton>
<BitMenuButton TItem=""BitMenuButtonOption"" Variant=""BitVariant.Fill"" Split Sticky>
    <BitMenuButtonOption Text=""Option A"" Key=""A"" />
    <BitMenuButtonOption Text=""Option B"" Key=""B"" IsEnabled=""false"" />
    <BitMenuButtonOption Text=""Option C"" Key=""C"" />
</BitMenuButton>

<BitMenuButton TItem=""BitMenuButtonOption"" Variant=""BitVariant.Outline"" Sticky>
    <BitMenuButtonOption Text=""Option A"" Key=""A"" />
    <BitMenuButtonOption Text=""Option B"" Key=""B"" IsEnabled=""false"" />
    <BitMenuButtonOption Text=""Option C"" Key=""C"" />
</BitMenuButton>
<BitMenuButton TItem=""BitMenuButtonOption"" Variant=""BitVariant.Outline"" Split Sticky>
    <BitMenuButtonOption Text=""Option A"" Key=""A"" />
    <BitMenuButtonOption Text=""Option B"" Key=""B"" IsEnabled=""false"" />
    <BitMenuButtonOption Text=""Option C"" Key=""C"" />
</BitMenuButton>

<BitMenuButton TItem=""BitMenuButtonOption"" Variant=""BitVariant.Text"" Sticky>
    <BitMenuButtonOption Text=""Option A"" Key=""A"" />
    <BitMenuButtonOption Text=""Option B"" Key=""B"" IsEnabled=""false"" />
    <BitMenuButtonOption Text=""Option C"" Key=""C"" />
</BitMenuButton>
<BitMenuButton TItem=""BitMenuButtonOption"" Variant=""BitVariant.Text"" Split Sticky>
    <BitMenuButtonOption Text=""Option A"" Key=""A"" />
    <BitMenuButtonOption Text=""Option B"" Key=""B"" IsEnabled=""false"" />
    <BitMenuButtonOption Text=""Option C"" Key=""C"" />
</BitMenuButton>";

    private readonly string example6RazorCode = @"
<BitMenuButton Text=""IconName"" IconName=""@BitIconName.Edit"" TItem=""BitMenuButtonOption"">
    <BitMenuButtonOption Text=""Option A"" Key=""A"" IconName=""@BitIconName.Emoji"" />
    <BitMenuButtonOption Text=""Option B"" Key=""B"" IconName=""@BitIconName.Emoji"" IsEnabled=""false"" />
    <BitMenuButtonOption Text=""Option C"" Key=""C"" IconName=""@BitIconName.Emoji2"" />
</BitMenuButton>

<BitMenuButton Text=""ChevronDownIcon"" TItem=""BitMenuButtonOption"" ChevronDownIcon=""@BitIconName.DoubleChevronDown"" Split>
    <BitMenuButtonOption Text=""Option A"" Key=""A"" IconName=""@BitIconName.Emoji"" />
    <BitMenuButtonOption Text=""Option B"" Key=""B"" IconName=""@BitIconName.Emoji"" IsEnabled=""false"" />
    <BitMenuButtonOption Text=""Option C"" Key=""C"" IconName=""@BitIconName.Emoji2"" />
</BitMenuButton>";

    private readonly string example7RazorCode = @"
<style>
    .custom-class {
        color: aqua;
        overflow: hidden;
        border-radius: 1rem;
    }

    .custom-item {
        color: aqua;
        background-color: darkgoldenrod;
    }

    .custom-icon {
        color: red;
    }

    .custom-text {
        color: aqua;
    }
</style>


<BitMenuButton Text=""Styled Button"" TItem=""BitMenuButtonOption"" Style=""width: 200px; height: 40px;"">
    <BitMenuButtonOption Text=""Option A"" Key=""A"" />
    <BitMenuButtonOption Text=""Option B"" Key=""B"" IsEnabled=""false"" />
    <BitMenuButtonOption Text=""Option C"" Key=""C"" />
</BitMenuButton>

<BitMenuButton Text=""Classed Button"" TItem=""BitMenuButtonOption"" Class=""custom-class"">
    <BitMenuButtonOption Text=""Option A"" Key=""A"" />
    <BitMenuButtonOption Text=""Option B"" Key=""B"" IsEnabled=""false"" />
    <BitMenuButtonOption Text=""Option C"" Key=""C"" />
</BitMenuButton>


<BitMenuButton Text=""Option Styled & Classed Button"" TItem=""BitMenuButtonOption"">
    <BitMenuButtonOption Text=""Option A"" Key=""A"" IconName=""@BitIconName.Emoji"" Style=""color:red"" />
    <BitMenuButtonOption Text=""Option B"" Key=""B"" IconName=""@BitIconName.Emoji"" Class=""custom-item"" />
    <BitMenuButtonOption Text=""Option C"" Key=""C"" IconName=""@BitIconName.Emoji2"" Style=""background:blue"" />
</BitMenuButton>


<BitMenuButton Text=""Styles"" TItem=""BitMenuButtonOption"" IconName=""@BitIconName.ExpandMenu""
               Styles=""@(new() { Icon = ""color: red;"",
                                 Text = ""color: aqua;"",
                                 ItemText = ""color: dodgerblue; font-size: 11px;"",
                                 Overlay = ""background-color: var(--bit-clr-bg-overlay);"" })"">
    <BitMenuButtonOption Text=""Option A"" Key=""A"" />
    <BitMenuButtonOption Text=""Option B"" Key=""B"" />
    <BitMenuButtonOption Text=""Option C"" Key=""C"" />
</BitMenuButton>

<BitMenuButton Text=""Classes"" TItem=""BitMenuButtonOption"" IconName=""@BitIconName.ExpandMenu""
               Classes=""@(new() { Icon = ""custom-icon"", Text = ""custom-text"" })"">
    <BitMenuButtonOption Text=""Option A"" Key=""A"" />
    <BitMenuButtonOption Text=""Option B"" Key=""B"" />
    <BitMenuButtonOption Text=""Option C"" Key=""C"" />
</BitMenuButton>";

    private readonly string example8RazorCode = @"
<style>
    .item-template-box {
        display: flex;
        width: 100%;
    }
</style>


<BitMenuButton TItem=""BitMenuButtonOption"">
    <HeaderTemplate>
        <div style=""font-weight: bold; color: #d13438;"">
            Custom Header!
        </div>
    </HeaderTemplate>
    <Options>
        <BitMenuButtonOption Text=""Option A"" Key=""A"" />
        <BitMenuButtonOption Text=""Option B"" Key=""B"" IsEnabled=""false"" />
        <BitMenuButtonOption Text=""Option C"" Key=""C"" />
    </Options>
</BitMenuButton>


<BitMenuButton Text=""Options"" TItem=""BitMenuButtonOption"" Split>
    <ItemTemplate Context=""item"">
        <div class=""item-template-box"">
            <span style=""color:brown"">@item.Text (@item.Key)</span>
        </div>
    </ItemTemplate>
    <Options>
        <BitMenuButtonOption Text=""Add"" Key=""add-key"" IconName=""@BitIconName.Add"" />
        <BitMenuButtonOption Text=""Edit"" Key=""edit-key"" IconName=""@BitIconName.Edit"" />
        <BitMenuButtonOption Text=""Delete"" Key=""delete-key"" IconName=""@BitIconName.Delete"" />
    </Options>
</BitMenuButton>


<BitMenuButton Text=""Options"" TItem=""BitMenuButtonOption"">
    <BitMenuButtonOption Text=""Add"" Key=""add-key"" IconName=""@BitIconName.Add"">
        <Template Context=""item""><div class=""item-template-box"" style=""color:green"">@item.Text (@item.Key)</div></Template>
    </BitMenuButtonOption>
    <BitMenuButtonOption Text=""Edit"" Key=""edit-key"" IconName=""@BitIconName.Edit"">
        <Template Context=""item""><div class=""item-template-box"" style=""color:yellow"">@item.Text (@item.Key)</div></Template>
    </BitMenuButtonOption>
    <BitMenuButtonOption Text=""Delete"" Key=""delete-key"" IconName=""@BitIconName.Delete"">
        <Template Context=""item""><div class=""item-template-box"" style=""color:red"">@item.Text (@item.Key)</div></Template>
    </BitMenuButtonOption>
</BitMenuButton>";

    private readonly string example9RazorCode = @"
<BitMenuButton Text=""Options""
               OnChange=""(BitMenuButtonOption item) => exampleChangedOption = item?.Key""
               OnClick=""(BitMenuButtonOption item) => exampleClickedOption = item?.Key"">
    <BitMenuButtonOption Text=""Option A"" Key=""A"" IconName=""@BitIconName.Emoji"" />
    <BitMenuButtonOption Text=""Option B"" Key=""B"" IconName=""@BitIconName.Emoji"" IsEnabled=""false"" />
    <BitMenuButtonOption Text=""Option C"" Key=""C"" IconName=""@BitIconName.Emoji2"" />
</BitMenuButton>

<BitMenuButton Split Text=""Options"" TItem=""BitMenuButtonOption""
               OnChange=""(BitMenuButtonOption item) => exampleChangedOption = item?.Key""
               OnClick=""@((BitMenuButtonOption item) => exampleClickedOption = ""Main button clicked"")"">
    <BitMenuButtonOption Text=""Option A"" Key=""A"" IconName=""@BitIconName.Emoji"" OnClick=""@(_ => exampleClickedOption = $""Option A"")"" />
    <BitMenuButtonOption Text=""Option B"" Key=""B"" IconName=""@BitIconName.Emoji"" IsEnabled=""false"" />
    <BitMenuButtonOption Text=""Option C"" Key=""C"" IconName=""@BitIconName.Emoji2"" OnClick=""@(_ => exampleClickedOption = $""Option C"")"" />
</BitMenuButton>

<BitMenuButton Sticky
               OnChange=""(BitMenuButtonOption item) => exampleChangedOption = item?.Key""
               OnClick=""(BitMenuButtonOption item) => exampleClickedOption = item?.Key"">
    <BitMenuButtonOption Text=""Option A"" Key=""A"" IconName=""@BitIconName.Emoji"" />
    <BitMenuButtonOption Text=""Option B"" Key=""B"" IconName=""@BitIconName.Emoji"" IsEnabled=""false"" />
    <BitMenuButtonOption Text=""Option C"" Key=""C"" IconName=""@BitIconName.Emoji2"" />
</BitMenuButton>

<BitMenuButton Split Sticky TItem=""BitMenuButtonOption""
               OnChange=""(BitMenuButtonOption item) => exampleChangedOption = item?.Key""
               OnClick=""(BitMenuButtonOption item) => exampleClickedOption = item?.Key"">
    <BitMenuButtonOption Text=""Option A"" Key=""A"" IconName=""@BitIconName.Emoji"" OnClick=""@(_ => exampleClickedOption = $""Option A"")"" />
    <BitMenuButtonOption Text=""Option B"" Key=""B"" IconName=""@BitIconName.Emoji"" IsEnabled=""false"" />
    <BitMenuButtonOption Text=""Option C"" Key=""C"" IconName=""@BitIconName.Emoji2"" OnClick=""@(_ => exampleClickedOption = $""Option C"")"" />
</BitMenuButton>


<div>Changed option: @exampleChangedOption</div>
<div>Clicked option: @exampleClickedOption</div>";
    private readonly string example9CsharpCode = @"
private string? exampleChangedOption;
private string? exampleClickedOption;";

    private readonly string example10RazorCode = @"
<BitMenuButton Text=""Coming soon..."" TItem=""BitMenuButtonOption"" IsEnabled=""false"" />


<BitMenuButton Sticky @bind-SelectedItem=""twoWaySelectedOption"" TItem=""BitMenuButtonOption"">
    <BitMenuButtonOption Text=""Option A"" Key=""A"" @ref=""optionA"" />
    <BitMenuButtonOption Text=""Option B"" Key=""B"" @ref=""optionB"" IsEnabled=""false"" />
    <BitMenuButtonOption Text=""Option C"" Key=""C"" @ref=""optionC"" />
</BitMenuButton>
<BitChoiceGroup Horizontal TItem=""BitChoiceGroupOption<BitMenuButtonOption>"" TValue=""BitMenuButtonOption"" @bind-Value=""@twoWaySelectedOption"">
    <BitChoiceGroupOption Text=""Option A"" Id=""A"" Value=""optionA"" />
    <BitChoiceGroupOption Text=""Option B"" Id=""B"" Value=""optionB"" IsEnabled=""false"" />
    <BitChoiceGroupOption Text=""Option C"" Id=""C"" Value=""optionC"" />
</BitChoiceGroup>


<BitMenuButton Sticky TItem=""BitMenuButtonOption"">
    <BitMenuButtonOption Text=""Option A"" Key=""A"" IconName=""@BitIconName.Emoji"" />
    <BitMenuButtonOption Text=""Option B"" Key=""B"" IconName=""@BitIconName.Emoji"" />
    <BitMenuButtonOption Text=""Option C"" Key=""C"" IconName=""@BitIconName.Emoji2"" IsSelected=""true"" />
</BitMenuButton>";
    private readonly string example10CsharpCode = @"
private BitMenuButtonOption twoWaySelectedOption = default!;";

    private readonly string example11RazorCode = @"
<BitMenuButton Text=""گزینه ها"" Dir=""BitDir.Rtl"" TItem=""BitMenuButtonOption"" IconName=""@BitIconName.Edit"">
    <BitMenuButtonOption Text=""گزینه الف"" Key=""A"" IconName=""@BitIconName.Emoji"" />
    <BitMenuButtonOption Text=""گزینه ب"" Key=""B"" IconName=""@BitIconName.Emoji"" />
    <BitMenuButtonOption Text=""گزینه ج"" Key=""C"" IconName=""@BitIconName.Emoji2"" />
</BitMenuButton>

<BitMenuButton Text=""گزینه ها"" Dir=""BitDir.Rtl"" TItem=""BitMenuButtonOption"" ChevronDownIcon=""@BitIconName.DoubleChevronDown"">
    <BitMenuButtonOption Text=""گزینه الف"" Key=""A"" IconName=""@BitIconName.Emoji"" />
    <BitMenuButtonOption Text=""گزینه ب"" Key=""B"" IconName=""@BitIconName.Emoji"" />
    <BitMenuButtonOption Text=""گزینه ج"" Key=""C"" IconName=""@BitIconName.Emoji2"" />
</BitMenuButton>";
}
