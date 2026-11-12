using System.Net.Mail;
using DnsClient;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Boilerplate.Server.Api.Infrastructure.HealthChecks;

/// <param name="SenderDomain">The domain of <c>Email:DefaultFromEmail</c>, the address every mail of the app is sent from.</param>
/// <param name="SmtpHost">The host of the <c>smtp</c> connection string, which tells the DKIM selectors worth trying.</param>
/// <param name="AppleSignIn">Whether Sign in with Apple is configured, whose users may hide behind Apple's private relay.</param>
public record EmailDeliverySettings(string SenderDomain, string SmtpHost, bool AppleSignIn)
{
    /// <summary>
    /// Null where the check has nothing to say: no sender address to take a domain from, or mail that is caught rather
    /// than delivered - the shared ethereal.email test mailbox, or a local catcher such as Mailpit in development.
    /// </summary>
    public static EmailDeliverySettings? For(string? defaultFromEmail, string? smtpHost, bool appleSignIn, bool isDevelopment)
    {
        if (string.IsNullOrWhiteSpace(smtpHost) || MailAddress.TryCreate(defaultFromEmail, out var address) is false)
            return null;

        if (smtpHost.EndsWith("ethereal.email", StringComparison.OrdinalIgnoreCase))
            return null;

        if (isDevelopment && (smtpHost is "localhost" || (System.Net.IPAddress.TryParse(smtpHost, out var ip) && System.Net.IPAddress.IsLoopback(ip))))
            return null;

        return new(address.Host.ToLowerInvariant(), smtpHost.ToLowerInvariant(), appleSignIn);
    }
}

/// <summary>
/// Whether mail from the app's sender domain can reach an inbox rather than the spam folder, from DNS alone: an SPF
/// record that doesn't allow everyone (<c>+all</c>), and a DMARC record. Either missing is Degraded - sign-in codes and
/// magic links then land in spam or nowhere - and never Unhealthy, as the app itself works. DKIM's selector is the
/// sending service's choice and unknown to the app, so DKIM is only looked up at the selectors well-known services
/// always use, and reported in the entry's data without affecting its status.
/// </summary>
public class EmailDeliveryHealthCheck(EmailDeliverySettings settings, EmailDeliveryHealthCheck.TxtLookup lookupTxt) : IHealthCheck
{
    /// <summary>The TXT records at a name, each one's strings joined. Empty when the name has none or doesn't exist.</summary>
    public delegate Task<string[]> TxtLookup(string name, CancellationToken cancellationToken);

    /// <summary>
    /// Selectors a service uses for every customer, by its SMTP host. Others (Amazon SES, Postmark, Mailgun...) give each
    /// domain selectors of its own, so nothing is tried for them.
    /// </summary>
    private static readonly (string HostSuffix, string[] Selectors)[] WellKnownDkimSelectors =
    [
        ("gmail.com", ["google"]),
        ("office365.com", ["selector1", "selector2"]),
        ("outlook.com", ["selector1", "selector2"]),
        ("sendgrid.net", ["s1", "s2"]),
        ("mailjet.com", ["mailjet"]),
        ("mandrillapp.com", ["mandrill"]),
        ("resend.com", ["resend"])
    ];

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var domain = settings.SenderDomain;

