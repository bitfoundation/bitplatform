using Microsoft.JSInterop;

namespace Bit.Butil;

/// <summary>
/// Returns a reference to the local storage object used to store data that may only be accessed by the origin that created it.
/// <br/>
/// More info: <see cref="https://developer.mozilla.org/en-US/docs/Web/API/Window/localStorage">https://developer.mozilla.org/en-US/docs/Web/API/Window/localStorage</see>
/// </summary>
public class LocalStorage(IJSRuntime js) : ButilStorage(js, "localStorage") { }
