//+:cnd:noEmit
using Boilerplate.Server.Api.Features.Identity.Services;
using Boilerplate.Client.Core.Infrastructure.Services.HttpMessageHandlers;
using Hangfire;
using Hangfire.EntityFrameworkCore;

namespace Microsoft.AspNetCore.Builder;

public static partial class WebApplicationBuilderExtensions
{
    extension(WebApplicationBuilder builder)
    {
        /// <summary>
        /// Server.Web's test services. Server.Api's are in <c>AddTestApiProjectServices</c>.
        /// </summary>
        public void AddTestProjectServices()
        {
            var services = builder.Services;

            builder.AddServerWebProjectServices();
            //#if (api == "Integrated")
            builder.AddTestApiProjectServices(); // The api is integrated into this host.
            //#endif

            // Register test-specific services for all tests here

            services.AddTransient<HttpClient>(sp =>
            {
                var handlerFactory = sp.GetRequiredService<HttpMessageHandlersChainFactory>();
                // Read on every resolve, so a test that overrides WebAppUrl is heard (See AppTestServer.Build).
                var webAppUrl = new Uri(sp.GetRequiredService<IConfiguration>()["WebAppUrl"]
                    ?? throw new InvalidOperationException("WebAppUrl is not configured."), UriKind.Absolute);
                var httpClient = new HttpClient(handlerFactory.Invoke())
                {
                    //#if (api == "Standalone")
                    // Server.Web accepts the api requests and forwards them to Server.Api through YARP (See its
                    // Program.Middlewares.cs).
                    //#endif
                    BaseAddress = webAppUrl
                };
                httpClient.DefaultRequestHeaders.Add("X-Origin", webAppUrl.ToString());
                return httpClient;
            });
        }

        /// <summary>
        /// Server.Api's test services, registered wherever its services are (See <c>AppTestServer.ApiApp</c>).
        /// </summary>
        public void AddTestApiProjectServices()
        {
            var services = builder.Services;

            // Capture every identity e-mail in-process (See TestIdentityEmailService) instead of rendering and delivering it,
            // so tests can read back the confirmation link / OTP / elevated-access token straight from the message. Capturing
            // synchronously as the e-mail is requested - rather than via the Hangfire delivery job - is deliberate: that job
            // can starve and never run under parallel test load.
            services.AddSingleton<EmailCaptureStore>();
            services.RemoveAll<IdentityEmailService>();
            services.AddScoped<IdentityEmailService, TestIdentityEmailService>();

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
