namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Extras.MessageBox;

public partial class BitMessageBoxDemo
{
    private readonly string example1RazorCode = @"
<BitMessageBox Title=""It's a title"" Body=""It's a body."" />";

    private readonly string example2RazorCode = @"
<BitMessageBox Title=""Ok""
               Body=""A single button, which is the default.""
               OkText=""Got it""
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

<BitMessageBox Title=""None""
               Body=""No buttons at all - only the close button ends this one.""
               Buttons=""BitMessageBoxButtons.None""
               OnResult=""v => buttonsResult = v"" />

<div>Last answer: <b>@buttonsResult</b></div>";
    private readonly string example2CsharpCode = @"
private BitMessageBoxResult buttonsResult;";

    private readonly string example3RazorCode = @"
<BitMessageBox Reversed
               Title=""Reversed""
               Body=""Cancel comes first here.""
               Buttons=""BitMessageBoxButtons.OkCancel"" />

<BitMessageBox Title=""Delete the file?""
               Body=""Keep is where the focus would land.""
               Buttons=""BitMessageBoxButtons.YesNo""
               YesText=""Delete""
               NoText=""Keep""
               PrimaryButtonColor=""BitColor.Error""
               DefaultButton=""BitMessageBoxResult.No"" />

<BitMessageBox Title=""ButtonColor""
               Body=""Every button in the primary color.""
               Buttons=""BitMessageBoxButtons.YesNoCancel""
               ButtonColor=""BitColor.Primary"" />";

    private readonly string example4RazorCode = @"
<BitMessageBox Title=""IconName""
               Body=""A glyph from the built-in Fluent set.""
               IconName=""@BitIconName.Lightbulb"" />

<BitMessageBox Title=""IconTemplate""
               Body=""Any markup can stand in for the glyph."">
    <IconTemplate>
        <span class=""example-emoji"">&#127881;</span>
    </IconTemplate>
</BitMessageBox>

<BitMessageBox Title=""IconAriaLabel""
               Body=""The glyph is announced as &quot;Warning&quot;.""
               IconName=""@BitIconName.Warning""
               IconAriaLabel=""Warning"" />";

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
<BitMessageBox @ref=""templatesMessageBox""
               Title=""Delete the workspace?""
               OnResult=""v => templatesResult = v"">
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
        <BitButton Color=""BitColor.Error""
                   IconName=""@BitIconName.Delete""
                   OnClick=""() => templatesMessageBox!.AnswerAsync(BitMessageBoxResult.Yes)"">
            Delete forever
        </BitButton>
        <BitButton Variant=""BitVariant.Outline""
                   Color=""BitColor.Tertiary""
                   OnClick=""() => templatesMessageBox!.AnswerAsync(BitMessageBoxResult.No)"">
            Keep it
        </BitButton>
    </FooterTemplate>
</BitMessageBox>

<div>Last answer: <b>@templatesResult</b></div>";
    private readonly string example6CsharpCode = @"
private BitMessageBox? templatesMessageBox;
private BitMessageBoxResult templatesResult;";

    private readonly string example7RazorCode = @"
<BitMessageBox AutoLoading
               Title=""Delete the file?""
               Buttons=""BitMessageBoxButtons.YesNo""
               YesText=""Delete""
               NoText=""Keep""
               PrimaryButtonColor=""BitColor.Error""
               DefaultButton=""BitMessageBoxResult.No""
               OnBeforeResult=""HandleBeforeResult""
               OnResult=""v => guardResult = v"">
    <BodyTemplate>
        <BitCheckbox @bind-Value=""guardConfirmed"" Label=""Yes, I understand this cannot be undone."" />
    </BodyTemplate>
</BitMessageBox>

<div>
    Last answer: <b>@guardResult</b>
    @if (guardRefused)
    {
        <span> - the last Delete was refused, since the box was not ticked.</span>
    }
</div>";
    private readonly string example7CsharpCode = @"
private bool guardConfirmed;
private bool guardRefused;
private BitMessageBoxResult guardResult;

private async Task HandleBeforeResult(BitMessageBoxBeforeResultArgs args)
{
    guardRefused = false;

    // Only the destructive answer is guarded: Keep and the close button end the box as they always would.
    if (args.Result is not BitMessageBoxResult.Yes) return;

    // The work the answer starts, which AutoLoading spins the pressed button through.
    await Task.Delay(1000, args.CancellationToken);

    if (guardConfirmed) return;

    // Refused: nothing is reported, and a box shown through the service would stay open.
    args.Cancel = true;
    guardRefused = true;
}";

