using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Bit.Bheadless;

/// <summary>
/// Registers a global keyboard shortcut while rendered. <c>mod</c> means ⌘ on Apple
/// platforms and Ctrl elsewhere. Renders nothing.
///
/// <code>
/// &lt;BitBhHotkey Keys="mod+k" OnTriggered="() => paletteOpen = true" /&gt;
/// &lt;BitBhHotkey Keys="shift+?" OnTriggered="ShowShortcuts" /&gt;
/// </code>
///
/// <para>Shortcuts without Ctrl / ⌘ are ignored while typing in a text field unless
/// <see cref="AllowInInputs"/> is set. Remember to document shortcuts for your users
/// and avoid overriding common browser / assistive-technology keys.</para>
/// </summary>
public sealed class BitBhHotkey : ComponentBase, IAsyncDisposable
{
    private int _handle;
    private string? _registered;
    private DotNetObjectReference<BitBhHotkey>? _selfRef;
    private bool _syncing;
    private bool _disposed;

    [Inject] private IServiceProvider Services { get; set; } = default!;
    [Inject] private IJSRuntime JS { get; set; } = default!;

    /// <summary>One or more comma-separated shortcuts, e.g. <c>"mod+k"</c> or <c>"mod+k, /"</c>.</summary>
    [Parameter, EditorRequired]
    public string Keys { get; set; } = string.Empty;

    /// <summary>Invoked when the shortcut is pressed.</summary>
    [Parameter]
    public EventCallback OnTriggered { get; set; }

    /// <summary>Whether the shortcut is active. Default: true.</summary>
    [Parameter]
    public bool Enabled { get; set; } = true;

    /// <summary>Also trigger while focus is in a text field.</summary>
    [Parameter]
    public bool AllowInInputs { get; set; }

    private BitBhInterop? _interop;
    private BitBhInterop Interop => _interop ??= BitBhInterop.Resolve(Services, JS);

    protected override Task OnAfterRenderAsync(bool firstRender) => SyncAsync();

    // Renders can arrive while a register is in flight. One loop applies them in order, so a handle is
    // never overwritten (its listener would keep swallowing the shortcut page-wide) and a hotkey disposed
    // meanwhile unregisters what the in-flight call registered.
    private async Task SyncAsync()
    {
        if (_syncing) return;
        _syncing = true;
        try
        {
            while (_disposed is false)
            {
                var key = Enabled && string.IsNullOrWhiteSpace(Keys) is false ? $"{Keys}|{AllowInInputs}" : null;
                if (key == _registered) break;
                if (_handle > 0)
                {
                    _registered = null;
                    await UnregisterAsync();
                    continue;
                }
                if (key is null)
                {
                    _registered = null;
                    break;
                }
                _selfRef ??= DotNetObjectReference.Create(this);
                var combos = Keys.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                _handle = await Interop.HotkeyRegisterAsync(_selfRef, combos, AllowInInputs);
                _registered = key;
            }
        }
        finally
        {
            _syncing = false;
        }
        if (_disposed) await UnregisterAsync();
    }

    private async Task UnregisterAsync()
    {
        if (_handle <= 0) return;
        var handle = _handle;
        _handle = 0;
        await Interop.HotkeyUnregisterAsync(handle);
    }

    /// <summary>Invoked by the JS runtime.</summary>
    [JSInvokable]
    public async Task OnHotkey()
    {
        if (_disposed) return;
        try
        {
            await OnTriggered.InvokeAsync();
        }
        catch (Exception ex)
        {
            // Reported like an exception of a DOM event handler, not lost in the runtime's rejected promise.
            await DispatchExceptionAsync(ex);
        }
    }

    public async ValueTask DisposeAsync()
    {
        _disposed = true;
        // An in-flight register is undone by SyncAsync once it returns.
        if (_syncing is false) await UnregisterAsync();
        _selfRef?.Dispose();
    }
}
