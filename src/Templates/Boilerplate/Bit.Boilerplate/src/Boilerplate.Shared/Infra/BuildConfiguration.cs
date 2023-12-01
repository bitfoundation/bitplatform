//-:cnd:noEmit
namespace Boilerplate.Shared.Infra;

public static class BuildConfiguration
{
    public static bool IsDebug()
    {
#if DEBUG
        return true;
#else
        return false;
#endif
    }


    public static bool IsRelease()
    {
#if DEBUG
        return false;
#else
        return true;
#endif
    }
}
