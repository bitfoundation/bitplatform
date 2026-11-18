
namespace Bit.BevTools;

/// <summary>Collects everything an extension contributes. Obtained through <see cref="IBevToolsExtension.Configure"/>.</summary>
public interface IBevToolsExtensionBuilder
{
    IBevToolsExtensionBuilder AddPanel(BevToolsPanelDescriptor panel);

    IBevToolsExtensionBuilder AddDiagnosticRule(IBevToolsDiagnosticRule rule);

    IBevToolsExtensionBuilder AddCommand(IBevToolsCommand command);

    /// <summary>Registers a state provider factory. It is invoked once per BevTools session (per circuit on the server).</summary>
    IBevToolsExtensionBuilder AddStateProvider(Func<IServiceProvider, IBevToolsStateProvider> factory);

    IBevToolsExtensionBuilder AddValueFormatter(Type type, BevToolsValueFormatter formatter);

    /// <summary>Marks a namespace prefix whose components are hidden from the component tree (library internals).</summary>
    IBevToolsExtensionBuilder HideComponentsInNamespace(string namespacePrefix);
}
