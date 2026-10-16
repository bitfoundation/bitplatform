using Microsoft.Extensions.DependencyInjection;

namespace Bit.BevTools;

/// <summary>Process-wide BevTools state: extension contributions, the DI service graph and the enabled flag. Singleton.</summary>
public sealed class BevToolsRegistry
{
    private readonly Lazy<BevServiceGraph?> _serviceGraph;

    internal BevToolsRegistry(BitBevToolsOptions options, bool isEnabled, string enabledReason, IServiceCollection? services)
    {
        Options = options;
        IsEnabled = isEnabled;
        EnabledReason = enabledReason;
        StartedAt = DateTimeOffset.UtcNow;
        var builder = new ExtensionBuilder();
        foreach (var extension in options.Extensions)
        {
            try
            {
                extension.Configure(builder);
                ExtensionNames.Add(extension.Name);
            }
            catch (Exception ex)
            {
                ExtensionErrors.Add(extension.Name + ": " + ex.Message);
            }
        }

        Panels = builder.Panels.OrderBy(p => p.Order).ToArray();
        Rules = builder.Rules;
        Commands = builder.Commands;
        StateProviderFactories = builder.StateProviderFactories;
        Formatters = builder.Formatters;
        HiddenNamespaces = builder.HiddenNamespaces;
        _serviceGraph = new Lazy<BevServiceGraph?>(() => services is null ? null : BevServiceGraph.Build(services), LazyThreadSafetyMode.ExecutionAndPublication);

        if (isEnabled)
        {
            ActivityObserver.EnsureStarted();
        }
    }

    public BitBevToolsOptions Options { get; }

    public bool IsEnabled { get; }

    /// <summary>Why BevTools is (or is not) running, so a developer never has to guess when the panel does not appear.</summary>
    public string EnabledReason { get; }

    public DateTimeOffset StartedAt { get; }

    public IReadOnlyList<BevToolsPanelDescriptor> Panels { get; }

    public IReadOnlyList<IBevDiagnosticRule> Rules { get; }

    public IReadOnlyList<IBevToolsCommand> Commands { get; }

    public IReadOnlyList<Func<IServiceProvider, IBevStateProvider>> StateProviderFactories { get; }

    public IReadOnlyDictionary<Type, BevToolsValueFormatter> Formatters { get; }

    public IReadOnlyList<string> HiddenNamespaces { get; }

    public List<string> ExtensionNames { get; } = [];

    public List<string> ExtensionErrors { get; } = [];

    /// <summary>Snapshot of the DI registrations captured at <c>AddBitBevToolsServices</c> time (built lazily on first use).</summary>
    public BevServiceGraph? ServiceGraph => _serviceGraph.Value;

    private sealed class ExtensionBuilder : IBevToolsExtensionBuilder
    {
        public List<BevToolsPanelDescriptor> Panels { get; } = [];

        public List<IBevDiagnosticRule> Rules { get; } = [];

        public List<IBevToolsCommand> Commands { get; } = [];

        public List<Func<IServiceProvider, IBevStateProvider>> StateProviderFactories { get; } = [];

        public Dictionary<Type, BevToolsValueFormatter> Formatters { get; } = [];

        public List<string> HiddenNamespaces { get; } = [];

        public IBevToolsExtensionBuilder AddPanel(BevToolsPanelDescriptor panel)
        {
            ArgumentNullException.ThrowIfNull(panel);
            if (!typeof(Microsoft.AspNetCore.Components.IComponent).IsAssignableFrom(panel.ComponentType))
            {
                throw new ArgumentException($"Panel '{panel.Id}' component type {panel.ComponentType.Name} is not a Blazor component.");
            }

            Panels.RemoveAll(p => p.Id == panel.Id);
            Panels.Add(panel);
            return this;
        }

        public IBevToolsExtensionBuilder AddDiagnosticRule(IBevDiagnosticRule rule)
        {
            ArgumentNullException.ThrowIfNull(rule);
            Rules.Add(rule);
            return this;
        }

        public IBevToolsExtensionBuilder AddCommand(IBevToolsCommand command)
        {
            ArgumentNullException.ThrowIfNull(command);
            Commands.Add(command);
            return this;
        }

        public IBevToolsExtensionBuilder AddStateProvider(Func<IServiceProvider, IBevStateProvider> factory)
        {
            ArgumentNullException.ThrowIfNull(factory);
            StateProviderFactories.Add(factory);
            return this;
        }

        public IBevToolsExtensionBuilder AddValueFormatter(Type type, BevToolsValueFormatter formatter)
        {
            ArgumentNullException.ThrowIfNull(type);
            ArgumentNullException.ThrowIfNull(formatter);
            Formatters[type] = formatter;
            return this;
        }

        public IBevToolsExtensionBuilder HideComponentsInNamespace(string namespacePrefix)
        {
            if (!string.IsNullOrWhiteSpace(namespacePrefix))
            {
                HiddenNamespaces.Add(namespacePrefix);
            }

            return this;
        }
    }
}
