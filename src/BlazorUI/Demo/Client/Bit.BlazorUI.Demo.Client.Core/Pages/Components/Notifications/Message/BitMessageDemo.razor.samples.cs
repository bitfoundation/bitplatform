namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Notifications.Message;

public partial class BitMessageDemo
{
    private readonly string example1RazorCode = @"
<BitMessage>This is a Message.</BitMessage>";

    private readonly string example2RazorCode = @"
<BitMessage Title=""Heads up"">Your session expires in 5 minutes.</BitMessage>

<BitMessage Multiline Title=""Upload failed"" OnDismiss=""() => {}"">
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
    Rendered as an <b>h3</b>, so it shows up in the heading list of a screen reader.
</BitMessage>";

    private readonly string example3RazorCode = @"
<BitMessage Variant=""BitVariant.Fill"">Filled</BitMessage>

<BitMessage Variant=""BitVariant.Outline"">Outlined</BitMessage>
<BitMessage Variant=""BitVariant.Text"">Texted</BitMessage>";

    private readonly string example4RazorCode = @"
<BitMessage Alignment=""BitAlignment.Start"">Start</BitMessage>
<BitMessage Alignment=""BitAlignment.Center"">Center</BitMessage>
<BitMessage Alignment=""BitAlignment.End"">End</BitMessage>";

    private readonly string example5RazorCode = @"
<BitMessage Elevation=""(int)elevation"">Elevated Message</BitMessage>

<BitSlider Label=""Elevation"" Min=""0"" Max=""24"" Step=""1"" @bind-Value=""elevation"" />";
    private readonly string example5CsharpCode = @"
private double elevation = 7;";

    private readonly string example6RazorCode = @"
<BitMessage Multiline>
    In the beginning, there is silence-a blank canvas yearning to be filled, a quiet space where creativity waits
    to awaken. These words are temporary, standing in place of ideas yet to come, a glimpse into the infinite
    possibilities that lie ahead. Think of this text as a bridge, connecting the empty spaces of now with the
    vibrant narratives of tomorrow. It whispers of the stories waiting to be told, of the thoughts yet to be
    shaped into meaning, and the emotions ready to resonate with every reader.
    <br />
    In this space, potential reigns supreme. It is a moment suspended in time, where imagination dances freely and
    each word has the power to transform into something extraordinary. Here lies the start of something new, an
    opportunity to craft, inspire, and create. Whether it's a tale of adventure, a reflection of truth, or an
    idea that sparks change, these lines are yours to fill, to shape, and to make uniquely yours. The journey
    begins here, in this quiet moment where everything is possible.
</BitMessage>

<BitMessage Multiline MaxLines=""2"" Title=""Storage almost full"">
    Your workspace is using 19.4 GB of its 20 GB. New uploads will start to fail once the limit is
    reached, and shared links to files already in the workspace will keep working. Remove the files you
    no longer need, or move them to an archive, to free the space back up.
</BitMessage>

<BitMessage Multiline Truncate MaxLines=""2"" Title=""Import failed"">
    Fourteen of the 320 rows could not be imported because their <b>Email</b> column was empty or held a
    value the address parser did not recognize. Every other row was imported and is already visible in
    the contacts list. Download the report to see which rows were left out, fix them in place, and run
    the import again - the rows that went through the first time are skipped rather than duplicated.
</BitMessage>";

    private readonly string example7RazorCode = @"
<BitMessage Truncate @bind-Expanded=""isTruncateExpanded"">
    In the beginning, there is silence-a blank canvas yearning to be filled, a quiet space where creativity waits
    to awaken. These words are temporary, standing in place of ideas yet to come, a glimpse into the infinite
    possibilities that lie ahead. Think of this text as a bridge, connecting the empty spaces of now with the
    vibrant narratives of tomorrow. It whispers of the stories waiting to be told, of the thoughts yet to be
    shaped into meaning, and the emotions ready to resonate with every reader.
    <br />
    In this space, potential reigns supreme. It is a moment suspended in time, where imagination dances freely and
    each word has the power to transform into something extraordinary. Here lies the start of something new-an
    opportunity to craft, inspire, and create. Whether it's a tale of adventure, a reflection of truth, or an
    idea that sparks change, these lines are yours to fill, to shape, and to make uniquely yours. The journey
    begins here, in this quiet moment where everything is possible.
</BitMessage>

<BitToggle Label=""Expanded"" @bind-Value=""isTruncateExpanded"" />

<BitMessage @ref=""truncatedMessage"" Truncate Color=""BitColor.Info"">
    Driven by its methods rather than by a binding. In the beginning, there is silence-a blank canvas
    yearning to be filled, a quiet space where creativity waits to awaken. These words are temporary,
    standing in place of ideas yet to come, a glimpse into the infinite possibilities that lie ahead.
</BitMessage>

<BitButton OnClick=""() => truncatedMessage!.ExpandAsync()"">Expand</BitButton>
<BitButton OnClick=""() => truncatedMessage!.CollapseAsync()"">Collapse</BitButton>
<BitButton OnClick=""() => truncatedMessage!.ToggleExpandAsync()"">Toggle</BitButton>";
    private readonly string example7CsharpCode = @"
private bool isTruncateExpanded;
private BitMessage? truncatedMessage;";

