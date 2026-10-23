using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Bit.Bink;

/// <summary>Which canvas layer a command buffer is drawn onto.</summary>
public enum BinkCanvasLayer
{
    /// <summary>The document surface: committed elements. Redrawn only when the document or viewport changes.</summary>
    Document,
    /// <summary>The overlay surface: the live stroke, selection handles and tool previews. Redrawn every frame of a gesture.</summary>
    Overlay,
}

/// <summary>Options passed to the browser layer when a canvas is attached.</summary>
internal sealed record BinkInteropOptions
{
    public bool LowLatency { get; init; } = true;
    public bool RejectTouchWhilePen { get; init; } = true;
    public bool SuppressContextMenu { get; init; } = true;
    public bool TrackHover { get; init; }
    public bool PreventDefaultOnDown { get; init; } = true;
    public bool WetInk { get; init; }
    public bool HandleKeyboard { get; init; } = true;
    public int WheelMode { get; init; }
    public bool Clipboard { get; init; }
    public bool Predict { get; init; }
    public bool DropFiles { get; init; }
    public string[]? DisabledShortcuts { get; init; }
}

/// <summary>Size and capability information reported by the browser.</summary>
internal sealed record BinkAttachResult
{
    public double Width { get; init; }
    public double Height { get; init; }
    public double DevicePixelRatio { get; init; } = 1;
    public int MaxTouchPoints { get; init; }
    public bool CoalescedEvents { get; init; }
    public bool PredictedEvents { get; init; }
    public bool PointerEvents { get; init; }
    public bool Tilt { get; init; }
    public bool Twist { get; init; }
    public bool OffscreenCanvas { get; init; }
    public bool LowLatency { get; init; }

    public BinkInputCapabilities ToCapabilities() => new()
    {
        PointerEvents = PointerEvents,
        CoalescedEvents = CoalescedEvents,
        PredictedEvents = PredictedEvents,
        Tilt = Tilt,
        Twist = Twist,
        MaxTouchPoints = MaxTouchPoints,
        DevicePixelRatio = DevicePixelRatio <= 0 ? 1 : DevicePixelRatio,
        OffscreenCanvas = OffscreenCanvas,
        LowLatencyCanvas = LowLatency,
    };
}

/// <summary>Measured text dimensions returned by the browser.</summary>
internal sealed record BinkTextMetrics
{
    public double Width { get; init; }
    public int Lines { get; init; } = 1;
}

/// <summary>Natural size of a loaded image.</summary>
internal sealed record BinkImageMetrics
{
    public int Width { get; init; }
    public int Height { get; init; }
}

/// <summary>Receives events from the browser layer.</summary>
internal interface IBinkInteropClient
{
    void OnSamples(ReadOnlySpan<double> data, int count);
    void OnResized(double width, double height, double devicePixelRatio);
    void OnKey(string key, bool ctrl, bool shift, bool alt);
    /// <summary>The element the inline text editor is open for, or null while it is closed.</summary>
    Guid? TextEditorElementId { get; }
    /// <summary>A commit or cancel from the editor that was open for <paramref name="elementId"/> when it was sent.</summary>
    void OnTextEditorKey(string key, string value, Guid elementId);
    void OnRedrawRequested();
    void OnResourcesLost();
    void OnWheel(float panX, float panY, float zoom, float x, float y);
    Task OnPasteAsync(string kind, string? text, IJSStreamReference? data, string? mediaType);
    Task OnDropAsync(string kind, IJSStreamReference data, string? mediaType, float x, float y);
}

/// <summary>
/// The bridge between the C# engine and the browser layer. One instance per canvas.
/// </summary>
/// <remarks>
/// <para>
/// All calls funnel through a single JavaScript module. In WebAssembly the high-volume rendering direction uses
/// <c>[JSImport]</c> with a memory view, which hands the command buffer to JavaScript without copying or
/// serializing it. Everywhere else, and for the low-volume calls, the standard <see cref="IJSRuntime"/> path is
/// used, so the same component works in Blazor Server and in hybrid hosts.
/// </para>
/// <para>Input always travels as one batched call per animation frame, never one call per pointer event.</para>
/// </remarks>
internal sealed class BinkInterop : IAsyncDisposable
{
    /// <summary>The module path used with Blazor's importer, which resolves it against the document base URI.</summary>
    private const string ModulePath = "./_content/Bit.Bink/bit-bink.js";

    /// <summary>The same file relative to the application root, for importers that do not use the document base.</summary>
    private const string ModuleRelativePath = "_content/Bit.Bink/bit-bink.js";

