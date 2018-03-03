#if DotNetCore

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Reflection;

namespace Bit.Data
{
    internal class BitEfCoreMigrationsOnlyWebHostBuilder
    {
        internal static TypeInfo DbContextType { get; set; }

        public void ConfigureServices(IServiceCollection services)
        {
            typeof(EntityFrameworkServiceCollectionExtensions).GetTypeInfo()
                .GetMethod(nameof(EntityFrameworkServiceCollectionExtensions.AddDbContext), new Type[] { typeof(IServiceCollection), typeof(Action<DbContextOptionsBuilder>), typeof(ServiceLifetime), typeof(ServiceLifetime) })
                .MakeGenericMethod(DbContextType)
                .Invoke(null, new object[] { services, null, ServiceLifetime.Scoped, ServiceLifetime.Scoped });
        }

        public void Configure(IApplicationBuilder aspNetCoreApp)
        {

        }
    }

    public class BitEfCoreMigrationsOnlyWebHostBuilder<TDbContext>
        where TDbContext : DbContext
    {
        public static IWebHost BuildWebHost(string[] args)
        {
            BitEfCoreMigrationsOnlyWebHostBuilder.DbContextType = typeof(TDbContext).GetTypeInfo();

            return new WebHostBuilder()
                 .UseStartup<BitEfCoreMigrationsOnlyWebHostBuilder>()
                 .UseKestrel()
                 .Build();
        }
    }
}
#endif