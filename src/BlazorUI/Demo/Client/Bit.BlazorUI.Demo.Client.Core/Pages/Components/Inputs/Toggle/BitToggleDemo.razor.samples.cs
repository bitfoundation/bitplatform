namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Inputs.Toggle;

public partial class BitToggleDemo
{
    private readonly string example1RazorCode = @"
<BitToggle Label=""Basic"" />
<BitToggle Label=""On by default"" DefaultValue=""true"" />
<BitToggle Label=""Disabled"" IsEnabled=""false"" />
<BitToggle Label=""Disabled and on"" IsEnabled=""false"" Value=""true"" />";

    private readonly string example2RazorCode = @"
<BitToggle Label=""Text"" Text=""This is a toggle!"" />

<BitToggle Label=""OnText & OffText"" OnText=""Toggle is On"" OffText=""Toggle is Off"" />

<BitToggle Label=""OnText only (Text as the fallback)"" OnText=""Enabled"" Text=""Not enabled yet"" />";

    private readonly string example3RazorCode = @"
<BitToggle Label=""Text content"">
    <OnContent>ON</OnContent>
    <OffContent>OFF</OffContent>
</BitToggle>

<BitToggle Label=""Icon content"" DefaultValue=""true"">
    <OnContent><BitIcon IconName=""@BitIconName.Accept"" /></OnContent>
    <OffContent><BitIcon IconName=""@BitIconName.Cancel"" /></OffContent>
</BitToggle>

<BitToggle Label=""Only one side"">
    <OffContent>Click me</OffContent>
</BitToggle>";

    private readonly string example4RazorCode = @"
<BitToggle Label=""Both states"" OnIconName=""@BitIconName.Accept"" OffIconName=""@BitIconName.Cancel"" />

<BitToggle Label=""On state only"" OnIconName=""@BitIconName.CheckMark"" DefaultValue=""true"" />

<BitToggle Label=""Icon and track content"" OnIconName=""@BitIconName.Sunny"" OffIconName=""@BitIconName.ClearNight"">
    <OnContent>Day</OnContent>
    <OffContent>Night</OffContent>
</BitToggle>

<BitToggle Label=""Disabled"" OnIconName=""@BitIconName.Accept"" OffIconName=""@BitIconName.Cancel"" IsEnabled=""false"" />";

    private readonly string example5RazorCode = @"
<BitToggle Label=""This is an inline label"" Inline />

<BitToggle>
    <LabelTemplate>
        <div style=""display:flex;align-items:center;gap:10px"">
            <BitLabel Style=""color:green"">This is custom Label</BitLabel>
            <BitIcon IconName=""@BitIconName.Filter"" />
        </div>
    </LabelTemplate>
</BitToggle>";

    private readonly string example6RazorCode = @"
<BitToggle Label=""Top"" LabelPosition=""BitLabelPosition.Top"" />
<BitToggle Label=""Bottom"" LabelPosition=""BitLabelPosition.Bottom"" />
<BitToggle Label=""Start"" LabelPosition=""BitLabelPosition.Start"" />
<BitToggle Label=""End"" LabelPosition=""BitLabelPosition.End"" />";

    private readonly string example7RazorCode = @"
<BitToggle Label=""This is a reversed label"" Reversed />

<BitToggle Label=""This is a reversed inline label"" Reversed Inline />";

    private readonly string example8RazorCode = @"
<BitToggle Label=""This is a full-width toggle"" FullWidth Inline />

<BitToggle Label=""This is a reversed full-width toggle"" Reversed FullWidth Inline />

<BitToggle FullWidth Inline>
    <LabelTemplate>
        <BitActionButton FullWidth>go go go</BitActionButton>
    </LabelTemplate>
</BitToggle>";

