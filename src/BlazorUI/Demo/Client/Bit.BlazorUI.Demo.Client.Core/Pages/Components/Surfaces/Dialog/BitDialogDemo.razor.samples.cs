namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Surfaces.Dialog;

public partial class BitDialogDemo
{
    private readonly string example1RazorCode = @"
<BitButton OnClick=""@(() => isOpenBasic = true)"">Open Dialog</BitButton>
<BitDialog @bind-IsOpen=""isOpenBasic"" Title=""Missing subject"" Message=""Do you want to send this message without a subject?"" />";
    private readonly string example1CsharpCode = @"
private bool isOpenBasic;";

    private readonly string example2RazorCode = @"
<style>
    .btn-container {
        gap: 1rem;
        display: flex;
        flex-flow: row wrap;
    }

    .dialog-body {
        max-width: 40rem;
        padding: 0 24px 24px;
    }
</style>

<div class=""btn-container"">
    <BitButton OnClick=""@(() => isOpenLabels = true)"">Custom labels</BitButton>
    <BitButton OnClick=""@(() => isOpenAcknowledge = true)"">Single action</BitButton>
    <BitButton OnClick=""@(() => { agreed = false; isOpenGated = true; })"">Gated Ok</BitButton>
</div>

<BitDialog @bind-IsOpen=""isOpenLabels""
           Title=""Delete this file?""
           Message=""This file will be moved to the trash. You can restore it for 30 days.""
           OkText=""Move to trash""
           CancelText=""Keep it"" />

<BitDialog @bind-IsOpen=""isOpenAcknowledge""
           ShowCancelButton=""false""
           ShowCloseButton=""false""
           Title=""Your session expired""
           Message=""Sign in again to pick up where you left off.""
           OkText=""Got it"" />

<BitDialog @bind-IsOpen=""isOpenGated""
           IsOkButtonEnabled=""agreed""
           Title=""Before you continue""
           OkText=""Accept"">
    <div class=""dialog-body"">
        <BitCheckbox @bind-Value=""agreed"" Label=""I have read and agree to the terms"" />
    </div>
</BitDialog>";
    private readonly string example2CsharpCode = @"
private bool isOpenLabels;
private bool isOpenAcknowledge;
private bool isOpenGated;
private bool agreed;";

    private readonly string example3RazorCode = @"
<style>
    .btn-container {
        gap: 1rem;
        display: flex;
        flex-flow: row wrap;
    }

    .dialog-header {
        gap: 0.5rem;
        display: flex;
        font-size: 20px;
        font-weight: 600;
        align-items: center;
    }

    .dialog-footer {
        gap: 0.5rem;
        display: flex;
        padding: 0 24px 24px;
        justify-content: flex-end;
    }
</style>

<div class=""btn-container"">
    <BitButton OnClick=""@(() => isOpenSubtitle = true)"">Subtitle</BitButton>
    <BitButton OnClick=""@(() => isOpenHeaderTemplate = true)"">HeaderTemplate</BitButton>
    <BitButton OnClick=""@(() => isOpenFooterTemplate = true)"">FooterTemplate</BitButton>
</div>

<BitDialog @bind-IsOpen=""isOpenSubtitle""
           Title=""Publish this version?""
           Subtitle=""Version 4.2.0 · 18 changed files""
           Message=""Everyone in the workspace will see this version as soon as it goes out."" />

<BitDialog @bind-IsOpen=""isOpenHeaderTemplate""
           AriaLabel=""Storage almost full""
           Message=""Delete something, or move up to the next plan to keep syncing."">
    <HeaderTemplate>
        <div class=""dialog-header"">
            <BitIcon IconName=""@BitIconName.Warning"" Color=""BitColor.Warning"" />
            <span>Storage almost full</span>
        </div>
    </HeaderTemplate>
</BitDialog>

<BitDialog @bind-IsOpen=""isOpenFooterTemplate""
           ShowOkButton=""false""
           ShowCancelButton=""false""
           Title=""Delete all""
           Message=""99+ emails will be deleted, and deleted emails cannot be recovered."">
    <FooterTemplate>
        <div class=""dialog-footer"">
            <BitButton Variant=""BitVariant.Text"" OnClick=""@(() => isOpenFooterTemplate = false)"">Not now</BitButton>
            <BitButton Color=""BitColor.Error"" OnClick=""@(() => isOpenFooterTemplate = false)"">Delete all</BitButton>
        </div>
    </FooterTemplate>
</BitDialog>";
    private readonly string example3CsharpCode = @"
private bool isOpenSubtitle;
private bool isOpenHeaderTemplate;
private bool isOpenFooterTemplate;";

