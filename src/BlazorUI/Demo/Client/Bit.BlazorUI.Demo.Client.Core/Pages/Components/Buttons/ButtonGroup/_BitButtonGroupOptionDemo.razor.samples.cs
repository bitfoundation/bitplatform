namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Buttons.ButtonGroup;

public partial class _BitButtonGroupOptionDemo
{
    private readonly string example1RazorCode = @"
<BitButtonGroup TItem=""BitButtonGroupOption"">
    <BitButtonGroupOption Text=""Add"" />
    <BitButtonGroupOption Text=""Edit"" />
    <BitButtonGroupOption Text=""Delete"" />
</BitButtonGroup>";

    private readonly string example2RazorCode = @"
<BitButtonGroup Variant=""BitVariant.Fill"" TItem=""BitButtonGroupOption"">
    <BitButtonGroupOption Text=""Add"" /><BitButtonGroupOption Text=""Edit"" /><BitButtonGroupOption Text=""Delete"" />
</BitButtonGroup>
<BitButtonGroup Variant=""BitVariant.Fill"" TItem=""BitButtonGroupOption"">
    <BitButtonGroupOption Text=""Add"" /><BitButtonGroupOption Text=""Edit"" IsEnabled=""false"" /><BitButtonGroupOption Text=""Delete"" />
</BitButtonGroup>
<BitButtonGroup Variant=""BitVariant.Fill"" IsEnabled=""false"" TItem=""BitButtonGroupOption"">
    <BitButtonGroupOption Text=""Add"" /><BitButtonGroupOption Text=""Edit"" IsEnabled=""false"" /><BitButtonGroupOption Text=""Delete"" />
</BitButtonGroup>

<BitButtonGroup Variant=""BitVariant.Outline"" TItem=""BitButtonGroupOption"">
    <BitButtonGroupOption Text=""Add"" /><BitButtonGroupOption Text=""Edit"" /><BitButtonGroupOption Text=""Delete"" />
</BitButtonGroup>
<BitButtonGroup Variant=""BitVariant.Outline"" TItem=""BitButtonGroupOption"">
    <BitButtonGroupOption Text=""Add"" /><BitButtonGroupOption Text=""Edit"" IsEnabled=""false"" /><BitButtonGroupOption Text=""Delete"" />
</BitButtonGroup>
<BitButtonGroup Variant=""BitVariant.Outline"" IsEnabled=""false"" TItem=""BitButtonGroupOption"">
    <BitButtonGroupOption Text=""Add"" /><BitButtonGroupOption Text=""Edit"" IsEnabled=""false"" /><BitButtonGroupOption Text=""Delete"" />
</BitButtonGroup>

<BitButtonGroup Variant=""BitVariant.Text"" TItem=""BitButtonGroupOption"">
    <BitButtonGroupOption Text=""Add"" /><BitButtonGroupOption Text=""Edit"" /><BitButtonGroupOption Text=""Delete"" />
</BitButtonGroup>
<BitButtonGroup Variant=""BitVariant.Text"" TItem=""BitButtonGroupOption"">
    <BitButtonGroupOption Text=""Add"" /><BitButtonGroupOption Text=""Edit"" IsEnabled=""false"" /><BitButtonGroupOption Text=""Delete"" />
</BitButtonGroup>
<BitButtonGroup Variant=""BitVariant.Text"" IsEnabled=""false"" TItem=""BitButtonGroupOption"">
    <BitButtonGroupOption Text=""Add"" /><BitButtonGroupOption Text=""Edit"" IsEnabled=""false"" /><BitButtonGroupOption Text=""Delete"" />
</BitButtonGroup>";

    private readonly string example3RazorCode = @"
<BitButtonGroup Variant=""BitVariant.Fill"" TItem=""BitButtonGroupOption"">
    <BitButtonGroupOption Text=""Add"" IconName=""@BitIconName.Add"" />
    <BitButtonGroupOption Text=""Edit"" IconName=""@BitIconName.Edit"" />
    <BitButtonGroupOption Text=""Delete"" IconName=""@BitIconName.Delete"" />
</BitButtonGroup>

<BitButtonGroup Variant=""BitVariant.Outline"" TItem=""BitButtonGroupOption"">
    <BitButtonGroupOption Text=""Add"" IconName=""@BitIconName.Add"" />
    <BitButtonGroupOption Text=""Edit"" IconName=""@BitIconName.Edit"" />
    <BitButtonGroupOption Text=""Delete"" IconName=""@BitIconName.Delete"" />
</BitButtonGroup>

<BitButtonGroup Variant=""BitVariant.Text"" TItem=""BitButtonGroupOption"">
    <BitButtonGroupOption Text=""Add"" IconName=""@BitIconName.Add"" />
    <BitButtonGroupOption Text=""Edit"" IconName=""@BitIconName.Edit"" />
    <BitButtonGroupOption Text=""Delete"" IconName=""@BitIconName.Delete"" />
</BitButtonGroup>";

    private readonly string example4RazorCode = @"
<BitButtonGroup Variant=""BitVariant.Fill"" TItem=""BitButtonGroupOption"" IconOnly>
    <BitButtonGroupOption Text=""Add"" IconName=""@BitIconName.Add"" />
    <BitButtonGroupOption Text=""Edit"" IconName=""@BitIconName.Edit"" />
    <BitButtonGroupOption Text=""Delete"" IconName=""@BitIconName.Delete"" />
</BitButtonGroup>

<BitButtonGroup Variant=""BitVariant.Outline"" TItem=""BitButtonGroupOption"" IconOnly>
    <BitButtonGroupOption Text=""Add"" IconName=""@BitIconName.Add"" />
    <BitButtonGroupOption Text=""Edit"" IconName=""@BitIconName.Edit"" />
    <BitButtonGroupOption Text=""Delete"" IconName=""@BitIconName.Delete"" />
</BitButtonGroup>

<BitButtonGroup Variant=""BitVariant.Text"" TItem=""BitButtonGroupOption"" IconOnly>
    <BitButtonGroupOption Text=""Add"" IconName=""@BitIconName.Add"" />
    <BitButtonGroupOption Text=""Edit"" IconName=""@BitIconName.Edit"" />
    <BitButtonGroupOption Text=""Delete"" IconName=""@BitIconName.Delete"" />
</BitButtonGroup>



<BitButtonGroup Variant=""BitVariant.Fill"" TItem=""BitButtonGroupOption"">
    <BitButtonGroupOption Text=""Add"" IconName=""@BitIconName.Add"" />
    <BitButtonGroupOption IconName=""@BitIconName.Edit"" />
    <BitButtonGroupOption Text=""Delete"" IconName=""@BitIconName.Delete"" />
</BitButtonGroup>

<BitButtonGroup Variant=""BitVariant.Outline"" TItem=""BitButtonGroupOption"">
    <BitButtonGroupOption Text=""Add"" IconName=""@BitIconName.Add"" />
    <BitButtonGroupOption IconName=""@BitIconName.Edit"" />
    <BitButtonGroupOption Text=""Delete"" IconName=""@BitIconName.Delete"" />
</BitButtonGroup>

<BitButtonGroup Variant=""BitVariant.Text"" TItem=""BitButtonGroupOption"">
    <BitButtonGroupOption Text=""Add"" IconName=""@BitIconName.Add"" />
    <BitButtonGroupOption IconName=""@BitIconName.Edit"" />
    <BitButtonGroupOption Text=""Delete"" IconName=""@BitIconName.Delete"" />
</BitButtonGroup>";

