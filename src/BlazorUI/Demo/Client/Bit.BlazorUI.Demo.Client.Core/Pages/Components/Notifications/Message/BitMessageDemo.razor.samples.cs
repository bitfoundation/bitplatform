namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Notifications.Message;

public partial class BitMessageDemo
{
    private readonly string example1RazorCode = @"
<BitMessage>This is a Message.</BitMessage>";

    private readonly string example2RazorCode = @"
<BitMessage Variant=""BitVariant.Fill"">Fill</BitMessage>
<BitMessage Variant=""BitVariant.Outline"">Outline</BitMessage>
<BitMessage Variant=""BitVariant.Text"">Text</BitMessage>";

    private readonly string example3RazorCode = @"
<BitMessage Multiline>
    Fourteen of the 320 rows could not be imported because their Email column was empty or held a value
    the address parser did not recognize. Every other row was imported and is already visible in the
    contacts list. Download the report to see which rows were left out, fix them in place, and run the
    import again - the rows that went through the first time are skipped rather than duplicated.
</BitMessage>

<BitMessage Multiline MaxLines=""2"">
    Held to two lines by MaxLines. Your workspace is using 19.4 GB of its 20 GB. New uploads will start
    to fail once the limit is reached, and shared links to files already in the workspace will keep
    working. Remove the files you no longer need, or move them to an archive, to free the space back up.
</BitMessage>";

    private readonly string example4RazorCode = @"
<BitMessage Truncate @bind-Expanded=""isTruncateExpanded"">
    Bound through Expanded. Fourteen of the 320 rows could not be imported because their Email column
    was empty or held a value the address parser did not recognize. Every other row was imported and
    is already visible in the contacts list.
</BitMessage>
<BitToggle Label=""Expanded"" @bind-Value=""isTruncateExpanded"" />


<BitMessage @ref=""truncatedMessage"" Truncate>
    Driven by its methods. Fourteen of the 320 rows could not be imported because their Email column
    was empty or held a value the address parser did not recognize. Every other row was imported and
    is already visible in the contacts list.
</BitMessage>
<div>
    <BitButton OnClick=""() => truncatedMessage!.ExpandAsync()"">Expand</BitButton>
    <BitButton OnClick=""() => truncatedMessage!.CollapseAsync()"">Collapse</BitButton>
    <BitButton OnClick=""() => truncatedMessage!.ToggleExpandAsync()"">Toggle</BitButton>
</div>


<BitMessage Multiline Truncate MaxLines=""2"">
    Capped at two lines, then unfolded by the button. Your workspace is using 19.4 GB of its 20 GB. New
    uploads will start to fail once the limit is reached, and shared links to files already in the
    workspace will keep working. Remove the files you no longer need, or move them to an archive, to free
    the space back up.
</BitMessage>";
    private readonly string example4CsharpCode = @"
private bool isTruncateExpanded;
private BitMessage? truncatedMessage;";

    private readonly string example5RazorCode = @"
<BitMessage Title=""Heads up"">Your session expires in 5 minutes.</BitMessage>

<BitMessage Multiline Title=""Upload failed"">
    The file <b>report-2024.xlsx</b> could not be uploaded because it exceeds the 25 MB limit.
    Compress the file or split it into parts and try again.
</BitMessage>

<BitMessage Multiline>
    <TitleTemplate>
        Saved to <BitLink Href=""https://bitplatform.dev"">your workspace</BitLink>
    </TitleTemplate>
    <Content>
        Everyone with access to the workspace can see this version now.
    </Content>
</BitMessage>

<BitMessage Multiline TitleElement=""h3"" Title=""A title that is a heading"">
    Rendered as an h3, so it shows up in the heading list of a screen reader.
</BitMessage>";

