using Microsoft.JSInterop;

namespace Bit.Butil;

/// <summary>
/// Returns a reference to the session storage object used to store data that may only be accessed by the origin that created it.
/// <br/>
/// More info: <see cref="https://developer.mozilla.org/en-US/docs/Web/API/Window/sessionStorage">https://developer.mozilla.org/en-US/docs/Web/API/Window/sessionStorage</see>
/// </summary>
public class SessionStorage(IJSRuntime js) : ButilStorage(js, "sessionStorage") { }