    private readonly string example5RazorCode = @"
<BitButtonGroup Variant=""BitVariant.Fill"" TItem=""BitButtonGroupOption"">
    <BitButtonGroupOption Text=""Add"" IconName=""@BitIconName.Add"" ReversedIcon />
    <BitButtonGroupOption Text=""Edit"" IconName=""@BitIconName.Edit"" ReversedIcon />
    <BitButtonGroupOption Text=""Delete"" IconName=""@BitIconName.Delete"" ReversedIcon />
</BitButtonGroup>

<BitButtonGroup Variant=""BitVariant.Outline"" TItem=""BitButtonGroupOption"">
    <BitButtonGroupOption Text=""Add"" IconName=""@BitIconName.Add"" ReversedIcon />
    <BitButtonGroupOption Text=""Edit"" IconName=""@BitIconName.Edit"" ReversedIcon />
    <BitButtonGroupOption Text=""Delete"" IconName=""@BitIconName.Delete"" ReversedIcon />
</BitButtonGroup>

<BitButtonGroup Variant=""BitVariant.Text"" TItem=""BitButtonGroupOption"">
    <BitButtonGroupOption Text=""Add"" IconName=""@BitIconName.Add"" ReversedIcon />
    <BitButtonGroupOption Text=""Edit"" IconName=""@BitIconName.Edit"" ReversedIcon />
    <BitButtonGroupOption Text=""Delete"" IconName=""@BitIconName.Delete"" ReversedIcon />
</BitButtonGroup>";

    private readonly string example6RazorCode = @"
<BitButtonGroup Toggle Variant=""BitVariant.Fill"" TItem=""BitButtonGroupOption"">
    <BitButtonGroupOption OnText=""Back (2X)"" OffText=""Back (1X)"" OnIconName=""@BitIconName.RewindTwoX"" OffIconName=""@BitIconName.Rewind"" />
    <BitButtonGroupOption OnText=""Resume"" OffText=""Play"" OnIconName=""@BitIconName.PlayResume"" OffIconName=""@BitIconName.Play"" />
    <BitButtonGroupOption OnText=""Forward (2X)"" OffText=""Forward (1X)"" OnIconName=""@BitIconName.FastForwardTwoX"" OffIconName=""@BitIconName.FastForward"" ReversedIcon />
</BitButtonGroup>

<BitButtonGroup Toggle Variant=""BitVariant.Outline"" TItem=""BitButtonGroupOption"">
    <BitButtonGroupOption OnText=""Back (2X)"" OffText=""Back (1X)"" OnIconName=""@BitIconName.RewindTwoX"" OffIconName=""@BitIconName.Rewind"" />
    <BitButtonGroupOption OnText=""Resume"" OffText=""Play"" OnIconName=""@BitIconName.PlayResume"" OffIconName=""@BitIconName.Play"" />
    <BitButtonGroupOption OnText=""Forward (2X)"" OffText=""Forward (1X)"" OnIconName=""@BitIconName.FastForwardTwoX"" OffIconName=""@BitIconName.FastForward"" ReversedIcon />
</BitButtonGroup>

<BitButtonGroup Toggle Variant=""BitVariant.Text"" TItem=""BitButtonGroupOption"">
    <BitButtonGroupOption OnText=""Back (2X)"" OffText=""Back (1X)"" OnIconName=""@BitIconName.RewindTwoX"" OffIconName=""@BitIconName.Rewind"" />
    <BitButtonGroupOption OnText=""Resume"" OffText=""Play"" OnIconName=""@BitIconName.PlayResume"" OffIconName=""@BitIconName.Play"" />
    <BitButtonGroupOption OnText=""Forward (2X)"" OffText=""Forward (1X)"" OnIconName=""@BitIconName.FastForwardTwoX"" OffIconName=""@BitIconName.FastForward"" ReversedIcon />
</BitButtonGroup>


<BitButtonGroup Toggle Variant=""BitVariant.Outline"" TItem=""BitButtonGroupOption"" @bind-ToggleKey=""toggleKey"">
    <BitButtonGroupOption Key=""back"" OnText=""Back (2X)"" OffText=""Back (1X)"" OnIconName=""@BitIconName.RewindTwoX"" OffIconName=""@BitIconName.Rewind"" />
    <BitButtonGroupOption Key=""play"" OnText=""Resume"" OffText=""Play"" OnIconName=""@BitIconName.PlayResume"" OffIconName=""@BitIconName.Play"" />
    <BitButtonGroupOption Key=""forward"" OnText=""Forward (2X)"" OffText=""Forward (1X)"" OnIconName=""@BitIconName.FastForwardTwoX"" OffIconName=""@BitIconName.FastForward"" ReversedIcon />
</BitButtonGroup>
<div>Toggle key: @toggleKey</div>
<BitButton OnClick=""@(() => toggleKey = ""forward"")"">Forward</BitButton>

<BitButtonGroup Toggle Variant=""BitVariant.Outline"" TItem=""BitButtonGroupOption"" DefaultToggleKey=""forward"" OnToggleChange=""(BitButtonGroupOption o) => onChangeToggleOption = o"">
    <BitButtonGroupOption Key=""back"" OnText=""Back (2X)"" OffText=""Back (1X)"" OnIconName=""@BitIconName.RewindTwoX"" OffIconName=""@BitIconName.Rewind"" />
    <BitButtonGroupOption Key=""play"" OnText=""Resume"" OffText=""Play"" OnIconName=""@BitIconName.PlayResume"" OffIconName=""@BitIconName.Play"" />
    <BitButtonGroupOption Key=""forward"" OnText=""Forward (2X)"" OffText=""Forward (1X)"" OnIconName=""@BitIconName.FastForwardTwoX"" OffIconName=""@BitIconName.FastForward"" ReversedIcon />
</BitButtonGroup>
<div>Changed toggle: @onChangeToggleOption?.Key , @onChangeToggleOption?.IsToggled</div>

<BitButtonGroup Toggle FixedToggle Variant=""BitVariant.Outline"" TItem=""BitButtonGroupOption"" DefaultToggleKey=""medium"">
    <BitButtonGroupOption Key=""low"" Text=""Low"" />
    <BitButtonGroupOption Key=""medium"" Text=""Medium"" />
    <BitButtonGroupOption Key=""high"" Text=""High"" />
</BitButtonGroup>";
    private readonly string example6CsharpCode = @"
private string? toggleKey = ""play"";
private BitButtonGroupOption? onChangeToggleOption;";

    private readonly string example7RazorCode = @"
<BitButtonGroup Variant=""BitVariant.Fill"" Vertical TItem=""BitButtonGroupOption"">
    <BitButtonGroupOption Text=""Add"" /><BitButtonGroupOption Text=""Edit"" /><BitButtonGroupOption Text=""Delete"" />
</BitButtonGroup>

<BitButtonGroup Variant=""BitVariant.Outline"" Vertical TItem=""BitButtonGroupOption"">
    <BitButtonGroupOption Text=""Add"" /><BitButtonGroupOption Text=""Edit"" /><BitButtonGroupOption Text=""Delete"" />
</BitButtonGroup>

<BitButtonGroup Variant=""BitVariant.Text"" Vertical TItem=""BitButtonGroupOption"">
    <BitButtonGroupOption Text=""Add"" /><BitButtonGroupOption Text=""Edit"" /><BitButtonGroupOption Text=""Delete"" />
</BitButtonGroup>";

