namespace TodoTemplate.Shared;

public class WebAppDeploymentTypeDetector
{
    public static WebAppDeploymentTypeDetector Current { get; set; } = new WebAppDeploymentTypeDetector();

    public virtual bool IsSPA()
    {
        return Mode == WebAppDeploymentType.SPA;
    }

    public virtual bool IsPWA()
    {
        return Mode == WebAppDeploymentType.PWA;
    }

    public virtual bool IsStatic()
    {
        return Mode == WebAppDeploymentType.Static;
    }

    public virtual bool IsPreRenderEnabledSPA()
    {
        return Mode == WebAppDeploymentType.PreRenderEnabledSPA;
    }

    public virtual WebAppDeploymentType Mode
    {
        get
        {
#if SPA
            return WebAppDeploymentType.SPA;
#elif PWA
            return WebAppDeploymentType.PWA;
#elif Static
            return WebAppDeploymentType.Static;
#else
            return WebAppDeploymentType.PreRenderEnabledSPA;
#endif
        }
    }
}

public enum WebAppDeploymentType
{
    SPA,
    PWA,
    PreRenderEnabledSPA,
    Static
}
