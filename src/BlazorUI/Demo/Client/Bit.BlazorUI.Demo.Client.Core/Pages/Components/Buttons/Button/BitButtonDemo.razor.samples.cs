namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Buttons.Button;

public partial class BitButtonDemo
{
    private readonly string example1RazorCode = @"
<BitButton>Button</BitButton>
<BitButton SecondaryText=""this is the secondary text"">Primary text</BitButton>
<BitButton SecondaryText=""secondary text only"" />";

    private readonly string example2RazorCode = @"
<BitButton Variant=""BitVariant.Fill"">Fill</BitButton>
<BitButton Variant=""BitVariant.Outline"">Outline</BitButton>
<BitButton Variant=""BitVariant.Text"">Text</BitButton>

<BitButton Variant=""BitVariant.Fill"" SecondaryText=""this is the secondary text"">Fill</BitButton>
<BitButton Variant=""BitVariant.Outline"" SecondaryText=""this is the secondary text"">Outline</BitButton>
<BitButton Variant=""BitVariant.Text"" SecondaryText=""this is the secondary text"">Text</BitButton>


<BitButton Variant=""BitVariant.Fill"" IsEnabled=""false"">Fill</BitButton>
<BitButton Variant=""BitVariant.Outline"" IsEnabled=""false"">Outline</BitButton>
<BitButton Variant=""BitVariant.Text"" IsEnabled=""false"">Text</BitButton>


<BitButton Rounded Variant=""BitVariant.Fill"">Fill</BitButton>
<BitButton Rounded Variant=""BitVariant.Outline"">Outline</BitButton>
<BitButton Rounded Variant=""BitVariant.Text"">Text</BitButton>";

    private readonly string example3RazorCode = @"
<BitButton IconName=""@BitIconName.Emoji"" Variant=""BitVariant.Fill"">Start</BitButton>
<BitButton IconName=""@BitIconName.Emoji"" Variant=""BitVariant.Outline"" SecondaryText=""this is the secondary text"">Start</BitButton>
<BitButton IconName=""@BitIconName.Emoji2"" Variant=""BitVariant.Text"" IconPosition=""BitIconPosition.End"">End</BitButton>

<BitButton IconUrl=""/images/bit-logo.svg"" Variant=""BitVariant.Fill"">Start</BitButton>
<BitButton IconUrl=""/images/bit-logo.svg"" Variant=""BitVariant.Outline"" SecondaryText=""IconUrl"">Start</BitButton>
<BitButton IconUrl=""/images/bit-logo.svg"" Variant=""BitVariant.Text"" IconPosition=""BitIconPosition.End"">End</BitButton>


<BitButton IconOnly IconName=""@BitIconName.Add"" Variant=""BitVariant.Fill"">Add</BitButton>
<BitButton IconOnly IconName=""@BitIconName.Edit"" Variant=""BitVariant.Outline"">Edit</BitButton>
<BitButton IconOnly AriaLabel=""Delete"" IconName=""@BitIconName.Delete"" Variant=""BitVariant.Text"" />

<BitButton Rounded IconOnly IconName=""@BitIconName.Add"" Variant=""BitVariant.Fill"">Add</BitButton>
<BitButton Rounded IconOnly IconName=""@BitIconName.Edit"" Variant=""BitVariant.Outline"">Edit</BitButton>
<BitButton Rounded IconOnly AriaLabel=""Delete"" IconName=""@BitIconName.Delete"" Variant=""BitVariant.Text"" />";

