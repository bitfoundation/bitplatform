﻿namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Buttons;

public partial class BitCompoundButtonDemo
{
    private readonly string example1RazorCode = @"
<BitCompoundButton SecondaryText=""This is the secondary text"">Compound Button</BitCompoundButton>";

    private readonly string example2RazorCode = @"
<BitCompoundButton Variant=""BitVariant.Fill"" SecondaryText=""This is the secondary text"">Fill</BitCompoundButton>
<BitCompoundButton Variant=""BitVariant.Outline"" SecondaryText=""This is the secondary text"">Outline</BitCompoundButton>
<BitCompoundButton Variant=""BitVariant.Text"" SecondaryText=""This is the secondary text"">Text</BitCompoundButton>";

    private readonly string example3RazorCode = @"
<BitCompoundButton SecondaryText=""This is the secondary text"">Fill</BitCompoundButton>
<BitCompoundButton SecondaryText=""This is the secondary text"" IsEnabled=""false"">Disabled</BitCompoundButton>
<BitCompoundButton SecondaryText=""This is the secondary text"" Href=""https://bitplatform.dev"">Link</BitCompoundButton>";

    private readonly string example4RazorCode = @"
<BitCompoundButton Variant=""BitVariant.Outline"" SecondaryText=""This is the secondary text"">Outline</BitCompoundButton>
<BitCompoundButton Variant=""BitVariant.Outline"" SecondaryText=""This is the secondary text"" IsEnabled=""false"">Disabled</BitCompoundButton>
<BitCompoundButton Variant=""BitVariant.Outline"" SecondaryText=""This is the secondary text"" Href=""https://bitplatform.dev"">Link</BitCompoundButton>";

    private readonly string example5RazorCode = @"
<BitCompoundButton Variant=""BitVariant.Text"" SecondaryText=""This is the secondary text"">Text</BitCompoundButton>
<BitCompoundButton Variant=""BitVariant.Text"" SecondaryText=""This is the secondary text"" IsEnabled=""false"">Disabled</BitCompoundButton>
<BitCompoundButton Variant=""BitVariant.Text"" SecondaryText=""This is the secondary text"" Href=""https://bitplatform.dev"">Link</BitCompoundButton>";

    private readonly string example6RazorCode = @"
<BitCompoundButton IconName=""@BitIconName.Emoji"" SecondaryText=""IconPosition Start"">
    Default (Start)
</BitCompoundButton>

<BitCompoundButton IconName=""@BitIconName.Emoji2""
                   IconPosition=""BitButtonIconPosition.End""
                   SecondaryText=""IconPosition End""
                   Variant=""BitVariant.Outline"">
    End
</BitCompoundButton>";

    private readonly string example7RazorCode = @"
<BitCompoundButton OnClick=""() => clickCounter++"" SecondaryText=""@($""Click count is: {@clickCounter}"")"">Click me</BitCompoundButton>";
    private readonly string example7CsharpCode = @"
private int clickCounter;";

    private readonly string example8RazorCode = @"
<EditForm Model=""validationButtonModel"" OnValidSubmit=""HandleValidSubmit"">
    <DataAnnotationsValidator />

    <BitTextField Label=""Required"" Required @bind-Value=""validationButtonModel.RequiredText"" />
    <ValidationMessage For=""() => validationButtonModel.RequiredText"" />
    <br />
    <BitTextField Label=""Non Required"" @bind-Value=""validationButtonModel.NonRequiredText"" />
    <ValidationMessage For=""() => validationButtonModel.NonRequiredText"" />
    <br />
    <div class=""buttons-container"">
        <BitCompoundButton ButtonType=BitButtonType.Submit Text=""Submit"" SecondaryText=""This is a Submit button"" />
        <BitCompoundButton ButtonType=BitButtonType.Reset Text=""Reset"" SecondaryText=""This is a Reset button"" />
        <BitCompoundButton ButtonType=BitButtonType.Button Text=""Button"" SecondaryText=""This is just a button"" />
    </div>
</EditForm>";
    private readonly string example8CsharpCode = @"
public class ButtonValidationModel
{
    [Required]
    public string RequiredText { get; set; } = string.Empty;

    public string? NonRequiredText { get; set; }
}

private ButtonValidationModel buttonValidationModel = new();

private async Task HandleValidSubmit()
{
    await Task.Delay(2000);

    buttonValidationModel = new();

    StateHasChanged();
}";

    private readonly string example9RazorCode = @"
<BitCompoundButton>
    <PrimaryTemplate>
        <span style=""color:red"">Primary Template!</span>
    </PrimaryTemplate>
    <SecondaryTemplate>
        <BitIcon IconName=""@BitIconName.AirplaneSolid"" />
        <span style=""color:blueviolet"">Secondary Template goes here!</span>
    </SecondaryTemplate>
</BitCompoundButton>

<BitCompoundButton Variant=""BitVariant.Outline"">
    <PrimaryTemplate>
        <span style=""color:darkcyan"">Primary Template!</span>
    </PrimaryTemplate>
    <SecondaryTemplate>
        <span style=""color:blueviolet"">Secondary Template goes here!</span>
        <BitGridLoading Size=""20"" Color=""@($""var({BitCss.Var.Color.Foreground.Primary})"")"" />
    </SecondaryTemplate>
</BitCompoundButton>";

