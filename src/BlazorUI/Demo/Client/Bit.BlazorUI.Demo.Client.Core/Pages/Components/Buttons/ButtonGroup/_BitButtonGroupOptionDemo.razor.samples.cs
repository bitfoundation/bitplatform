namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Buttons.ButtonGroup;

public partial class _BitButtonGroupOptionDemo
{
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

    private readonly string example4RazorCode = @"
<BitButtonGroup Variant=""BitVariant.Fill"" TItem=""BitButtonGroupOption"" IconOnly>
    <BitButtonGroupOption Text=""Add"" IconName=""@BitIconName.Add"" />
    <BitButtonGroupOption Text=""Edit"" IconName=""@BitIconName.Edit"" />
    <BitButtonGroupOption Text=""Delete"" IconName=""@BitIconName.Delete"" />
</BitButtonGroup>

<BitButtonGroup Variant=""BitVariant.Outline"" TItem=""BitButtonGroupOption"" IconOnly>
    <BitButtonGroupOption Text=""Add"" IconName=""@BitIconName.Add"" />
    <BitButtonGroupOption Text=""Edit"" IconName=""@BitIconName.Edit"" />
    <BitButtonGroupOption Text=""Delete"" IconName=""@BitIconName.Delete"" />
</BitButtonGroup>

<BitButtonGroup Variant=""BitVariant.Text"" TItem=""BitButtonGroupOption"" IconOnly>
    <BitButtonGroupOption Text=""Add"" IconName=""@BitIconName.Add"" />
    <BitButtonGroupOption Text=""Edit"" IconName=""@BitIconName.Edit"" />
    <BitButtonGroupOption Text=""Delete"" IconName=""@BitIconName.Delete"" />
</BitButtonGroup>



<BitButtonGroup Variant=""BitVariant.Fill"" TItem=""BitButtonGroupOption"">
    <BitButtonGroupOption Text=""Add"" IconName=""@BitIconName.Add"" />
    <BitButtonGroupOption IconName=""@BitIconName.Edit"" />
    <BitButtonGroupOption Text=""Delete"" IconName=""@BitIconName.Delete"" />
</BitButtonGroup>

<BitButtonGroup Variant=""BitVariant.Outline"" TItem=""BitButtonGroupOption"">
    <BitButtonGroupOption Text=""Add"" IconName=""@BitIconName.Add"" />
    <BitButtonGroupOption IconName=""@BitIconName.Edit"" />
    <BitButtonGroupOption Text=""Delete"" IconName=""@BitIconName.Delete"" />
</BitButtonGroup>

<BitButtonGroup Variant=""BitVariant.Text"" TItem=""BitButtonGroupOption"">
    <BitButtonGroupOption Text=""Add"" IconName=""@BitIconName.Add"" />
    <BitButtonGroupOption IconName=""@BitIconName.Edit"" />
    <BitButtonGroupOption Text=""Delete"" IconName=""@BitIconName.Delete"" />
</BitButtonGroup>";

    private readonly string example5RazorCode = @"
<BitButtonGroup Variant=""BitVariant.Fill"" TItem=""BitButtonGroupOption"">
    <BitButtonGroupOption Text=""Add"" IconName=""@BitIconName.Add"" ReversedIcon />
    <BitButtonGroupOption Text=""Edit"" IconName=""@BitIconName.Edit"" ReversedIcon />
    <BitButtonGroupOption Text=""Delete"" IconName=""@BitIconName.Delete"" ReversedIcon />
</BitButtonGroup>

<BitButtonGroup Variant=""BitVariant.Outline"" TItem=""BitButtonGroupOption"">
    <BitButtonGroupOption Text=""Add"" IconName=""@BitIconName.Add"" ReversedIcon />
    <BitButtonGroupOption Text=""Edit"" IconName=""@BitIconName.Edit"" ReversedIcon />
    <BitButtonGroupOption Text=""Delete"" IconName=""@BitIconName.Delete"" ReversedIcon />
</BitButtonGroup>

<BitButtonGroup Variant=""BitVariant.Text"" TItem=""BitButtonGroupOption"">
    <BitButtonGroupOption Text=""Add"" IconName=""@BitIconName.Add"" ReversedIcon />
    <BitButtonGroupOption Text=""Edit"" IconName=""@BitIconName.Edit"" ReversedIcon />
    <BitButtonGroupOption Text=""Delete"" IconName=""@BitIconName.Delete"" ReversedIcon />
</BitButtonGroup>";

