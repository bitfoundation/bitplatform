namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Notifications.SnackBar;

public partial class BitSnackBarDemo
{
    private readonly string example1RazorCode = @"
<BitSnackBar @ref=""basicRef"" />
<BitButton OnClick=""OpenBasicSnackBar"">Open SnackBar</BitButton>";
    private readonly string example1CsharpCode = @"
private BitSnackBar basicRef = default!;
private async Task OpenBasicSnackBar()
{
    await basicRef.Info(""This is title"", ""This is body"");
}";

    private readonly string example2RazorCode = @"
<BitSnackBar @ref=""positionRef"" Position=""position"" Offset=""@offset""
             AutoDismiss AutoDismissTime=""TimeSpan.FromSeconds(3)"" />

<BitChoiceGroup @bind-Value=""position"" Label=""Position"" Horizontal
                TItem=""BitChoiceGroupOption<BitSnackBarPosition>"" TValue=""BitSnackBarPosition"">
    <BitChoiceGroupOption Text=""TopStart"" Value=""BitSnackBarPosition.TopStart"" />
    <BitChoiceGroupOption Text=""TopCenter"" Value=""BitSnackBarPosition.TopCenter"" />
    <BitChoiceGroupOption Text=""TopEnd"" Value=""BitSnackBarPosition.TopEnd"" />
    <BitChoiceGroupOption Text=""BottomStart"" Value=""BitSnackBarPosition.BottomStart"" />
    <BitChoiceGroupOption Text=""BottomCenter"" Value=""BitSnackBarPosition.BottomCenter"" />
    <BitChoiceGroupOption Text=""BottomEnd"" Value=""BitSnackBarPosition.BottomEnd"" />
</BitChoiceGroup>

<BitChoiceGroup @bind-Value=""offset"" Label=""Offset"" Horizontal
                TItem=""BitChoiceGroupOption<string>"" TValue=""string"">
    <BitChoiceGroupOption Text=""8px (default)"" Value=""@(""8px"")"" />
    <BitChoiceGroupOption Text=""2rem"" Value=""@(""2rem"")"" />
    <BitChoiceGroupOption Text=""4rem"" Value=""@(""4rem"")"" />
</BitChoiceGroup>

<BitButton OnClick=""OpenPositionSnackBar"">Open SnackBar</BitButton>";
    private readonly string example2CsharpCode = @"
private string offset = ""8px"";
private BitSnackBar positionRef = default!;
private BitSnackBarPosition position = BitSnackBarPosition.BottomEnd;
private async Task OpenPositionSnackBar()
{
    await positionRef.Info($""{position}"", $""Pinned to the selected position, {offset} from the edges."");
}";

