using System;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace Bit.Butil;

/// <summary>
/// Wraps the installable-app hooks of a PWA: the
/// <see href="https://developer.mozilla.org/en-US/docs/Web/API/BeforeInstallPromptEvent">beforeinstallprompt</see>
/// event, the prompt it defers, and the
/// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Window/appinstalled_event">appinstalled</see>
/// event that follows a successful install.
/// </summary>
/// <remarks>
/// The browser fires <c>beforeinstallprompt</c> exactly once, early in the page's life, and only when
/// the app meets the install criteria (a manifest, a service worker, HTTPS, and - in Chromium - some
/// engagement). Butil's script captures that event and calls <c>preventDefault()</c> on it, which is
/// what suppresses the browser's own mini-infobar and lets <see cref="Prompt"/> show the dialog later,
/// from your own button.
/// <br/>
/// <b>Timing:</b> the capture happens when the <c>installPrompt</c> module is evaluated. With the
/// classic single bundle that is early enough. Under <see cref="BitButil.UseLazyScripts"/> the module
/// is imported on first use, which can be after the event has already fired - add this to your host
/// page so nothing is missed:
/// <code>
/// &lt;script&gt;
///   window.addEventListener('beforeinstallprompt', e =&gt; {
///     e.preventDefault();
///     window.BitButilDeferredInstallPrompt = e;
///   });
/// &lt;/script&gt;
/// </code>
/// Butil adopts that stashed event when the module loads.
/// <br/>
/// Only Chromium implements this event. On Safari and Firefox <see cref="IsSupported"/> is false and
/// installing is a manual browser-menu action, so treat the install button as an enhancement.
/// </remarks>
[ButilService(typeof(InstallPrompt))]
public class InstallPrompt(IJSRuntime js) : IAsyncDisposable
{
    internal const string AvailableMethodName = nameof(InvokeInstallAvailable);
    internal const string InstalledMethodName = nameof(InvokeAppInstalled);

    private readonly ConcurrentDictionary<Guid, Action<string[]>> _availableHandlers = new();
    private readonly ConcurrentDictionary<Guid, Action> _installedHandlers = new();

    // Per-instance callback reference (see Keyboard): listeners are isolated per circuit / WASM app
    // and released on disposal - no static state, no cross-circuit leak.
    private DotNetObjectReference<InstallPrompt>? _dotNetRef;
    private DotNetObjectReference<InstallPrompt> DotNetRef => DotNetObjectReferenceHelper.GetOrCreate(ref _dotNetRef, this);

    /// <summary>True when the runtime fires <c>beforeinstallprompt</c> at all (Chromium only).</summary>
    /// <remarks>
    /// During prerender/SSR (no JS runtime) this returns <c>default</c> (e.g. <c>false</c>/<c>0</c>)
    /// rather than throwing, so the result can't be distinguished from a genuine value. If you
    /// branch on it, defer the read to <c>OnAfterRenderAsync</c>.
    /// </remarks>
    public ValueTask<bool> IsSupported() => js.Invoke<bool>("BitButil.installPrompt.isSupported");

    /// <summary>
    /// True when a deferred prompt is in hand, i.e. <see cref="Prompt"/> would show the install
    /// dialog right now. This is the condition to show your install button on.
    /// </summary>
    /// <remarks>
    /// False both before the browser has decided the app is installable and after the prompt has
    /// been used - a deferred event may only be prompted once.
    /// </remarks>
    public ValueTask<bool> IsAvailable() => js.Invoke<bool>("BitButil.installPrompt.isAvailable");

    /// <summary>
    /// The platforms the captured event offers an install on (for example <c>["web"]</c>, or
    /// <c>["play", "web"]</c> when the manifest declares a related native app). Empty when no
    /// prompt is available.
    /// </summary>
    public ValueTask<string[]> GetPlatforms() => js.Invoke<string[]>("BitButil.installPrompt.getPlatforms");

    /// <summary>True when <c>appinstalled</c> has fired during this page's lifetime.</summary>
    /// <remarks>
    /// This is per page load, not persistent: a returning visitor to an already-installed app sees
    /// false here. Use <see cref="IsStandalone"/> to detect that the app is currently running as an
    /// installed app, or <see cref="Navigator.GetInstalledRelatedApps"/> to detect a native install.
    /// </remarks>
    public ValueTask<bool> WasInstalled() => js.Invoke<bool>("BitButil.installPrompt.wasInstalled");

    /// <summary>
    /// True when the page is running as an installed app rather than in a browser tab - any of the
    /// <c>standalone</c>, <c>minimal-ui</c>, <c>fullscreen</c> or <c>window-controls-overlay</c>
    /// display modes, plus iOS Safari's non-standard <c>navigator.standalone</c>.
    /// </summary>
    /// <remarks>
    /// This is the portable "am I installed" check, and the one to hide an install button on.
    /// </remarks>
    public ValueTask<bool> IsStandalone() => js.Invoke<bool>("BitButil.installPrompt.isStandalone");

