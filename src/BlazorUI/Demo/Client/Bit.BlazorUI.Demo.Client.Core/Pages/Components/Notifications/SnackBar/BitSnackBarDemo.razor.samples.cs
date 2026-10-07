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
<BitSnackBar @ref=""positionRef"" Position=""position"" Offset=""@offset"" TransitionDuration=""transitionDuration"" />

<BitChoiceGroup @bind-Value=""position"" Label=""Position"" Horizontal
                TItem=""BitChoiceGroupOption<BitPosition>"" TValue=""BitPosition"">
    <BitChoiceGroupOption Text=""TopStart"" Value=""BitPosition.TopStart"" />
    <BitChoiceGroupOption Text=""TopCenter"" Value=""BitPosition.TopCenter"" />
    <BitChoiceGroupOption Text=""TopEnd"" Value=""BitPosition.TopEnd"" />
    <BitChoiceGroupOption Text=""TopLeft"" Value=""BitPosition.TopLeft"" />
    <BitChoiceGroupOption Text=""TopRight"" Value=""BitPosition.TopRight"" />
    <BitChoiceGroupOption Text=""CenterStart"" Value=""BitPosition.CenterStart"" />
    <BitChoiceGroupOption Text=""Center"" Value=""BitPosition.Center"" />
    <BitChoiceGroupOption Text=""CenterEnd"" Value=""BitPosition.CenterEnd"" />
    <BitChoiceGroupOption Text=""CenterLeft"" Value=""BitPosition.CenterLeft"" />
    <BitChoiceGroupOption Text=""CenterRight"" Value=""BitPosition.CenterRight"" />
    <BitChoiceGroupOption Text=""BottomStart"" Value=""BitPosition.BottomStart"" />
    <BitChoiceGroupOption Text=""BottomCenter"" Value=""BitPosition.BottomCenter"" />
    <BitChoiceGroupOption Text=""BottomEnd"" Value=""BitPosition.BottomEnd"" />
    <BitChoiceGroupOption Text=""BottomLeft"" Value=""BitPosition.BottomLeft"" />
    <BitChoiceGroupOption Text=""BottomRight"" Value=""BitPosition.BottomRight"" />
</BitChoiceGroup>

<BitChoiceGroup @bind-Value=""offset"" Label=""Offset"" Horizontal
                TItem=""BitChoiceGroupOption<string>"" TValue=""string"">
    <BitChoiceGroupOption Text=""8px (default)"" Value=""@(""8px"")"" />
    <BitChoiceGroupOption Text=""2rem"" Value=""@(""2rem"")"" />
    <BitChoiceGroupOption Text=""4rem"" Value=""@(""4rem"")"" />
</BitChoiceGroup>

<BitChoiceGroup @bind-Value=""transitionDuration"" Label=""TransitionDuration"" Horizontal
                TItem=""BitChoiceGroupOption<int>"" TValue=""int"">
    <BitChoiceGroupOption Text=""0"" Value=""0"" />
    <BitChoiceGroupOption Text=""200ms (default)"" Value=""200"" />
    <BitChoiceGroupOption Text=""600ms"" Value=""600"" />
</BitChoiceGroup>

<BitButton OnClick=""OpenPositionSnackBar"">Open SnackBar</BitButton>";
    private readonly string example2CsharpCode = @"
private string offset = ""8px"";
private int transitionDuration = 200;
private BitSnackBar positionRef = default!;
private BitPosition position = BitPosition.BottomEnd;
private async Task OpenPositionSnackBar()
{
    await positionRef.Info($""{position}"", $""Pinned to the selected position, {offset} from the edges."");
}";

    private readonly string example3RazorCode = @"
<BitSnackBar @ref=""autoDismissRef"" AutoDismiss AutoDismissTime=""TimeSpan.FromSeconds(5)""
             ReverseProgress=""reverseProgress"" HideProgress=""hideProgress""
             PauseOnPageHidden PauseOnWindowBlur />

