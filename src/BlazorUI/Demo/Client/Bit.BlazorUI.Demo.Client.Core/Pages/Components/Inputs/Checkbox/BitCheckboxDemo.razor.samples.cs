namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Inputs.Checkbox;

public partial class BitCheckboxDemo
{

    private readonly string example1RazorCode = @"
<BitCheckbox Label=""Basic checkbox"" />
<BitCheckbox Label=""Disable checkbox"" IsEnabled=""false"" />
<BitCheckbox Label=""Disable checked checkbox"" IsEnabled=""false"" Value=""true"" />";

    private readonly string example2RazorCode = @"
<BitCheckbox Label=""Custom check icon"" CheckIconName=""@BitIconName.Heart"" />
<BitCheckbox Label=""Disabled custom check icon"" CheckIconName=""@BitIconName.WavingHand"" Value=""true"" IsEnabled=""false"" />";

    private readonly string example3RazorCode = @"
<BitCheckbox Label=""Reversed"" Reversed />
<BitCheckbox Label=""Reversed - Disabled"" Reversed IsEnabled=""false"" />
<BitCheckbox Label=""Reversed - Disable Checked"" Reversed IsEnabled=""false"" Value=""true"" />";

    private readonly string example4RazorCode = @"
<BitCheckbox>
    <LabelTemplate>
        <BitTag Color=""BitColor.Success"">Label Template</BitTag>
    </LabelTemplate>
</BitCheckbox>";

    private readonly string example5RazorCode = @"
<BitCheckbox Label=""Indeterminate checkbox"" Indeterminate />
<BitCheckbox Label=""Disabled indeterminate checkbox"" Indeterminate IsEnabled=""false"" />";

    private readonly string example6RazorCode = @"
<BitCheckbox Label=""One-way checked (Fixed)"" Value=""true"" />

<BitCheckbox Label=""One-way"" Value=""oneWayValue"" />
<BitToggleButton @bind-IsChecked=""oneWayValue"" Text=""Toggle"" />

<BitCheckbox Label=""Two-way controlled checkbox"" @bind-Value=""twoWayValue"" />
<BitToggleButton @bind-IsChecked=""twoWayValue"" Text=""Toggle"" />


<BitCheckbox Label=""One-way indeterminate (Fixed)"" Indeterminate />

<BitCheckbox Label=""One-way indeterminate"" Indeterminate=""oneWayIndeterminate"" />
<BitToggleButton @bind-IsChecked=""oneWayIndeterminate"" Text=""Toggle"" />

<BitCheckbox Label=""Two-way indeterminate"" @bind-Indeterminate=""twoWayIndeterminate"" />
<BitToggleButton @bind-IsChecked=""twoWayIndeterminate"" Text=""Toggle"" />";
    private readonly string example6CsharpCode = @"
private bool oneWayValue;
private bool twoWayValue;
private bool oneWayIndeterminate = true;
private bool twoWayIndeterminate = true;";

    private readonly string example7RazorCode = @"
<style>
    .validation-message {
        color: red;
        font-size: 0.75rem;
    }
</style>

<EditForm Model=""validationModel""
          OnValidSubmit=""HandleValidSubmit""
          OnInvalidSubmit=""HandleInvalidSubmit"">
    <DataAnnotationsValidator />
    <BitCheckbox Label=""I agree with the terms and conditions.""
                 @bind-Value=""validationModel.TermsAgreement"" />
    <ValidationMessage For=""@(() => validationModel.TermsAgreement)"" />

    <BitButton ButtonType=""BitButtonType.Submit"">Submit</BitButton>
</EditForm>";
    private readonly string example7CsharpCode = @"
private BitCheckboxValidationModel validationModel = new();

public class BitCheckboxValidationModel
{
    [Range(typeof(bool), ""true"", ""true"", ErrorMessage = ""You must agree to the terms and conditions."")]
    public bool TermsAgreement { get; set; }
}

private async Task HandleValidSubmit() { }

private void HandleInvalidSubmit() { }";

