namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Inputs.Checkbox;

public partial class BitCheckboxDemo
{

    private readonly string example1RazorCode = @"
<BitCheckbox Label=""Basic checkbox"" />
<BitCheckbox Label=""Checked by default"" DefaultValue=""true"" />
<BitCheckbox Label=""Hover over me"" Title=""The native tooltip of the checkbox"" />
<BitCheckbox Label=""Disable checkbox"" IsEnabled=""false"" />
<BitCheckbox Label=""Disable checked checkbox"" IsEnabled=""false"" Value=""true"" />";

    private readonly string example2RazorCode = @"
<BitCheckbox Label=""Custom check icon"" CheckIconName=""@BitIconName.Heart"" />
<BitCheckbox Label=""Custom unchecked icon"" UncheckedIconName=""@BitIconName.Cancel"" />
<BitCheckbox Label=""Custom indeterminate icon"" Indeterminate IndeterminateIconName=""@BitIconName.Remove"" />
<BitCheckbox Label=""Disabled custom check icon"" CheckIconName=""@BitIconName.WavingHand"" Value=""true"" IsEnabled=""false"" />";

    private readonly string example3RazorCode = @"
<BitCheckbox Label=""End"" LabelPosition=""BitLabelPosition.End"" />
<BitCheckbox Label=""Start"" LabelPosition=""BitLabelPosition.Start"" />
<BitCheckbox Label=""Top"" LabelPosition=""BitLabelPosition.Top"" />
<BitCheckbox Label=""Bottom"" LabelPosition=""BitLabelPosition.Bottom"" />
<BitCheckbox Label=""Reversed"" Reversed />";

    private readonly string example4RazorCode = @"
<style>
    .settings-panel {
        gap: 0.5rem;
        width: 20rem;
        display: flex;
        padding: 0.75rem 1rem;
        border-radius: 0.25rem;
        flex-direction: column;
        border: 1px solid var(--bit-clr-brd-sec);
    }
</style>

<div class=""settings-panel"">
    <BitCheckbox FullWidth Reversed Label=""Wi-Fi"" DefaultValue=""true"" />
    <BitCheckbox FullWidth Reversed Label=""Bluetooth"" />
    <BitCheckbox FullWidth Reversed Label=""Airplane mode"" />
</div>


<div class=""settings-panel"">
    <BitCheckbox FullWidth Label=""A label short enough to fit"" />
    <BitCheckbox FullWidth NoWrap
                 Title=""Send me a weekly digest of everything that happened in my workspace""
                 Label=""Send me a weekly digest of everything that happened in my workspace"" />
</div>";

    private readonly string example5RazorCode = @"
<style>
    .settings-panel {
        gap: 0.5rem;
        width: 20rem;
        display: flex;
        padding: 0.75rem 1rem;
        border-radius: 0.25rem;
        flex-direction: column;
        border: 1px solid var(--bit-clr-brd-sec);
    }
</style>

<div class=""settings-panel"">
    <BitCheckbox FullWidth Reversed
                 Label=""Auto renew""
                 Description=""The subscription is renewed one day before it expires."" />

    <BitCheckbox FullWidth Reversed
                 Label=""Share usage data""
                 Description=""Crash reports and feature usage only. Never the contents of your documents."" />
</div>


<BitCheckbox Label=""I accept the terms"">
    <DescriptionTemplate>
        Read the <BitLink Href=""/components/checkbox"">full agreement</BitLink> before you agree.
    </DescriptionTemplate>
</BitCheckbox>";