    private readonly string example4RazorCode = @"
<BitButton IsLoading=""fillIsLoading"" Variant=""BitVariant.Fill"" OnClick=""LoadingFillClick"">
    Click me
</BitButton>

<BitButton IsLoading=""outlineIsLoading"" Variant=""BitVariant.Outline"" LoadingLabel=""Loading..."" OnClick=""LoadingOutlineClick"">
    Click me
</BitButton>

<BitButton IsLoading=""textIsLoading"" Variant=""BitVariant.Text"" SecondaryText=""this is the secondary text"" OnClick=""LoadingTextClick"">
    Click me
</BitButton>


<BitButton IsLoading LoadingLabel=""End..."" Variant=""BitVariant.Outline"" LoadingLabelPosition=""BitLabelPosition.End"">End</BitButton>
<BitButton IsLoading LoadingLabel=""Start..."" Variant=""BitVariant.Outline"" LoadingLabelPosition=""BitLabelPosition.Start"">Start</BitButton>
<BitButton IsLoading LoadingLabel=""Top..."" Variant=""BitVariant.Outline"" LoadingLabelPosition=""BitLabelPosition.Top"">Top</BitButton>
<BitButton IsLoading LoadingLabel=""Bottom..."" Variant=""BitVariant.Outline"" LoadingLabelPosition=""BitLabelPosition.Bottom"">Bottom</BitButton>


<BitButton OnClick=""AutoLoadingClick"" AutoLoading>Click me</BitButton>
<div>AutoLoading click count: @autoLoadCount</div>

<BitButton OnClick=""AutoLoadingReclick"" AutoLoading Reclickable>Reclickable</BitButton>
<div>Re-clickable AutoLoading click count: @reclickableAutoLoadCount</div>


<BitButton AutoLoading OnClick=""FastOperation"" Variant=""BitVariant.Outline"">
    Without a delay
</BitButton>

<BitButton AutoLoading LoadingDelay=""500"" OnClick=""FastOperation"" Variant=""BitVariant.Outline"">
    LoadingDelay=""500""
</BitButton>";

    private readonly string example4CsharpCode = @"
private bool fillIsLoading;
private async Task LoadingFillClick()
{
    fillIsLoading = true;
    await Task.Delay(3000);
    fillIsLoading = false;
}

private bool outlineIsLoading;
private async Task LoadingOutlineClick()
{
    outlineIsLoading = true;
    await Task.Delay(3000);
    outlineIsLoading = false;
}

private bool textIsLoading;
private async Task LoadingTextClick()
{
    textIsLoading = true;
    await Task.Delay(3000);
    textIsLoading = false;
}

private int autoLoadCount;
private async Task AutoLoadingClick()
{
    autoLoadCount++;
    await Task.Delay(3000);
}

// A re-clickable button reports the loading state its click arrived in, which is how a handler
// that is being re-entered knows to abandon the run that is still in flight.
private int reclickableAutoLoadCount;
private TaskCompletionSource clickTsc = new();
private CancellationTokenSource delayCts = new();
private Task AutoLoadingReclick(bool isLoading)
{
    if (isLoading)
    {
        clickTsc.TrySetException(new TaskCanceledException());
        delayCts.Cancel();
    }

    delayCts = new();
    clickTsc = new();

    reclickableAutoLoadCount++;

    _ = Task.Delay(3000, delayCts.Token).ContinueWith(async delayTask =>
    {
        await delayTask;
        clickTsc.TrySetResult();
    });

    return clickTsc.Task;
}

// Finishes inside the 500ms delay of the second button, so only the first one ever shows a spinner.
private async Task FastOperation() => await Task.Delay(250);";

    private readonly string example5RazorCode = @"
<BitButton Href=""https://bitplatform.dev"" Target=""_blank"" IconName=""@BitIconName.Globe"" Variant=""BitVariant.Outline"">
    Open bitplatform.dev
</BitButton>

<BitButton Download=""bit-logo.svg"" Href=""/images/bit-logo.svg"" IconName=""@BitIconName.Download"" Variant=""BitVariant.Outline"">
    Download the bit logo
</BitButton>


<BitButton Rel=""BitLinkRels.NoFollow"" Href=""https://bitplatform.dev"" Target=""_blank"" IconName=""@BitIconName.Globe"" Variant=""BitVariant.Outline"">
    nofollow
</BitButton>

<BitButton Rel=""BitLinkRels.NoFollow | BitLinkRels.NoReferrer"" Href=""https://bitplatform.dev"" Target=""_blank"" IconName=""@BitIconName.Globe"" Variant=""BitVariant.Outline"">
    nofollow & noreferrer
</BitButton>";

