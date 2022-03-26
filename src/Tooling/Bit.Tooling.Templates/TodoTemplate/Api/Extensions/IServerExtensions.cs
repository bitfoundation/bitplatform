using Microsoft.AspNetCore.Hosting.Server.Features;

namespace Microsoft.AspNetCore.Hosting.Server;

public static class IServerExtensions
{
    public static Uri GetHostUri(this IServer server)
    {
        var address = server.Features.Get<IServerAddressesFeature>()!.Addresses
        .Where(add => Uri.TryCreate(add, UriKind.Absolute, out Uri? _))
        .Select(add => new Uri(add))
        .OrderByDescending(add => add.Scheme == Uri.UriSchemeHttps ? 1 : 0)
        .Select(add => add.GetLeftPart(UriPartial.Authority))
        .First();

        return new Uri(address);
    }
}
