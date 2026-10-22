using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Bit.BevTools;

/// <summary>
/// The one rule for attaching BevTools to a running app, which <see cref="BevToolsLoader.CanOpen"/> and the attach
/// itself both apply, so the button an app offers never leads to an attach that is refused. Attaching is the app's
/// decision, so it is allowed, except where that would expose other people's data: in a process that serves many users
/// the panel shows whoever opens it the server side objects of their circuit, injected services and
/// <c>IConfiguration</c> included. There it takes Development, or the app saying so explicitly.
/// </summary>
internal static class BevToolsAttachPolicy
{
    /// <summary>What every ASP.NET Core host (Kestrel, IIS, TestServer) registers, and nothing else does.</summary>
    internal const string ServerTypeName = "Microsoft.AspNetCore.Hosting.Server.IServer";

    /// <returns>Whether BevTools may be attached, and why, as the end of a sentence about it.</returns>
    public static (bool Allowed, string Reason) Resolve(IServiceProvider services, bool multiUserHost, bool allowInMultiUserHost, string howToAllow)
    {
        if (!multiUserHost)
        {
            return (true, "this process serves one user.");
        }

        if (allowInMultiUserHost)
        {
            return (true, "this process serves many users, which BitBevToolsLoaderOptions.AllowInMultiUserHost allows.");
        }

        var environment = services.GetService<IHostEnvironment>();
        return environment?.IsDevelopment() == true
            ? (true, $"this process serves many users, and IHostEnvironment reports '{environment.EnvironmentName}'.")
            : (false, $"this process serves many users (IHostEnvironment reports '{environment?.EnvironmentName ?? "nothing"}'), and the panel would show whoever opens it the server side objects of their circuit. {howToAllow}");
    }
}
