using Microsoft.Extensions.Logging;

namespace Bit.BlazorUI;

/// <summary>
/// Bridges Blazor to the client <c>BitTheme</c> script: preset names on the <c>bit-theme</c> attribute vs inline CSS variables from <see cref="BitTheme"/>.
/// <see cref="SetThemeAsync"/> selects packaged Fluent CSS for <c>:root[bit-theme]</c>.
/// <see cref="ApplyBitThemeAsync"/> sets <c>--bit-*</c> variables on the target element (default <c>document.body</c>), overriding stylesheet defaults for that subtree.
/// Nested <see cref="BitThemeProvider"/> scopes overrides to its root element.
/// </summary>
public class BitThemeManager : IAsyncDisposable
{
    private readonly IJSRuntime _js;
    private readonly BitThemeJsNotifierReceiver? _jsNotifierReceiver;
    private readonly ILogger<BitThemeManager>? _logger;
    private readonly SemaphoreSlim _jsNotifierRegistrationLock = new(1, 1);

    private DotNetObjectReference<BitThemeJsNotifierReceiver>? _jsNotifierReference;
    private volatile bool _jsNotifierRegistered;
    private bool _disposed;

    /// <summary>
    /// Creates a manager without a .NET notifier receiver. <see cref="BitThemeNotifications"/> won't fire from JS until a receiver is provided
    /// (resolve <see cref="BitThemeManager"/> from DI to get a fully-wired instance).
    /// </summary>
    public BitThemeManager(IJSRuntime js)
    {
        ArgumentNullException.ThrowIfNull(js);
        _js = js;
    }

    // Internal ctor: BitThemeJsNotifierReceiver is internal, so this signature can't be public.
    // DI wires this via a factory in IBitBlazorUIServiceCollectionExtensions.
    internal BitThemeManager(IJSRuntime js, BitThemeJsNotifierReceiver jsNotifierReceiver, ILoggerFactory? loggerFactory)
    {
        ArgumentNullException.ThrowIfNull(js);
        ArgumentNullException.ThrowIfNull(jsNotifierReceiver);

        _js = js;
        _jsNotifierReceiver = jsNotifierReceiver;
        _logger = loggerFactory?.CreateLogger<BitThemeManager>();
    }

    /// <summary>Returns the active <c>bit-theme</c> name from the document element.</summary>
    /// <exception cref="ObjectDisposedException">The manager has been disposed.</exception>
    public async ValueTask<string?> GetCurrentThemeAsync()
    {
        await EnsureJsNotifierRegisteredAsync().ConfigureAwait(false);
        return await _js.BitThemeGetCurrentTheme();
    }

    /// <summary>
    /// Sets the <c>bit-theme</c> attribute (use values from <see cref="BitThemePresets"/> or custom names matching your CSS).
    /// The name is normalized to the canonical form (trimmed and lower-cased invariantly) before it
    /// is written, so it matches the lowercase <c>:root[bit-theme=…]</c> selectors in the packaged
    /// CSS and stays consistent with <see cref="BitThemeName"/>. A blank name is a no-op.
    /// Returns <see langword="null"/> when JS interop is unavailable (e.g. prerendering / disconnected circuit).
    /// </summary>
    /// <remarks>
    /// This string overload only trims and lower-cases the value; unlike <see cref="BitThemeName.Custom(string)"/>
    /// it intentionally does NOT enforce the strict <c>[a-z0-9-]</c> / ≤64-char token rule, so arbitrary
    /// theme names that match your own CSS selectors are accepted. This is safe because the value is
    /// applied via the client script's <c>setAttribute</c> (an attribute-value sink, not HTML), so it
    /// cannot inject markup. If you also persist theme names for server-side first paint via
    /// <see cref="BitThemeSsr.BuildRootThemeAttributes(string?, string?)"/>, prefer names that satisfy the
    /// stricter token rule (or use <see cref="BitThemeName.Custom(string)"/>): the SSR path rejects
    /// non-conforming tokens and would treat such a persisted preference as "missing".
    /// </remarks>
    /// <exception cref="ObjectDisposedException">The manager has been disposed.</exception>
    public async ValueTask<string?> SetThemeAsync(string themeName)
    {
        await EnsureJsNotifierRegisteredAsync().ConfigureAwait(false);
        return await _js.BitThemeSetTheme(NormalizeThemeName(themeName));
    }

