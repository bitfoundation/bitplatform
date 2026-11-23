using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

[assembly: Bit.BevTools.BevToolsEntryPoint(typeof(Bit.BevTools.BevToolsLoaderEntryPoint))]

namespace Bit.BevTools;

/// <summary>What Bit.BevTools.Loader calls once it has loaded this assembly, which it cannot reference.</summary>
internal sealed class BevToolsLoaderEntryPoint : IBevToolsEntryPoint
{
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
    public Type PanelType => typeof(BevToolsPanel);

    public bool TryAddServices(IServiceCollection services, BevToolsLoaderContext context, out string reason)
    {
        if (services.FirstOrDefault(d => d.ServiceType == typeof(BevToolsOptions))?.ImplementationInstance is BevToolsOptions registered)
        {
            reason = "AddBitBevToolsServices was called before AddBitBevToolsLoaderServices" + (registered.ResolvedEnabled ? "." : ", and left BevTools off.");
            if (registered.ResolvedEnabled)
            {
                // The app's own registration, or this loader's from an earlier call: the configuration it has not had yet
                // goes to it, merged as a second AddBitBevToolsServices call merges.
                ApplyStartupConfiguration(services, context);
            }

            return registered.ResolvedEnabled;
        }

        bool enabled;
        if (context.Options.RegisterAtStartup is true)
        {
            (enabled, reason) = (true, "BevToolsLoaderOptions.RegisterAtStartup was set to true.");
        }
        else
        {
            var probe = new BevToolsOptions();
            Forward(context.Options, probe);
            (enabled, reason) = BitBevTools.ResolveEnabled(services, probe);
        }

        if (!enabled)
        {
            reason += " BevTools is attached on demand instead.";
            return false;
        }

        var applied = context.Options.BevToolsConfigurations.Count;
        services.AddBitBevToolsServices(options =>
        {
            Forward(context.Options, options);
            if (context.Options.RegisterAtStartup is true)
            {
                options.Enabled = true;
            }

            Configure(context.Options, options, skip: 0);
        });
        context.StartupConfigurationsApplied = applied;

        return true;
    }

    public IBevToolsAttachment Attach(IServiceProvider services, BevToolsLoaderContext context) =>
        new Attachment(BevToolsRuntimeAttacher.Attach(services, configure: null, context, openedByLoader: true));

    private static void ApplyStartupConfiguration(IServiceCollection services, BevToolsLoaderContext context)
    {
        var configurations = context.Options.BevToolsConfigurations;
        var skip = context.StartupConfigurationsApplied;
        if (configurations.Count <= skip)
        {
            return;
        }

        // A later AddBitBevToolsLoaderServices call may name assemblies the callbacks it adds use. A BevTools registered
        // at startup is never attached, so nothing would download them later: a callback that uses one that is not here
        // would fail the app's startup on a FileNotFoundException. Those callbacks are left out, and the session says so.
        if (!context.TryLoadAssembliesFromApp(out _, out var missing))
        {
            var problem = "The ConfigureBevTools callbacks a later AddBitBevToolsLoaderServices call added were not applied: " + missing
                + " BevTools was registered at startup, before they were named, and a BevTools registered at startup loads nothing later."
                + " Name them in the first AddBitBevToolsLoaderServices call, or keep them among the assemblies the app starts with.";
            services.AddBitBevToolsServices(options => options.ConfigurationProblems.Add(problem));
            context.StartupConfigurationsApplied = configurations.Count;
            return;
        }

        services.AddBitBevToolsServices(options => Configure(context.Options, options, skip));
        context.StartupConfigurationsApplied = configurations.Count;
    }

    /// <summary>
    /// Runs the app's <see cref="BevToolsLoaderOptions.ConfigureBevTools"/> callbacks, from the <paramref name="skip"/>th
    /// on. <see cref="BevToolsOptions.Enabled"/> keeps the value it had: whether BevTools runs is the loader's decision, and
    /// one configuration serves both paths, so a callback that turns BevTools on for one must not do it for the other.
    /// </summary>
    internal static void Configure(BevToolsLoaderOptions from, BevToolsOptions to, int skip)
    {
        var enabled = to.Enabled;
        try
        {
            // Indexed: a callback may itself register another (through an integration's own method).
            for (var i = skip; i < from.BevToolsConfigurations.Count; i++)
            {
                from.BevToolsConfigurations[i](to);
            }
        }
        finally
        {
            to.Enabled = enabled;
        }
    }

    public IDisposable BeginOwnWork() => BevToolsSessionResolver.BeginBevToolsWork();

    public async Task PrefetchAssetsAsync(IJSRuntime js)
    {
        // Only a browser fetches them: elsewhere the panel's assets come from the app itself (Blazor Hybrid) or from the
        // server the circuit needs anyway.
        if (!OperatingSystem.IsBrowser())
        {
            return;
        }

        // The panel imports the same specifier, which the page's module map then answers without a request.
        var module = await js.InvokeAsync<IJSObjectReference>("import", BevToolsPanel.ModulePath);
        try
        {
            // False where the browser cannot hold it in the page for good; the panel then links it itself.
            if (await module.InvokeAsync<bool>("prefetchStylesheet", BevToolsPanel.StylesheetPath))
            {
                BevToolsPanel.StylesheetInPage = true;
            }
        }
        finally
        {
            await module.DisposeAsync();
        }
    }

    /// <summary>The options the loader repeats, since the app cannot name <see cref="BevToolsOptions"/> at startup.</summary>
    internal static void Forward(BevToolsLoaderOptions from, BevToolsOptions to)
    {
        to.UseFrameworkInstrumentation = from.UseFrameworkInstrumentation;
        to.TrackHttp = from.TrackHttp;
        // Also what BevToolsAutoStart.Logs captured by before the attach, so the session goes on from the same level and bound.
        to.MinimumLogLevel = from.MinimumLogLevel;
        to.MaxLogs = from.MaxLogs;
        to.Ui.ShowBadge = from.ShowBadge;
        if (from.ToggleShortcut is { Length: > 0 } shortcut)
        {
            to.Ui.ToggleShortcut = shortcut;
        }
    }

    private sealed class Attachment(BevToolsSession session) : IBevToolsAttachment
    {
        public bool IsEnabled => session.IsEnabled;

        public string Reason => session.Registry.EnabledReason;

        public string? Open(string? panelId, string? query)
        {
            if (panelId is not null && !BevToolsPanel.IsBuiltInPanelId(panelId) && !session.Registry.Panels.Any(p => string.Equals(p.Id, panelId, StringComparison.Ordinal)))
            {
                return $"BevTools has no panel '{panelId}'. Its panels: " + string.Join(", ", BevToolsPanel.BuiltInPanelIds.Concat(session.Registry.Panels.Select(p => p.Id))) + ".";
            }

            session.Ui.Open(panelId, query);
            return null;
        }

        public void Dispose()
        {
            if (session.IsAttachedAtRuntime)
            {
                session.Dispose();
            }
        }
    }
}
