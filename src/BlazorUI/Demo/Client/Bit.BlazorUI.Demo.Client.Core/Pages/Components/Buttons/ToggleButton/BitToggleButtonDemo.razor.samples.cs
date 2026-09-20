namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Buttons.ToggleButton;

public partial class BitToggleButtonDemo
{
    private readonly string example1RazorCode = @"
<BitToggleButton>Microphone</BitToggleButton>";

    private readonly string example2RazorCode = @"
<BitToggleButton Variant=""BitVariant.Fill"">Fill</BitToggleButton>
<BitToggleButton Variant=""BitVariant.Outline"">Outline</BitToggleButton>
<BitToggleButton Variant=""BitVariant.Text"">Text</BitToggleButton>

<BitToggleButton Variant=""BitVariant.Fill"" IsEnabled=""false"">Fill</BitToggleButton>
<BitToggleButton Variant=""BitVariant.Outline"" IsEnabled=""false"">Outline</BitToggleButton>
<BitToggleButton Variant=""BitVariant.Text"" IsEnabled=""false"">Text</BitToggleButton>

<BitToggleButton IsEnabled=""false"" Title=""Connect a microphone to enable this"">Disabled, with a reason</BitToggleButton>
<BitToggleButton IsEnabled=""false"" AllowDisabledFocus=""false"">Not focusable</BitToggleButton>";

    private readonly string example3RazorCode = @"
<BitToggleButton Text=""Microphone"" />

<BitToggleButton OnText=""Muted"" OffText=""Unmuted"" />

<BitToggleButton IconOnly Title=""Microphone"" AriaLabel=""Microphone""
                 IconName=""@BitIconName.Microphone"" />

<BitToggleButton IconOnly AriaLabel=""Mute""
                 OnTitle=""Click to unmute"" OnIconName=""@BitIconName.MicOff""
                 OffTitle=""Click to mute"" OffIconName=""@BitIconName.Microphone"" />";

    private readonly string example4RazorCode = @"
<BitToggleButton Text=""Microphone"" IconName=""@BitIconName.Microphone"" />

<BitToggleButton OnText=""Muted"" OnIconName=""@BitIconName.MicOff""
                 OffText=""Unmuted"" OffIconName=""@BitIconName.Microphone"" />

<BitToggleButton IconPosition=""BitIconPosition.Start"" Text=""Start"" IconName=""@BitIconName.Microphone"" />
<BitToggleButton IconPosition=""BitIconPosition.End"" Text=""End"" IconName=""@BitIconName.Microphone"" />

<BitToggleButton AriaLabel=""Mute"" IconName=""@BitIconName.Microphone"" />
<BitToggleButton AriaLabel=""Mute""
                 OnIconName=""@BitIconName.MicOff"" OffIconName=""@BitIconName.Microphone"" />
<BitToggleButton IconOnly AriaLabel=""Mute"" IconName=""@BitIconName.Microphone"" Text=""Microphone"" />
<BitToggleButton IconOnly AriaLabel=""Mute""
                 OnText=""Muted"" OnIconName=""@BitIconName.MicOff""
                 OffText=""Unmuted"" OffIconName=""@BitIconName.Microphone"" />";

    private readonly string example5RazorCode = @"
<BitToggleButton DefaultIsChecked=""true""
                 OnText=""Muted"" OnIconName=""@BitIconName.MicOff""
                 OffText=""Unmuted"" OffIconName=""@BitIconName.Microphone"" />

<BitToggleButton @bind-IsChecked=""twoWayBoundValue""
                 Text=""@(twoWayBoundValue ? ""Muted"" : ""Unmuted"")""
                 IconName=""@(twoWayBoundValue ? BitIconName.MicOff : BitIconName.Microphone)"" />
<BitCheckbox Label=""Checked Toggle Button"" @bind-Value=""twoWayBoundValue"" />

<BitToggleButton OnChange=""v => onChangeValue = v""
                 OnText=""Muted"" OnIconName=""@BitIconName.MicOff""
                 OffText=""Unmuted"" OffIconName=""@BitIconName.Microphone"" />
<div>Check status: @onChangeValue</div>

<BitToggleButton @ref=""programmaticToggleRef"" OnText=""Muted"" OffText=""Unmuted"" />
<BitButton Variant=""BitVariant.Outline"" OnClick=""() => programmaticToggleRef.ToggleAsync()"">Toggle it</BitButton>
<BitButton Variant=""BitVariant.Outline"" OnClick=""FocusTheToggleButton"">Focus it</BitButton>";
    private readonly string example5CsharpCode = @"
private bool twoWayBoundValue;
private bool onChangeValue;
private BitToggleButton programmaticToggleRef = default!;

private async Task FocusTheToggleButton() => await programmaticToggleRef.FocusAsync();";