    private readonly IJSRuntime _jsRuntime;
    private readonly IBinkInteropClient _client;
    private readonly string _id;
    private readonly string _baseUri;
    private IJSObjectReference? _module;
    private DotNetObjectReference<InteropCallbacks>? _callbacks;
    private bool _useWasmFastPath;
    private bool _disposed;

    public BinkInterop(IJSRuntime jsRuntime, IBinkInteropClient client, string id, string baseUri)
    {
        ArgumentNullException.ThrowIfNull(jsRuntime);
        ArgumentNullException.ThrowIfNull(client);
        _jsRuntime = jsRuntime;
        _client = client;
        _id = id;
        _baseUri = baseUri;
    }

    /// <summary>True when the zero-copy WebAssembly rendering path is in use.</summary>
    public bool UsesFastPath => _useWasmFastPath;

    /// <summary>True when the browser module loaded, so calls that need no surface (such as a download) can work.</summary>
    public bool IsModuleLoaded => _module is not null && !_disposed;

    /// <summary>Loads the module, attaches to the element and returns the browser's capabilities.</summary>
    // Some of the options' getters are read only by System.Text.Json, so without this the trimmer drops them and those
    // options never reach the browser in trimmed (release) builds - which then falls back to its own defaults.
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(BinkInteropOptions))]
    public async Task<BinkAttachResult?> AttachAsync(ElementReference element, BinkInteropOptions options, CancellationToken cancellationToken = default)
    {
        // The module reference is held in a local across every await. Disposal can run at any of these points and
        // clears the field; resuming on a null field would throw a NullReferenceException nothing catches.
        IJSObjectReference module = await _jsRuntime.InvokeAsync<IJSObjectReference>("import", cancellationToken, ModulePath).ConfigureAwait(false);
        if (module is null) return null;
        if (_disposed)
        {
            await DisposeModuleAsync(module).ConfigureAwait(false);
            return null;
        }
        _module = module;

        BinkAttachResult? result = await module.InvokeAsync<BinkAttachResult?>("attach", cancellationToken, _id, element, options).ConfigureAwait(false);
        if (result is null || _disposed) return null;

        var callbacks = DotNetObjectReference.Create(new InteropCallbacks(_client));
        await module.InvokeVoidAsync("setDotNetCallbacks", cancellationToken, _id, callbacks).ConfigureAwait(false);
        if (_disposed)
        {
            callbacks.Dispose();
            return null;
        }
        _callbacks = callbacks;

        if (OperatingSystem.IsBrowser())
        {
            // The WebAssembly runtime resolves module specifiers against its own location under _framework, not
            // against the document base, so the fast path needs an absolute URL. The module keeps its surfaces on
            // globalThis, so importing it twice still gives both views the same state.
            _useWasmFastPath = await BinkWasmRenderBridge.TryInitializeAsync(ResolveModuleUrl()).ConfigureAwait(false);
        }
        return result;
    }

    private static async ValueTask DisposeModuleAsync(IJSObjectReference module)
    {
        try
        {
            await module.DisposeAsync().ConfigureAwait(false);
        }
        catch (JSDisconnectedException)
        {
        }
        catch (ObjectDisposedException)
        {
        }
    }

    private string ResolveModuleUrl()
    {
        if (Uri.TryCreate(_baseUri, UriKind.Absolute, out Uri? baseUri) && Uri.TryCreate(baseUri, ModuleRelativePath, out Uri? absolute))
        {
            return absolute.ToString();
        }
        return "/" + ModuleRelativePath;
    }

    /// <summary>Sets the canvas backing store size for a CSS size and device pixel ratio.</summary>
    public ValueTask SetSizeAsync(double cssWidth, double cssHeight, double devicePixelRatio)
        => InvokeVoidAsync("setSize", _id, cssWidth, cssHeight, devicePixelRatio);

    /// <summary>Sends a recorded command buffer to a canvas layer.</summary>
    public ValueTask ExecuteAsync(BinkCanvasLayer layer, BinkCanvasRenderContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (_disposed || context.IsEmpty) return ValueTask.CompletedTask;
        string layerName = layer == BinkCanvasLayer.Document ? "document" : "overlay";
        string[] strings = context.Strings.Count == 0 ? [] : [.. context.Strings];

        if (_useWasmFastPath)
        {
            // Hands the buffer straight to JavaScript as a view over managed memory: no copy, no serialization.
            BinkWasmRenderBridge.Execute(_id, layerName, context.CommandSegment, context.Count, strings);
            return ValueTask.CompletedTask;
        }
        // Packed floats rather than a double array: Blazor sends a byte array as binary, where an array of doubles
        // becomes JSON text of about eighteen characters a number that the browser then has to parse.
        return InvokeVoidAsync("execute", _id, layerName, context.ToSingleBytes(), context.Count, strings);
    }

    /// <summary>Updates the options given at attach, when the parameters behind them change.</summary>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(BinkInteropOptions))]
    public ValueTask SetOptionsAsync(BinkInteropOptions options) => InvokeVoidAsync("setOptions", _id, options);

    /// <summary>Sets the cursor shown over the canvas.</summary>
    public ValueTask SetCursorAsync(string cursor) => InvokeVoidAsync("setCursor", _id, cursor);

    /// <summary>Configures the provisional trail drawn while a sample batch is in flight (Blazor Server only).</summary>
    public ValueTask SetWetInkStyleAsync(BinkColor? color, float width, float opacity)
        => InvokeVoidAsync("setWetInkStyle", _id, color?.ToCss(), width, opacity);

    /// <summary>Loads an image in the browser and returns its natural size.</summary>
    public async Task<BinkImageMetrics?> LoadImageAsync(string uri, CancellationToken cancellationToken = default)
    {
        if (_module is null || _disposed) return null;
        try
        {
            return await _module.InvokeAsync<BinkImageMetrics>("loadImage", cancellationToken, _id, uri).ConfigureAwait(false);
        }
        catch (JSException)
        {
            return null;
        }
        catch (JSDisconnectedException)
        {
            return null;
        }
        catch (ObjectDisposedException)
        {
            return null;
        }
    }

    /// <summary>
    /// Tells the browser layer whether anything is selected. It decides from this whether Delete, Backspace and
    /// the arrow keys belong to the canvas or to the page, and that decision has to be made synchronously inside
    /// the key event.
    /// </summary>
    public ValueTask SetSelectionActiveAsync(bool active) => InvokeVoidAsync("setSelectionActive", _id, active);

    /// <summary>
    /// Completes a copy the user started from the keyboard, or writes the system clipboard directly for one the
    /// application started. Null settles a keyboard copy that had nothing to copy.
    /// </summary>
    public ValueTask ProvideClipboardAsync(string? text) => InvokeVoidAsync("provideClipboard", _id, text);

    /// <summary>
    /// Completes a copy with the selection's picture too: the commands that draw it at the given size, which the browser
    /// renders and puts on the clipboard as a PNG beside the text, for other applications to paste.
    /// </summary>
    public ValueTask ProvideClipboardAsync(string text, int width, int height, BinkCanvasRenderContext picture)
    {
        ArgumentNullException.ThrowIfNull(picture);
        string[] strings = picture.Strings.Count == 0 ? [] : [.. picture.Strings];
        return InvokeVoidAsync("provideClipboard", _id, text, width, height, picture.ToSingleBytes(), picture.Count, strings);
    }

    /// <summary>
    /// Reads text from the system clipboard, which the browser may ask the user to allow. Null when it is empty,
    /// refused or unavailable.
    /// </summary>
    public async Task<string?> ReadClipboardAsync(long maxLength, CancellationToken cancellationToken = default)
    {
        if (_module is null || _disposed) return null;
        try
        {
            IJSStreamReference? stream = await _module.InvokeAsync<IJSStreamReference?>("readClipboard", cancellationToken).ConfigureAwait(false);
            return stream is null ? null : await ReadTextAsync(stream, maxLength, cancellationToken).ConfigureAwait(false);
        }
        catch (JSException)
        {
            return null;
        }
        catch (JSDisconnectedException)
        {
            return null;
        }
        catch (IOException)
        {
            // Larger than the limit, or the stream broke off.
            return null;
        }
    }

    /// <summary>Reads a browser stream to the end, refusing more than <paramref name="maxLength"/> bytes.</summary>
    /// <exception cref="IOException">The stream is longer than <paramref name="maxLength"/>, or it broke off.</exception>
    public static async Task<byte[]> ReadBytesAsync(IJSStreamReference stream, long maxLength, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stream);
        await using (stream.ConfigureAwait(false))
        {
            // The length is known before reading. Checked here, because OpenReadStreamAsync reports a stream over its
            // limit with an ArgumentOutOfRangeException, which reads as a mistake by the caller rather than as data
            // that was too large.
            if (stream.Length > maxLength) throw new IOException($"The stream of {stream.Length} bytes exceeds the limit of {maxLength}.");
            await using Stream source = await stream.OpenReadStreamAsync(maxLength, cancellationToken).ConfigureAwait(false);
            using var buffer = new MemoryStream();
            await source.CopyToAsync(buffer, cancellationToken).ConfigureAwait(false);
            return buffer.ToArray();
        }
    }

    /// <summary>Reads a browser stream of UTF-8 text.</summary>
    public static async Task<string> ReadTextAsync(IJSStreamReference stream, long maxLength, CancellationToken cancellationToken = default)
        => System.Text.Encoding.UTF8.GetString(await ReadBytesAsync(stream, maxLength, cancellationToken).ConfigureAwait(false));

    /// <summary>Measures text with the browser's font metrics.</summary>
    public async Task<BinkTextMetrics?> MeasureTextAsync(string text, string font, CancellationToken cancellationToken = default)
    {
        if (_module is null || _disposed) return null;
        try
        {
            return await _module.InvokeAsync<BinkTextMetrics>("measureText", cancellationToken, _id, text, font).ConfigureAwait(false);
        }
        catch (JSException)
        {
            return null;
        }
        catch (JSDisconnectedException)
        {
            return null;
        }
        catch (ObjectDisposedException)
        {
            return null;
        }
    }

    /// <summary>
    /// Renders a command buffer into an offscreen canvas and returns encoded image bytes. The image comes back as a
    /// stream: returned as a byte array it would be a single message, which Blazor Server refuses above 32 KB.
    /// </summary>
    /// <param name="pixelWidth">Output width in pixels.</param>
    /// <param name="pixelHeight">Output height in pixels.</param>
    /// <param name="context">The recorded commands.</param>
    /// <param name="mimeType">The image type to encode.</param>
    /// <param name="quality">Quality for lossy types.</param>
    /// <param name="maxBytes">The largest encoded image accepted.</param>
    /// <param name="cancellationToken">Cancels the export.</param>
    public async Task<byte[]> RenderToImageAsync(int pixelWidth, int pixelHeight, BinkCanvasRenderContext context, string mimeType, double quality, long maxBytes, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (_module is null || _disposed) return [];
        string[] strings = context.Strings.Count == 0 ? [] : [.. context.Strings];
        IJSStreamReference image = await _module.InvokeAsync<IJSStreamReference>("renderToImage", cancellationToken, _id, pixelWidth, pixelHeight, context.ToSingleBytes(), context.Count, strings, mimeType, quality).ConfigureAwait(false);
        return await ReadBytesAsync(image, maxBytes, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Opens the browser's eyedropper. Null when there is none or the user cancelled it.</summary>
    public async Task<string?> PickColorAsync(CancellationToken cancellationToken = default)
    {
        if (_module is null || _disposed) return null;
        return await _module.InvokeAsync<string?>("pickColor", cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Whether the browser has an eyedropper.</summary>
    public async Task<bool> SupportsEyeDropperAsync(CancellationToken cancellationToken = default)
    {
        if (_module is null || _disposed) return false;
        return await _module.InvokeAsync<bool>("supportsEyeDropper", cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Offers bytes to the user as a file download.</summary>
    public ValueTask SaveFileAsync(string fileName, byte[] bytes, string mimeType)
        => InvokeVoidAsync("saveFile", fileName, bytes, mimeType);

    /// <summary>Returns the element's current CSS size and device pixel ratio.</summary>
    public async Task<BinkAttachResult?> GetSizeAsync(CancellationToken cancellationToken = default)
    {
        if (_module is null || _disposed) return null;
        return await _module.InvokeAsync<BinkAttachResult?>("getSize", cancellationToken, _id).ConfigureAwait(false);
    }

    private ValueTask InvokeVoidAsync(string identifier, params object?[] args)
    {
        if (_module is null || _disposed) return ValueTask.CompletedTask;
        return _module.InvokeVoidAsync(identifier, args);
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;
        if (_module is not null)
        {
            try
            {
                await _module.InvokeVoidAsync("detach", _id).ConfigureAwait(false);
                await _module.DisposeAsync().ConfigureAwait(false);
            }
            catch (JSDisconnectedException)
            {
                // The circuit or page is already gone; nothing to clean up on the other side.
            }
            catch (ObjectDisposedException)
            {
            }
            catch (TaskCanceledException)
            {
            }
#pragma warning disable CA1031 // Any other browser-side failure must still release the callback reference below.
            catch
            {
            }
#pragma warning restore CA1031
            finally
            {
                // The callback reference is held by the JS interop tracking table until it is disposed. Losing it
                // to an exception on the way out would pin this component for the lifetime of the circuit.
                _callbacks?.Dispose();
                _callbacks = null;
                _module = null;
            }
            return;
        }
        _callbacks?.Dispose();
        _callbacks = null;
    }

    /// <summary>The object JavaScript calls back into. Kept separate so the public surface stays small.</summary>
    internal sealed class InteropCallbacks(IBinkInteropClient client)
    {
        /// <summary>Receives one batch of pointer samples per animation frame.</summary>
        [JSInvokable]
        public void OnPointerSamples(double[] data, int count)
        {
            if (data is null || count <= 0) return;
            client.OnSamples(data.AsSpan(0, Math.Min(data.Length, count * BinkInputSample.PackedSize)), count);
        }

        /// <summary>Reports a size or device pixel ratio change.</summary>
        [JSInvokable]
        public void OnResized(double width, double height, double devicePixelRatio)
            => client.OnResized(width, height, devicePixelRatio);

        /// <summary>Reports a key press while the canvas has focus.</summary>
        [JSInvokable]
        public void OnKeyDown(string key, bool ctrl, bool shift, bool alt)
        {
            if (!string.IsNullOrEmpty(key)) client.OnKey(key, ctrl, shift, alt);
        }

        /// <summary>Reports a commit or cancel from the inline text editor, with its current value.</summary>
        [JSInvokable]
        public void OnTextEditorKey(string key, string value)
        {
            if (!string.IsNullOrEmpty(key) && client.TextEditorElementId is { } elementId) client.OnTextEditorKey(key, value ?? string.Empty, elementId);
        }

        /// <summary>
        /// Reports a commit or cancel from the inline text editor whose value is too long for one message, which a
        /// Blazor Server circuit refuses above 32 KB, so it arrives as a stream.
        /// </summary>
        [JSInvokable]
        public async Task OnTextEditorKeyStream(string key, IJSStreamReference value)
        {
            if (value is null) return;
            // Which editor the text came from is fixed now. While it is read, the click that ended the edit is
            // processed, and it can already have opened the editor on another text element.
            Guid? editor = string.IsNullOrEmpty(key) ? null : client.TextEditorElementId;
            if (editor is not { } elementId || key == "Escape")
            {
                // A cancel does not need the text, so it is applied at once, before anything else can happen.
                if (editor is { } cancelled) client.OnTextEditorKey(key, string.Empty, cancelled);
                await value.DisposeAsync().ConfigureAwait(true);
                return;
            }
            string text;
            try
            {
                // UTF-8 needs at most three bytes for each UTF-16 character.
                text = await ReadTextAsync(value, BinkTextElement.MaxLength * 3L).ConfigureAwait(true);
            }
            catch (IOException)
            {
                return;
            }
            catch (JSDisconnectedException)
            {
                return;
            }
            client.OnTextEditorKey(key, text, elementId);
        }

        /// <summary>
        /// Asks for both layers to be redrawn: an image the last frame referenced has finished decoding, or the
        /// browser restored a canvas whose 2D context it had discarded.
        /// </summary>
        [JSInvokable]
        public void OnRedrawRequested() => client.OnRedrawRequested();

        /// <summary>
        /// Reports that a frame referred to a retained path or string the browser does not hold, so both sides
        /// start over with a full redraw.
        /// </summary>
        [JSInvokable]
        public void OnResourcesLost() => client.OnResourcesLost();

        /// <summary>Reports one frame's worth of wheel and trackpad movement over the canvas.</summary>
        [JSInvokable]
        public void OnWheel(double panX, double panY, double zoom, double x, double y)
        {
            if (!double.IsFinite(panX) || !double.IsFinite(panY) || !double.IsFinite(zoom) || !double.IsFinite(x) || !double.IsFinite(y)) return;
            client.OnWheel((float)panX, (float)panY, (float)Math.Clamp(zoom, -5, 5), (float)x, (float)y);
        }

        /// <summary>
        /// Reports a paste while the canvas had focus: copied Bink content or plain text, an image as a stream, or
        /// <c>internal</c> when the canvas should paste the copy it kept itself.
        /// </summary>
        [JSInvokable]
        public Task OnPaste(string kind, string? text, IJSStreamReference? data, string? mediaType)
            => string.IsNullOrEmpty(kind) ? Task.CompletedTask : client.OnPasteAsync(kind, text, data, mediaType);

        /// <summary>
        /// Reports a file dropped on the canvas: <c>image</c> or <c>bink</c> (a saved drawing), as a stream, with where
        /// it was dropped in CSS pixels of the canvas.
        /// </summary>
        [JSInvokable]
        public Task OnDrop(string kind, IJSStreamReference data, string? mediaType, double x, double y)
        {
            if (string.IsNullOrEmpty(kind) || data is null) return Task.CompletedTask;
            return client.OnDropAsync(kind, data, mediaType, double.IsFinite(x) ? (float)x : 0f, double.IsFinite(y) ? (float)y : 0f);
        }
    }
}
