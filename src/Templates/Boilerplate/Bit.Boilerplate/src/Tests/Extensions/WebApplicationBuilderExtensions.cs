//+:cnd:noEmit
using Boilerplate.Server.Web;
using Boilerplate.Tests.Services;
using Boilerplate.Server.Api.Services;

namespace Microsoft.AspNetCore.Builder;

public static partial class WebApplicationBuilderExtensions
{
    public static void AddTestProjectServices(this WebApplicationBuilder builder)
    {
        var services = builder.Services;

        builder.AddServerWebProjectServices();

        //#if (advancedTests == true)
        services.AddTransient<PhoneService, FakePhoneService>();
        //#if (captcha == "reCaptcha")
        services.AddTransient<GoogleRecaptchaService, FakeGoogleRecaptchaService>();
        //#endif
        //#endif

        services.AddTransient(sp =>
        {
            return new HttpClient(sp.GetRequiredService<HttpMessageHandler>())
            {
                BaseAddress = new Uri(sp.GetRequiredService<IConfiguration>().GetServerAddress(), UriKind.Absolute)
            };
        });
    }
}
