using Microsoft.Extensions.DependencyInjection;

namespace Bit.BevTools;

/// <summary>
/// Process-wide BevTools state: extension contributions, the DI service graph and the enabled flag. Singleton when
/// BevTools is registered, and one per process too when Bit.BevTools.Loader attaches it; only a bare
/// <see cref="BitBevTools.Attach"/>, whose options can differ per call, gets one per session, which shares the graph.
/// </summary>
public sealed class BevToolsRegistry
{
    private readonly Lazy<BevToolsServiceGraph?> _serviceGraph;

    internal BevToolsRegistry(BevToolsOptions options, bool isEnabled, string enabledReason, IServiceCollection? services)
        : this(options, isEnabled, enabledReason, CreateGraph(services))
    {
    }

    internal BevToolsRegistry(BevToolsOptions options, bool isEnabled, string enabledReason, Lazy<BevToolsServiceGraph?> serviceGraph)
    {
        Options = options;
        IsEnabled = isEnabled;
        EnabledReason = enabledReason;
        StartedAt = DateTimeOffset.UtcNow;
        var builder = new ExtensionBuilder(ExtensionErrors);
        foreach (var extension in options.Extensions)
        {
            builder.Current = extension.Name;
            try
            {
                extension.Configure(builder);
                ExtensionNames.Add(extension.Name);
            }
            catch (Exception ex)
            {
                ExtensionErrors.Add(extension.Name + ": " + BevToolsRedactor.RedactText(ex.Message));
            }
        }

        Panels = builder.Panels.OrderBy(p => p.Order).ToArray();
        Rules = builder.Rules;
        Commands = builder.Commands;
        StateProviderFactories = builder.StateProviderFactories;
        Formatters = builder.Formatters;
        HiddenNamespaces = builder.HiddenNamespaces;
        _serviceGraph = serviceGraph;

        if (isEnabled)
        {
            BevToolsActivityObserver.EnsureStarted();
            BevToolsUnobservedTasks.EnsureSubscribed();
        }
    }

    public BevToolsOptions Options { get; }

    public bool IsEnabled { get; }

    /// <summary>Why BevTools is (or is not) running, so a developer never has to guess when the panel does not appear.</summary>
    public string EnabledReason { get; }

    public DateTimeOffset StartedAt { get; }

    public IReadOnlyList<BevToolsPanelDescriptor> Panels { get; }

    public IReadOnlyList<IBevToolsDiagnosticRule> Rules { get; }

    public IReadOnlyList<IBevToolsCommand> Commands { get; }

    public IReadOnlyList<Func<IServiceProvider, IBevToolsStateProvider>> StateProviderFactories { get; }

    public IReadOnlyDictionary<Type, BevToolsValueFormatter> Formatters { get; }

    public IReadOnlyList<string> HiddenNamespaces { get; }

    public List<string> ExtensionNames { get; } = [];

    public List<string> ExtensionErrors { get; } = [];

    /// <summary>Snapshot of the DI registrations captured at <c>AddBitBevToolsServices</c> time (built lazily on first use).</summary>
    public BevToolsServiceGraph? ServiceGraph => _serviceGraph.Value;

    /// <summary>Built on first use, once: it analyzes every constructor in the collection.</summary>
    internal static Lazy<BevToolsServiceGraph?> CreateGraph(IServiceCollection? services) =>
        new(() => services is null ? null : BevToolsServiceGraph.Build(services), LazyThreadSafetyMode.ExecutionAndPublication);

    private sealed class ExtensionBuilder(List<string> errors) : IBevToolsExtensionBuilder
    {
        // Which extension added each panel id, so a clash between two of them is reported rather than silent.
        private readonly Dictionary<string, string> _panelOwners = new(StringComparer.Ordinal);

        /// <summary>The name of the extension whose <c>Configure</c> is running, for the errors it causes.</summary>
        public string Current { get; set; } = string.Empty;

        public List<BevToolsPanelDescriptor> Panels { get; } = [];

        public List<IBevToolsDiagnosticRule> Rules { get; } = [];

        public List<IBevToolsCommand> Commands { get; } = [];

        public List<Func<IServiceProvider, IBevToolsStateProvider>> StateProviderFactories { get; } = [];

        public Dictionary<Type, BevToolsValueFormatter> Formatters { get; } = [];

        public List<string> HiddenNamespaces { get; } = [];

        public IBevToolsExtensionBuilder AddPanel(BevToolsPanelDescriptor panel)
        {
            ArgumentNullException.ThrowIfNull(panel);
            if (!typeof(Microsoft.AspNetCore.Components.IComponent).IsAssignableFrom(panel.ComponentType))
            {
                throw new ArgumentException($"Panel '{panel.Id}' component type {panel.ComponentType.Name} is not a Blazor component.");
            }

            // The panel host shows a built-in panel for its id before it looks at extensions, so such a tab would open the
            // built-in panel instead of this one. Only the panel is left out, not by throwing: that would drop the rest of
            // the extension's Configure, its rules and commands after this call in a fluent chain.
            if (BevToolsPanel.IsBuiltInPanelId(panel.Id))
            {
                errors.Add($"{Current}: Panel id '{panel.Id}' is a built-in panel's; the panel is left out.");
                return this;
            }

            if (_panelOwners.TryGetValue(panel.Id, out var owner) && !string.Equals(owner, Current, StringComparison.Ordinal))
            {
                errors.Add($"{Current}: Panel id '{panel.Id}' replaces the panel {owner} added.");
            }

            Panels.RemoveAll(p => p.Id == panel.Id);
            Panels.Add(panel);
            _panelOwners[panel.Id] = Current;
            return this;
        }

        public IBevToolsExtensionBuilder AddDiagnosticRule(IBevToolsDiagnosticRule rule)
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

        public IBevToolsExtensionBuilder AddStateProvider(Func<IServiceProvider, IBevToolsStateProvider> factory)
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