    private readonly string example6RazorCode = @"
<BitToggleButton OnColor=""BitColor.Success"" OffColor=""BitColor.Error""
                 OnText=""Recording"" OnIconName=""@BitIconName.CircleFill""
                 OffText=""Stopped"" OffIconName=""@BitIconName.CircleStopSolid"" />
<BitToggleButton DefaultIsChecked=""true""
                 OnColor=""BitColor.Success"" OffColor=""BitColor.Error""
                 OnText=""Recording"" OnIconName=""@BitIconName.CircleFill""
                 OffText=""Stopped"" OffIconName=""@BitIconName.CircleStopSolid"" />

<BitToggleButton OnVariant=""BitVariant.Fill"" OffVariant=""BitVariant.Outline""
                 OnText=""Following"" OffText=""Follow"" />
<BitToggleButton DefaultIsChecked=""true""
                 OnVariant=""BitVariant.Fill"" OffVariant=""BitVariant.Outline""
                 OnText=""Following"" OffText=""Follow"" />

<BitToggleButton Color=""BitColor.Info""
                 OnColor=""BitColor.Warning"" OnVariant=""BitVariant.Fill"" OffVariant=""BitVariant.Text""
                 OnText=""Notifications muted"" OnIconName=""@BitIconName.RingerOff""
                 OffText=""Notifications on"" OffIconName=""@BitIconName.Ringer"" />
<BitToggleButton DefaultIsChecked=""true"" Color=""BitColor.Info""
                 OnColor=""BitColor.Warning"" OnVariant=""BitVariant.Fill"" OffVariant=""BitVariant.Text""
                 OnText=""Notifications muted"" OnIconName=""@BitIconName.RingerOff""
                 OffText=""Notifications on"" OffIconName=""@BitIconName.Ringer"" />

<BitToggleButton IsEnabled=""false"" Variant=""BitVariant.Fill"" Text=""Fill"" />
<BitToggleButton IsEnabled=""false"" Variant=""BitVariant.Fill"" DefaultIsChecked=""true"" Text=""Fill"" />
<BitToggleButton IsEnabled=""false"" Variant=""BitVariant.Outline"" Text=""Outline"" />
<BitToggleButton IsEnabled=""false"" Variant=""BitVariant.Outline"" DefaultIsChecked=""true"" Text=""Outline"" />
<BitToggleButton IsEnabled=""false"" Variant=""BitVariant.Text"" Text=""Text"" />
<BitToggleButton IsEnabled=""false"" Variant=""BitVariant.Text"" DefaultIsChecked=""true"" Text=""Text"" />";

    private readonly string example7RazorCode = @"
<BitToggleButton ShowCheckMark Variant=""BitVariant.Outline"" DefaultIsChecked=""true"" Text=""Bold"" />
<BitToggleButton ShowCheckMark Variant=""BitVariant.Outline"" Text=""Italic"" />

<BitToggleButton ShowCheckMark FixedCheckMark Variant=""BitVariant.Outline"" DefaultIsChecked=""true"" Text=""Bold"" />
<BitToggleButton ShowCheckMark FixedCheckMark Variant=""BitVariant.Outline"" Text=""Italic"" />

<BitToggleButton ShowCheckMark FixedCheckMark CheckMarkIconName=""@BitIconName.FavoriteStarFill""
                 Variant=""BitVariant.Outline"" Color=""BitColor.Warning""
                 DefaultIsChecked=""true"" Text=""Favorite"" />";

    private readonly string example8RazorCode = @"
<style>
    .custom-template {
        gap: 0.5rem;
        display: flex;
        align-items: center;
    }
</style>


<BitToggleButton>
    <div class=""custom-template"">
        <BitIcon IconName=""@BitIconName.Airplane"" Color=""BitColor.Tertiary"" />
        <span>Custom template</span>
        <BitRollerLoading CustomSize=""20"" Color=""BitColor.Tertiary"" />
    </div>
</BitToggleButton>

<BitToggleButton AriaLabel=""Subscribe to this channel"">
    <OnTemplate>
        <div class=""custom-template"">
            <BitIcon IconName=""@BitIconName.CheckMark"" Color=""BitColor.Success"" />
            <span>Subscribed</span>
        </div>
    </OnTemplate>
    <OffTemplate>
        <div class=""custom-template"">
            <BitIcon IconName=""@BitIconName.Add"" Color=""BitColor.Tertiary"" />
            <span>Subscribe</span>
        </div>
    </OffTemplate>
</BitToggleButton>";

    private readonly string example9RazorCode = @"
<BitToggleButton OnClick=""() => clickCounter++"">
    Click me (@clickCounter)
</BitToggleButton>

<BitCheckbox Label=""Allow the change"" @bind-Value=""allowChange"" />
<BitToggleButton OnChanging=""HandleOnChanging"" OnText=""Published"" OffText=""Draft"" />
<div>Cancelled attempts: @cancelledCounter</div>

<style>
    .clickable-box {
        cursor: pointer;
        padding: 1rem;
        border-radius: 0.25rem;
        border: 1px dashed var(--bit-clr-brd-sec);
    }
</style>

<div class=""clickable-box"" @onclick=""() => containerClickCounter++"">
    <BitToggleButton Text=""Bubbles up"" />
    <BitToggleButton StopPropagation Text=""Stops here"" />
</div>
<div>Container clicks: @containerClickCounter</div>";
    private readonly string example9CsharpCode = @"
private int clickCounter;
private bool allowChange;
private int cancelledCounter;
private int containerClickCounter;

private void HandleOnChanging(BitToggleButtonChangeArgs args)
{
    if (allowChange) return;

    args.Cancel = true;
    cancelledCounter++;
}";

