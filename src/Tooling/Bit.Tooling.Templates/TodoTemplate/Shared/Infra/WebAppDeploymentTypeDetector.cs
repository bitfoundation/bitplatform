namespace TodoTemplate.Shared.Infra;

public class WebAppDeploymentTypeDetector
{
    public static WebAppDeploymentTypeDetector Current { get; set; } = new WebAppDeploymentTypeDetector();

    public virtual bool IsDefault()
    {
        return Mode == WebAppDeploymentType.Default;
    }

    public virtual bool IsPwa()
    {
        return Mode == WebAppDeploymentType.Pwa;
    }

    public virtual bool IsStatic()
    {
        return Mode == WebAppDeploymentType.Static;
    }

    public virtual bool IsSsr()
    {
        return Mode == WebAppDeploymentType.Ssr;
    }

    public virtual WebAppDeploymentType Mode
    {
        get
        {
#if SPA
            return WebAppDeploymentType.Default;
#elif PWA
            return WebAppDeploymentType.Pwa;
#elif Static
            return WebAppDeploymentType.Static;
#else
            return WebAppDeploymentType.Ssr;
#endif
        }
    }
}
