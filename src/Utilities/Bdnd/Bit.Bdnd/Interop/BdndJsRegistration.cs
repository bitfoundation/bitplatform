using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Bit.Bdnd;

/// <summary>
/// Owns the JS-side registration of a component's host element (an object exposing
/// <c>update(options)</c> and <c>dispose()</c>) and keeps it in sync across renders: the element
/// is registered again when Blazor re-creates it, and changed options are pushed to JS.
/// </summary>
internal sealed class BdndJsRegistration<TOptions> : IAsyncDisposable where TOptions : class
{
    private readonly Func<ElementReference, TOptions, ValueTask<IJSObjectReference?>> _register;
    private IJSObjectReference? _reference;
    private string? _elementId;
    private TOptions? _options;
    private (ElementReference Element, TOptions Options)? _pending;
    private bool _syncing;
    private bool _disposed;

    public BdndJsRegistration(Func<ElementReference, TOptions, ValueTask<IJSObjectReference?>> register)
        => _register = register;

    /// <summary>The live JS registration, or null until the element has been registered.</summary>
    public IJSObjectReference? Reference => _reference;

    /// <summary>
    /// Registers the element or pushes changed options. Calls made while a previous sync is still
    /// awaiting JS (renders can happen meanwhile) are coalesced into one more pass.
    /// </summary>
    public async Task SyncAsync(ElementReference element, TOptions options)
    {
        _pending = (element, options);
        if (_syncing) return;
        _syncing = true;
        try
        {
            while (_pending is { } next && _disposed is false)
            {
                _pending = null;
                await ApplyAsync(next.Element, next.Options);
            }
        }
        catch (JSDisconnectedException) { }
        // disposed while a call was in flight (e.g. its DotNetObjectReference was released)
        catch (Exception) when (_disposed) { }
        finally
        {
            _syncing = false;
        }
    }

    private async Task ApplyAsync(ElementReference element, TOptions options)
    {
        if (_reference is not null && _elementId == element.Id)
        {
            if (Equals(options, _options) is false)
            {
                await _reference.InvokeVoidAsync("update", options);
                // only once JS has them: after a failed update the next sync sends them again
                _options = options;
            }
            return;
        }

        await ReleaseAsync();
        var reference = await _register(element, options);
        if (_disposed)
        {
            await DisposeReferenceAsync(reference);
            return;
        }
        _reference = reference;
        _elementId = element.Id;
        _options = options;
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;
        await ReleaseAsync();
    }

    private async Task ReleaseAsync()
    {
        var reference = _reference;
        _reference = null;
        _elementId = null;
        _options = null;
        await DisposeReferenceAsync(reference);
    }

    private static async Task DisposeReferenceAsync(IJSObjectReference? reference)
    {
        if (reference is null) return;
        try { await reference.InvokeVoidAsync("dispose"); }
        catch (Exception ex) when (IsExpectedDisposalException(ex)) { }
        try { await reference.DisposeAsync(); }
        catch (Exception ex) when (IsExpectedDisposalException(ex)) { }
    }

    // Disposal is best-effort: the circuit may be gone or the JS object already released.
    private static bool IsExpectedDisposalException(Exception ex) =>
        ex is JSDisconnectedException or JSException or OperationCanceledException or ObjectDisposedException;
}