    private readonly string example10RazorCode = @"
<BitToggleButton IsLoading=""isLoading"" OnText=""Muted"" OffText=""Unmuted"" />
<BitCheckbox Label=""IsLoading"" @bind-Value=""isLoading"" />

<BitToggleButton IsLoading OnClick=""() => blockedClickCounter++"" Text=""@($""Blocked ({blockedClickCounter})"")"" />
<BitToggleButton IsLoading Reclickable OnClick=""() => reclickCounter++"" Text=""@($""Reclickable ({reclickCounter})"")"" />

<BitToggleButton AutoLoading OnChange=""HandleAutoLoadingChange""
                 OnText=""Muted"" OffText=""Unmuted"" />

<BitToggleButton AutoLoading OnChange=""HandleAutoLoadingChange"" Text=""No delay"" />
<BitToggleButton AutoLoading LoadingDelay=""700"" OnChange=""HandleAutoLoadingChange"" Text=""700ms delay"" />

<BitToggleButton IsLoading LoadingLabel=""Saving..."" LoadingLabelPosition=""BitLabelPosition.End"" Text=""End"" />
<BitToggleButton IsLoading LoadingLabel=""Saving..."" LoadingLabelPosition=""BitLabelPosition.Start"" Text=""Start"" />
<BitToggleButton IsLoading LoadingLabel=""Saving..."" LoadingLabelPosition=""BitLabelPosition.Top"" Text=""Top"" />
<BitToggleButton IsLoading LoadingLabel=""Saving..."" LoadingLabelPosition=""BitLabelPosition.Bottom"" Text=""Bottom"" />

<BitToggleButton IsLoading Text=""Muted"">
    <LoadingTemplate>
        <div class=""custom-template"">
            <BitRollerLoading CustomSize=""20"" Color=""BitColor.PrimaryForeground"" />
            <span>Working on it...</span>
        </div>
    </LoadingTemplate>
</BitToggleButton>";
    private readonly string example10CsharpCode = @"
private bool isLoading;
private int reclickCounter;
private int blockedClickCounter;

private async Task HandleAutoLoadingChange()
{
    // stands in for persisting the new state somewhere slow
    await Task.Delay(2000);
}";

    private readonly string example11RazorCode = @"
<style>
    .narrow-column {
        gap: 0.5rem;
        display: flex;
        max-width: 16rem;
        flex-flow: column nowrap;
    }
</style>


<div class=""narrow-column"">
    <BitToggleButton FullWidth OnText=""Muted"" OnIconName=""@BitIconName.MicOff""
                     OffText=""Unmuted"" OffIconName=""@BitIconName.Microphone"" />
</div>


<div class=""narrow-column"">
    <BitToggleButton FullWidth
                     OnText=""Syncing every folder on this device""
                     OffText=""Sync every folder on this device"" />

    <BitToggleButton FullWidth NoWrap Title=""Sync every folder on this device""
                     OnText=""Syncing every folder on this device""
                     OffText=""Sync every folder on this device"" />
</div>";

    private readonly string example12RazorCode = @"
<BitToggleButton Variant=""BitVariant.Outline"" Color=""BitColor.TertiaryBackground""
                 OnText=""Muted"" OnIconName=""@BitIconName.MicOff""
                 OffText=""Unmuted"" OffIconName=""@BitIconName.Microphone"" />

<BitToggleButton FixedColor Variant=""BitVariant.Outline"" Color=""BitColor.TertiaryBackground""
                 OnText=""Muted"" OnIconName=""@BitIconName.MicOff""
                 OffText=""Unmuted"" OffIconName=""@BitIconName.Microphone"" />

<BitToggleButton Variant=""BitVariant.Text"" Color=""BitColor.TertiaryBackground""
                 IconOnly AriaLabel=""Mute""
                 OnIconName=""@BitIconName.MicOff"" OffIconName=""@BitIconName.Microphone"" />

<BitToggleButton FixedColor Variant=""BitVariant.Text"" Color=""BitColor.TertiaryBackground""
                 IconOnly AriaLabel=""Mute""
                 OnIconName=""@BitIconName.MicOff"" OffIconName=""@BitIconName.Microphone"" />";

