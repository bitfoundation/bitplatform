using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Bit.BlazorHeadlessUI;

/// <summary>
/// Thin, typed wrapper around the library's JS runtime (<c>bit-blazorheadlessui.js</c>).
/// The module is imported lazily once per scope (circuit / WebAssembly app) and
/// reused. Every call is resilient to the circuit disconnecting and to running
/// without a browser (prerendering, unit tests), so components can call it from
/// <c>OnAfterRenderAsync</c> and <c>DisposeAsync</c> without guarding.
///
/// <para>
/// Components normally use higher-level helpers such as
/// <see cref="BitBhFloatingController"/>; the methods here are public so design
/// systems built on BlazorHeadlessUI can reuse the same primitives.
/// </para>
/// </summary>
public sealed class BitBhInterop : IAsyncDisposable, IDisposable
{
    private const string ModulePath = "./_content/Bit.BlazorHeadlessUI/bit-blazorheadlessui.js";
    private const DynamicallyAccessedMemberTypes JsonSerialized =
        DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.PublicProperties;

    private readonly IJSRuntime _js;
    private Task<IJSObjectReference>? _module;
    private bool _disposed;

    /// <summary>Creates the interop wrapper. Registered as a scoped service by <c>AddBitBlazorHeadlessUIServices()</c>.</summary>
    public BitBhInterop(IJSRuntime js)
    {
        _js = js;
    }

    private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<IJSRuntime, BitBhInterop> Fallback = new();

    /// <summary>Returns the registered instance, or a shared one per JS runtime when <c>AddBitBlazorHeadlessUIServices()</c> wasn't called.</summary>
    internal static BitBhInterop Resolve(IServiceProvider services, IJSRuntime js)
        => (BitBhInterop?)services.GetService(typeof(BitBhInterop)) ?? Fallback.GetValue(js, static r => new BitBhInterop(r));

    private Task<IJSObjectReference> Module
    {
        get
        {
            // A failed import (a JS interop timeout while a circuit reconnects, a network error) is not kept:
            // every later call would fail on it and the whole runtime would stay dead for the session.
            if (_module is { IsFaulted: true } or { IsCanceled: true }) _module = null;
            return _module ??= _js.InvokeAsync<IJSObjectReference>("import", ModulePath).AsTask();
        }
    }

    private async ValueTask<T> InvokeAsync<[DynamicallyAccessedMembers(JsonSerialized)] T>(string identifier, params object?[] args)
    {
        if (_disposed) return default!;
        try
        {
            var module = await Module;
            if (module is null) return default!;
            return await module.InvokeAsync<T>(identifier, args);
        }
        catch (Exception ex) when (IsBenign(ex))
        {
            return default!;
        }
    }

    private async ValueTask InvokeVoidAsync(string identifier, params object?[] args)
    {
        if (_disposed) return;
        try
        {
            var module = await Module;
            if (module is null) return;
            await module.InvokeVoidAsync(identifier, args);
        }
        catch (Exception ex) when (IsBenign(ex))
        {
        }
    }

    /// <summary>
    /// Exceptions that are expected when the browser is gone or not available
    /// (a disconnected circuit, a cancelled call, a disposed runtime, calling
    /// during static prerendering), plus JS errors: a failed DOM enhancement must
    /// never tear down the circuit, so it is reported to the debugger and ignored.
    /// </summary>
    private static bool IsBenign(Exception ex)
    {
        switch (ex)
        {
            case JSDisconnectedException:
            case OperationCanceledException:
            case ObjectDisposedException:
            case InvalidOperationException:
                return true;
            case JSException js:
                System.Diagnostics.Debug.WriteLine($"[BlazorHeadlessUI] JS error: {js.Message}");
                return true;
            default:
                return false;
        }
    }

    // ── Floating surfaces ────────────────────────────────────────────────────

    /// <summary>Opens a floating surface (popover, menu, dialog …). Returns a handle, or 0 when unavailable.</summary>
    public ValueTask<int> FloatingOpenAsync(object? dotNetRef, string contentId, IDictionary<string, object?> options)
        => InvokeAsync<int>("floating.open", dotNetRef, contentId, options);

