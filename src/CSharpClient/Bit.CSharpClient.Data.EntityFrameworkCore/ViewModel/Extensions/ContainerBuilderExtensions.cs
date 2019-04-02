using Bit.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace Autofac
{
    public static class ContainerBuilderExtensions
    {
        public static ContainerBuilder RegisterDbContext<TDbContext>(this ContainerBuilder containerBuilder, Action<DbContextOptionsBuilder> optionsAction = null)
            where TDbContext : EfCoreDbContextBase
        {
            if (containerBuilder == null)
                throw new ArgumentNullException(nameof(containerBuilder));

            IServiceCollection services = (IServiceCollection)containerBuilder.Properties[nameof(services)];

            services.AddEntityFrameworkSqlite();
            services.AddDbContext<TDbContext>(optionsAction, contextLifetime: ServiceLifetime.Transient, optionsLifetime: ServiceLifetime.Transient);

            containerBuilder.Register(c => (EfCoreDbContextBase)c.Resolve<TDbContext>());

            return containerBuilder;
        }
    }
}
