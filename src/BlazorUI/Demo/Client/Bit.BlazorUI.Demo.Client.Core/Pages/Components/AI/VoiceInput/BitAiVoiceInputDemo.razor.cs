namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.AI.VoiceInput;

public partial class BitAiVoiceInputDemo
{
    private readonly List<ComponentParameter> componentParameters =
    [
        new()
        {
            Name = "Classes",
            Type = "BitAiVoiceInputClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS classes for different parts of the voice input.",
            LinkType = LinkType.Link,
            Href = "#class-styles",
        },
        new()
        {
            Name = "Language",
            Type = "string?",
            DefaultValue = "null",
            Description = "The recognition language, as a BCP 47 language tag such as en-US. When null, the language of the browser is used. It is read when the dictation starts.",
        },
        new()
        {
            Name = "OnError",
            Type = "EventCallback<string>",
            DefaultValue = "",
            Description = "Raised when the transcription fails, with the name of the error the browser reported - not-allowed for a microphone the user refused, no-speech, audio-capture, network and the like. The dictation has stopped by then, and the live region has announced the failure.",
        },
        new()
        {
            Name = "OnInterim",
            Type = "EventCallback<string>",
            DefaultValue = "",
            Description = "Raised with the provisional text as the user speaks, which may still change, and with an empty one once that text is final or gone.",
        },
        new()
        {
            Name = "OnListeningChanged",
            Type = "EventCallback<bool>",
            DefaultValue = "",
            Description = "Raised when the dictation starts (true) or stops (false), however that happens: the button, a Disabled set while listening, a silence the browser ends it on, or an error.",
        },
        new()
        {
            Name = "OnTranscript",
            Type = "EventCallback<string>",
            DefaultValue = "",
            Description = "Raised with each finalised phrase, trimmed, once the recognizer has committed to it.",
        },
        new()
        {
            Name = "Styles",
            Type = "BitAiVoiceInputClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS styles for different parts of the voice input.",
            LinkType = LinkType.Link,
            Href = "#class-styles",
        },
    ];

    private readonly List<ComponentParameter> componentPublicMembers =
    [
        new()
        {
            Name = "IsListening",
            Type = "bool",
            DefaultValue = "false",
            Description = "Whether the dictation is running.",
        },
    ];

    private readonly List<ComponentSubClass> componentSubClasses =
    [
        new()
        {
            Id = "class-styles",
            Title = "BitAiVoiceInputClassStyles",
            Parameters =
            [
                new()
                {
                    Name = "Root",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the root element of the BitAiVoiceInput."
                },
                new()
                {
                    Name = "Button",
                    Type = "BitToggleButtonClassStyles?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the BitToggleButton of the BitAiVoiceInput that starts and stops the dictation (its Checked member is the state while the microphone is on).",
                    LinkType = LinkType.Link,
                    Href = "/components/togglebutton#class-styles"
                },
                new()
                {
                    Name = "LiveRegion",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the visually hidden live region of the BitAiVoiceInput that announces whether the microphone is on."
                },
            ]
        },
    ];



    private const string cssVariablesStyle = "--bit-ToggleButton-text-color:var(--bit-clr-pri);" +
                                             "--bit-ToggleButton-radius:50%;" +
                                             "--bit-ToggleButton-checked-background:var(--bit-clr-err);" +
                                             "--bit-ToggleButton-checked-color:var(--bit-clr-err-text);";

    private string basicTranscript = "nothing yet";

    private bool isListening;
    private string? interim;
    private string listeningTranscript = string.Empty;

    private string? note;
    private string? fieldInterim;

    private string? language = "en-US";
    private readonly List<BitChoiceGroupItem<string>> languages =
    [
        new() { Text = "English (US)", Value = "en-US" },
        new() { Text = "Français", Value = "fr-FR" },
        new() { Text = "Español", Value = "es-ES" },
        new() { Text = "Deutsch", Value = "de-DE" },
    ];
    private string languageTranscript = "nothing yet";

    private string errorTranscript = "nothing yet";
    private string lastError = "none";

    private string cascadeTranscript = "nothing yet";
    private readonly BitAiVoiceInputParams[] voiceInputParams = [new() { Language = "en-GB" }];

    private bool isDisabled;
    private string disabledTranscript = "nothing yet";

    private string styleTranscript = "nothing yet";

    private string rtlTranscript = "هنوز چیزی شنیده نشده";

    private void HandleBasicTranscript(string transcript)
    {
        basicTranscript = transcript;
    }

    private void HandleListeningTranscript(string transcript)
    {
        listeningTranscript = $"{listeningTranscript} {transcript}".Trim();
    }

    private void HandleInterim(string text)
    {
        interim = text;
    }

    private void HandleListeningChanged(bool listening)
    {
        isListening = listening;
    }

    private void AppendToNote(string transcript)
    {
        note = string.IsNullOrEmpty(note) ? transcript : $"{note} {transcript}";
        fieldInterim = null;
    }

    private void HandleLanguageTranscript(string transcript)
    {
        languageTranscript = transcript;
    }

    private void HandleErrorTranscript(string transcript)
    {
        errorTranscript = transcript;
    }

    private void HandleError(string error)
    {
        lastError = error;
    }

    private void HandleCascadeTranscript(string transcript)
    {
        cascadeTranscript = transcript;
    }

    private void HandleDisabledTranscript(string transcript)
    {
        disabledTranscript = transcript;
    }

    private void HandleStyleTranscript(string transcript)
    {
        styleTranscript = transcript;
    }

    private void HandleRtlTranscript(string transcript)
    {
        rtlTranscript = transcript;
    }
}