    /// <summary>Re-applies positioning / branch ids for an open surface.</summary>
    public ValueTask FloatingUpdateAsync(int handle, IDictionary<string, object?>? options = null)
        => handle <= 0 ? ValueTask.CompletedTask : InvokeVoidAsync("floating.update", handle, options);

    /// <summary>Moves the virtual anchor point of an open surface (context menus).</summary>
    public ValueTask FloatingSetPointAsync(int handle, double x, double y)
        => handle <= 0 ? ValueTask.CompletedTask : InvokeVoidAsync("floating.setPoint", handle, x, y);

    /// <summary>Closes a floating surface, optionally waiting for its CSS exit animation.</summary>
    public ValueTask FloatingCloseAsync(int handle, bool restoreFocus = true, bool waitForExit = true)
        => handle <= 0
            ? ValueTask.CompletedTask
            : InvokeVoidAsync("floating.close", handle, new Dictionary<string, object?>
            {
                ["restoreFocus"] = restoreFocus,
                ["waitForExit"] = waitForExit,
            });

    // ── Anchor positioning ───────────────────────────────────────────────────

    /// <summary>Starts anchoring <paramref name="floatingId"/> to <paramref name="referenceId"/>.</summary>
    public ValueTask<int> AnchorStartAsync(string referenceId, string floatingId, BitBhAnchorOptions options)
        => InvokeAsync<int>("anchor.start", referenceId, floatingId, options.ToJs());

    /// <summary>Stops an anchor handle and clears the inline positioning styles.</summary>
    public ValueTask AnchorStopAsync(int handle)
        => handle <= 0 ? ValueTask.CompletedTask : InvokeVoidAsync("anchor.stop", handle, true);

    /// <summary>Forces an immediate reposition.</summary>
    public ValueTask AnchorUpdateAsync(int handle)
        => handle <= 0 ? ValueTask.CompletedTask : InvokeVoidAsync("anchor.update", handle);

    // ── Focus ────────────────────────────────────────────────────────────────

    /// <summary>Focuses the element with the given id.</summary>
    public ValueTask<bool> FocusAsync(string id, bool preventScroll = true, bool select = false)
        => InvokeAsync<bool>("focus.focus", id, preventScroll, select);

    /// <summary>Focuses the first tabbable element inside the element with the given id.</summary>
    public ValueTask<bool> FocusFirstAsync(string containerId, bool preventScroll = true)
        => InvokeAsync<bool>("focus.focusFirst", containerId, preventScroll);

    /// <summary>Focuses the last tabbable element inside the element with the given id.</summary>
    public ValueTask<bool> FocusLastAsync(string containerId, bool preventScroll = true)
        => InvokeAsync<bool>("focus.focusLast", containerId, preventScroll);

    /// <summary>Moves focus to the next (or previous) tabbable element after the element with the given id.</summary>
    public ValueTask<bool> FocusAdjacentAsync(string id, bool backwards = false)
        => InvokeAsync<bool>("focus.focusAdjacent", id, backwards);

    /// <summary>Returns whether the focused element is inside the element with the given id.</summary>
    public ValueTask<bool> ContainsFocusAsync(string id)
        => InvokeAsync<bool>("focus.contains", id);

    /// <summary>Creates a focus scope (trap / loop / auto-focus / restore). Returns a handle.</summary>
    public ValueTask<int> CreateFocusScopeAsync(string containerId, BitBhFocusScopeOptions options)
        => InvokeAsync<int>("focus.createScope", containerId, options.ToJs());

    /// <summary>Destroys a focus scope and optionally restores focus.</summary>
    public ValueTask DestroyFocusScopeAsync(int handle, bool restoreFocus = true)
        => handle <= 0 ? ValueTask.CompletedTask : InvokeVoidAsync("focus.destroyScope", handle, restoreFocus);

    // ── Modal ────────────────────────────────────────────────────────────────

    /// <summary>Locks document scrolling (stack-aware). Pair with <see cref="UnlockScrollAsync"/>.</summary>
    public ValueTask LockScrollAsync() => InvokeVoidAsync("modal.lockScroll");

    /// <summary>Releases one scroll lock.</summary>
    public ValueTask UnlockScrollAsync() => InvokeVoidAsync("modal.unlockScroll");

    // ── Collections / DOM ────────────────────────────────────────────────────

