namespace Bit.Belemetry;

/// <summary>
/// The OpenTelemetry semantic-convention attribute and event names Belemetry emits and understands.
/// </summary>
/// <remarks>See <see href="https://opentelemetry.io/docs/specs/semconv/"/>.</remarks>
public static class BelemetrySemanticConventions
{
    /// <summary><c>service.name</c></summary>
    public const string ServiceName = "service.name";
    /// <summary><c>service.version</c></summary>
    public const string ServiceVersion = "service.version";
    /// <summary><c>service.namespace</c></summary>
    public const string ServiceNamespace = "service.namespace";
    /// <summary><c>service.instance.id</c></summary>
    public const string ServiceInstanceId = "service.instance.id";
    /// <summary><c>deployment.environment.name</c></summary>
    public const string DeploymentEnvironmentName = "deployment.environment.name";

    /// <summary><c>telemetry.sdk.name</c></summary>
    public const string TelemetrySdkName = "telemetry.sdk.name";
    /// <summary><c>telemetry.sdk.language</c></summary>
    public const string TelemetrySdkLanguage = "telemetry.sdk.language";
    /// <summary><c>telemetry.sdk.version</c></summary>
    public const string TelemetrySdkVersion = "telemetry.sdk.version";

    /// <summary><c>os.type</c></summary>
    public const string OsType = "os.type";
    /// <summary><c>os.description</c></summary>
    public const string OsDescription = "os.description";
    /// <summary><c>host.arch</c></summary>
    public const string HostArch = "host.arch";
    /// <summary><c>process.runtime.name</c></summary>
    public const string ProcessRuntimeName = "process.runtime.name";
    /// <summary><c>process.runtime.version</c></summary>
    public const string ProcessRuntimeVersion = "process.runtime.version";
    /// <summary><c>process.runtime.description</c></summary>
    public const string ProcessRuntimeDescription = "process.runtime.description";
    /// <summary><c>process.pid</c></summary>
    public const string ProcessPid = "process.pid";
    /// <summary><c>process.executable.name</c></summary>
    public const string ProcessExecutableName = "process.executable.name";
    /// <summary><c>process.owner</c></summary>
    public const string ProcessOwner = "process.owner";
    /// <summary><c>host.name</c></summary>
    public const string HostName = "host.name";
    /// <summary><c>host.id</c></summary>
    public const string HostId = "host.id";
    /// <summary><c>container.id</c></summary>
    public const string ContainerId = "container.id";
    /// <summary><c>cloud.provider</c></summary>
    public const string CloudProvider = "cloud.provider";
    /// <summary><c>cloud.platform</c></summary>
    public const string CloudPlatform = "cloud.platform";
    /// <summary><c>cloud.region</c></summary>
    public const string CloudRegion = "cloud.region";
    /// <summary><c>cloud.resource_id</c></summary>
    public const string CloudResourceId = "cloud.resource_id";
    /// <summary><c>device.model.identifier</c></summary>
    public const string DeviceModelIdentifier = "device.model.identifier";

    /// <summary><c>session.id</c></summary>
    public const string SessionId = "session.id";
    /// <summary><c>enduser.pseudo.id</c> - a random identifier not linked to the user's real identity.</summary>
    public const string EndUserPseudoId = "enduser.pseudo.id";
    /// <summary><c>user.id</c> - the authenticated user's identifier.</summary>
    public const string UserId = "user.id";

    /// <summary><c>exception.type</c></summary>
    public const string ExceptionType = "exception.type";
    /// <summary><c>exception.message</c></summary>
    public const string ExceptionMessage = "exception.message";
    /// <summary><c>exception.stacktrace</c></summary>
    public const string ExceptionStacktrace = "exception.stacktrace";

    /// <summary><c>http.request.method</c></summary>
    public const string HttpRequestMethod = "http.request.method";
    /// <summary><c>http.response.status_code</c></summary>
    public const string HttpResponseStatusCode = "http.response.status_code";
    /// <summary><c>http.route</c></summary>
    public const string HttpRoute = "http.route";
    /// <summary><c>url.full</c></summary>
    public const string UrlFull = "url.full";
    /// <summary><c>url.path</c></summary>
    public const string UrlPath = "url.path";
    /// <summary><c>url.query</c></summary>
    public const string UrlQuery = "url.query";
    /// <summary><c>url.scheme</c></summary>
    public const string UrlScheme = "url.scheme";
    /// <summary><c>server.address</c></summary>
    public const string ServerAddress = "server.address";
    /// <summary><c>server.port</c></summary>
    public const string ServerPort = "server.port";
    /// <summary><c>client.address</c></summary>
    public const string ClientAddress = "client.address";
    /// <summary><c>error.type</c></summary>
    public const string ErrorType = "error.type";
    /// <summary><c>db.system.name</c></summary>
    public const string DbSystemName = "db.system.name";
    /// <summary><c>db.query.text</c></summary>
    public const string DbQueryText = "db.query.text";

    /// <summary><c>page.title</c></summary>
    public const string PageTitle = "page.title";
    /// <summary><c>page.visit.duration</c> - how long a page was visited, in milliseconds.</summary>
    public const string PageVisitDuration = "page.visit.duration";
    /// <summary><c>browser.language</c></summary>
    public const string BrowserLanguage = "browser.language";
    /// <summary><c>web_vital.name</c></summary>
    public const string WebVitalName = "web_vital.name";
    /// <summary><c>web_vital.value</c></summary>
    public const string WebVitalValue = "web_vital.value";

    /// <summary><c>log.category</c> - the <c>ILogger</c> category name.</summary>
    public const string LogCategory = "log.category";
    /// <summary><c>log.event.id</c> - the <c>EventId.Id</c> of an <c>ILogger</c> entry.</summary>
    public const string LogEventId = "log.event.id";
    /// <summary><c>log.event.name</c> - the <c>EventId.Name</c> of an <c>ILogger</c> entry.</summary>
    public const string LogEventName = "log.event.name";
    /// <summary><c>log.record.template</c> - the message template of a structured <c>ILogger</c> entry.</summary>
    public const string LogMessageTemplate = "log.record.template";

    /// <summary>The event name of a page view log record.</summary>
    public const string PageViewEvent = "browser.page_view";
    /// <summary>The event name of a page visit time log record (how long the page view before it lasted).</summary>
    public const string PageVisitTimeEvent = "browser.page_visit_time";
    /// <summary>The event name of an interaction log record.</summary>
    public const string UserInteractionEvent = "browser.user_interaction";
    /// <summary>The event name of a web vital log record.</summary>
    public const string WebVitalEvent = "browser.web_vital";
    /// <summary>The event name of an exception log record.</summary>
    public const string ExceptionEvent = "exception";

    /// <summary>The <c>telemetry.sdk.name</c> Belemetry reports.</summary>
    public const string SdkName = "bit-belemetry";
}