    private readonly string example6RazorCode = @"
<BitCheckbox Label=""Indeterminate checkbox"" @bind-Indeterminate=""basicIndeterminate"" />
<BitCheckbox Label=""Indeterminate by default"" DefaultIndeterminate=""true"" />
<BitCheckbox Label=""Disabled indeterminate checkbox"" Indeterminate IsEnabled=""false"" />


<BitCheckbox Label=""Select all fruits""
             Value=""selectAll""
             OnChange=""HandleSelectAllChange""
             @bind-Indeterminate=""selectAllIndeterminate"" />
<BitCheckbox Label=""Apple"" Value=""apple"" OnChange=""v => { apple = v; RefreshSelectAll(); }"" />
<BitCheckbox Label=""Banana"" Value=""banana"" OnChange=""v => { banana = v; RefreshSelectAll(); }"" />
<BitCheckbox Label=""Orange"" Value=""orange"" OnChange=""v => { orange = v; RefreshSelectAll(); }"" />


<BitCheckbox Label=""Three-state checkbox"" ThreeState
             @bind-Value=""threeStateValue""
             @bind-Indeterminate=""threeStateIndeterminate"" />

<div>Value: <b>@threeStateValue</b>, Indeterminate: <b>@threeStateIndeterminate</b></div>


<BitCheckbox Label=""Subscribe to the newsletter"" ThreeState
             Value=""subscribed is true"" ValueChanged=""HandleSubscribedValueChanged""
             Indeterminate=""subscribed is null"" IndeterminateChanged=""HandleSubscribedIndeterminateChanged"" />

<div>subscribed: <b>@(subscribed?.ToString() ?? ""null"")</b></div>";

    private readonly string example6CsharpCode = @"
private bool basicIndeterminate = true;

private bool apple;
private bool banana;
private bool orange;
private bool selectAll;
private bool selectAllIndeterminate;

private void HandleSelectAllChange(bool value)
{
    selectAll = value;
    selectAllIndeterminate = false;
    apple = banana = orange = value;
}

private void RefreshSelectAll()
{
    var checkedCount = (apple ? 1 : 0) + (banana ? 1 : 0) + (orange ? 1 : 0);

    selectAll = checkedCount == 3;
    selectAllIndeterminate = checkedCount is > 0 and < 3;
}

private bool threeStateValue;
private bool threeStateIndeterminate;

private bool? subscribed = false;

// A click reports the mixed state before the value, so the mixed one has the last word: the value that
// follows a ""no answer"" is not an answer either, which is what keeps the null from being overwritten
// with the false underneath it.
private void HandleSubscribedIndeterminateChanged(bool indeterminate) => subscribed = indeterminate ? null : subscribed is true;

private void HandleSubscribedValueChanged(bool value) => subscribed = subscribed is null ? null : value;";

    private readonly string example7RazorCode = @"
<BitCheckbox Label=""One-way checked (Fixed)"" Value=""true"" />

<BitCheckbox Label=""One-way"" Value=""oneWayValue"" />
<BitToggleButton @bind-IsChecked=""oneWayValue"" Text=""Toggle"" />

<BitCheckbox Label=""Two-way controlled checkbox"" @bind-Value=""twoWayValue"" />
<BitToggleButton @bind-IsChecked=""twoWayValue"" Text=""Toggle"" />


<BitCheckbox Label=""One-way indeterminate (Fixed)"" Indeterminate />

<BitCheckbox Label=""One-way indeterminate"" Indeterminate=""oneWayIndeterminate"" />
<BitToggleButton @bind-IsChecked=""oneWayIndeterminate"" Text=""Toggle"" />

<BitCheckbox Label=""Two-way indeterminate"" @bind-Indeterminate=""twoWayIndeterminate"" />
<BitToggleButton @bind-IsChecked=""twoWayIndeterminate"" Text=""Toggle"" />";

    private readonly string example7CsharpCode = @"
private bool oneWayValue;
private bool twoWayValue;
private bool oneWayIndeterminate = true;
private bool twoWayIndeterminate = true;";

    private readonly string example8RazorCode = @"
<BitCheckbox>
    <LabelTemplate>
        <BitTag Color=""BitColor.Success"">Label Template</BitTag>
    </LabelTemplate>
</BitCheckbox>


<BitCheckbox @bind-Value=""customCheckboxValue"">
    <BitIcon Style=""display:flex;align-items:center;justify-content:center;padding:0;border:1px solid gray;width:22px;height:22px""
             IconName=""@(customCheckboxValue ? BitIconName.Accept : null)"" />
    <span>Custom basic checkbox</span>
</BitCheckbox>


<BitCheckbox @bind-Value=""customContentValue"" @bind-Indeterminate=""customContentIndeterminate"">
    <BitIcon Style=""display:flex;align-items:center;justify-content:center;padding:0;border:1px solid gray;width:22px;height:22px""
             IconName=""@(customContentIndeterminate ? BitIconName.Fingerprint : (customContentValue ? BitIconName.Accept : null))"" />
    <span>Custom indeterminate checkbox</span>
</BitCheckbox>
<BitButton OnClick=""() => customContentIndeterminate = true"">Make Indeterminate</BitButton>";