<BitToggle @bind-Value=""reverseProgress"" Label=""ReverseProgress"" Inline />
<BitToggle @bind-Value=""hideProgress"" Label=""HideProgress"" Inline />

<BitButton OnClick=""OpenAutoDismiss"">Hover me to pause the countdown</BitButton>
<BitButton OnClick=""OpenPerItemTime"">Per-item dismiss time</BitButton>";
    private readonly string example3CsharpCode = @"
private bool hideProgress;
private bool reverseProgress;
private BitSnackBar autoDismissRef = default!;

private async Task OpenAutoDismiss()
{
    await autoDismissRef.Info(""Dismissing in 5 seconds"", ""Hover over me and the countdown holds."");
}

private async Task OpenPerItemTime()
{
    await autoDismissRef.Show(""Quick one"", ""This item lives for 2 seconds."", autoDismissTime: TimeSpan.FromSeconds(2));
    await autoDismissRef.Show(""Slow one"", ""This item takes the host's 5 seconds."", BitColor.Success);
}";

    private readonly string example4RazorCode = @"
<BitSnackBar @ref=""persistentRef"" Persistent />
<BitButton OnClick=""OpenPersistentSnackBar"">Open SnackBar</BitButton>
<BitButton OnClick=""ClosePersistentSnackBar"">Close SnackBar</BitButton>

<BitSnackBar @ref=""perItemPersistentRef"" AutoDismiss AutoDismissTime=""TimeSpan.FromSeconds(5)"" />
<BitButton OnClick=""OpenMixedPersistence"">Open one of each</BitButton>";
    private readonly string example4CsharpCode = @"
private BitSnackBarItem? persistentItem;
private BitSnackBar persistentRef = default!;
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
    await perItemPersistentRef.Info(""Goes away"", ""Dismissed after 5 seconds, or by its button."");
    await perItemPersistentRef.Show(new BitSnackBarItem
    {
        Title = ""No button"",
        Body = ""Still counts down and still answers Escape."",
        Color = BitColor.Success,
        HideDismiss = true
    });
    await perItemPersistentRef.Show(new BitSnackBarItem
    {
        Title = ""Stays put"",
        Body = ""Persistent: no button and no countdown."",
        Color = BitColor.Warning,
        Persistent = true
    });
}";

    private readonly string example5RazorCode = @"
<BitSnackBar @ref=""swipeRef"" SwipeToDismiss SwipeThreshold=""60"" OnDismiss=""HandleSwipeDismiss"" />
<BitButton OnClick=""OpenSwipe"">Open, then drag it sideways</BitButton>

<div>Last dismissal: <b>@swipeResult</b></div>";
    private readonly string example5CsharpCode = @"
private string? swipeResult;
private BitSnackBar swipeRef = default!;

private async Task OpenSwipe()
{
    await swipeRef.Info(""Drag me away"", ""A drag of 60 pixels or more takes this off the screen."");
}

private void HandleSwipeDismiss(BitSnackBarItem item)
{
    swipeResult = $""{item.Title} ({item.DismissReason})"";
    StateHasChanged();
}";

    private readonly string example6RazorCode = @"
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
    private readonly string example6CsharpCode = @"
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

    private readonly string example7RazorCode = @"
<BitSnackBar @ref=""iconRef"" ShowIcon />
<BitButton OnClick=""OpenIconInfo"">Info</BitButton>
<BitButton OnClick=""OpenIconSuccess"">Success</BitButton>
<BitButton OnClick=""OpenIconError"">Error</BitButton>
<BitButton OnClick=""OpenPerItemIcon"">Per-item icon</BitButton>

<BitSnackBar @ref=""customIconRef"" ShowIcon IconName=""@BitIconName.Ringer"" />
<BitButton OnClick=""OpenCustomIcon"">Host-wide icon</BitButton>";
    private readonly string example7CsharpCode = @"
private BitSnackBar iconRef = default!;
private BitSnackBar customIconRef = default!;

private async Task OpenIconInfo() => await iconRef.Info(""Info"", ""The icon follows the color of the item."");

private async Task OpenIconSuccess() => await iconRef.Success(""Success"", ""The icon follows the color of the item."");