    private readonly string example8RazorCode = @"
<BitMessage Dismissible @bind-Dismissed=""isSelfDismissed"">
    Self-dismissing message: <strong>Dismissible</strong> needs no handler to disappear.
</BitMessage>

@if (isSelfDismissed)
{
    <BitButton OnClick=""() => isSelfDismissed = false"">Dismissed, click to bring it back</BitButton>
}

@if (isDismissed is false)
{
    <BitMessage OnDismiss=""() => isDismissed = true"">
        Dismiss option enabled by adding <strong>OnDismiss</strong> parameter.
    </BitMessage>
}
else
{
    <BitButton OnClick=""() => isDismissed = false"">Dismissed, click to reset</BitButton>
}

@if (isEscapeDismissed is false)
{
    <BitMessage DismissOnEscape TabIndex=""0"" OnDismiss=""() => isEscapeDismissed = true"">
        Focus me (or the dismiss button) and press <strong>Escape</strong> to dismiss.
    </BitMessage>
}
else
{
    <BitButton OnClick=""() => isEscapeDismissed = false"">Dismissed by Escape, click to reset</BitButton>
}

<BitMessage Dismissible
            DismissOnEscape
            OnDismissing=""HandleDismissing""
            @bind-Dismissed=""isGuardedDismissed"">
    Guarded by <strong>OnDismissing</strong>: the first attempt is refused, the second one goes through.
    Attempts so far: <strong>@dismissAttempts</strong>@(lastDismissReason is null ? """" : $"" (last reason: {lastDismissReason})"").
</BitMessage>

@if (isGuardedDismissed)
{
    <BitButton OnClick=""ResetGuardedMessage"">Dismissed, click to reset</BitButton>
}

<BitMessage @ref=""dismissableMessage"" Dismissible @bind-Dismissed=""isMethodDismissed"">
    Dismissed from the outside by <strong>DismissAsync</strong>, which goes through
    <strong>OnDismissing</strong> and stops the countdown just as the button does.
</BitMessage>

@if (isMethodDismissed)
{
    <BitButton OnClick=""() => isMethodDismissed = false"">Dismissed, click to bring it back</BitButton>
}
else
{
    <BitButton OnClick=""() => dismissableMessage!.DismissAsync()"">Dismiss the message above</BitButton>
}";
    private readonly string example8CsharpCode = @"
private bool isDismissed;
private bool isSelfDismissed;
private bool isEscapeDismissed;
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

    private readonly string example9RazorCode = @"
@if (isAutoDismissed is false)
{
    <BitMessage AutoDismissTime=""TimeSpan.FromSeconds(5)"" OnDismiss=""() => isAutoDismissed = true"">
        Auto-Dismiss option enabled by adding the <strong>AutoDismissTime</strong> parameter alongside OnDismiss.
        Hover me to hold the countdown.
    </BitMessage>
}
else
{
    <BitButton OnClick=""() => isAutoDismissed = false"">Auto-Dismissed, click to reset</BitButton>
}

<BitMessage Dismissible
            ShowAutoDismissProgress
            AutoDismissTime=""TimeSpan.FromSeconds(10)""
            @bind-Dismissed=""isProgressDismissed"">
    Ten seconds, drawn along the bottom edge. Hover me and the bar stops with the countdown.
</BitMessage>

@if (isProgressDismissed)
{
    <BitButton OnClick=""() => isProgressDismissed = false"">Auto-Dismissed, click to restart</BitButton>
}

<BitMessage @ref=""pausableMessage""
            ShowAutoDismissProgress
            AutoDismissTime=""TimeSpan.FromSeconds(15)""
            @bind-Dismissed=""isPausedDismissed"">
    Fifteen seconds, and no dismiss button: a message bound through <strong>Dismissed</strong> alone
    counts down all the same. Hold it from outside with the buttons below.
</BitMessage>

@if (isPausedDismissed)
{
    <BitButton OnClick=""() => isPausedDismissed = false"">Auto-Dismissed, click to restart</BitButton>
}
else
{
    <BitButton OnClick=""() => pausableMessage!.PauseAutoDismiss()"">Pause</BitButton>
    <BitButton OnClick=""() => pausableMessage!.ResumeAutoDismiss()"">Resume</BitButton>
}";
    private readonly string example9CsharpCode = @"
private bool isAutoDismissed;
private bool isProgressDismissed;
private bool isPausedDismissed;
private BitMessage? pausableMessage;";

    private readonly string example10RazorCode = @"
<BitMessage>
    <Content>
        A draft of this page was recovered from your last session.
    </Content>
    <Actions>
        <BitButton Size=""BitSize.Small"" Color=""BitColor.PrimaryBackground"">Restore</BitButton>
    </Actions>
</BitMessage>

<BitMessage Multiline Title=""Connection lost"" OnDismiss=""() => {}"">
    <Content>
        The last three changes could not be saved because the server could not be reached. They are
        kept on this device and will be sent as soon as the connection is back.
    </Content>
    <Actions>
        <BitButton Color=""BitColor.PrimaryBackground"">Retry now</BitButton>
        <BitButton Variant=""BitVariant.Text"" Color=""BitColor.PrimaryBackground"">Work offline</BitButton>
    </Actions>
</BitMessage>

<BitMessage>
    <Content>
        Message with single line and icon-only action buttons.
    </Content>
    <Actions>
        <BitButton AriaLabel=""Previous message"" Variant=""BitVariant.Text"" Color=""BitColor.Tertiary"" IconName=""@BitIconName.Up"" />
        <BitButton AriaLabel=""Next message"" Variant=""BitVariant.Text"" Color=""BitColor.Tertiary"" IconName=""@BitIconName.Down"" />
    </Actions>
</BitMessage>";

    private readonly string example11RazorCode = @"
<BitMessage HideIcon>Icon hidden</BitMessage>";

    private readonly string example12RazorCode = @"
<BitMessage IconName=""@BitIconName.CheckMark"">
    Message with a custom icon.
</BitMessage>

<BitMessage OnDismiss=""() => {}"" DismissIconName=""@BitIconName.Blocked2Solid"">
    Message with a custom dismiss icon.
</BitMessage>

<BitMessage>
    <IconTemplate>
        <BitSpinnerLoading CustomSize=""20"" CustomColor=""currentcolor"" />
    </IconTemplate>
    <Content>
        Message with a spinner in place of its icon, through <strong>IconTemplate</strong>.
    </Content>
</BitMessage>

<BitMessage Truncate ExpandIconName=""@BitIconName.ChevronDownEnd"" CollapseIconName=""@BitIconName.ChevronUpEnd"">
    In the beginning, there is silence-a blank canvas yearning to be filled, a quiet space where creativity waits
    to awaken. These words are temporary, standing in place of ideas yet to come, a glimpse into the infinite
    possibilities that lie ahead. Think of this text as a bridge, connecting the empty spaces of now with the
    vibrant narratives of tomorrow. It whispers of the stories waiting to be told, of the thoughts yet to be
    shaped into meaning, and the emotions ready to resonate with every reader.
    <br />
    In this space, potential reigns supreme. It is a moment suspended in time, where imagination dances freely and
    each word has the power to transform into something extraordinary. Here lies the start of something new-an
    opportunity to craft, inspire, and create. Whether it's a tale of adventure, a reflection of truth, or an
    idea that sparks change, these lines are yours to fill, to shape, and to make uniquely yours. The journey
    begins here, in this quiet moment where everything is possible.
</BitMessage>";

    private readonly string example13RazorCode = @"
<div style=""overflow:hidden;border-radius:0.25rem;border:1px solid var(--bit-clr-brd-sec)"">
    <BitMessage Square OnDismiss=""() => {}"">
        Scheduled maintenance starts at 02:00 UTC. Save your work before then.
    </BitMessage>
    <div style=""padding:1rem"">The content of the page sits below the banner.</div>
</div>";

    private readonly string example14RazorCode = @"
@if (isWarningDismissed is false)
{
    <BitMessage Truncate OnDismiss=""() => isWarningDismissed = true"">
        <Content>
            In the beginning, there is silence-a blank canvas yearning to be filled, a quiet space where creativity waits
            to awaken. These words are temporary, standing in place of ideas yet to come, a glimpse into the infinite
            possibilities that lie ahead. Think of this text as a bridge, connecting the empty spaces of now with the
            vibrant narratives of tomorrow. It whispers of the stories waiting to be told, of the thoughts yet to be
            shaped into meaning, and the emotions ready to resonate with every reader.
            <br />
            In this space, potential reigns supreme. It is a moment suspended in time, where imagination dances freely and
            each word has the power to transform into something extraordinary. Here lies the start of something new-an
            opportunity to craft, inspire, and create. Whether it's a tale of adventure, a reflection of truth, or an
            idea that sparks change, these lines are yours to fill, to shape, and to make uniquely yours. The journey
            begins here, in this quiet moment where everything is possible.
        </Content>
        <Actions>
            <BitButton Size=""BitSize.Small"" Color=""BitColor.PrimaryBackground"">Retry</BitButton>
            <BitButton Size=""BitSize.Small"" Color=""BitColor.PrimaryBackground"">Details</BitButton>
        </Actions>
    </BitMessage>
}
else
{
    <BitButton OnClick=""() => isWarningDismissed = false"">Dismissed, click to reset</BitButton>
}

@if (isErrorDismissed is false)
{
    <BitMessage Multiline Title=""Something went wrong"" OnDismiss=""() => isErrorDismissed = true"">
        <Content>
            In the beginning, there is silence-a blank canvas yearning to be filled, a quiet space where creativity waits
            to awaken. These words are temporary, standing in place of ideas yet to come, a glimpse into the infinite
            possibilities that lie ahead. Think of this text as a bridge, connecting the empty spaces of now with the
            vibrant narratives of tomorrow. It whispers of the stories waiting to be told, of the thoughts yet to be
            shaped into meaning, and the emotions ready to resonate with every reader.
            <br />
            In this space, potential reigns supreme. It is a moment suspended in time, where imagination dances freely and
            each word has the power to transform into something extraordinary. Here lies the start of something new-an
            opportunity to craft, inspire, and create. Whether it's a tale of adventure, a reflection of truth, or an
            idea that sparks change, these lines are yours to fill, to shape, and to make uniquely yours. The journey
            begins here, in this quiet moment where everything is possible.
        </Content>
        <Actions>
            <BitButton Color=""BitColor.PrimaryBackground"">Yes</BitButton>
            &nbsp;
            <BitButton Color=""BitColor.PrimaryBackground"">No</BitButton>
        </Actions>
    </BitMessage>
}
else
{
    <BitButton OnClick=""() => isErrorDismissed = false"">Dismissed, click to reset</BitButton>
}";
    private readonly string example14CsharpCode = @"
private bool isErrorDismissed;
private bool isWarningDismissed;";

    private readonly string example15RazorCode = @"
<BitMessage Color=""BitColor.Error"" IconAriaLabel=""Error"">
    The payment could not be authorized. Announced as ""Error: The payment could not be authorized.""
</BitMessage>

<BitMessage Color=""BitColor.Error"" Role=""status"">
    An error-colored message that reports no error, announced politely as a status.
</BitMessage>

<BitMessage Color=""BitColor.Error"" Role=""none"">
    Part of the page rather than news about it: not announced at all.
</BitMessage>

<BitMessage Color=""BitColor.Error"" Politeness=""BitPoliteness.Polite"">
    An error worth reporting but not worth interrupting for: still an <b>alert</b>, announced politely.
</BitMessage>

<BitMessage Color=""BitColor.Success"" Politeness=""BitPoliteness.Assertive"">
    A success the reader has to hear about at once: still a <b>status</b>, announced assertively.
</BitMessage>

<BitButton OnClick=""() => isDelayedDismissed = false"">Show a delayed-announcement message</BitButton>

<BitMessage DelayedAnnouncement Dismissible @bind-Dismissed=""isDelayedDismissed"">
    Announced reliably: the live region reached the page one render before this text landed in it.
</BitMessage>";
    private readonly string example15CsharpCode = @"
private bool isDelayedDismissed = true;";

    private readonly string example16RazorCode = @"
<BitButton OnClick=""() => isAutoFocusDismissed = false"">Show an auto-focused message</BitButton>

<BitMessage AutoFocus
            Dismissible
            Color=""BitColor.SevereWarning""
            @bind-Dismissed=""isAutoFocusDismissed"">
    This message took the focus when it appeared. Press <strong>Tab</strong> to reach its dismiss button.
</BitMessage>

<BitMessage @ref=""focusableMessage"" TabIndex=""0"" Color=""BitColor.Info"">
    A message with a <strong>TabIndex</strong> can be focused on demand.
</BitMessage>

<BitButton OnClick=""() => focusableMessage!.FocusAsync()"">Focus the message above</BitButton>";
    private readonly string example16CsharpCode = @"
private BitMessage? focusableMessage;
private bool isAutoFocusDismissed = true;";

    private readonly string example17RazorCode = @"
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
    <BitButton OnClick=""() => isDisabledSampleDismissed = false"">Dismissed, click to bring it back</BitButton>
}";
    private readonly string example17CsharpCode = @"
private bool isMessageEnabled = true;
private bool isDisabledSampleDismissed;";

    private readonly string example18RazorCode = @"
<BitMessage Color=""BitColor.Primary"">Primary.</BitMessage>
<BitMessage Color=""BitColor.Secondary"">Secondary.</BitMessage>
<BitMessage Color=""BitColor.Tertiary"">Tertiary.</BitMessage>
<BitMessage Color=""BitColor.Info"">Info (default).</BitMessage>
<BitMessage Color=""BitColor.Success"">Success.</BitMessage>
<BitMessage Color=""BitColor.Warning"">Warning.</BitMessage>
<BitMessage Color=""BitColor.SevereWarning"">SevereWarning.</BitMessage>
<BitMessage Color=""BitColor.Error"">Error.</BitMessage>

<div style=""background:var(--bit-clr-fg-sec);padding:1rem"">
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

    private readonly string example19RazorCode = @"
<link rel=""stylesheet"" href=""https://cdnjs.cloudflare.com/ajax/libs/font-awesome/7.0.1/css/all.min.css"" />

<BitMessage Color=""BitColor.Info"" Icon=""@(""fa-solid fa-circle-info"")"">
    FontAwesome message
</BitMessage>

<BitMessage Color=""BitColor.Success"" Icon=""@BitIconInfo.Css(""fa-solid fa-circle-check"")"">
    FontAwesome success
</BitMessage>

<BitMessage Color=""BitColor.Warning"" OnDismiss=""() => {}""
            DismissIcon=""@BitIconInfo.Fa(""solid xmark"")"">
    FontAwesome dismiss icon
</BitMessage>

<link rel=""stylesheet"" href=""https://cdn.jsdelivr.net/npm/bootstrap-icons@1.11.3/font/bootstrap-icons.min.css"" />

<BitMessage Color=""BitColor.Info"" Icon=""@BitIconInfo.Css(""bi bi-info-circle-fill"")"">
    Bootstrap message
</BitMessage>

<BitMessage Color=""BitColor.Success"" Icon=""@BitIconInfo.Bi(""check-circle-fill"")"">
    Bootstrap success
</BitMessage>

<BitMessage Truncate Color=""BitColor.Warning""
            ExpandIcon=""@BitIconInfo.Bi(""chevron-double-down"")""
            CollapseIcon=""@BitIconInfo.Bi(""chevron-double-up"")"">
    In the beginning, there is silence-a blank canvas yearning to be filled, a quiet space where creativity waits
    to awaken. These words are temporary, standing in place of ideas yet to come, a glimpse into the infinite
    possibilities that lie ahead. Think of this text as a bridge, connecting the empty spaces of now with the
    vibrant narratives of tomorrow. It whispers of the stories waiting to be told, of the thoughts yet to be
    shaped into meaning, and the emotions ready to resonate with every reader.
    <br />
    In this space, potential reigns supreme. It is a moment suspended in time, where imagination dances freely and
    each word has the power to transform into something extraordinary. Here lies the start of something new-an
    opportunity to craft, inspire, and create. Whether it's a tale of adventure, a reflection of truth, or an
    idea that sparks change, these lines are yours to fill, to shape, and to make uniquely yours. The journey
    begins here, in this quiet moment where everything is possible.
</BitMessage>";

    private readonly string example20RazorCode = @"
<BitMessage Size=""BitSize.Small"" Color=""BitColor.Primary"">Small</BitMessage>
<BitMessage Size=""BitSize.Medium"" Color=""BitColor.Secondary"">Medium</BitMessage>
<BitMessage Size=""BitSize.Large"" Color=""BitColor.Tertiary"">Large</BitMessage>";

    private readonly string example21RazorCode = @"
<style>
    .custom-class {
        padding: 1rem;
        color: deeppink;
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


<BitMessage Multiline
            OnDismiss=""() => {}""
            Color=""BitColor.Info""
            Style=""padding:8px;color:red;"">
    <b>Styled Message.</b>
    In the beginning, there is silence-a blank canvas yearning to be filled, a quiet space where creativity waits
    to awaken. These words are temporary, standing in place of ideas yet to come, a glimpse into the infinite
    possibilities that lie ahead. Think of this text as a bridge, connecting the empty spaces of now with the
    vibrant narratives of tomorrow. It whispers of the stories waiting to be told, of the thoughts yet to be
    shaped into meaning, and the emotions ready to resonate with every reader.
    <br />
    In this space, potential reigns supreme. It is a moment suspended in time, where imagination dances freely and
    each word has the power to transform into something extraordinary. Here lies the start of something new-an
    opportunity to craft, inspire, and create. Whether it's a tale of adventure, a reflection of truth, or an
    idea that sparks change, these lines are yours to fill, to shape, and to make uniquely yours. The journey
    begins here, in this quiet moment where everything is possible.
</BitMessage>

<BitMessage Truncate
            Class=""custom-class""
            Color=""BitColor.Success"">
    <b>Classed Message.</b>
    In the beginning, there is silence-a blank canvas yearning to be filled, a quiet space where creativity waits
    to awaken. These words are temporary, standing in place of ideas yet to come, a glimpse into the infinite
    possibilities that lie ahead. Think of this text as a bridge, connecting the empty spaces of now with the
    vibrant narratives of tomorrow. It whispers of the stories waiting to be told, of the thoughts yet to be
    shaped into meaning, and the emotions ready to resonate with every reader.
    <br />
    In this space, potential reigns supreme. It is a moment suspended in time, where imagination dances freely and
    each word has the power to transform into something extraordinary. Here lies the start of something new-an
    opportunity to craft, inspire, and create. Whether it's a tale of adventure, a reflection of truth, or an
    idea that sparks change, these lines are yours to fill, to shape, and to make uniquely yours. The journey
    begins here, in this quiet moment where everything is possible.
</BitMessage>


<BitMessage Multiline
            Title=""Styled title""
            OnDismiss=""() => {}""
            Color=""BitColor.Warning""
            Styles=""@(new() { Root=""padding:1rem"",
                              IconContainer=""line-height:1.25"",
                              Title=""color:darkred"",
                              Content=""color:blueviolet"",
                              ContentContainer=""margin:0 10px"",
                              DismissIcon=""font-size:1rem"",
                              Actions=""justify-content:center;gap:1rem"" })"">
    <Content>
        <b>Styles.</b>
        In the beginning, there is silence-a blank canvas yearning to be filled, a quiet space where creativity waits
        to awaken. These words are temporary, standing in place of ideas yet to come, a glimpse into the infinite
        possibilities that lie ahead. Think of this text as a bridge, connecting the empty spaces of now with the
        vibrant narratives of tomorrow. It whispers of the stories waiting to be told, of the thoughts yet to be
        shaped into meaning, and the emotions ready to resonate with every reader.
        <br />
        In this space, potential reigns supreme. It is a moment suspended in time, where imagination dances freely and
        each word has the power to transform into something extraordinary. Here lies the start of something new-an
        opportunity to craft, inspire, and create. Whether it's a tale of adventure, a reflection of truth, or an
        idea that sparks change, these lines are yours to fill, to shape, and to make uniquely yours. The journey
        begins here, in this quiet moment where everything is possible.
    </Content>
    <Actions>
        <BitButton Variant=""BitVariant.Text"">Ok</BitButton>
        <BitButton Variant=""BitVariant.Text"">Cancel</BitButton>
    </Actions>
</BitMessage>

<BitMessage Truncate
            OnDismiss=""() => {}""
            Color=""BitColor.SevereWarning""
            Classes=""@(new() { Icon=""custom-icon"",
                               Content=""custom-content"",
                               ExpanderIcon=""custom-expander-icon"",
                               DismissIcon=""custom-dismiss-icon"" })"">
    <b>Classes.</b>
    In the beginning, there is silence-a blank canvas yearning to be filled, a quiet space where creativity waits
    to awaken. These words are temporary, standing in place of ideas yet to come, a glimpse into the infinite
    possibilities that lie ahead. Think of this text as a bridge, connecting the empty spaces of now with the
    vibrant narratives of tomorrow. It whispers of the stories waiting to be told, of the thoughts yet to be
    shaped into meaning, and the emotions ready to resonate with every reader.
    <br />
    In this space, potential reigns supreme. It is a moment suspended in time, where imagination dances freely and
    each word has the power to transform into something extraordinary. Here lies the start of something new-an
    opportunity to craft, inspire, and create. Whether it's a tale of adventure, a reflection of truth, or an
    idea that sparks change, these lines are yours to fill, to shape, and to make uniquely yours. The journey
    begins here, in this quiet moment where everything is possible.
</BitMessage>";

    private readonly string example22RazorCode = @"
<BitMessage Dir=""BitDir.Rtl"" Color=""BitColor.Info"">
    پیام خبری (پیش فرض). <BitLink Href=""https://bitplatform.dev"">به وبسایت ما سر بزنید.</BitLink>
</BitMessage>

<BitMessage Dir=""BitDir.Rtl"" Color=""BitColor.Success"" Truncate OnDismiss=""() => {}"">
    پیام موفق. <BitLink Href=""https://bitplatform.dev"">به وبسایت ما سر بزنید.</BitLink>
    لورم ایپسوم متن ساختگی با تولید سادگی نامفهوم از صنعت چاپ و با استفاده از طراحان گرافیک است.
    چاپگرها و متون بلکه روزنامه و مجله در ستون و سطرآنچنان که لازم است
    و برای شرایط فعلی تکنولوژی مورد نیاز و کاربردهای متنوع با هدف بهبود ابزارهای کاربردی می باشد.
    کتابهای زیادی در شصت و سه درصد گذشته، حال و آینده شناخت فراوان جامعه و متخصصان را می طلبد تا با نرم افزارها
    شناخت بیشتری را برای طراحان رایانه ای علی الخصوص طراحان خلاقی و فرهنگ پیشرو در زبان فارسی ایجاد کرد.
</BitMessage>

<BitMessage Dir=""BitDir.Rtl"" Color=""BitColor.Warning"" Multiline OnDismiss=""() => {}"" Title=""پیام هشدار"">
    <BitLink Href=""https://bitplatform.dev"">به وبسایت ما سر بزنید.</BitLink>
    <br />
    لورم ایپسوم متن ساختگی با تولید سادگی نامفهوم از صنعت چاپ و با استفاده از طراحان گرافیک است.
    چاپگرها و متون بلکه روزنامه و مجله در ستون و سطرآنچنان که لازم است
    و برای شرایط فعلی تکنولوژی مورد نیاز و کاربردهای متنوع با هدف بهبود ابزارهای کاربردی می باشد.
    کتابهای زیادی در شصت و سه درصد گذشته، حال و آینده شناخت فراوان جامعه و متخصصان را می طلبد تا با نرم افزارها
    شناخت بیشتری را برای طراحان رایانه ای علی الخصوص طراحان خلاقی و فرهنگ پیشرو در زبان فارسی ایجاد کرد.
</BitMessage>

<BitMessage Dir=""BitDir.Rtl"" Color=""BitColor.SevereWarning"">
    پیام هشدار شدید. <BitLink Href=""https://bitplatform.dev"">به وبسایت ما سر بزنید.</BitLink>
</BitMessage>

<BitMessage Dir=""BitDir.Rtl"" Color=""BitColor.Error"">
    پیام خطا. <BitLink Href=""https://bitplatform.dev"">به وبسایت ما سر بزنید.</BitLink>
</BitMessage>";
}
