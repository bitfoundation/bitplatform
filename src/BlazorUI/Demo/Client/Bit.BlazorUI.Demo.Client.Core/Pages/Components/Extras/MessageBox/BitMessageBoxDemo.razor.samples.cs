namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Extras.MessageBox;

public partial class BitMessageBoxDemo
{
    private readonly string example1RazorCode = @"
<BitCard Style=""padding:0"">
    <BitMessageBox Title=""It's a title"" Body=""It's a body."" />
</BitCard>";

    private readonly string example2RazorCode = @"
<BitMessageBox Title=""Ok""
               Body=""A single button, which is the default.""
               OnResult=""v => buttonsResult = v"" />

<BitMessageBox Title=""OkCancel""
               Body=""An Ok and a Cancel button.""
               Buttons=""BitMessageBoxButtons.OkCancel""
               OnResult=""v => buttonsResult = v"" />

<BitMessageBox Title=""YesNo""
               Body=""A Yes and a No button.""
               Buttons=""BitMessageBoxButtons.YesNo""
               OnResult=""v => buttonsResult = v"" />

<BitMessageBox Title=""YesNoCancel""
               Body=""A Yes, a No and a Cancel button.""
               Buttons=""BitMessageBoxButtons.YesNoCancel""
               OnResult=""v => buttonsResult = v"" />

<div>Last answer: <b>@buttonsResult</b></div>";
    private readonly string example2CsharpCode = @"
private BitMessageBoxResult buttonsResult;";

    private readonly string example3RazorCode = @"
<BitMessageBox Reversed
               Title=""Reversed""
               Body=""Cancel comes first here.""
               Buttons=""BitMessageBoxButtons.OkCancel"" />

<BitMessageBox Title=""Default button""
               Body=""No is the button the focus would land on.""
               Buttons=""BitMessageBoxButtons.YesNo""
               DefaultButton=""BitMessageBoxResult.No"" />

<BitMessageBox Title=""Delete the file?""
               Body=""The destructive action is the one that stands out.""
               Buttons=""BitMessageBoxButtons.YesNo""
               YesText=""Delete""
               NoText=""Keep""
               PrimaryButtonColor=""BitColor.Error""
               DefaultButton=""BitMessageBoxResult.No"" />";

    private readonly string example4RazorCode = @"
<BitMessageBox Title=""IconName""
               Body=""A glyph from the built-in Fluent set.""
               IconName=""@BitIconName.Lightbulb"" />

<BitMessageBox Title=""IconTemplate""
               Body=""Any markup can stand in for the glyph."">
    <IconTemplate>
        <span class=""example-emoji"">&#127881;</span>
    </IconTemplate>
</BitMessageBox>";

    private readonly string example5RazorCode = @"
<BitMessageBox Title=""No close button""
               Body=""This one has to be answered.""
               ShowCloseButton=""false""
               Buttons=""BitMessageBoxButtons.YesNo"" />

<BitMessageBox Title=""Custom close button""
               Body=""Another glyph, and another name for it.""
               CloseIconName=""@BitIconName.Cancel""
               CloseButtonTitle=""Dismiss this message"" />";

    private readonly string example6RazorCode = @"
<BitMessageBox Title=""Delete the workspace?"">
    <BodyTemplate>
        <div>
            Everything in <b>Design system</b> is removed, including:
            <ul>
                <li>18 projects</li>
                <li>4 shared libraries</li>
            </ul>
            <BitLink Href=""/components/messagebox"">Read what this means</BitLink>
        </div>
    </BodyTemplate>
    <FooterTemplate>
        <BitButton Color=""BitColor.Error"" IconName=""@BitIconName.Delete"">Delete forever</BitButton>
        <BitButton Variant=""BitVariant.Outline"" Color=""BitColor.Tertiary"">Keep it</BitButton>
    </FooterTemplate>
</BitMessageBox>";