    private readonly string example8RazorCode = @"
<BitButton OnClick=""() => isModalOpen = true"">Show</BitButton>

<BitModal @bind-IsOpen=""isModalOpen"" TitleAriaId=""modal-msb-ttl"" SubtitleAriaId=""modal-msb-bdy"">
    <BitMessageBox Id=""modal-msb""
                   AutoFocus
                   TitleElement=""h2""
                   OnClose=""() => isModalOpen = false""
                   Title=""Session expiring""
                   Body=""You will be signed out in 2 minutes."" />
</BitModal>";
    private readonly string example8CsharpCode = @"
private bool isModalOpen;";

    private readonly string example9RazorCode = @"
<BitButton OnClick=""ShowMessageBox"">Show MessageBox</BitButton>

<div>Last answer: <b>@modalServiceResult</b></div>";
    private readonly string example9CsharpCode = @"
[AutoInject] private BitModalService modalService { get; set; } = default!;

private BitMessageBoxResult modalServiceResult;

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

    private readonly string example10RazorCode = @"
<BitButton OnClick=""ShowMessageBoxService"">Show</BitButton>
<BitButton Color=""BitColor.Info"" OnClick=""ShowInfoMessageBox"">Info</BitButton>
<BitButton Color=""BitColor.Success"" OnClick=""ShowSuccessMessageBox"">Success</BitButton>
<BitButton Color=""BitColor.Warning"" OnClick=""ShowWarningMessageBox"">Warning</BitButton>
<BitButton Color=""BitColor.SevereWarning"" OnClick=""ShowSevereWarningMessageBox"">SevereWarning</BitButton>
<BitButton Color=""BitColor.Error"" OnClick=""ShowErrorMessageBox"">Error</BitButton>
<BitButton Variant=""BitVariant.Outline"" OnClick=""ShowTimedMessageBox"">Closes in 5s</BitButton>

<div>Last answer: <b>@serviceResult</b></div>";
    private readonly string example10CsharpCode = @"
[AutoInject] private BitMessageBoxService messageBoxService { get; set; } = default!;

private BitMessageBoxResult serviceResult;

private async Task ShowMessageBoxService()
{
    serviceResult = await messageBoxService.Show(""TITLE"", ""BODY"", BitMessageBoxButtons.OkCancel);
}

private async Task ShowInfoMessageBox()
{
    serviceResult = await messageBoxService.ShowInfo(""Information"", ""The export finished in 4 seconds."");
}

private async Task ShowSuccessMessageBox()
{
    serviceResult = await messageBoxService.ShowSuccess(""Success"", ""Your changes are saved."");
}

private async Task ShowWarningMessageBox()
{
    serviceResult = await messageBoxService.ShowWarning(""Warning"", ""This workspace is almost out of space."");
}

private async Task ShowSevereWarningMessageBox()
{
    serviceResult = await messageBoxService.ShowSevereWarning(""Severe warning"", ""This workspace is out of space."");
}

private async Task ShowErrorMessageBox()
{
    serviceResult = await messageBoxService.ShowError(""Error"", ""The file could not be uploaded."");
}

private async Task ShowTimedMessageBox()
{
    // The token takes the box back off the screen; a cancelled showing answers None.
    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

    serviceResult = await messageBoxService.Show(new() { Title = ""Saved"", Body = ""This closes itself in 5 seconds."" }, cts.Token);
}";

