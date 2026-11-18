
namespace Bit.BevTools;

/// <summary>
/// Implemented by libraries that integrate with BevTools. Register with
/// <c>services.AddBitBevToolsServices(o =&gt; o.Extensions.Add(new MyExtension()))</c> or <c>services.AddBitBevToolsServicesExtension&lt;T&gt;()</c>.
/// </summary>
public interface IBevToolsExtension
{
    string Name { get; }

    void Configure(IBevToolsExtensionBuilder builder);
}