    private readonly string example3RazorCode = @"
<BitSnackBar @ref=""autoDismissRef"" AutoDismiss AutoDismissTime=""TimeSpan.FromSeconds(5)"" PauseOnPageHidden />
<BitButton OnClick=""OpenAutoDismiss"">Hover me to pause the countdown</BitButton>

<BitSnackBar @ref=""reverseProgressRef"" AutoDismiss AutoDismissTime=""TimeSpan.FromSeconds(5)"" ReverseProgress />
<BitButton OnClick=""OpenReverseProgress"">Draining progress bar</BitButton>

<BitSnackBar @ref=""noProgressRef"" AutoDismiss AutoDismissTime=""TimeSpan.FromSeconds(5)"" HideProgress />
<BitButton OnClick=""OpenNoProgress"">No progress bar</BitButton>

<BitSnackBar @ref=""perItemTimeRef"" AutoDismiss AutoDismissTime=""TimeSpan.FromSeconds(10)"" />
<BitButton OnClick=""OpenPerItemTime"">Per-item dismiss time</BitButton>";
    private readonly string example3CsharpCode = @"
private BitSnackBar autoDismissRef = default!;
private BitSnackBar noProgressRef = default!;
private BitSnackBar perItemTimeRef = default!;
private BitSnackBar reverseProgressRef = default!;

private async Task OpenAutoDismiss()
{
    await autoDismissRef.Info(""Dismissing in 5 seconds"", ""Hover over me and the countdown holds."");
}

private async Task OpenReverseProgress()
{
    await reverseProgressRef.Info(""Dismissing in 5 seconds"", ""The bar drains as the time runs out."");
}

private async Task OpenNoProgress()
{
    await noProgressRef.Info(""Dismissing in 5 seconds"", ""The countdown runs without a progress bar."");
}

private async Task OpenPerItemTime()
{
    await perItemTimeRef.Show(""Quick one"", ""This item lives for 2 seconds."", autoDismissTime: TimeSpan.FromSeconds(2));
    await perItemTimeRef.Show(""Slow one"", ""This item takes the host's 10 seconds."", BitColor.Success);
}";

    private readonly string example4RazorCode = @"
<BitSnackBar @ref=""persistentRef"" Persistent />
<BitButton OnClick=""OpenPersistentSnackBar"">Open SnackBar</BitButton>
<BitButton OnClick=""ClosePersistentSnackBar"">Close SnackBar</BitButton>

<BitSnackBar @ref=""perItemPersistentRef"" AutoDismiss AutoDismissTime=""TimeSpan.FromSeconds(3)"" />
<BitButton OnClick=""OpenMixedPersistence"">Open one of each</BitButton>

<BitSnackBar @ref=""hideDismissRef"" HideDismiss AutoDismiss AutoDismissTime=""TimeSpan.FromSeconds(5)"" />
<BitButton OnClick=""OpenHideDismiss"">No dismiss button</BitButton>";
    private readonly string example4CsharpCode = @"
private BitSnackBarItem? persistentItem;
private BitSnackBar persistentRef = default!;
private BitSnackBar hideDismissRef = default!;
private BitSnackBar perItemPersistentRef = default!;

private async Task OpenPersistentSnackBar()
{
    await ClosePersistentSnackBar();

    persistentItem = await persistentRef.Info(""This is persistent title"", ""This is persistent body"");
}

private async Task ClosePersistentSnackBar()
{
    if (persistentItem is not null)
    {
        await persistentRef.Close(persistentItem);
        persistentItem = null;
    }
}

private async Task OpenMixedPersistence()
{
    await perItemPersistentRef.Info(""Goes away"", ""This one is dismissed after 3 seconds."");
    await perItemPersistentRef.Show(new BitSnackBarItem
    {
        Title = ""Stays put"",
        Body = ""This one is persistent, so it has no dismiss button and no countdown."",
        Color = BitColor.Warning,
        Persistent = true
    });
}

private async Task OpenHideDismiss()
{
    await hideDismissRef.Info(""No way out but the clock"", ""This item has no dismiss button, but it still counts down."");
}";

    private readonly string example5RazorCode = @"
<BitSnackBar @ref=""stackingRef"" MaxItems=""3"" NewestOnTop=""newestOnTop"" PreventDuplicates=""preventDuplicates""
             OverflowBehavior=""overflowBehavior"" AutoDismiss AutoDismissTime=""TimeSpan.FromSeconds(4)""
             OnShow=""HandleStackingChange"" OnDismiss=""HandleStackingChange"" />

<BitChoiceGroup @bind-Value=""overflowBehavior"" Label=""Overflow behavior"" Horizontal
                TItem=""BitChoiceGroupOption<BitSnackBarOverflowBehavior>"" TValue=""BitSnackBarOverflowBehavior"">
    <BitChoiceGroupOption Text=""DismissOldest"" Value=""BitSnackBarOverflowBehavior.DismissOldest"" />
    <BitChoiceGroupOption Text=""Queue"" Value=""BitSnackBarOverflowBehavior.Queue"" />
    <BitChoiceGroupOption Text=""Skip"" Value=""BitSnackBarOverflowBehavior.Skip"" />
</BitChoiceGroup>

<BitToggle @bind-Value=""newestOnTop"" Label=""Newest on top"" Inline />
<BitToggle @bind-Value=""preventDuplicates"" Label=""Prevent duplicates"" Inline />

<BitButton OnClick=""OpenStacking"">Show (max 3)</BitButton>
<BitButton OnClick=""OpenDuplicate"">Show a duplicate</BitButton>

<div>
    Showing: <b>@stackingRef?.Items.Count</b>
    Waiting: <b>@stackingRef?.PendingItems.Count</b>
    Repeats suppressed: <b>@duplicateItem?.DuplicateCount</b>
</div>";
    private readonly string example5CsharpCode = @"
private int stackingCounter;
private bool newestOnTop;
private bool preventDuplicates;
private BitSnackBar stackingRef = default!;
private BitSnackBarItem? duplicateItem;
private BitSnackBarOverflowBehavior overflowBehavior;

private void HandleStackingChange(BitSnackBarItem item) => StateHasChanged();

private async Task OpenStacking()
{
    stackingCounter++;
    await stackingRef.Info($""Notification {stackingCounter}"", ""Only three of these fit at a time."");
}

private async Task OpenDuplicate()
{
    duplicateItem = await stackingRef.Info(""Duplicate"", ""Showing this twice only adds one while PreventDuplicates is on."");
}";

    private readonly string example6RazorCode = @"
<BitSnackBar @ref=""iconRef"" ShowIcon />
<BitButton OnClick=""OpenIconInfo"">Info</BitButton>
<BitButton OnClick=""OpenIconSuccess"">Success</BitButton>
<BitButton OnClick=""OpenIconError"">Error</BitButton>

<BitSnackBar @ref=""customIconRef"" ShowIcon IconName=""@BitIconName.Ringer"" />
<BitButton OnClick=""OpenCustomIcon"">Custom icon</BitButton>

<BitSnackBar @ref=""perItemIconRef"" ShowIcon />
<BitButton OnClick=""OpenPerItemIcon"">Per-item icon</BitButton>";
    private readonly string example6CsharpCode = @"
private BitSnackBar iconRef = default!;
private BitSnackBar customIconRef = default!;
private BitSnackBar perItemIconRef = default!;

private async Task OpenIconInfo() => await iconRef.Info(""Info"", ""The icon follows the color of the item."");

private async Task OpenIconSuccess() => await iconRef.Success(""Success"", ""The icon follows the color of the item."");

private async Task OpenIconError() => await iconRef.Error(""Error"", ""The icon follows the color of the item."");

private async Task OpenCustomIcon()
{
    await customIconRef.Info(""Reminder"", ""Every item of this host uses the Ringer icon."");
}

private async Task OpenPerItemIcon()
{
    await perItemIconRef.Show(new BitSnackBarItem
    {
        Title = ""Deployed"",
        Body = ""This one item asked for the Rocket icon."",
        Color = BitColor.Success,
        IconName = BitIconName.Rocket
    });
    await perItemIconRef.Show(new BitSnackBarItem
    {
        Title = ""No icon"",
        Body = ""And this one dropped its icon."",
        Color = BitColor.Info,
        HideIcon = true
    });
}";

    private readonly string example7RazorCode = @"
<BitSnackBar @ref=""actionsRef"" AutoDismiss AutoDismissTime=""TimeSpan.FromSeconds(8)"" ShowIcon>
    <ActionsTemplate Context=""item"">
        <BitButton Variant=""BitVariant.Text"" Color=""BitColor.TertiaryBackground"" OnClick=""() => Undo(item)"">Undo</BitButton>
    </ActionsTemplate>
</BitSnackBar>
<BitButton OnClick=""OpenActions"">Delete item</BitButton>

<div>Last action: <b>@actionResult</b></div>";
    private readonly string example7CsharpCode = @"
private string actionResult = ""-"";
private BitSnackBar actionsRef = default!;

private async Task OpenActions()
{
    actionResult = ""-"";
    await actionsRef.Warning(""Item deleted"", ""The item was moved to the recycle bin."");
}

private async Task Undo(BitSnackBarItem item)
{
    actionResult = $""Undone: {item.Title}"";
    await actionsRef.Close(item);
}";

    private readonly string example8RazorCode = @"
<BitSnackBar @ref=""singleLineRef"" />
<BitButton OnClick=""OpenSingleLine"">Single line</BitButton>

<BitSnackBar @ref=""multilineRef"" Multiline />
<BitButton OnClick=""OpenMultiline"">Multiline</BitButton>

<BitSnackBar @ref=""maxWidthRef"" Multiline MaxWidth=""20rem"" />
<BitButton OnClick=""OpenMaxWidth"">Multiline, capped at 20rem</BitButton>";
    private readonly string example8CsharpCode = @"
private BitSnackBar singleLineRef = default!;
private BitSnackBar multilineRef = default!;
private BitSnackBar maxWidthRef = default!;

private const string LongBody = ""This body is long enough that it does not fit on a single line, so it is either cut off with an ellipsis or wrapped over as many lines as it needs."";

private async Task OpenSingleLine() => await singleLineRef.Info(""A title that is also too long to fit on one line"", LongBody);

private async Task OpenMultiline() => await multilineRef.Info(""A title that is also too long to fit on one line"", LongBody);

private async Task OpenMaxWidth() => await maxWidthRef.Info(""A title that is also too long to fit on one line"", LongBody);";

    private readonly string example9RazorCode = @"
<BitSnackBar @ref=""titleTemplateRef"">
    <TitleTemplate Context=""title"">
        <div style=""display: flex; flex-direction: row; gap: 10px;"">
            <span>@title</span>
            <BitProgress Thickness=""20"" Style=""width: 40px;"" Indeterminate />
        </div>
    </TitleTemplate>
</BitSnackBar>
<BitButton OnClick=""OpenTitleTemplate"">Title template</BitButton>

<BitSnackBar @ref=""bodyTemplateRef"">
    <BodyTemplate Context=""body"">
        <div style=""display: flex; flex-flow: column nowrap; gap: 5px;"">
            <span style=""font-size: 12px; margin-bottom: 5px;"">@body</span>
            <div style=""display: flex; gap: 10px;"">
                <BitButton OnClick=""@(() => bodyTemplateAnswer = ""Yes"")"">Yes</BitButton>
                <BitButton OnClick=""@(() => bodyTemplateAnswer = ""No"")"">No</BitButton>
            </div>
            <span>Answer: @bodyTemplateAnswer</span>
        </div>
    </BodyTemplate>
</BitSnackBar>
<BitButton OnClick=""OpenBodyTemplate"">Body template</BitButton>

<BitSnackBar @ref=""fullTemplateRef"" AutoDismiss AutoDismissTime=""TimeSpan.FromSeconds(6)"">
    <Template Context=""item"">
        <div style=""display: flex; align-items: center; gap: 10px;"">
            <BitPersona PrimaryText=""@item.Title"" SecondaryText=""@item.Body"" Size=""BitPersonaSize.Size32"" />
            <BitButton Variant=""BitVariant.Text"" Color=""BitColor.TertiaryBackground""
                       IconName=""@BitIconName.Cancel"" OnClick=""() => fullTemplateRef.Close(item)"" />
        </div>
    </Template>
</BitSnackBar>
<BitButton OnClick=""OpenFullTemplate"">Item template</BitButton>";
    private readonly string example9CsharpCode = @"
private string? bodyTemplateAnswer;
private BitSnackBar bodyTemplateRef = default!;
private BitSnackBar titleTemplateRef = default!;
private BitSnackBar fullTemplateRef = default!;

private async Task OpenTitleTemplate()
{
    await titleTemplateRef.Warning(""This is title"", ""This is body"");
}

private async Task OpenBodyTemplate()
{
    bodyTemplateAnswer = null;
    await bodyTemplateRef.Error(""This is title"", ""This is body"");
}

private async Task OpenFullTemplate()
{
    await fullTemplateRef.Show(""Alice Johnson"", ""sent you a message"", BitColor.Primary);
}";

    private readonly string example10RazorCode = @"
<BitSnackBar @ref=""eventsRef"" DismissOnClick AutoDismiss AutoDismissTime=""TimeSpan.FromSeconds(6)""
             OnShow=""HandleOnShow"" OnDismiss=""HandleOnDismiss"" OnItemClick=""HandleOnItemClick"" />
<BitButton OnClick=""OpenEvents"">Open SnackBar</BitButton>

<ul>
    @foreach (var log in eventLogs)
    {
        <li>@log</li>
    }
</ul>";
    private readonly string example10CsharpCode = @"
private BitSnackBar eventsRef = default!;
private readonly List<string> eventLogs = [];

private void Log(string message)
{
    eventLogs.Insert(0, message);
    if (eventLogs.Count > 5) eventLogs.RemoveAt(eventLogs.Count - 1);
}

private void HandleOnShow(BitSnackBarItem item) => Log($""OnShow: {item.Title}"");

private void HandleOnDismiss(BitSnackBarItem item) => Log($""OnDismiss: {item.Title} ({item.DismissReason})"");

private void HandleOnItemClick(BitSnackBarItem item) => Log($""OnItemClick: {item.Title}"");

private async Task OpenEvents()
{
    await eventsRef.Info($""Notification {eventLogs.Count + 1}"", ""Click me, close me or wait - the reason is reported."");
}";

    private readonly string example11RazorCode = @"
<BitSnackBar @ref=""controlRef"" AutoDismiss AutoDismissTime=""TimeSpan.FromSeconds(6)"" ShowIcon
             OnShow=""HandleControlChange"" OnDismiss=""HandleControlChange"" />

<BitButton OnClick=""StartUpload"">Start upload</BitButton>
<BitButton OnClick=""CompleteUpload"" IsEnabled=""uploadItem is not null"">Complete upload</BitButton>

<BitButton OnClick=""PauseAll"">Pause</BitButton>
<BitButton OnClick=""ResumeAll"">Resume</BitButton>
<BitButton OnClick=""ClearAll"">Clear all</BitButton>

<div>Showing: <b>@controlRef?.Items.Count</b></div>";
    private readonly string example11CsharpCode = @"
private BitSnackBarItem? uploadItem;
private BitSnackBar controlRef = default!;

private void HandleControlChange(BitSnackBarItem item) => StateHasChanged();

private async Task StartUpload()
{
    uploadItem = await controlRef.Show(new BitSnackBarItem
    {
        Title = ""Uploading..."",
        Body = ""report.pdf"",
        Color = BitColor.Info,
        Persistent = true
    });
}

private async Task CompleteUpload()
{
    if (uploadItem is null) return;

    uploadItem.Title = ""Upload complete"";
    uploadItem.Color = BitColor.Success;
    uploadItem.Persistent = false;

    await controlRef.Update(uploadItem);

    uploadItem = null;
}

private async Task PauseAll()
{
    foreach (var item in controlRef.Items.ToArray())
    {
        await controlRef.Pause(item);
    }
}

private async Task ResumeAll()
{
    foreach (var item in controlRef.Items.ToArray())
    {
        await controlRef.Resume(item);
    }
}

private async Task ClearAll() => await controlRef.Clear();";

    private readonly string example12RazorCode = @"
<BitSnackBar @ref=""a11yRef"" ShowIcon AriaLabel=""Demo notifications"" DismissAriaLabel=""Dismiss notification"" />

<BitButton OnClick=""OpenPoliteA11y"">Polite (status)</BitButton>
<BitButton OnClick=""OpenAssertiveA11y"">Assertive (alert)</BitButton>
<BitButton OnClick=""OpenAnnounceText"">Custom announcement</BitButton>
<BitButton OnClick=""OpenSilentA11y"">Unannounced</BitButton>";
    private readonly string example12CsharpCode = @"
private BitSnackBar a11yRef = default!;

private async Task OpenPoliteA11y()
{
    await a11yRef.Success(""Saved"", ""A screen reader hears this at the next pause in what it is saying."");
}

private async Task OpenAssertiveA11y()
{
    await a11yRef.Error(""Save failed"", ""A problem interrupts the screen reader instead of waiting."");
}

private async Task OpenAnnounceText()
{
    await a11yRef.Show(new BitSnackBarItem
    {
        Title = ""ETA 5m"",
        Body = ""Sync in progress."",
        Color = BitColor.Info,
        AnnounceText = ""Estimated time of arrival: five minutes. Sync in progress.""
    });
}

private async Task OpenSilentA11y()
{
    await a11yRef.Show(new BitSnackBarItem
    {
        Title = ""Seen but not heard"",
        Body = ""A role that is not a live one leaves the item unannounced."",
        Color = BitColor.Warning,
        Role = ""presentation""
    });
}";

    private readonly string example13RazorCode = @"
<BitSnackBar @ref=""customizationRef""
             Dir=""direction""
             Size=""customSize""
             ShowIcon=""customShowIcon""
             Variant=""customVariant""
             Position=""basicSnackBarPosition""
             Multiline=""basicSnackBarMultiline""
             AutoDismiss=""basicSnackBarAutoDismiss""
             TransitionDuration=""customTransitionDuration""
             AutoDismissTime=""TimeSpan.FromSeconds(basicSnackBarDismissSeconds)"" />

<BitButton OnClick=""OpenCustomizationSnackBar"">Show</BitButton>

<BitChoiceGroup @bind-Value=""basicSnackBarColor"" Label=""Color"" TItem=""BitChoiceGroupOption<BitColor>"" TValue=""BitColor"">
    <BitChoiceGroupOption Text=""Primary"" Value=""BitColor.Primary"" />
    <BitChoiceGroupOption Text=""Secondary"" Value=""BitColor.Secondary"" />
    <BitChoiceGroupOption Text=""Tertiary"" Value=""BitColor.Tertiary"" />
    <BitChoiceGroupOption Text=""Info"" Value=""BitColor.Info"" />
    <BitChoiceGroupOption Text=""Success"" Value=""BitColor.Success"" />
    <BitChoiceGroupOption Text=""Warning"" Value=""BitColor.Warning"" />
    <BitChoiceGroupOption Text=""SevereWarning"" Value=""BitColor.SevereWarning"" />
    <BitChoiceGroupOption Text=""Error"" Value=""BitColor.Error"" />
    <BitChoiceGroupOption Text=""PrimaryBackground"" Value=""BitColor.PrimaryBackground"" />
    <BitChoiceGroupOption Text=""SecondaryBackground"" Value=""BitColor.SecondaryBackground"" />
    <BitChoiceGroupOption Text=""TertiaryBackground"" Value=""BitColor.TertiaryBackground"" />
    <BitChoiceGroupOption Text=""PrimaryForeground"" Value=""BitColor.PrimaryForeground"" />
    <BitChoiceGroupOption Text=""SecondaryForeground"" Value=""BitColor.SecondaryForeground"" />
    <BitChoiceGroupOption Text=""TertiaryForeground"" Value=""BitColor.TertiaryForeground"" />
    <BitChoiceGroupOption Text=""PrimaryBorder"" Value=""BitColor.PrimaryBorder"" />
    <BitChoiceGroupOption Text=""SecondaryBorder"" Value=""BitColor.SecondaryBorder"" />
    <BitChoiceGroupOption Text=""TertiaryBorder"" Value=""BitColor.TertiaryBorder"" />
</BitChoiceGroup>

<BitChoiceGroup @bind-Value=""basicSnackBarPosition"" Label=""Position"" TItem=""BitChoiceGroupOption<BitSnackBarPosition>"" TValue=""BitSnackBarPosition"">
    <BitChoiceGroupOption Text=""TopStart"" Value=""BitSnackBarPosition.TopStart"" />
    <BitChoiceGroupOption Text=""TopCenter"" Value=""BitSnackBarPosition.TopCenter"" />
    <BitChoiceGroupOption Text=""TopEnd"" Value=""BitSnackBarPosition.TopEnd"" />
    <BitChoiceGroupOption Text=""BottomStart"" Value=""BitSnackBarPosition.BottomStart"" />
    <BitChoiceGroupOption Text=""BottomCenter"" Value=""BitSnackBarPosition.BottomCenter"" />
    <BitChoiceGroupOption Text=""BottomEnd"" Value=""BitSnackBarPosition.BottomEnd"" />
</BitChoiceGroup>

<BitChoiceGroup @bind-Value=""direction"" Label=""Direction"" TItem=""BitChoiceGroupOption<BitDir>"" TValue=""BitDir"">
    <BitChoiceGroupOption Text=""LTR"" Value=""BitDir.Ltr"" />
    <BitChoiceGroupOption Text=""RTL"" Value=""BitDir.Rtl"" />
    <BitChoiceGroupOption Text=""Auto"" Value=""BitDir.Auto"" />
</BitChoiceGroup>

<BitChoiceGroup @bind-Value=""customVariant"" Label=""Variant"" TItem=""BitChoiceGroupOption<BitVariant>"" TValue=""BitVariant"">
    <BitChoiceGroupOption Text=""Fill"" Value=""BitVariant.Fill"" />
    <BitChoiceGroupOption Text=""Outline"" Value=""BitVariant.Outline"" />
    <BitChoiceGroupOption Text=""Text"" Value=""BitVariant.Text"" />
</BitChoiceGroup>

<BitChoiceGroup @bind-Value=""customSize"" Label=""Size"" TItem=""BitChoiceGroupOption<BitSize>"" TValue=""BitSize"">
    <BitChoiceGroupOption Text=""Small"" Value=""BitSize.Small"" />
    <BitChoiceGroupOption Text=""Medium"" Value=""BitSize.Medium"" />
    <BitChoiceGroupOption Text=""Large"" Value=""BitSize.Large"" />
</BitChoiceGroup>

<BitToggle @bind-Value=""basicSnackBarAutoDismiss"" Label=""Auto Dismiss"" Inline />
<BitNumberField @bind-Value=""basicSnackBarDismissSeconds"" IsEnabled=""basicSnackBarAutoDismiss"" Step=""1"" Min=""1"" Label=""Dismiss Time (based on second)"" />
<BitNumberField @bind-Value=""customTransitionDuration"" Step=""50"" Min=""0"" Max=""2000"" Label=""Transition Duration (ms)"" />

<BitToggle @bind-Value=""basicSnackBarMultiline"" Label=""Multiline"" Inline />
<BitToggle @bind-Value=""customShowIcon"" Label=""Show Icon"" Inline />

<BitTextField @bind-Value=""basicSnackBarTitle"" Label=""Title"" DefaultValue=""Title"" />
<BitTextField @bind-Value=""basicSnackBarBody"" Label=""Body"" Multiline Rows=""6"" DefaultValue=""This is a body!"" />";
    private readonly string example13CsharpCode = @"
private BitDir direction;
private bool customShowIcon;
private bool basicSnackBarMultiline;
private bool basicSnackBarAutoDismiss;
private int basicSnackBarDismissSeconds = 3;
private int customTransitionDuration = 200;
private BitSnackBar customizationRef = default!;
private BitSize customSize = BitSize.Medium;
private BitVariant customVariant = BitVariant.Fill;
private string basicSnackBarBody = ""This is body"";
private string basicSnackBarTitle = ""This is title"";
private BitColor basicSnackBarColor = BitColor.Info;
private BitSnackBarPosition basicSnackBarPosition = BitSnackBarPosition.BottomEnd;

private async Task OpenCustomizationSnackBar()
{
    await customizationRef.Show(basicSnackBarTitle, basicSnackBarBody, basicSnackBarColor);
}";

    private readonly string example14RazorCode = @"
<BitSnackBar @ref=""colorRef"" ShowIcon Variant=""colorVariant"" MaxItems=""4"" NewestOnTop />

<BitChoiceGroup @bind-Value=""colorVariant"" Label=""Variant"" Horizontal TItem=""BitChoiceGroupOption<BitVariant>"" TValue=""BitVariant"">
    <BitChoiceGroupOption Text=""Fill"" Value=""BitVariant.Fill"" />
    <BitChoiceGroupOption Text=""Outline"" Value=""BitVariant.Outline"" />
    <BitChoiceGroupOption Text=""Text"" Value=""BitVariant.Text"" />
</BitChoiceGroup>

<BitButton OnClick=""@(async () => await colorRef.Info(""Info"", ""This is an info notification.""))"">Info</BitButton>
<BitButton OnClick=""@(async () => await colorRef.Success(""Success"", ""This is a success notification.""))"">Success</BitButton>
<BitButton OnClick=""@(async () => await colorRef.Warning(""Warning"", ""This is a warning notification.""))"">Warning</BitButton>
<BitButton OnClick=""@(async () => await colorRef.SevereWarning(""SevereWarning"", ""This is a severe warning notification.""))"">SevereWarning</BitButton>
<BitButton OnClick=""@(async () => await colorRef.Error(""Error"", ""This is an error notification.""))"">Error</BitButton>
<BitButton OnClick=""@(async () => await colorRef.Show(""Primary"", ""This is a primary notification."", BitColor.Primary))"">Primary</BitButton>
<BitButton OnClick=""@(async () => await colorRef.Show(""Secondary"", ""This is a secondary notification."", BitColor.Secondary))"">Secondary</BitButton>
<BitButton OnClick=""@(async () => await colorRef.Show(""Tertiary"", ""This is a tertiary notification."", BitColor.Tertiary))"">Tertiary</BitButton>";
    private readonly string example14CsharpCode = @"
private BitSnackBar colorRef = default!;
private BitVariant colorVariant = BitVariant.Fill;";

    private readonly string example15RazorCode = @"
<link rel=""stylesheet"" href=""https://cdnjs.cloudflare.com/ajax/libs/font-awesome/7.0.1/css/all.min.css"" />

<BitSnackBar @ref=""dismissIconFaRef"" DismissIcon=""@BitIconInfo.Fa(""solid xmark"")"" />
<BitButton OnClick=""OpenDismissIconFa"">FontAwesome dismiss icon</BitButton>

<BitSnackBar @ref=""dismissIconCssRef"" DismissIcon=""@BitIconInfo.Css(""fa-solid fa-x"")"" />
<BitButton OnClick=""OpenDismissIconCss"">CSS classes dismiss icon</BitButton>

<BitSnackBar @ref=""leadingIconFaRef"" ShowIcon Icon=""@BitIconInfo.Fa(""solid circle-info"")"" />
<BitButton OnClick=""OpenLeadingIconFa"">FontAwesome leading icon</BitButton>


<link rel=""stylesheet"" href=""https://cdn.jsdelivr.net/npm/bootstrap-icons@1.11.3/font/bootstrap-icons.min.css"" />

<BitSnackBar @ref=""dismissIconBiRef"" DismissIcon=""@BitIconInfo.Bi(""x-lg"")"" />
<BitButton OnClick=""OpenDismissIconBi"">Bootstrap dismiss icon</BitButton>

<BitSnackBar @ref=""dismissIconImplicitRef"" DismissIcon=""@(""bi bi-x-circle"")"" />
<BitButton OnClick=""OpenDismissIconImplicit"">Implicit CSS dismiss icon</BitButton>";
    private readonly string example15CsharpCode = @"
private BitSnackBar dismissIconFaRef = default!;
private BitSnackBar dismissIconCssRef = default!;
private BitSnackBar leadingIconFaRef = default!;
private BitSnackBar dismissIconBiRef = default!;
private BitSnackBar dismissIconImplicitRef = default!;

private async Task OpenDismissIconFa()
{
    await dismissIconFaRef.Info(""Notification"", ""Click the FontAwesome dismiss icon to close."");
}

private async Task OpenDismissIconCss()
{
    await dismissIconCssRef.Info(""Notification"", ""Click the CSS class dismiss icon to close."");
}

private async Task OpenLeadingIconFa()
{
    await leadingIconFaRef.Info(""Notification"", ""The leading icon comes from FontAwesome."");
}

private async Task OpenDismissIconBi()
{
    await dismissIconBiRef.Info(""Notification"", ""Click the Bootstrap dismiss icon to close."");
}

private async Task OpenDismissIconImplicit()
{
    await dismissIconImplicitRef.Info(""Notification"", ""Click the implicit CSS dismiss icon to close."");
}";

    private readonly string example16RazorCode = @"
<BitSnackBar @ref=""sizeSmallRef"" ShowIcon Size=""BitSize.Small"" />
<BitButton OnClick=""OpenSizeSmall"">Small</BitButton>

<BitSnackBar @ref=""sizeMediumRef"" ShowIcon Size=""BitSize.Medium"" />
<BitButton OnClick=""OpenSizeMedium"">Medium</BitButton>

<BitSnackBar @ref=""sizeLargeRef"" ShowIcon Size=""BitSize.Large"" />
<BitButton OnClick=""OpenSizeLarge"">Large</BitButton>";
    private readonly string example16CsharpCode = @"
private BitSnackBar sizeSmallRef = default!;
private BitSnackBar sizeMediumRef = default!;
private BitSnackBar sizeLargeRef = default!;

private async Task OpenSizeSmall() => await sizeSmallRef.Info(""Small"", ""The small size snack bar."");

private async Task OpenSizeMedium() => await sizeMediumRef.Info(""Medium"", ""The medium size snack bar."");

private async Task OpenSizeLarge() => await sizeLargeRef.Info(""Large"", ""The large size snack bar."");";

    private readonly string example17RazorCode = @"
<style>
    .custom-class {
        background-color: tomato;
        box-shadow: gold 0 0 1rem;
    }

    .custom-container {
        border: 1px solid gold;
    }

    .custom-progress {
        background-color: red;
    }
</style>


<BitSnackBar @ref=""snackBarStyleRef"" />
<BitButton OnClick=""OpenSnackBarStyle"">Custom style</BitButton>

<BitSnackBar @ref=""snackBarClassRef"" />
<BitButton OnClick=""OpenSnackBarClass"">Custom class</BitButton>

<BitSnackBar @ref=""snackBarStylesRef""
             Styles=""@(new() { Container = ""width: 16rem; background-color: purple;"",
                               Header = ""background-color: rebeccapurple; padding: 0.2rem;"" })"" />
<BitButton OnClick=""OpenSnackBarStyles"">Custom styles</BitButton>

<BitSnackBar @ref=""snackBarClassesRef"" AutoDismiss
             Classes=""@(new() { Container = ""custom-container"",
                                ProgressBar = ""custom-progress"" })"" />
<BitButton OnClick=""OpenSnackBarClasses"">Custom classes</BitButton>";
    private readonly string example17CsharpCode = @"
private BitSnackBar snackBarStyleRef = default!;
private BitSnackBar snackBarClassRef = default!;
private BitSnackBar snackBarStylesRef = default!;
private BitSnackBar snackBarClassesRef = default!;

private async Task OpenSnackBarStyle()
{
    await snackBarStyleRef.Show(""This is title"", ""This is body"", cssStyle: ""background-color: dodgerblue; border-radius: 0.5rem;"");
}

private async Task OpenSnackBarClass()
{
    await snackBarClassRef.Show(""This is title"", ""This is body"", cssClass: ""custom-class"");
}

private async Task OpenSnackBarStyles()
{
    await snackBarStylesRef.Show(""This is title"", ""This is body"");
}

private async Task OpenSnackBarClasses()
{
    await snackBarClassesRef.Show(""This is title"", ""This is body"");
}";

    private readonly string example18RazorCode = @"
<BitSnackBar @ref=""rtlRef"" Dir=""BitDir.Rtl"" ShowIcon Position=""BitSnackBarPosition.BottomStart""
             AutoDismiss AutoDismissTime=""TimeSpan.FromSeconds(5)"" />
<BitButton Dir=""BitDir.Rtl"" OnClick=""OpenRtl"">نمایش پیام</BitButton>";
    private readonly string example18CsharpCode = @"
private BitSnackBar rtlRef = default!;

private async Task OpenRtl()
{
    await rtlRef.Success(""عنوان پیام"", ""این متن پیام است."");
}";
}
