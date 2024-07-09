namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Buttons;

public partial class BitButtonDemo
{
    private readonly string example1RazorCode = @"
<BitButton>Button</BitButton>";

    private readonly string example2RazorCode = @"
<BitButton Variant=""BitVariant.Fill"">Fill</BitButton>
<BitButton Variant=""BitVariant.Outline"">Outline</BitButton>
<BitButton Variant=""BitVariant.Text"">Text</BitButton>

<BitButton Variant=""BitVariant.Fill"" IsEnabled=""false"">Fill</BitButton>
<BitButton Variant=""BitVariant.Outline"" IsEnabled=""false"">Outline</BitButton>
<BitButton Variant=""BitVariant.Text"" IsEnabled=""false"">Text</BitButton>";

    private readonly string example3RazorCode = @"
<BitButton Color=""BitColor.Primary"">Primary</BitButton>
<BitButton Color=""BitColor.Primary"" Variant=""BitVariant.Outline"">Primary</BitButton>
<BitButton Color=""BitColor.Primary"" Variant=""BitVariant.Text"">Primary</BitButton>

<BitButton Color=""BitColor.Secondary"">Secondary</BitButton>
<BitButton Color=""BitColor.Secondary"" Variant=""BitVariant.Outline"">Secondary</BitButton>
<BitButton Color=""BitColor.Secondary"" Variant=""BitVariant.Text"">Secondary</BitButton>

<BitButton Color=""BitColor.Tertiary"">Tertiary</BitButton>
<BitButton Color=""BitColor.Tertiary"" Variant=""BitVariant.Outline"">Tertiary</BitButton>
<BitButton Color=""BitColor.Tertiary"" Variant=""BitVariant.Text"">Tertiary</BitButton>

<BitButton Color=""BitColor.Info"">Info</BitButton>
<BitButton Color=""BitColor.Info"" Variant=""BitVariant.Outline"">Info</BitButton>
<BitButton Color=""BitColor.Info"" Variant=""BitVariant.Text"">Info</BitButton>

<BitButton Color=""BitColor.Success"">Success</BitButton>
<BitButton Color=""BitColor.Success"" Variant=""BitVariant.Outline"">Success</BitButton>
<BitButton Color=""BitColor.Success"" Variant=""BitVariant.Text"">Success</BitButton>

<BitButton Color=""BitColor.Warning"">Warning</BitButton>
<BitButton Color=""BitColor.Warning"" Variant=""BitVariant.Outline"">Warning</BitButton>
<BitButton Color=""BitColor.Warning"" Variant=""BitVariant.Text"">Warning</BitButton>

<BitButton Color=""BitColor.SevereWarning"">SevereWarning</BitButton>
<BitButton Color=""BitColor.SevereWarning"" Variant=""BitVariant.Outline"">SevereWarning</BitButton>
<BitButton Color=""BitColor.SevereWarning"" Variant=""BitVariant.Text"">SevereWarning</BitButton>

<BitButton Color=""BitColor.Error"">Error</BitButton>
<BitButton Color=""BitColor.Error"" Variant=""BitVariant.Outline"">Error</BitButton>
<BitButton Color=""BitColor.Error"" Variant=""BitVariant.Text"">Error</BitButton>";

    private readonly string example4RazorCode = @"
<style>
    .custom-class {
        color: blueviolet;
        border-radius: 1rem;
    }

    .custom-root {
        min-width: 7.2rem;
        box-shadow: aqua 0 0 0.5rem;
    }

    .custom-container {
        text-shadow: aqua 0 0 0.5rem;
    }

    .custom-label {
        color: goldenrod;
    }

    .custom-spinner {
        border-color: aqua;
        border-top-color: goldenrod;
    }
</style>


<BitButton Style=""color:darkblue; font-weight:bold"">
    Styled Button
</BitButton>

<BitButton Class=""custom-class"" Variant=""BitVariant.Outline"">
    Classed Button
</BitButton>


<BitButton IsLoading=""stylesIsLoading""
           LoadingLabel=""Wait...""
           OnClick=""LoadingStylesClick""
           Styles=""@(new() { Root = ""border-radius: 1rem; min-width: 6rem;"",
                             LoadingLabel = ""color: tomato;"",
                             Spinner = ""border-color: goldenrod; border-top-color: tomato;"" })"">
    Fill
</BitButton>

<BitButton IsLoading=""classesIsLoading""
           LoadingLabel=""Sending...""
           OnClick=""LoadingClassesClick"" 
           Variant=""BitVariant.Outline""
           Classes=""@(new() { Root = ""custom-root"",
                              LoadingContainer = ""custom-container"",
                              LoadingLabel = ""custom-label"",
                              Spinner = ""custom-spinner"" })"">
    Outline
</BitButton>";
    private readonly string example4CsharpCode = @"
private bool stylesIsLoading;
private bool classesIsLoading;

private async Task LoadingStylesClick()
{
    stylesIsLoading = true;
    await Task.Delay(3000);
    stylesIsLoading = false;
}

private async Task LoadingClassesClick()
{
    classesIsLoading = true;
    await Task.Delay(3000);
    classesIsLoading = false;
}";

