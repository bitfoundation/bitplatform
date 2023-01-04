using Microsoft.JSInterop;

namespace Bit.Butil;

public static class BitButil
{
    public static void Init(IJSRuntime jsRuntime)
    {
        EventsJsInterop.Init(jsRuntime);
        ConsoleJsInterop.Init(jsRuntime);
    }
}