        try
        {
            var selectors = WellKnownDkimSelectors.FirstOrDefault(service => IsHostOf(settings.SmtpHost, service.HostSuffix)).Selectors ?? [];

            // Side by side: each is a round trip to the DNS server.
            var spfTask = lookupTxt(domain, cancellationToken);
            var dmarcTask = FindDmarc(domain, cancellationToken);
            var dkimTask = Task.WhenAll(selectors.Select(selector => DescribeDkim($"{selector}._domainkey.{domain}", cancellationToken)));

            var spf = (await spfTask).Where(IsSpf).ToArray();
            var (dmarcName, dmarc) = await dmarcTask;
            var dkim = await dkimTask;

            Dictionary<string, object> data = new()
            {
                ["Domain"] = domain,
                ["Spf"] = spf.Length is 0 ? "None" : string.Join(" | ", spf),
                ["Dmarc"] = dmarc.Length is 0 ? "None" : $"{string.Join(" | ", dmarc)} (at {dmarcName})",
                ["Dkim"] = dkim.Length is 0
                    ? $"Not looked up: the selector is the choice of the service behind {settings.SmtpHost}, which the app can't know."
                    : string.Join(", ", dkim)
            };

            if (settings.AppleSignIn)
            {
                data["AppleRelay"] = $"Mail to Sign in with Apple's private relay addresses (privaterelay.appleid.com) only arrives when {domain} is registered with Apple " +
                                     "(Certificates, Identifiers & Profiles > Services > Sign in with Apple for Email Communication) and its SPF passes. No API can confirm that.";
            }

            List<string> problems = [];

            if (spf.Length is 0)
            {
                problems.Add($"{domain} has no SPF record, so receivers can't tell this app's mail from forged mail and may put it in spam");
            }
            else if (spf.Length > 1)
            {
                problems.Add($"{domain} has {spf.Length} SPF records, which receivers treat as none (RFC 7208 §4.5)");
            }
            else if (AllowsEveryone(spf[0]))
            {
                problems.Add($"{domain}'s SPF record ends with +all, which lets any server send as {domain}");
            }

            // RFC 7489 §6.6.3: with more than one record, or one without a valid policy, receivers apply no DMARC at all.
            if (dmarc.Length is 0)
            {
                problems.Add($"{domain} has no DMARC record at _dmarc.{domain}, which Gmail and Yahoo require of bulk senders");
            }
            else if (dmarc.Length > 1)
            {
                problems.Add($"{dmarcName} has {dmarc.Length} DMARC records, which receivers treat as none");
            }
            else if (HasPolicy(dmarc[0]) is false)
            {
                problems.Add($"The DMARC record at {dmarcName} has no valid policy (p=none, quarantine or reject), so receivers apply none");
            }

            return problems.Count is 0
                ? HealthCheckResult.Healthy($"SPF and DMARC are set up for {domain}", data)
                : new HealthCheckResult(context.Registration.FailureStatus, $"Mail from {domain} may not reach inboxes: {string.Join("; ", problems)}.", data: data);
        }
        catch (Exception exp)
        {
            return new HealthCheckResult(context.Registration.FailureStatus, $"Could not read the DNS records of {domain}", exp);
        }
    }

    /// <summary>
    /// A receiver that finds no DMARC record at the sender's own domain falls back to its organizational domain
    /// (RFC 7489 §6.6.3), so mail.example.com is covered by _dmarc.example.com.
    /// </summary>
    private async Task<(string? Name, string[] Records)> FindDmarc(string domain, CancellationToken cancellationToken)
    {
        for (var candidate = domain; candidate.Count(c => c is '.') >= 1; candidate = candidate[(candidate.IndexOf('.') + 1)..])
        {
            var name = $"_dmarc.{candidate}";
            var records = (await lookupTxt(name, cancellationToken)).Where(IsDmarc).ToArray();

            // Every record found here, not the first: two are as good as none, and the organizational domain isn't asked then.
            if (records.Length > 0)
                return (name, records);
        }

        return (null, []);
    }

    /// <summary>
    /// Reported, never a finding: DKIM doesn't change the status, so a lookup that fails doesn't either. The DNS error
    /// itself stays out of the entry's data, which an OAuth token reads too.
    /// </summary>
    private async Task<string> DescribeDkim(string name, CancellationToken cancellationToken)
    {
        try
        {
            var records = await lookupTxt(name, cancellationToken);

            return records.Any(record => record.Contains("p=", StringComparison.OrdinalIgnoreCase)) ? $"found at {name}" : $"none at {name}";
        }
        catch (Exception exp) when (exp is not OperationCanceledException)
        {
            return $"couldn't be read at {name}";
        }
    }

    /// <summary><c>all</c> with no qualifier is <c>+all</c> too (RFC 7208 §4.6.2).</summary>
    private static bool AllowsEveryone(string spf)
    {
        return spf.Split(' ', StringSplitOptions.RemoveEmptyEntries).Any(term => term.Equals("+all", StringComparison.OrdinalIgnoreCase) || term.Equals("all", StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>The version section ends with a space or the end of the record, never a semicolon (RFC 7208 §4.5).</summary>
    private static bool IsSpf(string record)
    {
        return record.StartsWith("v=spf1", StringComparison.OrdinalIgnoreCase)
               && (record.Length == "v=spf1".Length || record["v=spf1".Length] is ' ');
    }

    /// <summary>Tags are separated by semicolons, with optional white space around them (RFC 7489 §6.4).</summary>
    private static bool IsDmarc(string record)
    {
        return record.StartsWith("v=DMARC1", StringComparison.OrdinalIgnoreCase)
               && (record.Length == "v=DMARC1".Length || record["v=DMARC1".Length] is ' ' or '\t' or ';');
    }

    /// <summary>
    /// A valid <c>p</c> tag, or else a reporting address in <c>rua</c>, with which receivers act as if it said
    /// <c>p=none</c> (RFC 7489 §6.6.3).
    /// </summary>
    private static bool HasPolicy(string dmarc)
    {
        var tags = dmarc.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                        .Select(tag => tag.Split('=', 2, StringSplitOptions.TrimEntries))
                        .Where(pair => pair.Length is 2)
                        .ToLookup(pair => pair[0], pair => pair[1], StringComparer.OrdinalIgnoreCase);

        return tags["p"].Any(policy => policy.Equals("none", StringComparison.OrdinalIgnoreCase)
                                       || policy.Equals("quarantine", StringComparison.OrdinalIgnoreCase)
                                       || policy.Equals("reject", StringComparison.OrdinalIgnoreCase))
               || tags["rua"].Any(uris => uris.Contains("mailto:", StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsHostOf(string host, string suffix)
    {
        return host.Equals(suffix, StringComparison.OrdinalIgnoreCase) || host.EndsWith($".{suffix}", StringComparison.OrdinalIgnoreCase);
    }

    private static readonly LookupClient lookupClient = new(new LookupClientOptions
    {
        // Answers are kept for their TTL, so a probe that repeats a failing check doesn't query DNS again.
        UseCache = true,
        Timeout = TimeSpan.FromSeconds(3),
        Retries = 1,
        ThrowDnsErrors = false
    });

    /// <summary>Through the machine's own DNS servers. A name that doesn't exist has no records; any other DNS error throws.</summary>
    public static async Task<string[]> LookupTxtWithDnsClient(string name, CancellationToken cancellationToken)
    {
        var response = await lookupClient.QueryAsync(name, QueryType.TXT, cancellationToken: cancellationToken);

        if (response.HasError && response.Header.ResponseCode is not DnsHeaderResponseCode.NotExistentDomain)
            throw new InvalidOperationException($"Looking up the TXT records of {name} failed: {response.ErrorMessage}");

        return [.. response.Answers.TxtRecords().Select(record => string.Concat(record.Text))];
    }
}