    private readonly string example8CsharpCode = @"
private bool customCheckboxValue;
private bool customContentValue;
private bool customContentIndeterminate = true;";

    private readonly string example9RazorCode = @"
<style>
    .clickable-box {
        padding: 1rem;
        cursor: pointer;
        border-radius: 0.25rem;
        border: 1px dashed gray;
    }
</style>

<BitCheckbox Label=""Click me""
             OnClick=""LogOnClick""
             OnChanging=""LogOnChanging""
             OnChange=""LogOnChange""
             OnFocus=""LogOnFocus""
             OnFocusIn=""LogOnFocusIn""
             OnFocusOut=""LogOnFocusOut""
             OnBlur=""LogOnBlur"" />
<div>@(string.IsNullOrEmpty(eventsLog) ? ""No clicks yet."" : eventsLog)</div>


<BitCheckbox Label=""Allow the change"" @bind-Value=""allowChange"" />
<BitCheckbox Label=""Guarded checkbox"" OnChanging=""HandleOnChanging"" />
<div>Cancelled attempts: @cancelledCounter</div>


<div class=""clickable-box"" @onclick=""() => containerClickCounter++"">
    <BitCheckbox Label=""Bubbles up"" />
    <BitCheckbox Label=""Stops here"" StopPropagation />
</div>
<div>Container clicks: @containerClickCounter</div>";

    private readonly string example9CsharpCode = @"
private string eventsLog = string.Empty;
private bool eventsCycleEnded;
private int cancelledCounter;
private int containerClickCounter;
private bool allowChange;

private void LogOnClick() => AppendEventLog(""OnClick"");

private void LogOnChanging(BitCheckboxChangeArgs args) => AppendEventLog($""OnChanging({args.Value})"");

private void LogOnChange(bool value)
{
    AppendEventLog($""OnChange({value})"");
    eventsCycleEnded = true;
}

private void LogOnFocus() => AppendEventLog(""OnFocus"");

private void LogOnFocusIn() => AppendEventLog(""OnFocusIn"");

private void LogOnFocusOut() => AppendEventLog(""OnFocusOut"");

private void LogOnBlur() => AppendEventLog(""OnBlur"");

// Every callback appends, so the log is the order they actually fired in - the focus arriving is still
// on the line when the click that follows it is written. Only the first one after a completed click
// starts the line over, which is what keeps a second click from being read as part of the first.
private void AppendEventLog(string name)
{
    if (eventsCycleEnded)
    {
        eventsLog = string.Empty;
        eventsCycleEnded = false;
    }

    eventsLog += eventsLog.Length == 0 ? name : $"" → {name}"";
}

private void HandleOnChanging(BitCheckboxChangeArgs args)
{
    if (allowChange) return;

    args.Cancel = true;
    cancelledCounter++;
}";

    private readonly string example10RazorCode = @"
<BitCheckbox Label=""Read-only checkbox"" ReadOnly @bind-Value=""readOnlyValue"" />
<BitToggleButton @bind-IsChecked=""readOnlyValue"" Text=""Change it from here"" />


<BitCheckbox Label=""I accept the terms"" Required />

<BitCheckbox Required>
    <LabelTemplate>
        I accept the <BitLink Href=""/components/checkbox"">terms</BitLink>
    </LabelTemplate>
</BitCheckbox>";

    private readonly string example10CsharpCode = @"
private bool readOnlyValue;";

    private readonly string example11RazorCode = @"
<style>
    .validation-message {
        color: red;
        font-size: 0.75rem;
    }
</style>

@if (string.IsNullOrEmpty(SuccessMessage))
{
    <EditForm Model=""validationModel""
              OnValidSubmit=""HandleValidSubmit""
              OnInvalidSubmit=""HandleInvalidSubmit"">
        <DataAnnotationsValidator />
        <BitCheckbox Label=""I agree with the terms and conditions.""
                     AriaDescribedby=""terms-error""
                     @bind-Value=""validationModel.TermsAgreement"" />
        <div id=""terms-error"">
            <ValidationMessage For=""@(() => validationModel.TermsAgreement)"" />
        </div>

        <BitButton ButtonType=""BitButtonType.Submit"">Submit</BitButton>
    </EditForm>
}
else
{
    <BitMessage Color=""BitColor.Success"">@SuccessMessage</BitMessage>
}";

