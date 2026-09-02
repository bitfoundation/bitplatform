using System;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace Bit.Butil;

/// <summary>
/// Wraps <see href="https://developer.mozilla.org/en-US/docs/Web/API/Window/launchQueue">window.launchQueue</see>
/// - how an installed app receives the files, and the target URL, it was launched with.
/// </summary>
/// <remarks>
/// This only ever fires for an <b>installed</b> app whose manifest declares what it handles:
/// <c>file_handlers</c> for "open with", <c>protocol_handlers</c> for a custom scheme,
/// <c>share_target</c> for a share, and <c>launch_handler</c> for how a second launch is routed
/// (a new window, or the existing one). A page open in a browser tab never receives a launch.
/// <br/>
/// <b>Timing:</b> the browser delivers the launch the moment a consumer exists, and a page may set
/// one only once. Butil installs its consumer while the <c>launchQueue</c> module is evaluated and
/// parks the launch until <see cref="SetConsumer(Action{LaunchParams})"/> is called, so a handler
/// registered in <c>OnAfterRenderAsync</c> still receives it. Under
/// <see cref="BitButil.UseLazyScripts"/> the module isn't imported until the first call into it -
/// call <see cref="IsSupported"/> early in app start-up so the consumer is in place.
/// <br/>
/// A launched file's contents stay on the JS side and are read by index - see <see cref="LaunchFile"/>.
/// </remarks>
[ButilService(typeof(LaunchQueue))]
public class LaunchQueue(IJSRuntime js) : IAsyncDisposable
{
    internal const string InvokeMethodName = nameof(InvokeLaunch);

    private readonly ConcurrentDictionary<Guid, Action<LaunchParams>> _handlers = new();

    // Per-instance callback reference (see Keyboard): the consumer is isolated per circuit / WASM
    // app and released on disposal - no static state, no cross-circuit leak.
    private DotNetObjectReference<LaunchQueue>? _dotNetRef;
    private DotNetObjectReference<LaunchQueue> DotNetRef => DotNetObjectReferenceHelper.GetOrCreate(ref _dotNetRef, this);

    /// <summary>True when the runtime exposes <c>window.launchQueue</c> (Chromium only).</summary>
    /// <remarks>
    /// During prerender/SSR (no JS runtime) this returns <c>default</c> (e.g. <c>false</c>/<c>0</c>)
    /// rather than throwing, so the result can't be distinguished from a genuine value. If you
    /// branch on it, defer the read to <c>OnAfterRenderAsync</c>.
    /// </remarks>
    public ValueTask<bool> IsSupported() => js.Invoke<bool>("BitButil.launchQueue.isSupported");

    /// <summary>
    /// True when the runtime also implements File Handling, i.e. a launch can carry files
    /// (<c>LaunchParams.files</c>). Launch handling without file handling is possible.
    /// </summary>
    public ValueTask<bool> SupportsFiles() => js.Invoke<bool>("BitButil.launchQueue.supportsFiles");

    /// <summary>
    /// Invoked from JS when the app is launched. Public + <see cref="JSInvokableAttribute"/> so it
    /// can be dispatched through the per-instance <see cref="DotNetObjectReference{T}"/>.
    /// </summary>
    [JSInvokable(InvokeMethodName)]
    public void InvokeLaunch(Guid id, LaunchParams launchParams)
    {
        if (_handlers.TryGetValue(id, out var handler)) handler.Invoke(launchParams ?? new LaunchParams());
    }

