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
            Description = "The recognition language, as a BCP 47 language tag such as en-US. When null, the language of the browser is used - or, with a transcriber, whatever the transcriber decides (an IBitAiSpeechToText is handed it). It is read when the dictation starts.",
        },
        new()
        {
            Name = "MaxRecordingDuration",
            Type = "TimeSpan",
            DefaultValue = "1 minute",
            Description = "The longest a recording for the transcriber may last; it is transcribed, not thrown away, when it reaches it. The time left is shown beside the button while it records, and announced once ten seconds before the end.",
        },
        new()
        {
            Name = "MaxRecordingSize",
            Type = "long",
            DefaultValue = "10 MB",
            Description = "The largest recording handed to the transcriber, in bytes; a larger one fails with too-large. It is streamed to .NET in chunks, on Blazor Server as on WebAssembly.",
        },
        new()
        {
            Name = "Mode",
            Type = "BitAiDictationMode",
            DefaultValue = "BitAiDictationMode.Auto",
            Description = "How speech is turned into text: through the app's transcriber, through the speech recognition of the browser, or through the one with the other as the fallback.",
            LinkType = LinkType.Link,
            Href = "#dictation-mode-enum",
        },
        new()
        {
            Name = "OnError",
            Type = "EventCallback<string>",
            DefaultValue = "",
            Description = "Raised when the transcription fails, with the name of the error the browser reported - not-allowed for a microphone the user refused, no-speech, audio-capture, network and the like - or, for a recording, not-allowed, audio-capture, recording-failed, too-large or transcription-failed (the transcriber threw). The dictation has stopped by then, and the live region has announced the failure.",
        },
        new()
        {
            Name = "OnInterim",
            Type = "EventCallback<string>",
            DefaultValue = "",
            Description = "Raised with the provisional text as the user speaks, which may still change, and with an empty one once that text is final or gone. Only the speech recognition of the browser has provisional text; a transcriber hears the whole recording at once.",
        },
        new()
        {
            Name = "OnListeningChanged",
            Type = "EventCallback<bool>",
            DefaultValue = "",
            Description = "Raised when the dictation starts (true) or stops (false), however that happens: the button, a Disabled set while listening, a silence the browser ends it on, an error, or another voice input of the page starting its own dictation (one runs at a time). A recording stops listening as it ends, before it is transcribed (IsTranscribing).",
        },
        new()
        {
            Name = "OnTranscript",
            Type = "EventCallback<string>",
            DefaultValue = "",
            Description = "Raised with each finalised phrase, trimmed, once the recognizer has committed to it - or, with a transcriber, with what it heard in the recording.",
        },
        new()
        {
            Name = "SilenceTimeout",
            Type = "TimeSpan?",
            DefaultValue = "null",
            Description = "Ends a recording for the transcriber once the user has said something and then been quiet for this long. When null, a recording ends when the user stops it, or at MaxRecordingDuration.",
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
        new()
        {
            Name = "Transcriber",
            Type = "Func<Stream, string, CancellationToken, Task<string?>>?",
            DefaultValue = "null",
            Description = "The app's speech-to-text, given a recording (read from its start, and disposed once the task completes), its media type (audio/webm in Chromium and Firefox, audio/mp4 in Safari) and a token cancelled when the voice input goes away; it returns what was said. With one, the microphone is recorded in the browser and the take transcribed by the app (see Mode). When null, an IBitAiSpeechToText registered through AddBitBlazorUIAIServices(ai => ai.UseSpeechToText<T>()) is used, if there is one.",
        },
    ];

    private readonly List<ComponentParameter> componentPublicMembers =
    [
        new()
        {
            Name = "IsListening",
            Type = "bool",
            DefaultValue = "false",
            Description = "Whether the dictation is running - what OnListeningChanged reported last. False while the browser cannot transcribe.",
        },
        new()
        {
            Name = "IsTranscribing",
            Type = "bool",
            DefaultValue = "false",
            Description = "Whether a recording is being transcribed by the app's transcriber - between the end of the recording and its transcript. The button waits, unavailable, meanwhile.",
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
                    Name = "Countdown",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the time left of a recording for the app's transcriber, shown beside the button while it records."
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



    private readonly List<ComponentSubEnum> componentSubEnums =
    [
        new()
        {
            Id = "dictation-mode-enum",
            Name = "BitAiDictationMode",
            Description = "How a BitAiVoiceInput turns speech into text.",
            Items =
            [
                new() { Name = "Auto", Description = "Through the app's transcriber when it supplies one (Transcriber, or an IBitAiSpeechToText service), recording the microphone in the browser; through the speech recognition of the browser otherwise.", Value = "0" },
                new() { Name = "Browser", Description = "Through the speech recognition of the browser where it has one, and through the app's transcriber, when it supplies one, only where it has none (Firefox, Android WebView).", Value = "1" },
                new() { Name = "Recording", Description = "Through the app's transcriber only, recording the microphone in the browser. With no transcriber the button is not shown.", Value = "2" },
            ]
        },
    ];

    private readonly List<ComponentCssVariable> componentCssVariables =
    [
        new() { Name = "--bit-AiVoiceInput-countdown-color", DefaultValue = "var(--bit-clr-fg-sec)", Description = "Color of the time left of a recording for the app's transcriber, shown beside the button while it records." },
    ];



    private const string cssVariablesStyle = "--bit-ToggleButton-text-color:var(--bit-clr-pri);" +
                                             "--bit-ToggleButton-radius:50%;" +
                                             "--bit-ToggleButton-checked-background:var(--bit-clr-err);" +
                                             "--bit-ToggleButton-checked-color:var(--bit-clr-err-text);";

    private string basicTranscript = "nothing yet";

    private BitAiVoiceInput listeningInput = default!;
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

    private BitAiDictationMode dictationMode;
    private readonly List<BitChoiceGroupItem<BitAiDictationMode>> dictationModes =
    [
        new() { Text = "Auto", Value = BitAiDictationMode.Auto },
        new() { Text = "Browser", Value = BitAiDictationMode.Browser },
        new() { Text = "Recording", Value = BitAiDictationMode.Recording },
    ];
    private bool isRecording;
    private string transcriberTranscript = "nothing yet";
    private string transcriberError = "none";

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

    // A stand-in for the app's speech-to-text, which would post the recording to its server (or to Whisper, Azure
    // Speech, ...) and return what it heard: this one only says what it was handed.
    private async Task<string?> DescribeRecordingAsync(Stream audio, string contentType, CancellationToken cancellationToken)
    {
        await Task.Delay(600, cancellationToken);

        return $"(a {audio.Length:N0}-byte {contentType} recording, which a real app sends to its speech-to-text)";
    }

    private void HandleTranscriberTranscript(string transcript)
    {
        transcriberTranscript = transcript;
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
