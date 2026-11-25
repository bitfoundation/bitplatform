namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Extras.VirtualKeyboard;

public partial class BitVirtualKeyboardDemo
{
    private readonly string example1RazorCode = @"
<BitVirtualKeyboard @bind-Value=""basicValue"" />
<div>Value: @basicValue</div>";
    private readonly string example1CsharpCode = @"
private string? basicValue;";

    private readonly string example2RazorCode = @"
<input @ref=""targetInput"" @bind=""targetValue"" @bind:event=""oninput"" placeholder=""Type with the keys below"" />
<div>Value: @targetValue</div>
<BitVirtualKeyboard Target=""targetInput"" Form=""BitVirtualKeyboardForm.Full"" />";
    private readonly string example2CsharpCode = @"
private ElementReference targetInput;
private string? targetValue;

// The input is taken by @ref in the first render, so the keyboard is handed it in the next one.
protected override void OnAfterRender(bool firstRender)
{
    if (firstRender) StateHasChanged();
}";

    private readonly string example3RazorCode = @"
<input data-bit-vkb placeholder=""Text"" />
<input data-bit-vkb type=""tel"" placeholder=""Phone"" />
<input data-bit-vkb type=""email"" placeholder=""Email"" />
<input data-bit-vkb=""pin"" placeholder=""PIN"" />
<div>Open: @followOpen</div>
<BitVirtualKeyboard Mode=""BitVirtualKeyboardMode.Docked"" FollowFocus @bind-Open=""followOpen"" />";
    private readonly string example3CsharpCode = @"
private bool followOpen;";

    private readonly string example4RazorCode = @"
@foreach (var purpose in purposes)
{
    <BitButton Size=""BitSize.Small"" Variant=""@(purpose == currentPurpose ? BitVariant.Fill : BitVariant.Outline)"" OnClick=""() => currentPurpose = purpose"">@purpose</BitButton>
}
<BitVirtualKeyboard Purpose=""currentPurpose"" @bind-Value=""purposeValue"" />
<div>Value: @purposeValue</div>";
    private readonly string example4CsharpCode = @"
private static readonly BitVirtualKeyboardPurpose[] purposes =
[
    BitVirtualKeyboardPurpose.Tel, BitVirtualKeyboardPurpose.Pin, BitVirtualKeyboardPurpose.Decimal, BitVirtualKeyboardPurpose.Calculator,
    BitVirtualKeyboardPurpose.Pos, BitVirtualKeyboardPurpose.Hex, BitVirtualKeyboardPurpose.Date, BitVirtualKeyboardPurpose.Iban,
    BitVirtualKeyboardPurpose.Alphabetic, BitVirtualKeyboardPurpose.Atm, BitVirtualKeyboardPurpose.Email, BitVirtualKeyboardPurpose.Url,
];
private BitVirtualKeyboardPurpose currentPurpose = BitVirtualKeyboardPurpose.Tel;
private string? purposeValue;";

    private readonly string example5RazorCode = @"
<BitVirtualKeyboard Languages=""@([""en"", ""fa"", ""ar"", ""he"", ""de"", ""ru""])"" Form=""BitVirtualKeyboardForm.Compact"" @bind-Value=""languagesValue"" />
<div dir=""auto"">Value: @languagesValue</div>";
    private readonly string example5CsharpCode = @"
private string? languagesValue;";

    private readonly string example6RazorCode = @"
<BitVirtualKeyboard Language=""de"" Form=""BitVirtualKeyboardForm.Full"" />";

    private readonly string example7RazorCode = @"
<BitVirtualKeyboard Layout=""shorthandLayout"" @bind-Value=""shorthandValue"" />
<div>Value: @shorthandValue</div>";
    private readonly string example7CsharpCode = @"
private string? shorthandValue;
private readonly BitVirtualKeyboardLayout shorthandLayout = new()
{
    Id = ""my-pad"",
    Name = ""My pad"",
    Layers =
    [
        new() { Id = ""base"", Rows = [""a b c {bksp*1.5}"", ""d e f {layer:symbols*1.5}"", ""{space} {enter*1.5}""] },
        new() { Id = ""symbols"", Rows = [""1 2 3 {bksp*1.5}"", ""4 5 6 {abc*1.5}"", ""{space} {enter*1.5}""] },
    ],
};";

