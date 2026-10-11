using System.Diagnostics.CodeAnalysis;

namespace Bit.BlazorUI;

/// <summary>
/// The browser's side of a component whose .NET keydown handler on its root acts on an Escape that root claims
/// (data-bit-esc, see Utils.claimEscape): the handler hears every key pressed inside the root, the ones a part of it
/// acted on and claimed first included, so the browser tells it which press is the root's own (Utils.watchEscapeClaim)
/// through the owner's <c>OnEscapeVerdict</c> callback, which hands the answer to <see cref="SetVerdict"/>.
/// The verdict is read once (<see cref="TakeForeign"/>), so it never outlives the press it was given for.
/// </summary>
internal sealed class BitEscapeClaimWatch<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T>
    : IAsyncDisposable where T : class
{
    private readonly IJSRuntime _js;
    private readonly T _owner;
    private readonly string _id = $"bit-esc-{Guid.NewGuid():N}";

    private DotNetObjectReference<T>? _dotnetObj;
    private bool _foreign;
    private bool _disposed;

    public BitEscapeClaimWatch(IJSRuntime js, T owner)
    {
        _js = js;
        _owner = owner;
    }

    /// <summary>What the browser answered for the Escape the owner's key handler is about to hear.</summary>
    public void SetVerdict(bool foreign) => _foreign = foreign;

    /// <summary>Whether the press the key handler is hearing was claimed by a part of the root first.</summary>
    public bool TakeForeign()
    {
        var foreign = _foreign;
        _foreign = false;
        return foreign;
    }

    /// <summary>Starts listening on the root. One disposed while it was being set up is never left registered.</summary>
    public async Task WatchAsync(ElementReference root)
    {
        if (_disposed) return;

        _dotnetObj ??= DotNetObjectReference.Create(_owner);

        try
        {
            await _js.BitUtilsWatchEscapeClaim(_id, root, _dotnetObj);
        }
        catch (JSDisconnectedException) { } // the circuit is gone, and the listener with it
        catch (OperationCanceledException) { }
        catch (JSException) { } // without the listener every Escape is taken for the root's own

        // Disposed while the browser was registering it: the registration DisposeAsync took back may have landed
        // after it, so it is taken back once more.
        if (_disposed) await UnwatchAsync();
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;

        _disposed = true;

        if (_dotnetObj is null) return;

        await UnwatchAsync();

        _dotnetObj.Dispose();
    }

    private async Task UnwatchAsync()
    {
        try
        {
            await _js.BitUtilsUnwatchEscapeClaim(_id);
        }
        catch (JSDisconnectedException) { } // the circuit is gone, and the listener with it
        catch (OperationCanceledException) { }
        catch (JSException) { }
    }
}