    private readonly string example4RazorCode = @"
<style>
    .dialog-title {
        margin: 0;
        display: flex;
        font-size: 20px;
        font-weight: 600;
        align-items: center;
        padding: 12px 12px 14px 24px;
        justify-content: space-between;
        border-top: 4px solid var(--bit-clr-pri);
    }

    .dialog-body {
        max-width: 40rem;
        padding: 0 24px 24px;
    }
</style>

<BitButton OnClick=""@(() => isOpenCustom = true)"">Open Dialog</BitButton>
<div>Selected option: @(optionValue ?? ""-"")</div>

<BitDialog @bind-IsOpen=""isOpenCustom""
           TitleAriaId=""dialog-custom-title""
           ShowCloseButton=""false"">
    <h2 class=""dialog-title"" id=""dialog-custom-title"">All emails together</h2>
    <div class=""dialog-body"">
        <p>Your inbox has changed: it no longer includes favorites, it is a single destination for all your emails.</p>
        <br />
        <BitChoiceGroup @bind-Value=""optionValue"" Label=""Show first"" TItem=""BitChoiceGroupOption<string>"" TValue=""string"">
            <BitChoiceGroupOption Text=""Newest"" Value=""@(""Newest"")"" />
            <BitChoiceGroupOption Text=""Unread"" Value=""@(""Unread"")"" />
            <BitChoiceGroupOption Text=""Starred"" Value=""@(""Starred"")"" />
        </BitChoiceGroup>
    </div>
</BitDialog>";
    private readonly string example4CsharpCode = @"
private bool isOpenCustom;
private string? optionValue;";

    private readonly string example5RazorCode = @"
<style>
    .btn-container {
        gap: 1rem;
        display: flex;
        flex-flow: row wrap;
    }
</style>

<div class=""btn-container"">
    <BitButton OnClick=""@(() => isOpenResult = true)"">Open Dialog</BitButton>
    <BitButton OnClick=""ShowAndAwait"">Show and await</BitButton>
</div>
<div>Result: @(resultDialogRef?.Result?.ToString() ?? ""-""), reason: @(resultDialogRef?.DismissReason?.ToString() ?? ""-"")</div>
<div>Awaited result: @awaitedResult</div>

<BitDialog @ref=""resultDialogRef""
           @bind-IsOpen=""isOpenResult""
           Title=""Missing subject""
           Message=""Do you want to send this message without a subject?"" />

<BitDialog @ref=""awaitDialogRef""
           Title=""Discard draft?""
           OkText=""Discard""
           CancelText=""Keep editing""
           Message=""Your changes since the last save will be lost."" />";
    private readonly string example5CsharpCode = @"
private bool isOpenResult;
private BitDialog resultDialogRef = default!;
private BitDialog awaitDialogRef = default!;
private string awaitedResult = ""-"";

private async Task ShowAndAwait()
{
    var result = await awaitDialogRef.Show();

    awaitedResult = result?.ToString() ?? ""(dismissed)"";
}";

    private readonly string example6RazorCode = @"
<BitButton OnClick=""@(() => isOpenEvents = true)"">Open Dialog</BitButton>
<div>Last event: @lastEvent</div>

<BitDialog @bind-IsOpen=""isOpenEvents""
           Title=""Missing subject""
           Message=""Press Ok and it waits a second before closing.""
           OnOpen=""@(() => lastEvent = ""OnOpen"")""
           OnOk=""HandleSlowOk""
           OnCancel=""@(() => lastEvent = ""OnCancel"")""
           OnClose=""@(() => lastEvent = ""OnClose"")""
           OnOverlayClick=""@(() => lastEvent = ""OnOverlayClick"")""
           OnDismiss=""@(() => lastEvent += "" → OnDismiss"")"" />";
    private readonly string example6CsharpCode = @"
private bool isOpenEvents;
private string lastEvent = ""-"";

private async Task HandleSlowOk()
{
    lastEvent = ""OnOk (working...)"";

    await Task.Delay(1000);

    lastEvent = ""OnOk"";
}";