    private readonly string example13RazorCode = @"
<BitToggleButton OnText=""Muted"" OffText=""Unmuted"" />

<BitToggleButton AriaLabel=""Mute"" OnText=""Muted"" OffText=""Unmuted"" />

<BitToggleButton IconOnly IconName=""@BitIconName.Microphone""
                 OnTitle=""Click to unmute"" OffTitle=""Click to mute"" />
<BitToggleButton IconOnly IconName=""@BitIconName.Microphone"" AriaLabel=""Mute""
                 OnTitle=""Click to unmute"" OffTitle=""Click to mute"" />

<BitToggleButton AriaMode=""BitToggleButtonAriaMode.Pressed"" AriaLabel=""Bold"" IconOnly IconName=""@BitIconName.Bold"" />
<BitToggleButton AriaMode=""BitToggleButtonAriaMode.Switch"" AriaLabel=""Airplane mode"" Text=""Airplane mode"" />
<BitToggleButton AriaMode=""BitToggleButtonAriaMode.None""
                 OnText=""Pause"" OnIconName=""@BitIconName.Pause""
                 OffText=""Play"" OffIconName=""@BitIconName.Play"" />

<BitToggleButton IconOnly
                 OnAriaLabel=""Unmute"" OnIconName=""@BitIconName.MicOff""
                 OffAriaLabel=""Mute"" OffIconName=""@BitIconName.Microphone"" />

<span id=""wifi-label"">Wi-Fi</span>
<BitToggleButton AriaLabelledBy=""wifi-label"" AriaMode=""BitToggleButtonAriaMode.Switch""
                 IconOnly IconName=""@BitIconName.StatusCircleCheckmark"" />

<BitToggleButton AriaLabel=""Show details""
                 AriaMode=""BitToggleButtonAriaMode.Expanded""
                 AriaControls=""accessibility-details""
                 AriaDescription=""Expands the details panel below the button.""
                 @bind-IsChecked=""detailsVisible""
                 Text=""Details"" />
<div id=""accessibility-details"" hidden=""@(detailsVisible is false)"">The details panel is visible.</div>

<BitToggleButton role=""checkbox"" Variant=""BitVariant.Text""
                 @bind-IsChecked=""statusBarVisible""
                 AriaLabel=""Show the status bar"" ShowCheckMark FixedCheckMark Text=""Status bar"" />";