    private readonly string example10RazorCode = @"
<BitCompoundButton Severity=""BitSeverity.Info"" SecondaryText=""This is the secondary text"">Info</BitCompoundButton>
<BitCompoundButton Severity=""BitSeverity.Info"" SecondaryText=""This is the secondary text"" Variant=""BitVariant.Outline"">Info</BitCompoundButton>
<BitCompoundButton Severity=""BitSeverity.Info"" SecondaryText=""This is the secondary text"" Variant=""BitVariant.Text"">Info</BitCompoundButton>

<BitCompoundButton Severity=""BitSeverity.Success"" SecondaryText=""This is the secondary text"">Success</BitCompoundButton>
<BitCompoundButton Severity=""BitSeverity.Success"" SecondaryText=""This is the secondary text"" Variant=""BitVariant.Outline"">Success</BitCompoundButton>
<BitCompoundButton Severity=""BitSeverity.Success"" SecondaryText=""This is the secondary text"" Variant=""BitVariant.Text"">Success</BitCompoundButton>

<BitCompoundButton Severity=""BitSeverity.Warning"" SecondaryText=""This is the secondary text"">Warning</BitCompoundButton>
<BitCompoundButton Severity=""BitSeverity.Warning"" SecondaryText=""This is the secondary text"" Variant=""BitVariant.Outline"">Warning</BitCompoundButton>
<BitCompoundButton Severity=""BitSeverity.Warning"" SecondaryText=""This is the secondary text"" Variant=""BitVariant.Text"">Warning</BitCompoundButton>

<BitCompoundButton Severity=""BitSeverity.SevereWarning"" SecondaryText=""This is the secondary text"">SevereWarning</BitCompoundButton>
<BitCompoundButton Severity=""BitSeverity.SevereWarning"" SecondaryText=""This is the secondary text"" Variant=""BitVariant.Outline"">SevereWarning</BitCompoundButton>
<BitCompoundButton Severity=""BitSeverity.SevereWarning"" SecondaryText=""This is the secondary text"" Variant=""BitVariant.Text"">SevereWarning</BitCompoundButton>

<BitCompoundButton Severity=""BitSeverity.Error"" SecondaryText=""This is the secondary text"">Error</BitCompoundButton>
<BitCompoundButton Severity=""BitSeverity.Error"" SecondaryText=""This is the secondary text"" Variant=""BitVariant.Outline"">Error</BitCompoundButton>
<BitCompoundButton Severity=""BitSeverity.Error"" SecondaryText=""This is the secondary text"" Variant=""BitVariant.Text"">Error</BitCompoundButton>";

    private readonly string example11RazorCode = @"
<BitCompoundButton Size=""BitSize.Small"" SecondaryText=""This is the secondary text"" Variant=""BitVariant.Fill"">Small</BitCompoundButton>
<BitCompoundButton Size=""BitSize.Medium"" SecondaryText=""This is the secondary text"" Variant=""BitVariant.Fill"">Medium</BitCompoundButton>
<BitCompoundButton Size=""BitSize.Large"" SecondaryText=""This is the secondary text"" Variant=""BitVariant.Fill"">Large</BitCompoundButton>

<BitCompoundButton Size=""BitSize.Small"" SecondaryText=""This is the secondary text"" Variant=""BitVariant.Outline"">Small</BitCompoundButton>
<BitCompoundButton Size=""BitSize.Medium"" SecondaryText=""This is the secondary text"" Variant=""BitVariant.Outline"">Medium</BitCompoundButton>
<BitCompoundButton Size=""BitSize.Large"" SecondaryText=""This is the secondary text"" Variant=""BitVariant.Outline"">Large</BitCompoundButton>

<BitCompoundButton Size=""BitSize.Small"" SecondaryText=""This is the secondary text"" Variant=""BitVariant.Text"">Small</BitCompoundButton>
<BitCompoundButton Size=""BitSize.Medium"" SecondaryText=""This is the secondary text"" Variant=""BitVariant.Text"">Medium</BitCompoundButton>
<BitCompoundButton Size=""BitSize.Large"" SecondaryText=""This is the secondary text"" Variant=""BitVariant.Text"">Large</BitCompoundButton>";

    private readonly string example12RazorCode = @"
<style>
    .custom-container {
        line-height: 2;
    }

    .custom-primary {
        line-height: 2;
        font-weight: 900;
        color: goldenrod;
    }

    .custom-secondary {
        line-height: 2;
        font-weight: 600;
        color: orangered;
    }
</style>


<BitCompoundButton SecondaryText=""This is secondary text""
                   Styles=""@(new() { TextContainer = ""line-height: 2;"",
                                     Primary = ""color: darkmagenta;"",
                                     Secondary = ""color: darkslateblue;"" })"">
    Fill
</BitCompoundButton>

<BitCompoundButton SecondaryText=""This is secondary text""
                   Variant=""BitVariant.Outline""
                   Classes=""@(new() { TextContainer = ""custom-container"",
                                      Primary = ""custom-primary"",
                                      Secondary = ""custom-secondary"" })"">
    Outline
</BitCompoundButton>";

    private readonly string example14RazorCode = @"
<BitCompoundButton Dir=""BitDir.Rtl"" 
                   IconName=""@BitIconName.Emoji"" 
                   SecondaryText=""محل قرار گیری نماد در ابتدا"">
    پیش فرض (ابتدا)
</BitCompoundButton>

<BitCompoundButton Dir=""BitDir.Rtl""
                   IconName=""@BitIconName.Emoji2""
                   Variant=""BitVariant.Outline""
                   IconPosition=""BitButtonIconPosition.End""
                   SecondaryText=""محل قرار گیری نماد در انتها"">
    انتها
</BitCompoundButton>";
}
