namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Buttons.ButtonGroup;

public partial class _BitButtonGroupOptionDemo
{
    private int clickCounter;
    private string? clickedOption;

    private readonly string example1RazorCode = @"
<BitButtonGroup TItem=""BitButtonGroupOption"">
    <BitButtonGroupOption Text=""Add"" />
    <BitButtonGroupOption Text=""Edit"" />
    <BitButtonGroupOption Text=""Delete"" />
</BitButtonGroup>";

    private readonly string example2RazorCode = @"
<BitButtonGroup Variant=""BitVariant.Fill"" TItem=""BitButtonGroupOption"">
    <BitButtonGroupOption Text=""Add"" /><BitButtonGroupOption Text=""Edit"" /><BitButtonGroupOption Text=""Delete"" />
</BitButtonGroup>
<BitButtonGroup Variant=""BitVariant.Fill"" TItem=""BitButtonGroupOption"">
    <BitButtonGroupOption Text=""Add"" /><BitButtonGroupOption Text=""Edit"" IsEnabled=""false"" /><BitButtonGroupOption Text=""Delete"" />
</BitButtonGroup>
<BitButtonGroup Variant=""BitVariant.Fill"" IsEnabled=""false"" TItem=""BitButtonGroupOption"">
    <BitButtonGroupOption Text=""Add"" /><BitButtonGroupOption Text=""Edit"" IsEnabled=""false"" /><BitButtonGroupOption Text=""Delete"" />
</BitButtonGroup>

<BitButtonGroup Variant=""BitVariant.Outline"" TItem=""BitButtonGroupOption"">
    <BitButtonGroupOption Text=""Add"" /><BitButtonGroupOption Text=""Edit"" /><BitButtonGroupOption Text=""Delete"" />
</BitButtonGroup>
<BitButtonGroup Variant=""BitVariant.Outline"" TItem=""BitButtonGroupOption"">
    <BitButtonGroupOption Text=""Add"" /><BitButtonGroupOption Text=""Edit"" IsEnabled=""false"" /><BitButtonGroupOption Text=""Delete"" />
</BitButtonGroup>
<BitButtonGroup Variant=""BitVariant.Outline"" IsEnabled=""false"" TItem=""BitButtonGroupOption"">
    <BitButtonGroupOption Text=""Add"" /><BitButtonGroupOption Text=""Edit"" IsEnabled=""false"" /><BitButtonGroupOption Text=""Delete"" />
</BitButtonGroup>

<BitButtonGroup Variant=""BitVariant.Text"" TItem=""BitButtonGroupOption"">
    <BitButtonGroupOption Text=""Add"" /><BitButtonGroupOption Text=""Edit"" /><BitButtonGroupOption Text=""Delete"" />
</BitButtonGroup>
<BitButtonGroup Variant=""BitVariant.Text"" TItem=""BitButtonGroupOption"">
    <BitButtonGroupOption Text=""Add"" /><BitButtonGroupOption Text=""Edit"" IsEnabled=""false"" /><BitButtonGroupOption Text=""Delete"" />
</BitButtonGroup>
<BitButtonGroup Variant=""BitVariant.Text"" IsEnabled=""false"" TItem=""BitButtonGroupOption"">
    <BitButtonGroupOption Text=""Add"" /><BitButtonGroupOption Text=""Edit"" IsEnabled=""false"" /><BitButtonGroupOption Text=""Delete"" />
</BitButtonGroup>";