    private readonly string example8RazorCode = @"
<BitButtonGroup OnItemClick=""item => clickedOption = item.Text"" TItem=""BitButtonGroupOption"">
    <BitButtonGroupOption Text=""Add"" />
    <BitButtonGroupOption Text=""Edit"" />
    <BitButtonGroupOption Text=""Delete"" />
</BitButtonGroup>
<div>Clicked item: <b>@clickedOption</b></div>

<BitButtonGroup TItem=""BitButtonGroupOption"">
    <BitButtonGroupOption Text=""Increase"" IconName=""@BitIconName.Add"" OnClick=""_ => { clickCounter++; StateHasChanged(); }"" />
    <BitButtonGroupOption Text=""Reset"" IconName=""@BitIconName.Reset"" OnClick=""_ => { clickCounter=0; StateHasChanged(); }"" />
    <BitButtonGroupOption Text=""Decrease"" IconName=""@BitIconName.Remove"" OnClick=""_ => { clickCounter--; StateHasChanged(); }"" />
</BitButtonGroup>
<div>Click count: <b>@clickCounter</b></div>";
    private readonly string example8CsharpCode = @"
private int clickCounter;
private string? clickedOption;";

    private readonly string example9RazorCode = @"
<BitButtonGroup FullWidth Variant=""BitVariant.Fill"" TItem=""BitButtonGroupOption"">
    <BitButtonGroupOption Text=""Add"" /><BitButtonGroupOption Text=""Edit"" /><BitButtonGroupOption Text=""Delete"" />
</BitButtonGroup>
<BitButtonGroup FullWidth Variant=""BitVariant.Outline"" TItem=""BitButtonGroupOption"">
    <BitButtonGroupOption Text=""Add"" /><BitButtonGroupOption Text=""Edit"" /><BitButtonGroupOption Text=""Delete"" />
</BitButtonGroup>
<BitButtonGroup FullWidth Variant=""BitVariant.Text"" TItem=""BitButtonGroupOption"">
    <BitButtonGroupOption Text=""Add"" /><BitButtonGroupOption Text=""Edit"" /><BitButtonGroupOption Text=""Delete"" />
</BitButtonGroup>";

    private readonly string example10RazorCode = @"
<BitButtonGroup Variant=""BitVariant.Outline""
                TItem=""BitButtonGroupOption""
                SelectionMode=""BitButtonGroupSelectionMode.Multiple""
                @bind-ToggleKeys=""formatKeys"">
    <BitButtonGroupOption Key=""bold"" Text=""Bold"" IconName=""@BitIconName.Bold"" />
    <BitButtonGroupOption Key=""italic"" Text=""Italic"" IconName=""@BitIconName.Italic"" />
    <BitButtonGroupOption Key=""underline"" Text=""Underline"" IconName=""@BitIconName.Underline"" />
</BitButtonGroup>
<div>Toggle keys: <b>@string.Join("", "", formatKeys ?? [])</b></div>

<BitButtonGroup Variant=""BitVariant.Outline""
                TItem=""BitButtonGroupOption""
                SelectionMode=""BitButtonGroupSelectionMode.Multiple""
                MaxToggles=""2""
                DefaultToggleKeys=""@defaultKeys"">
    <BitButtonGroupOption Key=""bold"" Text=""Bold"" IconName=""@BitIconName.Bold"" />
    <BitButtonGroupOption Key=""italic"" Text=""Italic"" IconName=""@BitIconName.Italic"" />
    <BitButtonGroupOption Key=""underline"" Text=""Underline"" IconName=""@BitIconName.Underline"" />
</BitButtonGroup>

<BitButtonGroup Variant=""BitVariant.Outline""
                TItem=""BitButtonGroupOption""
                SelectionMode=""BitButtonGroupSelectionMode.Multiple""
                FixedToggle
                DefaultToggleKeys=""@defaultKeys"">
    <BitButtonGroupOption Key=""bold"" Text=""Bold"" IconName=""@BitIconName.Bold"" />
    <BitButtonGroupOption Key=""italic"" Text=""Italic"" IconName=""@BitIconName.Italic"" />
    <BitButtonGroupOption Key=""underline"" Text=""Underline"" IconName=""@BitIconName.Underline"" />
</BitButtonGroup>";
    private readonly string example10CsharpCode = @"
private readonly string[] defaultKeys = [""bold""];
private IEnumerable<string>? formatKeys = [""bold""];";

    private readonly string example11RazorCode = @"
<BitButtonGroup FullWidth Variant=""BitVariant.Outline"" TItem=""BitButtonGroupOption"">
    <BitButtonGroupOption Text=""Day"" /><BitButtonGroupOption Text=""Week"" /><BitButtonGroupOption Text=""A whole month"" />
</BitButtonGroup>

<BitButtonGroup FullWidth Justified Variant=""BitVariant.Outline"" TItem=""BitButtonGroupOption"">
    <BitButtonGroupOption Text=""Day"" /><BitButtonGroupOption Text=""Week"" /><BitButtonGroupOption Text=""A whole month"" />
</BitButtonGroup>";

    private readonly string example12RazorCode = @"
<BitButtonGroup Detached Variant=""BitVariant.Fill"" TItem=""BitButtonGroupOption"">
    <BitButtonGroupOption Text=""Add"" /><BitButtonGroupOption Text=""Edit"" /><BitButtonGroupOption Text=""Delete"" />
</BitButtonGroup>

<BitButtonGroup Detached Variant=""BitVariant.Outline"" TItem=""BitButtonGroupOption"">
    <BitButtonGroupOption Text=""Add"" /><BitButtonGroupOption Text=""Edit"" /><BitButtonGroupOption Text=""Delete"" />
</BitButtonGroup>

<BitButtonGroup Detached Gap=""1.5rem"" Variant=""BitVariant.Outline"" TItem=""BitButtonGroupOption"">
    <BitButtonGroupOption Text=""Add"" /><BitButtonGroupOption Text=""Edit"" /><BitButtonGroupOption Text=""Delete"" />
</BitButtonGroup>";

    private readonly string example13RazorCode = @"
<BitButtonGroup Rounded Variant=""BitVariant.Fill"" TItem=""BitButtonGroupOption"">
    <BitButtonGroupOption Text=""Add"" /><BitButtonGroupOption Text=""Edit"" /><BitButtonGroupOption Text=""Delete"" />
</BitButtonGroup>
<BitButtonGroup Rounded Variant=""BitVariant.Outline"" TItem=""BitButtonGroupOption"">
    <BitButtonGroupOption Text=""Add"" /><BitButtonGroupOption Text=""Edit"" /><BitButtonGroupOption Text=""Delete"" />
</BitButtonGroup>

<BitButtonGroup Rounded Detached Variant=""BitVariant.Fill"" TItem=""BitButtonGroupOption"">
    <BitButtonGroupOption Text=""Add"" /><BitButtonGroupOption Text=""Edit"" /><BitButtonGroupOption Text=""Delete"" />
</BitButtonGroup>
<BitButtonGroup Rounded Detached Variant=""BitVariant.Outline"" TItem=""BitButtonGroupOption"">
    <BitButtonGroupOption Text=""Add"" /><BitButtonGroupOption Text=""Edit"" /><BitButtonGroupOption Text=""Delete"" />
</BitButtonGroup>";