    private readonly string example8RazorCode = @"
<BitCheckbox @bind-Value=""customCheckboxValue"">
    <BitIcon Style=""border:1px solid gray;width:22px;height:22px""
             IconName=""@(customCheckboxValue ? BitIconName.Accept : null)"" />
    <span>Custom basic checkbox</span>
</BitCheckbox>


<BitCheckbox @bind-Value=""customContentValue"" @bind-Indeterminate=""customContentIndeterminate"">
    <BitIcon Style=""border:1px solid gray;width:22px;height:22px""
             IconName=""@(customContentIndeterminate ? BitIconName.Fingerprint : (customContentValue ? BitIconName.Accept : null))"" />
    <span>Custom indeterminate checkbox</span>
</BitCheckbox>
<BitButton OnClick=""() => customContentIndeterminate = true"">Make Indeterminate</BitButton>";
    private readonly string example8CsharpCode = @"
private bool customCheckboxValue;
private bool customContentValue;
private bool customContentIndeterminate = true;";

    private readonly string example9RazorCode = @"
<BitCheckbox Size=""BitSize.Small"" Label=""Checkbox"" />
<BitCheckbox Size=""BitSize.Small"" Label=""Checkbox"" Indeterminate />
<BitCheckbox Size=""BitSize.Small"" Label=""Checkbox"" Value />
                
<BitCheckbox Size=""BitSize.Medium"" Label=""Checkbox"" />
<BitCheckbox Size=""BitSize.Medium"" Label=""Checkbox"" Indeterminate />
<BitCheckbox Size=""BitSize.Medium"" Label=""Checkbox"" Value />
                
<BitCheckbox Size=""BitSize.Large"" Label=""Checkbox"" />
<BitCheckbox Size=""BitSize.Large"" Label=""Checkbox"" Indeterminate />
<BitCheckbox Size=""BitSize.Large"" Label=""Checkbox"" Value />";

    private readonly string example10RazorCode = @"
<BitCheckbox Color=""BitColor.Primary"" Label=""Primary"" />
<BitCheckbox Color=""BitColor.Primary"" Label=""Primary"" Indeterminate />
<BitCheckbox Color=""BitColor.Primary"" Label=""Primary"" Value />
                
<BitCheckbox Color=""BitColor.Secondary"" Label=""Secondary"" />
<BitCheckbox Color=""BitColor.Secondary"" Label=""Secondary"" Indeterminate />
<BitCheckbox Color=""BitColor.Secondary"" Label=""Secondary"" Value />
                
<BitCheckbox Color=""BitColor.Tertiary"" Label=""Tertiary"" />
<BitCheckbox Color=""BitColor.Tertiary"" Label=""Tertiary"" Indeterminate />
<BitCheckbox Color=""BitColor.Tertiary"" Label=""Tertiary"" Value />
                
<BitCheckbox Color=""BitColor.Info"" Label=""Info"" />
<BitCheckbox Color=""BitColor.Info"" Label=""Info"" Indeterminate />
<BitCheckbox Color=""BitColor.Info"" Label=""Info"" Value />
                
<BitCheckbox Color=""BitColor.Success"" Label=""Success"" />
<BitCheckbox Color=""BitColor.Success"" Label=""Success"" Indeterminate />
<BitCheckbox Color=""BitColor.Success"" Label=""Success"" Value />
               
<BitCheckbox Color=""BitColor.Warning"" Label=""Warning"" />
<BitCheckbox Color=""BitColor.Warning"" Label=""Warning"" Indeterminate />
<BitCheckbox Color=""BitColor.Warning"" Label=""Warning"" Value />
                
<BitCheckbox Color=""BitColor.SevereWarning"" Label=""SevereWarning"" />
<BitCheckbox Color=""BitColor.SevereWarning"" Label=""SevereWarning"" Indeterminate />
<BitCheckbox Color=""BitColor.SevereWarning"" Label=""SevereWarning"" Value />
                
<BitCheckbox Color=""BitColor.Error"" Label=""Error"" />
<BitCheckbox Color=""BitColor.Error"" Label=""Error"" Indeterminate />
<BitCheckbox Color=""BitColor.Error"" Label=""Error"" Value />
                
<BitCheckbox Color=""BitColor.PrimaryBackground"" Label=""PrimaryBackground"" />
<BitCheckbox Color=""BitColor.PrimaryBackground"" Label=""PrimaryBackground"" Indeterminate />
<BitCheckbox Color=""BitColor.PrimaryBackground"" Label=""PrimaryBackground"" Value />
                    
<BitCheckbox Color=""BitColor.SecondaryBackground"" Label=""SecondaryBackground"" />
<BitCheckbox Color=""BitColor.SecondaryBackground"" Label=""SecondaryBackground"" Indeterminate />
<BitCheckbox Color=""BitColor.SecondaryBackground"" Label=""SecondaryBackground"" Value />
                    
<BitCheckbox Color=""BitColor.TertiaryBackground"" Label=""TertiaryBackground"" />
<BitCheckbox Color=""BitColor.TertiaryBackground"" Label=""TertiaryBackground"" Indeterminate />
<BitCheckbox Color=""BitColor.TertiaryBackground"" Label=""TertiaryBackground"" Value />
                    
<BitCheckbox Color=""BitColor.PrimaryForeground"" Label=""PrimaryForeground"" />
<BitCheckbox Color=""BitColor.PrimaryForeground"" Label=""PrimaryForeground"" Indeterminate />
<BitCheckbox Color=""BitColor.PrimaryForeground"" Label=""PrimaryForeground"" Value />
                    
<BitCheckbox Color=""BitColor.SecondaryForeground"" Label=""SecondaryForeground"" />
<BitCheckbox Color=""BitColor.SecondaryForeground"" Label=""SecondaryForeground"" Indeterminate />
<BitCheckbox Color=""BitColor.SecondaryForeground"" Label=""SecondaryForeground"" Value />
                    
<BitCheckbox Color=""BitColor.TertiaryForeground"" Label=""TertiaryForeground"" />
<BitCheckbox Color=""BitColor.TertiaryForeground"" Label=""TertiaryForeground"" Indeterminate />
<BitCheckbox Color=""BitColor.TertiaryForeground"" Label=""TertiaryForeground"" Value />
                    
<BitCheckbox Color=""BitColor.PrimaryBorder"" Label=""PrimaryBorder"" />
<BitCheckbox Color=""BitColor.PrimaryBorder"" Label=""PrimaryBorder"" Indeterminate />
<BitCheckbox Color=""BitColor.PrimaryBorder"" Label=""PrimaryBorder"" Value />
                    
<BitCheckbox Color=""BitColor.SecondaryBorder"" Label=""SecondaryBorder"" />
<BitCheckbox Color=""BitColor.SecondaryBorder"" Label=""SecondaryBorder"" Indeterminate />
<BitCheckbox Color=""BitColor.SecondaryBorder"" Label=""SecondaryBorder"" Value />
                    
<BitCheckbox Color=""BitColor.TertiaryBorder"" Label=""TertiaryBorder"" />
<BitCheckbox Color=""BitColor.TertiaryBorder"" Label=""TertiaryBorder"" Indeterminate />
<BitCheckbox Color=""BitColor.TertiaryBorder"" Label=""TertiaryBorder"" Value />";