    /// <summary>Returns <c>[id, text]</c> pairs for the elements tagged <c>data-bit-bh-item="key"</c>, in document order.</summary>
    public ValueTask<string[][]?> CollectionOrderAsync(string key)
        => InvokeAsync<string[][]?>("collection.order", key);

    /// <summary>Scrolls the element into view within its nearest scrolling ancestor.</summary>
    public ValueTask ScrollIntoViewAsync(string id)
        => InvokeVoidAsync("dom.scrollIntoView", id, "nearest");

    /// <summary>Scrolls a virtualized container so the item at <paramref name="index"/> becomes visible.</summary>
    public ValueTask ScrollToIndexAsync(string containerId, int index, float itemSize, string? itemId)
        => InvokeVoidAsync("dom.scrollToIndex", containerId, index, itemSize, itemId);

    /// <summary>Clicks the element (keyboard activation of link-like items).</summary>
    public ValueTask ClickAsync(string id) => InvokeVoidAsync("dom.click", id);

    /// <summary>Returns the bounding client rect of an element.</summary>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(BitBhRect))]
    public ValueTask<BitBhRect?> GetRectAsync(string id) => InvokeAsync<BitBhRect?>("dom.rect", id);

    // ── Portal ───────────────────────────────────────────────────────────────

    /// <summary>Moves the element to the portal root (or <paramref name="target"/>). Returns a handle.</summary>
    public ValueTask<int> PortalMoveAsync(string elementId, string? target = null)
        => InvokeAsync<int>("portal.move", elementId, (object?)target ?? true);

    /// <summary>Moves a portalled element back to its original position.</summary>
    public ValueTask PortalRestoreAsync(int handle)
        => handle <= 0 ? ValueTask.CompletedTask : InvokeVoidAsync("portal.restore", handle);

    // ── Presence / transitions ───────────────────────────────────────────────

    /// <summary>Resolves once the element's running exit animation / transition has finished.</summary>
    public ValueTask WaitForExitAsync(string id) => InvokeVoidAsync("presence.waitForExit", id);

    /// <summary>Runs the class-based enter transition. Returns false when interrupted.</summary>
    public ValueTask<bool> TransitionEnterAsync(string id, BitBhTransitionClasses classes)
        => InvokeAsync<bool>("transition.enter", id, classes.ToJs());

    /// <summary>Runs the class-based leave transition. Returns false when interrupted.</summary>
    public ValueTask<bool> TransitionLeaveAsync(string id, BitBhTransitionClasses classes)
        => InvokeAsync<bool>("transition.leave", id, classes.ToJs());

    // ── Announcer ────────────────────────────────────────────────────────────

    /// <summary>Announces a message through a visually hidden live region.</summary>
    public ValueTask AnnounceAsync(string message, BitBhPoliteness politeness = BitBhPoliteness.Polite, int clearAfterMs = 7000)
        => InvokeVoidAsync("announcer.announce", message, politeness == BitBhPoliteness.Assertive ? "assertive" : "polite", clearAfterMs);

    // ── Hotkeys ──────────────────────────────────────────────────────────────

    internal ValueTask<int> HotkeyRegisterAsync(object dotNetRef, string[] combos, bool allowInInputs)
        => InvokeAsync<int>("hotkeys.register", dotNetRef, combos, new Dictionary<string, object?> { ["allowInInputs"] = allowInInputs });

    internal ValueTask HotkeyUnregisterAsync(int handle)
        => handle <= 0 ? ValueTask.CompletedTask : InvokeVoidAsync("hotkeys.unregister", handle);

    // ── Sortable / slider ────────────────────────────────────────────────────

    internal ValueTask<int> SortableAttachAsync(object dotNetRef, string containerId, IDictionary<string, object?> options)
        => InvokeAsync<int>("sortable.attach", dotNetRef, containerId, options);

    internal ValueTask SortableUpdateAsync(int handle, IDictionary<string, object?> options)
        => handle <= 0 ? ValueTask.CompletedTask : InvokeVoidAsync("sortable.update", handle, options);

    internal ValueTask SortableDetachAsync(int handle)
        => handle <= 0 ? ValueTask.CompletedTask : InvokeVoidAsync("sortable.detach", handle);

    internal ValueTask<int> SliderAttachAsync(object dotNetRef, string rootId, string? trackId, IDictionary<string, object?> options)
        => InvokeAsync<int>("slider.attach", dotNetRef, rootId, trackId, options);