    private readonly string example14RazorCode = @"
<BitToggleButton Variant=""BitVariant.Fill"" Color=""BitColor.Primary"">Primary</BitToggleButton>
<BitToggleButton Variant=""BitVariant.Outline"" Color=""BitColor.Primary"">Primary</BitToggleButton>
<BitToggleButton Variant=""BitVariant.Text"" Color=""BitColor.Primary"">Primary</BitToggleButton>

<BitToggleButton Variant=""BitVariant.Fill"" Color=""BitColor.Secondary"">Secondary</BitToggleButton>
<BitToggleButton Variant=""BitVariant.Outline"" Color=""BitColor.Secondary"">Secondary</BitToggleButton>
<BitToggleButton Variant=""BitVariant.Text"" Color=""BitColor.Secondary"">Secondary</BitToggleButton>

<BitToggleButton Variant=""BitVariant.Fill"" Color=""BitColor.Tertiary"">Tertiary</BitToggleButton>
<BitToggleButton Variant=""BitVariant.Outline"" Color=""BitColor.Tertiary"">Tertiary</BitToggleButton>
<BitToggleButton Variant=""BitVariant.Text"" Color=""BitColor.Tertiary"">Tertiary</BitToggleButton>

<BitToggleButton Variant=""BitVariant.Fill"" Color=""BitColor.Info"">Info</BitToggleButton>
<BitToggleButton Variant=""BitVariant.Outline"" Color=""BitColor.Info"">Info</BitToggleButton>
<BitToggleButton Variant=""BitVariant.Text"" Color=""BitColor.Info"">Info</BitToggleButton>

<BitToggleButton Variant=""BitVariant.Fill"" Color=""BitColor.Success"">Success</BitToggleButton>
<BitToggleButton Variant=""BitVariant.Outline"" Color=""BitColor.Success"">Success</BitToggleButton>
<BitToggleButton Variant=""BitVariant.Text"" Color=""BitColor.Success"">Success</BitToggleButton>

<BitToggleButton Variant=""BitVariant.Fill"" Color=""BitColor.Warning"">Warning</BitToggleButton>
<BitToggleButton Variant=""BitVariant.Outline"" Color=""BitColor.Warning"">Warning</BitToggleButton>
<BitToggleButton Variant=""BitVariant.Text"" Color=""BitColor.Warning"">Warning</BitToggleButton>

<BitToggleButton Variant=""BitVariant.Fill"" Color=""BitColor.SevereWarning"">SevereWarning</BitToggleButton>
<BitToggleButton Variant=""BitVariant.Outline"" Color=""BitColor.SevereWarning"">SevereWarning</BitToggleButton>
<BitToggleButton Variant=""BitVariant.Text"" Color=""BitColor.SevereWarning"">SevereWarning</BitToggleButton>

<BitToggleButton Variant=""BitVariant.Fill"" Color=""BitColor.Error"">Error</BitToggleButton>
<BitToggleButton Variant=""BitVariant.Outline"" Color=""BitColor.Error"">Error</BitToggleButton>
<BitToggleButton Variant=""BitVariant.Text"" Color=""BitColor.Error"">Error</BitToggleButton>

<BitToggleButton Variant=""BitVariant.Fill"" Color=""BitColor.PrimaryBackground"">PrimaryBackground</BitToggleButton>
<BitToggleButton Variant=""BitVariant.Outline"" Color=""BitColor.PrimaryBackground"">PrimaryBackground</BitToggleButton>
<BitToggleButton Variant=""BitVariant.Text"" Color=""BitColor.PrimaryBackground"">PrimaryBackground</BitToggleButton>

<BitToggleButton Variant=""BitVariant.Fill"" Color=""BitColor.SecondaryBackground"">SecondaryBackground</BitToggleButton>
<BitToggleButton Variant=""BitVariant.Outline"" Color=""BitColor.SecondaryBackground"">SecondaryBackground</BitToggleButton>
<BitToggleButton Variant=""BitVariant.Text"" Color=""BitColor.SecondaryBackground"">SecondaryBackground</BitToggleButton>

<BitToggleButton Variant=""BitVariant.Fill"" Color=""BitColor.TertiaryBackground"">TertiaryBackground</BitToggleButton>
<BitToggleButton Variant=""BitVariant.Outline"" Color=""BitColor.TertiaryBackground"">TertiaryBackground</BitToggleButton>
<BitToggleButton Variant=""BitVariant.Text"" Color=""BitColor.TertiaryBackground"">TertiaryBackground</BitToggleButton>

<BitToggleButton Variant=""BitVariant.Fill"" Color=""BitColor.PrimaryForeground"">PrimaryForeground</BitToggleButton>
<BitToggleButton Variant=""BitVariant.Outline"" Color=""BitColor.PrimaryForeground"">PrimaryForeground</BitToggleButton>
<BitToggleButton Variant=""BitVariant.Text"" Color=""BitColor.PrimaryForeground"">PrimaryForeground</BitToggleButton>

<BitToggleButton Variant=""BitVariant.Fill"" Color=""BitColor.SecondaryForeground"">SecondaryForeground</BitToggleButton>
<BitToggleButton Variant=""BitVariant.Outline"" Color=""BitColor.SecondaryForeground"">SecondaryForeground</BitToggleButton>
<BitToggleButton Variant=""BitVariant.Text"" Color=""BitColor.SecondaryForeground"">SecondaryForeground</BitToggleButton>

<BitToggleButton Variant=""BitVariant.Fill"" Color=""BitColor.TertiaryForeground"">TertiaryForeground</BitToggleButton>
<BitToggleButton Variant=""BitVariant.Outline"" Color=""BitColor.TertiaryForeground"">TertiaryForeground</BitToggleButton>
<BitToggleButton Variant=""BitVariant.Text"" Color=""BitColor.TertiaryForeground"">TertiaryForeground</BitToggleButton>

<BitToggleButton Variant=""BitVariant.Fill"" Color=""BitColor.PrimaryBorder"">PrimaryBorder</BitToggleButton>
<BitToggleButton Variant=""BitVariant.Outline"" Color=""BitColor.PrimaryBorder"">PrimaryBorder</BitToggleButton>
<BitToggleButton Variant=""BitVariant.Text"" Color=""BitColor.PrimaryBorder"">PrimaryBorder</BitToggleButton>

<BitToggleButton Variant=""BitVariant.Fill"" Color=""BitColor.SecondaryBorder"">SecondaryBorder</BitToggleButton>
<BitToggleButton Variant=""BitVariant.Outline"" Color=""BitColor.SecondaryBorder"">SecondaryBorder</BitToggleButton>
<BitToggleButton Variant=""BitVariant.Text"" Color=""BitColor.SecondaryBorder"">SecondaryBorder</BitToggleButton>

<BitToggleButton Variant=""BitVariant.Fill"" Color=""BitColor.TertiaryBorder"">TertiaryBorder</BitToggleButton>
<BitToggleButton Variant=""BitVariant.Outline"" Color=""BitColor.TertiaryBorder"">TertiaryBorder</BitToggleButton>
<BitToggleButton Variant=""BitVariant.Text"" Color=""BitColor.TertiaryBorder"">TertiaryBorder</BitToggleButton>


<BitToggleButton IsEnabled=""false"" Variant=""BitVariant.Fill"" Color=""BitColor.Primary"">Primary</BitToggleButton>
<BitToggleButton IsEnabled=""false"" Variant=""BitVariant.Outline"" Color=""BitColor.Primary"">Primary</BitToggleButton>
<BitToggleButton IsEnabled=""false"" Variant=""BitVariant.Text"" Color=""BitColor.Primary"">Primary</BitToggleButton>

<BitToggleButton IsEnabled=""false"" Variant=""BitVariant.Fill"" Color=""BitColor.Secondary"">Secondary</BitToggleButton>
<BitToggleButton IsEnabled=""false"" Variant=""BitVariant.Outline"" Color=""BitColor.Secondary"">Secondary</BitToggleButton>
<BitToggleButton IsEnabled=""false"" Variant=""BitVariant.Text"" Color=""BitColor.Secondary"">Secondary</BitToggleButton>

<BitToggleButton IsEnabled=""false"" Variant=""BitVariant.Fill"" Color=""BitColor.Tertiary"">Tertiary</BitToggleButton>
<BitToggleButton IsEnabled=""false"" Variant=""BitVariant.Outline"" Color=""BitColor.Tertiary"">Tertiary</BitToggleButton>
<BitToggleButton IsEnabled=""false"" Variant=""BitVariant.Text"" Color=""BitColor.Tertiary"">Tertiary</BitToggleButton>

<BitToggleButton IsEnabled=""false"" Variant=""BitVariant.Fill"" Color=""BitColor.Info"">Info</BitToggleButton>
<BitToggleButton IsEnabled=""false"" Variant=""BitVariant.Outline"" Color=""BitColor.Info"">Info</BitToggleButton>
<BitToggleButton IsEnabled=""false"" Variant=""BitVariant.Text"" Color=""BitColor.Info"">Info</BitToggleButton>

<BitToggleButton IsEnabled=""false"" Variant=""BitVariant.Fill"" Color=""BitColor.Success"">Success</BitToggleButton>
<BitToggleButton IsEnabled=""false"" Variant=""BitVariant.Outline"" Color=""BitColor.Success"">Success</BitToggleButton>
<BitToggleButton IsEnabled=""false"" Variant=""BitVariant.Text"" Color=""BitColor.Success"">Success</BitToggleButton>

<BitToggleButton IsEnabled=""false"" Variant=""BitVariant.Fill"" Color=""BitColor.Warning"">Warning</BitToggleButton>
<BitToggleButton IsEnabled=""false"" Variant=""BitVariant.Outline"" Color=""BitColor.Warning"">Warning</BitToggleButton>
<BitToggleButton IsEnabled=""false"" Variant=""BitVariant.Text"" Color=""BitColor.Warning"">Warning</BitToggleButton>

<BitToggleButton IsEnabled=""false"" Variant=""BitVariant.Fill"" Color=""BitColor.SevereWarning"">SevereWarning</BitToggleButton>
<BitToggleButton IsEnabled=""false"" Variant=""BitVariant.Outline"" Color=""BitColor.SevereWarning"">SevereWarning</BitToggleButton>
<BitToggleButton IsEnabled=""false"" Variant=""BitVariant.Text"" Color=""BitColor.SevereWarning"">SevereWarning</BitToggleButton>

<BitToggleButton IsEnabled=""false"" Variant=""BitVariant.Fill"" Color=""BitColor.Error"">Error</BitToggleButton>
<BitToggleButton IsEnabled=""false"" Variant=""BitVariant.Outline"" Color=""BitColor.Error"">Error</BitToggleButton>
<BitToggleButton IsEnabled=""false"" Variant=""BitVariant.Text"" Color=""BitColor.Error"">Error</BitToggleButton>

<div style=""background:var(--bit-clr-fg-sec);padding:1rem"">
    <BitToggleButton IsEnabled=""false"" Variant=""BitVariant.Fill"" Color=""BitColor.PrimaryBackground"">PrimaryBackground</BitToggleButton>
    <BitToggleButton IsEnabled=""false"" Variant=""BitVariant.Outline"" Color=""BitColor.PrimaryBackground"">PrimaryBackground</BitToggleButton>
    <BitToggleButton IsEnabled=""false"" Variant=""BitVariant.Text"" Color=""BitColor.PrimaryBackground"">PrimaryBackground</BitToggleButton>