    private readonly string example6RazorCode = @"
@if (formIsValidSubmit is false)
{
    <EditForm Model=""buttonValidationModel"" OnValidSubmit=""HandleValidSubmit"" OnInvalidSubmit=""HandleInvalidSubmit"" novalidate>
        <DataAnnotationsValidator />

        <BitTextField Label=""Required"" Required @bind-Value=""buttonValidationModel.RequiredText"" />
        <ValidationMessage For=""() => buttonValidationModel.RequiredText"" style=""color:red"" />

        <BitTextField Label=""Non Required"" @bind-Value=""buttonValidationModel.NonRequiredText"" />

        <div class=""example-content"">
            <BitButton ButtonType=""BitButtonType.Submit"">Submit</BitButton>
            <BitButton ButtonType=""BitButtonType.Reset"" Variant=""BitVariant.Outline"">Reset</BitButton>
            <BitButton ButtonType=""BitButtonType.Button"" Variant=""BitVariant.Text"">Button</BitButton>
        </div>
    </EditForm>
}
else
{
    <BitMessage Color=""BitColor.Success"">The form submitted successfully.</BitMessage>
}


@if (externalFormSubmitted is false)
{
    <EditForm id=""demo-external-form"" Model=""externalFormModel"" OnValidSubmit=""HandleExternalFormValidSubmit"" novalidate>
        <DataAnnotationsValidator />

        <BitTextField Label=""Required"" Required @bind-Value=""externalFormModel.RequiredText"" />
        <ValidationMessage For=""() => externalFormModel.RequiredText"" style=""color:red"" />
    </EditForm>

    <BitButton FormId=""demo-external-form"" ButtonType=""BitButtonType.Submit"">
        External submit
    </BitButton>
}
else
{
    <BitMessage Color=""BitColor.Success"">The external form submitted successfully.</BitMessage>
}";

    private readonly string example6CsharpCode = @"
public class ButtonValidationModel
{
    [Required(ErrorMessage = ""Enter a text"")]
    public string? RequiredText { get; set; }

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
}

private bool externalFormSubmitted;
private ButtonValidationModel externalFormModel = new();

private async Task HandleExternalFormValidSubmit()
{
    externalFormSubmitted = true;

    await Task.Delay(2000);

    externalFormModel = new();

    externalFormSubmitted = false;

    StateHasChanged();
}";

    private readonly string example7RazorCode = @"
<BitButton Class=""custom-content"">
    <BitIcon IconName=""@BitIconName.Airplane"" Color=""BitColor.Tertiary"" />
    <span>A primary template</span>
    <BitRippleLoading CustomSize=""20"" Color=""BitColor.Tertiary"" />
</BitButton>

<BitButton Class=""custom-content"" Variant=""BitVariant.Outline"">
    <PrimaryTemplate>Primary text</PrimaryTemplate>
    <SecondaryTemplate>
        <BitIcon IconName=""@BitIconName.Accept"" />
        <span>A secondary template</span>
        <BitRollerLoading CustomSize=""20"" />
    </SecondaryTemplate>
</BitButton>


<BitButton IsLoading=""templateIsLoading"" OnClick=""LoadingTemplateClick"">
    <PrimaryTemplate>Click me</PrimaryTemplate>
    <LoadingTemplate>
        <div class=""custom-loading"">
            <BitEllipsisLoading CustomSize=""32"" Color=""BitColor.Tertiary"" />
            <span>Wait...</span>
        </div>
    </LoadingTemplate>
</BitButton>";

    private readonly string example7CsharpCode = @"
private bool templateIsLoading;
private async Task LoadingTemplateClick()
{
    templateIsLoading = true;
    await Task.Delay(3000);
    templateIsLoading = false;
}";

    private readonly string example8RazorCode = @"
<BitButton OnClick=""() => clickCounter++"">Click me (@clickCounter)</BitButton>


<div class=""example-content"" @onclick=""() => parentClickCounter++"">
    <BitButton StopPropagation OnClick=""() => buttonClickCounter++"">With StopPropagation</BitButton>
    <BitButton OnClick=""() => buttonClickCounter++"">Without StopPropagation</BitButton>
    <div>Button clicks: @buttonClickCounter, Parent clicks: @parentClickCounter</div>
</div>";

    private readonly string example8CsharpCode = @"
private int clickCounter;

private int parentClickCounter;
private int buttonClickCounter;";

    private readonly string example9RazorCode = @"
<BitToggle @bind-Value=""noWrap"" Label=""NoWrap"" />

<BitButton FullWidth IconName=""@BitIconName.Save"">Full width button</BitButton>

<div class=""narrow-container"">
    <BitButton FullWidth NoWrap=""noWrap"" IconName=""@BitIconName.Mail"" SecondaryText=""and every address it was also sent to"">
        Reply to everyone on this unusually long conversation
    </BitButton>
</div>";

