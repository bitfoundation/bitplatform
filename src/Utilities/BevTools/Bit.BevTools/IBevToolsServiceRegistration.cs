using Microsoft.Extensions.DependencyInjection;

namespace Bit.BevTools;

internal interface IBevToolsServiceRegistration
{
    void Apply(IServiceCollection services, bool enabled);
}
