namespace Bit.Platform.WebSite.Shared
{
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

    public enum BlazorMode
    {
        BlazorServer,
        BlazorWebAssembly
    }

}
