using Autofac;
using Bit.Data;
using System;
using System.Reflection;

namespace Prism.Ioc
{
    public static class ContainerBuilderExtensions
    {
        public static ContainerBuilder RegisterDbContext<TDbContext>(this ContainerBuilder containerBuilder)
            where TDbContext : EfCoreDbContextBase
        {
            if (containerBuilder == null)
                throw new ArgumentNullException(nameof(containerBuilder));

            containerBuilder
                .RegisterType<TDbContext>()
                .As(new[] { typeof(EfCoreDbContextBase).GetTypeInfo(), typeof(TDbContext).GetTypeInfo() })
                .PropertiesAutowired(PropertyWiringOptions.PreserveSetValues);

            return containerBuilder;
        }
    }
}