    private readonly string example11CsharpCode = @"
private string SuccessMessage = string.Empty;
private BitCheckboxValidationModel validationModel = new();

public class BitCheckboxValidationModel
{
    [Range(typeof(bool), ""true"", ""true"", ErrorMessage = ""You must agree to the terms and conditions."")]
    public bool TermsAgreement { get; set; }
}

private async Task HandleValidSubmit()
{
    SuccessMessage = ""Form Submitted Successfully!"";
    await Task.Delay(3000);
    SuccessMessage = string.Empty;
    StateHasChanged();
}

private void HandleInvalidSubmit()
{
    SuccessMessage = string.Empty;
}";

    private readonly string example12RazorCode = @"
<BitCheckbox Label=""Focus me with Tab, toggle me with Space"" />


<BitButton OnClick=""FocusTheCheckbox"">Focus the checkbox</BitButton>
<BitCheckbox @ref=""checkboxRef"" Label=""Programmatic focus target"" />


<BitCheckbox AriaLabel=""Select the row"" />
<BitCheckbox Label=""Auto renew"" AriaDescription=""The subscription will be renewed one day before it expires."" />


<span id=""newsletter-label"">Weekly newsletter</span>
<BitCheckbox AriaLabelledby=""newsletter-label"" />


<BitCheckbox Label=""Disabled and skipped"" IsEnabled=""false"" />
<BitCheckbox Label=""Disabled but still reachable"" IsEnabled=""false"" AllowDisabledFocus />


<BitCheckbox Label=""Item 3"" AriaSetSize=""10"" AriaPositionInSet=""3"" />
<BitCheckbox Label=""Item 4"" AriaSetSize=""10"" AriaPositionInSet=""4"" />


<BitCheckbox Label=""Select all fruits"" AriaControls=""fruits"" Indeterminate />
<div id=""fruits"">
    <BitCheckbox Label=""Apple"" Value />
    <BitCheckbox Label=""Banana"" />
</div>";

    private readonly string example12CsharpCode = @"
private BitCheckbox checkboxRef = default!;

private async Task FocusTheCheckbox() => await checkboxRef.FocusAsync();";

    private readonly string example13RazorCode = @"
<BitCheckbox Label=""Busy"" Loading />
<BitCheckbox Label=""Busy and checked"" Loading Value />
<BitCheckbox Label=""Busy and mixed"" Loading Indeterminate />


<BitCheckbox AutoLoading
             Label=""Sync with the server""
             OnChanging=""HandleSlowChanging"" />

<div>Saved: <b>@savedCount</b> time(s)</div>";

    private readonly string example13CsharpCode = @"
private int savedCount;

private async Task HandleSlowChanging(BitCheckboxChangeArgs args)
{
    await Task.Delay(2000);
    savedCount++;
}";

