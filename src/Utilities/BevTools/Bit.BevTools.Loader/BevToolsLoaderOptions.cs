using Microsoft.Extensions.Logging;

namespace Bit.BevTools;

/// <summary>Configuration for <c>AddBitBevToolsLoaderServices</c>.</summary>
/// <remarks>
/// The few BevTools options a loader forwards are repeated here on purpose: an app that lazy loads BevTools must not
/// mention a Bit.BevTools type in code that runs at startup, <c>Action&lt;BevToolsOptions&gt;</c> included.
/// </remarks>
public sealed class BevToolsLoaderOptions
{
    /// <summary>
    /// Whether BevTools is registered while the app starts, which lets it see everything from the first render. Null
    /// (default) leaves the decision to BevTools, which registers itself only when the host environment reports
    /// Development. Either way this needs Bit.BevTools among the assemblies the app starts with: a lazy loaded one is
    /// attached on demand by <see cref="BevToolsLoader.OpenAsync"/> instead, and sees only what happens after that.
    /// </summary>
    public bool? RegisterAtStartup { get; set; }

    /// <summary>
    /// Lets <see cref="BevToolsLoader.OpenAsync"/> attach BevTools outside Development in a process that serves many
    /// users: Blazor Server circuits, prerendering and static rendering. Off by default, because the panel shows
    /// whoever opens it the server side objects of their circuit, injected services and configuration included.
    /// It takes <see cref="Authorize"/> as well: a process that serves many users has to be told who may look. Such a
    /// session reads no application values unless <c>BevToolsOptions.ValueAccess</c> says otherwise.
    /// Blazor WebAssembly and Blazor Hybrid apps only ever show users their own process, so it does not apply there.
    /// </summary>
    public bool AllowInMultiUserHost { get; set; }

    /// <summary>
    /// Decides whether whoever is in the current scope may open BevTools: typically by role or claim, read through the
    /// scope's <c>AuthenticationStateProvider</c>. <see cref="BevToolsLoader.OpenAsync"/> asks it before Bit.BevTools is
    /// loaded or attached; false, or an exception, leaves BevTools closed and <see cref="BevToolsLoader.Problem"/> says
    /// why. Required with <see cref="AllowInMultiUserHost"/> outside Development, and honoured everywhere it is set.
    /// </summary>
    public Func<IServiceProvider, ValueTask<bool>>? Authorize { get; set; }

    /// <summary>
    /// Registers the framework's components ActivitySource and meters (.NET 10) at startup. A renderer reads them when
    /// it is created, so without them a session attached later cannot tell which renders a UI event caused. Meters
    /// also need the <c>MetricsSupport</c> MSBuild property in Blazor WebAssembly. Forwarded to
    /// <c>BevToolsOptions.UseFrameworkInstrumentation</c> when BevTools registers at startup.
    /// </summary>
    public bool UseFrameworkInstrumentation { get; set; } = true;

    /// <summary>Forwarded to <c>BevToolsOptions.TrackHttp</c>.</summary>
    public bool TrackHttp { get; set; } = true;

    /// <summary>Forwarded to <c>BevToolsOptions.Ui.ShowBadge</c>: show a small floating badge while the panel is closed.</summary>
    public bool ShowBadge { get; set; } = true;

    /// <summary>Forwarded to <c>BevToolsOptions.Ui.ToggleShortcut</c> when set.</summary>
    public string? ToggleShortcut { get; set; }

    /// <summary>
    /// The parts of BevTools that start with the app while BevTools itself waits to be attached: <see cref="BevToolsAutoStart.Logs"/>
    /// captures what the app logs from its first entry, and the session <see cref="BevToolsLoader.OpenAsync"/> attaches
    /// later shows those entries too. Each part runs inside this loader, on its own: it loads nothing of Bit.BevTools and
    /// instruments nothing else, so it adds only its own cost. None by default.
    /// <para>
    /// It applies only while BevTools is not registered at startup (<see cref="RegisterAtStartup"/>): one registered at
    /// startup runs whole from the first render and needs no head start. In a process that serves many users (Blazor
    /// Server) entries are kept per circuit, the ones written on its dispatcher, all circuits together within ten times
    /// <see cref="MaxLogs"/>, and only where an attach is allowed there (Development, or <see cref="AllowInMultiUserHost"/>
    /// with <see cref="Authorize"/>): an entry written outside every circuit belongs to no user. Outside Development a
    /// session attached there reads no values, so neither does the capture: names, types and templates only, none of the
    /// values' code run. Read by the first <c>AddBitBevToolsLoaderServices</c> call.
    /// </para>
    /// </summary>
    public BevToolsAutoStart AutoStart { get; set; }

    /// <summary>
    /// The lowest level <see cref="BevToolsAutoStart.Logs"/> captures. Forwarded to <c>BevToolsOptions.MinimumLogLevel</c>,
    /// so an attached session goes on recording from the same level. Information by default.
    /// </summary>
    public LogLevel MinimumLogLevel { get; set; } = LogLevel.Information;

    /// <summary>
    /// The most log entries <see cref="BevToolsAutoStart.Logs"/> keeps until the attach - the newest ones; the others are
    /// counted, not kept - per process, or per circuit in a process that serves many users. Forwarded to
    /// <c>BevToolsOptions.MaxLogs</c>. 1000 by default.
    /// </summary>
    public int MaxLogs { get; set; } = 1000;
}
