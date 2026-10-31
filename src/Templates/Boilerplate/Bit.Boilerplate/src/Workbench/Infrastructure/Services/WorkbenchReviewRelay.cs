using System.Text;
using System.Security.Cryptography;
using Bit.Bcrdt;
using Microsoft.AspNetCore.Hosting.Server;

namespace Boilerplate.Workbench.Infrastructure.Services;

public sealed partial class WorkbenchReviewRelay : IBcrdtTransportFactory
{
    public const string Route = "ws/review/{documentId}";

    public const string SecretHeader = "X-Workbench-Relay";

    public static string Secret { get; } = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));

    [AutoInject] private IServer server = default!;

    public IBcrdtTransport Create(string documentId)
    {
        var address = WorkbenchUrls.LocalBaseAddress(server);

        return new BcrdtWebSocketTransportFactory($"{address}{Route}", options => options.SetRequestHeader(SecretHeader, Secret)).Create(documentId);
    }

    public static bool IsTrusted(HttpContext context, string documentId)
    {
        return documentId == WorkbenchReviewDocument.Id
            && context.Request.Headers.TryGetValue(SecretHeader, out var secret)
            && CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(secret.ToString()), Encoding.UTF8.GetBytes(Secret));
    }
}