    private readonly string example9RazorCode = @"
<BitToggle Label=""Save to the server"" Loading=""isSaving"" Value=""savedValue"" OnChange=""HandleSaveToggle"" />

<div>Saved state: <b>@savedValue</b></div>


<BitToggle Label=""Save to the server (AutoLoading)"" AutoLoading @bind-Value=""autoLoadingValue"" OnChange=""SaveTheToggle"" />


<BitToggle Label=""Loading"" Loading />
<BitToggle Label=""Loading and on"" Loading Value=""true"" />
<BitToggle Label=""Loading with icons"" Loading OnIconName=""@BitIconName.Accept"" OffIconName=""@BitIconName.Cancel"" />";
    private readonly string example9CsharpCode = @"
private bool isSaving;
private bool savedValue;
private bool autoLoadingValue;

private async Task HandleSaveToggle(bool value)
{
    isSaving = true;

    await Task.Delay(1000);

    savedValue = value;
    isSaving = false;
}

private async Task SaveTheToggle(bool value)
{
    // AutoLoading keeps the toggle busy for exactly as long as this callback takes
    await Task.Delay(1000);
}";

    private readonly string example10RazorCode = @"
<BitToggle Label=""Read-only toggle"" ReadOnly @bind-Value=""readOnlyValue"" Text=""Locked"" />
<BitToggleButton @bind-IsChecked=""readOnlyValue"" Text=""Change it from here"" />


<BitToggle Label=""I accept the terms"" Required />";
    private readonly string example10CsharpCode = @"
private bool readOnlyValue;";

    private readonly string example11RazorCode = @"
<BitToggle Label=""Always on"" Value=""true"" />

<BitToggle Label=""One-way"" Value=""oneWayValue"" />
<BitToggleButton @bind-IsChecked=""oneWayValue"" OnText=""On"" OffText=""Off"" />

<BitToggle Label=""Two-way"" @bind-Value=""twoWayValue"" />
<BitToggleButton @bind-IsChecked=""twoWayValue"" OnText=""On"" OffText=""Off"" />


<BitButton OnClick=""() => methodToggleRef.ToggleAsync()"">Flip it</BitButton>
<BitButton OnClick=""() => methodToggleRef.ToggleAsync(true)"">Turn it on</BitButton>
<BitButton OnClick=""() => methodToggleRef.ToggleAsync(false)"">Turn it off</BitButton>

<BitToggle @ref=""methodToggleRef"" Label=""Driven from code"" ReadOnly
           Text=""Read-only, still scriptable"" OnChange=""LogMethodChange"" />

<div>@(string.IsNullOrEmpty(methodChangeLog) ? ""Nothing changed yet."" : methodChangeLog)</div>";
    private readonly string example11CsharpCode = @"
private bool oneWayValue;
private bool twoWayValue;

private string methodChangeLog = string.Empty;
private BitToggle methodToggleRef = default!;

private void LogMethodChange(bool value)
{
    methodChangeLog = $""OnChange fired with {value}"";
}";

