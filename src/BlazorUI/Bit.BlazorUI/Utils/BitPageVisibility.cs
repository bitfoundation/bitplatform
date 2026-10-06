namespace Bit.BlazorUI;

/// <summary>
/// The Page Visibility API provides events you can watch for to know when a document becomes visible or hidden.
/// <br />
/// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Page_Visibility_API"/>
/// </summary>
public class BitPageVisibility(IJSRuntime js) : IDisposable, IAsyncDisposable
{
    // The page is shared by every .NET side that listens to it (a replaced circuit, the two runtimes of the Auto
    // mode), so each instance registers itself under an id of its own and takes only itself back out.
    private readonly string _id = Guid.NewGuid().ToString("N");

    private Task? _initTask;
    private bool _isHiddenReported;
    private bool _isWindowBlurredReported;
    private DotNetObjectReference<BitPageVisibility>? _dotnetObj;



    /// <summary>
    /// Whether the content of the document is hidden, as last reported by the browser.
    /// </summary>
    public bool IsHidden { get; private set; }

    /// <summary>
    /// Whether the window has lost the focus, as last reported by the browser.
    /// </summary>
    public bool IsWindowBlurred { get; private set; }

    /// <summary>
    /// Fires when the content of the document has become visible or hidden.
    /// </summary>
    public event Func<bool, Task>? OnChange;

    /// <summary>
    /// Fires with <c>true</c> when the window has lost the focus and with <c>false</c> when it has got it back.
    /// </summary>
    /// <remarks>
    /// A window that is covered by another one, or whose focus went to the dev tools or to an iframe, is not
    /// hidden - <see cref="OnChange"/> never fires for it. This is the event to watch to hold something back
    /// while the page is not the one being worked in.
    /// </remarks>
    public event Func<bool, Task>? OnWindowFocusChange;



    /// <summary>
    /// Initializes the js api of the page visibility utility, and reads the state the page is already in into
    /// <see cref="IsHidden"/> and <see cref="IsWindowBlurred"/>, since no event is coming for it.
    /// </summary>
    /// <returns>
    /// <c>true</c> once the browser is reporting to this instance; <c>false</c> when it could not be asked - the
    /// script is not loaded, the circuit is gone or the call timed out - in which case a later call asks again.
    /// </returns>
    /// <remarks>
    /// An interop failure is answered rather than thrown, so a caller has nothing to catch: without the script the
    /// page is simply taken to be visible and focused, which is no reason to fail a render.
    /// </remarks>
    public async Task<bool> Init()
    {
        // Every caller waits for the one call already on its way rather than asking the page again.
        var initTask = _initTask ??= InitCore();

        try
        {
            await initTask;

            return true;
        }
        catch (Exception ex) when (ex is JSDisconnectedException or JSException or OperationCanceledException)
        {
            // A failed call wired nothing up, so the next caller is let to try again - unless one already has.
            if (ReferenceEquals(_initTask, initTask))
            {
                _initTask = null;
            }

            return false;
        }
    }

    private async Task InitCore()
    {
        _dotnetObj ??= DotNetObjectReference.Create(this);

        var state = await js.Invoke<BitPageVisibilityState?>("BitBlazorUI.PageVisibility.init", _id, _dotnetObj);
        if (state is null) return;

        // The listeners are live from the moment the script registered them, so a change can be reported before
        // this continuation runs; the snapshot is older than such a report and does not overwrite it.
        if (_isHiddenReported is false) IsHidden = state.Hidden;
        if (_isWindowBlurredReported is false) IsWindowBlurred = state.Blurred;
    }



    [JSInvokable("VisibilityChanged")]
    public async Task _VisibilityChanged(bool hidden)
    {
        IsHidden = hidden;
        _isHiddenReported = true;

        await RaiseAsync(OnChange, hidden);
    }

    [JSInvokable("WindowFocusChanged")]
    public async Task _WindowFocusChanged(bool blurred)
    {
        IsWindowBlurred = blurred;
        _isWindowBlurredReported = true;

        await RaiseAsync(OnWindowFocusChange, blurred);
    }



    public void Dispose()
    {
        _dotnetObj?.Dispose();
        _dotnetObj = null;

        GC.SuppressFinalize(this);
    }

    public async ValueTask DisposeAsync()
    {
        if (_dotnetObj is not null)
        {
            try
            {
                await js.InvokeVoid("BitBlazorUI.PageVisibility.dispose", _id);
            }
            catch (JSDisconnectedException) { } // the circuit is gone; the script drops the listener the next time it fails to answer
            catch (JSException) { }
            catch (OperationCanceledException) { }
        }

        Dispose();
    }



    // Every subscriber is handed the change on its own: a multicast delegate would stop at the first one that
    // throws, and leave the exceptions of all but the last one unobserved. A failing subscriber is not the page's
    // failure either, and letting it reach the script would have the page stop reporting to this instance.
    private static async Task RaiseAsync(Func<bool, Task>? handlers, bool value)
    {
        if (handlers is null) return;

        foreach (var handler in handlers.GetInvocationList().Cast<Func<bool, Task>>())
        {
            try
            {
                await handler(value);
            }
            catch { }
        }
    }



    private sealed class BitPageVisibilityState
    {
        public bool Hidden { get; set; }
        public bool Blurred { get; set; }
    }
}