    private readonly string example11RazorCode = @"
<BitButton OnClick=""ShowConfirm"">Confirm</BitButton>
<BitButton Color=""BitColor.Error"" OnClick=""ShowDangerousConfirm"">Delete something</BitButton>

<div>Last answer: <b>@(confirmResult?.ToString() ?? ""-"")</b></div>";
    private readonly string example11CsharpCode = @"
[AutoInject] private BitMessageBoxService messageBoxService { get; set; } = default!;

private bool? confirmResult;

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
        IconAriaLabel = ""Error"",
        // A click beside the box does not dismiss it; Escape still does.
        Modal = new() { Blocking = true }
    });
}";

    private readonly string example12RazorCode = @"
<BitButton OnClick=""ShowPrompt"">Rename</BitButton>
<BitButton OnClick=""ShowValidatedPrompt"">New folder</BitButton>
<BitButton OnClick=""ShowPasswordPrompt"">Password</BitButton>
<BitButton OnClick=""ShowMultilinePrompt"">Feedback</BitButton>

<div>Last answer: <b>@(promptResult ?? ""null"")</b></div>";
    private readonly string example12CsharpCode = @"
[AutoInject] private BitMessageBoxService messageBoxService { get; set; } = default!;

private string? promptResult;

private async Task ShowPrompt()
{
    promptResult = await messageBoxService.Prompt(""Rename"", ""The new name of the file:"", ""report.pdf"");
}

private async Task ShowValidatedPrompt()
{
    promptResult = await messageBoxService.Prompt(new()
    {
        Title = ""New folder"",
        Body = ""Folders hold the files of a project."",
        Label = ""Name"",
        Description = ""Up to 40 characters, no slashes."",
        Placeholder = ""Untitled folder"",
        AutoComplete = ""off"",
        MaxLength = 40,
        Required = true,
        RequiredMessage = ""Give the folder a name."",
        Validator = v => v.IndexOfAny(['/', '\\']) >= 0 ? ""A name cannot hold a slash."" : null,
        AsyncValidator = IsFolderNameTaken,
        OkText = ""Create""
    });
}

// Stands in for a call to the server, which is the only one that knows the folders already there.
private static async Task<string?> IsFolderNameTaken(string name, CancellationToken cancellationToken)
{
    await Task.Delay(800, cancellationToken);

    return string.Equals(name.Trim(), ""Projects"", StringComparison.OrdinalIgnoreCase) ? ""A folder with this name already exists."" : null;
}

private async Task ShowPasswordPrompt()
{
    var password = await messageBoxService.Prompt(new()
    {
        Title = ""Delete the account?"",
        Body = ""Enter your password to confirm."",
        Color = BitColor.Error,
        Label = ""Password"",
        InputType = BitInputType.Password,
        AutoComplete = ""current-password"",
        CanRevealPassword = true,
        Required = true,
        OkText = ""Delete"",
        PrimaryButtonColor = BitColor.Error
    });

    // The password itself is the caller's to check; only whether one was given is shown here.
    promptResult = password is null ? null : ""(a password)"";
}

private async Task ShowMultilinePrompt()
{
    promptResult = await messageBoxService.Prompt(new()
    {
        Title = ""Feedback"",
        Body = ""What could be better?"",
        Multiline = true,
        OkText = ""Send""
    });
}";

    private readonly string example13RazorCode = @"
<BitParams Parameters=""messageBoxParams"">
    <BitMessageBox Title=""Save changes?""
                   Body=""Your edits have not been saved.""
                   Buttons=""BitMessageBoxButtons.YesNoCancel"" />

    <BitMessageBox Title=""Discard the draft?""
                   Body=""The draft is removed for good.""
                   Buttons=""BitMessageBoxButtons.YesNoCancel""
                   YesText=""Discard"" />
</BitParams>";
    private readonly string example13CsharpCode = @"
private readonly BitMessageBoxParams[] messageBoxParams =
[
    new()
    {
        Size = BitSize.Small,
        YesText = ""Save"",
        NoText = ""Don't save"",
        CancelText = ""Go back"",
        CloseButtonTitle = ""Dismiss"",
        PrimaryButtonColor = BitColor.Primary,
        DefaultButton = BitMessageBoxResult.Cancel
    }
];";

    private readonly string example14RazorCode = @"
<BitMessageBox Color=""BitColor.Info"" Title=""Info"" Body=""Something worth knowing."" />

<BitMessageBox Color=""BitColor.Success"" Title=""Success"" Body=""Something went well."" />