    <BitToggleButton IsEnabled=""false"" Variant=""BitVariant.Fill"" Color=""BitColor.SecondaryBackground"">SecondaryBackground</BitToggleButton>
    <BitToggleButton IsEnabled=""false"" Variant=""BitVariant.Outline"" Color=""BitColor.SecondaryBackground"">SecondaryBackground</BitToggleButton>
    <BitToggleButton IsEnabled=""false"" Variant=""BitVariant.Text"" Color=""BitColor.SecondaryBackground"">SecondaryBackground</BitToggleButton>

    <BitToggleButton IsEnabled=""false"" Variant=""BitVariant.Fill"" Color=""BitColor.TertiaryBackground"">TertiaryBackground</BitToggleButton>
    <BitToggleButton IsEnabled=""false"" Variant=""BitVariant.Outline"" Color=""BitColor.TertiaryBackground"">TertiaryBackground</BitToggleButton>
    <BitToggleButton IsEnabled=""false"" Variant=""BitVariant.Text"" Color=""BitColor.TertiaryBackground"">TertiaryBackground</BitToggleButton>
</div>

<BitToggleButton IsEnabled=""false"" Variant=""BitVariant.Fill"" Color=""BitColor.PrimaryForeground"">PrimaryForeground</BitToggleButton>
<BitToggleButton IsEnabled=""false"" Variant=""BitVariant.Outline"" Color=""BitColor.PrimaryForeground"">PrimaryForeground</BitToggleButton>
<BitToggleButton IsEnabled=""false"" Variant=""BitVariant.Text"" Color=""BitColor.PrimaryForeground"">PrimaryForeground</BitToggleButton>

<BitToggleButton IsEnabled=""false"" Variant=""BitVariant.Fill"" Color=""BitColor.SecondaryForeground"">SecondaryForeground</BitToggleButton>
<BitToggleButton IsEnabled=""false"" Variant=""BitVariant.Outline"" Color=""BitColor.SecondaryForeground"">SecondaryForeground</BitToggleButton>
<BitToggleButton IsEnabled=""false"" Variant=""BitVariant.Text"" Color=""BitColor.SecondaryForeground"">SecondaryForeground</BitToggleButton>

<BitToggleButton IsEnabled=""false"" Variant=""BitVariant.Fill"" Color=""BitColor.TertiaryForeground"">TertiaryForeground</BitToggleButton>
<BitToggleButton IsEnabled=""false"" Variant=""BitVariant.Outline"" Color=""BitColor.TertiaryForeground"">TertiaryForeground</BitToggleButton>
<BitToggleButton IsEnabled=""false"" Variant=""BitVariant.Text"" Color=""BitColor.TertiaryForeground"">TertiaryForeground</BitToggleButton>

<BitToggleButton IsEnabled=""false"" Variant=""BitVariant.Fill"" Color=""BitColor.PrimaryBorder"">PrimaryBorder</BitToggleButton>
<BitToggleButton IsEnabled=""false"" Variant=""BitVariant.Outline"" Color=""BitColor.PrimaryBorder"">PrimaryBorder</BitToggleButton>
<BitToggleButton IsEnabled=""false"" Variant=""BitVariant.Text"" Color=""BitColor.PrimaryBorder"">PrimaryBorder</BitToggleButton>

<BitToggleButton IsEnabled=""false"" Variant=""BitVariant.Fill"" Color=""BitColor.SecondaryBorder"">SecondaryBorder</BitToggleButton>
<BitToggleButton IsEnabled=""false"" Variant=""BitVariant.Outline"" Color=""BitColor.SecondaryBorder"">SecondaryBorder</BitToggleButton>
<BitToggleButton IsEnabled=""false"" Variant=""BitVariant.Text"" Color=""BitColor.SecondaryBorder"">SecondaryBorder</BitToggleButton>

<BitToggleButton IsEnabled=""false"" Variant=""BitVariant.Fill"" Color=""BitColor.TertiaryBorder"">TertiaryBorder</BitToggleButton>
<BitToggleButton IsEnabled=""false"" Variant=""BitVariant.Outline"" Color=""BitColor.TertiaryBorder"">TertiaryBorder</BitToggleButton>
<BitToggleButton IsEnabled=""false"" Variant=""BitVariant.Text"" Color=""BitColor.TertiaryBorder"">TertiaryBorder</BitToggleButton>";

