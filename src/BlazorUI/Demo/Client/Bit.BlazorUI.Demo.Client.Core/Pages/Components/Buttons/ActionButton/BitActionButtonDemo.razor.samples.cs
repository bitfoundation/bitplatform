namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Buttons.ActionButton;

public partial class BitActionButtonDemo
{
    private readonly string example1RazorCode = @"
<BitActionButton IconName=""@BitIconName.AddFriend"">
    Create account
</BitActionButton>

<BitActionButton IconName=""@BitIconName.AddFriend"" IsEnabled=""false"">
    Disabled
</BitActionButton>

<BitActionButton IconName=""@BitIconName.AlarmClock"" AriaLabel=""Call"">
    AriaLabel=""Call""
</BitActionButton>

<BitActionButton>
    No Icon
</BitActionButton>

<BitActionButton IconOnly IconName=""@BitIconName.Phone"" AriaLabel=""Call"" />";

    private readonly string example2RazorCode = @"
<BitActionButton IconPosition=""BitIconPosition.Start"" IconName=""@BitIconName.AddFriend"">
    Start (default)
</BitActionButton>

<BitActionButton IconPosition=""BitIconPosition.End"" IconName=""@BitIconName.AddFriend"">
    End
</BitActionButton>";

    private readonly string example3RazorCode = @"
<BitActionButton IconName=""@BitIconName.Globe"" Href=""https://bitplatform.dev"" Target=""_blank"">
    Open bitplatform.dev in a new tab
</BitActionButton>

<BitActionButton IconName=""@BitIconName.Globe"" Href=""https://github.com/bitfoundation/bitplatform"">
    Go to bitplatform GitHub
</BitActionButton>";

