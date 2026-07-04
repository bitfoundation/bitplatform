using Bit.Butil;

namespace Bit.Websites.Platform.Client.Pages.Butil;

public partial class Butil28SpeechPage
{
    private SpeechVoice[] voices = [];
    private string? speakText = "Hello from Bit.Butil speech synthesis!";
    private string? voiceName;
    private string? getVoicesResult;
    private string? speakResult;
    private string? isSpeaking;

    private string? transcript;
    private bool recognizing;
    private IAsyncDisposable? recognitionHandle;


    protected override async Task OnAfterFirstRenderAsync()
    {
        await base.OnAfterFirstRenderAsync();

        await GetVoices();
    }


    private async Task GetVoices()
    {
        if (await speechSynthesis.IsSupported() is false)
        {
            getVoicesResult = "Speech synthesis is not supported.";
            return;
        }

        voices = await speechSynthesis.GetVoices();
        getVoicesResult = $"{voices.Length} voice(s) available.";
        StateHasChanged();
    }

    private async Task Speak()
    {
        if (string.IsNullOrWhiteSpace(speakText)) return;

        if (await speechSynthesis.IsSupported() is false)
        {
            speakResult = "Speech synthesis is not supported.";
            return;
        }

        await speechSynthesis.Speak(new SpeechUtterance
        {
            Text = speakText,
            VoiceName = string.IsNullOrWhiteSpace(voiceName) ? null : voiceName,
            Rate = 1,
            Pitch = 1,
        });
    }

    private async Task PauseSpeech()
    {
        if (await speechSynthesis.IsSupported() is false)
        {
            speakResult = "Speech synthesis is not supported.";
            return;
        }

        await speechSynthesis.Pause();
        speakResult = $"Paused (speaking={await speechSynthesis.IsSpeaking()}).";
    }

    private async Task ResumeSpeech()
    {
        if (await speechSynthesis.IsSupported() is false)
        {
            speakResult = "Speech synthesis is not supported.";
            return;
        }

        await speechSynthesis.Resume();
        speakResult = "Resumed.";
    }

    private async Task CancelSpeech()
    {
        if (await speechSynthesis.IsSupported() is false)
        {
            speakResult = "Speech synthesis is not supported.";
            return;
        }

        await speechSynthesis.Cancel();
        speakResult = "Cancelled.";
    }

    private async Task IsSpeaking()
    {
        if (await speechSynthesis.IsSupported() is false)
        {
            isSpeaking = "Speech synthesis is not supported.";
            return;
        }

        isSpeaking = (await speechSynthesis.IsSpeaking()).ToString();
    }

    private async Task StartRecognition()
    {
        if (await speechRecognition.IsSupported() is false)
        {
            transcript = "Speech recognition is not supported.";
            return;
        }

        await StopRecognitionInternal();

        transcript = "";
        recognizing = true;

        recognitionHandle = await speechRecognition.Start(
            new SpeechRecognitionOptions { Continuous = true, InterimResults = true },
            onResult: result =>
            {
                transcript = result.IsFinal
                    ? $"Final: {result.Transcript} ({result.Confidence:P0})"
                    : $"Interim: {result.Transcript}";
                InvokeAsync(StateHasChanged);
            },
            onError: err =>
            {
                transcript = $"Error: {err}";
                InvokeAsync(StateHasChanged);
            },
            onEnd: () =>
            {
                recognizing = false;
                InvokeAsync(StateHasChanged);
            });
    }

    private async Task StopRecognition()
    {
        await StopRecognitionInternal();
    }

    private async Task StopRecognitionInternal()
    {
        if (recognitionHandle is null) return;
        await recognitionHandle.DisposeAsync();
        recognitionHandle = null;
        recognizing = false;
    }


    protected override async ValueTask DisposeAsync(bool disposing)
    {
        // Stop any active/pending speech so navigating away cancels ongoing utterances.
        await speechSynthesis.Cancel();

        if (recognitionHandle is not null)
            await recognitionHandle.DisposeAsync();

        await base.DisposeAsync(disposing);
    }


