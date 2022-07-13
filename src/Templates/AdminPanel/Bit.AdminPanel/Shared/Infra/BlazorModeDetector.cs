//-:cnd:noEmit
namespace AdminPanel.Shared.Infra;

/// <summary>
/// https://bitplatform.dev/project-templates/adminpanel-template/getting-started#blazor-modes
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

    public virtual BlazorMode Mode
    {
        get
        {
#if BlazorWebAssembly
            return BlazorMode.BlazorWebAssembly;
#elif BlazorHybrid
                return BlazorMode.BlazorHybrid;
#else
            return BlazorMode.BlazorServer;
#endif
        }
    }
}
