using System.Diagnostics.CodeAnalysis;
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

    internal BevToolsLoader(BevToolsLoaderContext context, IServiceProvider services, IJSRuntime js)
    {
        _context = context;
        _services = services;
        _js = js;
    }

    /// <summary>
    /// False where BevTools may not be opened, which is outside Development in a process that serves many users
    /// (an ASP.NET Core host: Blazor Server, prerendering, static rendering), unless it was registered there at startup
    /// or <see cref="BitBevToolsLoaderOptions.AllowInMultiUserHost"/> is set. <see cref="Problem"/> then says why. The
    /// attach applies the same rule, so <see cref="OpenAsync"/> is not refused where this is true. Use it to decide
    /// whether to offer the button that calls <see cref="OpenAsync"/>.
    /// </summary>
    public bool CanOpen => _canOpen ??= ResolveCanOpen();

    /// <summary>True once BevTools runs in this scope: registered at startup, or attached by <see cref="OpenAsync"/>.</summary>
    public bool IsActive => _context.RegisteredAtStartup || _attachment is { IsEnabled: true };

    /// <summary>Why BevTools cannot be opened, or why the last <see cref="OpenAsync"/> failed.</summary>
    public string? Problem { get; private set; }

    /// <summary>Raised when <see cref="IsActive"/> or <see cref="Problem"/> changes.</summary>
    public event Action? Changed;

    /// <summary>
    /// Opens the BevTools panel. When BevTools was not registered at startup it first loads Bit.BevTools (in Blazor
    /// WebAssembly through LazyAssemblyLoader when it is marked BlazorWebAssemblyLazyLoad) and attaches it to the running
    /// app, which it then sees from that moment on. Returns false, with <see cref="Problem"/> saying why, when it could not.
    /// </summary>
    public async Task<bool> OpenAsync()
    {
        if (!CanOpen)
        {
            return false;
        }

        var entryPoint = await _context.LoadAsync(_js);
        if (entryPoint is null)
        {
            return Fail(_context.LoadError ?? "Bit.BevTools could not be loaded.");
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

            _attachment.Open();
        }
        catch (Exception ex)
        {
            return Fail(BevToolsLoaderContext.IsVersionMismatch(ex)
                ? BevToolsLoaderContext.VersionMismatch(entryPoint, ex)
                : "Attaching BevTools failed: " + ex.GetType().Name + ": " + ex.Message);
        }

        Problem = null;
        Changed?.Invoke();
        return true;
    }

    /// <summary>The panel to render: known once BevTools runs in this scope.</summary>
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
    internal Type? PanelType => IsActive ? _context.EntryPoint?.PanelType : null;

    private bool Fail(string problem)
    {
        Problem = problem;
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