    private readonly string example6RazorCode = @"
<BitButtonGroup Toggle Variant=""BitVariant.Fill"" TItem=""BitButtonGroupOption"">
    <BitButtonGroupOption OnText=""Back (2X)"" OffText=""Back (1X)"" OnIconName=""@BitIconName.RewindTwoX"" OffIconName=""@BitIconName.Rewind"" />
    <BitButtonGroupOption OnTitle=""Resume"" OffTitle=""Play"" OnIconName=""@BitIconName.PlayResume"" OffIconName=""@BitIconName.Play"" />
    <BitButtonGroupOption OnText=""Forward (2X)"" OffText=""Forward (1X)"" OnIconName=""@BitIconName.FastForwardTwoX"" OffIconName=""@BitIconName.FastForward"" ReversedIcon />
</BitButtonGroup>

<BitButtonGroup Toggle Variant=""BitVariant.Outline"" TItem=""BitButtonGroupOption"">
    <BitButtonGroupOption OnText=""Back (2X)"" OffText=""Back (1X)"" OnIconName=""@BitIconName.RewindTwoX"" OffIconName=""@BitIconName.Rewind"" />
    <BitButtonGroupOption OnTitle=""Resume"" OffTitle=""Play"" OnIconName=""@BitIconName.PlayResume"" OffIconName=""@BitIconName.Play"" />
    <BitButtonGroupOption OnText=""Forward (2X)"" OffText=""Forward (1X)"" OnIconName=""@BitIconName.FastForwardTwoX"" OffIconName=""@BitIconName.FastForward"" ReversedIcon />
</BitButtonGroup>

<BitButtonGroup Toggle Variant=""BitVariant.Text"" TItem=""BitButtonGroupOption"">
    <BitButtonGroupOption OnText=""Back (2X)"" OffText=""Back (1X)"" OnIconName=""@BitIconName.RewindTwoX"" OffIconName=""@BitIconName.Rewind"" />
    <BitButtonGroupOption OnTitle=""Resume"" OffTitle=""Play"" OnIconName=""@BitIconName.PlayResume"" OffIconName=""@BitIconName.Play"" />
    <BitButtonGroupOption OnText=""Forward (2X)"" OffText=""Forward (1X)"" OnIconName=""@BitIconName.FastForwardTwoX"" OffIconName=""@BitIconName.FastForward"" ReversedIcon />
</BitButtonGroup>";

    private readonly string example7RazorCode = @"
<BitButtonGroup Variant=""BitVariant.Fill"" Vertical TItem=""BitButtonGroupOption"">
    <BitButtonGroupOption Text=""Add"" /><BitButtonGroupOption Text=""Edit"" /><BitButtonGroupOption Text=""Delete"" />
</BitButtonGroup>

<BitButtonGroup Variant=""BitVariant.Outline"" Vertical TItem=""BitButtonGroupOption"">
    <BitButtonGroupOption Text=""Add"" /><BitButtonGroupOption Text=""Edit"" /><BitButtonGroupOption Text=""Delete"" />
</BitButtonGroup>

<BitButtonGroup Variant=""BitVariant.Text"" Vertical TItem=""BitButtonGroupOption"">
    <BitButtonGroupOption Text=""Add"" /><BitButtonGroupOption Text=""Edit"" /><BitButtonGroupOption Text=""Delete"" />
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