    /// <summary>
    /// Sets the <c>bit-theme</c> attribute from a strongly-typed <see cref="BitThemeName"/>
    /// (already normalized). Equivalent to passing <see cref="BitThemeName.Value"/> to
    /// <see cref="SetThemeAsync(string)"/>.
    /// </summary>
    public ValueTask<string?> SetThemeAsync(BitThemeName themeName)
    {
        return SetThemeAsync(themeName.Value);
    }

    /// <summary>
    /// Normalizes a theme name to the canonical form written onto the <c>bit-theme</c> attribute:
    /// trimmed and lower-cased with the invariant culture, matching <see cref="BitThemeName"/> and
    /// the lowercase CSS selectors. Blank input becomes <see cref="string.Empty"/>, which the client
    /// script treats as a no-op (the current theme is kept).
    /// </summary>
    private static string NormalizeThemeName(string? themeName)
    {
        return string.IsNullOrWhiteSpace(themeName)
            ? string.Empty
            : themeName.Trim().ToLowerInvariant();
    }

    /// <summary>
    /// Toggles between configured light and dark theme names.
    /// Returns <see langword="null"/> when JS interop is unavailable (e.g. prerendering / disconnected circuit).
    /// </summary>
    /// <exception cref="ObjectDisposedException">The manager has been disposed.</exception>
    public async ValueTask<string?> ToggleDarkLightAsync()
    {
        await EnsureJsNotifierRegisteredAsync().ConfigureAwait(false);
        return await _js.BitThemeToggleThemeDarkLight();
    }

    /// <summary>Applies <paramref name="bitTheme"/> as CSS custom properties on <paramref name="element"/> (default: body), overriding stylesheet tokens for that subtree.</summary>
    /// <exception cref="ObjectDisposedException">The manager has been disposed.</exception>
    public async ValueTask ApplyBitThemeAsync(BitTheme? bitTheme, ElementReference? element = null)
    {
        await EnsureJsNotifierRegisteredAsync().ConfigureAwait(false);
        await _js.BitThemeApplyBitTheme(BitThemeUtilities.ToCssVariables(bitTheme), element);
    }

    /// <summary>
    /// Removes custom properties previously applied by <see cref="ApplyBitThemeAsync"/> on <paramref name="element"/> (default: document body).
    /// Prefer scoping token overrides under <see cref="BitThemeProvider"/> when possible.
    /// </summary>
    /// <exception cref="ObjectDisposedException">The manager has been disposed.</exception>
    public async ValueTask ClearBitThemeOverridesAsync(ElementReference? element = null)
    {
        await EnsureJsNotifierRegisteredAsync().ConfigureAwait(false);
        await _js.BitThemeClearAppliedBitTheme(element);
    }

    /// <summary>
    /// Returns true when the OS / browser reports a dark <c>prefers-color-scheme</c>. Returns
    /// <see langword="false"/> when JS interop is unavailable (e.g. prerendering / disconnected circuit).
    /// </summary>
    /// <exception cref="ObjectDisposedException">The manager has been disposed.</exception>
    public async ValueTask<bool> IsSystemInDarkModeAsync()
    {
        await EnsureJsNotifierRegisteredAsync().ConfigureAwait(false);
        return await _js.BitThemeIsSystemDark();
    }

    /// <summary>
    /// Returns the persisted theme <em>preference</em> (the abstract stored key such as
    /// <c>system</c>, <c>dark</c>, or <c>fluent-light</c>) read by the client script from
    /// <c>localStorage</c> (or the preference cookie when cookie persistence is enabled). This can
    /// legitimately differ from <see cref="GetCurrentThemeAsync"/>: while following the OS, the
    /// stored preference stays <c>system</c> even though the applied <c>bit-theme</c> attribute
    /// resolves to a concrete light/dark name. Returns <see langword="null"/> when persistence is
    /// off, nothing has been stored yet, or JS interop is unavailable (e.g. prerendering /
    /// disconnected circuit).
    /// </summary>
    /// <exception cref="ObjectDisposedException">The manager has been disposed.</exception>
    public async ValueTask<string?> GetCurrentPersistedThemeAsync()
    {
        await EnsureJsNotifierRegisteredAsync().ConfigureAwait(false);
        return await _js.BitThemeGetCurrentPersistedTheme();
    }

