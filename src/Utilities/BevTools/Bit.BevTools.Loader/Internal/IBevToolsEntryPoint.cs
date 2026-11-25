using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

namespace Bit.BevTools;

/// <summary>What the loader asks of Bit.BevTools, which implements it. Internal to both assemblies.</summary>
internal interface IBevToolsEntryPoint
{
    /// <summary>The <c>BevToolsPanel</c> component.</summary>
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
    Type PanelType { get; }

    /// <summary>
    /// Registers BevTools while the app starts when it should run from the first render, or reports why not and
    /// leaves <paramref name="services"/> untouched.
    /// </summary>
    bool TryAddServices(IServiceCollection services, BevToolsLoaderContext context, out string reason);

    /// <summary>Attaches BevTools to the running app in the scope of <paramref name="services"/>, or returns the session registered at startup.</summary>
    IBevToolsAttachment Attach(IServiceProvider services, BevToolsLoaderContext context);

    /// <summary>
    /// Fetches what the panel loads in the browser when it first renders - its module and stylesheet - so a panel opened
    /// later, offline included, has them. Throws when the browser could not fetch them.
    /// </summary>
    Task PrefetchAssetsAsync(IJSRuntime js);

    /// <summary>
    /// Marks what runs until it is disposed as BevTools' own work, which no session records: the
    /// <see cref="BevToolsLoaderOptions.Authorize"/> callback's requests, logs and activities are not the app's.
    /// </summary>
    IDisposable BeginOwnWork();
}
