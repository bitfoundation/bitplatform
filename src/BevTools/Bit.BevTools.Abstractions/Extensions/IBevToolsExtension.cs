
namespace Bit.BevTools;

/// <summary>Describes an additional panel. <see cref="ComponentType"/> must be a Blazor component; it receives no parameters and can inject BevTools services.</summary>
public sealed record BevToolsPanelDescriptor(string Id, string Title, Type ComponentType, int Order = 100)
{
    /// <summary>Optional platform restriction; when set the panel is only shown when the renderer name matches (e.g. "Server").</summary>
    public string? RequiredPlatform { get; init; }
}

/// <summary>Formats values of a type for display in inspectors. Return null to fall back to default formatting.</summary>
public delegate string? BevToolsValueFormatter(object value);

/// <summary>Collects everything an extension contributes. Obtained through <see cref="IBevToolsExtension.Configure"/>.</summary>
public interface IBevToolsExtensionBuilder
{
    IBevToolsExtensionBuilder AddPanel(BevToolsPanelDescriptor panel);

    IBevToolsExtensionBuilder AddDiagnosticRule(IBevDiagnosticRule rule);

    IBevToolsExtensionBuilder AddCommand(IBevToolsCommand command);

    /// <summary>Registers a state provider factory. It is invoked once per BevTools session (per circuit on the server).</summary>
    IBevToolsExtensionBuilder AddStateProvider(Func<IServiceProvider, IBevStateProvider> factory);

    IBevToolsExtensionBuilder AddValueFormatter(Type type, BevToolsValueFormatter formatter);

    /// <summary>Marks a namespace prefix whose components are hidden from the component tree (library internals).</summary>
    IBevToolsExtensionBuilder HideComponentsInNamespace(string namespacePrefix);
}

/// <summary>
/// Implemented by libraries that integrate with BevTools. Register with
/// <c>services.AddBitBevToolsServices(o =&gt; o.Extensions.Add(new MyExtension()))</c> or <c>services.AddBitBevToolsServicesExtension&lt;T&gt;()</c>.
/// </summary>
public interface IBevToolsExtension
{
    string Name { get; }

    void Configure(IBevToolsExtensionBuilder builder);
}
