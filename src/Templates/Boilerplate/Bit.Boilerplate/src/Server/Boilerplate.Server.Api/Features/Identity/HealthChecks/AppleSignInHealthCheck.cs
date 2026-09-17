using AspNet.Security.OAuth.Apple;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Boilerplate.Server.Api.Features.Identity.HealthChecks;

/// <summary>
/// Signs the client secret as sign in does (a missing or invalid AppleAuthKey.p8 fails), then makes sure Apple answers.
/// Apple checks the authorization code first, so the secret itself can't be verified.
/// </summary>
public partial class AppleSignInHealthCheck : IHealthCheck
{
    [AutoInject] private IServiceProvider serviceProvider = default!;
    [AutoInject] private IAuthenticationSchemeProvider schemeProvider = default!;
    [AutoInject] private IOptionsMonitor<AppleAuthenticationOptions> optionsMonitor = default!;
    [AutoInject] private IHttpClientFactory httpClientFactory = default!;

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var scheme = await schemeProvider.GetSchemeAsync(AppleAuthenticationDefaults.AuthenticationScheme)
                ?? throw new InvalidOperationException("The Apple authentication scheme is not registered.");
            var options = optionsMonitor.Get(scheme.Name);

            if (options.GenerateClientSecret)
            {
                var httpContext = new DefaultHttpContext { RequestServices = serviceProvider, RequestAborted = cancellationToken };
                var clientSecret = await options.ClientSecretGenerator.GenerateAsync(new AppleGenerateClientSecretContext(httpContext, scheme, options));

                if (string.IsNullOrWhiteSpace(clientSecret))
                    throw new InvalidOperationException("No client secret was generated.");
            }

            // Sign in's own backchannel client (See RemoteAuthenticationOptionsConfigurator).
            using var httpClient = httpClientFactory.CreateClient(scheme.Name);
            using var response = await httpClient.GetAsync("https://appleid.apple.com/.well-known/openid-configuration", cancellationToken);
            response.EnsureSuccessStatusCode();

            return HealthCheckResult.Healthy("Sign in with Apple is healthy");
        }
        catch (Exception exp)
        {
            return new HealthCheckResult(context.Registration.FailureStatus, "Sign in with Apple is unhealthy", exp);
        }
    }
}