    private readonly string example3RazorCode = @"
<BitButtonGroup Color=""BitColor.Primary"" Variant=""BitVariant.Fill"" TItem=""BitButtonGroupOption"">
    <BitButtonGroupOption Text=""Add"" /><BitButtonGroupOption Text=""Edit"" /><BitButtonGroupOption Text=""Delete"" />
</BitButtonGroup>
<BitButtonGroup Color=""BitColor.Primary"" Variant=""BitVariant.Outline"" TItem=""BitButtonGroupOption"">
    <BitButtonGroupOption Text=""Add"" /><BitButtonGroupOption Text=""Edit"" /><BitButtonGroupOption Text=""Delete"" />
</BitButtonGroup>
<BitButtonGroup Color=""BitColor.Primary"" Variant=""BitVariant.Text"" TItem=""BitButtonGroupOption"">
    <BitButtonGroupOption Text=""Add"" /><BitButtonGroupOption Text=""Edit"" /><BitButtonGroupOption Text=""Delete"" />
</BitButtonGroup>

<BitButtonGroup Color=""BitColor.Secondary"" Variant=""BitVariant.Fill"" TItem=""BitButtonGroupOption"">
    <BitButtonGroupOption Text=""Add"" /><BitButtonGroupOption Text=""Edit"" /><BitButtonGroupOption Text=""Delete"" />
</BitButtonGroup>
<BitButtonGroup Color=""BitColor.Secondary"" Variant=""BitVariant.Outline"" TItem=""BitButtonGroupOption"">
    <BitButtonGroupOption Text=""Add"" /><BitButtonGroupOption Text=""Edit"" /><BitButtonGroupOption Text=""Delete"" />
</BitButtonGroup>
<BitButtonGroup Color=""BitColor.Secondary"" Variant=""BitVariant.Text"" TItem=""BitButtonGroupOption"">
    <BitButtonGroupOption Text=""Add"" /><BitButtonGroupOption Text=""Edit"" /><BitButtonGroupOption Text=""Delete"" />
</BitButtonGroup>

<BitButtonGroup Color=""BitColor.Tertiary"" Variant=""BitVariant.Fill"" TItem=""BitButtonGroupOption"">
    <BitButtonGroupOption Text=""Add"" /><BitButtonGroupOption Text=""Edit"" /><BitButtonGroupOption Text=""Delete"" />
</BitButtonGroup>
<BitButtonGroup Color=""BitColor.Tertiary"" Variant=""BitVariant.Outline"" TItem=""BitButtonGroupOption"">
    <BitButtonGroupOption Text=""Add"" /><BitButtonGroupOption Text=""Edit"" /><BitButtonGroupOption Text=""Delete"" />
</BitButtonGroup>
<BitButtonGroup Color=""BitColor.Tertiary"" Variant=""BitVariant.Text"" TItem=""BitButtonGroupOption"">
    <BitButtonGroupOption Text=""Add"" /><BitButtonGroupOption Text=""Edit"" /><BitButtonGroupOption Text=""Delete"" />
</BitButtonGroup>

<BitButtonGroup Color=""BitColor.Info"" Variant=""BitVariant.Fill"" TItem=""BitButtonGroupOption"">
    <BitButtonGroupOption Text=""Add"" /><BitButtonGroupOption Text=""Edit"" /><BitButtonGroupOption Text=""Delete"" />
</BitButtonGroup>
<BitButtonGroup Color=""BitColor.Info"" Variant=""BitVariant.Outline"" TItem=""BitButtonGroupOption"">
    <BitButtonGroupOption Text=""Add"" /><BitButtonGroupOption Text=""Edit"" /><BitButtonGroupOption Text=""Delete"" />
</BitButtonGroup>
<BitButtonGroup Color=""BitColor.Info"" Variant=""BitVariant.Text"" TItem=""BitButtonGroupOption"">
    <BitButtonGroupOption Text=""Add"" /><BitButtonGroupOption Text=""Edit"" /><BitButtonGroupOption Text=""Delete"" />
</BitButtonGroup>

<BitButtonGroup Color=""BitColor.Success"" Variant=""BitVariant.Fill"" TItem=""BitButtonGroupOption"">
    <BitButtonGroupOption Text=""Add"" /><BitButtonGroupOption Text=""Edit"" /><BitButtonGroupOption Text=""Delete"" />
</BitButtonGroup>
<BitButtonGroup Color=""BitColor.Success"" Variant=""BitVariant.Outline"" TItem=""BitButtonGroupOption"">
    <BitButtonGroupOption Text=""Add"" /><BitButtonGroupOption Text=""Edit"" /><BitButtonGroupOption Text=""Delete"" />
</BitButtonGroup>
<BitButtonGroup Color=""BitColor.Success"" Variant=""BitVariant.Text"" TItem=""BitButtonGroupOption"">
    <BitButtonGroupOption Text=""Add"" /><BitButtonGroupOption Text=""Edit"" /><BitButtonGroupOption Text=""Delete"" />
</BitButtonGroup>

<BitButtonGroup Color=""BitColor.Warning"" Variant=""BitVariant.Fill"" TItem=""BitButtonGroupOption"">
    <BitButtonGroupOption Text=""Add"" /><BitButtonGroupOption Text=""Edit"" /><BitButtonGroupOption Text=""Delete"" />
</BitButtonGroup>
<BitButtonGroup Color=""BitColor.Warning"" Variant=""BitVariant.Outline"" TItem=""BitButtonGroupOption"">
    <BitButtonGroupOption Text=""Add"" /><BitButtonGroupOption Text=""Edit"" /><BitButtonGroupOption Text=""Delete"" />
</BitButtonGroup>
<BitButtonGroup Color=""BitColor.Warning"" Variant=""BitVariant.Text"" TItem=""BitButtonGroupOption"">
    <BitButtonGroupOption Text=""Add"" /><BitButtonGroupOption Text=""Edit"" /><BitButtonGroupOption Text=""Delete"" />
</BitButtonGroup>

<BitButtonGroup Color=""BitColor.SevereWarning"" Variant=""BitVariant.Fill"" TItem=""BitButtonGroupOption"">
    <BitButtonGroupOption Text=""Add"" /><BitButtonGroupOption Text=""Edit"" /><BitButtonGroupOption Text=""Delete"" />
</BitButtonGroup>
<BitButtonGroup Color=""BitColor.SevereWarning"" Variant=""BitVariant.Outline"" TItem=""BitButtonGroupOption"">
    <BitButtonGroupOption Text=""Add"" /><BitButtonGroupOption Text=""Edit"" /><BitButtonGroupOption Text=""Delete"" />
</BitButtonGroup>
<BitButtonGroup Color=""BitColor.SevereWarning"" Variant=""BitVariant.Text"" TItem=""BitButtonGroupOption"">
    <BitButtonGroupOption Text=""Add"" /><BitButtonGroupOption Text=""Edit"" /><BitButtonGroupOption Text=""Delete"" />
</BitButtonGroup>

<BitButtonGroup Color=""BitColor.Error"" Variant=""BitVariant.Fill"" TItem=""BitButtonGroupOption"">
    <BitButtonGroupOption Text=""Add"" /><BitButtonGroupOption Text=""Edit"" /><BitButtonGroupOption Text=""Delete"" />
</BitButtonGroup>
<BitButtonGroup Color=""BitColor.Error"" Variant=""BitVariant.Outline"" TItem=""BitButtonGroupOption"">
    <BitButtonGroupOption Text=""Add"" /><BitButtonGroupOption Text=""Edit"" /><BitButtonGroupOption Text=""Delete"" />
</BitButtonGroup>
<BitButtonGroup Color=""BitColor.Error"" Variant=""BitVariant.Text"" TItem=""BitButtonGroupOption"">
    <BitButtonGroupOption Text=""Add"" /><BitButtonGroupOption Text=""Edit"" /><BitButtonGroupOption Text=""Delete"" />
</BitButtonGroup>";