    private readonly string example5RazorCode = @"
<BitButton IconName=""@BitIconName.Emoji"">Start</BitButton>
<BitButton IconName=""@BitIconName.Emoji"" Variant=""BitVariant.Outline"">Start</BitButton>
<BitButton IconName=""@BitIconName.Emoji"" Variant=""BitVariant.Text"">Start</BitButton>

<BitButton IconName=""@BitIconName.Emoji2"" ReversedIcon>End</BitButton>
<BitButton IconName=""@BitIconName.Emoji2"" Variant=""BitVariant.Outline"" ReversedIcon>End</BitButton>
<BitButton IconName=""@BitIconName.Emoji2"" Variant=""BitVariant.Text"" ReversedIcon>End</BitButton>";

    private readonly string example6RazorCode = @"
<BitButton Size=""BitSize.Small"" Variant=""BitVariant.Fill"">Fill</BitButton>
<BitButton Size=""BitSize.Small"" Variant=""BitVariant.Outline"">Outline</BitButton>
<BitButton Size=""BitSize.Small"" Variant=""BitVariant.Text"">Text</BitButton>

<BitButton Size=""BitSize.Medium"" Variant=""BitVariant.Fill"">Fill</BitButton>
<BitButton Size=""BitSize.Medium"" Variant=""BitVariant.Outline"">Outline</BitButton>
<BitButton Size=""BitSize.Medium"" Variant=""BitVariant.Text"">Text</BitButton>

<BitButton Size=""BitSize.Large"" Variant=""BitVariant.Fill"">Fill</BitButton>
<BitButton Size=""BitSize.Large"" Variant=""BitVariant.Outline"">Outline</BitButton>
<BitButton Size=""BitSize.Large"" Variant=""BitVariant.Text"">Text</BitButton>";

    private readonly string example7RazorCode = @"
<style>
    .custom-content {
        gap: 0.5rem;
        display: flex;
        align-items: center;
    }
</style>


<BitButton Class=""custom-content"">
    <BitIcon IconName=""@BitIconName.Airplane"" />
    <span>A Fill custom content</span>
    <BitRippleLoading Size=""20"" />
</BitButton>

<BitButton Class=""custom-content"" Variant=""BitVariant.Outline"">
    <BitIcon IconName=""@BitIconName.Accept"" />
    <span>An Outline custom content</span>
    <BitRollerLoading Size=""20"" />
</BitButton>

<BitButton Class=""custom-content"" Variant=""BitVariant.Text"">
    <BitIcon IconName=""@BitIconName.Asterisk"" />
    <span>A Text custom content</span>
    <BitCircleLoading Size=""20"" />
</BitButton>";

    private readonly string example8RazorCode = @"
<BitButton OnClick=""() => clickCounter++"">Click me (@clickCounter)</BitButton>";
    private readonly string example8CsharpCode = @"
private int clickCounter;";

    private readonly string example9RazorCode = @"
<BitButton IsLoading=""fillIsLoading""
           Style=""min-width: 6rem;""
           Variant=""BitVariant.Fill""
           OnClick=""LoadingFillClick"">
    Click me
</BitButton>

<BitButton IsLoading=""outlineIsLoading""
           Style=""min-width: 6rem;""
           Variant=""BitVariant.Outline""
           OnClick=""LoadingOutlineClick"">
    Click me
</BitButton>

<BitButton IsLoading=""textIsLoading""
           Style=""min-width: 6rem;""
           Variant=""BitVariant.Text""
           OnClick=""LoadingTextClick"">
    Click me
</BitButton>


<BitButton IsLoading=""fillIsLoading""
           Style=""min-width: 6rem;""
           Variant=""BitVariant.Fill""
           Color=""BitColor.Secondary""
           OnClick=""LoadingFillClick"">
    Click me
</BitButton>

<BitButton IsLoading=""outlineIsLoading""
           Style=""min-width: 6rem;""
           Variant=""BitVariant.Outline""
           Color=""BitColor.Secondary""
           OnClick=""LoadingOutlineClick"">
    Click me
</BitButton>

<BitButton IsLoading=""textIsLoading""
           Style=""min-width: 6rem;""
           Variant=""BitVariant.Text""
           Color=""BitColor.Secondary""
           OnClick=""LoadingTextClick"">
    Click me
</BitButton>";
    private readonly string example9CsharpCode = @"
private bool fillIsLoading;
private bool outlineIsLoading;
private bool textIsLoading;

private async Task LoadingFillClick()
{
    fillIsLoading = true;
    await Task.Delay(3000);
    fillIsLoading = false;
}

private async Task LoadingOutlineClick()
{
    outlineIsLoading = true;
    await Task.Delay(3000);
    outlineIsLoading = false;
}

private async Task LoadingTextClick()
{
    textIsLoading = true;
    await Task.Delay(3000);
    textIsLoading = false;
}";