    private readonly string example9CsharpCode = @"
private bool noWrap = true;";

    private const string example9ScssCode = @"
// Narrow enough that the label of the button inside it has to wrap, or be clamped by NoWrap.
.narrow-container {
    max-width: 18rem;
}";

    private readonly DemoCodeFile[] example9CodeFiles =
    [
        new("BitButtonDemo.razor.scss", example9ScssCode),
    ];

    private readonly string example10RazorCode = @"
<div class=""fixed-color-surface"">
    <BitButton Variant=""BitVariant.Outline"" Color=""BitColor.TertiaryBackground"" IconName=""@BitIconName.Emoji2"">
        Default
    </BitButton>

    <BitButton FixedColor Variant=""BitVariant.Outline"" Color=""BitColor.TertiaryBackground"" IconName=""@BitIconName.Emoji2"">
        FixedColor
    </BitButton>

    <BitButton FixedColor IconOnly AriaLabel=""Emoji"" Variant=""BitVariant.Text"" Color=""BitColor.TertiaryBackground"" IconName=""@BitIconName.Emoji2"" />
</div>";

    private const string example10ScssCode = @"
// A surface whose color the buttons on it are meant to keep matching, which is the case FixedColor is for.
.fixed-color-surface {
    gap: 0.5rem;
    display: flex;
    flex-wrap: wrap;
    padding: 1rem;
    border-radius: 0.25rem;
    align-items: flex-start;
    background-color: var(--bit-clr-bg-ter);
}";

    private readonly DemoCodeFile[] example10CodeFiles =
    [
        new("BitButtonDemo.razor.scss", example10ScssCode),
    ];

    private readonly string example11RazorCode = @"
<BitDropdown Label=""FloatPosition"" Items=""floatPositionList"" @bind-Value=""floatPosition"" FitWidth />
<BitTextField Label=""FloatOffset"" @bind-Value=""floatOffset"" Immediate />

<BitButton Rounded
           IconOnly
           AriaLabel=""Add""
           Size=""BitSize.Large""
           IconName=""@BitIconName.Add""
           OnClick=""ScrollToFloat""
           Float Draggable
           FloatPosition=""floatPosition""
           FloatOffset=""@floatOffset"" />


<div class=""float-container"">
    <BitButton IconOnly
               AriaLabel=""Edit""
               IconName=""@BitIconName.Edit""
               Draggable
               FloatAbsolute
               FloatPosition=""floatPosition""
               FloatOffset=""@floatOffset"" />
    <div class=""float-container-content"">
        Every story starts with a blank canvas, a quiet space waiting to be filled with ideas, emotions, and dreams.
        ...
    </div>
</div>";

    private readonly string example11CsharpCode = @"
private string? floatOffset = ""63px"";
private BitPosition floatPosition = BitPosition.BottomRight;

[Inject] private IJSRuntime _js { get; set; } = default!;
private async Task ScrollToFloat() => await _js.ScrollToElement(""example11"");

private readonly List<BitDropdownItem<BitPosition>> floatPositionList = Enum.GetValues<BitPosition>()
                                                                            .Cast<BitPosition>()
                                                                            .Select(enumValue => new BitDropdownItem<BitPosition>
                                                                            {
                                                                                Value = enumValue,
                                                                                Text = enumValue.ToString()
                                                                            })
                                                                            .ToList();";

    private const string example11ScssCode = @"
.float-container {
    position: relative;
    border: 1px solid var(--bit-clr-brd-sec);
}

.float-container-content {
    height: 300px;
    overflow: auto;
    padding: 0.5rem;
}";

    private readonly DemoCodeFile[] example11CodeFiles =
    [
        new("BitButtonDemo.razor.scss", example11ScssCode),
    ];