    /// <summary>
    /// Shows the install dialog and resolves once the user has answered it.
    /// </summary>
    /// <returns>
    /// The user's choice, plus the platform they installed on.
    /// <see cref="InstallPromptOutcome.Unavailable"/> means no deferred prompt was in hand or the
    /// call wasn't tied to a user gesture - nothing was shown.
    /// </returns>
    /// <remarks>
    /// Must be called from a user-gesture handler such as a button click. The captured event is
    /// spent afterwards whatever the answer, so <see cref="IsAvailable"/> becomes false; a user who
    /// dismissed the dialog can only be prompted again after the browser fires
    /// <c>beforeinstallprompt</c> anew, which it does on a later visit.
    /// </remarks>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(InstallPromptJsResult))]
    public async ValueTask<InstallPromptResult> Prompt()
    {
        var result = await js.Invoke<InstallPromptJsResult?>("BitButil.installPrompt.prompt");

        return new InstallPromptResult
        {
            Outcome = result?.Outcome switch
            {
                "accepted" => InstallPromptOutcome.Accepted,
                "dismissed" => InstallPromptOutcome.Dismissed,
                _ => InstallPromptOutcome.Unavailable,
            },
            Platform = result?.Platform ?? string.Empty,
        };
    }

    /// <summary>
    /// Invoked from JS when a prompt becomes available. Public + <see cref="JSInvokableAttribute"/>
    /// so it can be dispatched through the per-instance <see cref="DotNetObjectReference{T}"/>.
    /// </summary>
    [JSInvokable(AvailableMethodName)]
    public void InvokeInstallAvailable(Guid id, string[] platforms)
    {
        if (_availableHandlers.TryGetValue(id, out var handler)) handler.Invoke(platforms ?? []);
    }

    /// <summary>
    /// Invoked from JS when the app has been installed. Public + <see cref="JSInvokableAttribute"/>
    /// so it can be dispatched through the per-instance <see cref="DotNetObjectReference{T}"/>.
    /// </summary>
    [JSInvokable(InstalledMethodName)]
    public void InvokeAppInstalled(Guid id)
    {
        if (_installedHandlers.TryGetValue(id, out var handler)) handler.Invoke();
    }

    /// <summary>
    /// Calls <paramref name="handler"/> when an install prompt becomes available, with the platforms
    /// it offers. Fires immediately when one is already in hand, so a component that subscribes after
    /// the browser's event still learns about it.
    /// </summary>
    [DynamicDependency(nameof(InvokeInstallAvailable), typeof(InstallPrompt))]
    public async Task<ButilSubscription> OnAvailable(Action<string[]> handler)
    {
        ArgumentNullException.ThrowIfNull(handler);

        var id = Guid.NewGuid();
        _availableHandlers.TryAdd(id, handler);

        try
        {
            await js.InvokeVoid("BitButil.installPrompt.onAvailable", DotNetRef, id, AvailableMethodName);
        }
        catch
        {
            // Nothing is listening for this id, and the caller gets no subscription to dispose - so
            // the handler it captured has to be dropped here rather than kept alive until disposal.
            _availableHandlers.TryRemove(id, out _);
            try { await js.InvokeVoid("BitButil.installPrompt.offAvailable", id); } catch { /* the registration is what failed */ }
            throw;
        }

        return new ButilSubscription(id, async () =>
        {
            _availableHandlers.TryRemove(id, out _);
            await js.InvokeVoid("BitButil.installPrompt.offAvailable", id);
        });
    }

    /// <summary>
    /// Calls <paramref name="handler"/> when the app has been installed - the moment to hide the
    /// install button, or to thank the user.
    /// </summary>
    [DynamicDependency(nameof(InvokeAppInstalled), typeof(InstallPrompt))]
    public async Task<ButilSubscription> OnInstalled(Action handler)
    {
        ArgumentNullException.ThrowIfNull(handler);

        var id = Guid.NewGuid();
        _installedHandlers.TryAdd(id, handler);

        try
        {
            await js.InvokeVoid("BitButil.installPrompt.onInstalled", DotNetRef, id, InstalledMethodName);
        }
        catch
        {
            _installedHandlers.TryRemove(id, out _);
            try { await js.InvokeVoid("BitButil.installPrompt.offInstalled", id); } catch { /* the registration is what failed */ }
            throw;
        }

        return new ButilSubscription(id, async () =>
        {
            _installedHandlers.TryRemove(id, out _);
            await js.InvokeVoid("BitButil.installPrompt.offInstalled", id);
        });
    }

    /// <summary>Detaches every listener registered through this instance and releases its interop reference.</summary>
    public async ValueTask DisposeAsync()
    {
        try
        {
            var availableIds = _availableHandlers.Keys.ToArray();
            var installedIds = _installedHandlers.Keys.ToArray();
            _availableHandlers.Clear();
            _installedHandlers.Clear();

            foreach (var id in availableIds) await js.InvokeVoid("BitButil.installPrompt.offAvailable", id);
            foreach (var id in installedIds) await js.InvokeVoid("BitButil.installPrompt.offInstalled", id);
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
