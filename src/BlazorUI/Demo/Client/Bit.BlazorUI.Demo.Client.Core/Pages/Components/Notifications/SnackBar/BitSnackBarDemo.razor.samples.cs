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
<BitSnackBar @ref=""basicSnackBarRef""
             Dir=""direction""
             Position=""@basicSnackBarPosition""
             AutoDismiss=""@basicSnackBarAutoDismiss""
             AutoDismissTime=""TimeSpan.FromSeconds(basicSnackBarDismissSeconds)"" />

<BitButton OnClick=""OpenBasicSnackBar"">Show</BitButton>

<BitChoiceGroup @bind-Value=""basicSnackBarColor"" Label=""Type"" TItem=""BitChoiceGroupOption<BitColor>"" TValue=""BitColor"">
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

<BitToggle @bind-Value=""basicSnackBarAutoDismiss"" Label=""Auto Dismiss"" />
<BitNumberField @bind-Value=""basicSnackBarDismissSeconds"" Step=""1"" Min=""1"" Label=""Dismiss Time (based on second)"" />

<BitToggle @bind-Value=""basicSnackBarMultiline"" Label=""Multiline"" Inline />

<BitTextField @bind-Value=""basicSnackBarTitle"" Label=""Title"" DefaultValue=""Title"" />
<BitTextField @bind-Value=""basicSnackBarBody"" Label=""Body"" Multiline Rows=""6"" DefaultValue=""This is a body!"" />";
    private readonly string example2CsharpCode = @"
private BitDir direction;
private bool basicSnackBarMultiline;
private bool basicSnackBarAutoDismiss;
private int basicSnackBarDismissSeconds = 3;
private BitSnackBar customizationRef = default!;
private string basicSnackBarBody = ""This is body"";
private string basicSnackBarTitle = ""This is title"";
private BitColor basicSnackBarColor = BitColor.Info;
private BitSnackBarPosition basicSnackBarPosition = BitSnackBarPosition.BottomEnd;

private async Task OpenBasicSnackBar()
{
    await basicSnackBarRef.Show(basicSnackBarTitle, basicSnackBarBody, basicSnackBarColor);
}";

    private readonly string example3RazorCode = @"
<BitSnackBar @ref=""persistentRef"" Persistent />
<BitButton OnClick=""OpenPersistentSnackBar"">Open SnackBar</BitButton>
<BitButton OnClick=""ClosePersistentSnackBar"">Close SnackBar</BitButton>";
    private readonly string example3CsharpCode = @"
private BitSnackBarItem? persistentItem;
private BitSnackBar persistentRef = default!;
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
}";

    private readonly string example4RazorCode = @"
<BitSnackBar @ref=""dismissIconNameRef"" DismissIconName=""@BitIconName.Go"" />
<BitButton OnClick=""OpenDismissIconName"">Dismiss Icon Name</BitButton>

<BitSnackBar @ref=""titleTemplateRef"" AutoDismiss=""false"">
    <TitleTemplate Context=""title"">
        <div style=""display: flex; flex-direction: row; gap: 10px;"">
            <span>@title</span>
            <BitProgress Thickness=""20"" Style=""width: 40px;"" Indeterminate />
        </div>
    </TitleTemplate>
</BitSnackBar>
<BitButton OnClick=""OpenTitleTemplate"">Title Template</BitButton>

<BitSnackBar @ref=""bodyTemplateRef"" AutoDismiss=""false"">
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
<BitButton OnClick=""OpenBodyTemplate"">Body Template</BitButton>";
    private readonly string example4CsharpCode = @"
private string? bodyTemplateAnswer;
private BitSnackBar bodyTemplateRef = default!;
private BitSnackBar titleTemplateRef = default!;
private BitSnackBar dismissIconNameRef = default!;

private async Task OpenDismissIconName()
{
    await dismissIconNameRef.Success(""This is title"", ""This is body"");
}

private async Task OpenTitleTemplate()
{
    await titleTemplateRef.Warning(""This is title"", ""This is body"");
}

private async Task OpenBodyTemplate()
{
    await bodyTemplateRef.Error(""This is title"", ""This is body"");
}";

    private readonly string example5RazorCode = @"
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
<BitButton OnClick=""OpenSnackBarClass"">Custom style</BitButton>

<BitSnackBar @ref=""snackBarStylesRef""
             Styles=""@(new() { Container = ""width: 16rem; background-color: purple;"",
                               Header = ""background-color: rebeccapurple; padding: 0.2rem;"" })"" />
<BitButton OnClick=""OpenSnackBarStyles"">Custom styles</BitButton>

<BitSnackBar @ref=""snackBarClassesRef"" AutoDismiss
             Classes=""@(new() { Container = ""custom-container"",
                                ProgressBar = ""custom-progress"" })"" />
<BitButton OnClick=""OpenSnackBarClasses"">Custom classes</BitButton>";
    private readonly string example5CsharpCode = @"
private BitSnackBar snackBarStyleRef = default!;
private BitSnackBar snackBarClassRef = default!;
private BitSnackBar snackBarStylesRef = default!;
private BitSnackBar snackBarClassesRef = default!;

private async Task OpenSnackBarStyle()
{
    await snackBarClassRef.Show(""This is title"", ""This is body"", cssStyle: ""background-color: dodgerblue; border-radius: 0.5rem;"");
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
}";
}
