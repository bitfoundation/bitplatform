using Microsoft.Extensions.DependencyInjection;
using Microsoft.OData;
using System;
using System.Reflection;
using System.Web.OData;

namespace Bit.OData.Implementations
{
    public class ExtendedContainerBuilder : DefaultContainerBuilder, IContainerBuilder
    {
        public override IServiceProvider BuildContainer()
        {
            IServiceCollection services = (IServiceCollection)GetType().GetTypeInfo().BaseType.GetField(nameof(services), BindingFlags.NonPublic | BindingFlags.Instance).GetValue(this);

            return services.BuildServiceProvider(); // This line needs recompile against MS.Extensions.DI 2.0
        }
    }
}
