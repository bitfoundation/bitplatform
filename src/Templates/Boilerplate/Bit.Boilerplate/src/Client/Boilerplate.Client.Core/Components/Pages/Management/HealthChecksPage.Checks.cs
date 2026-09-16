//+:cnd:noEmit
using Boilerplate.Shared.Features.Diagnostic;

namespace Boilerplate.Client.Core.Components.Pages.Management;

public partial class HealthChecksPage
{
    /// <summary>
    /// The groups the page shows checks in, in this order. A check the catalog below doesn't know lands in "Other".
    /// </summary>
    private static readonly (string Name, string IconName, string About)[] categories =
    [
        ("Platform", BitIconName.Server, "What every request depends on: storage, database, jobs and the edge."),
        ("Messaging", BitIconName.Message, "How the app reaches people: email, SMS, push and realtime."),
        ("Identity", BitIconName.Permissions, "External sign in providers and bot protection."),
        ("AI", BitIconName.Robot, "The models behind the chatbot, search and voice calls."),
        ("Other", BitIconName.Diagnostic, "Checks this page has no description for.")
    ];

    /// <summary>
    /// Title, group, icon and what each check proves, keyed by registration name (See AddServerApiHealthChecks).
    /// </summary>
    private static readonly Dictionary<string, (string Title, string Category, string IconName, string About)> catalog = new()
    {
        ["binStorage"] = ("Disk space", "Platform", BitIconName.HardDrive, "At least 2 GB free where the app runs. The only liveness check."),
        ["AppDbContext"] = ("Database", "Platform", BitIconName.Database, "Opens a connection to the app's database."),
        ["hangfire"] = ("Background jobs", "Platform", BitIconName.Processing, "At least one Hangfire server is running."),
        ["appCertificate"] = ("App certificate", "Platform", BitIconName.Certificate, "The certificate that signs tokens and protects data is within its validity period."),
        ["StackExchange.Redis_redis-persistent"] = ("Redis (persistent)", "Platform", BitIconName.Server, "Hangfire storage and distributed locks."),
        ["StackExchange.Redis_redis-cache"] = ("Redis (cache)", "Platform", BitIconName.ServerProcesses, "The distributed cache and backplane. Memory keeps serving without it."),
        ["userProfileImages"] = ("File storage", "Platform", BitIconName.Photo2, "The storage container of the uploaded files."),
        ["cloudflare"] = ("Cloudflare", "Platform", BitIconName.Globe, "The zones whose edge cache the app purges."),
        ["serverApi"] = ("Server API", "Platform", BitIconName.PlugConnected, "The standalone API this web app calls."),

        ["smtp"] = ("Email", "Messaging", BitIconName.Mail, "Signs in to the SMTP server without sending anything."),
        ["sms"] = ("SMS", "Messaging", BitIconName.CellPhone, "The Twilio account is active."),
        ["firebase"] = ("Android push", "Messaging", BitIconName.Ringer, "Firebase Cloud Messaging accepts the credentials."),
        ["apns"] = ("Apple push", "Messaging", BitIconName.Ringer, "APNs accepts the credentials."),
        ["azureSignalR"] = ("Azure SignalR", "Messaging", BitIconName.Streaming, "The service is healthy and accepts the access key."),

        ["keycloakIdentity"] = ("Keycloak", "Identity", BitIconName.Permissions, "The realm's discovery document answers."),
        ["entraId"] = ("Microsoft Entra ID", "Identity", BitIconName.AuthenticatorApp, "The tenant answers and accepts the app's credentials."),
        ["appleSignIn"] = ("Sign in with Apple", "Identity", BitIconName.Signin, "The client secret can be signed and Apple answers."),
        ["googleSignIn"] = ("Sign in with Google", "Identity", BitIconName.Signin, "Can't be verified without a user; only reported while not configured."),
        ["gitHubSignIn"] = ("Sign in with GitHub", "Identity", BitIconName.Signin, "Can't be verified without a user; only reported while not configured."),
        ["twitterSignIn"] = ("Sign in with X", "Identity", BitIconName.Signin, "Can't be verified without a user; only reported while not configured."),
        ["facebookSignIn"] = ("Sign in with Facebook", "Identity", BitIconName.Signin, "Can't be verified without a user; only reported while not configured."),
        ["reCaptcha"] = ("reCAPTCHA", "Identity", BitIconName.Shield, "Google's siteverify answers."),

        ["aiChat"] = ("AI chat", "AI", BitIconName.ChatBot, "The chat model gives a short answer."),
        ["aiEmbedding"] = ("Embeddings", "AI", BitIconName.DocumentSearch, "One word gets embedded, as semantic search does."),
        ["aiSpeechToText"] = ("Speech to text", "AI", BitIconName.Microphone, "Half a second of silence gets transcribed."),
        ["aiTextToSpeech"] = ("Text to speech", "AI", BitIconName.Speakers, "\"OK\" is read aloud in the configured voice."),
        ["aiRealtime"] = ("Voice calls", "AI", BitIconName.Headset, "The realtime model issues a session for the configured voice.")
    };

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