    private readonly string example11RazorCode = @"
<style>
    .custom-class {
        padding: 0.5rem;
        border-radius: 0.125rem;
        background-color: #d3d3d347;
        border: 1px solid dodgerblue;
    }


    .custom-label {
        font-weight: bold;
        color: lightseagreen;
    }

    .custom-icon {
        color: lightseagreen
    }

    .custom-box {
        border-radius: 0.2rem;
        border-color: lightseagreen;
    }

    .custom-checked .custom-icon {
        color: white
    }

    .custom-checked:hover .custom-icon {
        color: whitesmoke;
    }

    .custom-checked .custom-box {
        background-color: lightseagreen;
    }

    .custom-checked:hover .custom-box {
        border-color: mediumseagreen;
    }
</style>


<BitCheckbox Label=""Styled checkbox"" Style=""color: dodgerblue; text-shadow: lightskyblue 0 0 1rem;"" />

<BitCheckbox Label=""Classed checkbox"" Class=""custom-class"" />


<BitCheckbox Label=""Styles"" 
             Styles=""@(new() { Checked = ""--check-color: deeppink; --icon-color: white;"", 
                               Label = ""color: var(--check-color);"",
                               Box = ""border-radius: 50%; border-color: var(--check-color); background-color: var(--check-color);"",
                               Icon = ""color: var(--icon-color);"" })"" />

<BitCheckbox Label=""Classes"" 
             Classes=""@(new() { Checked = ""custom-checked"",
                                Icon = ""custom-icon"",
                                Label=""custom-label"",
                                Box=""custom-box"" })"" />";

    private readonly string example12RazorCode = @"
<BitCheckbox Dir=""BitDir.Rtl"" Label=""چکباکس راست به چپ"" />
<BitCheckbox Dir=""BitDir.Rtl"" Label=""چکباکس غیرفعال"" IsEnabled=""false"" />
<BitCheckbox Dir=""BitDir.Rtl"" Label=""چکباکس غیرفعال چک شده"" IsEnabled=""false"" Value=""true"" />";
}
