using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;

namespace Bit.BevTools;

/// <summary>
/// Opens BevTools from the app's own code, such as an "Open dev tools" button, and loads and attaches it first when it
/// was not registered at startup. Scoped: one per circuit on the server, one per app in WebAssembly and Blazor Hybrid.
/// Needs <c>AddBitBevToolsLoaderServices</c>, and <c>&lt;BevToolsLoaderPanel /&gt;</c> where the panel should appear.
/// </summary>
public sealed class BevToolsLoader : IDisposable
{
    private readonly BevToolsLoaderContext _context;
    private readonly IServiceProvider _services;
    private readonly IJSRuntime _js;
    private IBevToolsAttachment? _attachment;
    private bool? _canOpen;
    private bool _authorized;
    private int _openings;

    internal BevToolsLoader(BevToolsLoaderContext context, IServiceProvider services, IJSRuntime js)
    {
        _context = context;
        _services = services;
        _js = js;
    }

    /// <summary>
    /// False where BevTools may not be opened, which is outside Development in a process that serves many users
    /// (an ASP.NET Core host: Blazor Server, prerendering, static rendering), unless it was registered there at startup
    /// or <see cref="BevToolsLoaderOptions.AllowInMultiUserHost"/> is set. <see cref="Problem"/> then says why. The
    /// attach applies the same rule, so <see cref="OpenAsync()"/> is not refused where this is true. Use it to decide
    /// whether to offer the button that calls <see cref="OpenAsync()"/>.
    /// </summary>
    public bool CanOpen => _canOpen ??= ResolveCanOpen();

    /// <summary>True once BevTools runs in this scope: registered at startup, or attached by <see cref="OpenAsync()"/>.</summary>
    public bool IsActive => _context.RegisteredAtStartup || _attachment is { IsEnabled: true };

    /// <summary>Why BevTools cannot be opened, or why the last <see cref="OpenAsync()"/> failed.</summary>
    public string? Problem { get; private set; }

    /// <summary>
    /// True when the last <see cref="OpenAsync()"/> failed because Bit.BevTools, or one of
    /// <see cref="BevToolsLoaderOptions.AdditionalAssemblies"/>, could not be downloaded: the browser is offline, or the
    /// server answered with an error. <see cref="Problem"/> says which, in plain words. Typically the moment someone
    /// troubleshoots, so the app may offer a fallback of its own then. Offline, nothing is attempted, and the next
    /// <see cref="OpenAsync()"/> once the browser is back online downloads it; a download the runtime attempted and lost
    /// stays lost until the page is reloaded (the runtime keeps the failure), which <see cref="Problem"/> says too.
    /// <see cref="PrefetchAsync"/> ahead of time avoids both.
    /// </summary>
    public bool DownloadFailed { get; private set; }

    /// <summary>Raised when <see cref="IsActive"/>, <see cref="Problem"/> or <see cref="DownloadFailed"/> changes.</summary>
    public event Action? Changed;

    /// <summary>
    /// Opens the BevTools panel. When BevTools was not registered at startup it first loads Bit.BevTools (in Blazor
    /// WebAssembly through LazyAssemblyLoader when it is marked BlazorWebAssemblyLazyLoad) and attaches it to the running
    /// app, which it then sees from that moment on. Returns false, with <see cref="Problem"/> saying why, when it could not;
    /// also false, leaving <see cref="Problem"/> to it, when a newer call overtook this one while it awaited
    /// <see cref="BevToolsLoaderOptions.Authorize"/> or the download: only the latest call acts on its answer.
    /// </summary>
    public Task<bool> OpenAsync() => OpenCoreAsync(panelId: null, query: null);

    /// <summary>
    /// Opens the BevTools panel as <see cref="OpenAsync()"/> does, on the panel <paramref name="panelId"/>: a built-in
    /// one (<c>"errors"</c>, <c>"logs"</c>, <c>"network"</c>, ...) or one the app added (<c>BevToolsOptions.AddPanel</c>, an
    /// extension's), with <paramref name="query"/> in its filter when given (<c>"level:warning"</c> in Logs). The entry
    /// point for an app's own buttons: an error boundary's "diagnose", a support gesture that lands on the app's panel.
    /// Returns false, with <see cref="Problem"/> naming the panels there are, when this session has no such panel; the
    /// panel then stays as it was.
    /// </summary>
    public Task<bool> OpenAsync(string panelId, string? query = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(panelId);
        return OpenCoreAsync(panelId, query);
    }