    private readonly string example7RazorCode = @"
<style>
    .btn-container {
        gap: 1rem;
        display: flex;
        flex-flow: row wrap;
    }
</style>

<div class=""btn-container"">
    <BitButton OnClick=""@(() => isOpenBlocking = true)"">IsBlocking</BitButton>
    <BitButton OnClick=""@(() => isOpenNoOverlayClick = true)"">CloseOnOverlayClick=""false""</BitButton>
    <BitButton OnClick=""@(() => isOpenNoEscape = true)"">CloseOnEscape=""false""</BitButton>
    <BitButton OnClick=""@(() => isOpenModeless = true)"">IsModeless</BitButton>
</div>

<BitDialog IsBlocking
           @bind-IsOpen=""isOpenBlocking""
           Title=""Two-factor code""
           Subtitle=""@preventedHint""
           Message=""Enter the six-digit code from your authenticator app to finish signing in.""
           OkText=""Verify""
           CancelText=""Use another method""
           OnOpen=""@(() => preventedHint = null)""
           OnDismissPrevented=""@(r => preventedHint = $""{r} will not close this one - answer it with a button."")"" />

<BitDialog CloseOnOverlayClick=""false""
           @bind-IsOpen=""isOpenNoOverlayClick""
           Title=""Edit profile""
           Message=""A click outside is refused, but the Escape key still closes it."" />

<BitDialog CloseOnEscape=""false""
           @bind-IsOpen=""isOpenNoEscape""
           Title=""Missing subject""
           Message=""The Escape key is refused, but a click outside still closes it."" />

<BitDialog IsModeless
           @bind-IsOpen=""isOpenModeless""
           Title=""Modeless""
           Message=""There is no overlay, so the page behind this one is still usable."" />";
    private readonly string example7CsharpCode = @"
private bool isOpenBlocking;
private bool isOpenNoOverlayClick;
private bool isOpenNoEscape;
private bool isOpenModeless;
private string? preventedHint;";

    private readonly string example8RazorCode = @"
<BitToggle Label=""The note has unsaved changes"" @bind-Value=""hasUnsavedChanges"" />
<BitButton OnClick=""@(() => isOpenGuarded = true)"">Open Dialog</BitButton>
<div>Last refused: @refusedGesture</div>

<BitDialog @bind-IsOpen=""isOpenGuarded""
           Title=""Edit the note""
           Subtitle=""@guardedHint""
           Message=""While the toggle is on, everything but Save is refused.""
           OkText=""Save""
           CancelText=""Discard""
           OnDismissing=""HandleDismissing""
           OnOpen=""@(() => { guardedHint = null; refusedGesture = ""-""; })""
           OnDismissPrevented=""@(r => { refusedGesture = r.ToString(); guardedHint = ""There are unsaved changes - save them first.""; })"" />";
    private readonly string example8CsharpCode = @"
private bool hasUnsavedChanges = true;
private bool isOpenGuarded;
private string? guardedHint;
private string refusedGesture = ""-"";

private void HandleDismissing(BitDialogDismissArgs args)
{
    // Save is the way out that is always let through, so the Dialog is never a trap.
    args.Cancel = hasUnsavedChanges && args.Reason is not BitDialogDismissReason.OkButton;
}";

    private readonly string example9RazorCode = @"
<style>
    .btn-container {
        gap: 1rem;
        display: flex;
        flex-flow: row wrap;
    }

    .dialog-body {
        max-width: 40rem;
        padding: 0 24px 24px;
    }
</style>

<div class=""btn-container"">
    <BitButton OnClick=""@(() => isOpenFocus = true)"">Default</BitButton>
    <BitButton OnClick=""@(() => isOpenNoFocus = true)"">AutoFocus & TrapFocus off</BitButton>
    <BitButton OnClick=""@(() => isOpenFocusCancel = true)"">AutoFocusButton</BitButton>
    <BitButton OnClick=""@(() => isOpenFocusSelector = true)"">AutoFocusSelector</BitButton>
</div>

<BitDialog @bind-IsOpen=""isOpenFocus""
           Title=""Rename the project""
           ShowCloseButton=""false"">
    <div class=""dialog-body"">
        <BitTextField Label=""Name"" DefaultValue=""Untitled project"" />
    </div>
</BitDialog>

<BitDialog AutoFocus=""false""
           TrapFocus=""false""
           @bind-IsOpen=""isOpenNoFocus""
           Title=""Rename the project""
           ShowCloseButton=""false"">
    <div class=""dialog-body"">
        <BitTextField Label=""Name"" DefaultValue=""Untitled project"" />
    </div>
</BitDialog>

<BitDialog IsAlert
           AutoFocusButton=""BitDialogButton.Cancel""
           @bind-IsOpen=""isOpenFocusCancel""
           ShowCloseButton=""false""
           Title=""Delete this workspace?""
           Message=""Every project, file and comment in it goes with it. This cannot be undone.""
           OkText=""Delete workspace"" />

<BitDialog AutoFocusSelector="".invite-email input""
           @bind-IsOpen=""isOpenFocusSelector""
           Title=""Invite a teammate""
           ShowCloseButton=""false""
           OkText=""Send invite"">
    <div class=""dialog-body"">
        <BitLink Href=""/components/dialog"">What can a guest see?</BitLink>
        <BitTextField Class=""invite-email"" Label=""Email"" Placeholder=""name@example.com"" />
    </div>
</BitDialog>";
    private readonly string example9CsharpCode = @"
private bool isOpenFocus;
private bool isOpenNoFocus;
private bool isOpenFocusCancel;
private bool isOpenFocusSelector;";

