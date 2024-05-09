namespace Boilerplate.Shared.Enums;

public class HeaderName
{
    public static readonly string RequestId = "Request-Id";

    /// <summary>
    /// For Android, iOS, and macOS users, the links shared via email will seamlessly open within the respective app, facilitated by the deep linking mechanism.
    /// However, on Windows, we need to notify the Windows app through a LocalHttpServer running inside the application itself.
    /// </summary>
    public static readonly string LocalHttpServerPort = "LocalHttpServerPort";

    public static readonly string UserAgent = "User-Agent";
}
