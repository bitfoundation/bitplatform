using System.Collections.Concurrent;
using Microsoft.JSInterop;

namespace Bit.BbScanner.Tests.Components;

/// <summary>
/// The page a scanner under Blazor Server or Blazor Hybrid talks to, without a browser: an <see cref="IJSRuntime"/> that
/// imports a fake of bit-bbscanner.js, answering its calls the way the module does - a camera that opens, frames that
/// are readied as the luminance of the region asked for and read back in slices, and the poll flags - so the
/// component's remote path runs end to end, decoder included.
/// </summary>
internal sealed class FakeScannerPage : IJSRuntime, IJSObjectReference
{
    private const int PollHidden = 1 << 24;
    private const int PollEnded = 1 << 25;
    private const int PollInterrupted = 1 << 26;
    private const int PollFrame = 1 << 27;

    /// <summary>Every call the module received, in order, with its arguments.</summary>
    public ConcurrentQueue<(string Identifier, object?[] Arguments)> Calls { get; } = new();

    /// <summary>What the page offers: 1, the camera API, and 2, a secure context.</summary>
    public int Capabilities { get; set; } = 3;

    /// <summary>The camera that opens, as the module describes it.</summary>
    public string CameraJson { get; set; } =
        """{"deviceId":"rear","label":"Rear camera","width":640,"height":480,"frameRate":30,"facingMode":"environment","supportsTorch":true,"torchOn":false,"supportsZoom":false,"zoomMin":0,"zoomMax":0,"zoomStep":0,"zoom":0}""";

    /// <summary>When set, opening the camera fails with it, as getUserMedia rejects.</summary>
    public Exception? StartFailure { get; set; }

    /// <summary>The scene in front of the camera, at the size the scanner configured; null for no frames.</summary>
    public byte[]? Frame { get; set; }

    /// <summary>The camera track ended - unplugged, revoked, or closed by the page's watchdog.</summary>
    public bool Ended { get; set; }

    /// <summary>The camera that ended was interrupted, by the page being hidden or by the watchdog.</summary>
    public bool Interrupted { get; set; }

    /// <summary>The page is hidden.</summary>
    public bool Hidden { get; set; }

    /// <summary>The size the scanner asked frames to be read at.</summary>
    public (int Width, int Height) Configured { get; private set; }

    /// <summary>The module path the scanner imported.</summary>
    public string? ImportedPath { get; private set; }

    /// <summary>The largest slice the scanner asked for in one readData call.</summary>
    public int LargestSlice { get; private set; }

    /// <summary>Exceptions the next calls of a function throw, one a call, as a call lost to a reconnecting circuit does.</summary>
    public ConcurrentDictionary<string, ConcurrentQueue<Exception>> Faults { get; } = new();

    /// <summary>What decoding an image the application handed over gives: luminance, and its size.</summary>
    public (byte[] Pixels, int Width, int Height)? DecodedImage { get; set; }

    /// <summary>The parts of encoded images the scanner sent, as their lengths.</summary>
    public ConcurrentQueue<int> StagedParts { get; } = new();

    /// <summary>The region of the latest frame grabFrameData readied.</summary>
    private byte[]? _frameData;

    /// <summary>Images readied for reading, by request.</summary>
    private readonly ConcurrentDictionary<int, byte[]> _prepared = new();

    /// <summary>What attachNativePreview was handed to report placements to: a DotNetObjectReference.</summary>
    public object? PreviewReceiver { get; private set; }

    /// <summary>Reports a placement of the video element, as the page does on an animation frame, in a 400 by 800 viewport.</summary>
    public void Place(double x, double y, double width, double height, bool visible = true)
    {
        var receiver = PreviewReceiver ?? throw new InvalidOperationException("No native preview is attached.");
        var value = receiver.GetType().GetProperty("Value")!.GetValue(receiver)!;
        value.GetType().GetMethod("Place")!.Invoke(value, [x, y, width, height, 12.0, visible, "rgb(255, 255, 255)", 0.0, 0.0, 400.0, 800.0]);
    }

    /// <summary>Makes the next call of <paramref name="identifier"/> throw <paramref name="exception"/>.</summary>
    public void FailNext(string identifier, Exception exception) => Faults.GetOrAdd(identifier, _ => new()).Enqueue(exception);

    public int Count(string identifier) => Calls.Count(c => c.Identifier == identifier);

    public object?[] Last(string identifier) => Calls.Last(c => c.Identifier == identifier).Arguments;

