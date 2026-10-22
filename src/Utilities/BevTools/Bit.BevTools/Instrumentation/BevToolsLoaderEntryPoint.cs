using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;

[assembly: Bit.BevTools.BevToolsEntryPoint(typeof(Bit.BevTools.BevToolsLoaderEntryPoint))]

namespace Bit.BevTools;

/// <summary>What Bit.BevTools.Loader calls once it has loaded this assembly, which it cannot reference.</summary>
internal sealed class BevToolsLoaderEntryPoint : IBevToolsEntryPoint
{
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
    public Type PanelType => typeof(BevToolsPanel);

    public bool TryAddServices(IServiceCollection services, BevToolsLoaderContext context, out string reason)
    {
        if (services.FirstOrDefault(d => d.ServiceType == typeof(BitBevToolsOptions))?.ImplementationInstance is BitBevToolsOptions registered)
        {
            reason = "AddBitBevToolsServices was called before AddBitBevToolsLoaderServices" + (registered.ResolvedEnabled ? "." : ", and left BevTools off.");
            return registered.ResolvedEnabled;
        }

        bool enabled;
        if (context.Options.RegisterAtStartup is true)
        {
            (enabled, reason) = (true, "BitBevToolsLoaderOptions.RegisterAtStartup was set to true.");
        }
        else
        {
            var probe = new BitBevToolsOptions();
            Forward(context.Options, probe);
            (enabled, reason) = BitBevTools.ResolveEnabled(services, probe);
        }

        if (!enabled)
        {
            reason += " BevTools is attached on demand instead.";
            return false;
        }

        services.AddBitBevToolsServices(options =>
        {
            Forward(context.Options, options);
            if (context.Options.RegisterAtStartup is true)
            {
                options.Enabled = true;
            }
        });

        return true;
    }

    public IBevToolsAttachment Attach(IServiceProvider services, BevToolsLoaderContext context) =>
        new Attachment(RuntimeAttachment.Attach(services, configure: null, context));

    /// <summary>The options the loader repeats, since the app cannot name <see cref="BitBevToolsOptions"/> at startup.</summary>
    internal static void Forward(BitBevToolsLoaderOptions from, BitBevToolsOptions to)
    {
        to.UseFrameworkInstrumentation = from.UseFrameworkInstrumentation;
        to.TrackHttp = from.TrackHttp;
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

        public void Open() => session.Ui.Open();

        public void Dispose()
        {
            if (session.IsAttachedAtRuntime)
            {
                session.Dispose();
            }
        }
    }
}