    private readonly string example8RazorCode = @"
<BitVirtualKeyboard Layout=""objectLayout"" @bind-Value=""objectValue"" OnCommand=""args => objectCommand = args.Command"" />
<div>Value: @objectValue</div>
<div>Command: @objectCommand</div>";
    private readonly string example8CsharpCode = @"
private string? objectValue;
private string? objectCommand;
private readonly BitVirtualKeyboardLayout objectLayout = new()
{
    Id = ""my-objects"",
    Name = ""Objects"",
    Keys = new()
    {
        [""e""] = new() { LongPress = [""é"", ""è"", ""ê"", ""ë""] },
        [""2""] = new() { Hint = ""ABC"", MultiTap = [""a"", ""b"", ""c""] },
        [""ok""] = new() { Action = BitVirtualKeyboardKeyAction.Enter, Height = 2, Label = ""OK"" },
        [""star""] = new() { Action = BitVirtualKeyboardKeyAction.Custom, Command = ""favorite"", Label = ""★"" },
    },
    DeadKeys = new() { [""acute""] = new() { [""a""] = ""á"", [""e""] = ""é"", ["" ""] = ""´"" } },
    Layers =
    [
        new() { Id = ""base"", Rows = [""e 2 {dead:acute} {ok}"", ""a {star} {bksp}""] },
    ],
};";

    private readonly string example9RazorCode = @"
<BitVirtualKeyboard Layout=""customizedLayout"" />";
    private readonly string example9CsharpCode = @"
private readonly BitVirtualKeyboardLayout customizedLayout = BitVirtualKeyboardLayouts.EnglishCompact with
{
    Id = ""my-english"",
    Displays = new(BitVirtualKeyboardLayouts.EnglishCompact.Displays) { [""sym""] = ""#+="" },
};";

    private readonly string example10RazorCode = @"
<BitVirtualKeyboard Layout=""jsonLayout"" @bind-Value=""jsonValue"" />
<div>Value: @jsonValue</div>";
    private readonly string example10CsharpCode = @"
private string? jsonValue;
private readonly BitVirtualKeyboardLayout jsonLayout = BitVirtualKeyboardLayout.FromJson(""""""
    {
      ""id"": ""json-pad"",
      ""name"": ""JSON pad"",
      ""layers"": [
        { ""id"": ""base"", ""rows"": [ ""7 8 9"", ""4 5 6"", ""1 2 3"", ""{bksp} 0 {enter}"" ] }
      ]
    }
    """""");";

    private readonly string example11RazorCode = @"
<BitVirtualKeyboard Form=""BitVirtualKeyboardForm.Compact""
                    @bind-Value=""eventsValue""
                    OnKeyPress=""HandleKeyPress""
                    OnLayerChange=""layer => eventsLayer = layer"" />
<div>Value: @eventsValue</div>
<div>Layer: @eventsLayer</div>";
    private readonly string example11CsharpCode = @"
private string? eventsValue;
private string? eventsLayer;

private static void HandleKeyPress(BitVirtualKeyboardKeyPressArgs args)
{
    if (args.Action is BitVirtualKeyboardKeyAction.Backspace) args.Cancel = true;
    if (args.Text is not null) args.Text = args.Text.ToUpperInvariant();
}";

    private readonly string example12RazorCode = @"
<BitVirtualKeyboard Purpose=""BitVirtualKeyboardPurpose.Pin"" Secure Scramble @bind-Value=""pinValue"" />
<div>Length: @(pinValue?.Length ?? 0)</div>";
    private readonly string example12CsharpCode = @"
private string? pinValue;";

    private readonly string example13RazorCode = @"
<BitVirtualKeyboard Purpose=""BitVirtualKeyboardPurpose.Numeric"">
    <KeyTemplate>
        <b>@context.Label</b>
    </KeyTemplate>
</BitVirtualKeyboard>";

    private readonly string example14RazorCode = @"
<BitVirtualKeyboard Purpose=""BitVirtualKeyboardPurpose.Numeric"" Disabled />";

    private readonly string example15RazorCode = @"
<BitVirtualKeyboard Purpose=""BitVirtualKeyboardPurpose.Numeric"" Size=""BitSize.Small"" />
<BitVirtualKeyboard Purpose=""BitVirtualKeyboardPurpose.Numeric"" Size=""BitSize.Large"" />";

    private readonly string example16RazorCode = @"
<BitVirtualKeyboard Purpose=""BitVirtualKeyboardPurpose.Numeric""
                    Style=""--bit-VirtualKeyboard-radius: 999px; --bit-VirtualKeyboard-background: transparent""
                    Styles=""@(new() { PrimaryKey = ""background: seagreen"" })"" />";

    private readonly string example17RazorCode = @"
<div dir=""rtl"">
    <BitVirtualKeyboard Dir=""BitDir.Rtl"" Language=""fa"" Form=""BitVirtualKeyboardForm.Compact"" />
</div>";
}