    private readonly string example14RazorCode = @"
<BitButtonGroup FullWidth Variant=""BitVariant.Outline"" TItem=""BitButtonGroupOption"" Overflow=""BitButtonGroupOverflow.Wrap"">
    <BitButtonGroupOption Text=""January"" /><BitButtonGroupOption Text=""February"" /><BitButtonGroupOption Text=""March"" />
    <BitButtonGroupOption Text=""April"" /><BitButtonGroupOption Text=""May"" /><BitButtonGroupOption Text=""June"" />
    <BitButtonGroupOption Text=""July"" /><BitButtonGroupOption Text=""August"" /><BitButtonGroupOption Text=""September"" />
</BitButtonGroup>

<BitButtonGroup FullWidth Variant=""BitVariant.Outline"" TItem=""BitButtonGroupOption"" Overflow=""BitButtonGroupOverflow.Scroll"">
    <BitButtonGroupOption Text=""January"" /><BitButtonGroupOption Text=""February"" /><BitButtonGroupOption Text=""March"" />
    <BitButtonGroupOption Text=""April"" /><BitButtonGroupOption Text=""May"" /><BitButtonGroupOption Text=""June"" />
    <BitButtonGroupOption Text=""July"" /><BitButtonGroupOption Text=""August"" /><BitButtonGroupOption Text=""September"" />
</BitButtonGroup>

<BitButtonGroup FullWidth Variant=""BitVariant.Outline"" TItem=""BitButtonGroupOption"" Overflow=""BitButtonGroupOverflow.Scrollbar"">
    <BitButtonGroupOption Text=""January"" /><BitButtonGroupOption Text=""February"" /><BitButtonGroupOption Text=""March"" />
    <BitButtonGroupOption Text=""April"" /><BitButtonGroupOption Text=""May"" /><BitButtonGroupOption Text=""June"" />
    <BitButtonGroupOption Text=""July"" /><BitButtonGroupOption Text=""August"" /><BitButtonGroupOption Text=""September"" />
</BitButtonGroup>";

    private readonly string example15RazorCode = @"
<BitButtonGroup ShowSelectionIndicator
                Variant=""BitVariant.Outline""
                TItem=""BitButtonGroupOption""
                SelectionMode=""BitButtonGroupSelectionMode.Single""
                DefaultToggleKey=""list"">
    <BitButtonGroupOption Key=""list"" Text=""List"" IconName=""@BitIconName.BulletedList"" />
    <BitButtonGroupOption Key=""grid"" Text=""Grid"" IconName=""@BitIconName.GridViewMedium"" />
    <BitButtonGroupOption Key=""tile"" Text=""Tile"" IconName=""@BitIconName.Tiles"" />
</BitButtonGroup>

<BitButtonGroup ShowSelectionIndicator
                Variant=""BitVariant.Outline""
                TItem=""BitButtonGroupOption""
                SelectionMode=""BitButtonGroupSelectionMode.Multiple""
                DefaultToggleKeys=""@indicatorDefaultKeys"">
    <BitButtonGroupOption Key=""name"" Text=""Name"" />
    <BitButtonGroupOption Key=""size"" Text=""Size"" />
    <BitButtonGroupOption Key=""date"" Text=""Date"" />
</BitButtonGroup>";

    private readonly string example16RazorCode = @"
<BitButtonGroup Variant=""BitVariant.Outline"" TItem=""BitButtonGroupOption"">
    <BitButtonGroupOption Text=""Save"" IconName=""@BitIconName.Save""
                          IsLoading=""@(loadingKey == ""save"")"" OnClick=""@(() => HandleLoadingClick(""save""))"" />
    <BitButtonGroupOption Text=""Sync"" IconName=""@BitIconName.Sync""
                          IsLoading=""@(loadingKey == ""sync"")"" OnClick=""@(() => HandleLoadingClick(""sync""))"" />
    <BitButtonGroupOption Text=""Publish"" IconName=""@BitIconName.PublishContent""
                          IsLoading=""@(loadingKey == ""publish"")"" OnClick=""@(() => HandleLoadingClick(""publish""))"" />
</BitButtonGroup>

<BitButtonGroup Variant=""BitVariant.Fill"" TItem=""BitButtonGroupOption"">
    <BitButtonGroupOption Text=""Inbox"" IconName=""@BitIconName.Inbox"" Badge=""12"" />
    <BitButtonGroupOption Text=""Drafts"" IconName=""@BitIconName.Edit"" Badge=""3"" />
    <BitButtonGroupOption Text=""Sent"" IconName=""@BitIconName.Send"" />
</BitButtonGroup>
<BitButtonGroup Variant=""BitVariant.Outline"" TItem=""BitButtonGroupOption"">
    <BitButtonGroupOption Text=""Inbox"" IconName=""@BitIconName.Inbox"" Badge=""12"" />
    <BitButtonGroupOption Text=""Drafts"" IconName=""@BitIconName.Edit"" Badge=""3"" />
    <BitButtonGroupOption Text=""Sent"" IconName=""@BitIconName.Send"" />
</BitButtonGroup>";
    private readonly string example16CsharpCode = @"
private string? loadingKey;

private async Task HandleLoadingClick(string key)
{
    loadingKey = key;
    StateHasChanged();

    await Task.Delay(2000);

    loadingKey = null;
    StateHasChanged();
}";

    private readonly string example17RazorCode = @"
<BitButtonGroup Variant=""BitVariant.Outline"" TItem=""BitButtonGroupOption"">
    <BitButtonGroupOption Text=""Home"" IconName=""@BitIconName.Home"" Href=""/"" />
    <BitButtonGroupOption Text=""Components"" IconName=""@BitIconName.Puzzle"" Href=""/components"" />
    <BitButtonGroupOption Text=""GitHub"" IconName=""@BitIconName.OpenInNewWindow"" Href=""https://github.com/bitfoundation/bitplatform"" Target=""_blank"" />
</BitButtonGroup>";

    private readonly string example18RazorCode = @"
<style>
    .custom-template {
        gap: 0.25rem;
        display: flex;
        align-items: center;
        flex-flow: column nowrap;
    }
</style>


<BitButtonGroup Variant=""BitVariant.Outline"" TItem=""BitButtonGroupOption"">
    <ItemTemplate Context=""option"">
        <div class=""custom-template"">
            <BitIcon IconName=""@option.IconName"" />
            <span>@option.Text</span>
        </div>
    </ItemTemplate>
    <Options>
        <BitButtonGroupOption Text=""Add"" IconName=""@BitIconName.Add"" />
        <BitButtonGroupOption Text=""Edit"" IconName=""@BitIconName.Edit"" />
        <BitButtonGroupOption Text=""Delete"" IconName=""@BitIconName.Delete"" />
    </Options>
</BitButtonGroup>

<BitButtonGroup Variant=""BitVariant.Outline"" TItem=""BitButtonGroupOption"">
    <BitButtonGroupOption Text=""Add"" IconName=""@BitIconName.Add"" />
    <BitButtonGroupOption Text=""Edit"">
        <Template Context=""option"">
            <div class=""custom-template"">
                <BitIcon IconName=""@BitIconName.Edit"" Color=""BitColor.Warning"" />
                <b>@option.Text</b>
            </div>
        </Template>
    </BitButtonGroupOption>
    <BitButtonGroupOption Text=""Delete"" IconName=""@BitIconName.Delete"" />
</BitButtonGroup>";