    private readonly string example14RazorCode = @"
<BitCheckbox Color=""BitColor.Primary"" Label=""Primary"" />
<BitCheckbox Color=""BitColor.Primary"" Label=""Primary"" Indeterminate />
<BitCheckbox Color=""BitColor.Primary"" Label=""Primary"" Value />

<BitCheckbox Color=""BitColor.Secondary"" Label=""Secondary"" />
<BitCheckbox Color=""BitColor.Secondary"" Label=""Secondary"" Indeterminate />
<BitCheckbox Color=""BitColor.Secondary"" Label=""Secondary"" Value />

<BitCheckbox Color=""BitColor.Tertiary"" Label=""Tertiary"" />
<BitCheckbox Color=""BitColor.Tertiary"" Label=""Tertiary"" Indeterminate />
<BitCheckbox Color=""BitColor.Tertiary"" Label=""Tertiary"" Value />

<BitCheckbox Color=""BitColor.Info"" Label=""Info"" />
<BitCheckbox Color=""BitColor.Info"" Label=""Info"" Indeterminate />
<BitCheckbox Color=""BitColor.Info"" Label=""Info"" Value />

<BitCheckbox Color=""BitColor.Success"" Label=""Success"" />
<BitCheckbox Color=""BitColor.Success"" Label=""Success"" Indeterminate />
<BitCheckbox Color=""BitColor.Success"" Label=""Success"" Value />

<BitCheckbox Color=""BitColor.Warning"" Label=""Warning"" />
<BitCheckbox Color=""BitColor.Warning"" Label=""Warning"" Indeterminate />
<BitCheckbox Color=""BitColor.Warning"" Label=""Warning"" Value />

<BitCheckbox Color=""BitColor.SevereWarning"" Label=""SevereWarning"" />
<BitCheckbox Color=""BitColor.SevereWarning"" Label=""SevereWarning"" Indeterminate />
<BitCheckbox Color=""BitColor.SevereWarning"" Label=""SevereWarning"" Value />

<BitCheckbox Color=""BitColor.Error"" Label=""Error"" />
<BitCheckbox Color=""BitColor.Error"" Label=""Error"" Indeterminate />
<BitCheckbox Color=""BitColor.Error"" Label=""Error"" Value />

<BitCheckbox Color=""BitColor.PrimaryBackground"" Label=""PrimaryBackground"" />
<BitCheckbox Color=""BitColor.PrimaryBackground"" Label=""PrimaryBackground"" Indeterminate />
<BitCheckbox Color=""BitColor.PrimaryBackground"" Label=""PrimaryBackground"" Value />

<BitCheckbox Color=""BitColor.SecondaryBackground"" Label=""SecondaryBackground"" />
<BitCheckbox Color=""BitColor.SecondaryBackground"" Label=""SecondaryBackground"" Indeterminate />
<BitCheckbox Color=""BitColor.SecondaryBackground"" Label=""SecondaryBackground"" Value />

<BitCheckbox Color=""BitColor.TertiaryBackground"" Label=""TertiaryBackground"" />
<BitCheckbox Color=""BitColor.TertiaryBackground"" Label=""TertiaryBackground"" Indeterminate />
<BitCheckbox Color=""BitColor.TertiaryBackground"" Label=""TertiaryBackground"" Value />

<BitCheckbox Color=""BitColor.PrimaryForeground"" Label=""PrimaryForeground"" />
<BitCheckbox Color=""BitColor.PrimaryForeground"" Label=""PrimaryForeground"" Indeterminate />
<BitCheckbox Color=""BitColor.PrimaryForeground"" Label=""PrimaryForeground"" Value />

<BitCheckbox Color=""BitColor.SecondaryForeground"" Label=""SecondaryForeground"" />
<BitCheckbox Color=""BitColor.SecondaryForeground"" Label=""SecondaryForeground"" Indeterminate />
<BitCheckbox Color=""BitColor.SecondaryForeground"" Label=""SecondaryForeground"" Value />

<BitCheckbox Color=""BitColor.TertiaryForeground"" Label=""TertiaryForeground"" />
<BitCheckbox Color=""BitColor.TertiaryForeground"" Label=""TertiaryForeground"" Indeterminate />
<BitCheckbox Color=""BitColor.TertiaryForeground"" Label=""TertiaryForeground"" Value />

<BitCheckbox Color=""BitColor.PrimaryBorder"" Label=""PrimaryBorder"" />
<BitCheckbox Color=""BitColor.PrimaryBorder"" Label=""PrimaryBorder"" Indeterminate />
<BitCheckbox Color=""BitColor.PrimaryBorder"" Label=""PrimaryBorder"" Value />

<BitCheckbox Color=""BitColor.SecondaryBorder"" Label=""SecondaryBorder"" />
<BitCheckbox Color=""BitColor.SecondaryBorder"" Label=""SecondaryBorder"" Indeterminate />
<BitCheckbox Color=""BitColor.SecondaryBorder"" Label=""SecondaryBorder"" Value />

<BitCheckbox Color=""BitColor.TertiaryBorder"" Label=""TertiaryBorder"" />
<BitCheckbox Color=""BitColor.TertiaryBorder"" Label=""TertiaryBorder"" Indeterminate />
<BitCheckbox Color=""BitColor.TertiaryBorder"" Label=""TertiaryBorder"" Value />


<BitCheckbox IsEnabled=""false"" Color=""BitColor.Primary"" Label=""Primary"" />
<BitCheckbox IsEnabled=""false"" Color=""BitColor.Primary"" Label=""Primary"" Indeterminate />
<BitCheckbox IsEnabled=""false"" Color=""BitColor.Primary"" Label=""Primary"" Value />

<BitCheckbox IsEnabled=""false"" Color=""BitColor.Secondary"" Label=""Secondary"" />
<BitCheckbox IsEnabled=""false"" Color=""BitColor.Secondary"" Label=""Secondary"" Indeterminate />
<BitCheckbox IsEnabled=""false"" Color=""BitColor.Secondary"" Label=""Secondary"" Value />

<BitCheckbox IsEnabled=""false"" Color=""BitColor.Tertiary"" Label=""Tertiary"" />
<BitCheckbox IsEnabled=""false"" Color=""BitColor.Tertiary"" Label=""Tertiary"" Indeterminate />
<BitCheckbox IsEnabled=""false"" Color=""BitColor.Tertiary"" Label=""Tertiary"" Value />

<BitCheckbox IsEnabled=""false"" Color=""BitColor.Info"" Label=""Info"" />
<BitCheckbox IsEnabled=""false"" Color=""BitColor.Info"" Label=""Info"" Indeterminate />
<BitCheckbox IsEnabled=""false"" Color=""BitColor.Info"" Label=""Info"" Value />

<BitCheckbox IsEnabled=""false"" Color=""BitColor.Success"" Label=""Success"" />
<BitCheckbox IsEnabled=""false"" Color=""BitColor.Success"" Label=""Success"" Indeterminate />
<BitCheckbox IsEnabled=""false"" Color=""BitColor.Success"" Label=""Success"" Value />

<BitCheckbox IsEnabled=""false"" Color=""BitColor.Warning"" Label=""Warning"" />
<BitCheckbox IsEnabled=""false"" Color=""BitColor.Warning"" Label=""Warning"" Indeterminate />
<BitCheckbox IsEnabled=""false"" Color=""BitColor.Warning"" Label=""Warning"" Value />

<BitCheckbox IsEnabled=""false"" Color=""BitColor.SevereWarning"" Label=""SevereWarning"" />
<BitCheckbox IsEnabled=""false"" Color=""BitColor.SevereWarning"" Label=""SevereWarning"" Indeterminate />
<BitCheckbox IsEnabled=""false"" Color=""BitColor.SevereWarning"" Label=""SevereWarning"" Value />

<BitCheckbox IsEnabled=""false"" Color=""BitColor.Error"" Label=""Error"" />
<BitCheckbox IsEnabled=""false"" Color=""BitColor.Error"" Label=""Error"" Indeterminate />
<BitCheckbox IsEnabled=""false"" Color=""BitColor.Error"" Label=""Error"" Value />

<div style=""background:var(--bit-clr-fg-sec);color:var(--bit-clr-bg-sec);padding:1rem"">
    <BitCheckbox IsEnabled=""false"" Color=""BitColor.PrimaryBackground"" Label=""PrimaryBackground"" />
    <BitCheckbox IsEnabled=""false"" Color=""BitColor.PrimaryBackground"" Label=""PrimaryBackground"" Indeterminate />
    <BitCheckbox IsEnabled=""false"" Color=""BitColor.PrimaryBackground"" Label=""PrimaryBackground"" Value />