    private readonly string example10RazorCode = @"
<BitButton IsLoading=""true""
           LoadingLabel=""End...""
           Style=""min-width: 6.5rem;""
           Variant=""BitVariant.Outline""
           LoadingLabelPosition=""BitLabelPosition.End"">
    End
</BitButton>

<BitButton IsLoading=""true""
           LoadingLabel=""Start...""
           Style=""min-width: 6.5rem;""
           Variant=""BitVariant.Outline""
           LoadingLabelPosition=""BitLabelPosition.Start"">
    Start
</BitButton>

<BitButton IsLoading=""true""
           LoadingLabel=""Bottom...""
           Style=""min-width: 6.5rem;""
           Variant=""BitVariant.Outline""
           LoadingLabelPosition=""BitLabelPosition.Bottom"">
    Bottom
</BitButton>

<BitButton IsLoading=""true""
           LoadingLabel=""Top...""
           Style=""min-width: 6.5rem;""
           Variant=""BitVariant.Outline""
           LoadingLabelPosition=""BitLabelPosition.Top"">
    Top
</BitButton>";

    private readonly string example11RazorCode = @"
<style>
    .custom-loading {
        display: flex;
        gap: 0.3125rem;
        align-items: center;
        justify-content: center;
    }
</style>


<BitButton IsLoading=""true""
           Size=""BitSize.Large""
           Title=""Ellipsis loading..."">
    <LoadingTemplate>
        <div class=""custom-loading"">
            <BitEllipsisLoading Size=""20"" />
            <span>Wait...</span>
        </div>
    </LoadingTemplate>
    <Content>
        Ellipsis...
    </Content>
</BitButton>";

    private readonly string example12RazorCode = @"
<EditForm Model=""buttonValidationModel"" OnValidSubmit=""HandleValidSubmit"">
    <DataAnnotationsValidator />
    <BitTextField Label=""Required"" Required @bind-Value=""buttonValidationModel.RequiredText"" />
    <ValidationMessage For=""() => buttonValidationModel.RequiredText"" />
    <BitTextField Label=""Nonrequired"" @bind-Value=""buttonValidationModel.NonRequiredText"" />
    <ValidationMessage For=""() => buttonValidationModel.NonRequiredText"" />
    <div>
        <BitButton ButtonType=""BitButtonType.Submit"">Submit</BitButton>
        <BitButton ButtonType=""BitButtonType.Reset"">Reset</BitButton>
        <BitButton ButtonType=""BitButtonType.Button"">Button</BitButton>
    </div>
</EditForm>";
    private readonly string example12CsharpCode = @"
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

    private readonly string example13RazorCode = @"
<BitButton Dir=""BitDir.Rtl""
           IconName=""@BitIconName.Emoji""
           Variant=""BitVariant.Fill"">
    دکمه با آیکون
</BitButton>

<BitButton Dir=""BitDir.Rtl""
           IconName=""@BitIconName.Emoji""
           Variant=""BitVariant.Outline"">
    دکمه با آیکون
</BitButton>

<BitButton Dir=""BitDir.Rtl""
           IconName=""@BitIconName.Emoji""
           Variant=""BitVariant.Text"">
    دکمه با آیکون
</BitButton>


<BitButton IsLoading
           Dir=""BitDir.Rtl""
           LoadingLabel=""در حال بارگذاری""
           Variant=""BitVariant.Fill"" />
<BitButton IsLoading
           Dir=""BitDir.Rtl""
           LoadingLabel=""در حال بارگذاری""
           Variant=""BitVariant.Outline"" />
<BitButton IsLoading
           Dir=""BitDir.Rtl""
           LoadingLabel=""در حال بارگذاری""
           Variant=""BitVariant.Text"" />";
}