private async Task OpenIconError() => await iconRef.Error(""Error"", ""The icon follows the color of the item."");

private async Task OpenPerItemIcon()
{
    await iconRef.Show(new BitSnackBarItem
    {
        Title = ""Deployed"",
        Body = ""This one item asked for the Rocket icon."",
        Color = BitColor.Success,
        IconName = BitIconName.Rocket
    });
    await iconRef.Show(new BitSnackBarItem
    {
        Title = ""No icon"",
        Body = ""And this one dropped its icon."",
        Color = BitColor.Info,
        HideIcon = true
    });
}

private async Task OpenCustomIcon()
{
    await customIconRef.Info(""Reminder"", ""Every item of this host uses the Ringer icon."");
}";

    private readonly string example8RazorCode = @"
<BitSnackBar @ref=""actionsRef"" AutoDismiss AutoDismissTime=""TimeSpan.FromSeconds(8)"" ShowIcon>
    <ActionsTemplate Context=""item"">
        <BitButton Variant=""BitVariant.Text"" Color=""BitColor.TertiaryBackground"" OnClick=""() => Undo(item)"">Undo</BitButton>
    </ActionsTemplate>
</BitSnackBar>
<BitButton OnClick=""OpenActions"">Delete item</BitButton>

<div>Last action: <b>@actionResult</b></div>";
    private readonly string example8CsharpCode = @"
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

    private readonly string example9RazorCode = @"
<BitSnackBar @ref=""multilineRef"" Multiline=""multiline"" MaxWidth=""@maxWidth"" />