    private readonly string example4RazorCode = @"
<BitButtonGroup Variant=""BitVariant.Fill"" TItem=""BitButtonGroupOption"">
    <BitButtonGroupOption Text=""Add"" IconName=""@BitIconName.Add"" />
    <BitButtonGroupOption Text=""Edit"" IconName=""@BitIconName.Edit"" />
    <BitButtonGroupOption Text=""Delete"" IconName=""@BitIconName.Delete"" />
</BitButtonGroup>

<BitButtonGroup Variant=""BitVariant.Outline"" TItem=""BitButtonGroupOption"">
    <BitButtonGroupOption Text=""Add"" IconName=""@BitIconName.Add"" />
    <BitButtonGroupOption Text=""Edit"" IconName=""@BitIconName.Edit"" />
    <BitButtonGroupOption Text=""Delete"" IconName=""@BitIconName.Delete"" />
</BitButtonGroup>

<BitButtonGroup Variant=""BitVariant.Text"" TItem=""BitButtonGroupOption"">
    <BitButtonGroupOption Text=""Add"" IconName=""@BitIconName.Add"" />
    <BitButtonGroupOption Text=""Edit"" IconName=""@BitIconName.Edit"" />
    <BitButtonGroupOption Text=""Delete"" IconName=""@BitIconName.Delete"" />
</BitButtonGroup>";

