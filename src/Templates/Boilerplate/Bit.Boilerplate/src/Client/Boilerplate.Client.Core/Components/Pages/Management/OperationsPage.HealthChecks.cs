//+:cnd:noEmit
using Boilerplate.Shared.Features.Diagnostic;

namespace Boilerplate.Client.Core.Components.Pages.Management;

public partial class OperationsPage
{
    /// <summary>
    /// The groups the page shows checks in, in this order: each check lands in the group its report names (See
    /// HealthCheckCategories), and in "Other" when it names none of these. Each check's title, icon and about come from
    /// the report too (See HealthCheckDescriptions).
    /// </summary>
    private static readonly (string Name, string IconName, string About)[] categories =
    [
        ("Platform", BitIconName.Server, "What every request depends on: storage, database, jobs and the edge."),
        ("Messaging", BitIconName.Message, "How the app reaches people: email, SMS, push and realtime."),
        ("Identity", BitIconName.Permissions, "External sign in providers and bot protection."),
        ("AI", BitIconName.Robot, "The models behind the chatbot, search and voice calls."),
        ("Integrations", BitIconName.PlugConnected, "The other services the app calls, such as a payment gateway or a map."),
        //#if (api == "Standalone")
        ("Web server", BitIconName.Website, "Server.Web, which renders the pages in front of the standalone API."),
        //#endif
        ("Other", BitIconName.Diagnostic, "Checks this page has no description for.")
    ];

    /// <summary>
    /// What no health check can prove, because it needs a user, a device or a delivery, and how to verify it.
    /// </summary>
    private static readonly (string Title, string IconName, string Why, string HowTo, ManualTest Test)[] manualChecks =
    [
        //#if (notification == true)
        ("Push notifications", BitIconName.Ringer,
            "Web push signs its messages locally, and the Android and Apple checks only prove the credentials.",
            "Tap the button on each device you want to verify. If everything works, you'll see a notification.", ManualTest.Push),
        //#endif
        ("Email delivery", BitIconName.Mail,
            "Signing in to the SMTP server doesn't prove a message reaches an inbox; SPF, DKIM and spam filters decide that.",
            "Tap the button to email your account's address, if it has one. Check that it arrives, and not in the spam folder.", ManualTest.Email),
        ("SMS delivery", BitIconName.Message,
            "An active Twilio account doesn't prove the sender number can reach every country.",
            "Tap the button to text your account's phone number, if it has one.", ManualTest.Sms),
        ("Social sign in", BitIconName.Signin,
            "Google, GitHub, X and Facebook only answer a user signing in, so their client secrets can't be verified.",
            "Sign in with each configured provider from the sign in page.", ManualTest.None),
        ("Sign in with Apple and Keycloak", BitIconName.AuthenticatorApp,
            "Apple checks the authorization code before the client secret, and Keycloak's secret is only used during a sign in.",
            "Sign in with Apple and with Keycloak from the sign in page.", ManualTest.None),
        //#if (captcha == "reCaptcha")
        ("reCAPTCHA secret", BitIconName.Shield,
            "Google checks the response before the secret, so the check can't tell a wrong secret apart.",
            "Create an account; sign up fails with a captcha error when the secret is wrong.", ManualTest.None),
        //#endif
    ];

    private enum ManualTest { None, Push, Email, Sms }

    private sealed class CheckView(string name, HealthReportEntryDto entry)
    {
        // Server.Web's own checks get a group of their own (See AddWebAppChecks); a category the page has no group for, "Other".
        private readonly string category = name.StartsWith("web:", StringComparison.Ordinal) ? "Web server"
            : categories.Any(category => category.Name == entry.Category) ? entry.Category!
            : "Other";

        public string Name => name;

        public HealthReportEntryDto Entry => entry;

        public string Title => entry.Title;

        public string Category => category;

        public string IconName => entry.IconName;

        public string About => entry.About ?? "";

        public string? Description => entry.Description;

        /// <summary>
        /// A missing configuration reports Degraded (See AddNotConfiguredCheck), but asks for a different fix.
        /// </summary>
        public Tone Tone => entry switch
        {
            { IsNotConfigured: true } => Tone.NotConfigured,
            { Status: HealthCheckStatus.Healthy } => Tone.Healthy,
            { Status: HealthCheckStatus.Degraded } => Tone.Degraded,
            _ => Tone.Unhealthy
        };

        public string StatusText => Tone switch
        {
            Tone.NotConfigured => "Not configured",
            _ => entry.Status.ToString()
        };

        public int Rank => Tone switch
        {
            Tone.Unhealthy => 0,
            Tone.Degraded => 1,
            Tone.NotConfigured => 2,
            _ => 3
        };

        /// <summary>
        /// Its failure makes /health answer 503.
        /// </summary>
        public bool IsCritical => entry.FailureStatus is HealthCheckStatus.Unhealthy;

        public bool IsLiveness => entry.Tags.Contains("live");

        /// <summary>
        /// The tags the card shows as they are: liveness, the category and a missing configuration are shown otherwise.
        /// </summary>
        public string[] OtherTags => [.. entry.Tags.Where(tag => tag is not ("live" or HealthCheckTags.NotConfigured)
                                                                 && tag.StartsWith(HealthCheckCategories.TagPrefix, StringComparison.Ordinal) is false)];

        public TimeSpan Duration => entry.Duration;

        public double? TimeoutUsage => entry.Timeout is { TotalMilliseconds: > 0 } timeout
            ? Math.Min(100, 100 * Duration.TotalMilliseconds / timeout.TotalMilliseconds)
            : null;

        public DateTimeOffset? VerifiedAt => entry.VerifiedAt;

        public TimeSpan? CacheDuration => entry.CacheDuration;

        /// <summary>What the check put in its result, without the run's fields repeated for older app versions.</summary>
        public IEnumerable<KeyValuePair<string, string?>> ExtraData => entry.Data.Where(item => HealthReportEntryDto.LegacyDataKeys.Contains(item.Key) is false);

        public bool HasDetails => string.IsNullOrWhiteSpace(entry.Exception) is false || ExtraData.Any();

        /// <summary>
        /// The exception's first line, which is its type and message; the rest is the stack trace.
        /// </summary>
        public string? ExceptionSummary => entry.Exception?.Split('\n', 2)[0].Trim();
    }
}