<BitChoiceGroup @bind-Value=""maxWidth"" Label=""MaxWidth"" Horizontal
                TItem=""BitChoiceGroupOption<string>"" TValue=""string"">
    <BitChoiceGroupOption Text=""None"" Value=""@("""")"" />
    <BitChoiceGroupOption Text=""20rem"" Value=""@(""20rem"")"" />
</BitChoiceGroup>
<BitToggle @bind-Value=""multiline"" Label=""Multiline"" Inline />

<BitButton OnClick=""OpenMultiline"">Show a long message</BitButton>";
    private readonly string example9CsharpCode = @"
private bool multiline;
private string maxWidth = ""20rem"";
private BitSnackBar multilineRef = default!;

private async Task OpenMultiline()
{
    await multilineRef.Info(""A title that is also too long to fit on one line"",
                            ""This body is long enough that it does not fit on a single line, so it is either cut off with an ellipsis or wrapped over as many lines as it needs."");
}";

    private readonly string example10RazorCode = @"
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
            <BitButton Variant=""BitVariant.Text"" Color=""BitColor.TertiaryBackground"" AriaLabel=""Close""
                       IconName=""@BitIconName.Cancel"" OnClick=""() => fullTemplateRef.Close(item)"" />
        </div>
    </Template>
</BitSnackBar>
<BitButton OnClick=""OpenFullTemplate"">Item template</BitButton>";
    private readonly string example10CsharpCode = @"
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

    private readonly string example11RazorCode = @"
<BitSnackBar @ref=""eventsRef"" DismissOnClick AutoDismiss AutoDismissTime=""TimeSpan.FromSeconds(6)""
             OnShow=""HandleOnShow"" OnDismiss=""HandleOnDismiss"" OnItemClick=""HandleOnItemClick"" />
<BitButton OnClick=""OpenEvents"">Open SnackBar</BitButton>

<ul>
    @foreach (var log in eventLogs)
    {
        <li>@log</li>
    }
</ul>";
    private readonly string example11CsharpCode = @"
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

    private readonly string example12RazorCode = @"
<BitSnackBar @ref=""controlRef"" AutoDismiss AutoDismissTime=""TimeSpan.FromSeconds(6)"" ShowIcon
             OnShow=""HandleControlChange"" OnDismiss=""HandleControlChange"" />

<BitButton OnClick=""StartUpload"">Start upload</BitButton>
<BitButton OnClick=""CompleteUpload"" Disabled=""uploadItem is null"">Complete upload</BitButton>

<BitButton OnClick=""TrackExport"">Track a task</BitButton>
<BitButton OnClick=""TrackFailingExport"">Track a failing task</BitButton>

<BitButton OnClick=""PauseAll"">Pause</BitButton>
<BitButton OnClick=""ResumeAll"">Resume</BitButton>
<BitButton OnClick=""ClearAll"">Clear all</BitButton>

<div>Showing: <b>@controlRef?.Items.Count</b></div>";
    private readonly string example12CsharpCode = @"
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
        IsLoading = true
    });
}

private async Task CompleteUpload()
{
    if (uploadItem is null) return;

    uploadItem.Title = ""Upload complete"";
    uploadItem.Color = BitColor.Success;
    uploadItem.IsLoading = false;

    await controlRef.Update(uploadItem);

    uploadItem = null;
}

private async Task TrackExport()
{
    await controlRef.Track(ExportAsync(), ""Exporting..."", rows => $""Exported {rows} rows"", ex => ""Export failed"", ""orders.csv"");
}

private async Task TrackFailingExport()
{
    try
    {
        await controlRef.Track(FailingExportAsync(), ""Exporting..."", ""Exported"", ex => $""Export failed: {ex.Message}"", ""orders.csv"");
    }
    catch (InvalidOperationException)
    {
        // Already reported on screen; the failure is still the caller's to handle.
    }
}

private static async Task<int> ExportAsync()
{
    await Task.Delay(2000);
    return 1250;
}

private static async Task FailingExportAsync()
{
    await Task.Delay(2000);
    throw new InvalidOperationException(""the disk is full"");
}

private async Task PauseAll()
{
    foreach (var item in controlRef.Items)
    {
        await controlRef.Pause(item);
    }
}

private async Task ResumeAll()
{
    foreach (var item in controlRef.Items)
    {
        await controlRef.Resume(item);
    }
}

private async Task ClearAll() => await controlRef.Clear();";

    private readonly string example13RazorCode = @"
<BitSnackBar ServiceHost ShowIcon AutoDismiss AutoDismissTime=""TimeSpan.FromSeconds(5)"" />

<BitButton OnClick=""SaveThroughService"">Save</BitButton>
<BitButton OnClick=""FailThroughService"">Fail</BitButton>";
    private readonly string example13CsharpCode = @"
// Program.cs: builder.Services.AddBitBlazorUIServices();

[Inject] private BitSnackBarService snackBarService { get; set; } = default!;

private async Task SaveThroughService()
{
    await snackBarService.Success(""Saved"", ""Shown from a component that has no reference to the host."");
}

private async Task FailThroughService()
{
    await snackBarService.Error(""Save failed"", ""The same service reports problems too."");
}";

    private readonly string example14RazorCode = @"
<BitSnackBar @ref=""a11yRef"" ShowIcon AriaLabel=""Demo notifications"" DismissAriaLabel=""Dismiss notification"" />

<BitButton OnClick=""OpenPoliteA11y"">Polite (status)</BitButton>
<BitButton OnClick=""OpenAssertiveA11y"">Assertive (alert)</BitButton>
<BitButton OnClick=""OpenAnnounceText"">Custom announcement</BitButton>
<BitButton OnClick=""OpenSilentA11y"">Unannounced</BitButton>

<BitSnackBar @ref=""hotkeyRef"" Hotkey=""@([""F8""])""
             AutoDismiss AutoDismissTime=""TimeSpan.FromSeconds(30)"" ShowIcon>
    <ActionsTemplate Context=""item"">
        <BitButton Variant=""BitVariant.Text"" Color=""BitColor.TertiaryBackground""
                   OnClick=""() => hotkeyRef.Close(item)"">Got it</BitButton>
    </ActionsTemplate>
</BitSnackBar>
<BitButton OnClick=""OpenHotkey"">Open, then press F8</BitButton>
<BitButton OnClick=""FocusSnackBars"">Focus the region from code</BitButton>";
    private readonly string example14CsharpCode = @"
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
}

private BitSnackBar hotkeyRef = default!;

private async Task OpenHotkey()
{
    await hotkeyRef.Info(""Report ready"", ""Press F8 to jump here, then Tab to the action."");
}

private async Task FocusSnackBars() => await hotkeyRef.FocusAsync();";

    private readonly string example15RazorCode = @"
<BitParams Parameters=""snackBarParams"">
    <BitSnackBar @ref=""cascadedRef"" />
    <BitSnackBar @ref=""cascadedOwnRef"" Position=""BitPosition.BottomCenter"" />
</BitParams>
<BitSnackBar @ref=""uncascadedRef"" />

<BitButton OnClick=""OpenCascaded"">Takes the position, icon and lifetime from the cascade</BitButton>
<BitButton OnClick=""OpenCascadedOwn"">Its own position, the cascaded rest</BitButton>
<BitButton OnClick=""OpenUncascaded"">Outside the cascade, back to the defaults</BitButton>";
    private readonly string example15CsharpCode = @"
private readonly BitSnackBarParams[] snackBarParams =
[
    new()
    {
        Position = BitPosition.TopCenter,
        ShowIcon = true,
        AutoDismiss = true,
        AutoDismissTime = TimeSpan.FromSeconds(4),
    }
];
private BitSnackBar cascadedRef = default!;
private BitSnackBar cascadedOwnRef = default!;
private BitSnackBar uncascadedRef = default!;

private async Task OpenCascaded() => await cascadedRef.Success(""Cascaded"", ""Top center, with an icon, for 4 seconds."");

private async Task OpenCascadedOwn() => await cascadedOwnRef.Success(""Own position"", ""Bottom center, the rest from the cascade."");

private async Task OpenUncascaded() => await uncascadedRef.Success(""Defaults"", ""Bottom end, no icon, until dismissed."");";

    private readonly string example16RazorCode = @"
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
    private readonly string example16CsharpCode = @"
private BitSnackBar colorRef = default!;
private BitVariant colorVariant = BitVariant.Fill;";

    private readonly string example17RazorCode = @"
<link rel=""stylesheet"" href=""https://cdnjs.cloudflare.com/ajax/libs/font-awesome/7.0.1/css/all.min.css"" />
<link rel=""stylesheet"" href=""https://cdn.jsdelivr.net/npm/bootstrap-icons@1.11.3/font/bootstrap-icons.min.css"" />

<BitSnackBar @ref=""iconFaRef"" ShowIcon Icon=""@BitIconInfo.Fa(""solid circle-info"")"" DismissIcon=""@BitIconInfo.Fa(""solid xmark"")"" />
<BitButton OnClick=""OpenIconFa"">FontAwesome</BitButton>

<BitSnackBar @ref=""iconBiRef"" ShowIcon Icon=""@BitIconInfo.Bi(""info-circle-fill"")"" DismissIcon=""@BitIconInfo.Bi(""x-lg"")"" />
<BitButton OnClick=""OpenIconBi"">Bootstrap</BitButton>

<BitSnackBar @ref=""iconCssRef"" ShowIcon Icon=""@BitIconInfo.Css(""fa-solid fa-bell"")"" DismissIcon=""@(""bi bi-x-circle"")"" />
<BitButton OnClick=""OpenIconCss"">CSS classes</BitButton>";
    private readonly string example17CsharpCode = @"
private BitSnackBar iconFaRef = default!;
private BitSnackBar iconBiRef = default!;
private BitSnackBar iconCssRef = default!;

private async Task OpenIconFa() => await iconFaRef.Info(""FontAwesome"", ""Both icons come from FontAwesome."");

private async Task OpenIconBi() => await iconBiRef.Info(""Bootstrap"", ""Both icons come from Bootstrap Icons."");

private async Task OpenIconCss() => await iconCssRef.Info(""CSS classes"", ""Icons from plain CSS class names."");";

    private readonly string example18RazorCode = @"
<BitSnackBar @ref=""sizeRef"" ShowIcon Size=""size"" />

<BitChoiceGroup @bind-Value=""size"" Label=""Size"" Horizontal TItem=""BitChoiceGroupOption<BitSize>"" TValue=""BitSize"">
    <BitChoiceGroupOption Text=""Small"" Value=""BitSize.Small"" />
    <BitChoiceGroupOption Text=""Medium"" Value=""BitSize.Medium"" />
    <BitChoiceGroupOption Text=""Large"" Value=""BitSize.Large"" />
</BitChoiceGroup>

<BitButton OnClick=""OpenSize"">Open SnackBar</BitButton>";
    private readonly string example18CsharpCode = @"
private BitSnackBar sizeRef = default!;
private BitSize size = BitSize.Medium;

private async Task OpenSize() => await sizeRef.Info($""{size}"", $""The {size.ToString().ToLowerInvariant()} size snack bar."");";

    private readonly string example19RazorCode = @"
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
<BitButton OnClick=""OpenSnackBarStyle"">Item style</BitButton>
<BitButton OnClick=""OpenSnackBarClass"">Item class</BitButton>

<BitSnackBar @ref=""snackBarStylesRef""
             Styles=""@(new() { Container = ""width: 16rem; background-color: purple;"",
                               Header = ""background-color: rebeccapurple; padding: 0.2rem;"" })"" />
<BitButton OnClick=""OpenSnackBarStyles"">Styles</BitButton>

<BitSnackBar @ref=""snackBarClassesRef"" AutoDismiss
             Classes=""@(new() { Container = ""custom-container"",
                                ProgressBar = ""custom-progress"" })"" />
<BitButton OnClick=""OpenSnackBarClasses"">Classes</BitButton>

<BitSnackBar @ref=""cssVarsRef"" ShowIcon AutoDismiss AutoDismissTime=""TimeSpan.FromSeconds(6)""
             Style=""--bit-SnackBar-background: #1e1b4b; --bit-SnackBar-color: #e0e7ff; --bit-SnackBar-icon-color: #a5b4fc; --bit-SnackBar-progress-color: #818cf8; --bit-SnackBar-radius: 1rem; --bit-SnackBar-padding: 1rem 1.25rem; --bit-SnackBar-min-width: 18rem; --bit-SnackBar-gap: 1rem;"" />
<BitButton OnClick=""OpenCssVars"">CSS variables</BitButton>";
    private readonly string example19CsharpCode = @"
private BitSnackBar snackBarStyleRef = default!;
private BitSnackBar snackBarStylesRef = default!;
private BitSnackBar snackBarClassesRef = default!;
private BitSnackBar cssVarsRef = default!;

private async Task OpenSnackBarStyle()
{
    await snackBarStyleRef.Show(""This is title"", ""This is body"", cssStyle: ""background-color: dodgerblue; border-radius: 0.5rem;"");
}

private async Task OpenSnackBarClass()
{
    await snackBarStyleRef.Show(""This is title"", ""This is body"", cssClass: ""custom-class"");
}

private async Task OpenSnackBarStyles()
{
    await snackBarStylesRef.Show(""This is title"", ""This is body"");
}

private async Task OpenSnackBarClasses()
{
    await snackBarClassesRef.Show(""This is title"", ""This is body"");
}

private async Task OpenCssVars()
{
    await cssVarsRef.Info(""Restyled"", ""Background, text, icon, bar, radius, padding and width from CSS variables."");
}";

    private readonly string example20RazorCode = @"
<BitSnackBar @ref=""rtlRef"" Dir=""BitDir.Rtl"" ShowIcon Position=""BitPosition.BottomStart""
             AutoDismiss AutoDismissTime=""TimeSpan.FromSeconds(5)"" />
<BitButton Dir=""BitDir.Rtl"" OnClick=""OpenRtl"">نمایش پیام</BitButton>";
    private readonly string example20CsharpCode = @"
private BitSnackBar rtlRef = default!;

private async Task OpenRtl()
{
    await rtlRef.Success(""عنوان پیام"", ""این متن پیام است."");
}";
}