    private readonly string example19RazorCode = @"
<BitButtonGroup IconOnly Variant=""BitVariant.Outline"" TItem=""BitButtonGroupOption"">
    <BitButtonGroupOption Text=""Add"" IconName=""@BitIconName.Add"" Title=""Add a new record"" AriaLabel=""Add"" />
    <BitButtonGroupOption Text=""Edit"" IconName=""@BitIconName.Edit"" Title=""Edit the selected record"" AriaLabel=""Edit"" />
    <BitButtonGroupOption Text=""Delete"" IconName=""@BitIconName.Delete"" Title=""Delete the selected record"" AriaLabel=""Delete"" />
</BitButtonGroup>

<BitButtonGroup Toggle Variant=""BitVariant.Outline"" TItem=""BitButtonGroupOption"" DefaultToggleKey=""mute"">
    <BitButtonGroupOption Key=""mute""
                          AriaLabel=""Mute""
                          OnText=""Muted""
                          OffText=""Mute""
                          OnTitle=""The sound is muted, click to unmute""
                          OffTitle=""Click to mute the sound""
                          OnIconName=""@BitIconName.Volume0""
                          OffIconName=""@BitIconName.Volume3"" />
    <BitButtonGroupOption Key=""repeat""
                          AriaLabel=""Repeat""
                          OnText=""Repeating""
                          OffText=""Repeat""
                          OnTitle=""Repeat is on, click to turn it off""
                          OffTitle=""Click to repeat the playlist""
                          OnIconName=""@BitIconName.RepeatOne""
                          OffIconName=""@BitIconName.RepeatAll"" />
</BitButtonGroup>";

    private readonly string example20RazorCode = @"
<BitButtonGroup AriaLabel=""Text alignment""
                Variant=""BitVariant.Outline""
                TItem=""BitButtonGroupOption""
                SelectionMode=""BitButtonGroupSelectionMode.Single""
                DefaultToggleKey=""start"">
    <BitButtonGroupOption Key=""start"" Text=""Start"" IconName=""@BitIconName.AlignLeft"" AriaLabel=""Align start"" />
    <BitButtonGroupOption Key=""center"" Text=""Center"" IconName=""@BitIconName.AlignCenter"" AriaLabel=""Align center"" />
    <BitButtonGroupOption Key=""end"" Text=""End"" IconName=""@BitIconName.AlignRight"" AriaLabel=""Align end"" />
</BitButtonGroup>

<BitButtonGroup AriaLabel=""Text alignment (selection follows focus)""
                SelectOnFocus
                Variant=""BitVariant.Outline""
                TItem=""BitButtonGroupOption""
                SelectionMode=""BitButtonGroupSelectionMode.Single""
                DefaultToggleKey=""start"">
    <BitButtonGroupOption Key=""start"" Text=""Start"" IconName=""@BitIconName.AlignLeft"" AriaLabel=""Align start"" />
    <BitButtonGroupOption Key=""center"" Text=""Center"" IconName=""@BitIconName.AlignCenter"" AriaLabel=""Align center"" />
    <BitButtonGroupOption Key=""end"" Text=""End"" IconName=""@BitIconName.AlignRight"" AriaLabel=""Align end"" />
</BitButtonGroup>

<BitButtonGroup AriaLabel=""Operations with a disabled button""
                DisabledInteractive
                Variant=""BitVariant.Outline""
                TItem=""BitButtonGroupOption"">
    <BitButtonGroupOption Text=""Add"" /><BitButtonGroupOption Text=""Edit"" IsEnabled=""false"" /><BitButtonGroupOption Text=""Delete"" />
</BitButtonGroup>

<BitButtonGroup AriaLabel=""Operations""
                Navigable=""false""
                Variant=""BitVariant.Outline""
                TItem=""BitButtonGroupOption"">
    <BitButtonGroupOption Text=""Add"" /><BitButtonGroupOption Text=""Edit"" /><BitButtonGroupOption Text=""Delete"" />
</BitButtonGroup>";