    <BitCheckbox IsEnabled=""false"" Color=""BitColor.SecondaryBackground"" Label=""SecondaryBackground"" />
    <BitCheckbox IsEnabled=""false"" Color=""BitColor.SecondaryBackground"" Label=""SecondaryBackground"" Indeterminate />
    <BitCheckbox IsEnabled=""false"" Color=""BitColor.SecondaryBackground"" Label=""SecondaryBackground"" Value />

    <BitCheckbox IsEnabled=""false"" Color=""BitColor.TertiaryBackground"" Label=""TertiaryBackground"" />
    <BitCheckbox IsEnabled=""false"" Color=""BitColor.TertiaryBackground"" Label=""TertiaryBackground"" Indeterminate />
    <BitCheckbox IsEnabled=""false"" Color=""BitColor.TertiaryBackground"" Label=""TertiaryBackground"" Value />
</div>

<BitCheckbox IsEnabled=""false"" Color=""BitColor.PrimaryForeground"" Label=""PrimaryForeground"" />
<BitCheckbox IsEnabled=""false"" Color=""BitColor.PrimaryForeground"" Label=""PrimaryForeground"" Indeterminate />
<BitCheckbox IsEnabled=""false"" Color=""BitColor.PrimaryForeground"" Label=""PrimaryForeground"" Value />

<BitCheckbox IsEnabled=""false"" Color=""BitColor.SecondaryForeground"" Label=""SecondaryForeground"" />
<BitCheckbox IsEnabled=""false"" Color=""BitColor.SecondaryForeground"" Label=""SecondaryForeground"" Indeterminate />
<BitCheckbox IsEnabled=""false"" Color=""BitColor.SecondaryForeground"" Label=""SecondaryForeground"" Value />

<BitCheckbox IsEnabled=""false"" Color=""BitColor.TertiaryForeground"" Label=""TertiaryForeground"" />
<BitCheckbox IsEnabled=""false"" Color=""BitColor.TertiaryForeground"" Label=""TertiaryForeground"" Indeterminate />
<BitCheckbox IsEnabled=""false"" Color=""BitColor.TertiaryForeground"" Label=""TertiaryForeground"" Value />

<BitCheckbox IsEnabled=""false"" Color=""BitColor.PrimaryBorder"" Label=""PrimaryBorder"" />
<BitCheckbox IsEnabled=""false"" Color=""BitColor.PrimaryBorder"" Label=""PrimaryBorder"" Indeterminate />
<BitCheckbox IsEnabled=""false"" Color=""BitColor.PrimaryBorder"" Label=""PrimaryBorder"" Value />

<BitCheckbox IsEnabled=""false"" Color=""BitColor.SecondaryBorder"" Label=""SecondaryBorder"" />
<BitCheckbox IsEnabled=""false"" Color=""BitColor.SecondaryBorder"" Label=""SecondaryBorder"" Indeterminate />
<BitCheckbox IsEnabled=""false"" Color=""BitColor.SecondaryBorder"" Label=""SecondaryBorder"" Value />

<BitCheckbox IsEnabled=""false"" Color=""BitColor.TertiaryBorder"" Label=""TertiaryBorder"" />
<BitCheckbox IsEnabled=""false"" Color=""BitColor.TertiaryBorder"" Label=""TertiaryBorder"" Indeterminate />
<BitCheckbox IsEnabled=""false"" Color=""BitColor.TertiaryBorder"" Label=""TertiaryBorder"" Value />";