    private readonly string example7RazorCode = @"
<BitButton OnClick=""() => isModalOpen = true"">Show</BitButton>

<BitModal @bind-IsOpen=""isModalOpen"">
    <BitMessageBox AutoFocus
                   OnClose=""() => isModalOpen = false""
                   Title=""This is the Title""
                   Body=""This is the Body!"" />
</BitModal>";
    private readonly string example7CsharpCode = @"
private bool isModalOpen;";

    private readonly string example8RazorCode = @"
<BitButton OnClick=""ShowMessageBox"">Show MessageBox</BitButton>

<div>Last answer: <b>@modalServiceResult</b></div>

<BitModalContainer />";
    private readonly string example8CsharpCode = @"
private BitMessageBoxResult modalServiceResult;

[AutoInject] private BitModalService modalService { get; set; } = default!;
private async Task ShowMessageBox()
{
    var modalRef = await modalService.Show<BitMessageBox>(modalRef => new()
    {
        { nameof(BitMessageBox.Title), ""This is a title"" },
        { nameof(BitMessageBox.Body), ""This is a body."" },
        { nameof(BitMessageBox.AutoFocus), true },
        { nameof(BitMessageBox.Buttons), BitMessageBoxButtons.OkCancel },
        { nameof(BitMessageBox.OnResult), EventCallback.Factory.Create<BitMessageBoxResult>(this, r => modalRef.CloseWith(r)) }
    });

    modalServiceResult = (await modalRef.Result) as BitMessageBoxResult? ?? BitMessageBoxResult.None;
}";

    private readonly string example9RazorCode = @"
<BitButton OnClick=""ShowMessageBoxService"">Show</BitButton>
<BitButton Color=""BitColor.Info"" OnClick=""ShowInfoMessageBox"">Info</BitButton>
<BitButton Color=""BitColor.Success"" OnClick=""ShowSuccessMessageBox"">Success</BitButton>
<BitButton Color=""BitColor.Warning"" OnClick=""ShowWarningMessageBox"">Warning</BitButton>
<BitButton Color=""BitColor.Error"" OnClick=""ShowErrorMessageBox"">Error</BitButton>

@* The service shows the message box through the BitModalService, so mount its container. *@
<BitModalContainer />";
    private readonly string example9CsharpCode = @"
[AutoInject] private BitMessageBoxService messageBoxService { get; set; } = default!;

private async Task ShowMessageBoxService()
{
    await messageBoxService.Show(""TITLE"", ""BODY"");
}

private async Task ShowInfoMessageBox()
{
    await messageBoxService.ShowInfo(""Information"", ""The export finished in 4 seconds."");
}

private async Task ShowSuccessMessageBox()
{
    await messageBoxService.ShowSuccess(""Success"", ""Your changes are saved."");
}

private async Task ShowWarningMessageBox()
{
    await messageBoxService.ShowWarning(""Warning"", ""This workspace is almost out of space."");
}

private async Task ShowErrorMessageBox()
{
    await messageBoxService.ShowError(""Error"", ""The file could not be uploaded."");
}";

    private readonly string example10RazorCode = @"
<BitButton OnClick=""ShowConfirm"">Confirm</BitButton>
<BitButton Color=""BitColor.Error"" OnClick=""ShowDangerousConfirm"">Delete something</BitButton>

<div>Last answer: <b>@confirmResult</b></div>

<BitModalContainer />";
    private readonly string example10CsharpCode = @"
private bool? confirmResult;

[AutoInject] private BitMessageBoxService messageBoxService { get; set; } = default!;

private async Task ShowConfirm()
{
    confirmResult = await messageBoxService.Confirm(""Publish"", ""Publish these changes to production?"");
}

private async Task ShowDangerousConfirm()
{
    confirmResult = await messageBoxService.Confirm(new()
    {
        Title = ""Delete the workspace?"",
        Body = ""Everything in it is removed. This cannot be undone."",
        Color = BitColor.Error,
        Buttons = BitMessageBoxButtons.YesNo,
        PrimaryButtonColor = BitColor.Error,
        YesText = ""Delete forever"",
        NoText = ""Keep it"",
        DefaultButton = BitMessageBoxResult.No,
        IconAriaLabel = ""Error""
    });
}";

