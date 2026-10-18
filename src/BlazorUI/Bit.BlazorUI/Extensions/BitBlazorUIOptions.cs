namespace Bit.BlazorUI;

/// <summary>
/// What <see cref="IBitBlazorUIServiceCollectionExtensions.AddBitBlazorUIServices(Microsoft.Extensions.DependencyInjection.IServiceCollection, Action{BitBlazorUIOptions})"/>
/// applies along with the services, so an app configures the library in one place in its Program.cs.
/// </summary>
/// <remarks>
/// <see cref="LazyScripts"/> and <see cref="ContentPath"/> set the process-wide toggles of <see cref="BitBlazorUIScripts"/>
/// - a nicer front for them, not a per-container setting: the last registration to run wins, and every circuit of a Blazor
/// Server app shares them. Left <see langword="null"/>, each keeps whatever it is already set to (by the
/// <c>BitBlazorUILazyScripts</c> MSBuild property, an earlier call, or the default). They decide how the packages load
/// their scripts, never what the app publishes: the MSBuild property also keeps the bundles out of the published output,
/// and the publish-time trimming (<c>BitBlazorUITrimAssets</c>) is a build step with no runtime counterpart.
/// </remarks>
public sealed class BitBlazorUIOptions
{
    /// <summary>
    /// <see langword="true"/> to import the packages' scripts one module at a time, on first use, rather than from the
    /// bundles their <c>&lt;script&gt;</c> tags load; <see langword="false"/> to insist on the bundles. See
    /// <see cref="BitBlazorUIScripts.UseLazyScripts"/> and <see cref="BitBlazorUIScripts.UseBundledScripts"/>.
    /// </summary>
    public bool? LazyScripts { get; set; }

    /// <summary>
    /// Where the packages' static web assets are served from with the lazy scripts on, relative to the app's base href:
    /// the folder holding <c>Bit.BlazorUI/</c> and the other packages'. Default <c>./_content/</c>; only needed when they are
    /// served from somewhere else (a CDN, a base path of their own).
    /// </summary>
    public string? ContentPath { get; set; }

    /// <summary>
    /// Registers the eligible services as singletons instead of scoped - the <c>trySingleton</c> argument of
    /// <see cref="IBitBlazorUIServiceCollectionExtensions.AddBitBlazorUIServices(Microsoft.Extensions.DependencyInjection.IServiceCollection, bool)"/>,
    /// and as safe as it is there: only for a single-user hosting model (Blazor WebAssembly and Hybrid), never Blazor Server.
    /// </summary>
    public bool TrySingleton { get; set; }
}
