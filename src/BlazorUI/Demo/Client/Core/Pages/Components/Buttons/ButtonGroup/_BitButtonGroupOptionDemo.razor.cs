﻿namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Buttons.ButtonGroup;

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
<BitButtonGroup ButtonStyle=""BitButtonStyle.Primary"" TItem=""BitButtonGroupOption"">
    <BitButtonGroupOption Text=""Add"" /><BitButtonGroupOption Text=""Edit"" /><BitButtonGroupOption Text=""Delete"" />
</BitButtonGroup>
<BitButtonGroup ButtonStyle=""BitButtonStyle.Primary"" TItem=""BitButtonGroupOption"">
    <BitButtonGroupOption Text=""Add"" /><BitButtonGroupOption Text=""Edit"" IsEnabled=""false"" /><BitButtonGroupOption Text=""Delete"" />
</BitButtonGroup>
<BitButtonGroup ButtonStyle=""BitButtonStyle.Primary"" IsEnabled=""false"" TItem=""BitButtonGroupOption"">
    <BitButtonGroupOption Text=""Add"" /><BitButtonGroupOption Text=""Edit"" IsEnabled=""false"" /><BitButtonGroupOption Text=""Delete"" />
</BitButtonGroup>

<BitButtonGroup ButtonStyle=""BitButtonStyle.Standard"" TItem=""BitButtonGroupOption"">
    <BitButtonGroupOption Text=""Add"" /><BitButtonGroupOption Text=""Edit"" /><BitButtonGroupOption Text=""Delete"" />
</BitButtonGroup>
<BitButtonGroup ButtonStyle=""BitButtonStyle.Standard"" TItem=""BitButtonGroupOption"">
    <BitButtonGroupOption Text=""Add"" /><BitButtonGroupOption Text=""Edit"" IsEnabled=""false"" /><BitButtonGroupOption Text=""Delete"" />
</BitButtonGroup>
<BitButtonGroup ButtonStyle=""BitButtonStyle.Standard"" IsEnabled=""false"" TItem=""BitButtonGroupOption"">
    <BitButtonGroupOption Text=""Add"" /><BitButtonGroupOption Text=""Edit"" IsEnabled=""false"" /><BitButtonGroupOption Text=""Delete"" />
</BitButtonGroup>

<BitButtonGroup ButtonStyle=""BitButtonStyle.Text"" TItem=""BitButtonGroupOption"">
    <BitButtonGroupOption Text=""Add"" /><BitButtonGroupOption Text=""Edit"" /><BitButtonGroupOption Text=""Delete"" />
</BitButtonGroup>
<BitButtonGroup ButtonStyle=""BitButtonStyle.Text"" TItem=""BitButtonGroupOption"">
    <BitButtonGroupOption Text=""Add"" /><BitButtonGroupOption Text=""Edit"" IsEnabled=""false"" /><BitButtonGroupOption Text=""Delete"" />
</BitButtonGroup>
<BitButtonGroup ButtonStyle=""BitButtonStyle.Text"" IsEnabled=""false"" TItem=""BitButtonGroupOption"">
    <BitButtonGroupOption Text=""Add"" /><BitButtonGroupOption Text=""Edit"" IsEnabled=""false"" /><BitButtonGroupOption Text=""Delete"" />
</BitButtonGroup>";

    private readonly string example3RazorCode = @"
<BitButtonGroup ButtonStyle=""BitButtonStyle.Primary"" TItem=""BitButtonGroupOption"">
    <BitButtonGroupOption Text=""Add"" IconName=""@BitIconName.Add"" />
    <BitButtonGroupOption Text=""Edit"" IconName=""@BitIconName.Edit"" />
    <BitButtonGroupOption Text=""Delete"" IconName=""@BitIconName.Delete"" />
</BitButtonGroup>

<BitButtonGroup ButtonStyle=""BitButtonStyle.Standard"" TItem=""BitButtonGroupOption"">
    <BitButtonGroupOption Text=""Add"" IconName=""@BitIconName.Add"" />
    <BitButtonGroupOption Text=""Edit"" IconName=""@BitIconName.Edit"" />
    <BitButtonGroupOption Text=""Delete"" IconName=""@BitIconName.Delete"" />
</BitButtonGroup>

<BitButtonGroup ButtonStyle=""BitButtonStyle.Text"" TItem=""BitButtonGroupOption"">
    <BitButtonGroupOption Text=""Add"" IconName=""@BitIconName.Add"" />
    <BitButtonGroupOption Text=""Edit"" IconName=""@BitIconName.Edit"" />
    <BitButtonGroupOption Text=""Delete"" IconName=""@BitIconName.Delete"" />
</BitButtonGroup>";

    private readonly string example4RazorCode = @"
