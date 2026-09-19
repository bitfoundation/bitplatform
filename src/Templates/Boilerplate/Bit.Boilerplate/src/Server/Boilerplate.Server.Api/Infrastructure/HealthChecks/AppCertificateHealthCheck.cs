using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Boilerplate.Server.Api.Infrastructure.HealthChecks;

/// <summary>
/// Token signing and Data Protection keep using an expired AppCertificate silently, so this reports Degraded within
/// <see cref="RenewalWindow"/> of expiry and Unhealthy outside its validity period (See AppCertificate.md).
/// </summary>
public partial class AppCertificateHealthCheck : IHealthCheck
{
    public static readonly TimeSpan RenewalWindow = TimeSpan.FromDays(30);

    [AutoInject] private IConfiguration configuration = default!;
    [AutoInject] private TimeProvider timeProvider = default!;

    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Check(AppCertificateService.GetActiveAppCertificate(configuration), timeProvider.GetUtcNow()));
    }

    public static HealthCheckResult Check(X509Certificate2 certificate, DateTimeOffset now)
    {
        var notBefore = new DateTimeOffset(certificate.NotBefore);
        var notAfter = new DateTimeOffset(certificate.NotAfter);
        var data = new Dictionary<string, object> { ["NotBefore"] = notBefore, ["NotAfter"] = notAfter };

        if (now < notBefore || now >= notAfter)
            return HealthCheckResult.Unhealthy($"AppCertificate is only valid from {notBefore:u} to {notAfter:u}", data: data);

        if (notAfter - now <= RenewalWindow)
            return HealthCheckResult.Degraded($"AppCertificate expires on {notAfter:u}", data: data);

        return HealthCheckResult.Healthy("AppCertificate is valid", data: data);
    }
}
