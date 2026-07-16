//+:cnd:noEmit
using Boilerplate.Tests.Infrastructure.Services;
using Boilerplate.Server.Api.Features.Identity.Services;
using Boilerplate.Client.Core.Infrastructure.Services.HttpMessageHandlers;
using Hangfire;
using Hangfire.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;

namespace Microsoft.AspNetCore.Builder;

public static partial class WebApplicationBuilderExtensions
{
    extension(WebApplicationBuilder builder)
    {
        public void AddTestProjectServices()
        {
            var services = builder.Services;

            builder.AddServerWebProjectServices();

            // Register test-specific services for all tests here

            // Capture every identity e-mail in-process (See TestIdentityEmailService) instead of rendering and delivering it,
            // so tests can read back the confirmation link / OTP / elevated-access token straight from the message. Capturing
            // synchronously as the e-mail is requested - rather than via the Hangfire delivery job - is deliberate: that job
            // can starve and never run under parallel test load.
            services.AddSingleton<EmailCaptureStore>();
            services.RemoveAll<IdentityEmailService>();
            services.AddScoped<IdentityEmailService, TestIdentityEmailService>();

            services.AddTransient<HttpClient>(sp =>
            {
                var handlerFactory = sp.GetRequiredService<HttpMessageHandlersChainFactory>();
                return new HttpClient(handlerFactory.Invoke())
                {
                    BaseAddress = new Uri(sp.GetRequiredService<IConfiguration>().GetServerAddress(), UriKind.Absolute)
                };
            });

            services.AddHangfire((sp, hangfireConfiguration) =>
            {
                hangfireConfiguration.UseEFCoreStorage(optionsBuilder =>
                {
                    var keepAliveConnection = new Microsoft.Data.Sqlite.SqliteConnection($"Data Source=BoilerplateJobs-{Guid.NewGuid():N};Mode=Memory;Cache=Shared;");
                    keepAliveConnection.Open();
                    sp.GetRequiredService<IHostApplicationLifetime>().ApplicationStopped.Register(keepAliveConnection.Dispose);

                    optionsBuilder.UseSqlite(keepAliveConnection.ConnectionString);
                }, new()
                {
                    Schema = "jobs",
                    QueuePollInterval = new TimeSpan(0, 0, 1)
                })
                .UseDatabaseCreator();

                // Hangfire keeps its log provider in a process-wide static (Hangfire.Logging.LogProvider.CurrentLogProvider).
                // With several hosts per dotnet test runner process, each AddHangfire binds that static to its own (disposable) ILoggerFactory,
                // so disposing one host makes every still-running host throw ObjectDisposedException the next time Hangfire
                // logs (e.g. while constructing a background job client during a request). Bind logging to the never-disposed
                // NullLoggerFactory so the shared static stays valid for the whole lifetime of the process.
                hangfireConfiguration.UseLogProvider(new Hangfire.AspNetCore.AspNetCoreLogProvider(Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance));

                hangfireConfiguration.UseRecommendedSerializerSettings();
                hangfireConfiguration.UseSimpleAssemblyNameTypeSerializer();
                hangfireConfiguration.UseIgnoredAssemblyVersionTypeResolver();
                hangfireConfiguration.SetDataCompatibilityLevel(CompatibilityLevel.Version_180);
            });
        }
    }
}