<BitButtonGroup ButtonStyle=""BitButtonStyle.Primary"" Vertical TItem=""BitButtonGroupOption"">
    <BitButtonGroupOption Text=""Add"" /><BitButtonGroupOption Text=""Edit"" /><BitButtonGroupOption Text=""Delete"" />
</BitButtonGroup>

<BitButtonGroup ButtonStyle=""BitButtonStyle.Standard"" Vertical TItem=""BitButtonGroupOption"">
    <BitButtonGroupOption Text=""Add"" /><BitButtonGroupOption Text=""Edit"" /><BitButtonGroupOption Text=""Delete"" />
</BitButtonGroup>

<BitButtonGroup ButtonStyle=""BitButtonStyle.Text"" Vertical TItem=""BitButtonGroupOption"">
    <BitButtonGroupOption Text=""Add"" /><BitButtonGroupOption Text=""Edit"" /><BitButtonGroupOption Text=""Delete"" />
</BitButtonGroup>";

    private readonly string example5RazorCode = @"
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
    private readonly string example5CsharpCode = @"
private int clickCounter;
private string? clickedOption;";

    private readonly string example6RazorCode = @"
<BitButtonGroup Color=""BitButtonColor.Info"" ButtonStyle=""BitButtonStyle.Primary"" TItem=""BitButtonGroupOption"">
    <BitButtonGroupOption Text=""Add"" /><BitButtonGroupOption Text=""Edit"" /><BitButtonGroupOption Text=""Delete"" />
</BitButtonGroup>
<BitButtonGroup Color=""BitButtonColor.Info"" ButtonStyle=""BitButtonStyle.Standard"" TItem=""BitButtonGroupOption"">
    <BitButtonGroupOption Text=""Add"" /><BitButtonGroupOption Text=""Edit"" /><BitButtonGroupOption Text=""Delete"" />
</BitButtonGroup>
<BitButtonGroup Color=""BitButtonColor.Info"" ButtonStyle=""BitButtonStyle.Text"" TItem=""BitButtonGroupOption"">
    <BitButtonGroupOption Text=""Add"" /><BitButtonGroupOption Text=""Edit"" /><BitButtonGroupOption Text=""Delete"" />
</BitButtonGroup>

<BitButtonGroup Color=""BitButtonColor.Success"" ButtonStyle=""BitButtonStyle.Primary"" TItem=""BitButtonGroupOption"">
    <BitButtonGroupOption Text=""Add"" /><BitButtonGroupOption Text=""Edit"" /><BitButtonGroupOption Text=""Delete"" />
</BitButtonGroup>
<BitButtonGroup Color=""BitButtonColor.Success"" ButtonStyle=""BitButtonStyle.Standard"" TItem=""BitButtonGroupOption"">
    <BitButtonGroupOption Text=""Add"" /><BitButtonGroupOption Text=""Edit"" /><BitButtonGroupOption Text=""Delete"" />
</BitButtonGroup>
<BitButtonGroup Color=""BitButtonColor.Success"" ButtonStyle=""BitButtonStyle.Text"" TItem=""BitButtonGroupOption"">
    <BitButtonGroupOption Text=""Add"" /><BitButtonGroupOption Text=""Edit"" /><BitButtonGroupOption Text=""Delete"" />
</BitButtonGroup>

<BitButtonGroup Color=""BitButtonColor.Warning"" ButtonStyle=""BitButtonStyle.Primary"" TItem=""BitButtonGroupOption"">
    <BitButtonGroupOption Text=""Add"" /><BitButtonGroupOption Text=""Edit"" /><BitButtonGroupOption Text=""Delete"" />
</BitButtonGroup>
<BitButtonGroup Color=""BitButtonColor.Warning"" ButtonStyle=""BitButtonStyle.Standard"" TItem=""BitButtonGroupOption"">
    <BitButtonGroupOption Text=""Add"" /><BitButtonGroupOption Text=""Edit"" /><BitButtonGroupOption Text=""Delete"" />
</BitButtonGroup>
<BitButtonGroup Color=""BitButtonColor.Warning"" ButtonStyle=""BitButtonStyle.Text"" TItem=""BitButtonGroupOption"">
    <BitButtonGroupOption Text=""Add"" /><BitButtonGroupOption Text=""Edit"" /><BitButtonGroupOption Text=""Delete"" />
</BitButtonGroup>

<BitButtonGroup Color=""BitButtonColor.SevereWarning"" ButtonStyle=""BitButtonStyle.Primary"" TItem=""BitButtonGroupOption"">
    <BitButtonGroupOption Text=""Add"" /><BitButtonGroupOption Text=""Edit"" /><BitButtonGroupOption Text=""Delete"" />
</BitButtonGroup>
<BitButtonGroup Color=""BitButtonColor.SevereWarning"" ButtonStyle=""BitButtonStyle.Standard"" TItem=""BitButtonGroupOption"">
    <BitButtonGroupOption Text=""Add"" /><BitButtonGroupOption Text=""Edit"" /><BitButtonGroupOption Text=""Delete"" />
