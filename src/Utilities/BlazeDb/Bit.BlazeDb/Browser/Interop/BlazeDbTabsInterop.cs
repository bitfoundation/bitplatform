using System.Runtime.InteropServices.JavaScript;
using System.Runtime.Versioning;

namespace Bit.BlazeDb.Browser;

/// <summary>Bindings to the bit-blazedb-tabs.js ES module.</summary>
[SupportedOSPlatform("browser")]
internal static partial class BlazeDbTabsInterop
{
    private const string ModuleName = BlazeDbBrowserModules.Tabs;

    public static Task EnsureModuleAsync(string? moduleUrl) => BlazeDbBrowserModules.EnsureAsync(ModuleName, moduleUrl);

    // handle identifies this channel object; several may share one channelName.
    [JSImport("open", ModuleName)]
    public static partial void Open(
        string handle, string channelName, [JSMarshalAs<JSType.Function<JSType.Number>>] Action<double>? onMessage);

    [JSImport("post", ModuleName)]
    public static partial void Post(string handle, double generation);

    [JSImport("close", ModuleName)]
    public static partial void Close(string handle);

    [JSImport("isSupported", ModuleName)]
    public static partial bool IsSupported();
}
