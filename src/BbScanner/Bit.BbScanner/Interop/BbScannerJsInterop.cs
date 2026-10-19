using System.Runtime.InteropServices.JavaScript;
using System.Runtime.Versioning;

namespace Bit.BbScanner;

/// <summary>
/// The complete JavaScript surface of the library.
/// </summary>
/// <remarks>
/// <para>
/// Every method here is a source generated <see cref="JSImportAttribute"/> binding, not a
/// <c>IJSRuntime.InvokeAsync</c> call. That matters for the frame path: a JSImport call is a
/// direct invocation with no JSON serialisation, and a <see cref="JSType.MemoryView"/> parameter
/// hands JavaScript a view straight onto the managed buffer, so a camera frame is copied exactly
/// once, by the browser, into memory the decoder already owns.
/// </para>
/// <para>
/// The control methods could have used ordinary Blazor interop, but keeping them here means the
/// whole bridge is one module with one lifetime, and the component never needs an
/// <see cref="Microsoft.JSInterop.IJSObjectReference"/> at all.
/// </para>
/// </remarks>
[SupportedOSPlatform("browser")]
internal static partial class BbScannerJsInterop
{
    private const string ModuleName = "bit-bbscanner";

    /// <summary>
    /// Path the JavaScript module is served from by the static web assets pipeline.
    /// </summary>
    /// <remarks>
    /// The path is relative to the runtime itself, which is served from <c>_framework/</c>, not to
    /// the application root: <c>./_content/...</c> would be fetched from
    /// <c>_framework/_content/...</c> and 404. Stepping up one level lands on the real asset while
    /// staying relative, so an application deployed under a sub path still resolves it.
    /// </remarks>
    internal const string ModulePath = "../_content/Bit.BbScanner/bit-bbscanner.js";

    private static Task? _import;

    /// <summary>Whether the module is loaded, so its bindings are safe to invoke.</summary>
    /// <remarks>
    /// Invoking a <see cref="JSImportAttribute"/> binding before its module has been imported is a
    /// runtime assertion, not a catchable exception: it tears the WebAssembly runtime down, and
    /// every interop call after that - including Blazor's own navigation handling - fails with
    /// "runtime already exited". Callers therefore check this first rather than catch afterwards.
    /// </remarks>
    internal static bool IsImported { get; private set; }

    /// <summary>Loads the JavaScript module once per application.</summary>
    internal static Task EnsureImportedAsync() => IsImported ? Task.CompletedTask : ImportCoreAsync();

    private static async Task ImportCoreAsync()
    {
        try
        {
            // Components mounted together share the one in-flight import.
            await (_import ??= JSHost.ImportAsync(ModuleName, ModulePath)).ConfigureAwait(false);
        }
        catch
        {
            // A failed import is not cached, so a later component can try again.
            _import = null;
            throw;
        }

        IsImported = true;
    }

    [JSImport("listCameras", ModuleName)]
    internal static partial Task<string> ListCamerasAsync();

    [JSImport("start", ModuleName)]
    internal static partial Task<string> StartAsync(
        string sessionId,
        string videoElementId,
        string? deviceId,
        int requestedWidth,
        int requestedHeight,
        string? facingMode);

    [JSImport("configure", ModuleName)]
    internal static partial void Configure(string sessionId, int width, int height);

    [JSImport("grabFrame", ModuleName)]
    internal static partial int GrabFrame(
        string sessionId,
        [JSMarshalAs<JSType.MemoryView>] Span<byte> buffer);

    [JSImport("setTorch", ModuleName)]
    internal static partial Task<bool> SetTorchAsync(string sessionId, bool on);

    [JSImport("setZoom", ModuleName)]
    internal static partial Task<bool> SetZoomAsync(string sessionId, double value);

    [JSImport("describe", ModuleName)]
    internal static partial string Describe(string sessionId);

    [JSImport("getViewSize", ModuleName)]
    internal static partial int GetViewSize(string sessionId);

    [JSImport("pollChanges", ModuleName)]
    internal static partial int PollChanges(string sessionId);

    [JSImport("stop", ModuleName)]
    internal static partial void Stop(string sessionId);

    [JSImport("dispose", ModuleName)]
    internal static partial void DisposeSession(string sessionId);

    [JSImport("isTrackEnded", ModuleName)]
    internal static partial bool IsTrackEnded(string sessionId);

    [JSImport("prepareImageFromInput", ModuleName)]
    internal static partial Task<int> PrepareImageFromInputAsync(
        string sessionId,
        string inputElementId,
        int requestId,
        int maxDimension);

    [JSImport("readPrepared", ModuleName)]
    internal static partial int ReadPrepared(
        string sessionId,
        int requestId,
        [JSMarshalAs<JSType.MemoryView>] Span<byte> buffer);

    [JSImport("releasePrepared", ModuleName)]
    internal static partial void ReleasePrepared(string sessionId, int requestId);

    [JSImport("click", ModuleName)]
    internal static partial void Click(string elementId);

    [JSImport("isCameraSupported", ModuleName)]
    internal static partial bool IsCameraSupported();

    [JSImport("isSecureContext", ModuleName)]
    internal static partial bool IsSecureContext();
}
