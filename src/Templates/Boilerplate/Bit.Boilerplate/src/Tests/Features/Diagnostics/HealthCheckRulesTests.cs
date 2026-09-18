using System.Text;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Boilerplate.Server.Api.Infrastructure.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Boilerplate.Tests.Features.Diagnostics;

/// <summary>
/// <see cref="CachedHealthCheck"/>, <see cref="AppCertificateHealthCheck"/> and <see cref="SmtpHealthCheck"/> without a server.
/// </summary>
[TestClass, TestCategory("UnitTest")]
public partial class HealthCheckRulesTests
{
    public TestContext TestContext { get; set; } = default!;

    [TestMethod]
    public async Task CachedHealthCheck_Should_ReuseAHealthyResult_UntilItIsOld()
    {
        var timeProvider = new ManualTimeProvider();
        var innerCheck = new CountingHealthCheck();
        var cachedCheck = new CachedHealthCheck(innerCheck, new(), timeProvider, cacheDuration: TimeSpan.FromMinutes(5));

        var checkedAt = timeProvider.Now;
        await Check(cachedCheck);
        timeProvider.Now += TimeSpan.FromMinutes(4);
        var reused = await Check(cachedCheck);

        Assert.AreEqual(1, innerCheck.Calls, "A healthy result younger than the cache duration must be reused.");
        Assert.AreEqual(checkedAt, reused.Data["CheckedAt"], "A reused result must say when the check really ran.");
        Assert.AreEqual(TimeSpan.FromMinutes(5), reused.Data["CacheDuration"]);
        Assert.IsInstanceOfType<TimeSpan>(reused.Data["Duration"], "A reused result must say how long the check really ran.");

        timeProvider.Now += TimeSpan.FromMinutes(2);
        await Check(cachedCheck);

        Assert.AreEqual(2, innerCheck.Calls, "A healthy result older than the cache duration must be checked again.");
    }

    [TestMethod]
    public async Task CachedHealthCheck_Should_NeverReuseAFailingResult()
    {
        var innerCheck = new CountingHealthCheck { NextResult = HealthCheckResult.Degraded("Down") };
        var cachedCheck = new CachedHealthCheck(innerCheck, new(), new ManualTimeProvider(), cacheDuration: TimeSpan.FromMinutes(5));

        Assert.AreEqual(HealthStatus.Degraded, (await Check(cachedCheck)).Status);
        Assert.AreEqual(HealthStatus.Degraded, (await Check(cachedCheck)).Status);
        Assert.AreEqual(2, innerCheck.Calls, "Every probe must retry a failing check, so a recovery shows up right away.");

        innerCheck.NextResult = HealthCheckResult.Healthy();

        Assert.AreEqual(HealthStatus.Healthy, (await Check(cachedCheck)).Status);
        Assert.AreEqual(HealthStatus.Healthy, (await Check(cachedCheck)).Status);
        Assert.AreEqual(3, innerCheck.Calls, "Once recovered, the healthy result is reused again.");
    }

    [TestMethod]
    [DataRow(HealthStatus.Healthy)]
    [DataRow(HealthStatus.Degraded)]
    public async Task CachedHealthCheck_Should_ShareOneRun_BetweenConcurrentProbes(HealthStatus status)
    {
        var gate = new TaskCompletionSource();
        var innerCheck = new CountingHealthCheck { NextResult = new HealthCheckResult(status), Gate = gate.Task };
        var cachedCheck = new CachedHealthCheck(innerCheck, new(), new ManualTimeProvider(), cacheDuration: TimeSpan.FromMinutes(5));

        var probes = Enumerable.Range(0, 5).Select(_ => Check(cachedCheck)).ToArray();
        gate.SetResult();
        var results = await Task.WhenAll(probes);

        Assert.AreEqual(1, innerCheck.Calls, "Probes that arrive together must not each make the call.");
        Assert.IsTrue(results.All(r => r.Status == status));

        await Check(cachedCheck);
        Assert.AreEqual(status is HealthStatus.Healthy ? 1 : 2, innerCheck.Calls, "A finished run must not be shared with a later probe.");
    }

    [TestMethod]
    [DataRow(-10, 90, HealthStatus.Healthy, DisplayName = "Valid")]
    [DataRow(-10, 10, HealthStatus.Degraded, DisplayName = "Expires within the renewal window")]
    [DataRow(-10, -1, HealthStatus.Unhealthy, DisplayName = "Expired")]
    [DataRow(1, 90, HealthStatus.Unhealthy, DisplayName = "Not valid yet")]
    public void AppCertificateHealthCheck_Should_FollowTheValidityPeriod(int notBeforeDays, int notAfterDays, HealthStatus expectedStatus)
    {
        var now = DateTimeOffset.UtcNow;
        using var rsa = RSA.Create(2048);
        using var certificate = new CertificateRequest("CN=AppCertificate", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1)
            .CreateSelfSigned(now.AddDays(notBeforeDays), now.AddDays(notAfterDays));

        var result = AppCertificateHealthCheck.Check(certificate, now);

        Assert.AreEqual(expectedStatus, result.Status, result.Description);
    }

