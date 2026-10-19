using System.Runtime.InteropServices.JavaScript;
using System.Runtime.Versioning;

namespace Bit.Bink;

/// <summary>
/// The zero-copy rendering path for Blazor WebAssembly.
/// </summary>
/// <remarks>
/// <para>
/// Standard Blazor interop serializes arguments to JSON. For a canvas that is redrawn every animation frame with
/// buffers of thousands of numbers, that serialization dominates the frame budget. <c>[JSImport]</c> can instead
/// pass an <see cref="ArraySegment{T}"/> as a memory view: JavaScript reads the managed buffer directly, so a
/// redraw costs one call and no allocation.
/// </para>
/// <para>
/// This type only compiles into meaningful code on the browser platform. Every entry point is guarded so the
/// same assembly still runs on a server, where <see cref="BinkInterop"/> falls back to <c>IJSRuntime</c>.
/// </para>
/// </remarks>
internal static partial class BinkWasmRenderBridge
{
    private const string ModuleName = "bit-bink";
    private static Task<bool>? _initialization;
    private static bool _available;

    /// <summary>Imports the module into the WebAssembly runtime. Returns false when the fast path is unavailable.</summary>
    /// <remarks>
    /// Every caller shares the one import. A flag set before awaiting it made a second canvas attaching while the
    /// import was still in flight read "not available" and fall back to the slow path for the rest of its life.
    /// </remarks>
    public static Task<bool> TryInitializeAsync(string modulePath) => _initialization ??= InitializeAsync(modulePath);

    private static async Task<bool> InitializeAsync(string modulePath)
    {
        if (!OperatingSystem.IsBrowser()) return false;
        try
        {
            await ImportModuleAsync(modulePath).ConfigureAwait(false);
            _available = true;
        }
        catch (Exception ex) when (ex is JSException or InvalidOperationException or PlatformNotSupportedException or FileNotFoundException)
        {
            // Any failure here is not fatal: rendering continues through the IJSRuntime path.
            _available = false;
        }
        return _available;
    }

    [SupportedOSPlatform("browser")]
    private static Task ImportModuleAsync(string modulePath) => JSHost.ImportAsync(ModuleName, modulePath);

    /// <summary>Sends a command buffer to a canvas layer without copying it.</summary>
    public static void Execute(string id, string layer, ArraySegment<double> commands, int count, string[] strings)
    {
        if (!_available || !OperatingSystem.IsBrowser()) return;
        ExecuteCore(id, layer, commands, count, strings);
    }

    [SupportedOSPlatform("browser")]
    private static void ExecuteCore(string id, string layer, ArraySegment<double> commands, int count, string[] strings)
        => ExecuteImport(id, layer, commands, count, strings);

    // The ArraySegment is marshalled as a MemoryView: JavaScript sees a typed-array view over the managed buffer
    // for the duration of the call, so nothing is copied or serialized.
    [SupportedOSPlatform("browser")]
    [JSImport("execute", ModuleName)]
    private static partial void ExecuteImport(
        string id,
        string layer,
        [JSMarshalAs<JSType.MemoryView>] ArraySegment<double> commands,
        int count,
        string[] strings);
}
