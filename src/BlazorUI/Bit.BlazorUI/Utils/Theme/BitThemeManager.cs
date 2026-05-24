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

    private DotNetObjectReference<BitThemeJsNotifierReceiver>? _jsNotifierReference;
    private bool _jsNotifierRegistered;

    /// <summary>
    /// Creates a manager without a .NET notifier receiver. <see cref="BitThemeNotifications"/> won't fire from JS until a receiver is provided
    /// (use the other constructor or resolve <see cref="BitThemeManager"/> from DI).
    /// </summary>
    public BitThemeManager(IJSRuntime js)
    {
        _js = js;
        _jsNotifierReceiver = null;
    }

    public BitThemeManager(IJSRuntime js, BitThemeJsNotifierReceiver jsNotifierReceiver)
    {
        _js = js;
        _jsNotifierReceiver = jsNotifierReceiver;
    }

    /// <summary>Returns the active <c>bit-theme</c> name from the document element.</summary>
    public async ValueTask<string> GetCurrentThemeAsync()
    {
        await EnsureJsNotifierRegisteredAsync().ConfigureAwait(false);
        return await _js.BitThemeGetCurrentTheme();
    }

    /// <summary>Sets the <c>bit-theme</c> attribute (use values from <see cref="BitThemePresets"/> or custom names matching your CSS).</summary>
    public async ValueTask<string> SetThemeAsync(string themeName)
    {
        await EnsureJsNotifierRegisteredAsync().ConfigureAwait(false);
        return await _js.BitThemeSetTheme(themeName);
    }

    /// <summary>Toggles between configured light and dark theme names.</summary>
    public async ValueTask<string> ToggleDarkLightAsync()
    {
        await EnsureJsNotifierRegisteredAsync().ConfigureAwait(false);
        return await _js.BitThemeToggleThemeDarkLight();
    }

    /// <summary>Applies <paramref name="bitTheme"/> as CSS custom properties on <paramref name="element"/> (default: body), overriding stylesheet tokens for that subtree.</summary>
    public async ValueTask ApplyBitThemeAsync(BitTheme? bitTheme, ElementReference? element = null)
    {
        await EnsureJsNotifierRegisteredAsync().ConfigureAwait(false);
        await _js.BitThemeApplyBitTheme(BitThemeUtilities.ToCssVariables(bitTheme), element);
    }

    /// <summary>
    /// Removes custom properties previously applied by <see cref="ApplyBitThemeAsync"/> on <paramref name="element"/> (default: document body).
    /// Prefer scoping token overrides under <see cref="BitThemeProvider"/> when possible.
    /// </summary>
    public async ValueTask ClearBitThemeOverridesAsync(ElementReference? element = null)
    {
        await EnsureJsNotifierRegisteredAsync().ConfigureAwait(false);
        await _js.BitThemeClearAppliedBitTheme(element);
    }

    public async ValueTask<bool> IsSystemInDarkMode()
    {
        await EnsureJsNotifierRegisteredAsync().ConfigureAwait(false);
        return await _js.BitThemeIsSystemDark();
    }

    public async ValueTask<string> GetCurrentPersistedThemeAsync()
    {
        await EnsureJsNotifierRegisteredAsync().ConfigureAwait(false);
        return await _js.BitThemeGetCurrentPersistedTheme();
    }

    /// <summary>Ensures the client script can notify <see cref="BitThemeNotifications"/>; safe to call multiple times.</summary>
    public async ValueTask EnsureThemeNotificationsRegisteredAsync()
    {
        await EnsureJsNotifierRegisteredAsync().ConfigureAwait(false);
    }

    private async ValueTask EnsureJsNotifierRegisteredAsync()
    {
        if (_jsNotifierRegistered) return;
        if (_jsNotifierReceiver is null) return; // no-op when constructed without a receiver
        if (_js.IsRuntimeInvalid()) return; // e.g. prerendering / disconnected circuit; retry on next call.

        _jsNotifierReference ??= DotNetObjectReference.Create(_jsNotifierReceiver);
        await _js.BitThemeRegisterDotNetNotifier(_jsNotifierReference).ConfigureAwait(false);
        _jsNotifierRegistered = true;
    }

    public async ValueTask DisposeAsync()
    {
        if (_jsNotifierReference is null) return;

        try
        {
            await _js.BitThemeUnregisterDotNetNotifier().ConfigureAwait(false);
        }
        catch (JSDisconnectedException)
        {
            // Circuit gone
        }

        _jsNotifierReference.Dispose();
        _jsNotifierReference = null;
        _jsNotifierRegistered = false;
    }
}
