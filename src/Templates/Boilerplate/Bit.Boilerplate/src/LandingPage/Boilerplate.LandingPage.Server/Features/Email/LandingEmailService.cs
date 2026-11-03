using System.Net.Mail;
using FluentEmail.Core;
using Microsoft.AspNetCore.Components.Web;

namespace Boilerplate.LandingPage.Server.Features.Email;

public sealed partial class LandingEmailService(IServiceProvider serviceProvider, HtmlRenderer htmlRenderer, LandingServerSettings settings, ILogger<LandingEmailService> logger)
{
    public async Task SendAsync<TTemplate>(string toEmail, string subject, CultureInfo culture, Dictionary<string, object?> parameters, CancellationToken cancellationToken)
        where TTemplate : IComponent
    {
        var body = await htmlRenderer.Dispatcher.InvokeAsync(async () =>
        {
            LandingCultures.Apply(culture);
            var output = await htmlRenderer.RenderComponentAsync<TTemplate>(ParameterView.FromDictionary(parameters));
            return output.ToHtmlString();
        });

        if (serviceProvider.GetService<IFluentEmail>() is not { } fluentEmail)
        {
            LogEmailNotSent(logger, subject, toEmail, string.Join(' ', parameters.Values.OfType<Uri>()));
            return;
        }

        var result = await fluentEmail
            .To(toEmail)
            .Subject(subject)
            .SetFrom(settings.Email.DefaultFromEmail, settings.Email.DefaultFromName)
            .Body(body, isHtml: true)
            .SendAsync(cancellationToken);

        if (result.Successful is false)
            throw new InvalidOperationException(string.Join(", ", result.ErrorMessages));
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "No SMTP server is configured (ConnectionStrings:smtp), so \"{Subject}\" to {To} was not sent. Its links: {Links}")]
    private static partial void LogEmailNotSent(ILogger logger, string subject, string to, string links);
}

public static class LandingEmailServiceCollectionExtensions
{
    public static IServiceCollection AddLandingEmail(this IServiceCollection services, IConfiguration configuration, LandingServerSettings settings)
    {
        services.AddScoped<HtmlRenderer>();
        services.AddScoped<LandingEmailService>();

        if (configuration.GetConnectionString("smtp") is not { Length: > 0 } connectionString)
            return services;

        // [mirror] smtp connection string parsing - keep in sync with:
        // - src/Server/Boilerplate.Server.Api/Program.Services.cs (GetSmtpSettings)
        var values = connectionString.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(part => part.Split('=', 2))
            .Where(pair => pair.Length is 2)
            .ToDictionary(pair => pair[0], pair => pair[1], StringComparer.OrdinalIgnoreCase);

        var endpoint = new Uri(values.GetValueOrDefault("Endpoint", "smtp://localhost:25"));
        var port = endpoint.Port is -1 ? 25 : endpoint.Port;
        var userName = values.GetValueOrDefault("UserName");
        var password = values.GetValueOrDefault("Password");
        var enableSsl = values.GetValueOrDefault("EnableSsl", port is 465 or 587 ? "true" : "false").Equals("false", StringComparison.OrdinalIgnoreCase) is false;

        services.AddFluentEmail(settings.Email.DefaultFromEmail)
            .AddSmtpSender(() =>
            {
                var client = new SmtpClient(endpoint.Host, port) { EnableSsl = enableSsl };

                if (string.IsNullOrEmpty(userName) is false)
                {
                    client.Credentials = new NetworkCredential(userName, password);
                }

                return client;
            });

        return services;
    }
}
