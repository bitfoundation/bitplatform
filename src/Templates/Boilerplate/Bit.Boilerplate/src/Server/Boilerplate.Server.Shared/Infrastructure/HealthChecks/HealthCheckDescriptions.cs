namespace Boilerplate.Server.Shared.Infrastructure.HealthChecks;

/// <summary>What a health check is called, its icon (a <c>BitIconName</c>) and what a pass proves.</summary>
/// <param name="IconName">Null takes the icon of the check's category (See HealthCheckCategories.IconOf).</param>
public record HealthCheckDescription(string Title, string? IconName = null, string? About = null);

/// <summary>
/// What /healthz reports as each check's title, icon and about (See HealthReportEntryDto.Title), by registration name,
/// so the Operations page and a monitor show a check the way the app names it. The template's own checks are described
/// here; describe one of your own next to its registration, e.g.
/// <c>.AddCheck&lt;PaymentGatewayHealthCheck&gt;("paymentGateway", tags: [HealthCheckCategories.Integrations]).Describe("paymentGateway", "Payment gateway", "Money", "Authorizes and voids a one cent payment.")</c>.
/// A check described nowhere is reported under its registration name, with its category's icon.
/// </summary>
public class HealthCheckDescriptions : Dictionary<string, HealthCheckDescription>
{
    public HealthCheckDescriptions()
    {
        this["binStorage"] = new("Disk space", "HardDrive", "At least 2 GB free on the instance that answered this report. The only liveness check.");
        this["AppDbContext"] = new("Database", "Database", "Opens a connection to the app's database.");
        this["hangfire"] = new("Background jobs", "Processing", "At least one Hangfire server is running.");
        this["appCertificate"] = new("App certificate", "Certificate", "The certificate that signs tokens and protects data is within its validity period.");
        this["StackExchange.Redis_redis-persistent"] = new("Redis (persistent)", "Server", "Hangfire storage and distributed locks.");
        this["StackExchange.Redis_redis-cache"] = new("Redis (cache)", "ServerProcesses", "The distributed cache and backplane. Memory keeps serving without it.");
        this["userProfileImages"] = new("File storage", "Photo2", "The storage container of the uploaded files.");
        //#if (cloudflare == true)
        this["cloudflare"] = new("Cloudflare", "Globe", "The zones whose edge cache the app purges.");
        //#endif
        this["serverApi"] = new("Server API", "PlugConnected", "Server.Web reaches the API it prerenders with and forwards to.");

        this["smtp"] = new("Email", "Mail", "Signs in to the SMTP server without sending anything.");
        this["emailDelivery"] = new("Email sender domain", "MailCheck", "The sender's domain has SPF and DMARC records, so mail can reach inboxes rather than spam.");
        this["sms"] = new("SMS", "CellPhone", "The Twilio account is active.");
        this["firebase"] = new("Android push", "Ringer", "Firebase Cloud Messaging accepts the credentials.");
        this["apns"] = new("Apple push", "Ringer", "APNs accepts the credentials.");
        this["azureSignalR"] = new("Azure SignalR", "Streaming", "The service is healthy and accepts the access key.");

        this["keycloakIdentity"] = new("Keycloak", "Permissions", "The realm's discovery document answers.");
        this["entraId"] = new("Microsoft Entra ID", "AuthenticatorApp", "The tenant answers and accepts the app's credentials.");
        this["appleSignIn"] = new("Sign in with Apple", "Signin", "The client secret can be signed and Apple answers.");
        this["googleSignIn"] = new("Sign in with Google", "Signin", "Can't be verified without a user; only reported while not configured.");
        this["gitHubSignIn"] = new("Sign in with GitHub", "Signin", "Can't be verified without a user; only reported while not configured.");
        this["twitterSignIn"] = new("Sign in with X", "Signin", "Can't be verified without a user; only reported while not configured.");
        this["facebookSignIn"] = new("Sign in with Facebook", "Signin", "Can't be verified without a user; only reported while not configured.");
        this["reCaptcha"] = new("reCAPTCHA", "Shield", "Google's siteverify answers.");

        this["aiChat"] = new("AI chat", "ChatBot", "The chat model gives a short answer.");
        this["aiEmbedding"] = new("Embeddings", "DocumentSearch", "One word gets embedded, as semantic search does.");
        this["aiSpeechToText"] = new("Speech to text", "Microphone", "Half a second of silence gets transcribed.");
        this["aiTextToSpeech"] = new("Text to speech", "Speakers", "\"OK\" is read aloud in the configured voice.");
        this["aiRealtime"] = new("Voice calls", "Headset", "The realtime model issues a session for the configured voice.");
    }
}
