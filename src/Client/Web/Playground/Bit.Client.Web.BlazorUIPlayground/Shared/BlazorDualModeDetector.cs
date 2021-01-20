namespace Bit.Client.Web.BlazorUIPlayground.Shared
{
    public class BlazorDualModeDetector
    {
        public static BlazorDualModeDetector Current { get; set; } = new BlazorDualModeDetector();

        public virtual bool IsRunningServerSideBlazor()
        {
#if BlazorClient
            return false;
#else
            return true;
#endif
        }

        public virtual bool IsRunningClientSideBlazor()
        {
#if BlazorClient
            return true;
#else
            return false;
#endif
        }
    }
}
