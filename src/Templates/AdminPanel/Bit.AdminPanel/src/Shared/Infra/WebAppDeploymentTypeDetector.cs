//-:cnd:noEmit
namespace AdminPanel.Shared.Infra;

/// <summary>
/// https://bitplatform.dev/adminpanel/hosting-models
/// </summary>
public class WebAppDeploymentTypeDetector
{
    public static WebAppDeploymentTypeDetector Current { get; set; } = new WebAppDeploymentTypeDetector();

    public virtual bool IsPrerenderedOnly()
    {
        return Mode == WebAppDeploymentType.PrerenderedOnly;
    }

    public virtual bool IsSpa()
    {
        return Mode == WebAppDeploymentType.Spa;
    }

    public virtual bool IsPwa()
    {
        return Mode == WebAppDeploymentType.Pwa;
    }

    public virtual bool IsSpaPrerendered()
    {
        return Mode == WebAppDeploymentType.SpaPrerendered;
    }

    public virtual bool IsPwaPrerendered()
    {
        return Mode == WebAppDeploymentType.PwaPrerendered;
    }

    public virtual WebAppDeploymentType Mode
    {
        get
        {
#if PrerenderedOnly
            return WebAppDeploymentType.PrerenderedOnly;
#elif Spa
            return WebAppDeploymentType.Spa;
#elif Pwa
            return WebAppDeploymentType.Pwa;
#elif SpaPrerendered
            return WebAppDeploymentType.SpaPrerendered;
#elif PwaPrerendered
            return WebAppDeploymentType.PwaPrerendered;
#endif
        }
    }
