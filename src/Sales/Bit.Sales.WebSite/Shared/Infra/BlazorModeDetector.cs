namespace Bit.Sales.WebSite.Shared.Infra;

/// <summary>
/// https://bitplatform.dev/project-templates/todo-template/getting-started#blazor-modes
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