    private readonly string example10RazorCode = @"
<style>
    .position-grid {
        gap: 0.5rem;
        display: grid;
        grid-template-columns: repeat(3, 1fr);
    }
</style>

<div class=""position-grid"">
    @foreach (var pos in dialogPositions)
    {
        <BitButton Variant=""BitVariant.Outline"" OnClick=""() => OpenDialogInPosition(pos)"">@pos</BitButton>
    }
</div>

<BitDialog @bind-IsOpen=""isOpenPosition""
           Position=""position""
           Title=""@position.ToString()""
           Message=""Do you want to send this message without a subject?"" />";
    private readonly string example10CsharpCode = @"
private bool isOpenPosition;
private BitDialogPosition position;
private readonly BitDialogPosition[] dialogPositions =
[
    BitDialogPosition.TopLeft, BitDialogPosition.TopCenter, BitDialogPosition.TopRight,
    BitDialogPosition.CenterLeft, BitDialogPosition.Center, BitDialogPosition.CenterRight,
    BitDialogPosition.BottomLeft, BitDialogPosition.BottomCenter, BitDialogPosition.BottomRight,
];

private void OpenDialogInPosition(BitDialogPosition value)
{
    position = value;
    isOpenPosition = true;
}";

    private readonly string example11RazorCode = @"
<style>
    .btn-container {
        gap: 1rem;
        display: flex;
        flex-flow: row wrap;
    }

    .relative-container {
        width: 100%;
        height: 20rem;
        overflow: auto;
        padding: 0.5rem;
        margin-top: 1rem;
        position: relative;
        box-sizing: border-box;
        border: 2px solid var(--bit-clr-brd-pri);
    }
</style>

<div class=""btn-container"">
    <BitButton OnClick=""@(() => isOpenScrollLock = true)"">AutoToggleScroll</BitButton>
    <BitButton OnClick=""@(() => isOpenAbsolute = true)"">AbsolutePosition</BitButton>
</div>

<BitDialog AutoToggleScroll
           @bind-IsOpen=""isOpenScrollLock""
           Title=""The page is locked""
           Message=""The page behind this Dialog cannot be scrolled while it is open."" />

<div class=""relative-container"">
    <BitDialog AbsolutePosition
               AutoToggleScroll
               ScrollerSelector="".relative-container""
               @bind-IsOpen=""isOpenAbsolute""
               Title=""Inside the box""
               Message=""This Dialog covers and locks the bordered box, not the page."" />

    Every story starts with a blank canvas, a quiet space waiting to be filled with ideas, emotions, and dreams.
    These placeholder words symbolize the beginning - a moment of possibility where creativity has yet to take shape.
    Imagine this text as the scaffolding of something remarkable, a foundation upon which connections and
    inspirations will be built. Soon, these lines will transform into narratives that provoke thought,
    spark emotion, and resonate with those who encounter them.
    <br />
    Once upon a time, stories wove connections between people, a symphony of voices crafting shared dreams.
    Each word carried meaning, each pause brought understanding. Placeholder text reminds us of that moment
    when possibilities are limitless, waiting for content to emerge. The spaces here are open for growth,
    for ideas that change minds and spark emotions. This is where the journey begins.
    <br />
    In the beginning, there is silence, a blank canvas yearning to be filled, a quiet space where creativity waits
    to awaken. These words are temporary, standing in place of ideas yet to come, a glimpse into the infinite
    possibilities that lie ahead. Think of this text as a bridge, connecting the empty spaces of now with the
    vibrant narratives of tomorrow.
</div>";
    private readonly string example11CsharpCode = @"
private bool isOpenScrollLock;
private bool isOpenAbsolute;";