    private readonly string example12RazorCode = @"
<BitButton Title=""Save your changes"" IconName=""@BitIconName.Save"" Variant=""BitVariant.Outline"">
    Hover me
</BitButton>

<BitButton IconOnly Title=""Delete"" AriaLabel=""Delete"" Color=""BitColor.Error"" IconName=""@BitIconName.Delete"" />


<BitButton IconName=""@BitIconName.Download""
           Variant=""BitVariant.Outline""
           AriaDescription=""SVG, 12 kilobytes""
           Href=""/images/bit-logo.svg""
           Download=""bit-logo.svg"">
    Download the bit logo
</BitButton>


<BitButton IsEnabled=""false"" IconName=""@BitIconName.Blocked"" Title=""Pick a plan first"">
    Disabled (still focusable)
</BitButton>

<BitButton IsEnabled=""false"" AllowDisabledFocus=""false"" IconName=""@BitIconName.Blocked"">
    Disabled (skipped by Tab)
</BitButton>


<BitButton Variant=""BitVariant.Outline""
           IconName=""@BitIconName.CaretRightSolid8""
           OnClick=""@(async () => await focusButtonRef.FocusAsync())"">
    Focus the next button
</BitButton>

<BitButton @ref=""focusButtonRef"" Color=""BitColor.Success"" IconName=""@BitIconName.Flag"">
    Focus lands here
</BitButton>";

    private readonly string example12CsharpCode = @"
private BitButton focusButtonRef = default!;";

    private readonly string example13RazorCode = @"
<BitParams Parameters=""@buttonParams"">
    <BitButton IconName=""@BitIconName.Save"">Save</BitButton>
    <BitButton IconName=""@BitIconName.Copy"">Duplicate</BitButton>
    <BitButton Color=""BitColor.Error"" IconName=""@BitIconName.Delete"">Delete</BitButton>
</BitParams>

<BitButton IconName=""@BitIconName.Save"">Outside the cascade, and back to the defaults</BitButton>";

    private readonly string example13CsharpCode = @"
private readonly BitButtonParams[] buttonParams =
[
    new()
    {
        Size = BitSize.Small,
        Rounded = true,
        Variant = BitVariant.Outline,
        Color = BitColor.Secondary,
    }
];";