    private readonly string example10RazorCode = @"
<BitButtonGroup FullWidth Variant=""BitVariant.Fill"" TItem=""BitButtonGroupOption"">
    <BitButtonGroupOption Text=""Add"" /><BitButtonGroupOption Text=""Edit"" /><BitButtonGroupOption Text=""Delete"" />
</BitButtonGroup>
<BitButtonGroup FullWidth Variant=""BitVariant.Outline"" TItem=""BitButtonGroupOption"">
    <BitButtonGroupOption Text=""Add"" /><BitButtonGroupOption Text=""Edit"" /><BitButtonGroupOption Text=""Delete"" />
</BitButtonGroup>
<BitButtonGroup FullWidth Variant=""BitVariant.Text"" TItem=""BitButtonGroupOption"">
    <BitButtonGroupOption Text=""Add"" /><BitButtonGroupOption Text=""Edit"" /><BitButtonGroupOption Text=""Delete"" />
</BitButtonGroup>";

    private readonly string example11RazorCode = @"
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
</BitButtonGroup>


<BitButtonGroup Color=""BitColor.PrimaryBackground"" Variant=""BitVariant.Fill"" TItem=""BitButtonGroupOption"">
    <BitButtonGroupOption Text=""Add"" /><BitButtonGroupOption Text=""Edit"" /><BitButtonGroupOption Text=""Delete"" />
</BitButtonGroup>
<BitButtonGroup Color=""BitColor.PrimaryBackground"" Variant=""BitVariant.Outline"" TItem=""BitButtonGroupOption"">
    <BitButtonGroupOption Text=""Add"" /><BitButtonGroupOption Text=""Edit"" /><BitButtonGroupOption Text=""Delete"" />
</BitButtonGroup>
<BitButtonGroup Color=""BitColor.PrimaryBackground"" Variant=""BitVariant.Text"" TItem=""BitButtonGroupOption"">
    <BitButtonGroupOption Text=""Add"" /><BitButtonGroupOption Text=""Edit"" /><BitButtonGroupOption Text=""Delete"" />
</BitButtonGroup>

<BitButtonGroup Color=""BitColor.SecondaryBackground"" Variant=""BitVariant.Fill"" TItem=""BitButtonGroupOption"">
    <BitButtonGroupOption Text=""Add"" /><BitButtonGroupOption Text=""Edit"" /><BitButtonGroupOption Text=""Delete"" />
</BitButtonGroup>
<BitButtonGroup Color=""BitColor.SecondaryBackground"" Variant=""BitVariant.Outline"" TItem=""BitButtonGroupOption"">
    <BitButtonGroupOption Text=""Add"" /><BitButtonGroupOption Text=""Edit"" /><BitButtonGroupOption Text=""Delete"" />
</BitButtonGroup>
<BitButtonGroup Color=""BitColor.SecondaryBackground"" Variant=""BitVariant.Text"" TItem=""BitButtonGroupOption"">
    <BitButtonGroupOption Text=""Add"" /><BitButtonGroupOption Text=""Edit"" /><BitButtonGroupOption Text=""Delete"" />
</BitButtonGroup>

<BitButtonGroup Color=""BitColor.TertiaryBackground"" Variant=""BitVariant.Fill"" TItem=""BitButtonGroupOption"">
    <BitButtonGroupOption Text=""Add"" /><BitButtonGroupOption Text=""Edit"" /><BitButtonGroupOption Text=""Delete"" />
</BitButtonGroup>
<BitButtonGroup Color=""BitColor.TertiaryBackground"" Variant=""BitVariant.Outline"" TItem=""BitButtonGroupOption"">
    <BitButtonGroupOption Text=""Add"" /><BitButtonGroupOption Text=""Edit"" /><BitButtonGroupOption Text=""Delete"" />
</BitButtonGroup>
<BitButtonGroup Color=""BitColor.TertiaryBackground"" Variant=""BitVariant.Text"" TItem=""BitButtonGroupOption"">
    <BitButtonGroupOption Text=""Add"" /><BitButtonGroupOption Text=""Edit"" /><BitButtonGroupOption Text=""Delete"" />
</BitButtonGroup>


<BitButtonGroup Color=""BitColor.PrimaryForeground"" Variant=""BitVariant.Fill"" TItem=""BitButtonGroupOption"">
    <BitButtonGroupOption Text=""Add"" /><BitButtonGroupOption Text=""Edit"" /><BitButtonGroupOption Text=""Delete"" />
</BitButtonGroup>
<BitButtonGroup Color=""BitColor.PrimaryForeground"" Variant=""BitVariant.Outline"" TItem=""BitButtonGroupOption"">
    <BitButtonGroupOption Text=""Add"" /><BitButtonGroupOption Text=""Edit"" /><BitButtonGroupOption Text=""Delete"" />
</BitButtonGroup>
<BitButtonGroup Color=""BitColor.PrimaryForeground"" Variant=""BitVariant.Text"" TItem=""BitButtonGroupOption"">
    <BitButtonGroupOption Text=""Add"" /><BitButtonGroupOption Text=""Edit"" /><BitButtonGroupOption Text=""Delete"" />
</BitButtonGroup>

<BitButtonGroup Color=""BitColor.SecondaryForeground"" Variant=""BitVariant.Fill"" TItem=""BitButtonGroupOption"">
    <BitButtonGroupOption Text=""Add"" /><BitButtonGroupOption Text=""Edit"" /><BitButtonGroupOption Text=""Delete"" />
</BitButtonGroup>
<BitButtonGroup Color=""BitColor.SecondaryForeground"" Variant=""BitVariant.Outline"" TItem=""BitButtonGroupOption"">
    <BitButtonGroupOption Text=""Add"" /><BitButtonGroupOption Text=""Edit"" /><BitButtonGroupOption Text=""Delete"" />
</BitButtonGroup>
<BitButtonGroup Color=""BitColor.SecondaryForeground"" Variant=""BitVariant.Text"" TItem=""BitButtonGroupOption"">
    <BitButtonGroupOption Text=""Add"" /><BitButtonGroupOption Text=""Edit"" /><BitButtonGroupOption Text=""Delete"" />
</BitButtonGroup>

<BitButtonGroup Color=""BitColor.TertiaryForeground"" Variant=""BitVariant.Fill"" TItem=""BitButtonGroupOption"">
    <BitButtonGroupOption Text=""Add"" /><BitButtonGroupOption Text=""Edit"" /><BitButtonGroupOption Text=""Delete"" />
</BitButtonGroup>
<BitButtonGroup Color=""BitColor.TertiaryForeground"" Variant=""BitVariant.Outline"" TItem=""BitButtonGroupOption"">
    <BitButtonGroupOption Text=""Add"" /><BitButtonGroupOption Text=""Edit"" /><BitButtonGroupOption Text=""Delete"" />
</BitButtonGroup>
<BitButtonGroup Color=""BitColor.TertiaryForeground"" Variant=""BitVariant.Text"" TItem=""BitButtonGroupOption"">
    <BitButtonGroupOption Text=""Add"" /><BitButtonGroupOption Text=""Edit"" /><BitButtonGroupOption Text=""Delete"" />
</BitButtonGroup>


<BitButtonGroup Color=""BitColor.PrimaryBorder"" Variant=""BitVariant.Fill"" TItem=""BitButtonGroupOption"">
    <BitButtonGroupOption Text=""Add"" /><BitButtonGroupOption Text=""Edit"" /><BitButtonGroupOption Text=""Delete"" />
</BitButtonGroup>
<BitButtonGroup Color=""BitColor.PrimaryBorder"" Variant=""BitVariant.Outline"" TItem=""BitButtonGroupOption"">
    <BitButtonGroupOption Text=""Add"" /><BitButtonGroupOption Text=""Edit"" /><BitButtonGroupOption Text=""Delete"" />
</BitButtonGroup>
<BitButtonGroup Color=""BitColor.PrimaryBorder"" Variant=""BitVariant.Text"" TItem=""BitButtonGroupOption"">
    <BitButtonGroupOption Text=""Add"" /><BitButtonGroupOption Text=""Edit"" /><BitButtonGroupOption Text=""Delete"" />
</BitButtonGroup>

<BitButtonGroup Color=""BitColor.SecondaryBorder"" Variant=""BitVariant.Fill"" TItem=""BitButtonGroupOption"">
    <BitButtonGroupOption Text=""Add"" /><BitButtonGroupOption Text=""Edit"" /><BitButtonGroupOption Text=""Delete"" />
</BitButtonGroup>
<BitButtonGroup Color=""BitColor.SecondaryBorder"" Variant=""BitVariant.Outline"" TItem=""BitButtonGroupOption"">
    <BitButtonGroupOption Text=""Add"" /><BitButtonGroupOption Text=""Edit"" /><BitButtonGroupOption Text=""Delete"" />
</BitButtonGroup>
<BitButtonGroup Color=""BitColor.SecondaryBorder"" Variant=""BitVariant.Text"" TItem=""BitButtonGroupOption"">
    <BitButtonGroupOption Text=""Add"" /><BitButtonGroupOption Text=""Edit"" /><BitButtonGroupOption Text=""Delete"" />
</BitButtonGroup>

<BitButtonGroup Color=""BitColor.TertiaryBorder"" Variant=""BitVariant.Fill"" TItem=""BitButtonGroupOption"">
    <BitButtonGroupOption Text=""Add"" /><BitButtonGroupOption Text=""Edit"" /><BitButtonGroupOption Text=""Delete"" />
</BitButtonGroup>
<BitButtonGroup Color=""BitColor.TertiaryBorder"" Variant=""BitVariant.Outline"" TItem=""BitButtonGroupOption"">
    <BitButtonGroupOption Text=""Add"" /><BitButtonGroupOption Text=""Edit"" /><BitButtonGroupOption Text=""Delete"" />
</BitButtonGroup>
<BitButtonGroup Color=""BitColor.TertiaryBorder"" Variant=""BitVariant.Text"" TItem=""BitButtonGroupOption"">
    <BitButtonGroupOption Text=""Add"" /><BitButtonGroupOption Text=""Edit"" /><BitButtonGroupOption Text=""Delete"" />
</BitButtonGroup>";

