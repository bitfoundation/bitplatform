using Bit.Butil;

namespace Bit.Websites.Platform.Client.Pages.Butil;

public partial class Butil24MediaDevicesPage
{
    private string? isSupported;
    private string? devices;
    private string? streamResult;

    private bool requestAudio = true;
    private bool requestVideo = true;
    private bool enabled = true;

    private ElementReference preview;
    private MediaStreamHandle? stream;
    private bool startingStream;


    private async Task IsSupported()
    {
        isSupported = (await mediaDevices.IsSupported()).ToString();
    }

    private async Task EnumerateDevices()
    {
        var result = await mediaDevices.EnumerateDevices();
        if (result.Length == 0)
        {
            devices = "No media devices reported.";
            return;
        }

        devices = string.Join(", ", result.Select(d =>
        {
            var label = string.IsNullOrEmpty(d.Label)
                ? d.DeviceId[..Math.Min(8, d.DeviceId.Length)] + "…"
                : d.Label;
            return $"{d.Kind}: {label}";
        }));
    }

    private async Task StartStream()
    {
        // Guard against overlapping clicks racing across the GetUserMedia await and
        // leaking/overwriting the active stream - only one invocation may run at a time.
        if (startingStream) return;
        startingStream = true;

        try
        {
            await StopStream();

            if (!requestAudio && !requestVideo)
            {
                streamResult = "Enable at least audio or video.";
                return;
            }

            stream = await mediaDevices.GetUserMedia(requestAudio, requestVideo);
            if (stream is null)
            {
                streamResult = "GetUserMedia returned null - permission denied or no device available.";
                return;
            }

            await stream.AttachTo(preview);
            enabled = true;
            streamResult = $"Stream started → {stream.Id}";
        }
        finally
        {
            startingStream = false;
        }
    }

    private async Task ToggleEnabled()
    {
        if (stream is null)
        {
            streamResult = "Start a stream first.";
            return;
        }

        enabled = !enabled;
        await stream.SetEnabled(enabled);
        streamResult = $"Tracks enabled → {enabled}";
    }

    private async Task StopStream()
    {
        if (stream is null) return;
        await stream.DisposeAsync();
        stream = null;
        streamResult = "Stream stopped.";
    }


    protected override async ValueTask DisposeAsync(bool disposing)
    {
        if (stream is not null)
            await stream.DisposeAsync();

        await base.DisposeAsync(disposing);
    }


    private readonly string isSupportedExampleCode =
@"@inject Bit.Butil.MediaDevices mediaDevices

<BitButton OnClick=""IsSupported"">IsSupported</BitButton>

<div>Is supported: @isSupported</div>

@code {
    private string? isSupported;

    private async Task IsSupported()
    {
        isSupported = (await mediaDevices.IsSupported()).ToString();
    }
}";

    private readonly string enumerateDevicesExampleCode =
@"@inject Bit.Butil.MediaDevices mediaDevices

<BitButton OnClick=""EnumerateDevices"">EnumerateDevices</BitButton>

<div>Devices: @devices</div>

@code {
    private string? devices;

    private async Task EnumerateDevices()
    {
        var result = await mediaDevices.EnumerateDevices();
        devices = string.Join("", "", result.Select(d => $""{d.Kind}: {d.Label}""));
    }
}";

    private readonly string getUserMediaExampleCode =
@"@inject Bit.Butil.MediaDevices mediaDevices

<BitCheckbox @bind-Value=""requestAudio"" Label=""Audio"" />
<BitCheckbox @bind-Value=""requestVideo"" Label=""Video"" />

<BitButton OnClick=""StartStream"">Start stream</BitButton>
<BitButton OnClick=""ToggleEnabled"">Toggle enabled</BitButton>
<BitButton OnClick=""StopStream"">Stop stream</BitButton>

<video @ref=""preview"" autoplay playsinline muted></video>

@code {
    private bool requestAudio = true;
    private bool requestVideo = true;
    private bool enabled = true;
    private ElementReference preview;
    private MediaStreamHandle? stream;

    private async Task StartStream()
    {
        await StopStream();
        stream = await mediaDevices.GetUserMedia(requestAudio, requestVideo);
        if (stream is null) return;
        await stream.AttachTo(preview);
        enabled = true;
    }

    private async Task ToggleEnabled()
    {
        if (stream is null) return;
        enabled = !enabled;
        await stream.SetEnabled(enabled);
    }

    private async Task StopStream()
    {
        if (stream is null) return;
        await stream.DisposeAsync();
        stream = null;
    }
}";
}