    private readonly string example12RazorCode = @"
<style>
    .btn-container {
        gap: 1rem;
        display: flex;
        flex-flow: row wrap;
    }

    .dialog-title {
        display: flex;
        font-size: 20px;
        font-weight: 600;
        align-items: center;
        padding: 12px 12px 14px 24px;
        justify-content: space-between;
        border-top: 4px solid var(--bit-clr-pri);
    }

    .dialog-body {
        max-width: 40rem;
        padding: 0 24px 24px;
    }
</style>

<BitToggle Label=""IsDraggable"" @bind-Value=""isDraggable"" />
<div class=""btn-container"">
    <BitButton OnClick=""@(() => isOpenDraggable = true)"">Header handle</BitButton>
    <BitButton OnClick=""@(() => isOpenDragHandle = true)"">DragElementSelector</BitButton>
</div>

<BitDialog IsDraggable=""isDraggable""
           @bind-IsOpen=""isOpenDraggable""
           Title=""Draggable dialog""
           Message=""Drag it by its header while the toggle is on."" />

<BitDialog IsDraggable
           DragElementSelector="".dialog-title""
           @bind-IsOpen=""isOpenDragHandle""
           TitleAriaId=""dialog-drag-title""
           ShowCloseButton=""false"">
    <div class=""dialog-title"">
        <span id=""dialog-drag-title"">Drag me by this bar</span>
        <BitButton Variant=""BitVariant.Text"" OnClick=""@(() => isOpenDragHandle = false)"" IconName=""@BitIconName.ChromeClose"" Title=""Close"" />
    </div>
    <div class=""dialog-body"">
        The bar above is the handle; the button inside it still closes the Dialog.
    </div>
</BitDialog>";
    private readonly string example12CsharpCode = @"
private bool isDraggable = true;
private bool isOpenDraggable;
private bool isOpenDragHandle;";

    private readonly string example13RazorCode = @"
<style>
    .dialog-body {
        max-width: 40rem;
        padding: 0 24px 24px;
    }
</style>

<BitButton OnClick=""@(() => isOpenOuter = true)"">Open Dialog</BitButton>

<BitDialog @bind-IsOpen=""isOpenOuter""
           Title=""Publish this version?""
           Message=""Everyone in the workspace will see this version as soon as it goes out.""
           OkText=""Publish"">
    <div class=""dialog-body"">
        <BitDropdown Combo
                     Label=""Notify""
                     Items=""audienceItems""
                     DefaultValue=""@string.Empty""
                     Placeholder=""Type to filter"" />
        <br />
        <BitButton Variant=""BitVariant.Text"" OnClick=""@(() => isOpenInner = true)"">What changed?</BitButton>

        <BitDialog @bind-IsOpen=""isOpenInner""
                   Title=""Changes in 4.2.0""
                   Subtitle=""18 changed files""
                   ShowOkButton=""false""
                   CancelText=""Back""
                   Message=""Press Escape here and only this Dialog closes."" />
    </div>
</BitDialog>";
    private readonly string example13CsharpCode = @"
private bool isOpenOuter;
private bool isOpenInner;

private readonly List<BitDropdownItem<string>> audienceItems =
[
    new() { Text = ""Everyone"", Value = ""all"" },
    new() { Text = ""Editors"", Value = ""editors"" },
    new() { Text = ""Reviewers"", Value = ""reviewers"" },
    new() { Text = ""Nobody"", Value = ""none"" }
];";

    private readonly string example14RazorCode = @"
<style>
    .btn-container {
        gap: 1rem;
        display: flex;
        flex-flow: row wrap;
    }

    .dialog-body {
        max-width: 40rem;
        padding: 0 24px 24px;
    }
</style>

<div class=""btn-container"">
    <BitButton OnClick=""@(() => isOpenKeptMounted = true)"">KeepMounted</BitButton>
    <BitButton OnClick=""@(() => isOpenUnmounted = true)"">Default</BitButton>
</div>

<BitDialog KeepMounted
           @bind-IsOpen=""isOpenKeptMounted""
           Title=""Report an issue""
           ShowCloseButton=""false""
           OkText=""Send"">
    <div class=""dialog-body"">
        <BitTextField Label=""What happened?"" Multiline Rows=""4"" />
    </div>
</BitDialog>

<BitDialog @bind-IsOpen=""isOpenUnmounted""
           Title=""Report an issue""
           ShowCloseButton=""false""
           OkText=""Send"">
    <div class=""dialog-body"">
        <BitTextField Label=""What happened?"" Multiline Rows=""4"" />
    </div>
</BitDialog>";
    private readonly string example14CsharpCode = @"
private bool isOpenKeptMounted;
private bool isOpenUnmounted;";

