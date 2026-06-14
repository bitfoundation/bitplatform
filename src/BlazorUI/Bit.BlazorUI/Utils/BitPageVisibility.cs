namespace Bit.BlazorUI;

/// <summary>
/// The Page Visibility API provides events you can watch for to know when a document becomes visible or hidden.
/// <br />
/// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Page_Visibility_API"/>
/// </summary>
public class BitPageVisibility(IJSRuntime js) : IDisposable
{
    private bool _isInitialized;
    private DotNetObjectReference<BitPageVisibility>? _dotnetObj;



    /// <summary>
    /// Fires when the content of the document has become visible or hidden.
    /// </summary>
    public event Func<bool, Task>? OnChange;



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



    public void Dispose()
    {
        if (_isInitialized)
        {
            _isInitialized = false;

            // Tears down the global visibilitychange listener and resets the JS-side init guard,
            // so the now-disposed _dotnetObj is no longer referenced and a future instance can re-init.
            _ = js.InvokeVoid("BitBlazorUI.PageVisibility.dispose");
        }

        _dotnetObj?.Dispose();
    }
}
