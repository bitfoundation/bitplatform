namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Buttons.ButtonGroup;

public partial class _BitButtonGroupOptionDemo
{
    private readonly string example1RazorCode = @"
<BitButtonGroup TItem=""BitButtonGroupOption"">
    <BitButtonGroupOption Text=""Add"" Key=""add-key"" IconName=""@BitIconName.Add"" />
    <BitButtonGroupOption Text=""Edit"" Key=""edit-key"" IconName=""@BitIconName.Edit"" />
    <BitButtonGroupOption Text=""Delete"" Key=""delete-key"" IconName=""@BitIconName.Delete"" />
</BitButtonGroup>

<BitButtonGroup ButtonStyle=""BitButtonStyle.Standard"" TItem=""BitButtonGroupOption"">
    <BitButtonGroupOption Text=""Add"" Key=""add-key"" IconName=""@BitIconName.Add"" />
    <BitButtonGroupOption Text=""Edit"" Key=""edit-key"" IconName=""@BitIconName.Edit"" />
    <BitButtonGroupOption Text=""Delete"" Key=""delete-key"" IconName=""@BitIconName.Delete"" />
</BitButtonGroup>

<BitButtonGroup ButtonStyle=""BitButtonStyle.Text"" TItem=""BitButtonGroupOption"">
    <BitButtonGroupOption Text=""Add"" Key=""add-key"" IconName=""@BitIconName.Add"" />
    <BitButtonGroupOption Text=""Edit"" Key=""edit-key"" IconName=""@BitIconName.Edit"" />
    <BitButtonGroupOption Text=""Delete"" Key=""delete-key"" IconName=""@BitIconName.Delete"" />
</BitButtonGroup>";

    private readonly string example2RazorCode = @"
<BitButtonGroup ButtonStyle=""BitButtonStyle.Primary"" TItem=""BitButtonGroupOption"">
    <BitButtonGroupOption IsEnabled=""false"" Text=""Add (Disabled)"" Key=""add-key"" IconName=""@BitIconName.Add"" />
    <BitButtonGroupOption Text=""Edit"" Key=""edit-key"" IconName=""@BitIconName.Edit"" />
    <BitButtonGroupOption Text=""Delete"" Key=""delete-key"" IconName=""@BitIconName.Delete"" />
</BitButtonGroup>";

    private readonly string example3RazorCode = @"
<BitButtonGroup ButtonStyle=""BitButtonStyle.Standard"" TItem=""BitButtonGroupOption"">
    <BitButtonGroupOption Text=""Add"" Key=""add-key"" IconName=""@BitIconName.Add"" />
    <BitButtonGroupOption IsEnabled=""false"" Text=""Edit (Disabled)"" Key=""edit-key"" IconName=""@BitIconName.Edit"" />
    <BitButtonGroupOption Text=""Delete"" Key=""delete-key"" IconName=""@BitIconName.Delete"" />
</BitButtonGroup>";

    private readonly string example4RazorCode = @"
<BitButtonGroup ButtonStyle=""BitButtonStyle.Text"" TItem=""BitButtonGroupOption"">
    <BitButtonGroupOption Text=""Add"" Key=""add-key"" IconName=""@BitIconName.Add"" />
    <BitButtonGroupOption Text=""Edit"" Key=""edit-key"" IconName=""@BitIconName.Edit"" />
    <BitButtonGroupOption IsEnabled=""false"" Text=""Delete (Disabled)"" Key=""delete-key"" IconName=""@BitIconName.Delete"" />
</BitButtonGroup>";

    private readonly string example5RazorCode = @"
<div>Click count: @clickCounter</div>
<BitButtonGroup TItem=""BitButtonGroupOption"">
    <BitButtonGroupOption OnClick=""@(() => clickCounter++)"" Text=""Add"" Key=""add-key"" IconName=""@BitIconName.Add"" />
    <BitButtonGroupOption OnClick=""@(() => clickCounter = 0)"" Text=""Reset"" Key=""reset-key"" IconName=""@BitIconName.Refresh"" />
    <BitButtonGroupOption OnClick=""@(() => clickCounter--)"" Text=""Remove"" Key=""remove-key"" IconName=""@BitIconName.Remove"" />
</BitButtonGroup>";
    private readonly string example5CsharpCode = @"
private int clickCounter;";