    /// <summary>
    /// Downloads and loads Bit.BevTools and <see cref="BevToolsLoaderOptions.AdditionalAssemblies"/>, and in a browser the
    /// panel's script and stylesheet, without attaching anything, so a later <see cref="OpenAsync()"/> in this page needs
    /// no download: call it when the app is idle, or once it is online, for the moment someone troubleshoots without a
    /// connection. (A reload starts over: what survives one is up to the browser's cache and the app's service worker.)
    /// Loading the assemblies runs none of BevTools: it observes nothing until <see cref="OpenAsync()"/> attaches it, but
    /// the assemblies stay in memory from then on, and the stylesheet in the page - its rules apply to BevTools' own
    /// elements only. It
    /// asks no <see cref="BevToolsLoaderOptions.Authorize"/>, since it shows nothing: call it only for the people the app
    /// would offer BevTools to (<see cref="IsAuthorizedAsync"/>). Returns true once they are loaded - at once when they
    /// already are, or the app started with them - and false where <see cref="CanOpen"/> is false (nothing is downloaded
    /// that could not be opened) or when they could not be loaded; <see cref="Problem"/> and <see cref="DownloadFailed"/>
    /// are left to <see cref="OpenAsync()"/>, as nobody asked to open anything.
    /// </summary>
    public async Task<bool> PrefetchAsync()
    {
        if (!CanOpen)
        {
            return false;
        }

        using (BevToolsHooks.BeginOwnWork())
        {
            if (await _context.LoadAsync(_js) is not { } entryPoint)
            {
                return false;
            }

            try
            {
                await entryPoint.PrefetchAssetsAsync(_js);
                return true;
            }
            catch (Exception ex) when (ex is JSException or JSDisconnectedException or OperationCanceledException || BevToolsLoaderContext.IsVersionMismatch(ex))
            {
                // Offline again, or a Bit.BevTools of another version: OpenAsync says which, when someone opens it.
                return false;
            }
        }
    }

    private async Task<bool> OpenCoreAsync(string? panelId, string? query)
    {
        if (!CanOpen)
        {
            return false;
        }

        // Asked before anything is downloaded or attached, and on every open: whoever is in the scope can change (a
        // sign-out in the same circuit), and a decision cached from before would outlive it.
        var opening = ++_openings;
        var refusal = await AuthorizeAsync(logFailure: true);
        if (opening != _openings)
        {
            return false;
        }

        if (refusal is not null)
        {
            _authorized = false;
            return Fail(refusal);
        }

        _authorized = true;
        IBevToolsEntryPoint? entryPoint;
        using (BevToolsHooks.BeginOwnWork())
        {
            // What loading logs (LazyAssemblyLoader, the download) is BevTools' doing, not the app's.
            entryPoint = await _context.LoadAsync(_js);
        }
        if (opening != _openings)
        {
            return false;
        }

        if (entryPoint is null)
        {
            return Fail(_context.LoadError ?? "Bit.BevTools could not be loaded.", downloadFailed: _context.LoadFailedToDownload);
        }

        // Typically called from an @onclick, where an exception would end a Blazor Server circuit: whatever attaching
        // fails with becomes the Problem this method promises instead.
        try
        {
            _attachment ??= entryPoint.Attach(_services, _context);
            if (!_attachment.IsEnabled)
            {
                return Fail(_attachment.Reason);
            }

            if (_attachment.Open(panelId, query) is { } panelProblem)
            {
                return Fail(panelProblem);
            }
        }
        catch (Exception ex)
        {
            return Fail(BevToolsLoaderContext.IsVersionMismatch(ex)
                ? BevToolsLoaderContext.VersionMismatch(entryPoint, ex)
                : "Attaching BevTools failed: " + ex.GetType().Name + ": " + ex.Message);
        }

        Problem = null;
        DownloadFailed = false;
        Changed?.Invoke();
        return true;
    }