    private readonly string example12RazorCode = @"
<style>
    .custom-class {
        margin-inline: 1rem;
        border-radius: 1rem;
        border-color: tomato;
        border-width: 0.25rem;
    }

    .custom-class button {
        color: tomato;
        border-color: tomato;
    }

    .custom-class button:hover {
        color: unset;
        background-color: lightcoral;
    }

    .custom-item {
        color: peachpuff;
        background-color: tomato;
    }

    .custom-btn {
        color: aliceblue;
        border-color: aliceblue;
        background-color: crimson;
    }
</style>


<BitButtonGroup Style=""border-radius: 1rem; margin: 1rem; box-shadow: tomato 0 0 1rem;"" TItem=""BitButtonGroupOption"">
    <BitButtonGroupOption Text=""Add"" /><BitButtonGroupOption Text=""Edit"" /><BitButtonGroupOption Text=""Delete"" />
</BitButtonGroup>

<BitButtonGroup Class=""custom-class"" Variant=""BitVariant.Outline"" TItem=""BitButtonGroupOption"">
    <BitButtonGroupOption Text=""Add"" /><BitButtonGroupOption Text=""Edit"" /><BitButtonGroupOption Text=""Delete"" />
</BitButtonGroup>


<BitButtonGroup Variant=""BitVariant.Text"" TItem=""BitButtonGroupOption"">
    <BitButtonGroupOption Text=""Styled"" IconName=""@BitIconName.Brush"" Style=""color: tomato; border-color: brown; background-color: peachpuff;"" />
    <BitButtonGroupOption Text=""Classed"" IconName=""@BitIconName.FormatPainter"" Class=""custom-item"" />
</BitButtonGroup>

<BitButtonGroup Variant=""BitVariant.Text""
                TItem=""BitButtonGroupOption""
                Styles=""@(new() { Button = ""color: darkcyan; border-color: deepskyblue; background-color: azure;"" })"">
    <BitButtonGroupOption Text=""Add"" /><BitButtonGroupOption Text=""Edit"" /><BitButtonGroupOption Text=""Delete"" />
</BitButtonGroup>

<BitButtonGroup Variant=""BitVariant.Text""
                TItem=""BitButtonGroupOption""
                Classes=""@(new() { Button = ""custom-btn"" })"">
    <BitButtonGroupOption Text=""Add"" /><BitButtonGroupOption Text=""Edit"" /><BitButtonGroupOption Text=""Delete"" />
</BitButtonGroup>";

    private readonly string example13RazorCode = @"
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