    private readonly string example6RazorCode = @"
<BitButtonGroup Color=""BitButtonColor.Info"" TItem=""BitButtonGroupOption"">
    <BitButtonGroupOption Text=""Add"" Key=""add-key"" IconName=""@BitIconName.Add"" />
    <BitButtonGroupOption Text=""Edit"" Key=""edit-key"" IconName=""@BitIconName.Edit"" />
    <BitButtonGroupOption Text=""Delete"" Key=""delete-key"" IconName=""@BitIconName.Delete"" />
</BitButtonGroup>

<BitButtonGroup Color=""BitButtonColor.Info"" ButtonStyle=""BitButtonStyle.Standard"" TItem=""BitButtonGroupOption"">
    <BitButtonGroupOption Text=""Add"" Key=""add-key"" IconName=""@BitIconName.Add"" />
    <BitButtonGroupOption Text=""Edit"" Key=""edit-key"" IconName=""@BitIconName.Edit"" />
    <BitButtonGroupOption Text=""Delete"" Key=""delete-key"" IconName=""@BitIconName.Delete"" />
</BitButtonGroup>

<BitButtonGroup Color=""BitButtonColor.Info"" ButtonStyle=""BitButtonStyle.Text"" TItem=""BitButtonGroupOption"">
    <BitButtonGroupOption Text=""Add"" Key=""add-key"" IconName=""@BitIconName.Add"" />
    <BitButtonGroupOption Text=""Edit"" Key=""edit-key"" IconName=""@BitIconName.Edit"" />
    <BitButtonGroupOption Text=""Delete"" Key=""delete-key"" IconName=""@BitIconName.Delete"" />
</BitButtonGroup>


<BitButtonGroup Color=""BitButtonColor.Success"" TItem=""BitButtonGroupOption"">
    <BitButtonGroupOption Text=""Add"" Key=""add-key"" IconName=""@BitIconName.Add"" />
    <BitButtonGroupOption Text=""Edit"" Key=""edit-key"" IconName=""@BitIconName.Edit"" />
    <BitButtonGroupOption Text=""Delete"" Key=""delete-key"" IconName=""@BitIconName.Delete"" />
</BitButtonGroup>

<BitButtonGroup Color=""BitButtonColor.Success"" ButtonStyle=""BitButtonStyle.Standard"" TItem=""BitButtonGroupOption"">
    <BitButtonGroupOption Text=""Add"" Key=""add-key"" IconName=""@BitIconName.Add"" />
    <BitButtonGroupOption Text=""Edit"" Key=""edit-key"" IconName=""@BitIconName.Edit"" />
    <BitButtonGroupOption Text=""Delete"" Key=""delete-key"" IconName=""@BitIconName.Delete"" />
</BitButtonGroup>

<BitButtonGroup Color=""BitButtonColor.Success"" ButtonStyle=""BitButtonStyle.Text"" TItem=""BitButtonGroupOption"">
    <BitButtonGroupOption Text=""Add"" Key=""add-key"" IconName=""@BitIconName.Add"" />
    <BitButtonGroupOption Text=""Edit"" Key=""edit-key"" IconName=""@BitIconName.Edit"" />
    <BitButtonGroupOption Text=""Delete"" Key=""delete-key"" IconName=""@BitIconName.Delete"" />
</BitButtonGroup>


<BitButtonGroup Color=""BitButtonColor.Warning"" TItem=""BitButtonGroupOption"">
    <BitButtonGroupOption Text=""Add"" Key=""add-key"" IconName=""@BitIconName.Add"" />
    <BitButtonGroupOption Text=""Edit"" Key=""edit-key"" IconName=""@BitIconName.Edit"" />
    <BitButtonGroupOption Text=""Delete"" Key=""delete-key"" IconName=""@BitIconName.Delete"" />
</BitButtonGroup>

<BitButtonGroup Color=""BitButtonColor.Warning"" ButtonStyle=""BitButtonStyle.Standard"" TItem=""BitButtonGroupOption"">
    <BitButtonGroupOption Text=""Add"" Key=""add-key"" IconName=""@BitIconName.Add"" />
    <BitButtonGroupOption Text=""Edit"" Key=""edit-key"" IconName=""@BitIconName.Edit"" />
    <BitButtonGroupOption Text=""Delete"" Key=""delete-key"" IconName=""@BitIconName.Delete"" />
</BitButtonGroup>

<BitButtonGroup Color=""BitButtonColor.Warning"" ButtonStyle=""BitButtonStyle.Text"" TItem=""BitButtonGroupOption"">
    <BitButtonGroupOption Text=""Add"" Key=""add-key"" IconName=""@BitIconName.Add"" />
    <BitButtonGroupOption Text=""Edit"" Key=""edit-key"" IconName=""@BitIconName.Edit"" />
    <BitButtonGroupOption Text=""Delete"" Key=""delete-key"" IconName=""@BitIconName.Delete"" />
</BitButtonGroup>


<BitButtonGroup Color=""BitButtonColor.SevereWarning"" TItem=""BitButtonGroupOption"">
    <BitButtonGroupOption Text=""Add"" Key=""add-key"" IconName=""@BitIconName.Add"" />
    <BitButtonGroupOption Text=""Edit"" Key=""edit-key"" IconName=""@BitIconName.Edit"" />
    <BitButtonGroupOption Text=""Delete"" Key=""delete-key"" IconName=""@BitIconName.Delete"" />
</BitButtonGroup>

<BitButtonGroup Color=""BitButtonColor.SevereWarning"" ButtonStyle=""BitButtonStyle.Standard"" TItem=""BitButtonGroupOption"">
    <BitButtonGroupOption Text=""Add"" Key=""add-key"" IconName=""@BitIconName.Add"" />
    <BitButtonGroupOption Text=""Edit"" Key=""edit-key"" IconName=""@BitIconName.Edit"" />
    <BitButtonGroupOption Text=""Delete"" Key=""delete-key"" IconName=""@BitIconName.Delete"" />
</BitButtonGroup>

<BitButtonGroup Color=""BitButtonColor.SevereWarning"" ButtonStyle=""BitButtonStyle.Text"" TItem=""BitButtonGroupOption"">
    <BitButtonGroupOption Text=""Add"" Key=""add-key"" IconName=""@BitIconName.Add"" />
    <BitButtonGroupOption Text=""Edit"" Key=""edit-key"" IconName=""@BitIconName.Edit"" />
    <BitButtonGroupOption Text=""Delete"" Key=""delete-key"" IconName=""@BitIconName.Delete"" />
</BitButtonGroup>


<BitButtonGroup Color=""BitButtonColor.Error"" TItem=""BitButtonGroupOption"">
    <BitButtonGroupOption Text=""Add"" Key=""add-key"" IconName=""@BitIconName.Add"" />
    <BitButtonGroupOption Text=""Edit"" Key=""edit-key"" IconName=""@BitIconName.Edit"" />
    <BitButtonGroupOption Text=""Delete"" Key=""delete-key"" IconName=""@BitIconName.Delete"" />
</BitButtonGroup>

<BitButtonGroup Color=""BitButtonColor.Error"" ButtonStyle=""BitButtonStyle.Standard"" TItem=""BitButtonGroupOption"">
    <BitButtonGroupOption Text=""Add"" Key=""add-key"" IconName=""@BitIconName.Add"" />
    <BitButtonGroupOption Text=""Edit"" Key=""edit-key"" IconName=""@BitIconName.Edit"" />
    <BitButtonGroupOption Text=""Delete"" Key=""delete-key"" IconName=""@BitIconName.Delete"" />
</BitButtonGroup>

<BitButtonGroup Color=""BitButtonColor.Error"" ButtonStyle=""BitButtonStyle.Text"" TItem=""BitButtonGroupOption"">
    <BitButtonGroupOption Text=""Add"" Key=""add-key"" IconName=""@BitIconName.Add"" />
    <BitButtonGroupOption Text=""Edit"" Key=""edit-key"" IconName=""@BitIconName.Edit"" />
    <BitButtonGroupOption Text=""Delete"" Key=""delete-key"" IconName=""@BitIconName.Delete"" />
</BitButtonGroup>";

