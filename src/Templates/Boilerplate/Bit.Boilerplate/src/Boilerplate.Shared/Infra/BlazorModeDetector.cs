//-:cnd:noEmit
namespace Boilerplate.Shared.Infra;
using OS = OperatingSystem;

/// <summary>
/// https://bitplatform.dev/templates/hosting-models
/// </summary>
public class BlazorModeDetector
{
    public static BlazorModeDetector Current { get; set; } = new BlazorModeDetector();

    public virtual bool IsBlazorServer()
    {
        return Mode == BlazorMode.BlazorServer;
    }

    public virtual bool IsBlazorWebAssembly()
    {
        return Mode == BlazorMode.BlazorWebAssembly;
    }

    public virtual bool IsBlazorHybrid()
    {
        return OS.IsAndroid() || OS.IsIOS() || OS.IsMacOS() || OS.IsMacCatalyst() || OS.IsWindows();
    }

    public virtual BlazorMode Mode
    {
        get
        {
#if BlazorWebAssembly                  
            return BlazorMode.BlazorWebAssembly;
#else
            return BlazorMode.BlazorServer;
#endif
        }
    }
}