    private readonly string example15RazorCode = @"
<style>
    .btn-container {
        gap: 1rem;
        display: flex;
        flex-flow: row wrap;
    }
</style>

<div class=""btn-container"">
    <BitButton OnClick=""() => programmaticDialogRef.Open()"">Open</BitButton>
    <BitButton Variant=""BitVariant.Outline"" OnClick=""() => programmaticDialogRef.Toggle()"">Toggle</BitButton>
</div>

<BitDialog @ref=""programmaticDialogRef""
           Title=""Driven by methods""
           Message=""This Dialog has no IsOpen binding: it is opened and closed through its reference.""
           ShowOkButton=""false""
           CancelText=""Close"" />";
    private readonly string example15CsharpCode = @"
private BitDialog programmaticDialogRef = default!;";

    private readonly string example16RazorCode = @"
<style>
    .btn-container {
        gap: 1rem;
        display: flex;
        flex-flow: row wrap;
    }
</style>

<BitParams Parameters=""dialogParams"">
    <div class=""btn-container"">
        <BitButton OnClick=""@(() => isOpenCascadedFile = true)"">Delete file</BitButton>
        <BitButton OnClick=""@(() => isOpenCascadedFolder = true)"">Delete folder</BitButton>
    </div>

    <BitDialog @bind-IsOpen=""isOpenCascadedFile""
               Title=""Delete this file?""
               Message=""It moves to the trash, where it stays for 30 days."" />

    <BitDialog @bind-IsOpen=""isOpenCascadedFolder""
               OkText=""Delete folder""
               Title=""Delete this folder?""
               Message=""Everything inside it moves to the trash too."" />
</BitParams>";
    private readonly string example16CsharpCode = @"
private bool isOpenCascadedFile;
private bool isOpenCascadedFolder;

private readonly BitDialogParams[] dialogParams =
[
    new()
    {
        OkText = ""Delete"",
        CancelText = ""Keep"",
        ShowCloseButton = false,
        CloseOnOverlayClick = false,
        AutoFocusButton = BitDialogButton.Cancel,
        Position = BitDialogPosition.TopCenter,
    }
];";

    private readonly string example17RazorCode = @"
<style>
    .btn-container {
        gap: 1rem;
        display: flex;
        flex-flow: row wrap;
    }
</style>

<div class=""btn-container"">
    @foreach (var color in dialogColors)
    {
        <BitButton Color=""color"" OnClick=""() => OpenDialogInColor(color)"">@color</BitButton>
    }
</div>

<BitDialog @bind-IsOpen=""isOpenColor""
           Color=""dialogColor""
           AutoFocusButton=""BitDialogButton.Cancel""
           Title=""@($""{dialogColor} dialog"")""
           Message=""The buttons, the ring around the focused one and the Ok spinner follow the color.""
           OkText=""Confirm""
           OnOk=""HandleColorOk"" />";
    private readonly string example17CsharpCode = @"
private bool isOpenColor;
private BitColor dialogColor = BitColor.Primary;
private readonly BitColor[] dialogColors = Enum.GetValues<BitColor>();

private void OpenDialogInColor(BitColor color)
{
    dialogColor = color;
    isOpenColor = true;
}

private async Task HandleColorOk()
{
    await Task.Delay(1000);
}";

