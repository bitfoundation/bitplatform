using System.Text;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Boilerplate.Server.Api.Infrastructure.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Boilerplate.Tests.Features.Diagnostics;

/// <summary>
/// <see cref="CachedHealthCheck"/>, <see cref="AppCertificateHealthCheck"/>, <see cref="SmtpHealthCheck"/> and <see cref="EmailDeliveryHealthCheck"/> without a server.
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
        Assert.AreEqual(checkedAt, reused.Data["VerifiedAt"], "A reused result must say when the check really ran.");
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

    private const string Spf = "v=spf1 include:_spf.google.com ~all";
    private const string Dmarc = "v=DMARC1; p=quarantine; rua=mailto:dmarc@example.com";

    /// <summary>
    /// <c>emailDelivery</c>, from records handed to it instead of a DNS server. Missing SPF or DMARC sends sign-in codes
    /// to spam, so it is Degraded, never Unhealthy: the app itself works.
    /// </summary>
    [TestMethod]
    [DataRow(Spf, Dmarc, HealthStatus.Healthy, DisplayName = "SPF and DMARC")]
    [DataRow(null, Dmarc, HealthStatus.Degraded, DisplayName = "No SPF")]
    [DataRow("v=spf1 +all", Dmarc, HealthStatus.Degraded, DisplayName = "An SPF that lets every server send")]
    [DataRow("v=spf1 a mx all", Dmarc, HealthStatus.Degraded, DisplayName = "A bare all, which is +all")]
    [DataRow("v=spf1; -all", Dmarc, HealthStatus.Degraded, DisplayName = "No SPF: a semicolon doesn't end its version (RFC 7208 §4.5)")]
    [DataRow(Spf, null, HealthStatus.Degraded, DisplayName = "No DMARC")]
    [DataRow(null, null, HealthStatus.Degraded, DisplayName = "Neither")]
    public async Task EmailDeliveryHealthCheck_Should_NeedSpfAndDmarc(string? spf, string? dmarc, HealthStatus expectedStatus)
    {
        Dictionary<string, string[]> records = new()
        {
            // Another TXT record at the domain must not pass for SPF.
            ["example.com"] = spf is null ? ["google-site-verification=abc"] : ["google-site-verification=abc", spf]
        };

        if (dmarc is not null)
        {
            records["_dmarc.example.com"] = [dmarc];
        }

        var result = await CheckEmailDelivery(new("example.com", "smtp.example.net", AppleSignIn: false), records);

        Assert.AreEqual(expectedStatus, result.Status, result.Description);
        Assert.AreEqual("example.com", result.Data["Domain"]);
    }

    /// <summary>Two SPF records are a permanent error to receivers (RFC 7208 §4.5), the same as none.</summary>
    [TestMethod]
    public async Task EmailDeliveryHealthCheck_Should_ReportDegraded_ForTwoSpfRecords()
    {
        var result = await CheckEmailDelivery(new("example.com", "smtp.example.net", AppleSignIn: false), new()
        {
            ["example.com"] = [Spf, "v=spf1 include:sendgrid.net ~all"],
            ["_dmarc.example.com"] = [Dmarc]
        });

        Assert.AreEqual(HealthStatus.Degraded, result.Status);
        Assert.Contains("2 SPF records", result.Description!);
    }

    /// <summary>
    /// Two DMARC records at a name, or one without a valid policy, and receivers apply no DMARC at all (RFC 7489 §6.6.3),
    /// without falling back to the organizational domain. A record without <c>p</c> but with a reporting address counts
    /// as <c>p=none</c>.
    /// </summary>
    [TestMethod]
    [DataRow(new[] { "v=DMARC1; p=reject" }, HealthStatus.Healthy, null, DisplayName = "One record with a policy")]
    [DataRow(new[] { "v=DMARC1;p=none" }, HealthStatus.Healthy, null, DisplayName = "No space after the version")]
    [DataRow(new[] { "v=DMARC1; rua=mailto:dmarc@example.com" }, HealthStatus.Healthy, null, DisplayName = "No policy, but a reporting address: p=none")]
    [DataRow(new[] { "v=DMARC1; p=quarantine", "v=DMARC1; p=reject" }, HealthStatus.Degraded, "2 DMARC records", DisplayName = "Two records")]
    [DataRow(new[] { "v=DMARC1" }, HealthStatus.Degraded, "no valid policy", DisplayName = "No policy")]
    [DataRow(new[] { "v=DMARC1; p=strict" }, HealthStatus.Degraded, "no valid policy", DisplayName = "A policy that isn't one")]
    public async Task EmailDeliveryHealthCheck_Should_NeedOneDmarcRecord_WithAPolicy(string[] dmarc, HealthStatus expectedStatus, string? expectedProblem)
    {
        var result = await CheckEmailDelivery(new("mail.example.com", "smtp.example.net", AppleSignIn: false), new()
        {
            ["mail.example.com"] = [Spf],
            ["_dmarc.mail.example.com"] = dmarc,
            // Not reached: a receiver stops at the records it found.
            ["_dmarc.example.com"] = [Dmarc]
        });

        Assert.AreEqual(expectedStatus, result.Status, result.Description);
        Assert.Contains("_dmarc.mail.example.com", (string)result.Data["Dmarc"]);
        if (expectedProblem is not null)
        {
            Assert.Contains(expectedProblem, result.Description!);
        }
    }

    /// <summary>A receiver falls back to the organizational domain's DMARC record (RFC 7489 §6.6.3), and so does the check.</summary>
    [TestMethod]
    public async Task EmailDeliveryHealthCheck_Should_AcceptTheOrganizationalDomainsDmarc()
    {
        var result = await CheckEmailDelivery(new("mail.example.com", "smtp.example.net", AppleSignIn: false), new()
        {
            ["mail.example.com"] = [Spf],
            ["_dmarc.example.com"] = [Dmarc]
        });

        Assert.AreEqual(HealthStatus.Healthy, result.Status, result.Description);
        Assert.Contains("_dmarc.example.com", (string)result.Data["Dmarc"]);
    }

    /// <summary>
    /// DKIM is looked up only at the selectors the SMTP host's service uses for everyone, and only reported: a missing
    /// one leaves a healthy domain healthy, as the app can't know a selector the service chose.
    /// </summary>
    [TestMethod]
    [DataRow("smtp.gmail.com", "google", true, DisplayName = "Google Workspace, signed")]
    [DataRow("smtp.office365.com", "selector1", false, DisplayName = "Microsoft 365, not signed")]
    [DataRow("smtp.sendgrid.net", "s1", true, DisplayName = "SendGrid, signed")]
    [DataRow("smtp.azurecomm.net", "selector1-azurecomm-prod-net", true, DisplayName = "Azure Communication Services, signed")]
    public async Task EmailDeliveryHealthCheck_Should_OnlyReportDkim_AtTheServicesSelectors(string smtpHost, string selector, bool signed)
    {
        Dictionary<string, string[]> records = new()
        {
            ["example.com"] = [Spf],
            ["_dmarc.example.com"] = [Dmarc]
        };

        if (signed)
        {
            records[$"{selector}._domainkey.example.com"] = ["v=DKIM1; k=rsa; p=MIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8A"];
        }

        var result = await CheckEmailDelivery(new("example.com", smtpHost, AppleSignIn: false), records);

        Assert.AreEqual(HealthStatus.Healthy, result.Status, result.Description);
        Assert.Contains($"{(signed ? "found" : "none")} at {selector}._domainkey.example.com", (string)result.Data["Dkim"]);
    }

    /// <summary>
    /// A DKIM name usually points at the sending service's own DNS, so its lookup can fail while SPF and DMARC answer. It
    /// is reported, and the status stays what SPF and DMARC make it.
    /// </summary>
    [TestMethod]
    public async Task EmailDeliveryHealthCheck_Should_KeepTheStatus_WhenADkimLookupFails()
    {
        var result = await Check(new EmailDeliveryHealthCheck(new("example.com", "smtp.office365.com", AppleSignIn: false), (name, _) => name switch
        {
            "example.com" => Task.FromResult<string[]>([Spf]),
            "_dmarc.example.com" => Task.FromResult<string[]>([Dmarc]),
            "selector1._domainkey.example.com" => Task.FromException<string[]>(new InvalidOperationException("SERVFAIL")),
            _ => Task.FromResult<string[]>([])
        }));

        Assert.AreEqual(HealthStatus.Healthy, result.Status, result.Description);
        Assert.IsNull(result.Exception);
        Assert.Contains("couldn't be read at selector1._domainkey.example.com", (string)result.Data["Dkim"]);
        Assert.Contains("none at selector2._domainkey.example.com", (string)result.Data["Dkim"]);
    }

    [TestMethod]
    public async Task EmailDeliveryHealthCheck_Should_NotGuessDkimSelectors_ForAServiceThatGivesEachDomainItsOwn()
    {
        List<string> lookedUp = [];

        var result = await CheckEmailDelivery(new("example.com", "email-smtp.eu-west-1.amazonaws.com", AppleSignIn: false), new()
        {
            ["example.com"] = [Spf],
            ["_dmarc.example.com"] = [Dmarc]
        }, lookedUp);

        Assert.AreEqual(HealthStatus.Healthy, result.Status, result.Description);
        Assert.DoesNotContain(name => name.Contains("._domainkey.", StringComparison.Ordinal), lookedUp);
        Assert.Contains("Not looked up", (string)result.Data["Dkim"]);
    }

    /// <summary>Apple's private relay only takes mail from a domain registered with Apple, which no API can confirm.</summary>
    [TestMethod]
    [DataRow(true, DisplayName = "Sign in with Apple configured")]
    [DataRow(false, DisplayName = "Not configured")]
    public async Task EmailDeliveryHealthCheck_Should_NoteApplesPrivateRelay_OnlyWithSignInWithApple(bool appleSignIn)
    {
        var result = await CheckEmailDelivery(new("example.com", "smtp.example.net", appleSignIn), new()
        {
            ["example.com"] = [Spf],
            ["_dmarc.example.com"] = [Dmarc]
        });

        Assert.AreEqual(HealthStatus.Healthy, result.Status, "A note, not a finding.");
        Assert.AreEqual(appleSignIn, result.Data.ContainsKey("AppleRelay"));
        if (appleSignIn)
        {
            Assert.Contains("privaterelay.appleid.com", (string)result.Data["AppleRelay"]);
        }
    }

    [TestMethod]
    public async Task EmailDeliveryHealthCheck_Should_ReportTheRegisteredFailureStatus_WhenDnsFails()
    {
        var result = await Check(new EmailDeliveryHealthCheck(new("example.com", "smtp.example.net", AppleSignIn: false),
            (_, _) => Task.FromException<string[]>(new InvalidOperationException("SERVFAIL"))));

        Assert.AreEqual(HealthStatus.Degraded, result.Status);
        Assert.IsNotNull(result.Exception);
    }

    /// <summary>
    /// Nothing to check where mail is caught rather than delivered, or where there is no sender address to take a
    /// domain from.
    /// </summary>
    [TestMethod]
    [DataRow("DoNotReply@example.com", "smtp.ethereal.email", false, false, DisplayName = "The shared ethereal.email test mailbox")]
    [DataRow("DoNotReply@example.com", "localhost", true, false, DisplayName = "Mailpit in development")]
    [DataRow("DoNotReply@example.com", "127.0.0.1", true, false, DisplayName = "A loopback catcher in development")]
    [DataRow("DoNotReply@example.com", "localhost", false, true, DisplayName = "A local relay outside development")]
    [DataRow("", "smtp.example.net", false, false, DisplayName = "No sender address")]
    [DataRow("DoNotReply@Example.COM", "smtp.example.net", false, true, DisplayName = "A real sender and SMTP server")]
    public void EmailDeliverySettings_Should_OnlyExist_WhereMailIsDelivered(string defaultFromEmail, string smtpHost, bool isDevelopment, bool expected)
    {
        var settings = EmailDeliverySettings.For(defaultFromEmail, smtpHost, appleSignIn: false, isDevelopment);

        Assert.AreEqual(expected, settings is not null);
        if (settings is not null)
        {
            Assert.AreEqual("example.com", settings.SenderDomain);
        }
    }

    private async Task<HealthCheckResult> CheckEmailDelivery(EmailDeliverySettings settings, Dictionary<string, string[]> records, List<string>? lookedUp = null)
    {
        return await Check(new EmailDeliveryHealthCheck(settings, (name, _) =>
        {
            if (lookedUp is not null)
            {
                lock (lookedUp)
                {
                    lookedUp.Add(name);
                }
            }

            return Task.FromResult(records.GetValueOrDefault(name) ?? []);
        }));
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
