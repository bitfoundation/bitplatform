namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Buttons.ActionButton;

public partial class BitActionButtonDemo
{
    private readonly string example1RazorCode = @"
<BitActionButton IconName=""@BitIconName.AddFriend"">
    Create account
</BitActionButton>

<BitActionButton IconPosition=""BitIconPosition.End"" IconName=""@BitIconName.AddFriend"">
    End Icon
</BitActionButton>

<BitActionButton IconName=""@BitIconName.AddFriend"" IsEnabled=""false"">
    Disabled
</BitActionButton>

<BitActionButton IconName=""@BitIconName.AlarmClock"" AriaLabel=""Call"">
    AriaLabel
</BitActionButton>

<BitActionButton>
    No Icon
</BitActionButton>

<BitActionButton IconOnly IconName=""@BitIconName.Phone"">
</BitActionButton>";

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
<BitActionButton Rel=""BitLinkRels.NoFollow"" Href=""https://bitplatform.dev"" Target=""_blank"" IconName=""@BitIconName.Globe"">
    Open bitplatform.dev with a rel attribute (nofollow)
</BitActionButton>

<BitActionButton Rel=""BitLinkRels.NoFollow | BitLinkRels.NoReferrer"" Href=""https://bitplatform.dev"" Target=""_blank"" IconName=""@BitIconName.Globe"">
    Open bitplatform.dev with a rel attribute (nofollow & noreferrer)
</BitActionButton>";

    private readonly string example5RazorCode = @"
<BitActionButton IconName=""@BitIconName.AddFriend"">
        <div style=""display:flex;gap:0.5rem;"">
        <b>This is a custom template</b>
            <BitSpinnerLoading CustomSize=""20"" />
        </div>
</BitActionButton>";

    private readonly string example6RazorCode = @"
<EditForm Model=""validationButtonModel"" OnValidSubmit=""HandleValidSubmit"">
    <DataAnnotationsValidator />

    <ValidationSummary />

    <BitTextField Label=""Required"" Required @bind-Value=""validationButtonModel.RequiredText"" />
    <ValidationMessage For=""() => validationButtonModel.RequiredText"" style=""color:red"" />

    <BitTextField Label=""Nonrequired"" @bind-Value=""validationButtonModel.NonRequiredText"" />

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
</EditForm>";
    private readonly string example6CsharpCode = @"
public class ButtonValidationModel
{
    [Required]
    public string RequiredText { get; set; } = string.Empty;

    public string? NonRequiredText { get; set; }
}

private ButtonValidationModel validationButtonModel = new();

private async Task HandleValidSubmit()
{
    await Task.Delay(2000);

    validationButtonModel = new();

    StateHasChanged();
}";

    private readonly string example7RazorCode = @"
<BitActionButton FullWidth IconName=""@BitIconName.NavigationFlipper"">
    FullWidth
</BitActionButton>

<BitActionButton FullWidth IconPosition=""BitIconPosition.End"" IconName=""@BitIconName.Forward"">
    FullWidth with end icon
</BitActionButton>";

    private readonly string example8RazorCode = @"
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
</BitActionButton>";
    private readonly string example9CsharpCode = @"
private bool isLoading;";

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
<BitActionButton Size=""BitSize.Small"" IconName=""@BitIconName.FontSize"">
    Small
</BitActionButton>

<BitActionButton Size=""BitSize.Medium"" IconName=""@BitIconName.FontSize"">
    Medium
</BitActionButton>

<BitActionButton Size=""BitSize.Large"" IconName=""@BitIconName.FontSize"">
    Large
</BitActionButton>";

    private readonly string example13RazorCode = @"
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

    private readonly string example14RazorCode = @"
<BitActionButton Dir=""BitDir.Rtl"" IconName=""@BitIconName.AddFriend"">
    ساخت حساب
</BitActionButton>";

}
