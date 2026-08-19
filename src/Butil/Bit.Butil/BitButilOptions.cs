namespace Bit.Butil;

/// <summary>
/// The options <see cref="BitButil.AddBitButilServices(Microsoft.Extensions.DependencyInjection.IServiceCollection, System.Action{BitButilOptions})"/>
/// applies: the runtime toggles of the library, in one place, next to the registration.
/// </summary>
/// <remarks>
/// Each option maps onto one of the static, process-wide toggles on <see cref="BitButil"/>
/// (<see cref="BitButil.UseLazyScripts"/>, <see cref="BitButil.UseFastInvoke"/>, ...) - it is a nicer front for
/// them, not a per-container setting, so the last registration to run wins and the values are shared by every
/// circuit of a Blazor Server app. Leave a property <see langword="null"/> to keep whatever the toggle is
/// already set to (by the MSBuild property, an earlier call, or the default).
/// <br/>
/// Note the limit of what runtime options can do: they decide how the library loads and calls its JavaScript,
/// not what the published output contains. The <c>BitButilLazyScripts</c> MSBuild property additionally keeps
/// the unused shape of the scripts (the bundle, in lazy mode) out of the published static web assets, and
/// publish-time bundle trimming (<c>BitButilTrimScripts</c>) is inherently a build step with no runtime
/// equivalent - see the package README.
/// </remarks>
public sealed class BitButilOptions
{
    /// <summary>
    /// <see langword="true"/> to load Bit.Butil's JavaScript per module, on first use, instead of from the
    /// <c>bit-butil.js</c> bundle referenced by a <c>&lt;script&gt;</c> tag; <see langword="false"/> to insist on
    /// the bundle. See <see cref="BitButil.UseLazyScripts"/> and <see cref="BitButil.UseBundledScripts"/>.
    /// </summary>
    public bool? LazyScripts { get; set; }

    /// <summary>
    /// Where the per-module scripts are served from when <see cref="LazyScripts"/> is on, relative to the app's
    /// base href. Default <c>./_content/Bit.Butil/modules/</c>; only needed when the package's static web assets
    /// are served from somewhere else (a CDN, a custom base path).
    /// </summary>
    public string? ScriptModulesPath { get; set; }

    /// <summary>
    /// <see langword="true"/> to use the synchronous in-process invoke path on Blazor WebAssembly for the APIs
    /// backed by synchronous JavaScript; <see langword="false"/> to run everything asynchronously. See
    /// <see cref="BitButil.UseFastInvoke"/> and <see cref="BitButil.UseNormalInvoke"/>.
    /// </summary>
    public bool? FastInvoke { get; set; }
}