</BitButtonGroup>
<BitButtonGroup Color=""BitButtonColor.SevereWarning"" ButtonStyle=""BitButtonStyle.Text"" TItem=""BitButtonGroupOption"">
    <BitButtonGroupOption Text=""Add"" /><BitButtonGroupOption Text=""Edit"" /><BitButtonGroupOption Text=""Delete"" />
</BitButtonGroup>

<BitButtonGroup Color=""BitButtonColor.Error"" ButtonStyle=""BitButtonStyle.Primary"" TItem=""BitButtonGroupOption"">
    <BitButtonGroupOption Text=""Add"" /><BitButtonGroupOption Text=""Edit"" /><BitButtonGroupOption Text=""Delete"" />
</BitButtonGroup>
<BitButtonGroup Color=""BitButtonColor.Error"" ButtonStyle=""BitButtonStyle.Standard"" TItem=""BitButtonGroupOption"">
    <BitButtonGroupOption Text=""Add"" /><BitButtonGroupOption Text=""Edit"" /><BitButtonGroupOption Text=""Delete"" />
</BitButtonGroup>
<BitButtonGroup Color=""BitButtonColor.Error"" ButtonStyle=""BitButtonStyle.Text"" TItem=""BitButtonGroupOption"">
    <BitButtonGroupOption Text=""Add"" /><BitButtonGroupOption Text=""Edit"" /><BitButtonGroupOption Text=""Delete"" />
</BitButtonGroup>";

    private readonly string example7RazorCode = @"
<BitButtonGroup Size=""BitButtonSize.Small"" ButtonStyle=""BitButtonStyle.Primary"" TItem=""BitButtonGroupOption"">
    <BitButtonGroupOption Text=""Add"" /><BitButtonGroupOption Text=""Edit"" /><BitButtonGroupOption Text=""Delete"" />
</BitButtonGroup>
<BitButtonGroup Size=""BitButtonSize.Small"" ButtonStyle=""BitButtonStyle.Standard"" TItem=""BitButtonGroupOption"">
    <BitButtonGroupOption Text=""Add"" /><BitButtonGroupOption Text=""Edit"" /><BitButtonGroupOption Text=""Delete"" />
</BitButtonGroup>
<BitButtonGroup Size=""BitButtonSize.Small"" ButtonStyle=""BitButtonStyle.Text"" TItem=""BitButtonGroupOption"">
    <BitButtonGroupOption Text=""Add"" /><BitButtonGroupOption Text=""Edit"" /><BitButtonGroupOption Text=""Delete"" />
</BitButtonGroup>

<BitButtonGroup Size=""BitButtonSize.Medium"" ButtonStyle=""BitButtonStyle.Primary"" TItem=""BitButtonGroupOption"">
    <BitButtonGroupOption Text=""Add"" /><BitButtonGroupOption Text=""Edit"" /><BitButtonGroupOption Text=""Delete"" />
</BitButtonGroup>
<BitButtonGroup Size=""BitButtonSize.Medium"" ButtonStyle=""BitButtonStyle.Standard"" TItem=""BitButtonGroupOption"">
    <BitButtonGroupOption Text=""Add"" /><BitButtonGroupOption Text=""Edit"" /><BitButtonGroupOption Text=""Delete"" />
</BitButtonGroup>
<BitButtonGroup Size=""BitButtonSize.Medium"" ButtonStyle=""BitButtonStyle.Text"" TItem=""BitButtonGroupOption"">
    <BitButtonGroupOption Text=""Add"" /><BitButtonGroupOption Text=""Edit"" /><BitButtonGroupOption Text=""Delete"" />
</BitButtonGroup>

<BitButtonGroup Size=""BitButtonSize.Large"" ButtonStyle=""BitButtonStyle.Primary"" TItem=""BitButtonGroupOption"">
    <BitButtonGroupOption Text=""Add"" /><BitButtonGroupOption Text=""Edit"" /><BitButtonGroupOption Text=""Delete"" />
</BitButtonGroup>
<BitButtonGroup Size=""BitButtonSize.Large"" ButtonStyle=""BitButtonStyle.Standard"" TItem=""BitButtonGroupOption"">
    <BitButtonGroupOption Text=""Add"" /><BitButtonGroupOption Text=""Edit"" /><BitButtonGroupOption Text=""Delete"" />
</BitButtonGroup>
<BitButtonGroup Size=""BitButtonSize.Large"" ButtonStyle=""BitButtonStyle.Text"" TItem=""BitButtonGroupOption"">
    <BitButtonGroupOption Text=""Add"" /><BitButtonGroupOption Text=""Edit"" /><BitButtonGroupOption Text=""Delete"" />
</BitButtonGroup>";

    private readonly string example8RazorCode = @"
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
}