    /// <summary>
    /// Data keys the page shows on its own (See CachedHealthCheck), so the details list leaves them out.
    /// </summary>
    private static readonly string[] knownDataKeys = ["CheckedAt", "CacheDuration", "Duration"];

    private sealed class CheckView(string name, HealthReportEntryDto entry)
    {
        private readonly (string Title, string Category, string IconName, string About) info =
            catalog.GetValueOrDefault(name, (name, "Other", BitIconName.Diagnostic, ""));

        public string Name => name;

        public HealthReportEntryDto Entry => entry;

        public string Title => info.Title;

        public string Category => info.Category;

        public string IconName => info.IconName;

        public string About => info.About;

        public string? Description => entry.Description;

        /// <summary>
        /// A missing configuration reports Degraded (See AddNotConfiguredCheck), but asks for a different fix.
        /// </summary>
        public Tone Tone => entry.Status switch
        {
            HealthCheckStatus.Healthy => Tone.Healthy,
            HealthCheckStatus.Degraded when entry.Description?.Contains("is not configured", StringComparison.Ordinal) is true => Tone.NotConfigured,
            HealthCheckStatus.Degraded => Tone.Degraded,
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
        /// How long the check really ran; a cached one's report duration is only the cache lookup (See CachedHealthCheck).
        /// </summary>
        public TimeSpan Duration => TimeSpan.TryParse(entry.Data.GetValueOrDefault("Duration"), CultureInfo.InvariantCulture, out var duration) ? duration : entry.Duration;

        public double? TimeoutUsage => entry.Timeout is { TotalMilliseconds: > 0 } timeout
            ? Math.Min(100, 100 * Duration.TotalMilliseconds / timeout.TotalMilliseconds)
            : null;

        /// <summary>
        /// When a cached check really ran, which may be well before this report (See CachedHealthCheck).
        /// </summary>
        public DateTimeOffset? VerifiedAt => DateTimeOffset.TryParse(entry.Data.GetValueOrDefault("CheckedAt"), CultureInfo.InvariantCulture, out var at) ? at : null;

        public TimeSpan? CacheDuration => TimeSpan.TryParse(entry.Data.GetValueOrDefault("CacheDuration"), CultureInfo.InvariantCulture, out var duration) ? duration : null;

        public IEnumerable<KeyValuePair<string, string?>> ExtraData => entry.Data.Where(item => knownDataKeys.Contains(item.Key) is false);

        public bool HasDetails => string.IsNullOrWhiteSpace(entry.Exception) is false || ExtraData.Any();

        /// <summary>
        /// The exception's first line, which is its type and message; the rest is the stack trace.
        /// </summary>
        public string? ExceptionSummary => entry.Exception?.Split('\n', 2)[0].Trim();
    }
}