    ValueTask<TValue> IJSRuntime.InvokeAsync<TValue>(string identifier, object?[]? args)
        => ((IJSRuntime)this).InvokeAsync<TValue>(identifier, CancellationToken.None, args);

    ValueTask<TValue> IJSRuntime.InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
    {
        if (identifier != "import")
        {
            throw new InvalidOperationException($"The page has no global function '{identifier}'.");
        }

        ImportedPath = args?[0] as string;
        return new((TValue)(object)this);
    }

    ValueTask<TValue> IJSObjectReference.InvokeAsync<TValue>(string identifier, object?[]? args)
        => ((IJSObjectReference)this).InvokeAsync<TValue>(identifier, CancellationToken.None, args);

    ValueTask<TValue> IJSObjectReference.InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
    {
        args ??= [];
        Calls.Enqueue((identifier, args));
        if (Faults.TryGetValue(identifier, out var faults) && faults.TryDequeue(out var fault))
        {
            return ValueTask.FromException<TValue>(fault);
        }

        object? result = identifier switch
        {
            "capabilities" => Capabilities,
            "listCameras" => """[{"deviceId":"rear","label":"Rear camera"}]""",
            "queryCameraPermission" => "granted",
            "start" => Start(),
            "configure" => Configure((int)args[1]!, (int)args[2]!),
            "poll" => Poll(),
            "grabFrameData" => GrabFrameData((int)args[1]!, (int)args[2]!, (int)args[3]!, (int)args[4]!),
            "readData" => ReadData((int)args[1]!, (int)args[2]!, (int)args[3]!),
            "attachNativePreview" => AttachPreview(args[2]),
            "detachNativePreview" => AttachPreview(null),
            "stageImagePart" => StageImagePart((byte[])args[3]!),
            "prepareStagedParts" => PrepareStagedParts((int)args[1]!, (int)args[2]!),
            "readPreparedData" => _prepared.TryGetValue((int)args[1]!, out var prepared) ? prepared.Length : 0,
            "releasePrepared" => _prepared.TryRemove((int)args[1]!, out _) ? null : null,
            "describe" => Ended ? "" : CameraJson,
            "getViewSize" => (640 << 16) | 480,
            "setTorch" => true,
            "setZoom" => false,
            "stop" => Stop(),
            _ => null,
        };

        return result switch
        {
            ValueTask<TValue> failed => failed,
            null => new(default(TValue)!),
            _ => new((TValue)result),
        };
    }

    private object Start()
    {
        if (StartFailure is { } failure)
        {
            return ValueTask.FromException<string>(failure);
        }

        Ended = false;
        Interrupted = false;
        return CameraJson;
    }

    private object? Configure(int width, int height)
    {
        Configured = (width, height);
        return null;
    }

    private object? Stop()
    {
        // The module's releaseStream forgets how the camera ended along with the camera.
        Interrupted = false;
        return null;
    }

    private int Poll()
    {
        var flags = Hidden ? PollHidden : 0;
        if (Ended)
        {
            flags |= PollEnded;
        }

        if (Interrupted || Hidden)
        {
            flags |= PollInterrupted;
        }

        if (!Ended && Frame is not null && Configured.Width > 0)
        {
            flags |= PollFrame;
        }

        return flags;
    }

    private object? AttachPreview(object? receiver)
    {
        PreviewReceiver = receiver;
        return null;
    }

    private int GrabFrameData(int x, int y, int width, int height)
    {
        var (frameWidth, _) = Configured;
        if (Frame is not { } frame || Ended)
        {
            return 0;
        }

        var region = new byte[width * height];
        for (var row = 0; row < height; row++)
        {
            frame.AsSpan(((y + row) * frameWidth) + x, width).CopyTo(region.AsSpan(row * width));
        }

        _frameData = region;
        return region.Length;
    }

    private byte[] ReadData(int key, int offset, int count)
    {
        LargestSlice = Math.Max(LargestSlice, count);
        var data = key == -1 ? _frameData : _prepared.GetValueOrDefault(key);
        if (data is null || offset >= data.Length)
        {
            return [];
        }

        return data.AsSpan(offset, Math.Min(count, data.Length - offset)).ToArray();
    }

    private object? StageImagePart(byte[] part)
    {
        StagedParts.Enqueue(part.Length);
        return null;
    }

    private int PrepareStagedParts(int requestId, int count)
    {
        if (count != StagedParts.Count || DecodedImage is not { } image)
        {
            return 0;
        }

        _prepared[requestId] = image.Pixels;
        return (image.Width << 16) | image.Height;
    }

    ValueTask IAsyncDisposable.DisposeAsync() => ValueTask.CompletedTask;
}