    private readonly string example5RazorCode = @"
<BitButtonGroup Variant=""BitVariant.Fill"" Vertical TItem=""BitButtonGroupOption"">
    <BitButtonGroupOption Text=""Add"" /><BitButtonGroupOption Text=""Edit"" /><BitButtonGroupOption Text=""Delete"" />
</BitButtonGroup>

<BitButtonGroup Variant=""BitVariant.Outline"" Vertical TItem=""BitButtonGroupOption"">
    <BitButtonGroupOption Text=""Add"" /><BitButtonGroupOption Text=""Edit"" /><BitButtonGroupOption Text=""Delete"" />
</BitButtonGroup>

<BitButtonGroup Variant=""BitVariant.Text"" Vertical TItem=""BitButtonGroupOption"">
    <BitButtonGroupOption Text=""Add"" /><BitButtonGroupOption Text=""Edit"" /><BitButtonGroupOption Text=""Delete"" />
</BitButtonGroup>";

    private readonly string example6RazorCode = @"
<BitButtonGroup Size=""BitSize.Small"" Variant=""BitVariant.Fill"" TItem=""BitButtonGroupOption"">
    <BitButtonGroupOption Text=""Add"" /><BitButtonGroupOption Text=""Edit"" /><BitButtonGroupOption Text=""Delete"" />
</BitButtonGroup>
<BitButtonGroup Size=""BitSize.Small"" Variant=""BitVariant.Outline"" TItem=""BitButtonGroupOption"">
    <BitButtonGroupOption Text=""Add"" /><BitButtonGroupOption Text=""Edit"" /><BitButtonGroupOption Text=""Delete"" />
</BitButtonGroup>
<BitButtonGroup Size=""BitSize.Small"" Variant=""BitVariant.Text"" TItem=""BitButtonGroupOption"">
    <BitButtonGroupOption Text=""Add"" /><BitButtonGroupOption Text=""Edit"" /><BitButtonGroupOption Text=""Delete"" />
</BitButtonGroup>

<BitButtonGroup Size=""BitSize.Medium"" Variant=""BitVariant.Fill"" TItem=""BitButtonGroupOption"">
    <BitButtonGroupOption Text=""Add"" /><BitButtonGroupOption Text=""Edit"" /><BitButtonGroupOption Text=""Delete"" />
</BitButtonGroup>
<BitButtonGroup Size=""BitSize.Medium"" Variant=""BitVariant.Outline"" TItem=""BitButtonGroupOption"">
    <BitButtonGroupOption Text=""Add"" /><BitButtonGroupOption Text=""Edit"" /><BitButtonGroupOption Text=""Delete"" />
</BitButtonGroup>
<BitButtonGroup Size=""BitSize.Medium"" Variant=""BitVariant.Text"" TItem=""BitButtonGroupOption"">
    <BitButtonGroupOption Text=""Add"" /><BitButtonGroupOption Text=""Edit"" /><BitButtonGroupOption Text=""Delete"" />
</BitButtonGroup>

<BitButtonGroup Size=""BitSize.Large"" Variant=""BitVariant.Fill"" TItem=""BitButtonGroupOption"">
    <BitButtonGroupOption Text=""Add"" /><BitButtonGroupOption Text=""Edit"" /><BitButtonGroupOption Text=""Delete"" />
</BitButtonGroup>
<BitButtonGroup Size=""BitSize.Large"" Variant=""BitVariant.Outline"" TItem=""BitButtonGroupOption"">
    <BitButtonGroupOption Text=""Add"" /><BitButtonGroupOption Text=""Edit"" /><BitButtonGroupOption Text=""Delete"" />
</BitButtonGroup>
<BitButtonGroup Size=""BitSize.Large"" Variant=""BitVariant.Text"" TItem=""BitButtonGroupOption"">
    <BitButtonGroupOption Text=""Add"" /><BitButtonGroupOption Text=""Edit"" /><BitButtonGroupOption Text=""Delete"" />
</BitButtonGroup>";