    /// <summary>
    /// The panel to render: known once BevTools runs in this scope and, with <see cref="BevToolsLoaderOptions.Authorize"/>
    /// set, once the latest <see cref="OpenAsync()"/> was accepted by it - a BevTools registered at startup included.
    /// </summary>
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
    internal Type? PanelType => IsActive && (_authorized || _context.Options.Authorize is null) ? _context.EntryPoint?.PanelType : null;

    /// <summary>
    /// Whether <see cref="BevToolsLoaderOptions.Authorize"/> accepts whoever is in this scope (true when none is
    /// set), on top of <see cref="CanOpen"/>. Use it to show the "Open dev tools" button only to the people allowed to
    /// press it; <see cref="OpenAsync()"/> asks again either way. A callback that throws is a refusal here too, and is not
    /// logged: this runs whenever the app renders its button, for every visitor, and <see cref="OpenAsync()"/> logs it.
    /// </summary>
    public async Task<bool> IsAuthorizedAsync() => CanOpen && await AuthorizeAsync(logFailure: false) is null;

    /// <returns>Null when authorized, otherwise why not.</returns>
    private async Task<string?> AuthorizeAsync(bool logFailure)
    {
        if (_context.Options.Authorize is not { } authorize)
        {
            return null;
        }

        // Outside the callback's try: a failure here is Bit.BevTools', and blaming the app's callback for it would send
        // its developers debugging code that never ran. Still a refusal, since the callback could not be asked.
        IDisposable? ownWork;
        try
        {
            // Before Bit.BevTools is loaded the loader marks it itself: what the callback logs is not the app's, and the
            // logs BevToolsAutoStart.Logs keeps for the session about to attach must not show it.
            ownWork = _context.EntryPoint?.BeginOwnWork() ?? BevToolsHooks.BeginOwnWork();
        }
        catch (Exception ex)
        {
            return BevToolsLoaderContext.IsVersionMismatch(ex)
                ? BevToolsLoaderContext.VersionMismatch(_context.EntryPoint!, ex)
                : "BevTools is not opened here: Bit.BevTools failed before asking BevToolsLoaderOptions.Authorize: " + ex.GetType().Name + ": " + ex.Message;
        }

        using (ownWork)
        {
            try
            {
                return await authorize(_services) ? null : "BevTools is not opened here: BevToolsLoaderOptions.Authorize did not accept the current user.";
            }
            catch (Exception ex)
            {
                // A failing check is a refusal, never an opening: the callback is a security control. Problem goes to
                // whoever pressed the button, whom the callback did not accept, so it names the exception and the log
                // holds the rest - said only once the log has it, so nobody searches a log that has no logger.
                var logged = false;
                if (logFailure)
                {
                    try
                    {
                        if (_services.GetService<ILoggerFactory>() is { } loggerFactory)
                        {
                            loggerFactory.CreateLogger("Bit.BevTools").LogWarning(ex, "BevToolsLoaderOptions.Authorize threw; BevTools stays closed.");
                            logged = true;
                        }
                    }
                    catch
                    {
                        // A log line that cannot be written must not turn the refusal into an exception.
                    }
                }

                return "BevTools is not opened here: BevToolsLoaderOptions.Authorize threw " + ex.GetType().Name + (logged ? " (logged)." : ".");
            }
        }
    }

    private bool Fail(string problem, bool downloadFailed = false)
    {
        Problem = problem;
        DownloadFailed = downloadFailed;
        Changed?.Invoke();
        return false;
    }

    private bool ResolveCanOpen()
    {
        if (_context.RegisteredAtStartup)
        {
            return true;
        }

        var (allowed, reason) = _context.ResolveAttach(_services);
        if (!allowed)
        {
            Problem = "BevTools is not opened here: " + reason;
        }

        return allowed;
    }

    public void Dispose()
    {
        // Ends a session the attach created with the scope - a circuit on the server - instead of leaving it to the
        // garbage collector; a session DI created is disposed by DI.
        _attachment?.Dispose();
        _attachment = null;
        Changed = null;
    }
}