    private readonly string getVoicesExampleCode =
@"@inject Bit.Butil.SpeechSynthesis speechSynthesis

<BitButton OnClick=""GetVoices"">GetVoices</BitButton>

<div>Voices count: @voices.Length</div>

@code {
    private SpeechVoice[] voices = [];

    private async Task GetVoices()
    {
        if (await speechSynthesis.IsSupported() is false) return;
        voices = await speechSynthesis.GetVoices();
    }
}";

    private readonly string speakExampleCode =
@"@inject Bit.Butil.SpeechSynthesis speechSynthesis

<BitTextField @bind-Value=""speakText"" Label=""Text"" Multiline Rows=""2"" />

<select @bind=""voiceName"">
    <option value="""">Default voice</option>
    @foreach (var voice in voices)
    {
        <option value=""@voice.Name"">@voice.Name (@voice.Lang)</option>
    }
</select>

<BitButton OnClick=""Speak"">Speak</BitButton>

@code {
    private string? speakText = ""Hello from Bit.Butil speech synthesis!"";
    private string? voiceName;

    private async Task Speak()
    {
        await speechSynthesis.Speak(new SpeechUtterance
        {
            Text = speakText,
            VoiceName = string.IsNullOrWhiteSpace(voiceName) ? null : voiceName,
            Rate = 1,
            Pitch = 1,
        });
    }
}";

    private readonly string pauseResumeCancelExampleCode =
@"@inject Bit.Butil.SpeechSynthesis speechSynthesis

<BitButton OnClick=""PauseSpeech"">Pause</BitButton>
<BitButton OnClick=""ResumeSpeech"">Resume</BitButton>
<BitButton OnClick=""CancelSpeech"">Cancel</BitButton>

@code {
    private async Task PauseSpeech()
    {
        await speechSynthesis.Pause();
    }

    private async Task ResumeSpeech()
    {
        await speechSynthesis.Resume();
    }

    private async Task CancelSpeech()
    {
        await speechSynthesis.Cancel();
    }
}";

    private readonly string isSpeakingExampleCode =
@"@inject Bit.Butil.SpeechSynthesis speechSynthesis

<BitButton OnClick=""IsSpeaking"">IsSpeaking</BitButton>

<div>Is speaking: @isSpeaking</div>

@code {
    private string? isSpeaking;

    private async Task IsSpeaking()
    {
        isSpeaking = (await speechSynthesis.IsSpeaking()).ToString();
    }
}";

    private readonly string startExampleCode =
@"@inject Bit.Butil.SpeechRecognition speechRecognition

<BitButton OnClick=""StartRecognition"">Start</BitButton>

<div>Transcript: @transcript</div>

@code {
    private string? transcript;
    private IAsyncDisposable? recognitionHandle;

    private async Task StartRecognition()
    {
        if (await speechRecognition.IsSupported() is false) return;

        recognitionHandle = await speechRecognition.Start(
            new SpeechRecognitionOptions { Continuous = true, InterimResults = true },
            onResult: result =>
            {
                transcript = result.IsFinal
                    ? $""Final: {result.Transcript} ({result.Confidence:P0})""
                    : $""Interim: {result.Transcript}"";
                InvokeAsync(StateHasChanged);
            },
            onError: err => { InvokeAsync(StateHasChanged); },
            onEnd: () => { InvokeAsync(StateHasChanged); });
    }
}";

    private readonly string stopExampleCode =
@"@inject Bit.Butil.SpeechRecognition speechRecognition

<BitButton OnClick=""StopRecognition"">Stop</BitButton>

@code {
    private IAsyncDisposable? recognitionHandle;

    private async Task StopRecognition()
    {
        if (recognitionHandle is null) return;
        await recognitionHandle.DisposeAsync();
        recognitionHandle = null;
    }
}";
}
