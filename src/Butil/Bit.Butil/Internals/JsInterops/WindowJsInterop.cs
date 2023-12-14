using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace Bit.Butil;

internal static class WindowJsInterop
{
    internal static async Task WindowAddBeforeUnload(this IJSRuntime js)
        => await js.InvokeVoidAsync("BitButil.window.addBeforeUnload");

    internal static async Task WindowRemoveBeforeUnload(this IJSRuntime js)
        => await js.InvokeVoidAsync("BitButil.window.removeBeforeUnload");
}
