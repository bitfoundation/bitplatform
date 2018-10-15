#if DotNetCore

using Bit.Data;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Bit.Data
{
    internal class BitEfCoreMigrationsOnlyWebHostBuilder
    {
        internal static TypeInfo DbContextType { get; set; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddEntityFrameworkSqlite();
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
                 .UseFakeServer()
                 .Build();
        }
    }

    internal class FakeServer : IServer
    {
        public IFeatureCollection Features => new FeatureCollection();

        public void Dispose()
        {

        }

        public Task StartAsync<TContext>(IHttpApplication<TContext> application, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}

namespace Microsoft.AspNetCore.Hosting
{
    internal static class HttpListenerWebHostBuilderExtensions
    {
        public static IWebHostBuilder UseFakeServer(this IWebHostBuilder builder)
        {
            return builder.ConfigureServices(services =>
            {
                services.AddSingleton<IServer, FakeServer>();
            });
        }
    }
}
#endif
