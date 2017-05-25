namespace Bit.Core.Contracts
{
    public interface IAppEvents
    {
        void OnAppStartup();

        void OnAppEnd();
    }
}