    /// <summary>
    /// Registers the handler that receives this app's launch. Fires immediately when the launch has
    /// already arrived, so registering late is safe.
    /// </summary>
    /// <param name="handler">Called once per launch, with the target URL and any files.</param>
    /// <returns>
    /// A subscription that unregisters the handler on dispose. Only the most recent registration
    /// receives launches; the underlying <c>setConsumer</c> is set once by Butil and never replaced.
    /// </returns>
    [DynamicDependency(nameof(InvokeLaunch), typeof(LaunchQueue))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(LaunchParams))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(LaunchFile))]
    public async Task<ButilSubscription> SetConsumer(Action<LaunchParams> handler)
    {
        ArgumentNullException.ThrowIfNull(handler);

        var id = Guid.NewGuid();
        _handlers.TryAdd(id, handler);

        await js.InvokeVoid("BitButil.launchQueue.setConsumer", DotNetRef, id, InvokeMethodName);

        return new ButilSubscription(id, async () =>
        {
            _handlers.TryRemove(id, out _);
            await js.InvokeVoid("BitButil.launchQueue.clearConsumer", id);
        });
    }

    /// <summary>
    /// Reads a launched file as text.
    /// </summary>
    /// <param name="file">A file from the launch.</param>
    /// <returns>The contents, or null when the handle went stale or access was refused.</returns>
    public ValueTask<string?> ReadText(LaunchFile file)
    {
        ArgumentNullException.ThrowIfNull(file);
        return ReadText(file.Index);
    }

    /// <summary>Reads a launched file as text, by its <see cref="LaunchFile.Index"/>.</summary>
    public ValueTask<string?> ReadText(int index) => js.Invoke<string?>("BitButil.launchQueue.readText", index);

    /// <summary>
    /// Reads a launched file as bytes.
    /// </summary>
    /// <returns>The contents, or null when the handle went stale or access was refused.</returns>
    public ValueTask<byte[]?> ReadBytes(LaunchFile file)
    {
        ArgumentNullException.ThrowIfNull(file);
        return ReadBytes(file.Index);
    }

    /// <summary>Reads a launched file as bytes, by its <see cref="LaunchFile.Index"/>.</summary>
    public ValueTask<byte[]?> ReadBytes(int index) => js.Invoke<byte[]?>("BitButil.launchQueue.readBytes", index);

    /// <summary>
    /// Writes text back to a launched file - the "save" half of an editor launched through
    /// <c>file_handlers</c>.
    /// </summary>
    /// <returns>
    /// False when the runtime can't write to the handle, or the user refused the write permission
    /// the browser asks for on the first save.
    /// </returns>
    public ValueTask<bool> WriteText(LaunchFile file, string contents)
    {
        ArgumentNullException.ThrowIfNull(file);
        return WriteText(file.Index, contents);
    }

    /// <summary>Writes text back to a launched file, by its <see cref="LaunchFile.Index"/>.</summary>
    public ValueTask<bool> WriteText(int index, string contents)
        => js.Invoke<bool>("BitButil.launchQueue.writeText", index, contents);

    /// <summary>Writes bytes back to a launched file.</summary>
    /// <returns>False when the runtime can't write to the handle, or the write permission was refused.</returns>
    public ValueTask<bool> WriteBytes(LaunchFile file, byte[] contents)
    {
        ArgumentNullException.ThrowIfNull(file);
        return WriteBytes(file.Index, contents);
    }

    /// <summary>Writes bytes back to a launched file, by its <see cref="LaunchFile.Index"/>.</summary>
    public ValueTask<bool> WriteBytes(int index, byte[] contents)
        => js.Invoke<bool>("BitButil.launchQueue.writeBytes", index, contents);

    /// <summary>Unregisters every handler registered through this instance and releases its interop reference.</summary>
    public async ValueTask DisposeAsync()
    {
        try
        {
            var ids = _handlers.Keys.ToArray();
            _handlers.Clear();
            foreach (var id in ids) await js.InvokeVoid("BitButil.launchQueue.clearConsumer", id);
        }
        catch (Exception ex) when (ex.IsIgnorableDisposalException()) { } // teardown: circuit gone, cancelled, or already disposed
        finally
        {
            _dotNetRef?.Dispose();
            _dotNetRef = null;
        }
        GC.SuppressFinalize(this);
    }
}
