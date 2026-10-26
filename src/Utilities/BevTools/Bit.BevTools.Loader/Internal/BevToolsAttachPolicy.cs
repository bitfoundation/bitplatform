using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Bit.BevTools;

/// <summary>
/// The one rule for attaching BevTools to a running app, which <see cref="BevToolsLoader.CanOpen"/> and the attach
/// itself both apply, so the button an app offers never leads to an attach that is refused. Attaching is the app's
/// decision, so it is allowed, except where that would expose other people's data: in a process that serves many users
/// the panel shows whoever opens it the server side objects of their circuit, injected services and
/// <c>IConfiguration</c> included. There it takes Development, or the app saying so explicitly and saying who may look.
/// </summary>
internal static class BevToolsAttachPolicy
{
    /// <summary>What every ASP.NET Core host (Kestrel, IIS, TestServer) registers, and nothing else does.</summary>
    internal const string ServerTypeName = "Microsoft.AspNetCore.Hosting.Server.IServer";

    /// <summary>
    /// Allowing it in a process that serves many users takes <paramref name="allowInMultiUserHost"/> and
    /// <paramref name="authorizes"/> both: a callback that decides who may open it (<see cref="BitBevToolsLoaderOptions.Authorize"/>).
    /// </summary>
    /// <returns>Whether BevTools may be attached, and why, as the end of a sentence about it.</returns>
    public static (bool Allowed, string Reason) Resolve(IServiceProvider services, bool multiUserHost, bool allowInMultiUserHost, bool authorizes, string howToAllow)
    {
        if (!multiUserHost)
        {
            return (true, "this process serves one user.");
        }

        var environment = services.GetService<IHostEnvironment>();
        if (environment?.IsDevelopment() == true)
        {
            return (true, $"this process serves many users, and IHostEnvironment reports '{environment.EnvironmentName}'.");
        }

        if (allowInMultiUserHost && authorizes)
        {
            return (true, "this process serves many users, which BitBevToolsLoaderOptions.AllowInMultiUserHost allows to whoever BitBevToolsLoaderOptions.Authorize accepts.");
        }

        var why = $"this process serves many users (IHostEnvironment reports '{environment?.EnvironmentName ?? "nothing"}'), and the panel would show whoever opens it the server side objects of their circuit.";
        return allowInMultiUserHost
            ? (false, why + " BitBevToolsLoaderOptions.AllowInMultiUserHost needs BitBevToolsLoaderOptions.Authorize too, to decide who may open it.")
            : (false, why + " " + howToAllow);
    }
}
