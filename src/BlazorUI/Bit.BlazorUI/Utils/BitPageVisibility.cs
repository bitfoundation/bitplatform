namespace Bit.BlazorUI;

/// <summary>
/// The Page Visibility API provides events you can watch for to know when a document becomes visible or hidden.
/// <br />
/// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Page_Visibility_API"/>
/// </summary>
public class BitPageVisibility(IJSRuntime js) : IDisposable, IAsyncDisposable
{
    private bool _isInitialized;
    private DotNetObjectReference<BitPageVisibility>? _dotnetObj;



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
    /// Initializes the js api of the page visibility utility.
    /// </summary>
    public async Task Init()
    {
        if (_isInitialized) return;

        _isInitialized = true;

        _dotnetObj = DotNetObjectReference.Create(this);

        await js.InvokeVoid("BitBlazorUI.PageVisibility.init", _dotnetObj);
    }



    [JSInvokable("VisibilityChanged")]
    public async Task _VisibilityChanged(bool hidden)
    {
        var onChange = OnChange;
        if (onChange is not null)
        {
            await onChange(hidden);
        }
    }

    [JSInvokable("WindowFocusChanged")]
    public async Task _WindowFocusChanged(bool blurred)
    {
        var onWindowFocusChange = OnWindowFocusChange;
        if (onWindowFocusChange is not null)
        {
            await onWindowFocusChange(blurred);
        }
    }



    /// <summary>
    /// The synchronous counterpart of <see cref="DisposeAsync"/>, for a scope that is torn down with
    /// <c>Dispose</c> rather than <c>DisposeAsync</c> (the DI container throws for a scoped service that
    /// offers only the asynchronous one). The JS teardown can't be awaited here, so it is started and left
    /// to finish on its own.
    /// </summary>
    public void Dispose()
    {
        _ = DisposeCoreAsync();

        GC.SuppressFinalize(this);
    }

    public async ValueTask DisposeAsync()
    {
        await DisposeCoreAsync();

        GC.SuppressFinalize(this);
    }

    private async ValueTask DisposeCoreAsync()
    {
        if (_isInitialized is false) return;

        _isInitialized = false;

        try
        {
            // Removes the global visibilitychange/blur/focus listeners and resets the JS-side init guard,
            // so a later instance can init again.
            await js.InvokeVoid("BitBlazorUI.PageVisibility.dispose");
        }
        catch
        {
            // Disposal never throws: it runs from the DI scope's teardown, where an exception would abort
            // the disposal of everything else in the scope, and every failure here means the same thing -
            // the listeners can no longer be reached (the circuit is gone, the runtime torn down, the call
            // timed out, or the teardown itself failed in the browser).
        }
        finally
        {
            _dotnetObj?.Dispose();
        }
    }
}