    private readonly string example7RazorCode = @"
<BitButtonGroup Size=""BitButtonSize.Small"" ButtonStyle=""BitButtonStyle.Text"" TItem=""BitButtonGroupOption"">
    <BitButtonGroupOption Text=""Add"" Key=""add-key"" IconName=""@BitIconName.Add"" />
    <BitButtonGroupOption Text=""Edit"" Key=""edit-key"" IconName=""@BitIconName.Edit"" />
    <BitButtonGroupOption Text=""Delete"" Key=""delete-key"" IconName=""@BitIconName.Delete"" />
</BitButtonGroup>

<BitButtonGroup Size=""BitButtonSize.Medium"" ButtonStyle=""BitButtonStyle.Standard"" TItem=""BitButtonGroupOption"">
    <BitButtonGroupOption Text=""Add"" Key=""add-key"" IconName=""@BitIconName.Add"" />
    <BitButtonGroupOption Text=""Edit"" Key=""edit-key"" IconName=""@BitIconName.Edit"" />
    <BitButtonGroupOption Text=""Delete"" Key=""delete-key"" IconName=""@BitIconName.Delete"" />
</BitButtonGroup>

<BitButtonGroup Size=""BitButtonSize.Large"" ButtonStyle=""BitButtonStyle.Primary"" TItem=""BitButtonGroupOption"">
    <BitButtonGroupOption Text=""Add"" Key=""add-key"" IconName=""@BitIconName.Add"" />
    <BitButtonGroupOption Text=""Edit"" Key=""edit-key"" IconName=""@BitIconName.Edit"" />
    <BitButtonGroupOption Text=""Delete"" Key=""delete-key"" IconName=""@BitIconName.Delete"" />
</BitButtonGroup>";

