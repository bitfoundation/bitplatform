//-:cnd:noEmit
namespace Boilerplate.Shared.Infra;

public static class BuildConfigurationDetector
{
    public static bool IsDebug()
    {
        return Current == BuildConfiguration.Debug;
    }


    public static bool IsRelease()
    {
        return Current == BuildConfiguration.Release;
    }


    public static BuildConfiguration Current
    {
        get
        {
#if DEBUG
            return BuildConfiguration.Debug;
#else
            return BuildConfigurationMode.Release;
#endif

        }
    }
}