    private readonly string example14RazorCode = @"
<BitButton Color=""BitColor.Primary"">Primary</BitButton>
<BitButton Color=""BitColor.Primary"" Variant=""BitVariant.Outline"">Primary</BitButton>
<BitButton Color=""BitColor.Primary"" Variant=""BitVariant.Text"">Primary</BitButton>

<BitButton Color=""BitColor.Secondary"">Secondary</BitButton>
<BitButton Color=""BitColor.Secondary"" Variant=""BitVariant.Outline"">Secondary</BitButton>
<BitButton Color=""BitColor.Secondary"" Variant=""BitVariant.Text"">Secondary</BitButton>

<BitButton Color=""BitColor.Tertiary"">Tertiary</BitButton>
<BitButton Color=""BitColor.Tertiary"" Variant=""BitVariant.Outline"">Tertiary</BitButton>
<BitButton Color=""BitColor.Tertiary"" Variant=""BitVariant.Text"">Tertiary</BitButton>

<BitButton Color=""BitColor.Info"">Info</BitButton>
<BitButton Color=""BitColor.Info"" Variant=""BitVariant.Outline"">Info</BitButton>
<BitButton Color=""BitColor.Info"" Variant=""BitVariant.Text"">Info</BitButton>

<BitButton Color=""BitColor.Success"">Success</BitButton>
<BitButton Color=""BitColor.Success"" Variant=""BitVariant.Outline"">Success</BitButton>
<BitButton Color=""BitColor.Success"" Variant=""BitVariant.Text"">Success</BitButton>

<BitButton Color=""BitColor.Warning"">Warning</BitButton>
<BitButton Color=""BitColor.Warning"" Variant=""BitVariant.Outline"">Warning</BitButton>
<BitButton Color=""BitColor.Warning"" Variant=""BitVariant.Text"">Warning</BitButton>

<BitButton Color=""BitColor.SevereWarning"">SevereWarning</BitButton>
<BitButton Color=""BitColor.SevereWarning"" Variant=""BitVariant.Outline"">SevereWarning</BitButton>
<BitButton Color=""BitColor.SevereWarning"" Variant=""BitVariant.Text"">SevereWarning</BitButton>

<BitButton Color=""BitColor.Error"">Error</BitButton>
<BitButton Color=""BitColor.Error"" Variant=""BitVariant.Outline"">Error</BitButton>
<BitButton Color=""BitColor.Error"" Variant=""BitVariant.Text"">Error</BitButton>


<BitButton Color=""BitColor.PrimaryBackground"">PrimaryBackground</BitButton>
<BitButton Color=""BitColor.PrimaryBackground"" Variant=""BitVariant.Outline"">PrimaryBackground</BitButton>
<BitButton Color=""BitColor.PrimaryBackground"" Variant=""BitVariant.Text"">PrimaryBackground</BitButton>

<BitButton Color=""BitColor.SecondaryBackground"">SecondaryBackground</BitButton>
<BitButton Color=""BitColor.SecondaryBackground"" Variant=""BitVariant.Outline"">SecondaryBackground</BitButton>
<BitButton Color=""BitColor.SecondaryBackground"" Variant=""BitVariant.Text"">SecondaryBackground</BitButton>

<BitButton Color=""BitColor.TertiaryBackground"">TertiaryBackground</BitButton>
<BitButton Color=""BitColor.TertiaryBackground"" Variant=""BitVariant.Outline"">TertiaryBackground</BitButton>
<BitButton Color=""BitColor.TertiaryBackground"" Variant=""BitVariant.Text"">TertiaryBackground</BitButton>


<BitButton Color=""BitColor.PrimaryForeground"">PrimaryForeground</BitButton>
<BitButton Color=""BitColor.PrimaryForeground"" Variant=""BitVariant.Outline"">PrimaryForeground</BitButton>
<BitButton Color=""BitColor.PrimaryForeground"" Variant=""BitVariant.Text"">PrimaryForeground</BitButton>

<BitButton Color=""BitColor.SecondaryForeground"">SecondaryForeground</BitButton>
<BitButton Color=""BitColor.SecondaryForeground"" Variant=""BitVariant.Outline"">SecondaryForeground</BitButton>
<BitButton Color=""BitColor.SecondaryForeground"" Variant=""BitVariant.Text"">SecondaryForeground</BitButton>

<BitButton Color=""BitColor.TertiaryForeground"">TertiaryForeground</BitButton>
<BitButton Color=""BitColor.TertiaryForeground"" Variant=""BitVariant.Outline"">TertiaryForeground</BitButton>
<BitButton Color=""BitColor.TertiaryForeground"" Variant=""BitVariant.Text"">TertiaryForeground</BitButton>


<BitButton Color=""BitColor.PrimaryBorder"">PrimaryBorder</BitButton>
<BitButton Color=""BitColor.PrimaryBorder"" Variant=""BitVariant.Outline"">PrimaryBorder</BitButton>
<BitButton Color=""BitColor.PrimaryBorder"" Variant=""BitVariant.Text"">PrimaryBorder</BitButton>

<BitButton Color=""BitColor.SecondaryBorder"">SecondaryBorder</BitButton>
<BitButton Color=""BitColor.SecondaryBorder"" Variant=""BitVariant.Outline"">SecondaryBorder</BitButton>
<BitButton Color=""BitColor.SecondaryBorder"" Variant=""BitVariant.Text"">SecondaryBorder</BitButton>

<BitButton Color=""BitColor.TertiaryBorder"">TertiaryBorder</BitButton>
<BitButton Color=""BitColor.TertiaryBorder"" Variant=""BitVariant.Outline"">TertiaryBorder</BitButton>
<BitButton Color=""BitColor.TertiaryBorder"" Variant=""BitVariant.Text"">TertiaryBorder</BitButton>";

    private readonly string example15RazorCode = @"
<link rel=""stylesheet"" href=""https://cdnjs.cloudflare.com/ajax/libs/font-awesome/7.0.1/css/all.min.css"" />
<link rel=""stylesheet"" href=""https://cdn.jsdelivr.net/npm/bootstrap-icons@1.11.3/font/bootstrap-icons.min.css"" />

<BitButton Icon=""@(""fa-solid fa-house"")"" Variant=""BitVariant.Fill"">House</BitButton>
<BitButton Icon=""@BitIconInfo.Css(""fa-solid fa-heart"")"" Variant=""BitVariant.Outline"" Color=""BitColor.Secondary"">Heart</BitButton>
<BitButton Icon=""@BitIconInfo.Fa(""brands github"")"" Variant=""BitVariant.Text"" Color=""BitColor.Tertiary"">GitHub</BitButton>
<BitButton Icon=""@BitIconInfo.Fa(""solid rocket"")"" Variant=""BitVariant.Fill"" Color=""BitColor.Error"">Rocket</BitButton>

<BitButton Icon=""@(""bi bi-house-fill"")"" Variant=""BitVariant.Fill"">House</BitButton>
<BitButton Icon=""@BitIconInfo.Css(""bi bi-heart-fill"")"" Variant=""BitVariant.Outline"" Color=""BitColor.Secondary"">Heart</BitButton>
<BitButton Icon=""@BitIconInfo.Bi(""github"")"" Variant=""BitVariant.Text"" Color=""BitColor.Tertiary"">GitHub</BitButton>
<BitButton Icon=""@BitIconInfo.Bi(""gear-fill"")"" Variant=""BitVariant.Fill"" Color=""BitColor.Error"">Gear</BitButton>";