    /// <summary>
    /// Eagerly wires up the JS-to-.NET notifier so <see cref="BitThemeNotifications.ThemeChanged"/>
    /// can fire from the client script.
    /// </summary>
    /// <remarks>
    /// Every other public method on <see cref="BitThemeManager"/> already calls the same
    /// registration step internally, so this is only useful when an app wants to subscribe to
    /// <see cref="BitThemeNotifications"/> at startup without yet calling any other manager
    /// method. Safe to call multiple times - registration is idempotent and serialized by an
    /// internal semaphore.
    /// </remarks>
    /// <exception cref="ObjectDisposedException">The manager has been disposed.</exception>
    public async ValueTask EnsureThemeNotificationsRegisteredAsync()
    {
        await EnsureJsNotifierRegisteredAsync().ConfigureAwait(false);
    }

    private async ValueTask EnsureJsNotifierRegisteredAsync()
    {
        // Fail fast so public operations (which all funnel through here) do not proceed into JS
        // interop after the manager has been disposed - checked before the receiver guard so
        // receiver-less managers throw after DisposeAsync() too, instead of silently continuing.
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (_jsNotifierRegistered) return;
        if (_jsNotifierReceiver is null) return; // no-op when constructed without a receiver
        if (_js.IsRuntimeInvalid()) return; // e.g. prerendering / disconnected circuit; retry on next call.

        // Serialize concurrent callers (e.g. Task.WhenAll over multiple manager methods) so only one
        // BitThemeRegisterDotNetNotifier invocation is in flight; subsequent callers wait on the semaphore
        // and then short-circuit on the _jsNotifierRegistered fast-path.
        await _jsNotifierRegistrationLock.WaitAsync().ConfigureAwait(false);
        try
        {
            if (_jsNotifierRegistered) return;
            ObjectDisposedException.ThrowIf(_disposed, this); // disposal raced ahead while we were waiting on the semaphore.
            if (_js.IsRuntimeInvalid()) return;

            _jsNotifierReference ??= DotNetObjectReference.Create(_jsNotifierReceiver!);

            try
            {
                await _js.BitThemeRegisterDotNetNotifier(_jsNotifierReference).ConfigureAwait(false);
            }
            catch (JSDisconnectedException)
            {
                // The circuit dropped after the IsRuntimeInvalid check above. Swallow it so the public
                // methods keep their null-on-disconnect behavior, and leave the flag false to retry later.
                return;
            }
            catch (OperationCanceledException)
            {
                // Teardown raced the interop call (covers TaskCanceledException, e.g. the JS interop
                // timeout). Same contract as above: swallow, and leave the flag false to retry later.
                return;
            }

            // InvokeVoid silently no-ops when the runtime is invalid; if it became invalid between the
            // initial check and the awaited call, leave the flag false so a later call can retry.
            if (_js.IsRuntimeInvalid()) return;

            _jsNotifierRegistered = true;
        }
        finally
        {
            _jsNotifierRegistrationLock.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        // Acquire the registration lock so any in-flight EnsureJsNotifierRegisteredAsync completes
        // before we tear down state; new callers will short-circuit on _disposed.
        // We intentionally do NOT Dispose the SemaphoreSlim here: a caller that already passed the
        // early _disposed check (or was queued on WaitAsync) could otherwise observe ObjectDisposedException
        // on its WaitAsync/Release. SemaphoreSlim only holds an unmanaged handle if AvailableWaitHandle
        // is used (it isn't here), so leaving it for the GC is safe.
        await _jsNotifierRegistrationLock.WaitAsync().ConfigureAwait(false);
        try
        {
            if (_disposed) return;
            _disposed = true;

            if (_jsNotifierReference is null) return;

            try
            {
                await _js.BitThemeUnregisterDotNetNotifier().ConfigureAwait(false);
            }
            catch (JSDisconnectedException) { } // circuit gone - nothing to unregister
            catch (OperationCanceledException) { } // teardown raced the interop call (covers TaskCanceledException)
            catch (JSException ex)
            {
                // Missing JS module (e.g. after a page refresh or navigation) - safe to ignore at
                // teardown. Route through ILogger when a factory was provided so hosts get their
                // configured pipeline (Console.WriteLine bypasses logging filters and shows up
                // raw in production terminals).
                _logger?.LogDebug(ex, "BitBlazorUI.Theme.unregisterDotNetNotifier failed during dispose; ignoring.");
            }
            finally
            {
                // Always release the .NET reference and reset state, even if the JS call threw an
                // exception type we don't catch above, so we never leak the DotNetObjectReference or
                // leave _jsNotifierRegistered in an inconsistent state.
                _jsNotifierReference.Dispose();
                _jsNotifierReference = null;
                _jsNotifierRegistered = false;
            }
        }
        finally
        {
            _jsNotifierRegistrationLock.Release();
        }
    }
}