    private readonly string example21RazorCode = @"
<BitButtonGroup Color=""BitColor.Primary"" Variant=""BitVariant.Fill"" TItem=""BitButtonGroupOption"">
    <BitButtonGroupOption Text=""Add"" /><BitButtonGroupOption Text=""Edit"" /><BitButtonGroupOption Text=""Delete"" />
</BitButtonGroup>
<BitButtonGroup Color=""BitColor.Primary"" Variant=""BitVariant.Outline"" TItem=""BitButtonGroupOption"">
    <BitButtonGroupOption Text=""Add"" /><BitButtonGroupOption Text=""Edit"" /><BitButtonGroupOption Text=""Delete"" />
</BitButtonGroup>
<BitButtonGroup Color=""BitColor.Primary"" Variant=""BitVariant.Text"" TItem=""BitButtonGroupOption"">
    <BitButtonGroupOption Text=""Add"" /><BitButtonGroupOption Text=""Edit"" /><BitButtonGroupOption Text=""Delete"" />
</BitButtonGroup>

<BitButtonGroup Color=""BitColor.Secondary"" Variant=""BitVariant.Fill"" TItem=""BitButtonGroupOption"">
    <BitButtonGroupOption Text=""Add"" /><BitButtonGroupOption Text=""Edit"" /><BitButtonGroupOption Text=""Delete"" />
</BitButtonGroup>
<BitButtonGroup Color=""BitColor.Secondary"" Variant=""BitVariant.Outline"" TItem=""BitButtonGroupOption"">
    <BitButtonGroupOption Text=""Add"" /><BitButtonGroupOption Text=""Edit"" /><BitButtonGroupOption Text=""Delete"" />
</BitButtonGroup>
<BitButtonGroup Color=""BitColor.Secondary"" Variant=""BitVariant.Text"" TItem=""BitButtonGroupOption"">
    <BitButtonGroupOption Text=""Add"" /><BitButtonGroupOption Text=""Edit"" /><BitButtonGroupOption Text=""Delete"" />
</BitButtonGroup>

<BitButtonGroup Color=""BitColor.Tertiary"" Variant=""BitVariant.Fill"" TItem=""BitButtonGroupOption"">
    <BitButtonGroupOption Text=""Add"" /><BitButtonGroupOption Text=""Edit"" /><BitButtonGroupOption Text=""Delete"" />
</BitButtonGroup>
<BitButtonGroup Color=""BitColor.Tertiary"" Variant=""BitVariant.Outline"" TItem=""BitButtonGroupOption"">
    <BitButtonGroupOption Text=""Add"" /><BitButtonGroupOption Text=""Edit"" /><BitButtonGroupOption Text=""Delete"" />
</BitButtonGroup>
<BitButtonGroup Color=""BitColor.Tertiary"" Variant=""BitVariant.Text"" TItem=""BitButtonGroupOption"">
    <BitButtonGroupOption Text=""Add"" /><BitButtonGroupOption Text=""Edit"" /><BitButtonGroupOption Text=""Delete"" />
</BitButtonGroup>

<BitButtonGroup Color=""BitColor.Info"" Variant=""BitVariant.Fill"" TItem=""BitButtonGroupOption"">
    <BitButtonGroupOption Text=""Add"" /><BitButtonGroupOption Text=""Edit"" /><BitButtonGroupOption Text=""Delete"" />
</BitButtonGroup>
<BitButtonGroup Color=""BitColor.Info"" Variant=""BitVariant.Outline"" TItem=""BitButtonGroupOption"">
    <BitButtonGroupOption Text=""Add"" /><BitButtonGroupOption Text=""Edit"" /><BitButtonGroupOption Text=""Delete"" />
</BitButtonGroup>
<BitButtonGroup Color=""BitColor.Info"" Variant=""BitVariant.Text"" TItem=""BitButtonGroupOption"">
    <BitButtonGroupOption Text=""Add"" /><BitButtonGroupOption Text=""Edit"" /><BitButtonGroupOption Text=""Delete"" />
</BitButtonGroup>

<BitButtonGroup Color=""BitColor.Success"" Variant=""BitVariant.Fill"" TItem=""BitButtonGroupOption"">
    <BitButtonGroupOption Text=""Add"" /><BitButtonGroupOption Text=""Edit"" /><BitButtonGroupOption Text=""Delete"" />
</BitButtonGroup>
<BitButtonGroup Color=""BitColor.Success"" Variant=""BitVariant.Outline"" TItem=""BitButtonGroupOption"">
    <BitButtonGroupOption Text=""Add"" /><BitButtonGroupOption Text=""Edit"" /><BitButtonGroupOption Text=""Delete"" />
</BitButtonGroup>
<BitButtonGroup Color=""BitColor.Success"" Variant=""BitVariant.Text"" TItem=""BitButtonGroupOption"">
    <BitButtonGroupOption Text=""Add"" /><BitButtonGroupOption Text=""Edit"" /><BitButtonGroupOption Text=""Delete"" />
</BitButtonGroup>

<BitButtonGroup Color=""BitColor.Warning"" Variant=""BitVariant.Fill"" TItem=""BitButtonGroupOption"">
    <BitButtonGroupOption Text=""Add"" /><BitButtonGroupOption Text=""Edit"" /><BitButtonGroupOption Text=""Delete"" />
</BitButtonGroup>
<BitButtonGroup Color=""BitColor.Warning"" Variant=""BitVariant.Outline"" TItem=""BitButtonGroupOption"">
    <BitButtonGroupOption Text=""Add"" /><BitButtonGroupOption Text=""Edit"" /><BitButtonGroupOption Text=""Delete"" />
</BitButtonGroup>
<BitButtonGroup Color=""BitColor.Warning"" Variant=""BitVariant.Text"" TItem=""BitButtonGroupOption"">
    <BitButtonGroupOption Text=""Add"" /><BitButtonGroupOption Text=""Edit"" /><BitButtonGroupOption Text=""Delete"" />
</BitButtonGroup>

<BitButtonGroup Color=""BitColor.SevereWarning"" Variant=""BitVariant.Fill"" TItem=""BitButtonGroupOption"">
    <BitButtonGroupOption Text=""Add"" /><BitButtonGroupOption Text=""Edit"" /><BitButtonGroupOption Text=""Delete"" />
</BitButtonGroup>
<BitButtonGroup Color=""BitColor.SevereWarning"" Variant=""BitVariant.Outline"" TItem=""BitButtonGroupOption"">
    <BitButtonGroupOption Text=""Add"" /><BitButtonGroupOption Text=""Edit"" /><BitButtonGroupOption Text=""Delete"" />
</BitButtonGroup>
<BitButtonGroup Color=""BitColor.SevereWarning"" Variant=""BitVariant.Text"" TItem=""BitButtonGroupOption"">
    <BitButtonGroupOption Text=""Add"" /><BitButtonGroupOption Text=""Edit"" /><BitButtonGroupOption Text=""Delete"" />
</BitButtonGroup>

<BitButtonGroup Color=""BitColor.Error"" Variant=""BitVariant.Fill"" TItem=""BitButtonGroupOption"">
    <BitButtonGroupOption Text=""Add"" /><BitButtonGroupOption Text=""Edit"" /><BitButtonGroupOption Text=""Delete"" />
</BitButtonGroup>
<BitButtonGroup Color=""BitColor.Error"" Variant=""BitVariant.Outline"" TItem=""BitButtonGroupOption"">
    <BitButtonGroupOption Text=""Add"" /><BitButtonGroupOption Text=""Edit"" /><BitButtonGroupOption Text=""Delete"" />
</BitButtonGroup>
<BitButtonGroup Color=""BitColor.Error"" Variant=""BitVariant.Text"" TItem=""BitButtonGroupOption"">
    <BitButtonGroupOption Text=""Add"" /><BitButtonGroupOption Text=""Edit"" /><BitButtonGroupOption Text=""Delete"" />
</BitButtonGroup>


<BitButtonGroup Color=""BitColor.PrimaryBackground"" Variant=""BitVariant.Fill"" TItem=""BitButtonGroupOption"">
    <BitButtonGroupOption Text=""Add"" /><BitButtonGroupOption Text=""Edit"" /><BitButtonGroupOption Text=""Delete"" />
</BitButtonGroup>
<BitButtonGroup Color=""BitColor.PrimaryBackground"" Variant=""BitVariant.Outline"" TItem=""BitButtonGroupOption"">
    <BitButtonGroupOption Text=""Add"" /><BitButtonGroupOption Text=""Edit"" /><BitButtonGroupOption Text=""Delete"" />
</BitButtonGroup>
<BitButtonGroup Color=""BitColor.PrimaryBackground"" Variant=""BitVariant.Text"" TItem=""BitButtonGroupOption"">
    <BitButtonGroupOption Text=""Add"" /><BitButtonGroupOption Text=""Edit"" /><BitButtonGroupOption Text=""Delete"" />
</BitButtonGroup>

<BitButtonGroup Color=""BitColor.SecondaryBackground"" Variant=""BitVariant.Fill"" TItem=""BitButtonGroupOption"">
    <BitButtonGroupOption Text=""Add"" /><BitButtonGroupOption Text=""Edit"" /><BitButtonGroupOption Text=""Delete"" />
</BitButtonGroup>
<BitButtonGroup Color=""BitColor.SecondaryBackground"" Variant=""BitVariant.Outline"" TItem=""BitButtonGroupOption"">
    <BitButtonGroupOption Text=""Add"" /><BitButtonGroupOption Text=""Edit"" /><BitButtonGroupOption Text=""Delete"" />
</BitButtonGroup>
<BitButtonGroup Color=""BitColor.SecondaryBackground"" Variant=""BitVariant.Text"" TItem=""BitButtonGroupOption"">
    <BitButtonGroupOption Text=""Add"" /><BitButtonGroupOption Text=""Edit"" /><BitButtonGroupOption Text=""Delete"" />
</BitButtonGroup>

<BitButtonGroup Color=""BitColor.TertiaryBackground"" Variant=""BitVariant.Fill"" TItem=""BitButtonGroupOption"">
    <BitButtonGroupOption Text=""Add"" /><BitButtonGroupOption Text=""Edit"" /><BitButtonGroupOption Text=""Delete"" />
</BitButtonGroup>
<BitButtonGroup Color=""BitColor.TertiaryBackground"" Variant=""BitVariant.Outline"" TItem=""BitButtonGroupOption"">
    <BitButtonGroupOption Text=""Add"" /><BitButtonGroupOption Text=""Edit"" /><BitButtonGroupOption Text=""Delete"" />
</BitButtonGroup>
<BitButtonGroup Color=""BitColor.TertiaryBackground"" Variant=""BitVariant.Text"" TItem=""BitButtonGroupOption"">
    <BitButtonGroupOption Text=""Add"" /><BitButtonGroupOption Text=""Edit"" /><BitButtonGroupOption Text=""Delete"" />
</BitButtonGroup>


<BitButtonGroup Color=""BitColor.PrimaryForeground"" Variant=""BitVariant.Fill"" TItem=""BitButtonGroupOption"">
    <BitButtonGroupOption Text=""Add"" /><BitButtonGroupOption Text=""Edit"" /><BitButtonGroupOption Text=""Delete"" />
</BitButtonGroup>
<BitButtonGroup Color=""BitColor.PrimaryForeground"" Variant=""BitVariant.Outline"" TItem=""BitButtonGroupOption"">
    <BitButtonGroupOption Text=""Add"" /><BitButtonGroupOption Text=""Edit"" /><BitButtonGroupOption Text=""Delete"" />
</BitButtonGroup>
<BitButtonGroup Color=""BitColor.PrimaryForeground"" Variant=""BitVariant.Text"" TItem=""BitButtonGroupOption"">
    <BitButtonGroupOption Text=""Add"" /><BitButtonGroupOption Text=""Edit"" /><BitButtonGroupOption Text=""Delete"" />
</BitButtonGroup>

<BitButtonGroup Color=""BitColor.SecondaryForeground"" Variant=""BitVariant.Fill"" TItem=""BitButtonGroupOption"">
    <BitButtonGroupOption Text=""Add"" /><BitButtonGroupOption Text=""Edit"" /><BitButtonGroupOption Text=""Delete"" />
</BitButtonGroup>
<BitButtonGroup Color=""BitColor.SecondaryForeground"" Variant=""BitVariant.Outline"" TItem=""BitButtonGroupOption"">
    <BitButtonGroupOption Text=""Add"" /><BitButtonGroupOption Text=""Edit"" /><BitButtonGroupOption Text=""Delete"" />
</BitButtonGroup>
<BitButtonGroup Color=""BitColor.SecondaryForeground"" Variant=""BitVariant.Text"" TItem=""BitButtonGroupOption"">
    <BitButtonGroupOption Text=""Add"" /><BitButtonGroupOption Text=""Edit"" /><BitButtonGroupOption Text=""Delete"" />
</BitButtonGroup>

<BitButtonGroup Color=""BitColor.TertiaryForeground"" Variant=""BitVariant.Fill"" TItem=""BitButtonGroupOption"">
    <BitButtonGroupOption Text=""Add"" /><BitButtonGroupOption Text=""Edit"" /><BitButtonGroupOption Text=""Delete"" />
</BitButtonGroup>
<BitButtonGroup Color=""BitColor.TertiaryForeground"" Variant=""BitVariant.Outline"" TItem=""BitButtonGroupOption"">
    <BitButtonGroupOption Text=""Add"" /><BitButtonGroupOption Text=""Edit"" /><BitButtonGroupOption Text=""Delete"" />
</BitButtonGroup>
<BitButtonGroup Color=""BitColor.TertiaryForeground"" Variant=""BitVariant.Text"" TItem=""BitButtonGroupOption"">
    <BitButtonGroupOption Text=""Add"" /><BitButtonGroupOption Text=""Edit"" /><BitButtonGroupOption Text=""Delete"" />
</BitButtonGroup>


<BitButtonGroup Color=""BitColor.PrimaryBorder"" Variant=""BitVariant.Fill"" TItem=""BitButtonGroupOption"">
    <BitButtonGroupOption Text=""Add"" /><BitButtonGroupOption Text=""Edit"" /><BitButtonGroupOption Text=""Delete"" />
</BitButtonGroup>
<BitButtonGroup Color=""BitColor.PrimaryBorder"" Variant=""BitVariant.Outline"" TItem=""BitButtonGroupOption"">
    <BitButtonGroupOption Text=""Add"" /><BitButtonGroupOption Text=""Edit"" /><BitButtonGroupOption Text=""Delete"" />
</BitButtonGroup>
<BitButtonGroup Color=""BitColor.PrimaryBorder"" Variant=""BitVariant.Text"" TItem=""BitButtonGroupOption"">
    <BitButtonGroupOption Text=""Add"" /><BitButtonGroupOption Text=""Edit"" /><BitButtonGroupOption Text=""Delete"" />
</BitButtonGroup>

<BitButtonGroup Color=""BitColor.SecondaryBorder"" Variant=""BitVariant.Fill"" TItem=""BitButtonGroupOption"">
    <BitButtonGroupOption Text=""Add"" /><BitButtonGroupOption Text=""Edit"" /><BitButtonGroupOption Text=""Delete"" />
</BitButtonGroup>
<BitButtonGroup Color=""BitColor.SecondaryBorder"" Variant=""BitVariant.Outline"" TItem=""BitButtonGroupOption"">
    <BitButtonGroupOption Text=""Add"" /><BitButtonGroupOption Text=""Edit"" /><BitButtonGroupOption Text=""Delete"" />
</BitButtonGroup>
<BitButtonGroup Color=""BitColor.SecondaryBorder"" Variant=""BitVariant.Text"" TItem=""BitButtonGroupOption"">
    <BitButtonGroupOption Text=""Add"" /><BitButtonGroupOption Text=""Edit"" /><BitButtonGroupOption Text=""Delete"" />
</BitButtonGroup>

<BitButtonGroup Color=""BitColor.TertiaryBorder"" Variant=""BitVariant.Fill"" TItem=""BitButtonGroupOption"">
    <BitButtonGroupOption Text=""Add"" /><BitButtonGroupOption Text=""Edit"" /><BitButtonGroupOption Text=""Delete"" />
</BitButtonGroup>
<BitButtonGroup Color=""BitColor.TertiaryBorder"" Variant=""BitVariant.Outline"" TItem=""BitButtonGroupOption"">
    <BitButtonGroupOption Text=""Add"" /><BitButtonGroupOption Text=""Edit"" /><BitButtonGroupOption Text=""Delete"" />
</BitButtonGroup>
<BitButtonGroup Color=""BitColor.TertiaryBorder"" Variant=""BitVariant.Text"" TItem=""BitButtonGroupOption"">
    <BitButtonGroupOption Text=""Add"" /><BitButtonGroupOption Text=""Edit"" /><BitButtonGroupOption Text=""Delete"" />
</BitButtonGroup>";