    private readonly string example15RazorCode = @"
<link rel=""stylesheet"" href=""https://cdnjs.cloudflare.com/ajax/libs/font-awesome/7.0.1/css/all.min.css"" />

<BitToggleButton Icon=""@(""fa-solid fa-microphone"")"" Text=""Microphone"" />

<BitToggleButton OnIcon=""@BitIconInfo.Css(""fa-solid fa-microphone-slash"")"" OnText=""Muted""
                 OffIcon=""@BitIconInfo.Css(""fa-solid fa-microphone"")"" OffText=""Unmuted""
                 Color=""BitColor.Secondary"" />

<BitToggleButton OnIcon=""@BitIconInfo.Fa(""solid volume-xmark"")"" OnText=""Muted""
                 OffIcon=""@BitIconInfo.Fa(""solid volume-high"")"" OffText=""Unmuted""
                 Color=""BitColor.Tertiary"" />

<BitToggleButton OnIcon=""@BitIconInfo.Fa(""regular circle-pause"")"" OnText=""Paused""
                 OffIcon=""@BitIconInfo.Fa(""regular circle-play"")"" OffText=""Playing""
                 Color=""BitColor.Success"" />

<BitToggleButton ShowCheckMark FixedCheckMark
                 CheckMarkIcon=""@BitIconInfo.Fa(""solid check"")""
                 Variant=""BitVariant.Outline"" Text=""Bold"" />";

    private readonly string example16RazorCode = @"
<BitToggleButton Size=""BitSize.Small"" Text=""Small"" IconName=""@BitIconName.Microphone"" />
<BitToggleButton Size=""BitSize.Medium"" Text=""Medium"" IconName=""@BitIconName.Microphone"" />
<BitToggleButton Size=""BitSize.Large"" Text=""Large"" IconName=""@BitIconName.Microphone"" />


<BitToggleButton Size=""BitSize.Small"" IconOnly AriaLabel=""Mute"" IconName=""@BitIconName.Microphone"" />
<BitToggleButton Size=""BitSize.Medium"" IconOnly AriaLabel=""Mute"" IconName=""@BitIconName.Microphone"" />
<BitToggleButton Size=""BitSize.Large"" IconOnly AriaLabel=""Mute"" IconName=""@BitIconName.Microphone"" />";

