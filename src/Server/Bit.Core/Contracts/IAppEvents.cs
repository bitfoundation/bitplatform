namespace Bit.Core.Contracts
{
    /// <summary>
    /// <see cref="IAppEvents"/>'s implementations which are registered using <see cref="IDependencyManagerExtensions.RegisterAppEvents{TAppEvents}(IDependencyManager)"/> will be resolved on app startup and app shutdown to allow you execute code at those times.
    /// Note that you don't have access to neither of current request and current user.
    /// </summary>
    public interface IAppEvents
    {
        /// <summary>
        /// This code will be executed at app startup time.
        /// </summary>
        void OnAppStartup();

        /// <summary>
        /// This code will be executed at app shutdown time.
        /// </summary>
        void OnAppEnd();
    }
}