    private readonly string example7RazorCode = @"
<style>
    .custom-class {
        padding: 2rem;
        border-radius:1rem;
        background-color: blueviolet;
    }

    .custom-item {
        color: blueviolet;
        background-color: goldenrod;
    }
</style>

<BitButtonGroup Style=""padding:1rem;background:red"" TItem=""BitButtonGroupOption"">
    <BitButtonGroupOption Text=""Add"" /><BitButtonGroupOption Text=""Edit"" /><BitButtonGroupOption Text=""Delete"" />
</BitButtonGroup>
<BitButtonGroup Class=""custom-class"" TItem=""BitButtonGroupOption"">
    <BitButtonGroupOption Text=""Add"" /><BitButtonGroupOption Text=""Edit"" /><BitButtonGroupOption Text=""Delete"" />
</BitButtonGroup>

<BitButtonGroup TItem=""BitButtonGroupOption"">
    <BitButtonGroupOption Text=""Styled"" IconName=""@BitIconName.Brush"" Style=""color:darkred"" />
    <BitButtonGroupOption Text=""Classed"" IconName=""@BitIconName.FormatPainter"" Class=""custom-item"" />
</BitButtonGroup>";

    private readonly string example8RazorCode = @"
<BitButtonGroup OnItemClick=""item => clickedOption = item.Text"" TItem=""BitButtonGroupOption"">
    <BitButtonGroupOption Text=""Add"" />
    <BitButtonGroupOption Text=""Edit"" />
    <BitButtonGroupOption Text=""Delete"" />
</BitButtonGroup>
<div>Clicked item: <b>@clickedOption</b></div>

<BitButtonGroup TItem=""BitButtonGroupOption"">
    <BitButtonGroupOption Text=""Increase"" IconName=""@BitIconName.Add"" OnClick=""_ => { clickCounter++; StateHasChanged(); }"" />
    <BitButtonGroupOption Text=""Reset"" IconName=""@BitIconName.Reset"" OnClick=""_ => { clickCounter=0; StateHasChanged(); }"" />
    <BitButtonGroupOption Text=""Decrease"" IconName=""@BitIconName.Remove"" OnClick=""_ => { clickCounter--; StateHasChanged(); }"" />
</BitButtonGroup>
<div>Click count: <b>@clickCounter</b></div>";
    private readonly string example8CsharpCode = @"
private int clickCounter;
private string? clickedOption;";

    private readonly string example9RazorCode = @"
<BitButtonGroup Dir=""BitDir.Rtl"" Variant=""BitVariant.Fill"" TItem=""BitButtonGroupOption"">
    <BitButtonGroupOption Text=""اضافه کردن"" IconName=""@BitIconName.Add"" />
    <BitButtonGroupOption Text=""ویرایش"" IconName=""@BitIconName.Edit"" />
    <BitButtonGroupOption Text=""حذف"" IconName=""@BitIconName.Delete"" />
</BitButtonGroup>

<BitButtonGroup Dir=""BitDir.Rtl"" Variant=""BitVariant.Outline"" TItem=""BitButtonGroupOption"">
    <BitButtonGroupOption Text=""اضافه کردن"" IconName=""@BitIconName.Add"" />
    <BitButtonGroupOption Text=""ویرایش"" IconName=""@BitIconName.Edit"" />
    <BitButtonGroupOption Text=""حذف"" IconName=""@BitIconName.Delete"" />
</BitButtonGroup>

<BitButtonGroup Dir=""BitDir.Rtl"" Variant=""BitVariant.Text"" TItem=""BitButtonGroupOption"">
    <BitButtonGroupOption Text=""اضافه کردن"" IconName=""@BitIconName.Add"" />
    <BitButtonGroupOption Text=""ویرایش"" IconName=""@BitIconName.Edit"" />
    <BitButtonGroupOption Text=""حذف"" IconName=""@BitIconName.Delete"" />
</BitButtonGroup>";
}
