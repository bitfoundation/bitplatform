namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.AI.VoiceInput;

public partial class BitAiVoiceInputDemo
{
    private readonly string example1RazorCode = @"
<BitAiVoiceInput OnTranscript=""HandleBasicTranscript"" />

<div>Heard: <b>@basicTranscript</b></div>";
    private readonly string example1CsharpCode = @"
private string basicTranscript = ""nothing yet"";

private void HandleBasicTranscript(string transcript)
{
    basicTranscript = transcript;
}";

    private readonly string example2RazorCode = @"
<BitAiVoiceInput OnTranscript=""HandleListeningTranscript""
                 OnInterim=""HandleInterim""
                 OnListeningChanged=""HandleListeningChanged"" />

<div>Listening: <b>@isListening</b></div>
<div>Provisional: <i>@interim</i></div>
<div>Final: <b>@listeningTranscript</b></div>";
    private readonly string example2CsharpCode = @"
private bool isListening;
private string? interim;
private string listeningTranscript = string.Empty;

private void HandleListeningTranscript(string transcript)
{
    listeningTranscript = $""{listeningTranscript} {transcript}"".Trim();
}

private void HandleInterim(string text)
{
    interim = text;
}

private void HandleListeningChanged(bool listening)
{
    isListening = listening;
}";

    private readonly string example3RazorCode = @"
<div style=""display:flex;align-items:flex-start;gap:0.5rem"">
    <BitTextField @bind-Value=""note""
                  Multiline
                  Rows=""3""
                  Placeholder=""Type or dictate a note""
                  Description=""@fieldInterim""
                  Style=""flex:1"" />
    <BitAiVoiceInput OnTranscript=""AppendToNote"" OnInterim=""v => fieldInterim = v"" />
</div>";
    private readonly string example3CsharpCode = @"
private string? note;
private string? fieldInterim;

private void AppendToNote(string transcript)
{
    note = string.IsNullOrEmpty(note) ? transcript : $""{note} {transcript}"";
    fieldInterim = null;
}";

    private readonly string example4RazorCode = @"
<BitChoiceGroup @bind-Value=""language"" Items=""languages"" Horizontal />

<BitAiVoiceInput Language=""@language"" OnTranscript=""HandleLanguageTranscript"" />

<div>Heard (@language): <b>@languageTranscript</b></div>";
    private readonly string example4CsharpCode = @"
private string? language = ""en-US"";
private readonly List<BitChoiceGroupItem<string>> languages =
[
    new() { Text = ""English (US)"", Value = ""en-US"" },
    new() { Text = ""Français"", Value = ""fr-FR"" },
    new() { Text = ""Español"", Value = ""es-ES"" },
    new() { Text = ""Deutsch"", Value = ""de-DE"" },
];
private string languageTranscript = ""nothing yet"";

private void HandleLanguageTranscript(string transcript)
{
    languageTranscript = transcript;
}";

    private readonly string example5RazorCode = @"
<BitAiVoiceInput OnTranscript=""HandleErrorTranscript"" OnError=""HandleError"" />

<div>Heard: <b>@errorTranscript</b></div>
<div>Last error: <b>@lastError</b></div>";
    private readonly string example5CsharpCode = @"
private string errorTranscript = ""nothing yet"";
private string lastError = ""none"";

private void HandleErrorTranscript(string transcript)
{
    errorTranscript = transcript;
}

private void HandleError(string error)
{
    lastError = error;
}";

    private readonly string example6RazorCode = @"
<BitParams Parameters=""voiceInputParams"">
    <div>British English: <BitAiVoiceInput OnTranscript=""HandleCascadeTranscript"" /></div>

    <div>Its own Language, German: <BitAiVoiceInput Language=""de-DE"" OnTranscript=""HandleCascadeTranscript"" /></div>
</BitParams>

<div>Heard: <b>@cascadeTranscript</b></div>";
    private readonly string example6CsharpCode = @"
private string cascadeTranscript = ""nothing yet"";
private readonly BitAiVoiceInputParams[] voiceInputParams = [new() { Language = ""en-GB"" }];

private void HandleCascadeTranscript(string transcript)
{
    cascadeTranscript = transcript;
}";

    private readonly string example7RazorCode = @"
<BitToggle @bind-Value=""isDisabled"" Text=""Disabled"" />
<BitAiVoiceInput Disabled=""isDisabled"" OnTranscript=""HandleDisabledTranscript"" />

<div>Heard: <b>@disabledTranscript</b></div>";
    private readonly string example7CsharpCode = @"
private bool isDisabled;
private string disabledTranscript = ""nothing yet"";

private void HandleDisabledTranscript(string transcript)
{
    disabledTranscript = transcript;
}";

    private readonly string example8RazorCode = @"
<style>
    .custom-class {
        padding: 0.25rem;
        border-radius: 0.5rem;
        background-color: var(--bit-clr-bg-sec);
    }

    .custom-button {
        border: 2px solid var(--bit-clr-pri);
    }

    .custom-checked {
        border-color: var(--bit-clr-err);
        box-shadow: 0 0 0 0.25rem var(--bit-clr-err-light);
    }
</style>

<BitAiVoiceInput Style=""padding:0.25rem;border:1px dashed var(--bit-clr-brd-pri);border-radius:50%""
                 OnTranscript=""HandleStyleTranscript"" />

<BitAiVoiceInput Class=""custom-class"" OnTranscript=""HandleStyleTranscript"" />

<BitAiVoiceInput Styles=""@(new() { Button = new() { Root = ""border-radius:50%"", Checked = ""background-color:var(--bit-clr-err);color:var(--bit-clr-err-text)"" } })""
                 OnTranscript=""HandleStyleTranscript"" />

<BitAiVoiceInput Classes=""@(new() { Button = new() { Root = ""custom-button"", Checked = ""custom-checked"" } })""
                 OnTranscript=""HandleStyleTranscript"" />

<span style=""@cssVariablesStyle"">
    <BitAiVoiceInput OnTranscript=""HandleStyleTranscript"" />
</span>

<div>Heard: <b>@styleTranscript</b></div>";
    private readonly string example8CsharpCode = @"
private const string cssVariablesStyle = ""--bit-ToggleButton-text-color:var(--bit-clr-pri);"" +
                                         ""--bit-ToggleButton-radius:50%;"" +
                                         ""--bit-ToggleButton-checked-background:var(--bit-clr-err);"" +
                                         ""--bit-ToggleButton-checked-color:var(--bit-clr-err-text);"";

private string styleTranscript = ""nothing yet"";

private void HandleStyleTranscript(string transcript)
{
    styleTranscript = transcript;
}";

    private readonly string example9RazorCode = @"
<div dir=""rtl"" style=""display:flex;align-items:center;gap:0.5rem"">
    <BitAiVoiceInput Dir=""BitDir.Rtl"" Language=""fa-IR"" OnTranscript=""HandleRtlTranscript"" />
    <span>شنیده شد: <b>@rtlTranscript</b></span>
</div>";
    private readonly string example9CsharpCode = @"
private string rtlTranscript = ""هنوز چیزی شنیده نشده"";

private void HandleRtlTranscript(string transcript)
{
    rtlTranscript = transcript;
}";
}
