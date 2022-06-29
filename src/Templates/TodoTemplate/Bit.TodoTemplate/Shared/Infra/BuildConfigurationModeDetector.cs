//-:cnd:noEmit
namespace TodoTemplate.Shared.Infra;

public class BuildConfigurationModeDetector
{
    public static BuildConfigurationModeDetector Current { get; set; } = new BuildConfigurationModeDetector();

    public virtual bool IsDebug()
    {
        return Mode == BuildConfigurationMode.Debug;
    }


    public virtual bool Release()
    {
        return Mode == BuildConfigurationMode.Release;
    }


    public virtual BuildConfigurationMode Mode
    {
        get
        {
#if DEBUG
            return BuildConfigurationMode.Debug;
#else
            return BuildConfigurationMode.Release;
#endif

        }
    }
}