    private readonly string example18RazorCode = @"
<style>
    .btn-container {
        gap: 1rem;
        display: flex;
        flex-flow: row wrap;
    }
</style>

<link rel=""stylesheet"" href=""https://cdnjs.cloudflare.com/ajax/libs/font-awesome/7.0.1/css/all.min.css"" />
<link rel=""stylesheet"" href=""https://cdn.jsdelivr.net/npm/bootstrap-icons@1.11.3/font/bootstrap-icons.min.css"" />

<div class=""btn-container"">
    <BitButton OnClick=""@(() => isOpenIconName = true)"">CloseIconName</BitButton>
    <BitButton OnClick=""@(() => isOpenIconFa = true)"">FontAwesome</BitButton>
    <BitButton OnClick=""@(() => isOpenIconBi = true)"">Bootstrap</BitButton>
    <BitButton OnClick=""@(() => isOpenIconCss = true)"">Css</BitButton>
</div>

<BitDialog @bind-IsOpen=""isOpenIconName""
           Title=""CloseIconName""
           Message=""A built-in Fluent UI icon, relabeled with CloseButtonTitle.""
           CloseButtonTitle=""Dismiss""
           CloseIconName=""@BitIconName.ChromeClose"" />

<BitDialog @bind-IsOpen=""isOpenIconFa""
           Title=""FontAwesome""
           Message=""A FontAwesome glyph, through BitIconInfo.Fa.""
           CloseIcon=""@BitIconInfo.Fa(""solid xmark"")"" />

<BitDialog @bind-IsOpen=""isOpenIconBi""
           Title=""Bootstrap Icons""
           Message=""A Bootstrap Icons glyph, through BitIconInfo.Bi.""
           CloseIcon=""@BitIconInfo.Bi(""x-lg"")"" />

<BitDialog @bind-IsOpen=""isOpenIconCss""
           Title=""CSS classes""
           Message=""Any CSS icon classes, through BitIconInfo.Css.""
           CloseIcon=""@BitIconInfo.Css(""fa-solid fa-circle-xmark"")"" />";
    private readonly string example18CsharpCode = @"
private bool isOpenIconName;
private bool isOpenIconFa;
private bool isOpenIconBi;
private bool isOpenIconCss;";

    private readonly string example19RazorCode = @"
<style>
    .btn-container {
        gap: 1rem;
        display: flex;
        flex-flow: row wrap;
    }

    .dialog-body {
        max-width: 40rem;
        padding: 0 24px 24px;
    }
</style>

<div class=""btn-container"">
    <BitButton OnClick=""@(() => isOpenWidth = true)"">Width</BitButton>
    <BitButton OnClick=""@(() => isOpenResponsive = true)"">MinWidth & MaxWidth</BitButton>
    <BitButton OnClick=""@(() => isOpenHeight = true)"">Height</BitButton>
    <BitButton OnClick=""@(() => isOpenFullWidth = true)"">FullWidth</BitButton>
    <BitButton OnClick=""@(() => isOpenFullSize = true)"">FullSize</BitButton>
</div>

<BitDialog Width=""32rem""
           @bind-IsOpen=""isOpenWidth""
           Title=""Fixed width""
           Message=""This Dialog is 32rem wide however little it has to say."" />

<BitDialog MinWidth=""20rem""
           MaxWidth=""min(100%, 28rem)""
           @bind-IsOpen=""isOpenResponsive""
           Title=""Responsive width""
           Message=""No narrower than 20rem, no wider than 28rem, and never wider than the screen."" />

<BitDialog Height=""24rem""
           MaxHeight=""min(100%, 24rem)""
           @bind-IsOpen=""isOpenHeight""
           Title=""Fixed height""
           OkText=""Agree"">
    <div class=""dialog-body"">
        Every story starts with a blank canvas, a quiet space waiting to be filled with ideas, emotions, and dreams.
    These placeholder words symbolize the beginning - a moment of possibility where creativity has yet to take shape.
    Imagine this text as the scaffolding of something remarkable, a foundation upon which connections and
    inspirations will be built. Soon, these lines will transform into narratives that provoke thought,
    spark emotion, and resonate with those who encounter them.
    <br />
    Once upon a time, stories wove connections between people, a symphony of voices crafting shared dreams.
    Each word carried meaning, each pause brought understanding. Placeholder text reminds us of that moment
    when possibilities are limitless, waiting for content to emerge. The spaces here are open for growth,
    for ideas that change minds and spark emotions. This is where the journey begins.
    <br />
    In the beginning, there is silence, a blank canvas yearning to be filled, a quiet space where creativity waits
    to awaken. These words are temporary, standing in place of ideas yet to come, a glimpse into the infinite
    possibilities that lie ahead. Think of this text as a bridge, connecting the empty spaces of now with the
    vibrant narratives of tomorrow.
    </div>
</BitDialog>

<BitDialog FullWidth
           Position=""BitDialogPosition.BottomCenter""
           @bind-IsOpen=""isOpenFullWidth""
           Title=""Full width""
           Message=""Stretched across the screen, at the bottom."" />

<BitDialog FullSize
           @bind-IsOpen=""isOpenFullSize""
           Title=""Full size""
           Message=""Stretched across the whole screen."" />";
    private readonly string example19CsharpCode = @"
private bool isOpenWidth;
private bool isOpenResponsive;
private bool isOpenHeight;
private bool isOpenFullWidth;
private bool isOpenFullSize;";

    private readonly string example20RazorCode = @"
<style>
    .btn-container {
        gap: 1rem;
        display: flex;
        flex-flow: row wrap;
    }

