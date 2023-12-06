using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace Bit.Butil;

/// <summary>
/// The History interface allows manipulation of the browser session history, that is the pages visited in the tab or frame that the current page is loaded in.
/// <br/>
/// More info: https://developer.mozilla.org/en-US/docs/Web/API/History
/// </summary>
public class History(IJSRuntime js)
{
    /// <summary>
    /// Returns an Integer representing the number of elements in the session history, including the currently loaded page.
    /// For example, for a page loaded in a new tab this property returns 1.
    /// </summary>
    public async Task<int> GetLength()
    {
        return await js.HistoryGetLength();
    }

    /// <summary>
    /// Gets default scroll restoration behavior on history navigation. This property can be either auto or manual.
    /// </summary>
    public async Task<ScrollRestoration> GetScrollRestoration()
    {
        return await js.HistoryGetScrollRestoration();
    }

    /// <summary>
    /// Allows web applications to explicitly set default scroll restoration behavior on history navigation. This property can be either auto or manual.
    /// </summary>
    public async Task SetScrollRestoration(ScrollRestoration value)
    {
        await js.HistorySetScrollRestoration(value);
    }

    /// <summary>
    /// Returns an any value representing the state at the top of the history stack.
    /// </summary>
    public async Task<object> GetState()
    {
        return await js.HistoryGetState();
    }

    /// <summary>
    /// This asynchronous method goes to the previous page in session history, the same action as when the user clicks the browser's Back button.
    /// Calling this method to go back beyond the first page in the session history has no effect and doesn't raise an exception.
    /// </summary>
    public async Task GoBack()
    {
        await js.HistoryGoBack();
    }

    /// <summary>
    /// This asynchronous method goes to the next page in session history, the same action as when the user clicks the browser's Forward button.
    /// Calling this method to go forward beyond the most recent page in the session history has no effect and doesn't raise an exception.
    /// </summary>
    /// <returns></returns>
    public async Task GoForward()
    {
        await js.HistoryGoForward();
    }

    /// <summary>
    /// Asynchronously loads a page from the session history, identified by its relative location to the current page, for example -1 for the previous page or 1 for the next page.
    /// Calling this method without parameters or a value of 0 reloads the current page.
    /// </summary>
    public async Task Go(int? delta = null)
    {
        await js.HistoryGo(delta);
    }

    /// <summary>
    /// Pushes the given data onto the session history stack with the specified title (and, if provided, URL).
    /// </summary>
    /// <param name="state">The state object can be anything that can be serialized.</param>
    /// <param name="url">The new history entry's URL. The new URL must be of the same origin as the current URL; otherwise PushState throws an exception.</param>
    public async Task PushState(object? state = null, string? url = null)
    {
        await js.HistoryPushState(state, string.Empty, url);
    }


    /// <summary>
    /// Updates the most recent entry on the history stack to have the specified data, title, and, if provided, URL.
    /// </summary>
    /// <param name="state">An object which is associated with the history entry passed to the ReplaceState() method. The state object can be null.</param>
    /// <param name="url">The URL of the history entry. The new URL must be of the same origin as the current URL; otherwise ReplaceState throws an exception.</param>
    public async Task ReplaceState(object? state = null, string? url = null)
    {
        await js.HistoryReplaceState(state, string.Empty, url);
    }
}
