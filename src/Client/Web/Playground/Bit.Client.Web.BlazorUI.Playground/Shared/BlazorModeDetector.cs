namespace Bit.Client.Web.BlazorUI.Playground.Shared
{
    public class BlazorModeDetector
    {
        public static BlazorModeDetector Current { get; set; } = new BlazorModeDetector();

        public virtual bool IsServer()
        {
            return Mode == BlazorMode.Server;
        }

        public virtual bool IsClient()
        {
            return Mode == BlazorMode.Client;
        }

        public virtual BlazorMode Mode
        {
            get
            {
#if BlazorClient
                return BlazorMode.Client;
#else
                return BlazorMode.Server;
#endif
            }
        }
    }

    public enum BlazorMode
    {
        Server,
        Client
    }

}