    private readonly string example12RazorCode = @"
<style>
    .clickable-box {
        padding: 1rem;
        cursor: pointer;
        width: fit-content;
        border-radius: 0.25rem;
        border: 1px dashed gray;
    }
</style>


<BitToggle Label=""Flip me""
           OnClick=""LogOnClick""
           OnChanging=""LogOnChanging""
           OnChange=""LogOnChange"" />

<div>@(string.IsNullOrEmpty(eventsLog) ? ""No clicks yet."" : eventsLog)</div>


<BitToggle Label=""Allow the change"" @bind-Value=""allowChange"" />

<BitToggle Label=""Guarded toggle"" OnChanging=""HandleOnChanging"" />

<div>Cancelled attempts: @cancelledCounter</div>


<div class=""clickable-box"" @onclick=""() => containerClickCounter++"">
    <BitToggle Label=""Bubbles up"" />
    <BitToggle Label=""Stops here"" StopPropagation />
</div>

<div>Container clicks: @containerClickCounter</div>


<BitToggle Label=""Focus me""
           OnFocus=""LogOnFocus""
           OnBlur=""LogOnBlur""
           OnFocusIn=""LogOnFocusIn""
           OnFocusOut=""LogOnFocusOut"" />

<div>@(string.IsNullOrEmpty(focusLog) ? ""Not focused yet."" : focusLog)</div>";
    private readonly string example12CsharpCode = @"
private string eventsLog = string.Empty;
private string focusLog = string.Empty;
private int cancelledCounter;
private int containerClickCounter;
private bool allowChange;

private void LogOnClick()
{
    eventsLog = ""OnClick"";
}

private void LogOnChanging(BitToggleChangeArgs args)
{
    eventsLog += $"" → OnChanging (to {args.Value})"";
}

private void LogOnChange(bool value)
{
    eventsLog += $"" → OnChange ({value})"";
}

private void HandleOnChanging(BitToggleChangeArgs args)
{
    if (allowChange) return;

    args.Cancel = true;
    cancelledCounter++;
}

private void LogOnFocus()
{
    focusLog = ""OnFocus"";
}

private void LogOnFocusIn()
{
    focusLog += "" → OnFocusIn"";
}

private void LogOnFocusOut()
{
    focusLog = ""OnFocusOut"";
}

private void LogOnBlur()
{
    focusLog += "" → OnBlur"";
}";

    private readonly string example13RazorCode = @"
<style>
    .validation-message {
        color: red;
    }
</style>

<EditForm Model=""validationModel"" OnValidSubmit=""HandleValidSubmit"" OnInvalidSubmit=""HandleInvalidSubmit"">
    <DataAnnotationsValidator />

    <BitToggle Label=""Terms and conditions"" Text=""I agree."" @bind-Value=""validationModel.TermsAgreement"" />
    <ValidationMessage For=""@(() => validationModel.TermsAgreement)"" />

    <BitButton ButtonType=""BitButtonType.Submit"">Submit</BitButton>
</EditForm>";
    private readonly string example13CsharpCode = @"
private BitToggleValidationModel validationModel = new();

public class BitToggleValidationModel
{
    [Range(typeof(bool), ""true"", ""true"", ErrorMessage = ""You must agree to the terms and conditions."")]
    public bool TermsAgreement { get; set; }
}

private void HandleValidSubmit() { }
private void HandleInvalidSubmit() { }";

    private readonly string example14RazorCode = @"
<BitToggle Label=""Focus me with Tab, flip me with Space"" />
<BitToggle Label=""Hover over my knob"" Title=""The native tooltip of the toggle"" />


<BitButton OnClick=""FocusTheToggle"">Focus the toggle</BitButton>
<BitToggle @ref=""toggleRef"" Label=""Programmatic focus target"" />


<BitToggle AriaLabel=""Dark mode"" />
<BitToggle Label=""Auto renew"" AriaDescription=""The subscription will be renewed one day before it expires."" />


<div id=""offline-mode-heading""><b>Offline mode</b></div>
<div id=""offline-mode-hint"">Keep a copy of the last synced data on this device.</div>

<BitToggle AriaLabelledby=""offline-mode-heading"" AriaDescribedby=""offline-mode-hint"" Text=""Enabled"" />


<BitToggle Label=""Show advanced settings"" AriaControls=""advanced-settings-panel""
           @bind-Value=""advancedSettingsVisible"" />

<div id=""advanced-settings-panel"">
    @if (advancedSettingsVisible)
    {
        <BitMessage Color=""BitColor.Info"">The panel this toggle controls.</BitMessage>
    }
</div>


<BitToggle Text=""Dark mode"" />
<BitToggle Label=""Notifications"" OnText=""Allowed"" OffText=""Blocked"" />";
    private readonly string example14CsharpCode = @"
private BitToggle toggleRef = default!;
private bool advancedSettingsVisible;

private async Task FocusTheToggle()
{
    await toggleRef.FocusAsync();
}";