    private readonly string example16RazorCode = @"
<BitButton Size=""BitSize.Small"" IconOnly AriaLabel=""Emoji"" IconName=""@BitIconName.Emoji2"" />
<BitButton Size=""BitSize.Small"" IconName=""@BitIconName.Emoji2"">Fill</BitButton>
<BitButton Size=""BitSize.Small"" IconName=""@BitIconName.Emoji2"" Variant=""BitVariant.Outline"">Outline</BitButton>
<BitButton Size=""BitSize.Small"" IconName=""@BitIconName.Emoji2"" Variant=""BitVariant.Text"">Text</BitButton>
<BitButton Size=""BitSize.Small"" IsLoading LoadingLabel=""Loading..."" Variant=""BitVariant.Outline"">Loading</BitButton>
<BitButton Size=""BitSize.Small"" SecondaryText=""this is the secondary text"" IconName=""@BitIconName.Emoji2"">Compound</BitButton>

<BitButton Size=""BitSize.Medium"" IconOnly AriaLabel=""Emoji"" IconName=""@BitIconName.Emoji2"" />
<BitButton Size=""BitSize.Medium"" IconName=""@BitIconName.Emoji2"">Fill</BitButton>
<BitButton Size=""BitSize.Medium"" IconName=""@BitIconName.Emoji2"" Variant=""BitVariant.Outline"">Outline</BitButton>
<BitButton Size=""BitSize.Medium"" IconName=""@BitIconName.Emoji2"" Variant=""BitVariant.Text"">Text</BitButton>
<BitButton Size=""BitSize.Medium"" IsLoading LoadingLabel=""Loading..."" Variant=""BitVariant.Outline"">Loading</BitButton>
<BitButton Size=""BitSize.Medium"" SecondaryText=""this is the secondary text"" IconName=""@BitIconName.Emoji2"">Compound</BitButton>

<BitButton Size=""BitSize.Large"" IconOnly AriaLabel=""Emoji"" IconName=""@BitIconName.Emoji2"" />
<BitButton Size=""BitSize.Large"" IconName=""@BitIconName.Emoji2"">Fill</BitButton>
<BitButton Size=""BitSize.Large"" IconName=""@BitIconName.Emoji2"" Variant=""BitVariant.Outline"">Outline</BitButton>
<BitButton Size=""BitSize.Large"" IconName=""@BitIconName.Emoji2"" Variant=""BitVariant.Text"">Text</BitButton>
<BitButton Size=""BitSize.Large"" IsLoading LoadingLabel=""Loading..."" Variant=""BitVariant.Outline"">Loading</BitButton>
<BitButton Size=""BitSize.Large"" SecondaryText=""this is the secondary text"" IconName=""@BitIconName.Emoji2"">Compound</BitButton>";