    private readonly string example15RazorCode = @"
<link rel=""stylesheet"" href=""https://cdnjs.cloudflare.com/ajax/libs/font-awesome/7.0.1/css/all.min.css"" />

<BitCheckbox Label=""House (CheckIcon string)"" CheckIcon=""@(""fa-solid fa-house"")"" />

<BitCheckbox Label=""Heart (BitIconInfo.Css)"" CheckIcon=""@BitIconInfo.Css(""fa-solid fa-heart"")"" Color=""BitColor.Secondary"" />

<BitCheckbox Label=""Rocket (BitIconInfo.Fa)"" CheckIcon=""@BitIconInfo.Fa(""solid rocket"")"" Color=""BitColor.Error"" />

<BitCheckbox Label=""Minus (IndeterminateIcon)"" Indeterminate IndeterminateIcon=""@BitIconInfo.Fa(""solid minus"")"" />


<link rel=""stylesheet"" href=""https://cdn.jsdelivr.net/npm/bootstrap-icons@1.11.3/font/bootstrap-icons.min.css"" />

<BitCheckbox Label=""House (CheckIcon string)"" CheckIcon=""@(""bi bi-house-fill"")"" />

<BitCheckbox Label=""Heart (BitIconInfo.Css)"" CheckIcon=""@BitIconInfo.Css(""bi bi-heart-fill"")"" Color=""BitColor.Secondary"" />

<BitCheckbox Label=""Gear (BitIconInfo.Bi)"" CheckIcon=""@BitIconInfo.Bi(""gear-fill"")"" Color=""BitColor.Error"" />

<BitCheckbox Label=""Square (UncheckedIcon)"" UncheckedIcon=""@BitIconInfo.Bi(""app"")"" />";

