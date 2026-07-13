//+:cnd:noEmit
using Boilerplate.Tests.Infrastructure.Services;
using Boilerplate.Server.Api.Features.Identity.Services;
using Boilerplate.Client.Core.Infrastructure.Services.HttpMessageHandlers;

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
        }
    }
}