    .custom-container {
        border: 2px solid tomato;
    }

    .custom-header {
        background-color: #fff3f0;
    }

    .custom-ok {
        border-color: tomato;
        background-color: tomato;
    }

    .custom-dialog-variables {
        --bit-Dialog-radius: 1.5rem;
        --bit-Dialog-padding: 2rem;
        --bit-Dialog-margin: 1rem;
        --bit-Dialog-max-width: 26rem;
        --bit-Dialog-text-align: center;
        --bit-Dialog-border-width: 2px;
        --bit-Dialog-border-color: #7a2e8e;
        --bit-Dialog-title-color: #7a2e8e;
        --bit-Dialog-title-font-weight: 700;
        --bit-Dialog-overlay-backdrop-filter: blur(4px);
        --bit-Dialog-overlay-background: rgba(122, 46, 142, 0.25);
    }
</style>

<div class=""btn-container"">
    <BitButton OnClick=""@(() => isOpenStyles = true)"">Styles</BitButton>
    <BitButton OnClick=""@(() => isOpenClasses = true)"">Classes</BitButton>
    <BitButton OnClick=""@(() => isOpenCssVariables = true)"">CSS variables</BitButton>
</div>

<BitDialog @bind-IsOpen=""isOpenStyles""
           Title=""Styled Dialog""
           Subtitle=""Every part reachable on its own""
           Message=""The overlay, the container, the title and the two buttons are restyled here.""
           Styles=""@(new()
           {
               Overlay = ""backdrop-filter: blur(2px);"",
               Container = ""width: 24rem; border: 2px solid blueviolet;"",
               Title = ""color: blueviolet;"",
               Message = ""font-style: italic;"",
               OkButton = ""background-color: blueviolet; border-color: blueviolet;"",
               CancelButton = ""color: blueviolet; border-color: blueviolet;""
           })"" />

<div>
    <BitDialog @bind-IsOpen=""isOpenClasses""
               Title=""Classed Dialog""
               Message=""The same parts, reached with CSS classes of your own.""
               Classes=""@(new()
               {
                   Container = ""custom-container"",
                   Header = ""custom-header"",
                   OkButton = ""custom-ok""
               })"" />
</div>

<div class=""custom-dialog-variables"">
    <BitDialog @bind-IsOpen=""isOpenCssVariables""
               Title=""Re-skinned with variables""
               Subtitle=""Set once, on an ancestor""
               Message=""Every Dialog under the element that sets them takes them - no part of this one was restyled on its own."" />
</div>";
    private readonly string example20CsharpCode = @"
private bool isOpenStyles;
private bool isOpenClasses;
private bool isOpenCssVariables;";

    private readonly string example21RazorCode = @"
<style>
    .btn-container {
        gap: 1rem;
        display: flex;
        flex-flow: row wrap;
    }
</style>

<div class=""btn-container"" dir=""rtl"">
    <BitButton Dir=""BitDir.Rtl"" OnClick=""@(() => isOpenRtl = true)"">باز کردن پنجره پیام</BitButton>
    <BitButton Dir=""BitDir.Rtl"" OnClick=""@(() => isOpenRtlStart = true)"">TopStart</BitButton>
    <BitButton Dir=""BitDir.Rtl"" OnClick=""@(() => isOpenRtlLeft = true)"">TopLeft</BitButton>
</div>

<BitDialog @bind-IsOpen=""isOpenRtl""
           Dir=""BitDir.Rtl""
           Title=""بدون موضوع""
           OkText=""تایید""
           CancelText=""انصراف""
           CloseButtonTitle=""بستن""
           Message=""آیا می خواهید این پیام را بدون موضوع ارسال کنید؟"" />

<BitDialog @bind-IsOpen=""isOpenRtlStart""
           Dir=""BitDir.Rtl""
           Position=""BitDialogPosition.TopStart""
           Title=""TopStart""
           OkText=""تایید""
           CancelText=""انصراف""
           Message=""موقعیت منطقی: ابتدای جهت خواندن"" />

<BitDialog @bind-IsOpen=""isOpenRtlLeft""
           Dir=""BitDir.Rtl""
           Position=""BitDialogPosition.TopLeft""
           Title=""TopLeft""
           OkText=""تایید""
           CancelText=""انصراف""
           Message=""موقعیت فیزیکی: همیشه سمت چپ"" />";
    private readonly string example21CsharpCode = @"
private bool isOpenRtl;
private bool isOpenRtlStart;
private bool isOpenRtlLeft;";
}