    private readonly string example22RazorCode = @"
<link rel=""stylesheet"" href=""https://cdnjs.cloudflare.com/ajax/libs/font-awesome/7.0.1/css/all.min.css"" />
    
    <BitButtonGroup Variant=""BitVariant.Fill"" TItem=""BitButtonGroupOption"">
        <BitButtonGroupOption Text=""Add"" Icon=""@(""fa-solid fa-plus"")"" />
        <BitButtonGroupOption Text=""Edit"" Icon=""@(""fa-solid fa-pen"")"" />
        <BitButtonGroupOption Text=""Delete"" Icon=""@(""fa-solid fa-trash"")"" />
    </BitButtonGroup>
    
    <br />
    <br />
    
    <BitButtonGroup Variant=""BitVariant.Outline"" Color=""BitColor.Secondary"" TItem=""BitButtonGroupOption"">
        <BitButtonGroupOption Text=""Add"" Icon=""@BitIconInfo.Css(""fa-solid fa-plus"")"" />
        <BitButtonGroupOption Text=""Edit"" Icon=""@BitIconInfo.Css(""fa-solid fa-pen"")"" />
        <BitButtonGroupOption Text=""Delete"" Icon=""@BitIconInfo.Css(""fa-solid fa-trash"")"" />
    </BitButtonGroup>
    
    <br />
    <br />