    private readonly string example17RazorCode = @"
<style>
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
        border: none;
        color: blueviolet;
        background: transparent;
    }

    .custom-text {
        position: relative;
    }

    .custom-root:hover .custom-text {
        color: darkviolet;
    }

    .custom-text::after {
        content: '';
        left: 0;
        width: 0;
        height: 2px;
        bottom: -6px;
        position: absolute;
        transition: 0.3s ease;
        background: linear-gradient(90deg, #ff00cc, #3333ff);
    }

    .custom-icon {
        color: hotpink;
    }

    .custom-checked {
        border: none;
        background-color: transparent;
    }

    .custom-checked .custom-text::after {
        width: 100%;
    }

    .custom-checked .custom-icon {
        color: hotpink;
    }
</style>


<BitToggleButton Style=""background-color: transparent; border-color: blueviolet; color: blueviolet;""
                 Variant=""BitVariant.Outline""
                 OnText=""Styled Button: Muted"" OnIconName=""@BitIconName.MicOff""
                 OffText=""Styled Button: Unmuted"" OffIconName=""@BitIconName.Microphone"" />

<BitToggleButton Class=""custom-class""
                 OnText=""Classed Button: Muted"" OnIconName=""@BitIconName.MicOff""
                 OffText=""Classed Button: Unmuted"" OffIconName=""@BitIconName.Microphone"" />


<BitToggleButton OnText=""Styled Button: Muted"" OnIconName=""@BitIconName.MicOff""
                 OffText=""Styled Button: Unmuted"" OffIconName=""@BitIconName.Microphone""
                 Styles=""@(new() { Root = ""--toggle-background: pink; background: var(--toggle-background); border: none;"",
                                   Checked = ""--toggle-background: peachpuff;"",
                                   Icon = ""color: red;"",
                                   Text = ""color: tomato;"" })"" />

<BitToggleButton Variant=""BitVariant.Text""
                 OnText=""Classed Button: Muted"" OnIconName=""@BitIconName.MicOff""
                 OffText=""Classed Button: Unmuted"" OffIconName=""@BitIconName.Microphone""
                 Classes=""@(new() { Root = ""custom-root"",
                                    Checked = ""custom-checked"",
                                    Icon = ""custom-icon"",
                                    Text = ""custom-text"" })"" />


<BitToggleButton ShowCheckMark FixedCheckMark Variant=""BitVariant.Outline"" Text=""Check mark""
                 Styles=""@(new() { CheckMark = ""color: hotpink;"" })"" />

<BitToggleButton IsLoading LoadingLabel=""Saving..."" Text=""Loading parts""
                 Styles=""@(new() { Spinner = ""border-top-color: gold;"",
                                   LoadingLabel = ""color: gold; font-style: italic;"" })"" />


<BitToggleButton Text=""Pill"" IconName=""@BitIconName.Tag""
                 Style=""--bit-ToggleButton-radius: 999px;"" />

<BitToggleButton Variant=""BitVariant.Outline"" Text=""Tinted while checked""
                 Style=""--bit-ToggleButton-checked-background: color-mix(in srgb, var(--bit-clr-pri) 15%, transparent);
                        --bit-ToggleButton-checked-hover-background: color-mix(in srgb, var(--bit-clr-pri) 25%, transparent);
                        --bit-ToggleButton-checked-color: var(--bit-clr-pri);
                        --bit-ToggleButton-checked-border-color: var(--bit-clr-pri);"" />

<BitToggleButton Text=""Big glyph, wide gap"" IconName=""@BitIconName.Zoom""
                 Style=""--bit-ToggleButton-icon-size: 1.5rem; --bit-ToggleButton-gap: 1rem;"" />


<div style=""--bit-ToggleButton-radius: 0; --bit-ToggleButton-min-height: 2.5rem; --bit-ToggleButton-font-weight: 600;"">
    <BitToggleButton Variant=""BitVariant.Outline"" ShowCheckMark FixedCheckMark Text=""Bold"" />
    <BitToggleButton Variant=""BitVariant.Outline"" ShowCheckMark FixedCheckMark Text=""Italic"" />
    <BitToggleButton Variant=""BitVariant.Outline"" ShowCheckMark FixedCheckMark Text=""Underline"" />
</div>";

    private readonly string example18RazorCode = @"
<div dir=""rtl"">
    <BitToggleButton Dir=""BitDir.Rtl"" Variant=""BitVariant.Fill""
                     OnText=""صدا قطع"" OnIconName=""@BitIconName.MicOff""
                     OffText=""صدا وصل"" OffIconName=""@BitIconName.Microphone"" />

    <BitToggleButton Dir=""BitDir.Rtl"" Variant=""BitVariant.Outline""
                     OnText=""صدا قطع"" OnIconName=""@BitIconName.MicOff""
                     OffText=""صدا وصل"" OffIconName=""@BitIconName.Microphone"" />

    <BitToggleButton Dir=""BitDir.Rtl"" Variant=""BitVariant.Text""
                     OnText=""صدا قطع"" OnIconName=""@BitIconName.MicOff""
                     OffText=""صدا وصل"" OffIconName=""@BitIconName.Microphone"" />
</div>";
}
