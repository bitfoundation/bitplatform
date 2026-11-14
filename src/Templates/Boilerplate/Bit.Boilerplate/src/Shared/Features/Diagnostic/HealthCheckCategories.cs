namespace Boilerplate.Shared.Features.Diagnostic;

/// <summary>
/// What kind of dependency a health check watches, so the Operations page and a monitor reading /healthz can group a
/// check whose name they don't know and draw a fitting icon for it (See <see cref="HealthReportEntryDto.Category"/>).
/// The category travels as one of the registration's tags, as ASP.NET Core's HealthCheckRegistration has no other
/// metadata: every check the template registers carries exactly one, and a check of your own takes one the same way,
/// e.g. <c>AddCheck&lt;PaymentGatewayHealthCheck&gt;("paymentGateway", tags: [HealthCheckCategories.Integrations])</c>.
/// A category of your own works too: any tag that starts with <see cref="TagPrefix"/>, such as <c>"category:Payments"</c>.
/// </summary>
public static class HealthCheckCategories
{
    public const string TagPrefix = "category:";

    /// <summary>What every request depends on: storage, database, cache, jobs, certificates and the edge.</summary>
    public const string Platform = "category:Platform";

    /// <summary>How the app reaches people: email, SMS, push and realtime.</summary>
    public const string Messaging = "category:Messaging";

    /// <summary>External sign in providers and bot protection.</summary>
    public const string Identity = "category:Identity";

    /// <summary>The models behind the chatbot, search and voice calls.</summary>
    public const string AI = "category:AI";

    /// <summary>Any other service the app calls, such as a payment gateway, a map or a CRM.</summary>
    public const string Integrations = "category:Integrations";

    /// <summary>The project's other sites, such as its landing page or a blog (See <c>HealthChecks:Websites</c>).</summary>
    public const string Websites = "category:Websites";

    /// <summary>
    /// The icon (a <c>BitIconName</c>) of a check in <paramref name="category"/>, a name without <see cref="TagPrefix"/>,
    /// that has none of its own, and of that category's group on the Operations page.
    /// </summary>
    public static string IconOf(string? category) => category switch
    {
        "Platform" => "Server",
        "Messaging" => "Message",
        "Identity" => "Permissions",
        "AI" => "Robot",
        "Integrations" => "PlugConnected",
        "Websites" => "Globe",
        _ => "Diagnostic"
    };
}