    private readonly string example4RazorCode = @"
<BitActionButton IconName=""@BitIconName.Download"" Href=""/images/bit-logo.svg"" Download="""">
    Download the logo
</BitActionButton>

<BitActionButton IconName=""@BitIconName.Download"" Href=""/images/bit-logo.svg"" Download=""bit-platform-logo.svg"">
    Download with a custom file name
</BitActionButton>";

    private readonly string example5RazorCode = @"
<BitActionButton Rel=""BitLinkRels.NoFollow"" Href=""https://bitplatform.dev"" Target=""_blank"" IconName=""@BitIconName.Globe"">
    Open bitplatform.dev with a rel attribute (nofollow)
</BitActionButton>

<BitActionButton Rel=""BitLinkRels.NoFollow | BitLinkRels.NoReferrer"" Href=""https://bitplatform.dev"" Target=""_blank"" IconName=""@BitIconName.Globe"">
    Open bitplatform.dev with a rel attribute (nofollow & noreferrer)
</BitActionButton>";

    private readonly string example6RazorCode = @"
<BitActionButton IconName=""@BitIconName.AddFriend"">
        <div style=""display:flex;gap:0.5rem;"">
        <b>This is a custom template</b>
            <BitSpinnerLoading CustomSize=""20"" />
        </div>
</BitActionButton>";

    private readonly string example7RazorCode = @"
@if (formIsValidSubmit is false)
{
    <EditForm Model=""buttonValidationModel"" OnValidSubmit=""HandleValidSubmit"" OnInvalidSubmit=""HandleInvalidSubmit"" novalidate>
        <DataAnnotationsValidator />

        <BitTextField Label=""Required"" Required @bind-Value=""buttonValidationModel.RequiredText"" />
        <ValidationMessage For=""() => buttonValidationModel.RequiredText"" style=""color:red"" />

        <BitTextField Label=""Non-required"" @bind-Value=""buttonValidationModel.NonRequiredText"" />
                    
        <div>
            <BitActionButton IconName=""@BitIconName.SendMirrored"" ButtonType=""BitButtonType.Submit"">
                Submit
            </BitActionButton>

            <BitActionButton IconName=""@BitIconName.Reset"" ButtonType=""BitButtonType.Reset"">
                Reset
            </BitActionButton>

            <BitActionButton IconName=""@BitIconName.ButtonControl"" ButtonType=""BitButtonType.Button"">
                Button
            </BitActionButton>
        </div>
    </EditForm>
}
else
{
    <BitMessage Color=""BitColor.Success"">
        The form submitted successfully.
    </BitMessage>
}";
    private readonly string example7CsharpCode = @"
public class ButtonValidationModel
{
    [Required]
    public string RequiredText { get; set; } = string.Empty;

    public string? NonRequiredText { get; set; }
}

private bool formIsValidSubmit;
private ButtonValidationModel buttonValidationModel = new();

private async Task HandleValidSubmit()
{
    formIsValidSubmit = true;

    await Task.Delay(2000);

    buttonValidationModel = new();

    formIsValidSubmit = false;

    StateHasChanged();
}

private void HandleInvalidSubmit()
{
    formIsValidSubmit = false;
}";

    private readonly string example8RazorCode = @"
<BitActionButton FullWidth IconName=""@BitIconName.NavigationFlipper"">
    FullWidth
</BitActionButton>

<BitActionButton FullWidth IconPosition=""BitIconPosition.End"" IconName=""@BitIconName.Forward"">
    FullWidth with end icon
</BitActionButton>";

    private readonly string example9RazorCode = @"
<BitToggle @bind-Value=""isLoading"" Label=""Toggle loading"" />

<BitActionButton IsLoading=""isLoading"" IconName=""@BitIconName.Save"">
    Save changes
</BitActionButton>

<BitActionButton IsLoading=""isLoading"" IconName=""@BitIconName.CloudUpload"">
    Upload file
</BitActionButton>

<BitActionButton IsLoading=""isLoading"" IconName=""@BitIconName.Send"" Color=""BitColor.Success"">
    Send message
</BitActionButton>

<BitActionButton AutoLoading OnClick=""HandleAutoLoadingClick"" IconName=""@BitIconName.Save"">
    AutoLoading
</BitActionButton>

<BitActionButton AutoLoading OnClick=""HandleAutoLoadingClick"" LoadingLabel=""Saving..."" IconName=""@BitIconName.Save"">
    AutoLoading with LoadingLabel
</BitActionButton>

<BitActionButton AutoLoading OnClick=""HandleAutoLoadingClick"" LoadingDelay=""500"" IconName=""@BitIconName.Save"">
    AutoLoading with LoadingDelay
</BitActionButton>

<BitActionButton AutoLoading OnClick=""HandleGuardedClick"" IconName=""@BitIconName.Shield"">
    Guarded (@guardedClickCount)
</BitActionButton>

<BitActionButton AutoLoading Reclickable OnClick=""HandleReclickableClick"" IconName=""@BitIconName.RepeatAll"">
    Reclickable (@reclickableClickCount)
</BitActionButton>";
    private readonly string example9CsharpCode = @"
private bool isLoading;
private int guardedClickCount;
private int reclickableClickCount;

private async Task HandleAutoLoadingClick()
{
    await Task.Delay(2000);
}

private async Task HandleGuardedClick()
{
    guardedClickCount++;

    await Task.Delay(2000);
}

private async Task HandleReclickableClick()
{
    reclickableClickCount++;

    await Task.Delay(2000);
}";

    private readonly string example10RazorCode = @"
<BitToggle @bind-Value=""templateIsLoading"" Label=""Toggle loading"" />

<BitActionButton IsLoading=""templateIsLoading"" IconName=""@BitIconName.Download"">
    <Body>
        Download
    </Body>
    <LoadingTemplate>
        <BitRingLoading CustomSize=""20"" Color=""BitColor.Tertiary"" /> Downloading...
    </LoadingTemplate>
</BitActionButton>";
    private readonly string example10CsharpCode = @"
private bool templateIsLoading;";

    private readonly string example11RazorCode = @"
<BitActionButton Underlined IconName=""@BitIconName.Link"">
    Link style
</BitActionButton>

<BitActionButton Underlined IconName=""@BitIconName.OpenInNewTab"" Href=""https://github.com/bitfoundation/bitplatform"" Target=""_blank"">
    Open GitHub
</BitActionButton>

<BitActionButton Underlined Color=""BitColor.Info"" IconName=""@BitIconName.Info"">
    More info
</BitActionButton>";

    private readonly string example12RazorCode = @"
<BitActionButton Color=""BitColor.Primary"" IconName=""@BitIconName.ColorSolid"">
    Primary
</BitActionButton>
<BitActionButton Color=""BitColor.Primary"">
    Primary
</BitActionButton>

<BitActionButton Color=""BitColor.Secondary"" IconName=""@BitIconName.ColorSolid"">
    Secondary
</BitActionButton>
<BitActionButton Color=""BitColor.Secondary"">
    Secondary
</BitActionButton>

<BitActionButton Color=""BitColor.Tertiary"" IconName=""@BitIconName.ColorSolid"">
    Tertiary
</BitActionButton>
<BitActionButton Color=""BitColor.Tertiary"">
    Tertiary
</BitActionButton>

<BitActionButton Color=""BitColor.Info"" IconName=""@BitIconName.ColorSolid"">
    Info
</BitActionButton>
<BitActionButton Color=""BitColor.Info"">
    Info
</BitActionButton>

<BitActionButton Color=""BitColor.Success"" IconName=""@BitIconName.ColorSolid"">
    Success
</BitActionButton>
<BitActionButton Color=""BitColor.Success"">
    Success
</BitActionButton>

<BitActionButton Color=""BitColor.Warning"" IconName=""@BitIconName.ColorSolid"">
    Warning
</BitActionButton>
<BitActionButton Color=""BitColor.Warning"">
    Warning
</BitActionButton>

<BitActionButton Color=""BitColor.SevereWarning"" IconName=""@BitIconName.ColorSolid"">
    SevereWarning
</BitActionButton>
<BitActionButton Color=""BitColor.SevereWarning"">
    SevereWarning
</BitActionButton>

<BitActionButton Color=""BitColor.Error"" IconName=""@BitIconName.ColorSolid"">
    Error
</BitActionButton>
<BitActionButton Color=""BitColor.Error"">
    Error
</BitActionButton>

<BitActionButton Color=""BitColor.PrimaryBackground"" IconName=""@BitIconName.ColorSolid"">
    PrimaryBackground
</BitActionButton>
<BitActionButton Color=""BitColor.PrimaryBackground"">
    PrimaryBackground
</BitActionButton>

<BitActionButton Color=""BitColor.SecondaryBackground"" IconName=""@BitIconName.ColorSolid"">
    SecondaryBackground
</BitActionButton>
<BitActionButton Color=""BitColor.SecondaryBackground"">
    SecondaryBackground
</BitActionButton>

<BitActionButton Color=""BitColor.TertiaryBackground"" IconName=""@BitIconName.ColorSolid"">
    TertiaryBackground
</BitActionButton>
<BitActionButton Color=""BitColor.TertiaryBackground"">
    TertiaryBackground
</BitActionButton>

<BitActionButton Color=""BitColor.PrimaryForeground"" IconName=""@BitIconName.ColorSolid"">
    PrimaryForeground
</BitActionButton>
<BitActionButton Color=""BitColor.PrimaryForeground"">
    PrimaryForeground
</BitActionButton>

<BitActionButton Color=""BitColor.SecondaryForeground"" IconName=""@BitIconName.ColorSolid"">
    SecondaryForeground
</BitActionButton>
<BitActionButton Color=""BitColor.SecondaryForeground"">
    SecondaryForeground
</BitActionButton>

<BitActionButton Color=""BitColor.TertiaryForeground"" IconName=""@BitIconName.ColorSolid"">
    TertiaryForeground
</BitActionButton>
<BitActionButton Color=""BitColor.TertiaryForeground"">
    TertiaryForeground
</BitActionButton>

<BitActionButton Color=""BitColor.PrimaryBorder"" IconName=""@BitIconName.ColorSolid"">
    PrimaryBorder
</BitActionButton>
<BitActionButton Color=""BitColor.PrimaryBorder"">
    PrimaryBorder
</BitActionButton>

<BitActionButton Color=""BitColor.SecondaryBorder"" IconName=""@BitIconName.ColorSolid"">
    SecondaryBorder
</BitActionButton>
<BitActionButton Color=""BitColor.SecondaryBorder"">
    SecondaryBorder
</BitActionButton>

<BitActionButton Color=""BitColor.TertiaryBorder"" IconName=""@BitIconName.ColorSolid"">
    TertiaryBorder
</BitActionButton>
<BitActionButton Color=""BitColor.TertiaryBorder"">
    TertiaryBorder
</BitActionButton>";

    private readonly string example13RazorCode = @"
<link rel=""stylesheet"" href=""https://cdnjs.cloudflare.com/ajax/libs/font-awesome/7.0.1/css/all.min.css"" />

<BitActionButton Icon=""@(""fa-solid fa-house"")"">
    House (Icon=@@(""fa-solid fa-house""))
</BitActionButton>
        
<BitActionButton Icon=""@BitIconInfo.Css(""fa-solid fa-heart"")"" Color=""BitColor.Secondary"">
    Heart (Icon=""@@BitIconInfo.Css(""fa-solid fa-heart"")"")
</BitActionButton>
        
<BitActionButton Icon=""@BitIconInfo.Fa(""fa-brands fa-github"")"" Color=""BitColor.Tertiary"">
    GitHub (Icon=""@@BitIconInfo.Fa(""fa-brands fa-github"")"")
</BitActionButton>
        
<BitActionButton Icon=""@BitIconInfo.Fa(""solid rocket"")"" Color=""BitColor.Error"">
    Rocket (Icon=""@@BitIconInfo.Fa(""solid rocket"")"")
</BitActionButton>


<link rel=""stylesheet"" href=""https://cdn.jsdelivr.net/npm/bootstrap-icons@1.11.3/font/bootstrap-icons.min.css"" />

<BitActionButton Icon=""@(""bi bi-house-fill"")"">
    House (Icon=@@(""bi bi-house-fill""))
</BitActionButton>
        
<BitActionButton Icon=""@BitIconInfo.Css(""bi bi-heart-fill"")"" Color=""BitColor.Secondary"">
    Heart (Icon=""@@BitIconInfo.Css(""bi bi-heart-fill"")"")
</BitActionButton>
        
<BitActionButton Icon=""@BitIconInfo.Bi(""github"")"" Color=""BitColor.Tertiary"">
    GitHub (Icon=""@@BitIconInfo.Bi(""github"")"")
</BitActionButton>
        
<BitActionButton Icon=""@BitIconInfo.Bi(""gear-fill"")"" Color=""BitColor.Error"">
    Gear (Icon=""@@BitIconInfo.Bi(""gear-fill"")"")
</BitActionButton>";

    private readonly string example14RazorCode = @"
<BitActionButton Size=""BitSize.Small"" IconName=""@BitIconName.FontSize"">
    Small
</BitActionButton>

<BitActionButton Size=""BitSize.Medium"" IconName=""@BitIconName.FontSize"">
    Medium
</BitActionButton>

<BitActionButton Size=""BitSize.Large"" IconName=""@BitIconName.FontSize"">
    Large
</BitActionButton>";

    private readonly string example15RazorCode = @"
<BitActionButton OnClick=""() => clickCounter++"" IconName=""@BitIconName.TouchPointer"">
    Click me (@clickCounter)
</BitActionButton>


<div class=""clickable-row"" @onclick=""() => rowClickCount++"">
    <span>Row clicks: @rowClickCount | Button clicks: @innerClickCount</span>

    <BitActionButton OnClick=""() => innerClickCount++"" IconName=""@BitIconName.Edit"">
        Bubbles up
    </BitActionButton>

    <BitActionButton StopPropagation OnClick=""() => innerClickCount++"" IconName=""@BitIconName.Edit"">
        StopPropagation
    </BitActionButton>
</div>";
    private readonly string example15CsharpCode = @"
private int clickCounter;
private int rowClickCount;
private int innerClickCount;";

    private readonly string example16RazorCode = @"
<BitActionButton Title=""Save your changes"" IconName=""@BitIconName.Save"">
    Hover me
</BitActionButton>

<BitActionButton IconOnly Title=""Delete"" AriaLabel=""Delete"" Color=""BitColor.Error"" IconName=""@BitIconName.Delete"" />";

    private readonly string example17RazorCode = @"
<BitActionButton IsEnabled=""false"" IconName=""@BitIconName.Blocked"">
    Disabled (skipped by Tab)
</BitActionButton>

<BitActionButton IsEnabled=""false"" AllowDisabledFocus IconName=""@BitIconName.Blocked"">
    Disabled (still focusable)
</BitActionButton>";

    private readonly string example18RazorCode = @"
<style>
    .custom-icon {
        color: hotpink;
    }

    .custom-content {
        position: relative;
    }

    .custom-content::after {
        content: '';
        left: 0;
        width: 0;
        height: 2px;
        bottom: -6px;
        position: absolute;
        transition: 0.3s ease;
        background: linear-gradient(90deg, #ff00cc, #3333ff);
    }

    .custom-root:hover .custom-content {
        color: blueviolet;
    }

    .custom-root:hover .custom-content::after {
        width: 100%;
    }
</style>


<BitActionButton IconName=""@BitIconName.Brush""
                 Styles=""@(new() { Root = ""font-size: 1.5rem;"",
                                   Icon = ""color: blueviolet;"",
                                   Content = ""text-shadow: aqua 0 0 1rem;"" })"">
    Action Button Styles
</BitActionButton>

<BitActionButton IconName=""@BitIconName.FormatPainter""
                 Classes=""@(new() { Root = ""custom-root"",
                                    Icon = ""custom-icon"",
                                    Content = ""custom-content"" })"">
    Action Button Classes (Hover me)
</BitActionButton>";

    private readonly string example19RazorCode = @"
<BitActionButton Dir=""BitDir.Rtl"" IconName=""@BitIconName.AddFriend"">
    ساخت حساب
</BitActionButton>";

}
