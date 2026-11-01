using System.Text;
using System.Security.Cryptography;
using Bit.Bcrdt;
using Microsoft.AspNetCore.Hosting.Server;

namespace Boilerplate.Workbench.Infrastructure.Services;

/// <summary>
/// Connects each circuit's copy of the review document to the server's copy, over a WebSocket to the Workbench itself.
/// A secret made at startup marks these connections, so no browser can open one.
/// </summary>
public sealed partial class WorkbenchReviewRelay : IBcrdtTransportFactory
{
    public const string Route = "ws/review/{documentId}";

    public const string SecretHeader = "X-Workbench-Relay";

    /// <summary>A random value made at startup that only this process knows.</summary>
    public static string Secret { get; } = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));

    [AutoInject] private IServer server = default!;

    public IBcrdtTransport Create(string documentId)
    {
        var address = WorkbenchUrls.LocalBaseAddress(server);

        // Always a loopback address, so the secret never leaves this machine.
        if (address.IsLoopback is false)
        {
            address = new UriBuilder(address) { Host = "localhost" }.Uri;
        }

        return new BcrdtWebSocketTransportFactory($"{address}{Route}", options => options.SetRequestHeader(SecretHeader, Secret)).Create(documentId);
    }

    /// <summary>Accepts a relay connection only to the review document and only with the secret header.</summary>
    public static bool IsTrusted(HttpContext context, string documentId)
    {
        return documentId == WorkbenchReviewDocument.Id
            && context.Request.Headers.TryGetValue(SecretHeader, out var secret)
            && CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(secret.ToString()), Encoding.UTF8.GetBytes(Secret));
    }
}