    private readonly string example15RazorCode = @"
<BitToggle Label=""Auto renew""
           Description=""The subscription is renewed one day before it expires."" />


<div style=""max-width:32rem"">
    <BitToggle FullWidth Inline
               Label=""Offline mode""
               Description=""Keep a copy of the last synced data on this device."" />

    <BitToggle FullWidth Inline
               Label=""Usage statistics""
               Description=""Send anonymous usage data to help us prioritize what to build next."" />
</div>


<BitToggle Label=""Beta features"">
    <DescriptionTemplate>
        Turning this on opts you into features that are still changing.
        <BitLink Href=""https://bitplatform.dev"" Target=""_blank"">Learn more</BitLink>
    </DescriptionTemplate>
</BitToggle>";

    private readonly string example16RazorCode = @"
<BitToggle Color=""BitColor.Primary"" Label=""Primary"" Value />
<BitToggle Color=""BitColor.Secondary"" Label=""Secondary"" Value />
<BitToggle Color=""BitColor.Tertiary"" Label=""Tertiary"" Value />
<BitToggle Color=""BitColor.Info"" Label=""Info"" Value />
<BitToggle Color=""BitColor.Success"" Label=""Success"" Value />
<BitToggle Color=""BitColor.Warning"" Label=""Warning"" Value />
<BitToggle Color=""BitColor.SevereWarning"" Label=""SevereWarning"" Value />
<BitToggle Color=""BitColor.Error"" Label=""Error"" Value />

<div style=""background:var(--bit-clr-fg-sec);color:var(--bit-clr-bg-sec);padding:1rem"">
    <BitToggle Color=""BitColor.PrimaryBackground"" Label=""PrimaryBackground"" Value />
    <BitToggle Color=""BitColor.SecondaryBackground"" Label=""SecondaryBackground"" Value />
    <BitToggle Color=""BitColor.TertiaryBackground"" Label=""TertiaryBackground"" Value />
</div>

<BitToggle Color=""BitColor.PrimaryForeground"" Label=""PrimaryForeground"" Value />
<BitToggle Color=""BitColor.SecondaryForeground"" Label=""SecondaryForeground"" Value />
<BitToggle Color=""BitColor.TertiaryForeground"" Label=""TertiaryForeground"" Value />

<BitToggle Color=""BitColor.PrimaryBorder"" Label=""PrimaryBorder"" Value />
<BitToggle Color=""BitColor.SecondaryBorder"" Label=""SecondaryBorder"" Value />
<BitToggle Color=""BitColor.TertiaryBorder"" Label=""TertiaryBorder"" Value />


<BitToggle Color=""BitColor.Success"" Label=""Success"" />
<BitToggle Color=""BitColor.Warning"" Label=""Warning"" />
<BitToggle Color=""BitColor.Error"" Label=""Error"" />";

    private readonly string example17RazorCode = @"
<link rel=""stylesheet"" href=""https://cdnjs.cloudflare.com/ajax/libs/font-awesome/7.0.1/css/all.min.css"" />

<BitToggle Label=""Check / Xmark (string)"" OnIcon=""@(""fa-solid fa-check"")"" OffIcon=""@(""fa-solid fa-xmark"")"" />

<BitToggle Label=""Sun / Moon (BitIconInfo.Fa)"" Color=""BitColor.Warning""
           OnIcon=""@BitIconInfo.Fa(""solid sun"")"" OffIcon=""@BitIconInfo.Fa(""solid moon"")"" />

<BitToggle Label=""Rocket (BitIconInfo.Css)"" Color=""BitColor.Error"" OnIcon=""@BitIconInfo.Css(""fa-solid fa-rocket"")"" />


<link rel=""stylesheet"" href=""https://cdn.jsdelivr.net/npm/bootstrap-icons@1.11.3/font/bootstrap-icons.min.css"" />

<BitToggle Label=""Check / X (string)"" OnIcon=""@(""bi bi-check-lg"")"" OffIcon=""@(""bi bi-x-lg"")"" />

<BitToggle Label=""Bell (BitIconInfo.Bi)"" Color=""BitColor.Success""
           OnIcon=""@BitIconInfo.Bi(""bell-fill"")"" OffIcon=""@BitIconInfo.Bi(""bell-slash"")"" />";

