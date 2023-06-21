//-:cnd:noEmit
namespace AdminPanel.Shared.Infra;

/// <summary>
/// https://bitplatform.dev/adminpanel/hosting-models
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
        return Mode == BlazorMode.BlazorHybrid;
    }

    public virtual bool IsBlazorElectron()
    {
        return Mode == BlazorMode.BlazorElectron;
    }

    public virtual BlazorMode Mode
    {
        get
        {
#if BlazorElectron
            return BlazorMode.BlazorElectron;
#elif BlazorWebAssembly                  
            return BlazorMode.BlazorWebAssembly;
#elif BlazorHybrid
            return BlazorMode.BlazorHybrid;
#else
            return BlazorMode.BlazorServer;
#endif
        }
    }
}