    private readonly string example17RazorCode = @"
<BitButton Style=""background-color: transparent; border-color: blueviolet; color: blueviolet;""
           SecondaryText=""this is the secondary text""
           Variant=""BitVariant.Outline"">
    Styled Button
</BitButton>

<BitButton Class=""custom-class"">
    Classed Button
</BitButton>


<BitButton IsLoading=""stylesIsLoading"" LoadingLabel=""Wait..."" OnClick=""LoadingStylesClick"" Styles=""@(new()
           {
               Root = ""background-color: peachpuff; border-color: peachpuff; min-width: 6rem;"",
               LoadingLabel = ""color: tomato; font-weight: bold;"",
               Spinner = ""border-color: tomato; border-top-color: goldenrod;""
           })"">
    Click me
</BitButton>

<BitButton IsLoading=""classesIsLoading"" LoadingLabel=""Sending..."" OnClick=""LoadingClassesClick"" Variant=""BitVariant.Outline"" Classes=""@(new()
           {
               Root = ""custom-root"",
               LoadingContainer = ""custom-container"",
               Spinner = ""custom-spinner""
           })"">
    Click me
</BitButton>


@* The public CSS variables are inherited, so .compact-buttons in the stylesheet beside this page
   re-skins both of the buttons inside it without naming a class of the component. *@
<div class=""example-content compact-buttons"">
    <BitButton IconName=""@BitIconName.Accept"">Accept</BitButton>
    <BitButton IconName=""@BitIconName.Cancel"" Variant=""BitVariant.Outline"">Cancel</BitButton>
</div>

<BitButton Style=""--bit-Button-radius: 0; --bit-Button-min-height: 3rem; --bit-Button-padding: 0 2rem;"">
    Squared off
</BitButton>

<BitButton Variant=""BitVariant.Outline""
           Style=""--bit-Button-border-width: 2px;
                  --bit-Button-hover-background: transparent;
                  --bit-Button-hover-color: var(--bit-clr-suc);
                  --bit-Button-hover-border-color: var(--bit-clr-suc);"">
    Ghost on hover
</BitButton>

<BitButton Style=""--bit-Button-shadow: var(--bit-shd-card);
                  --bit-Button-text-transform: uppercase;
                  --bit-Button-letter-spacing: 0.06em;"">
    Raised & tracked
</BitButton>";

    private readonly string example17CsharpCode = @"
private bool stylesIsLoading;
private async Task LoadingStylesClick()
{
    stylesIsLoading = true;
    await Task.Delay(3000);
    stylesIsLoading = false;
}

private bool classesIsLoading;
private async Task LoadingClassesClick()
{
    classesIsLoading = true;
    await Task.Delay(3000);
    classesIsLoading = false;
}";

    private const string example17ScssCode = @"
.compact-buttons {
    --bit-Button-gap: 0.25rem;
    --bit-Button-font-size: 0.75rem;
    --bit-Button-padding: 0 0.75rem;
    --bit-Button-min-height: 1.75rem;
}

::deep {
    .custom-class {
        border-radius: 1rem;
        border-color: blueviolet;
        transition: background-color 1s;
        background: linear-gradient(90deg, magenta, transparent) blue;
    }

    .custom-class:hover {
        border-color: magenta;
        background-color: magenta;
    }

    .custom-root {
        color: aqua;
        min-width: 7.2rem;
        font-weight: bold;
        border-radius: 1rem;
        border-color: aqua;
        box-shadow: aqua 0 0 0.5rem;
    }

    .custom-root:hover {
        color: black;
        background-color: aqua;
    }

    .custom-container {
        text-shadow: tomato 0 0 0.5rem;
    }

    .custom-spinner {
        border-color: red;
        border-top-color: goldenrod;
    }
}";

    private readonly DemoCodeFile[] example17CodeFiles =
    [
        new("BitButtonDemo.razor.scss", example17ScssCode),
    ];

    private readonly string example18RazorCode = @"
<BitButton Dir=""BitDir.Rtl"" IconName=""@BitIconName.Emoji"" Variant=""BitVariant.Fill"">
    دکمه با آیکن
</BitButton>
<BitButton Dir=""BitDir.Rtl"" IconName=""@BitIconName.Emoji"" Variant=""BitVariant.Outline"" SecondaryText=""این متن ثانویه است"">
    دکمه با آیکن
</BitButton>
<BitButton Dir=""BitDir.Rtl"" IconName=""@BitIconName.Emoji"" Variant=""BitVariant.Text"" IconPosition=""BitIconPosition.End"">
    دکمه با آیکن
</BitButton>

<BitButton IsLoading Dir=""BitDir.Rtl"" LoadingLabel=""در حال بارگذاری"" Variant=""BitVariant.Fill"" />
<BitButton IsLoading Dir=""BitDir.Rtl"" LoadingLabel=""در حال بارگذاری"" Variant=""BitVariant.Outline"" />
<BitButton IsLoading Dir=""BitDir.Rtl"" LoadingLabel=""در حال بارگذاری"" Variant=""BitVariant.Text"" />";
}