    private readonly string example6RazorCode = @"
<BitMessage Alignment=""BitAlignment.Start"">Start</BitMessage>
<BitMessage Alignment=""BitAlignment.Center"">Center</BitMessage>
<BitMessage Alignment=""BitAlignment.End"">End</BitMessage>


<div class=""banner-host"">
    <BitMessage Square>Scheduled maintenance starts at 02:00 UTC. Save your work before then.</BitMessage>
    <div class=""banner-body"">The content of the page sits below the banner.</div>
</div>


<BitMessage Elevation=""(int)elevation"">Elevated message</BitMessage>
<BitSlider Label=""Elevation"" Min=""0"" Max=""24"" Step=""1"" @bind-Value=""elevation"" />

<style>
    .banner-host {
        overflow: hidden;
        border-radius: 0.25rem;
        border: 1px solid var(--bit-clr-brd-sec);
    }

    .banner-body {
        padding: 1rem;
    }
</style>";
    private readonly string example6CsharpCode = @"
private double elevation = 7;";

    private readonly string example7RazorCode = @"
<BitMessage Dismissible @bind-Dismissed=""isSelfDismissed"">
    <b>Dismissible</b> removes itself, no handler needed.
</BitMessage>
@if (isSelfDismissed)
{
    <BitButton OnClick=""() => isSelfDismissed = false"">Bring it back</BitButton>
}


@if (isDismissed is false)
{
    <BitMessage OnDismiss=""() => isDismissed = true"">
        <b>OnDismiss</b> reports the dismissal; its handler removes the message.
    </BitMessage>
}
else
{
    <BitButton OnClick=""() => isDismissed = false"">Bring it back</BitButton>
}


<BitMessage Dismissible
            DismissOnEscape
            OnDismissing=""HandleDismissing""
            @bind-Dismissed=""isGuardedDismissed"">
    Press the button, or focus it and press Escape: the first attempt is refused.
    Attempts: <b>@dismissAttempts</b>@(lastDismissReason is null ? """" : $"" (last reason: {lastDismissReason})"")
</BitMessage>
@if (isGuardedDismissed)
{
    <BitButton OnClick=""ResetGuardedMessage"">Bring it back</BitButton>
}


<BitMessage @ref=""dismissableMessage"" Dismissible @bind-Dismissed=""isMethodDismissed"">
    Dismissed from outside by <b>DismissAsync</b>.
</BitMessage>
@if (isMethodDismissed)
{
    <BitButton OnClick=""() => isMethodDismissed = false"">Bring it back</BitButton>
}
else
{
    <BitButton OnClick=""() => dismissableMessage!.DismissAsync()"">DismissAsync</BitButton>
}";
    private readonly string example7CsharpCode = @"
private bool isSelfDismissed;
private bool isDismissed;
private bool isMethodDismissed;
private BitMessage? dismissableMessage;

private int dismissAttempts;
private bool isGuardedDismissed;
private BitMessageDismissReason? lastDismissReason;

private void HandleDismissing(BitMessageDismissArgs args)
{
    dismissAttempts++;
    lastDismissReason = args.Reason;

    // The first attempt is refused; the next one goes through.
    args.Cancel = dismissAttempts < 2;
}

private void ResetGuardedMessage()
{
    dismissAttempts = 0;
    lastDismissReason = null;
    isGuardedDismissed = false;
}";

    private readonly string example8RazorCode = @"
@if (isAutoDismissed is false)
{
    <BitMessage AutoDismissTime=""TimeSpan.FromSeconds(5)"" OnDismiss=""() => isAutoDismissed = true"">
        Gone in five seconds. Hover me to hold the countdown.
    </BitMessage>
}
else
{
    <BitButton OnClick=""() => isAutoDismissed = false"">Restart</BitButton>
}


<BitMessage Dismissible
            ShowAutoDismissProgress
            AutoDismissTime=""TimeSpan.FromSeconds(10)""
            @bind-Dismissed=""isProgressDismissed"">
    Ten seconds, drawn along the bottom edge.
</BitMessage>
@if (isProgressDismissed)
{
    <BitButton OnClick=""() => isProgressDismissed = false"">Restart</BitButton>
}


<BitMessage @ref=""pausableMessage""
            ShowAutoDismissProgress
            AutoDismissTime=""TimeSpan.FromSeconds(15)""
            @bind-Dismissed=""isPausedDismissed"">
    Fifteen seconds and no button: a <b>Dismissed</b> binding alone counts down too.
</BitMessage>
@if (isPausedDismissed)
{
    <BitButton OnClick=""() => isPausedDismissed = false"">Restart</BitButton>
}
else
{
    <div>
        <BitButton OnClick=""() => pausableMessage!.PauseAutoDismiss()"">Pause</BitButton>
        <BitButton OnClick=""() => pausableMessage!.ResumeAutoDismiss()"">Resume</BitButton>
    </div>
}";
    private readonly string example8CsharpCode = @"
private bool isAutoDismissed;
private bool isProgressDismissed;
private bool isPausedDismissed;
private BitMessage? pausableMessage;";

    private readonly string example9RazorCode = @"
<BitMessage>
    <Content>A draft of this page was recovered from your last session.</Content>
    <Actions>
        <BitButton Size=""BitSize.Small"" Color=""BitColor.PrimaryBackground"">Restore</BitButton>
    </Actions>
</BitMessage>

<BitMessage Multiline Title=""Connection lost"" Color=""BitColor.Warning"" OnDismiss=""() => {}"">
    <Content>
        The last three changes could not be saved because the server could not be reached. They are
        kept on this device and will be sent as soon as the connection is back.
    </Content>
    <Actions>
        <BitButton Color=""BitColor.PrimaryBackground"">Retry now</BitButton>
        <BitButton Variant=""BitVariant.Text"" Color=""BitColor.PrimaryBackground"">Work offline</BitButton>
    </Actions>
</BitMessage>

<BitMessage Truncate OnDismiss=""() => {}"">
    <Content>
        One of three notices, with icon-only actions to step through them. The expander and the dismiss
        button stay beside the actions.
    </Content>
    <Actions>
        <BitButton AriaLabel=""Previous notice"" Variant=""BitVariant.Text"" Color=""BitColor.PrimaryBackground"" IconName=""@BitIconName.Up"" />
        <BitButton AriaLabel=""Next notice"" Variant=""BitVariant.Text"" Color=""BitColor.PrimaryBackground"" IconName=""@BitIconName.Down"" />
    </Actions>
</BitMessage>";

    private readonly string example10RazorCode = @"
<BitMessage IconName=""@BitIconName.CheckMark"">A custom icon.</BitMessage>

<BitMessage OnDismiss=""() => {}"" DismissIconName=""@BitIconName.Blocked2Solid"">A custom dismiss icon.</BitMessage>

<BitMessage Truncate ExpandIconName=""@BitIconName.ChevronDownEnd"" CollapseIconName=""@BitIconName.ChevronUpEnd"">
    Custom expand and collapse icons. Fourteen of the 320 rows could not be imported because their Email
    column was empty or held a value the address parser did not recognize.
</BitMessage>

<BitMessage>
    <IconTemplate>
        <BitSpinnerLoading CustomSize=""20"" CustomColor=""currentcolor"" />
    </IconTemplate>
    <Content>A spinner in place of the icon, through IconTemplate.</Content>
</BitMessage>

<BitMessage HideIcon>No icon.</BitMessage>";

    private readonly string example11RazorCode = @"
<BitMessage Color=""BitColor.Error"" IconAriaLabel=""Error"">
    The payment could not be authorized. Announced as ""Error: The payment could not be authorized.""
</BitMessage>

<BitMessage Color=""BitColor.Error"" Role=""status"">
    Error-colored but reporting no error: announced politely as a status.
</BitMessage>

<BitMessage Color=""BitColor.Error"" Politeness=""BitPoliteness.Polite"">
    Still an alert, but announced politely.
</BitMessage>

<BitMessage Color=""BitColor.Error"" Role=""none"">
    Part of the page rather than news about it: not announced at all.
</BitMessage>


<BitButton OnClick=""() => isDelayedDismissed = false"">Show a delayed-announcement message</BitButton>
<BitMessage DelayedAnnouncement Dismissible @bind-Dismissed=""isDelayedDismissed"">
    Announced reliably: the live region reached the page one render before this text.
</BitMessage>


<BitButton OnClick=""() => isAutoFocusDismissed = false"">Show an auto-focused message</BitButton>
<BitMessage AutoFocus
            Dismissible
            Title=""Action required""
            Color=""BitColor.SevereWarning""
            @bind-Dismissed=""isAutoFocusDismissed"">
    This message took the focus. Press Tab to reach its dismiss button.
</BitMessage>


<BitMessage @ref=""focusableMessage"" TabIndex=""0"">
    A message with a TabIndex can be focused on demand.
</BitMessage>
<BitButton OnClick=""() => focusableMessage!.FocusAsync()"">FocusAsync</BitButton>";
    private readonly string example11CsharpCode = @"
private bool isDelayedDismissed = true;
private bool isAutoFocusDismissed = true;
private BitMessage? focusableMessage;";

    private readonly string example12RazorCode = @"
<BitMessage IsEnabled=""isMessageEnabled""
            Truncate
            Dismissible
            DismissOnEscape
            ShowAutoDismissProgress
            Color=""BitColor.Warning""
            Title=""Licence expiring""
            AutoDismissTime=""TimeSpan.FromSeconds(10)""
            @bind-Dismissed=""isDisabledSampleDismissed"">
    Your licence runs out in 14 days. Renew it before then to keep the shared workspaces open to
    everyone who is using them today.
</BitMessage>
<BitToggle Label=""IsEnabled"" @bind-Value=""isMessageEnabled"" />
@if (isDisabledSampleDismissed)
{
    <BitButton OnClick=""() => isDisabledSampleDismissed = false"">Bring it back</BitButton>
}";
    private readonly string example12CsharpCode = @"
private bool isMessageEnabled = true;
private bool isDisabledSampleDismissed;";

    private readonly string example13RazorCode = @"
<BitMessage Color=""BitColor.Error""
            Title=""Payment failed""
            Style=""--bit-Message-background: color-mix(in srgb, var(--bit-clr-err) 12%, var(--bit-clr-bg-pri));
                   --bit-Message-border-color: var(--bit-clr-err);
                   --bit-Message-icon-color: var(--bit-clr-err);
                   --bit-Message-color: var(--bit-clr-fg-pri);"">
    A tinted surface: the card was declined. Try another payment method.
</BitMessage>

<BitMessage Color=""BitColor.Success""
            Style=""--bit-Message-background: var(--bit-clr-bg-sec);
                   --bit-Message-border-color: var(--bit-clr-suc);
                   --bit-Message-border-width: 0 0 0 4px;
                   --bit-Message-icon-color: var(--bit-clr-suc);
                   --bit-Message-color: var(--bit-clr-fg-pri);
                   --bit-Message-radius: 0;"">
    An accent bar: the report is ready to download.
</BitMessage>


<div style=""--bit-Message-radius: 1rem;
            --bit-Message-shadow: var(--bit-shd-4);
            --bit-Message-icon-size: 1.25rem;
            --bit-Message-title-font-weight: 700;
            --bit-Message-progress-height: 6px;"">
    <BitMessage Title=""Set on an ancestor"">Both messages in this box take its variables.</BitMessage>
    <BitMessage Dismissible
                ShowAutoDismissProgress
                Color=""BitColor.Success""
                AutoDismissTime=""TimeSpan.FromSeconds(20)""
                @bind-Dismissed=""isCssVarsDismissed"">
        Rounded, shadowed, with a thicker countdown.
    </BitMessage>
    @if (isCssVarsDismissed)
    {
        <BitButton OnClick=""() => isCssVarsDismissed = false"">Restart</BitButton>
    }
</div>";
    private readonly string example13CsharpCode = @"
private bool isCssVarsDismissed;";

    private readonly string example14RazorCode = @"
<BitParams Parameters=""@messageParams"">
    <BitMessage Color=""BitColor.Success"">Your changes were saved.</BitMessage>
    <BitMessage Color=""BitColor.Warning"">
        Truncated by the cascade. Your workspace is using 19.4 GB of its 20 GB, and new uploads will
        start to fail once the limit is reached.
    </BitMessage>
    <BitMessage Color=""BitColor.Error"" Variant=""BitVariant.Fill"">Keeps its own Fill variant.</BitMessage>
</BitParams>";
    private readonly string example14CsharpCode = @"
private readonly BitMessageParams[] messageParams =
[
    new()
    {
        Variant = BitVariant.Outline,
        Size = BitSize.Small,
        Truncate = true,
    }
];";

    private readonly string example15RazorCode = @"
<BitMessage Color=""BitColor.Primary"">Primary.</BitMessage>
<BitMessage Color=""BitColor.Secondary"">Secondary.</BitMessage>
<BitMessage Color=""BitColor.Tertiary"">Tertiary.</BitMessage>
<BitMessage Color=""BitColor.Info"">Info (default).</BitMessage>
<BitMessage Color=""BitColor.Success"">Success.</BitMessage>
<BitMessage Color=""BitColor.Warning"">Warning.</BitMessage>
<BitMessage Color=""BitColor.SevereWarning"">SevereWarning.</BitMessage>
<BitMessage Color=""BitColor.Error"">Error.</BitMessage>

<div style=""background:var(--bit-clr-fg-sec);padding:1rem;margin:1rem 0;"">
    <BitMessage Color=""BitColor.PrimaryBackground"">PrimaryBackground.</BitMessage>
    <BitMessage Color=""BitColor.SecondaryBackground"">SecondaryBackground.</BitMessage>
    <BitMessage Color=""BitColor.TertiaryBackground"">TertiaryBackground.</BitMessage>
</div>

<BitMessage Color=""BitColor.PrimaryForeground"">PrimaryForeground.</BitMessage>
<BitMessage Color=""BitColor.SecondaryForeground"">SecondaryForeground.</BitMessage>
<BitMessage Color=""BitColor.TertiaryForeground"">TertiaryForeground.</BitMessage>
<BitMessage Color=""BitColor.PrimaryBorder"">PrimaryBorder.</BitMessage>
<BitMessage Color=""BitColor.SecondaryBorder"">SecondaryBorder.</BitMessage>
<BitMessage Color=""BitColor.TertiaryBorder"">TertiaryBorder.</BitMessage>";

    private readonly string example16RazorCode = @"
<link rel=""stylesheet"" href=""https://cdnjs.cloudflare.com/ajax/libs/font-awesome/7.0.1/css/all.min.css"" />
<link rel=""stylesheet"" href=""https://cdn.jsdelivr.net/npm/bootstrap-icons@1.11.3/font/bootstrap-icons.min.css"" />

<BitMessage Color=""BitColor.Info"" Icon=""@(""fa-solid fa-circle-info"")"">
    FontAwesome icon (Icon=""fa-solid fa-circle-info"")
</BitMessage>

<BitMessage Color=""BitColor.Warning"" OnDismiss=""() => {}"" DismissIcon=""@BitIconInfo.Fa(""solid xmark"")"">
    FontAwesome dismiss icon (DismissIcon=""BitIconInfo.Fa(""solid xmark"")"")
</BitMessage>

<BitMessage Color=""BitColor.Success"" Icon=""@BitIconInfo.Bi(""check-circle-fill"")"">
    Bootstrap icon (Icon=""BitIconInfo.Bi(""check-circle-fill"")"")
</BitMessage>

<BitMessage Truncate
            Color=""BitColor.Error""
            Icon=""@BitIconInfo.Css(""bi bi-x-octagon-fill"")""
            ExpandIcon=""@BitIconInfo.Bi(""chevron-double-down"")""
            CollapseIcon=""@BitIconInfo.Bi(""chevron-double-up"")"">
    Bootstrap expand and collapse icons. Fourteen of the 320 rows could not be imported because their
    Email column was empty or held a value the address parser did not recognize.
</BitMessage>";

    private readonly string example17RazorCode = @"
<BitMessage Size=""BitSize.Small"" OnDismiss=""() => {}"">Small</BitMessage>
<BitMessage Size=""BitSize.Medium"" OnDismiss=""() => {}"">Medium</BitMessage>
<BitMessage Size=""BitSize.Large"" OnDismiss=""() => {}"">Large</BitMessage>";

    private readonly string example18RazorCode = @"
<style>
    .custom-class {
        padding: 1rem;
        font-size: 1rem;
        font-style: italic;
    }

    .custom-icon {
        font-size: 2rem;
    }

    .custom-content {
        font-size: 1.5rem;
    }

    .custom-expander-icon {
        margin: 0.5rem;
        font-size: 2rem;
    }

    .custom-dismiss-icon {
        margin: 0.5rem;
        font-size: 2rem;
    }
</style>


<BitMessage Style=""padding: 8px; font-style: italic;"">Styled message.</BitMessage>

<BitMessage Class=""custom-class"" Color=""BitColor.Success"">Classed message.</BitMessage>

<BitMessage Multiline
            Title=""Styled title""
            OnDismiss=""() => {}""
            Color=""BitColor.Warning""
            Styles=""@(new() { Root = ""padding: 1rem;"",
                              Title = ""color: darkred;"",
                              Content = ""color: blueviolet;"",
                              DismissIcon = ""font-size: 1rem;"",
                              Actions = ""justify-content: center; gap: 1rem;"" })"">
    <Content>Styles for the root, the title, the content, the dismiss icon and the actions.</Content>
    <Actions>
        <BitButton Variant=""BitVariant.Text"" Color=""BitColor.PrimaryBackground"">Ok</BitButton>
        <BitButton Variant=""BitVariant.Text"" Color=""BitColor.PrimaryBackground"">Cancel</BitButton>
    </Actions>
</BitMessage>

<BitMessage Truncate
            OnDismiss=""() => {}""
            Color=""BitColor.SevereWarning""
            Classes=""@(new() { Icon = ""custom-icon"",
                               Content = ""custom-content"",
                               ExpanderIcon = ""custom-expander-icon"",
                               DismissIcon = ""custom-dismiss-icon"" })"">
    Classes for the icon, the content, the expander icon and the dismiss icon.
</BitMessage>";

    private readonly string example19RazorCode = @"
<BitMessage Dir=""BitDir.Rtl"" Color=""BitColor.Info"">
    پیام خبری (پیش فرض). <BitLink Href=""https://bitplatform.dev"">به وبسایت ما سر بزنید.</BitLink>
</BitMessage>

<BitMessage Dir=""BitDir.Rtl"" Color=""BitColor.Success"" Truncate OnDismiss=""() => {}"">
    پیام موفق. <BitLink Href=""https://bitplatform.dev"">به وبسایت ما سر بزنید.</BitLink>
    لورم ایپسوم متن ساختگی با تولید سادگی نامفهوم از صنعت چاپ و با استفاده از طراحان گرافیک است.
    چاپگرها و متون بلکه روزنامه و مجله در ستون و سطرآنچنان که لازم است.
</BitMessage>

<BitMessage Dir=""BitDir.Rtl"" Color=""BitColor.Warning"" Multiline OnDismiss=""() => {}"" Title=""پیام هشدار"">
    لورم ایپسوم متن ساختگی با تولید سادگی نامفهوم از صنعت چاپ و با استفاده از طراحان گرافیک است.
    چاپگرها و متون بلکه روزنامه و مجله در ستون و سطرآنچنان که لازم است.
</BitMessage>

<BitMessage Dir=""BitDir.Rtl"" Color=""BitColor.Error"">
    پیام خطا. <BitLink Href=""https://bitplatform.dev"">به وبسایت ما سر بزنید.</BitLink>
</BitMessage>";
}
