namespace Foundation.Core.Contracts
{
    public interface IAppEvents
    {
        void OnAppStartup();

        void OnAppEnd();
    }
}