    private readonly string example11RazorCode = @"
<BitMessageBox Color=""BitColor.Info"" Title=""Info"" Body=""Something worth knowing."" />
<BitMessageBox Color=""BitColor.Success"" Title=""Success"" Body=""Something went well."" />
<BitMessageBox Color=""BitColor.Warning"" Title=""Warning"" Body=""Something needs attention."" />
<BitMessageBox Color=""BitColor.SevereWarning"" Title=""SevereWarning"" Body=""Something needs attention now."" />
<BitMessageBox Color=""BitColor.Error"" Title=""Error"" Body=""Something went wrong."" />
<BitMessageBox Color=""BitColor.Primary"" Title=""Primary"" Body=""The accent of the theme."" />";

    private readonly string example12RazorCode = @"
<link rel=""stylesheet"" href=""https://cdnjs.cloudflare.com/ajax/libs/font-awesome/7.0.1/css/all.min.css"" />
<link rel=""stylesheet"" href=""https://cdnjs.cloudflare.com/ajax/libs/bootstrap-icons/1.11.3/font/bootstrap-icons.min.css"" />

<BitMessageBox Color=""BitColor.Warning""
               Icon=""@BitIconInfo.Fa(""solid triangle-exclamation"")""
               Title=""FontAwesome""
               Body=""The glyph comes from FontAwesome through BitIconInfo.Fa."" />

<BitMessageBox Color=""BitColor.Info""
               Icon=""@BitIconInfo.Bi(""info-circle"")""
               CloseIcon=""@BitIconInfo.Bi(""x-lg"")""
               Title=""Bootstrap Icons""
               Body=""The glyph and the close icon come from Bootstrap Icons."" />";

    private readonly string example13RazorCode = @"
<BitMessageBox Size=""BitSize.Small"" Color=""BitColor.Info"" Title=""Small"" Body=""The small size."" />
<BitMessageBox Size=""BitSize.Medium"" Color=""BitColor.Info"" Title=""Medium"" Body=""The medium size."" />
<BitMessageBox Size=""BitSize.Large"" Color=""BitColor.Info"" Title=""Large"" Body=""The large size."" />";

    private readonly string example14RazorCode = @"
<style>
    .custom-msg {
        background: linear-gradient(180deg, #3e0f0f, transparent) #000;
    }

    .custom-msg-btn {
        color: #fff;
        font-weight: bold;
        border-radius: 1rem;
        border-color: #8f0101;
        transition: background-color 1s;
        background: linear-gradient(90deg, #d10000, transparent) #8f0101;
    }

    .custom-msg-btn:hover {
        color: #fff;
        font-weight: bold;
        border-color: #8f0101;
        background-color: #8f0101;
    }
</style>

<BitCard Style=""padding:0"">
    <BitMessageBox Title=""It's a title""
                   Body=""It's a body.""
                   Styles=""@(new() { Root = ""background: linear-gradient(180deg, #222444, transparent) #000"", OkButton = new() { Root = ""border-radius:1rem"" } })"" />
</BitCard>

<BitCard Style=""padding:0"">
    <BitMessageBox Title=""It's a title""
                   Body=""It's a body.""
                   Buttons=""BitMessageBoxButtons.OkCancel""
                   Classes=""@(new() { Root = ""custom-msg"", ActionButton = new() { Root = ""custom-msg-btn"" } })"" />
</BitCard>";

    private readonly string example15RazorCode = @"
<BitCard Style=""padding:0"">
    <BitMessageBox Dir=""BitDir.Rtl""
                   Color=""BitColor.Warning""
                   Title=""عنوان پیام""
                   Body=""متن تست پیام...""
                   Buttons=""BitMessageBoxButtons.YesNo""
                   YesText=""بله""
                   NoText=""خیر""
                   CloseButtonTitle=""بستن"" />
</BitCard>";
}
