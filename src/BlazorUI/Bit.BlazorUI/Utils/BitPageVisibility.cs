namespace Bit.BlazorUI;

/// <summary>
/// The Page Visibility API provides events you can watch for to know when a document becomes visible or hidden.
/// <br />
/// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Page_Visibility_API"/>
/// </summary>
public class BitPageVisibility(IJSRuntime js)
{
    private Task? _initTask;
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
    public async Task Init()
    {
        if (_initTask is not null)
        {
            // A later caller only waits for the state the first one asked for; a failure is the first caller's.
            try { await _initTask; } catch { }
            return;
        }

        _initTask = InitCore();

        await _initTask;
    }

    private async Task InitCore()
    {
        _dotnetObj = DotNetObjectReference.Create(this);

        var state = await js.Invoke<BitPageVisibilityState?>("BitBlazorUI.PageVisibility.init", _dotnetObj);
        if (state is null) return;

        IsHidden = state.Hidden;
        IsWindowBlurred = state.Blurred;
    }



    [JSInvokable("VisibilityChanged")]
    public async Task _VisibilityChanged(bool hidden)
    {
        IsHidden = hidden;

        var onChange = OnChange;
        if (onChange is not null)
        {
            await onChange(hidden);
        }
    }

    [JSInvokable("WindowFocusChanged")]
    public async Task _WindowFocusChanged(bool blurred)
    {
        IsWindowBlurred = blurred;

        var onWindowFocusChange = OnWindowFocusChange;
        if (onWindowFocusChange is not null)
        {
            await onWindowFocusChange(blurred);
        }
    }



    private sealed class BitPageVisibilityState
    {
        public bool Hidden { get; set; }
        public bool Blurred { get; set; }
    }
}