    private readonly string example18RazorCode = @"
<BitToggle Size=""BitSize.Small"" Label=""Off"" />
<BitToggle Size=""BitSize.Small"" Label=""On"" Value />
<BitToggle Size=""BitSize.Small"" Label=""With icons"" Value OnIconName=""@BitIconName.Accept"" OffIconName=""@BitIconName.Cancel"" />

<BitToggle Size=""BitSize.Medium"" Label=""Off"" />
<BitToggle Size=""BitSize.Medium"" Label=""On"" Value />
<BitToggle Size=""BitSize.Medium"" Label=""With icons"" Value OnIconName=""@BitIconName.Accept"" OffIconName=""@BitIconName.Cancel"" />

<BitToggle Size=""BitSize.Large"" Label=""Off"" />
<BitToggle Size=""BitSize.Large"" Label=""On"" Value />
<BitToggle Size=""BitSize.Large"" Label=""With icons"" Value OnIconName=""@BitIconName.Accept"" OffIconName=""@BitIconName.Cancel"" />";

    private readonly string example19RazorCode = @"
<style>
    .custom-class {
        padding: 0.5rem;
        border-radius: 0.25rem;
        background-color: #d3d3d347;
        border: 1px solid dodgerblue;
    }

    .custom-thumb {
        background: #fff;
        width: 30px;
        height: 30px;
    }

    .custom-button {
        padding: 0;
        width: 52px;
        height: 22px;
        border: none;
        background: #ccc;
        border-radius: 11px;
    }

    .custom-check .custom-thumb {
        background: #ff6868;
    }

    .custom-check .custom-button {
        background: #ffcece;
    }

    .custom-check .custom-button:hover .custom-thumb {
        background: #ff6868;
    }

    .custom-on-content {
        font-weight: 700;
        letter-spacing: 0.1em;
    }

    .custom-off-content {
        opacity: 0.75;
        font-style: italic;
    }
</style>


<BitToggle Label=""Styled toggle"" Style=""background-color:#ff6a00b3;padding:0.5rem;border-radius:0.25rem"" />

<BitToggle Label=""Classed toggle"" Class=""custom-class"" />


<BitToggle Label=""Styles""
           Styles=""@(new() { Root = ""--toggle-background: lightgray;"", Checked = ""--toggle-background: #2ecc71;"",
                             Thumb = ""background: whitesmoke; height: 28px; width: 28px;"",
                             Button = ""background: var(--toggle-background); border: none; border-radius: 60px; padding: 0; height: 30px; width: 50px;"" } )"" />

<BitToggle Label=""Classes""
           Classes=""@(new() { Thumb = ""custom-thumb"",
                              Button = ""custom-button"",
                              Checked = ""custom-check"" } )"" />

<BitToggle Label=""Each side of the track content""
           Classes=""@(new() { OnContent = ""custom-on-content"",
                              OffContent = ""custom-off-content"" } )"">
    <OnContent>ON</OnContent>
    <OffContent>OFF</OffContent>
</BitToggle>";

    private readonly string example20RazorCode = @"
<BitToggle Label=""این یک تاگل است"" Dir=""BitDir.Rtl"" OnText=""روشن"" OffText=""خاموش"" />

<BitToggle Label=""این یک تاگل خطی است"" Dir=""BitDir.Rtl"" Inline />

<BitToggle Label=""این یک تاگل خطی برعکس است"" Dir=""BitDir.Rtl"" Reversed Inline />

<BitToggle Label=""این یک تاگل با محتوا است"" Dir=""BitDir.Rtl"" OnIconName=""@BitIconName.Accept"" OffIconName=""@BitIconName.Cancel"">
    <OnContent>روشن</OnContent>
    <OffContent>خاموش</OffContent>
</BitToggle>";
}