    internal ValueTask SliderUpdateAsync(int handle, IDictionary<string, object?> options)
        => handle <= 0 ? ValueTask.CompletedTask : InvokeVoidAsync("slider.update", handle, options);

    internal ValueTask SliderDetachAsync(int handle)
        => handle <= 0 ? ValueTask.CompletedTask : InvokeVoidAsync("slider.detach", handle);

    /// <inheritdoc />
    public void Dispose()
    {
        // Synchronous disposal (e.g. a test host): release the module without waiting.
        if (_disposed) return;
        _ = DisposeAsync().AsTask();
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;
        if (_module is null) return;
        try
        {
            var module = await _module;
            await module.DisposeAsync();
        }
        catch (Exception ex) when (IsBenign(ex))
        {
        }
    }
}

/// <summary>A DOM rectangle in CSS pixels.</summary>
public sealed record BitBhRect(double X, double Y, double Width, double Height);

/// <summary>Live-region politeness.</summary>
public enum BitBhPoliteness
{
    /// <summary>Announced when the user is idle (<c>aria-live="polite"</c>).</summary>
    Polite,

    /// <summary>Interrupts the user (<c>aria-live="assertive"</c>). Use sparingly.</summary>
    Assertive,
}

/// <summary>Options for <see cref="BitBhInterop.CreateFocusScopeAsync"/>.</summary>
public sealed class BitBhFocusScopeOptions
{
    /// <summary>Keep focus inside the container (Tab / Shift+Tab wrap, focus is pulled back).</summary>
    public bool Trap { get; set; }

    /// <summary>Wrap Tab / Shift+Tab at the edges without pulling focus back.</summary>
    public bool Loop { get; set; }

    /// <summary><c>"first"</c> (default), <c>"container"</c>, <c>"none"</c>, or the id of an element to focus.</summary>
    public string AutoFocus { get; set; } = "first";

    /// <summary>Restore focus to the previously focused element when the scope is destroyed.</summary>
    public bool RestoreFocus { get; set; } = true;

    /// <summary>Optional id of the element to receive focus when the scope is destroyed.</summary>
    public string? ReturnFocusId { get; set; }

    /// <summary>Optional element to focus first (takes precedence over <see cref="AutoFocus"/>).</summary>
    public ElementReference? AutoFocusElement { get; set; }

    /// <summary>Optional element to receive focus when the scope is destroyed (takes precedence over <see cref="ReturnFocusId"/>).</summary>
    public ElementReference? ReturnFocusElement { get; set; }

    internal Dictionary<string, object?> ToJs() => new()
    {
        ["trap"] = Trap,
        ["loop"] = Loop,
        ["autoFocus"] = AutoFocusElement is { Id: not null } af ? af : AutoFocus,
        ["restore"] = RestoreFocus,
        ["returnFocusId"] = ReturnFocusElement is { Id: not null } rf ? rf : ReturnFocusId,
    };
}

/// <summary>CSS class sets for <see cref="BitBhTransition"/>.</summary>
public sealed class BitBhTransitionClasses
{
    /// <summary>Classes applied during the whole enter phase.</summary>
    public string? Enter { get; set; }
    /// <summary>Classes applied on the first frame of enter.</summary>
    public string? EnterFrom { get; set; }
    /// <summary>Classes applied from the second frame of enter until it ends.</summary>
    public string? EnterTo { get; set; }
    /// <summary>Classes applied once the enter phase completed.</summary>
    public string? Entered { get; set; }
    /// <summary>Classes applied during the whole leave phase.</summary>
    public string? Leave { get; set; }
    /// <summary>Classes applied on the first frame of leave.</summary>
    public string? LeaveFrom { get; set; }
    /// <summary>Classes applied from the second frame of leave until it ends.</summary>
    public string? LeaveTo { get; set; }

    internal Dictionary<string, object?> ToJs() => new()
    {
        ["enter"] = Enter,
        ["enterFrom"] = EnterFrom,
        ["enterTo"] = EnterTo,
        ["entered"] = Entered,
        ["leave"] = Leave,
        ["leaveFrom"] = LeaveFrom,
        ["leaveTo"] = LeaveTo,
    };
}