<BitMessageBox Color=""BitColor.Warning"" Title=""Warning"" Body=""Something needs attention."" />

<BitMessageBox Color=""BitColor.SevereWarning"" Title=""SevereWarning"" Body=""Something needs attention now."" />

<BitMessageBox Color=""BitColor.Error"" Title=""Error"" Body=""Something went wrong."" />

<BitMessageBox Color=""BitColor.Primary"" Title=""Primary"" Body=""The accent of the theme."" />

<BitMessageBox Color=""BitColor.Error"" HideIcon Title=""HideIcon"" Body=""An Error without its glyph."" />";

    private readonly string example15RazorCode = @"
<link rel=""stylesheet"" href=""https://cdnjs.cloudflare.com/ajax/libs/font-awesome/7.0.1/css/all.min.css"" />
<link rel=""stylesheet"" href=""https://cdnjs.cloudflare.com/ajax/libs/bootstrap-icons/1.11.3/font/bootstrap-icons.min.css"" />

<BitMessageBox Color=""BitColor.Warning""
               Icon=""@BitIconInfo.Fa(""solid triangle-exclamation"")""
               Title=""FontAwesome""
               Body=""The glyph comes from FontAwesome."" />

<BitMessageBox Color=""BitColor.Info""
               Icon=""@BitIconInfo.Bi(""info-circle"")""
               CloseIcon=""@BitIconInfo.Bi(""x-lg"")""
               Title=""Bootstrap Icons""
               Body=""The glyph and the close icon come from Bootstrap Icons."" />";

    private readonly string example16RazorCode = @"
<BitMessageBox Size=""BitSize.Small"" Color=""BitColor.Info"" Title=""Small"" Body=""The small size."" />

<BitMessageBox Size=""BitSize.Medium"" Color=""BitColor.Info"" Title=""Medium"" Body=""The medium size."" />

<BitMessageBox Size=""BitSize.Large"" Color=""BitColor.Info"" Title=""Large"" Body=""The large size."" />";

    private readonly string example17RazorCode = @"
<style>
    .custom-msg {
        background: linear-gradient(180deg, #3e0f0f, transparent) #000;
    }

    .custom-msg-txt {
        color: #fff;
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


<BitMessageBox Title=""It's a title""
               Body=""It's a body.""
               Styles=""@(new() { Root = ""background: linear-gradient(180deg, #222444, transparent) #000"",
                                 Title = ""color: #fff"",
                                 Body = ""color: #fff"",
                                 CloseButton = new() { Root = ""color: #fff"" },
                                 OkButton = new() { Root = ""border-radius:1rem"" } })"" />

<BitMessageBox Title=""It's a title""
               Body=""It's a body.""
               Buttons=""BitMessageBoxButtons.OkCancel""
               Classes=""@(new() { Root = ""custom-msg"",
                                  Title = ""custom-msg-txt"",
                                  Body = ""custom-msg-txt"",
                                  CloseButton = new() { Root = ""custom-msg-txt"" },
                                  ActionButton = new() { Root = ""custom-msg-btn"" } })"" />

<BitMessageBox Title=""CSS variables""
               Body=""Centered, with a larger glyph.""
               Color=""BitColor.Success""
               Buttons=""BitMessageBoxButtons.OkCancel""
               Style=""@cssVariablesStyle"" />";
    private readonly string example17CsharpCode = @"
private const string cssVariablesStyle = ""--bit-MessageBox-text-align:center;"" +
                                         ""--bit-MessageBox-actions-justify:center;"" +
                                         ""--bit-MessageBox-icon-size:2rem;"" +
                                         ""--bit-MessageBox-title-color:var(--bit-clr-suc);"" +
                                         ""--bit-MessageBox-padding:2rem;"";";

    private readonly string example18RazorCode = @"
<BitMessageBox Dir=""BitDir.Rtl""
               Color=""BitColor.Warning""
               Title=""عنوان پیام""
               Body=""متن تست پیام...""
               Buttons=""BitMessageBoxButtons.YesNo""
               YesText=""بله""
               NoText=""خیر""
               CloseButtonTitle=""بستن"" />";
}