    private readonly string example8RazorCode = @"
<style>
    .custom-class {
        color: aqua;
        background-color: goldenrod;
    }
</style>

<BitButtonGroup ButtonStyle=""BitButtonStyle.Primary"" TItem=""BitButtonGroupOption"">
    <BitButtonGroupOption Style=""color:darkblue; font-weight:bold;"" Text=""Styled"" Key=""styled-key"" IconName=""@BitIconName.Brush"" />
    <BitButtonGroupOption Class=""custom-class"" Text=""Classed"" Key=""classed-key"" IconName=""@BitIconName.FormatPainter"" />
</BitButtonGroup>";

    private readonly string example9RazorCode = @"
<BitButtonGroup Vertical TItem=""BitButtonGroupOption"">
    <BitButtonGroupOption Text=""Add"" Key=""add-key"" IconName=""@BitIconName.Add"" />
    <BitButtonGroupOption Text=""Edit"" Key=""edit-key"" IconName=""@BitIconName.Edit"" />
    <BitButtonGroupOption Text=""Delete"" Key=""delete-key"" IconName=""@BitIconName.Delete"" />
</BitButtonGroup>

<BitButtonGroup Vertical ButtonStyle=""BitButtonStyle.Standard"" TItem=""BitButtonGroupOption"">
    <BitButtonGroupOption Text=""Add"" Key=""add-key"" IconName=""@BitIconName.Add"" />
    <BitButtonGroupOption Text=""Edit"" Key=""edit-key"" IconName=""@BitIconName.Edit"" />
    <BitButtonGroupOption Text=""Delete"" Key=""delete-key"" IconName=""@BitIconName.Delete"" />
</BitButtonGroup>

<BitButtonGroup Vertical ButtonStyle=""BitButtonStyle.Text"" TItem=""BitButtonGroupOption"">
    <BitButtonGroupOption Text=""Add"" Key=""add-key"" IconName=""@BitIconName.Add"" />
    <BitButtonGroupOption Text=""Edit"" Key=""edit-key"" IconName=""@BitIconName.Edit"" />
    <BitButtonGroupOption Text=""Delete"" Key=""delete-key"" IconName=""@BitIconName.Delete"" />
</BitButtonGroup>";

    private readonly string example10RazorCode = @"
Visible: [ <BitButtonGroup Visibility=""BitVisibility.Visible"" TItem=""BitButtonGroupOption"">
               <BitButtonGroupOption Text=""Add"" Key=""add-key"" IconName=""@BitIconName.Add"" />
               <BitButtonGroupOption Text=""Edit"" Key=""edit-key"" IconName=""@BitIconName.Edit"" />
               <BitButtonGroupOption Text=""Ok"" Key=""ok-key"" IconName=""@BitIconName.CheckMark"" />
           </BitButtonGroup> ]

Hidden: [ <BitButtonGroup Visibility=""BitVisibility.Hidden"" TItem=""BitButtonGroupOption"">
              <BitButtonGroupOption Text=""Add"" Key=""add-key"" IconName=""@BitIconName.Add"" />
              <BitButtonGroupOption Text=""Edit"" Key=""edit-key"" IconName=""@BitIconName.Edit"" />
              <BitButtonGroupOption Text=""Ok"" Key=""ok-key"" IconName=""@BitIconName.CheckMark"" />
          </BitButtonGroup> ]

Collapsed: [ <BitButtonGroup Visibility=""BitVisibility.Collapsed"" TItem=""BitButtonGroupOption"">
                 <BitButtonGroupOption Text=""Add"" Key=""add-key"" IconName=""@BitIconName.Add"" />
                 <BitButtonGroupOption Text=""Edit"" Key=""edit-key"" IconName=""@BitIconName.Edit"" />
                 <BitButtonGroupOption Text=""Ok"" Key=""ok-key"" IconName=""@BitIconName.CheckMark"" />
             </BitButtonGroup> ]";
}