    private readonly string example16RazorCode = @"
<BitCheckbox Size=""BitSize.Small"" Label=""Checkbox"" />
<BitCheckbox Size=""BitSize.Small"" Label=""Checkbox"" Indeterminate />
<BitCheckbox Size=""BitSize.Small"" Label=""Checkbox"" Value />

<BitCheckbox Size=""BitSize.Medium"" Label=""Checkbox"" />
<BitCheckbox Size=""BitSize.Medium"" Label=""Checkbox"" Indeterminate />
<BitCheckbox Size=""BitSize.Medium"" Label=""Checkbox"" Value />

<BitCheckbox Size=""BitSize.Large"" Label=""Checkbox"" />
<BitCheckbox Size=""BitSize.Large"" Label=""Checkbox"" Indeterminate />
<BitCheckbox Size=""BitSize.Large"" Label=""Checkbox"" Value />";

    private readonly string example17RazorCode = @"
<style>
    .custom-class {
        padding: 0.5rem;
        border-radius: 0.125rem;
        background-color: #d3d3d347;
        border: 1px solid dodgerblue;
    }


    .custom-label {
        font-weight: bold;
        color: lightseagreen;
    }

    .custom-icon {
        color: lightseagreen
    }

    .custom-box {
        border-radius: 0.2rem;
        border-color: lightseagreen;
    }

    .custom-checked .custom-icon {
        color: white
    }

    .custom-checked:hover .custom-icon {
        color: whitesmoke;
    }

    .custom-checked .custom-box {
        background-color: lightseagreen;
    }

    .custom-checked:hover .custom-box {
        border-color: mediumseagreen;
    }
</style>


<BitCheckbox Label=""Styled checkbox"" Style=""color: dodgerblue; text-shadow: lightskyblue 0 0 1rem;"" />

<BitCheckbox Label=""Classed checkbox"" Class=""custom-class"" />


<BitCheckbox Label=""Styles""
             Styles=""@(new() { Checked = ""--check-color: deeppink; --icon-color: white;"",
                               Label = ""color: var(--check-color);"",
                               Box = ""border-radius: 50%; border-color: var(--check-color); background-color: var(--check-color);"",
                               Icon = ""color: var(--icon-color);"" })"" />

<BitCheckbox Label=""Classes""
             Classes=""@(new() { Checked = ""custom-checked"",
                                Icon = ""custom-icon"",
                                Label=""custom-label"",
                                Box=""custom-box"" })"" />


<BitCheckbox Label=""Rounded, thick, custom fill"" Value
             Style=""--bit-Checkbox-radius: 50%; --bit-Checkbox-border-width: 2px; --bit-Checkbox-checked-background: rebeccapurple; --bit-Checkbox-check-color: white;"" />

<BitCheckbox Label=""Bigger box, wider gap"" Value
             Style=""--bit-Checkbox-box-size: 1.75rem; --bit-Checkbox-gap: 1rem;"" />

<BitCheckbox Label=""Mixed state in its own color"" Indeterminate
             Style=""--bit-Checkbox-indeterminate-color: darkorange;"" />


<div style=""--bit-Checkbox-border-color: var(--bit-clr-suc); --bit-Checkbox-checked-background: var(--bit-clr-suc); --bit-Checkbox-checked-hover-background: var(--bit-clr-suc-hover); --bit-Checkbox-focus-color: var(--bit-clr-suc-focus); --bit-Checkbox-description-color: var(--bit-clr-suc);"">
    <BitCheckbox Label=""Analytics"" Description=""Anonymous usage statistics."" Value />
    <BitCheckbox Label=""Crash reports"" Description=""Stack traces only, never your data."" />
</div>";

    private readonly string example18RazorCode = @"
<BitCheckbox Dir=""BitDir.Rtl"" Label=""چکباکس راست به چپ"" />
<BitCheckbox Dir=""BitDir.Rtl"" Label=""چکباکس غیرفعال"" IsEnabled=""false"" />
<BitCheckbox Dir=""BitDir.Rtl"" Label=""چکباکس غیرفعال چک شده"" IsEnabled=""false"" Value=""true"" />";
}
