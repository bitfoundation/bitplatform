using Hangfire;

namespace Bit.Hangfire.Contracts
{
    public interface IHangfireOptionsCustomizer
    {
        void Customize(IGlobalConfiguration globalConfiguration, BackgroundJobServerOptions options, JobStorage storage);
    }
}