    <BitButtonGroup Variant=""BitVariant.Text"" Color=""BitColor.Tertiary"" TItem=""BitButtonGroupOption"">
        <BitButtonGroupOption Text=""Add"" Icon=""@BitIconInfo.Fa(""solid plus"")"" />
        <BitButtonGroupOption Text=""Edit"" Icon=""@BitIconInfo.Fa(""solid pen"")"" />
        <BitButtonGroupOption Text=""Delete"" Icon=""@BitIconInfo.Fa(""solid trash"")"" />
    </BitButtonGroup>";

    private readonly string example23RazorCode = @"
<BitButtonGroup Size=""BitSize.Small"" Variant=""BitVariant.Fill"" TItem=""BitButtonGroupOption"">
    <BitButtonGroupOption Text=""Add"" /><BitButtonGroupOption Text=""Edit"" /><BitButtonGroupOption Text=""Delete"" />
</BitButtonGroup>
<BitButtonGroup Size=""BitSize.Small"" Variant=""BitVariant.Outline"" TItem=""BitButtonGroupOption"">
    <BitButtonGroupOption Text=""Add"" /><BitButtonGroupOption Text=""Edit"" /><BitButtonGroupOption Text=""Delete"" />
</BitButtonGroup>
<BitButtonGroup Size=""BitSize.Small"" Variant=""BitVariant.Text"" TItem=""BitButtonGroupOption"">
    <BitButtonGroupOption Text=""Add"" /><BitButtonGroupOption Text=""Edit"" /><BitButtonGroupOption Text=""Delete"" />
</BitButtonGroup>

<BitButtonGroup Size=""BitSize.Medium"" Variant=""BitVariant.Fill"" TItem=""BitButtonGroupOption"">
    <BitButtonGroupOption Text=""Add"" /><BitButtonGroupOption Text=""Edit"" /><BitButtonGroupOption Text=""Delete"" />
</BitButtonGroup>
<BitButtonGroup Size=""BitSize.Medium"" Variant=""BitVariant.Outline"" TItem=""BitButtonGroupOption"">
    <BitButtonGroupOption Text=""Add"" /><BitButtonGroupOption Text=""Edit"" /><BitButtonGroupOption Text=""Delete"" />
</BitButtonGroup>
<BitButtonGroup Size=""BitSize.Medium"" Variant=""BitVariant.Text"" TItem=""BitButtonGroupOption"">
    <BitButtonGroupOption Text=""Add"" /><BitButtonGroupOption Text=""Edit"" /><BitButtonGroupOption Text=""Delete"" />
</BitButtonGroup>

<BitButtonGroup Size=""BitSize.Large"" Variant=""BitVariant.Fill"" TItem=""BitButtonGroupOption"">
    <BitButtonGroupOption Text=""Add"" /><BitButtonGroupOption Text=""Edit"" /><BitButtonGroupOption Text=""Delete"" />
</BitButtonGroup>
<BitButtonGroup Size=""BitSize.Large"" Variant=""BitVariant.Outline"" TItem=""BitButtonGroupOption"">
    <BitButtonGroupOption Text=""Add"" /><BitButtonGroupOption Text=""Edit"" /><BitButtonGroupOption Text=""Delete"" />
</BitButtonGroup>
<BitButtonGroup Size=""BitSize.Large"" Variant=""BitVariant.Text"" TItem=""BitButtonGroupOption"">
    <BitButtonGroupOption Text=""Add"" /><BitButtonGroupOption Text=""Edit"" /><BitButtonGroupOption Text=""Delete"" />
</BitButtonGroup>";

    private readonly string example24RazorCode = @"
<style>
    .custom-class {
        margin-inline: 1rem;
        border-radius: 1rem;
        border-color: tomato;
        border-width: 0.25rem;
    }

    .custom-class button {
        color: tomato;
        border-color: tomato;
    }

    .custom-class button:hover {
        color: unset;
        background-color: lightcoral;
    }

    .custom-item {
        color: peachpuff;
        background-color: tomato;
    }

    .custom-btn {
        color: aliceblue;
        border-color: aliceblue;
        background-color: crimson;
    }
</style>


<BitButtonGroup Style=""border-radius: 1rem; margin: 1rem; box-shadow: tomato 0 0 1rem;"" TItem=""BitButtonGroupOption"">
    <BitButtonGroupOption Text=""Add"" /><BitButtonGroupOption Text=""Edit"" /><BitButtonGroupOption Text=""Delete"" />
</BitButtonGroup>

<BitButtonGroup Class=""custom-class"" Variant=""BitVariant.Outline"" TItem=""BitButtonGroupOption"">
    <BitButtonGroupOption Text=""Add"" /><BitButtonGroupOption Text=""Edit"" /><BitButtonGroupOption Text=""Delete"" />
</BitButtonGroup>


<BitButtonGroup Variant=""BitVariant.Text"" TItem=""BitButtonGroupOption"">
    <BitButtonGroupOption Text=""Styled"" IconName=""@BitIconName.Brush"" Style=""color: tomato; border-color: brown; background-color: peachpuff;"" />
    <BitButtonGroupOption Text=""Classed"" IconName=""@BitIconName.FormatPainter"" Class=""custom-item"" />
</BitButtonGroup>

<BitButtonGroup Variant=""BitVariant.Text""
                TItem=""BitButtonGroupOption""
                Styles=""@(new() { Button = ""color: darkcyan; border-color: deepskyblue; background-color: azure;"" })"">
    <BitButtonGroupOption Text=""Add"" /><BitButtonGroupOption Text=""Edit"" /><BitButtonGroupOption Text=""Delete"" />
</BitButtonGroup>

<BitButtonGroup Variant=""BitVariant.Text""
                TItem=""BitButtonGroupOption""
                Classes=""@(new() { Button = ""custom-btn"" })"">
    <BitButtonGroupOption Text=""Add"" /><BitButtonGroupOption Text=""Edit"" /><BitButtonGroupOption Text=""Delete"" />
</BitButtonGroup>";

    private readonly string example25RazorCode = @"
<BitButtonGroup Dir=""BitDir.Rtl"" Variant=""BitVariant.Fill"" TItem=""BitButtonGroupOption"">
    <BitButtonGroupOption Text=""اضافه کردن"" IconName=""@BitIconName.Add"" />
    <BitButtonGroupOption Text=""ویرایش"" IconName=""@BitIconName.Edit"" />
    <BitButtonGroupOption Text=""حذف"" IconName=""@BitIconName.Delete"" />
</BitButtonGroup>

<BitButtonGroup Dir=""BitDir.Rtl"" Variant=""BitVariant.Outline"" TItem=""BitButtonGroupOption"">
    <BitButtonGroupOption Text=""اضافه کردن"" IconName=""@BitIconName.Add"" />
    <BitButtonGroupOption Text=""ویرایش"" IconName=""@BitIconName.Edit"" />
    <BitButtonGroupOption Text=""حذف"" IconName=""@BitIconName.Delete"" />
</BitButtonGroup>

<BitButtonGroup Dir=""BitDir.Rtl"" Variant=""BitVariant.Text"" TItem=""BitButtonGroupOption"">
    <BitButtonGroupOption Text=""اضافه کردن"" IconName=""@BitIconName.Add"" />
    <BitButtonGroupOption Text=""ویرایش"" IconName=""@BitIconName.Edit"" />
    <BitButtonGroupOption Text=""حذف"" IconName=""@BitIconName.Delete"" />
</BitButtonGroup>";
}