    /// <summary>
    /// The template's development AppCertificate must be renewed before generated projects report it (See AppCertificate.md).
    /// </summary>
    [TestMethod]
    public void ShippedAppCertificate_Should_NotBeCloseToExpiry()
    {
        using var certificate = X509Certificate2.CreateFromPemFile(Path.Combine(AppContext.BaseDirectory, "AppCertificate.crt"),
            Path.Combine(AppContext.BaseDirectory, "AppCertificate.key"));

        var result = AppCertificateHealthCheck.Check(certificate, DateTimeOffset.UtcNow);

        Assert.AreEqual(HealthStatus.Healthy, result.Status, $"{result.Description}. Renew the shipped AppCertificate.");
    }

    [TestMethod]
    [DataRow("user", "secret", HealthStatus.Healthy, DisplayName = "Accepted credentials")]
    [DataRow("user", "wrong", HealthStatus.Degraded, DisplayName = "Rejected credentials")]
    [DataRow("", "", HealthStatus.Healthy, DisplayName = "No credentials")]
    public async Task SmtpHealthCheck_Should_SignInWhenCredentialsAreSet(string userName, string password, HealthStatus expectedStatus)
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;

        var smtpServer = RunFakeSmtpServer(listener, TestContext.CancellationToken);

        var result = await Check(new SmtpHealthCheck(new SmtpSettings("127.0.0.1", port, userName, password, EnableSsl: false)));

        Assert.AreEqual(expectedStatus, result.Status, result.Exception?.Message);
        await smtpServer;
    }

    [TestMethod]
    public async Task SmtpHealthCheck_Should_ReportTheRegisteredFailureStatus_WhenNothingListens()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();

        var result = await Check(new SmtpHealthCheck(new SmtpSettings("127.0.0.1", port, "user", "secret", EnableSsl: false)));

        Assert.AreEqual(HealthStatus.Degraded, result.Status);
    }

    /// <summary>
    /// Just enough SMTP for the check. Like Exchange Online, it only offers AUTH LOGIN; it accepts user / secret.
    /// </summary>
    private static async Task RunFakeSmtpServer(TcpListener listener, CancellationToken cancellationToken)
    {
        using var client = await listener.AcceptTcpClientAsync(cancellationToken);
        await using var stream = client.GetStream();
        using var reader = new StreamReader(stream, Encoding.ASCII);
        await using var writer = new StreamWriter(stream, Encoding.ASCII) { NewLine = "\r\n", AutoFlush = true };

        await writer.WriteLineAsync("220 fake.smtp ESMTP");

        while (await reader.ReadLineAsync(cancellationToken) is { } command)
        {
            if (command.StartsWith("EHLO ", StringComparison.Ordinal))
            {
                await writer.WriteLineAsync("250-fake.smtp");
                await writer.WriteLineAsync("250 AUTH LOGIN XOAUTH2");
            }
            else if (command is "AUTH LOGIN")
            {
                await writer.WriteLineAsync("334 VXNlcm5hbWU6");
                var userName = await reader.ReadLineAsync(cancellationToken);
                await writer.WriteLineAsync("334 UGFzc3dvcmQ6");
                var password = await reader.ReadLineAsync(cancellationToken);

                await writer.WriteLineAsync(userName == Convert.ToBase64String("user"u8) && password == Convert.ToBase64String("secret"u8)
                    ? "235 2.7.0 Authentication successful"
                    : "535 5.7.8 Authentication credentials invalid");
            }
            else if (command.StartsWith("AUTH ", StringComparison.Ordinal))
            {
                await writer.WriteLineAsync("504 5.7.4 Unrecognized authentication type");
            }
            else if (command is "QUIT")
            {
                await writer.WriteLineAsync("221 Bye");
                return;
            }
            else
            {
                await writer.WriteLineAsync("502 Command not implemented");
            }
        }
    }

    private async Task<HealthCheckResult> Check(IHealthCheck healthCheck)
    {
        var registration = new HealthCheckRegistration("test", healthCheck, failureStatus: HealthStatus.Degraded, tags: null);

        return await healthCheck.CheckHealthAsync(new HealthCheckContext { Registration = registration }, TestContext.CancellationToken);
    }

    private sealed class ManualTimeProvider : TimeProvider
    {
        public DateTimeOffset Now { get; set; } = DateTimeOffset.UtcNow;

        public override DateTimeOffset GetUtcNow() => Now;
    }

    private sealed class CountingHealthCheck : IHealthCheck
    {
        public HealthCheckResult NextResult { get; set; } = HealthCheckResult.Healthy();

        public Task Gate { get; set; } = Task.CompletedTask;

        private int calls;
        public int Calls => calls;

        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            Interlocked.Increment(ref calls);
            await Gate;
            return NextResult;
        }
    }
}
