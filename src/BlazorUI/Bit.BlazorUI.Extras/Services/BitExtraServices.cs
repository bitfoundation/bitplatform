namespace Bit.BlazorUI;

public class BitExtraServices(IJSRuntime js)
{
    public async Task AddRootCssClasses()
    {
        var cssClasses = new List<string>();

        if (OperatingSystem.IsBrowser())
        {
            cssClasses.Add("bit-browser");
        }
        else if (OperatingSystem.IsWindows())
        {
            cssClasses.Add("bit-windows");
        }
        else if (OperatingSystem.IsMacOS() || OperatingSystem.IsMacCatalyst())
        {
            cssClasses.Add("bit-macos");
        }
        else if (OperatingSystem.IsIOS() && OperatingSystem.IsMacCatalyst() is false)
        {
            cssClasses.Add("bit-ios");
        }
        else if (OperatingSystem.IsAndroid())
        {
            cssClasses.Add("bit-android");
        }

        var cssVariables = new Dictionary<string, string>();

        await js.BitExtrasApplyRootClasses(cssClasses, cssVariables);
    }
}
